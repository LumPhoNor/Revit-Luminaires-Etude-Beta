using System;
using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
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
                // Créer onglet personnalisé "SkyLight"
                string tabName = "SkyLight";
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

                // Créer panneau "initium"
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "initium");
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

                // Icône du bouton
                string logoPath = @"C:\Users\JEDI-Lee\Documents\Projets Plugin\Logo\Logo Bouton Analyse V5b.png";
                if (File.Exists(logoPath))
                {
                    try
                    {
                        // LargeImage : 32x32 standard Revit
                        BitmapImage bmpLarge = new BitmapImage();
                        bmpLarge.BeginInit();
                        bmpLarge.UriSource = new Uri(logoPath);
                        bmpLarge.CacheOption = BitmapCacheOption.OnLoad;
                        bmpLarge.EndInit();
                        bmpLarge.Freeze();

                        // Image : 16x16 (petit bouton Revit)
                        BitmapImage bmpSmall = new BitmapImage();
                        bmpSmall.BeginInit();
                        bmpSmall.UriSource = new Uri(logoPath);
                        bmpSmall.DecodePixelWidth = 16;
                        bmpSmall.DecodePixelHeight = 16;
                        bmpSmall.CacheOption = BitmapCacheOption.OnLoad;
                        bmpSmall.EndInit();
                        bmpSmall.Freeze();

                        buttonAnalyzeData.LargeImage = bmpLarge;
                        buttonAnalyzeData.Image = bmpSmall;
                        Logger.Debug("Application", "Icône bouton chargée");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning("Application", $"Icône non chargée : {ex.Message}");
                    }
                }

                PushButton buttonAnalyze = panel.AddItem(buttonAnalyzeData) as PushButton;
                Logger.Info("Application", "Bouton 'Analyse Éclairement' ajouté");


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
