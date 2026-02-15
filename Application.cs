using System;
using Autodesk.Revit.UI;
using System.Reflection;

namespace RevitLightingPlugin
{
    /// <summary>
    /// Point d'entrée principal du plugin Revit
    /// Crée l'interface utilisateur (onglet + boutons)
    /// </summary>
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Créer onglet personnalisé "Éclairage"
                string tabName = "Éclairage";
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // L'onglet existe déjà, on continue
                }

                // Créer panneau "Analyse"
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Analyse");

                // Chemin vers notre DLL
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // BOUTON 1 : Analyse d'éclairement
                PushButtonData buttonAnalyzeData = new PushButtonData(
                    "LightingAnalysis",
                    "Analyse\nÉclairement",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.LightingAnalysisCommand"
                );
                buttonAnalyzeData.ToolTip = "Analyse l'éclairement des pièces sélectionnées";
                buttonAnalyzeData.LongDescription = "Ouvre une interface pour sélectionner les pièces à analyser et calcule l'éclairement selon les normes EN 12464-1.";

                PushButton buttonAnalyze = panel.AddItem(buttonAnalyzeData) as PushButton;

                // BOUTON 2 : Catalogue de luminaires
                PushButtonData buttonCatalogData = new PushButtonData(
                    "LuminaireCatalog",
                    "Catalogue\nLuminaires",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.ManageLuminairesCommand"
                );
                buttonCatalogData.ToolTip = "Gérer le catalogue de luminaires";
                buttonCatalogData.LongDescription = "Ouvre le catalogue de luminaires pour ajouter, modifier ou supprimer des luminaires, et importer des fichiers IES.";

                PushButton buttonCatalog = panel.AddItem(buttonCatalogData) as PushButton;

                // BOUTON 3 : Test Parser IES (Phase 2)
                PushButtonData buttonTestIESData = new PushButtonData(
                    "TestIESParser",
                    "Test\nParser IES",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.TestIESParserCommand"
                );
                buttonTestIESData.ToolTip = "Tester le parser IES";
                buttonTestIESData.LongDescription = "Ouvre un fichier IES et affiche toutes les données extraites (flux, puissance, fabricant, courbe photométrique, etc.)";

                PushButton buttonTestIES = panel.AddItem(buttonTestIESData) as PushButton;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Erreur", $"Erreur au démarrage du plugin :\n{ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Nettoyage si nécessaire
            return Result.Succeeded;
        }
    }
}
