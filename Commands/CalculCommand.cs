using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Core;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CalculCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Separator("CALCUL COMMAND");
            Logger.Info("CalculCmd", "Lancement du calcul d'éclairement");

            if (!CalculationSession.IsConfigured)
            {
                TaskDialog.Show("Skylightning",
                    "Aucun paramètre configuré.\n\n" +
                    "Veuillez d'abord cliquer sur « Paramètres » pour configurer " +
                    "les pièces et les options de calcul.");
                return Result.Cancelled;
            }

            try
            {
                var uidoc        = commandData.Application.ActiveUIDocument;
                var doc          = uidoc.Document;
                var selectedRooms = CalculationSession.SelectedRooms;
                var roomActivities = CalculationSession.RoomActivities;
                var settings     = CalculationSession.Settings;
                var viewSelections = CalculationSession.ViewSelections
                                     ?? new Dictionary<ElementId, RoomViewSelection>();

                Logger.Info("CalculCmd",
                    $"Session : {selectedRooms.Count} pièce(s), grille={settings.GridSpacing}m");

                var loadingWindow = LoadingWindow.ShowLoading();

                try
                {
                    loadingWindow?.SetStatus("Initialisation du calculateur...");
                    var calculator = new LightingCalculator(doc);
                    var results    = new List<CalculationResult>();

                    string tempFolder = Path.Combine(Path.GetTempPath(), "RevitLightingPlugin", "Views");
                    var viewExporter  = new ViewExporter(doc, tempFolder);
                    var roomViewExports = new Dictionary<ElementId, RoomViewsExport>();

                    // Export vues 2D/3D
                    loadingWindow?.SetStatus("Export des vues 2D/3D...");
                    foreach (var room in selectedRooms)
                    {
                        loadingWindow?.SetStatus($"Export vue : {room.Name}");
                        ElementId planId   = null;
                        ElementId view3dId = null;
                        if (viewSelections.ContainsKey(room.Id))
                        {
                            planId   = viewSelections[room.Id].PlanViewId;
                            view3dId = viewSelections[room.Id].View3DId;
                        }
                        roomViewExports[room.Id] = viewExporter.ExportRoomViews(room, planId, view3dId);
                    }

                    // Calculs photométriques
                    Logger.Separator("CALCULS D'ÉCLAIREMENT");
                    loadingWindow?.SetStatus("Calcul photométrique en cours...");

                    foreach (var room in selectedRooms)
                    {
                        var roomSw = Stopwatch.StartNew();
                        Logger.Info("CalculCmd", $"📊 Pièce : {room.Name} ({room.Number})");
                        loadingWindow?.SetStatus($"Analyse : {room.Name}");

                        try
                        {
                            RoomActivityType activityType = null;
                            if (roomActivities != null && roomActivities.ContainsKey(room.Id))
                                activityType = roomActivities[room.Id];

                            var result = new CalculationResult
                            {
                                RoomName    = room.Name,
                                RoomNumber  = room.Number,
                                GridSpacing = settings.GridSpacing,
                                WallMargin  = settings.WallMargin
                            };

                            var areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
                            if (areaParam != null && areaParam.HasValue)
                                result.RoomArea = areaParam.AsDouble() * 0.092903;

                            var heightParam = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
                            if (heightParam != null && heightParam.HasValue && heightParam.AsDouble() > 0)
                                result.HauteurPiece = heightParam.AsDouble() * 0.3048;
                            else
                            {
                                var bbox = room.get_BoundingBox(null);
                                if (bbox != null)
                                    result.HauteurPiece = (bbox.Max.Z - bbox.Min.Z) * 0.3048;
                            }

                            if (roomViewExports.ContainsKey(room.Id))
                            {
                                result.PlanImagePath  = roomViewExports[room.Id].PlanImagePath;
                                result.View3DImagePath = roomViewExports[room.Id].View3DImagePath;
                            }

                            result.HeightResults = new List<HeightAnalysisResult>();

                            foreach (double height in settings.WorkPlaneHeights)
                            {
                                var lr = calculator.CalculateForRoom(room, settings, height);
                                if (lr == null) continue;

                                string gridMapPath  = null;
                                string heatmap3DPath = null;

                                if (lr.GridPoints.Count > 0)
                                {
                                    int reqLux = activityType?.RequiredLux ?? 500;

                                    gridMapPath = Path.Combine(tempFolder,
                                        $"GridMap_{room.Id.Value}_H{height:F2}.png");
                                    try { GridMapGenerator.GenerateGridMap(room, lr.GridPoints, reqLux, gridMapPath, settings.GridSpacing, settings.WallMargin); }
                                    catch { gridMapPath = null; }

                                    heatmap3DPath = Path.Combine(tempFolder,
                                        $"Heatmap3D_{room.Id.Value}_H{height:F2}.png");
                                    try { Heatmap3DGenerator.GenerateHeatmap3D(lr.GridPoints, room.Name, reqLux, heatmap3DPath); }
                                    catch { heatmap3DPath = null; }
                                }

                                result.HeightResults.Add(new HeightAnalysisResult
                                {
                                    WorkPlaneHeight    = height,
                                    AverageIlluminance = lr.AverageIlluminance,
                                    MinIlluminance     = lr.MinIlluminance,
                                    MaxIlluminance     = lr.MaxIlluminance,
                                    Uniformity         = lr.UniformityRatio,
                                    LocalUniformity    = lr.LocalUniformity,
                                    GridMapPath        = gridMapPath,
                                    Heatmap3DPath      = heatmap3DPath,
                                    GridPoints         = lr.GridPoints,
                                    MeetsStandard      = lr.AverageIlluminance >= (activityType?.RequiredLux ?? 500)
                                });

                                if (result.LuminaireCount == 0)
                                {
                                    result.LuminaireCount = lr.LuminaireCount;
                                    result.PuissanceTotale = lr.TotalPower;
                                    result.LuminaireCalculatedHeightMeters = lr.LuminaireCalculatedHeightMeters;

                                    if (lr.Luminaires != null)
                                    {
                                        result.LuminairesUtilises = lr.Luminaires
                                            .GroupBy(l => new { l.Nom, l.Fabricant, l.Reference })
                                            .Select(g => new LuminaireUsageInfo
                                            {
                                                TypeName           = g.Key.Nom,
                                                Fabricant          = g.Key.Fabricant,
                                                Reference          = g.Key.Reference,
                                                Quantity           = g.Count(),
                                                FluxLumineux       = g.First().FluxLumineux,
                                                Puissance          = g.First().Puissance,
                                                TemperatureCouleur = g.First().TemperatureCouleur
                                            }).ToList();
                                    }
                                }

                                Logger.Info("CalculCmd",
                                    $"  ✅ h={height:F2}m => Em={lr.AverageIlluminance:F0} lux");
                            }

                            if (result.HeightResults.Count > 0)
                            {
                                var primary = result.HeightResults[0];
                                result.AverageIlluminance = primary.AverageIlluminance;
                                result.MinIlluminance     = primary.MinIlluminance;
                                result.MaxIlluminance     = primary.MaxIlluminance;
                                result.Uniformity         = primary.Uniformity;
                                result.LocalUniformity    = primary.LocalUniformity;
                                result.GridMapPath        = primary.GridMapPath;
                            }

                            if (activityType != null)
                            {
                                result.EclairementRequis = activityType.RequiredLux;
                                result.UniformiteRequise = activityType.UniformityMin;
                                result.TypeActivite      = activityType.DisplayName;
                                result.MeetsStandard     = result.AverageIlluminance >= activityType.RequiredLux;

                                var recs = new List<string>();
                                if (result.AverageIlluminance < activityType.RequiredLux)
                                {
                                    double deficit = activityType.RequiredLux - result.AverageIlluminance;
                                    recs.Add($"Éclairement insuffisant : {result.AverageIlluminance:F0} lux " +
                                             $"(requis {activityType.RequiredLux} lux, déficit {deficit / activityType.RequiredLux * 100:F0}%)");
                                    if (result.LuminaireCount > 0 && result.AverageIlluminance > 0)
                                    {
                                        int add = (int)Math.Ceiling(result.LuminaireCount * (activityType.RequiredLux / result.AverageIlluminance - 1));
                                        recs.Add($"Suggestion : ajouter environ {add} luminaire(s) similaire(s)");
                                    }
                                }
                                if (result.Uniformity < activityType.UniformityMin)
                                    recs.Add($"Uniformité à améliorer : {result.Uniformity:F2} (min {activityType.UniformityMin:F2})");

                                result.Remarques = recs.Count > 0
                                    ? string.Join("\n", recs)
                                    : $"✓ Conforme EN 12464-1 pour {activityType.DisplayName}";
                            }
                            else
                            {
                                result.EclairementRequis = 500;
                                result.UniformiteRequise = 0.60;
                                result.TypeActivite      = "Non spécifié";
                                result.MeetsStandard     = result.AverageIlluminance >= 500;
                            }

                            if (result.RoomArea > 0)
                                result.DensitePuissance = result.PuissanceTotale / result.RoomArea;

                            roomSw.Stop();
                            Logger.Performance($"Calcul {room.Name}", roomSw.Elapsed);
                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("CalculCmd", $"Erreur pièce {room.Name}", ex);
                            TaskDialog.Show("Erreur de calcul",
                                $"Erreur pour la pièce {room.Name} :\n{ex.Message}");
                        }
                    }

                    // Nettoyage vues temporaires
                    var toClean = roomViewExports.Values
                        .SelectMany(v => new[] { v.PlanViewId, v.View3DId })
                        .Where(id => id != ElementId.InvalidElementId)
                        .ToList();
                    viewExporter.CleanupTemporaryViews(toClean);

                    if (results.Count == 0)
                    {
                        LoadingWindow.CloseInstance();
                        TaskDialog.Show("Attention", "Aucun résultat de calcul disponible.");
                        return Result.Failed;
                    }

                    loadingWindow?.SetStatus("Génération du rapport...");
                    System.Threading.Thread.Sleep(400);
                    LoadingWindow.CloseInstance();

                    var resultsWindow = new ResultsWindow(uidoc, results);
                    resultsWindow.ShowDialog();

                    stopwatch.Stop();
                    Logger.Performance("Analyse complète", stopwatch.Elapsed);
                    Logger.Info("CalculCmd", $"✅ {results.Count} résultat(s) calculé(s)");
                    Logger.Separator();
                    return Result.Succeeded;
                }
                finally
                {
                    LoadingWindow.CloseInstance();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoadingWindow.CloseInstance();
                Logger.Critical("CalculCmd", "Erreur critique dans CalculCommand", ex);
                Logger.Separator();
                message = ex.Message;
                TaskDialog.Show("Erreur", $"Une erreur s'est produite :\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
