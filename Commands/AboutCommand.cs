using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class AboutCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var dlg = new TaskDialog("Skylightning — À propos");
            dlg.MainInstruction = "Skylightning";
            dlg.MainContent =
                "Plugin d'analyse photométrique pour Autodesk Revit\n\n" +
                "Version : 1.0.0\n" +
                "Norme   : EN 12464-1\n" +
                "Moteur  : Radiosité Monte Carlo (64 rayons)\n\n" +
                "© 2026 Soufiane Ben Ahmed Ghandri — Tous droits réservés\n" +
                "Contact : skylightning.support@gmail.com";
            dlg.Show();
            return Result.Succeeded;
        }
    }
}
