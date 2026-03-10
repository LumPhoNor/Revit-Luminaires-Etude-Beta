using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Core;

namespace RevitLightingPlugin
{
    public class Application : IExternalApplication
    {
        private static readonly string LogoDir =
            @"C:\Users\User\Documents\Projets Plugin\Logo";

        public Result OnStartup(UIControlledApplication application)
        {
            Logger.Initialize();
            Logger.Separator("APPLICATION STARTUP");
            Logger.Info("Application", "Démarrage du plugin RevitLightingPlugin");
            Logger.EnterMethod("Application", "OnStartup");

            try
            {
                string tabName = "SkyLight";
                try
                {
                    application.CreateRibbonTab(tabName);
                    Logger.Info("Application", $"Onglet '{tabName}' créé");
                }
                catch
                {
                    Logger.Warning("Application", $"Onglet '{tabName}' existe déjà");
                }

                RibbonPanel panel = application.CreateRibbonPanel(tabName, "initium");

                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                PushButtonData buttonData = new PushButtonData(
                    "LightingAnalysis",
                    "Analyse\nÉclairement",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.LightingAnalysisCommand"
                );
                buttonData.ToolTip = "Analyse l'éclairement des pièces sélectionnées";
                buttonData.LongDescription =
                    "Ouvre une interface pour sélectionner les pièces à analyser " +
                    "et calcule l'éclairement selon les normes EN 12464-1.";

                // Icônes générées via SkyLightTheme (partagées avec LoadingWindow)
                buttonData.LargeImage = RevitLightingPlugin.UI.SkyLightTheme.CreateSkyLightIcon(64);
                buttonData.Image      = RevitLightingPlugin.UI.SkyLightTheme.CreateSkyLightIcon(16);

                panel.AddItem(buttonData);
                Logger.Info("Application", "Bouton 'Analyse Éclairement' ajouté");

                ApplyPanelTheme(tabName);

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
            Logger.Separator("APPLICATION SHUTDOWN");
            Logger.Info("Application", "Arrêt du plugin RevitLightingPlugin");
            Logger.EnterMethod("Application", "OnShutdown");
            try
            {
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

        // ─────────────────────────────────────────────────────────────────────
        //  Fond barre titre panneau ribbon
        // ─────────────────────────────────────────────────────────────────────

        private static void ApplyPanelTheme(string tabName)
        {
            try
            {
                var titleBrush = new SolidColorBrush(Color.FromRgb(35, 60, 92));
                titleBrush.Freeze();

                var adwRibbon = Autodesk.Windows.ComponentManager.Ribbon;
                foreach (Autodesk.Windows.RibbonTab tab in adwRibbon.Tabs)
                {
                    if (!string.Equals(tab.Id,    tabName, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(tab.Title, tabName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (Autodesk.Windows.RibbonPanel adwPanel in tab.Panels)
                    {
                        // Barre titre : défaut Revit (pas de couleur personnalisée)
                        // → évite le rectangle flottant vide
                    }

                    Logger.Info("Application", "Thème barre titre appliqué");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Application", $"Thème panneau non appliqué : {ex.Message}");
            }
        }
    }
}
