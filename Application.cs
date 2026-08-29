using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;
using RevitLightingPlugin.Core;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin
{
    public class Application : IExternalApplication
    {
        private const string TabName = "Skylightning";

        public Result OnStartup(UIControlledApplication application)
        {
            Logger.Initialize();
            Logger.Separator("APPLICATION STARTUP");
            Logger.Info("Application", "Démarrage du plugin Skylightning");
            Logger.EnterMethod("Application", "OnStartup");

            try
            {
                try   { application.CreateRibbonTab(TabName); Logger.Info("Application", $"Onglet '{TabName}' créé"); }
                catch { Logger.Warning("Application", $"Onglet '{TabName}' existe déjà"); }

                RibbonPanel panel = application.CreateRibbonPanel(TabName, "Skylightning");
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // PNG logo (chargé via MemoryStream — centré dans 32×32)
                var pngLarge = LoadButtonIcon(32);
                var pngSmall = LoadButtonIcon(16);
                Logger.Info("Application", $"PNG logo : {pngLarge?.PixelWidth}×{pngLarge?.PixelHeight} px");

                // ── Bouton PARAMÈTRES (grand — logo PNG centré) ───────────────
                var parametresData = new PushButtonData(
                    "SkylightningParametres",
                    "Paramètres",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.ParametresCommand")
                {
                    ToolTip    = "Configure les pièces, l'analyse et les vues",
                    LargeImage = pngLarge,
                    Image      = pngSmall
                };

                // ── Bouton CALCUL (grand) ─────────────────────────────────────
                var calculData = new PushButtonData(
                    "SkylightningCalcul",
                    "Calcul",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.CalculCommand")
                {
                    ToolTip    = "Lance le calcul d'éclairement",
                    LargeImage = CreateCalcIconLarge(),
                    Image      = CreateCalcIcon()
                };

                // ── Bouton À PROPOS ⓘ (grand) ─────────────────────────────────
                var aboutData = new PushButtonData(
                    "SkylightningAbout",
                    "À propos",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.AboutCommand")
                {
                    ToolTip    = "Version et informations du plugin",
                    LargeImage = CreateInfoIconLarge(),
                    Image      = CreateInfoIcon()
                };

                // Layout : [Paramètres (grand)] │ [Calcul (grand)] │ [À propos (grand)]
                panel.AddItem(parametresData);
                panel.AddSeparator();
                panel.AddItem(calculData);
                panel.AddSeparator();
                panel.AddItem(aboutData);

                Logger.Info("Application", "Ruban Skylightning configuré");
                ApplyPanelTheme();

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

        // ── Charge le PNG via MemoryStream (évite tout pb de chemin/URI/DPI) ──────
        private static BitmapSource LoadButtonIcon(int size)
        {
            try
            {
                string path = SkylightningTheme.LogoV21Path;
                if (!File.Exists(path))
                {
                    Logger.Warning("Application", $"PNG introuvable : {path}");
                    return SkylightningTheme.CreateSkylightningIcon(size);
                }

                byte[] bytes = File.ReadAllBytes(path);
                using (var ms = new System.IO.MemoryStream(bytes))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource     = ms;
                    bmp.DecodePixelWidth = size;
                    bmp.CacheOption      = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    Logger.Info("Application", $"PNG chargé : {bmp.PixelWidth}×{bmp.PixelHeight} px");
                    return bmp;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Application", $"Erreur chargement PNG logo : {ex.Message}", ex);
                return SkylightningTheme.CreateSkylightningIcon(size);
            }
        }

        // ── Icône Paramètres : 3 barres réglage bleues (16px) ────────────────
        private static BitmapSource CreateSettingsIcon()
        {
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var blue = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8
                var pen  = new System.Windows.Media.Pen(blue, 1.3);
                double[] ys = { 4, 8, 12 };
                double[] xs = { 5, 10, 7 };
                for (int i = 0; i < 3; i++)
                {
                    dc.DrawLine(pen,
                        new System.Windows.Point(2,  ys[i]),
                        new System.Windows.Point(14, ys[i]));
                    dc.DrawEllipse(blue, null,
                        new System.Windows.Point(xs[i], ys[i]), 2.0, 2.0);
                }
            }
            var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        // ── Icône Calcul : éclair bleu (16px) ────────────────────────────────
        private static BitmapSource CreateCalcIcon()
        {
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var blue = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8
                var geom = new System.Windows.Media.StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new System.Windows.Point(10, 1), true, true);
                    ctx.LineTo(new System.Windows.Point(5,  8), true, false);
                    ctx.LineTo(new System.Windows.Point(9,  8), true, false);
                    ctx.LineTo(new System.Windows.Point(4, 15), true, false);
                    ctx.LineTo(new System.Windows.Point(13, 7), true, false);
                    ctx.LineTo(new System.Windows.Point(9,  7), true, false);
                }
                geom.Freeze();
                dc.DrawGeometry(blue, null, geom);
            }
            var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        // ── Icône Calcul grand (32px) ─────────────────────────────────────────
        private static BitmapSource CreateCalcIconLarge() => BuildCalcIcon(32);

        private static BitmapSource BuildCalcIcon(int size)
        {
            double s = size;
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var blue = new SolidColorBrush(Color.FromRgb(29, 78, 216));
                var geom = new System.Windows.Media.StreamGeometry();
                using (var ctx = geom.Open())
                {
                    // Éclair centré proportionnel à size
                    ctx.BeginFigure(new System.Windows.Point(s*0.62, s*0.06), true, true);
                    ctx.LineTo(new System.Windows.Point(s*0.31, s*0.50), true, false);
                    ctx.LineTo(new System.Windows.Point(s*0.56, s*0.50), true, false);
                    ctx.LineTo(new System.Windows.Point(s*0.25, s*0.94), true, false);
                    ctx.LineTo(new System.Windows.Point(s*0.81, s*0.44), true, false);
                    ctx.LineTo(new System.Windows.Point(s*0.56, s*0.44), true, false);
                }
                geom.Freeze();
                dc.DrawGeometry(blue, null, geom);
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        // ── Icône À propos : cercle bleu + "i" blanc (16px / 32px) ───────────
        private static BitmapSource CreateInfoIcon()      => BuildInfoIcon(16);
        private static BitmapSource CreateInfoIconLarge() => BuildInfoIcon(32);

        private static BitmapSource BuildInfoIcon(int size)
        {
            double s  = size;
            double cx = s / 2.0;
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var blue  = new SolidColorBrush(Color.FromRgb( 29,  78, 216)); // #1D4ED8
                var white = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                dc.DrawEllipse(blue, null,
                    new System.Windows.Point(cx, cx), s * 0.45, s * 0.45);
                var tf = new System.Windows.Media.Typeface(
                    new System.Windows.Media.FontFamily("Arial"),
                    System.Windows.FontStyles.Normal,
                    System.Windows.FontWeights.Bold,
                    System.Windows.FontStretches.Normal);
#pragma warning disable CS0618
                var ft = new System.Windows.Media.FormattedText(
                    "i",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    tf, s * 0.58, white);
#pragma warning restore CS0618
                dc.DrawText(ft,
                    new System.Windows.Point(cx - ft.Width / 2, cx - ft.Height / 2 - s * 0.02));
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
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

        private static void ApplyPanelTheme() { }

        // Commande vide : le clic sur le logo ne fait rien
        private class NoOpCommand : System.Windows.Input.ICommand
        {
#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) { }
        }
    }
}
