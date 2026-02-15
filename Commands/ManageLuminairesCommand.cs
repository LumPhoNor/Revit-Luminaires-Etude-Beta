using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Commands
{
    /// <summary>
    /// Commande pour ouvrir le catalogue de luminaires
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ManageLuminairesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Ouvrir la fenêtre du catalogue
                var catalogWindow = new LuminaireCatalogWindow();
                catalogWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Erreur : {ex.Message}";
                TaskDialog.Show("Erreur", $"Impossible d'ouvrir le catalogue de luminaires.\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}