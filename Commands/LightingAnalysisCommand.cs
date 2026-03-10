using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.Core;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class LightingAnalysisCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Separator("LIGHTING ANALYSIS COMMAND");
            Logger.Info("LightingAnalysisCmd", "🚀 Commande d'analyse d'éclairement lancée");
            Logger.EnterMethod("LightingAnalysisCommand", "Execute");

            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                Logger.Info("LightingAnalysisCmd", $"Document: {doc.Title}");

                // Ouvrir la fenêtre de sélection des pièces AVEC choix du type d'activité
                Logger.Debug("LightingAnalysisCmd", "Ouverture fenêtre de sélection des pièces");
                var roomSelectionWindow = new RoomSelectionWindow(doc);
                if (roomSelectionWindow.ShowDialog() != true)
                {
                    Logger.Warning("LightingAnalysisCmd", "Sélection de pièces annulée par l'utilisateur");
                    Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Cancelled");
                    return Result.Cancelled;
                }

                var selectedRooms = roomSelectionWindow.SelectedRooms;
                var roomActivities = roomSelectionWindow.RoomActivities;
                Logger.Info("LightingAnalysisCmd", $"{selectedRooms.Count} pièce(s) sélectionnée(s)");

                if (selectedRooms == null || selectedRooms.Count == 0)
                {
                    Logger.Warning("LightingAnalysisCmd", "Aucune pièce sélectionnée");
                    TaskDialog.Show("Erreur", "Aucune pièce sélectionnée.");
                    Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Failed");
                    return Result.Failed;
                }

                // Ouvrir la fenêtre de configuration d'analyse
                Logger.Debug("LightingAnalysisCmd", "Ouverture fenêtre de configuration d'analyse");
                var analysisWindow = new LightingAnalysisWindow(doc);
                if (analysisWindow.ShowDialog() != true)
                {
                    Logger.Warning("LightingAnalysisCmd", "Configuration d'analyse annulée par l'utilisateur");
                    Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Cancelled");
                    return Result.Cancelled;
                }

                var settings = analysisWindow.Settings;
                double maintenanceFactor = analysisWindow.MaintenanceFactor;
                Logger.Info("LightingAnalysisCmd", $"Paramètres: GridSpacing={settings.GridSpacing}m, Heights={string.Join(",", settings.WorkPlaneHeights)}m, MF={maintenanceFactor:F2}");

                // Ouvrir la fenêtre de sélection des vues 2D/3D
                Logger.Debug("LightingAnalysisCmd", "Ouverture fenêtre de sélection des vues");
                var viewSelectionWindow = new ViewSelectionWindow(doc, selectedRooms);
                if (viewSelectionWindow.ShowDialog() != true)
                {
                    Logger.Warning("LightingAnalysisCmd", "Sélection des vues annulée par l'utilisateur");
                    Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Cancelled");
                    return Result.Cancelled;
                }
                var viewSelections = viewSelectionWindow.Selections;
                Logger.Info("LightingAnalysisCmd", $"Sélection des vues confirmée pour {viewSelections.Count} pièce(s)");

                // Afficher la fenêtre de chargement animée
                var loadingWindow = LoadingWindow.ShowLoading();

                try
                {
                // Effectuer les calculs
                Logger.Info("LightingAnalysisCmd", "Initialisation du calculateur d'éclairement");
                loadingWindow?.SetStatus("Initialisation du calculateur...");
                var calculator = new LightingCalculator(doc);
                var results = new List<CalculationResult>();

                // Export des vues 2D/3D
                string tempFolder = Path.Combine(Path.GetTempPath(), "RevitLightingPlugin", "Views");
                Logger.Debug("LightingAnalysisCmd", $"Dossier temporaire vues: {tempFolder}");
                var viewExporter = new ViewExporter(doc, tempFolder);
                var roomViewExports = new Dictionary<ElementId, RoomViewsExport>();

                // Exporter vues pour chaque pièce (avec les choix de l'utilisateur)
                Logger.Info("LightingAnalysisCmd", "Export des vues 2D/3D des pièces");
                loadingWindow?.SetStatus("Export des vues 2D/3D...");
                foreach (var room in selectedRooms)
                {
                    Logger.Debug("LightingAnalysisCmd", $"Export vues pour pièce: {room.Name}");
                    loadingWindow?.SetStatus($"Export vue : {room.Name}");

                    ElementId planId  = null; // null = Automatique par défaut
                    ElementId view3dId = null;
                    if (viewSelections.ContainsKey(room.Id))
                    {
                        planId   = viewSelections[room.Id].PlanViewId;
                        view3dId = viewSelections[room.Id].View3DId;
                    }

                    var viewExport = viewExporter.ExportRoomViews(room, planId, view3dId);
                    roomViewExports[room.Id] = viewExport;
                }

                // Effectuer les calculs
                Logger.Separator("CALCULS D'ÉCLAIREMENT");
                Logger.Info("LightingAnalysisCmd", $"Début des calculs pour {selectedRooms.Count} pièce(s)");
                loadingWindow?.SetStatus("Calcul photométrique en cours...");

                foreach (var room in selectedRooms)
                {
                    var roomStopwatch = Stopwatch.StartNew();
                    Logger.Info("LightingAnalysisCmd", $"📊 Calcul pour pièce: {room.Name} ({room.Number})");
                    loadingWindow?.SetStatus($"Analyse : {room.Name}");

                    try
                    {
                        // Récupérer le type d'activité choisi pour cette pièce
                        RoomActivityType activityType = null;
                        if (roomActivities.ContainsKey(room.Id))
                        {
                            activityType = roomActivities[room.Id];
                            Logger.Debug("LightingAnalysisCmd", $"Type d'activité: {activityType.DisplayName} (requis: {activityType.RequiredLux} lux)");
                        }

                        // Créer le résultat de base (propriétés au niveau pièce)
                        var result = new CalculationResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            GridSpacing = settings.GridSpacing,
                            WallMargin = settings.WallMargin
                        };

                        Parameter areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
                        if (areaParam != null && areaParam.HasValue)
                        {
                            result.RoomArea = areaParam.AsDouble() * 0.092903;
                        }

                        // Hauteur sous plafond
                        Parameter heightParam = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
                        if (heightParam != null && heightParam.HasValue && heightParam.AsDouble() > 0)
                        {
                            result.HauteurPiece = heightParam.AsDouble() * 0.3048;
                        }
                        else
                        {
                            BoundingBoxXYZ roomBbox = room.get_BoundingBox(null);
                            if (roomBbox != null)
                                result.HauteurPiece = (roomBbox.Max.Z - roomBbox.Min.Z) * 0.3048;
                        }

                        // Ajouter les chemins des images exportées
                        if (roomViewExports.ContainsKey(room.Id))
                        {
                            var viewExport = roomViewExports[room.Id];
                            result.PlanImagePath = viewExport.PlanImagePath;
                            result.View3DImagePath = viewExport.View3DImagePath;
                        }

                        // Calculer pour chaque hauteur
                        result.HeightResults = new List<HeightAnalysisResult>();

                        foreach (double height in settings.WorkPlaneHeights)
                        {
                            Logger.Debug("LightingAnalysisCmd", $"Calcul pour hauteur: {height:F2}m");
                            var lightingResult = calculator.CalculateForRoom(room, settings, height);

                            if (lightingResult != null)
                            {
                                // Générer le grid map pour cette hauteur
                                string gridMapPath = null;
                                if (lightingResult.GridPoints.Count > 0)
                                {
                                    gridMapPath = Path.Combine(tempFolder, $"GridMap_{room.Id.Value}_H{height:F2}.png");
                                    try
                                    {
                                        int requiredLux = activityType != null ? activityType.RequiredLux : 500;
                                        GridMapGenerator.GenerateGridMap(room, lightingResult.GridPoints, requiredLux, gridMapPath, settings.GridSpacing, settings.WallMargin);
                                    }
                                    catch
                                    {
                                        gridMapPath = null;
                                    }
                                }

                                // Générer le heatmap 3D pour cette hauteur
                                string heatmap3DPath = null;
                                if (lightingResult.GridPoints.Count > 0)
                                {
                                    heatmap3DPath = Path.Combine(tempFolder, $"Heatmap3D_{room.Id.Value}_H{height:F2}.png");
                                    try
                                    {
                                        int requiredLux = activityType != null ? activityType.RequiredLux : 500;
                                        Heatmap3DGenerator.GenerateHeatmap3D(lightingResult.GridPoints, room.Name, requiredLux, heatmap3DPath);
                                    }
                                    catch
                                    {
                                        heatmap3DPath = null;
                                    }
                                }

                                var heightResult = new HeightAnalysisResult
                                {
                                    WorkPlaneHeight = height,
                                    AverageIlluminance = lightingResult.AverageIlluminance,
                                    MinIlluminance = lightingResult.MinIlluminance,
                                    MaxIlluminance = lightingResult.MaxIlluminance,
                                    Uniformity = lightingResult.UniformityRatio,
                                    LocalUniformity = lightingResult.LocalUniformity, // P4
                                    GridMapPath = gridMapPath,
                                    Heatmap3DPath = heatmap3DPath,
                                    GridPoints = lightingResult.GridPoints,
                                    MeetsStandard = activityType != null
                                        ? lightingResult.AverageIlluminance >= activityType.RequiredLux
                                        : lightingResult.AverageIlluminance >= 500
                                };

                                result.HeightResults.Add(heightResult);
                                Logger.Info("LightingAnalysisCmd", $"  ✅ h={height:F2}m => Em={heightResult.AverageIlluminance:F0} lux, U0={heightResult.Uniformity:F2}");

                                // Récupérer les luminaires (une seule fois, indépendant de la hauteur)
                                if (result.LuminaireCount == 0)
                                {
                                    result.LuminaireCount = lightingResult.LuminaireCount;
                                    result.PuissanceTotale = lightingResult.TotalPower;
                                    result.LuminaireCalculatedHeightMeters = lightingResult.LuminaireCalculatedHeightMeters;

                                    if (lightingResult.Luminaires != null)
                                    {
                                        var groupedLuminaires = lightingResult.Luminaires
                                            .GroupBy(l => new { l.Nom, l.Fabricant, l.Reference })
                                            .Select(g => new LuminaireUsageInfo
                                            {
                                                TypeName = g.Key.Nom,
                                                Fabricant = g.Key.Fabricant,
                                                Reference = g.Key.Reference,
                                                Quantity = g.Count(),
                                                FluxLumineux = g.First().FluxLumineux,
                                                Puissance = g.First().Puissance,
                                                TemperatureCouleur = g.First().TemperatureCouleur
                                            })
                                            .ToList();

                                        result.LuminairesUtilises = groupedLuminaires;
                                    }
                                }
                            }
                        }

                        // Utiliser les valeurs de la première hauteur comme valeurs principales
                        if (result.HeightResults.Count > 0)
                        {
                            var primary = result.HeightResults[0];
                            result.AverageIlluminance = primary.AverageIlluminance;
                            result.MinIlluminance = primary.MinIlluminance;
                            result.MaxIlluminance = primary.MaxIlluminance;
                            result.Uniformity = primary.Uniformity;
                            result.LocalUniformity = primary.LocalUniformity; // P4
                            result.GridMapPath = primary.GridMapPath;
                        }

                        // Utiliser les valeurs du type d'activité
                        if (activityType != null)
                        {
                            result.EclairementRequis = activityType.RequiredLux;
                            result.UniformiteRequise = activityType.UniformityMin;
                            result.TypeActivite = activityType.DisplayName;

                            // Vérifier la conformité : basée UNIQUEMENT sur l'éclairement moyen
                            result.MeetsStandard = result.AverageIlluminance >= activityType.RequiredLux;

                            // Générer des recommandations personnalisées
                            var recommendations = new List<string>();
                            bool illuminanceOk = result.AverageIlluminance >= activityType.RequiredLux;
                            bool uniformityOk = result.Uniformity >= activityType.UniformityMin;

                            if (!illuminanceOk)
                            {
                                double deficit = activityType.RequiredLux - result.AverageIlluminance;
                                double percentageDeficit = (deficit / activityType.RequiredLux) * 100;
                                recommendations.Add($"Éclairement insuffisant : {result.AverageIlluminance:F0} lux au lieu de {activityType.RequiredLux} lux requis (déficit de {percentageDeficit:F0}%)");

                                // Suggestion de nombre de luminaires à ajouter
                                if (result.LuminaireCount > 0 && result.AverageIlluminance > 0)
                                {
                                    double ratio = activityType.RequiredLux / result.AverageIlluminance;
                                    int additionalLuminaires = (int)Math.Ceiling(result.LuminaireCount * (ratio - 1));
                                    recommendations.Add($"Suggestion : ajouter environ {additionalLuminaires} luminaire(s) similaire(s)");
                                }
                            }

                            if (!uniformityOk)
                            {
                                recommendations.Add($"Note : Uniformité à améliorer : {result.Uniformity:F2} au lieu de {activityType.UniformityMin:F2} minimum");
                                recommendations.Add("Suggestion : mieux répartir les luminaires dans l'espace");
                            }

                            if (recommendations.Count > 0)
                            {
                                result.Remarques = string.Join("\n", recommendations);
                            }
                            else
                            {
                                result.Remarques = $"✓ Conforme à la norme EN 12464-1 pour {activityType.DisplayName}";
                            }
                        }
                        else
                        {
                            // Fallback si pas de type d'activité défini
                            result.EclairementRequis = 500;
                            result.UniformiteRequise = 0.60;
                            result.TypeActivite = "Non spécifié";
                            result.MeetsStandard = result.AverageIlluminance >= 500;
                        }

                        // Calculer la densité de puissance
                        if (result.RoomArea > 0)
                        {
                            result.DensitePuissance = result.PuissanceTotale / result.RoomArea;
                        }

                        roomStopwatch.Stop();
                        Logger.Performance($"Calcul pièce {room.Name}", roomStopwatch.Elapsed);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LightingAnalysisCmd", $"Erreur calcul pour pièce {room.Name}", ex);
                        TaskDialog.Show("Erreur de calcul",
                            $"Erreur lors du calcul pour la pièce {room.Name} :\n{ex.Message}");
                    }
                }

                // Nettoyer les vues temporaires
                var viewIdsToClean = roomViewExports.Values
                    .SelectMany(v => new[] { v.PlanViewId, v.View3DId })
                    .Where(id => id != ElementId.InvalidElementId)
                    .ToList();

                viewExporter.CleanupTemporaryViews(viewIdsToClean);

                if (results.Count == 0)
                {
                    LoadingWindow.CloseInstance();
                    Logger.Warning("LightingAnalysisCmd", "Aucun résultat de calcul disponible");
                    TaskDialog.Show("Attention", "Aucun résultat de calcul disponible.");
                    Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Failed");
                    return Result.Failed;
                }

                // Fermer la fenêtre de chargement avant d'afficher les résultats
                loadingWindow?.SetStatus("Génération du rapport...");
                System.Threading.Thread.Sleep(400); // Laisser le temps de voir le message
                LoadingWindow.CloseInstance();

                // Afficher les résultats
                Logger.Info("LightingAnalysisCmd", $"✅ {results.Count} résultat(s) calculé(s) avec succès");
                Logger.Debug("LightingAnalysisCmd", "Affichage de la fenêtre de résultats");
                var resultsWindow = new ResultsWindow(uidoc, results);
                resultsWindow.ShowDialog();

                stopwatch.Stop();
                Logger.Performance("Analyse d'éclairement complète", stopwatch.Elapsed);
                Logger.Info("LightingAnalysisCmd", "✅ Commande terminée avec succès");
                Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Succeeded");
                Logger.Separator();
                return Result.Succeeded;
                } // fin try loading
                finally
                {
                    LoadingWindow.CloseInstance();
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LoadingWindow.CloseInstance();
                Logger.Critical("LightingAnalysisCmd", "Erreur critique dans la commande d'analyse", ex);
                Logger.ExitMethod("LightingAnalysisCommand", "Execute", "Result.Failed");
                Logger.Separator();

                message = ex.Message;
                TaskDialog.Show("Erreur", $"Une erreur s'est produite :\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}