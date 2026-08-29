using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Core;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ParametresCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Logger.Separator("PARAMETRES COMMAND");
            Logger.Info("ParametresCmd", "Ouverture des paramètres d'analyse");

            try
            {
                var doc = commandData.Application.ActiveUIDocument.Document;

                // 1. Sélection des pièces + type d'activité
                var roomWin = new RoomSelectionWindow(doc);
                if (roomWin.ShowDialog() != true)
                {
                    Logger.Warning("ParametresCmd", "Sélection de pièces annulée");
                    return Result.Cancelled;
                }

                var selectedRooms  = roomWin.SelectedRooms;
                var roomActivities = roomWin.RoomActivities;

                if (selectedRooms == null || selectedRooms.Count == 0)
                {
                    TaskDialog.Show("Skylightning", "Aucune pièce sélectionnée.");
                    return Result.Failed;
                }

                // 2. Configuration de l'analyse (grille, hauteurs, MF…)
                var analysisWin = new LightingAnalysisWindow(doc);
                if (analysisWin.ShowDialog() != true)
                {
                    Logger.Warning("ParametresCmd", "Configuration d'analyse annulée");
                    return Result.Cancelled;
                }

                // 3. Sélection des vues 2D/3D par pièce
                var viewWin = new ViewSelectionWindow(doc, selectedRooms);
                if (viewWin.ShowDialog() != true)
                {
                    Logger.Warning("ParametresCmd", "Sélection des vues annulée");
                    return Result.Cancelled;
                }

                // Persister dans la session
                CalculationSession.SelectedRooms    = selectedRooms;
                CalculationSession.RoomActivities   = roomActivities;
                CalculationSession.Settings         = analysisWin.Settings;
                CalculationSession.MaintenanceFactor = analysisWin.MaintenanceFactor;
                CalculationSession.ViewSelections   = viewWin.Selections;

                Logger.Info("ParametresCmd",
                    $"Session sauvegardée : {selectedRooms.Count} pièce(s), " +
                    $"grille={analysisWin.Settings.GridSpacing}m");

                RevitLightingPlugin.Application.SetCalculReady(true);

                TaskDialog.Show("Skylightning — Paramètres",
                    $"Configuration enregistrée pour {selectedRooms.Count} pièce(s).\n\n" +
                    "Cliquez sur « Calcul » pour lancer l'analyse.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Critical("ParametresCmd", "Erreur dans ParametresCommand", ex);
                message = ex.Message;
                TaskDialog.Show("Erreur", $"Une erreur s'est produite :\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
