using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Ouvrir la fenêtre de sélection des pièces AVEC choix du type d'activité
                var roomSelectionWindow = new RoomSelectionWindow(doc);
                if (roomSelectionWindow.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                var selectedRooms = roomSelectionWindow.SelectedRooms;
                var roomActivities = roomSelectionWindow.RoomActivities;

                if (selectedRooms == null || selectedRooms.Count == 0)
                {
                    TaskDialog.Show("Erreur", "Aucune pièce sélectionnée.");
                    return Result.Failed;
                }

                // Ouvrir la fenêtre de configuration d'analyse
                var analysisWindow = new LightingAnalysisWindow(doc);
                if (analysisWindow.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                var settings = analysisWindow.Settings;
                double maintenanceFactor = analysisWindow.MaintenanceFactor;

                // Effectuer les calculs
                var calculator = new LightingCalculator(doc);
                var results = new List<CalculationResult>();

                // Export des vues 2D/3D
                string tempFolder = Path.Combine(Path.GetTempPath(), "RevitLightingPlugin", "Views");
                var viewExporter = new ViewExporter(doc, tempFolder);
                var roomViewExports = new Dictionary<ElementId, RoomViewsExport>();

                // Exporter vues pour chaque pièce
                foreach (var room in selectedRooms)
                {
                    var viewExport = viewExporter.ExportRoomViews(room);
                    roomViewExports[room.Id] = viewExport;
                }

                // Effectuer les calculs
                foreach (var room in selectedRooms)
                {
                    try
                    {
                        // Récupérer le type d'activité choisi pour cette pièce
                        RoomActivityType activityType = null;
                        if (roomActivities.ContainsKey(room.Id))
                        {
                            activityType = roomActivities[room.Id];
                        }

                        // Créer le résultat de base (propriétés au niveau pièce)
                        var result = new CalculationResult
                        {
                            RoomName = room.Name,
                            RoomNumber = room.Number,
                            GridSpacing = settings.GridSpacing
                        };

                        Parameter areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
                        if (areaParam != null && areaParam.HasValue)
                        {
                            result.RoomArea = areaParam.AsDouble() * 0.092903;
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
                                        GridMapGenerator.GenerateGridMap(room, lightingResult.GridPoints, requiredLux, gridMapPath, settings.GridSpacing);
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

                                // Récupérer les luminaires (une seule fois, indépendant de la hauteur)
                                if (result.LuminaireCount == 0)
                                {
                                    result.LuminaireCount = lightingResult.LuminaireCount;
                                    result.PuissanceTotale = lightingResult.TotalPower;

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

                            // Vérifier la conformité selon le type d'activité
                            result.MeetsStandard = result.AverageIlluminance >= activityType.RequiredLux &&
                                                  result.Uniformity >= activityType.UniformityMin;

                            // Générer des recommandations personnalisées
                            if (!result.MeetsStandard)
                            {
                                var recommendations = new List<string>();

                                if (result.AverageIlluminance < activityType.RequiredLux)
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

                                if (result.Uniformity < activityType.UniformityMin)
                                {
                                    recommendations.Add($"Uniformité insuffisante : {result.Uniformity:F2} au lieu de {activityType.UniformityMin:F2} minimum");
                                    recommendations.Add("Suggestion : mieux répartir les luminaires dans l'espace");
                                }

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

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
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
                    TaskDialog.Show("Attention", "Aucun résultat de calcul disponible.");
                    return Result.Failed;
                }

                // Afficher les résultats - CORRECTION ICI !
                var resultsWindow = new ResultsWindow(uidoc, results);
                resultsWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Erreur", $"Une erreur s'est produite :\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}