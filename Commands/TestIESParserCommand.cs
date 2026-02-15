using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Core;
using System.IO;

namespace RevitLightingPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class TestIESParserCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Demander à l'utilisateur de sélectionner un fichier IES
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Sélectionnez un fichier IES",
                    Filter = "Fichiers IES (*.ies)|*.ies|Tous les fichiers (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openDialog.ShowDialog() != true)
                {
                    return Result.Cancelled;
                }

                string iesFilePath = openDialog.FileName;

                // Parser le fichier
                var data = IESParser.ParseFile(iesFilePath);

                // Afficher les résultats dans une TaskDialog
                string results = $"📄 Fichier : {data.FileName}\n\n" +
                                $"🏭 FABRICANT\n" +
                                $"   Fabricant : {data.Manufacturer}\n" +
                                $"   Référence : {data.CatalogNumber}\n" +
                                $"   Nom : {data.LuminaireName}\n\n" +
                                $"💡 PERFORMANCES\n" +
                                $"   Flux lumineux : {data.TotalLumens:F0} lm\n" +
                                $"   Puissance : {data.InputWatts:F1} W\n" +
                                $"   Efficacité : {data.Efficacy:F1} lm/W\n\n" +
                                $"📐 DIMENSIONS\n" +
                                $"   Largeur : {data.Width:F3} m\n" +
                                $"   Longueur : {data.Length:F3} m\n" +
                                $"   Hauteur : {data.Height:F3} m\n\n" +
                                $"📊 PHOTOMÉTRIE\n" +
                                $"   Angles verticaux : {data.NumberOfVerticalAngles}\n" +
                                $"   Angles horizontaux : {data.NumberOfHorizontalAngles}\n" +
                                $"   Candela max : {data.MaxCandela:F0} cd\n" +
                                $"   Candela moyenne : {data.AverageCandela:F0} cd";

                TaskDialog td = new TaskDialog("Résultats du parsing IES")
                {
                    MainInstruction = "✅ Fichier IES analysé avec succès !",
                    MainContent = results,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                
                td.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erreur", $"Erreur lors du parsing du fichier IES :\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
