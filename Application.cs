using System;
using Autodesk.Revit.UI;
using System.Reflection;
using RevitLightingPlugin.Core;

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
            // Initialiser le système de logging
            Logger.Initialize();
            Logger.Separator("APPLICATION STARTUP");
            Logger.Info("Application", "Démarrage du plugin RevitLightingPlugin");
            Logger.EnterMethod("Application", "OnStartup");

            try
            {
                // Créer onglet personnalisé "Éclairage"
                string tabName = "Éclairage";
                try
                {
                    application.CreateRibbonTab(tabName);
                    Logger.Info("Application", $"Onglet '{tabName}' créé");
                }
                catch
                {
                    // L'onglet existe déjà, on continue
                    Logger.Warning("Application", $"Onglet '{tabName}' existe déjà");
                }

                // Créer panneau "Analyse"
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Analyse");
                Logger.Info("Application", "Panneau 'Analyse' créé");

                // Chemin vers notre DLL
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                Logger.Debug("Application", $"Assembly path: {assemblyPath}");

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
                Logger.Info("Application", "Bouton 'Analyse Éclairement' ajouté");

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
                Logger.Info("Application", "Bouton 'Catalogue Luminaires' ajouté");

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
                Logger.Info("Application", "Bouton 'Test Parser IES' ajouté");

                Logger.Info("Application", "✅ Plugin démarré avec succès");
                Logger.ExitMethod("Application", "OnStartup", "Result.Succeeded");
                Logger.Separator();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Critical("Application", "Erreur critique au démarrage du plugin", ex);
                Logger.ExitMethod("Application", "OnStartup", "Result.Failed");
                TaskDialog.Show("Erreur", $"Erreur au démarrage du plugin :\n{ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Nettoyage si nécessaire
            Logger.Separator("APPLICATION SHUTDOWN");
            Logger.Info("Application", "Arrêt du plugin RevitLightingPlugin");
            Logger.EnterMethod("Application", "OnShutdown");

            try
            {
                // Nettoyage si nécessaire
                Logger.Info("Application", "✅ Plugin arrêté proprement");
                Logger.ExitMethod("Application", "OnShutdown", "Result.Succeeded");
                Logger.Close();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Application", "Erreur lors de l'arrêt du plugin", ex);
                Logger.Close();
                return Result.Failed;
            }
        }
    }
}
