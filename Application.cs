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
        private static PushButton _calculButton;

        /// <summary>
        /// Met à jour l'icône du bouton Calcul : éclair vide (gris) si le paramétrage
        /// n'est pas fait, ou rempli en bleu une fois qu'il l'est. Appelé par
        /// ParametresCommand une fois la configuration enregistrée.
        /// </summary>
        public static void SetCalculReady(bool ready)
        {
            if (_calculButton == null) return;
            _calculButton.LargeImage = CreateCalcIconLarge(ready);
            _calculButton.Image      = CreateCalcIcon(ready);
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Revit charge le plugin hors du mecanisme deps.json standard de .NET :
        /// la resolution native par defaut ne trouve pas SQLite.Interop.dll dans
        /// runtimes\{rid}\native\ a cote du plugin (fonctionnait tel quel en
        /// .NET Framework/R24). On resout donc le chemin nous-memes.
        /// </summary>
        private static void RegisterSQLiteNativeResolver()
        {
            var sqliteAssembly = typeof(System.Data.SQLite.SQLiteConnection).Assembly;
            System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(sqliteAssembly, (name, assembly, searchPath) =>
            {
                if (!string.Equals(name, "SQLite.Interop.dll", StringComparison.OrdinalIgnoreCase))
                    return IntPtr.Zero;

                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string rid = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                string nativePath = Path.Combine(pluginDir, "runtimes", rid, "native", "SQLite.Interop.dll");

                if (File.Exists(nativePath))
                    return System.Runtime.InteropServices.NativeLibrary.Load(nativePath);

                Logger.Warning("Application", $"SQLite.Interop.dll introuvable à '{nativePath}'");
                return IntPtr.Zero;
            });
        }
#endif

        public Result OnStartup(UIControlledApplication application)
        {
            Logger.Initialize();
            Logger.Separator("APPLICATION STARTUP");
            Logger.Info("Application", "Démarrage du plugin Skylightning");
            Logger.EnterMethod("Application", "OnStartup");

#if NET8_0_OR_GREATER
            RegisterSQLiteNativeResolver();
#endif

            try
            {
                try   { application.CreateRibbonTab(TabName); Logger.Info("Application", $"Onglet '{TabName}' créé"); }
                catch { Logger.Warning("Application", $"Onglet '{TabName}' existe déjà"); }

                RibbonPanel panel = application.CreateRibbonPanel(TabName, "Skylightning");
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // ── Bouton PARAMÈTRES (roue dentée bleue) ─────────────────────
                var parametresData = new PushButtonData(
                    "SkylightningParametres",
                    "Paramètres",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.ParametresCommand")
                {
                    ToolTip    = "Configure les pièces, l'analyse et les vues",
                    LargeImage = CreateGearIconLarge(),
                    Image      = CreateGearIcon()
                };

                // ── Bouton CALCUL (éclair — vide tant que le paramétrage n'est
                //    pas fait, se remplit en bleu une fois configuré) ─────────
                var calculData = new PushButtonData(
                    "SkylightningCalcul",
                    "Calcul",
                    assemblyPath,
                    "RevitLightingPlugin.Commands.CalculCommand")
                {
                    ToolTip    = "Lance le calcul d'éclairement",
                    LargeImage = CreateCalcIconLarge(false),
                    Image      = CreateCalcIcon(false)
                };

                // ── Bouton À PROPOS ⓘ (icône réduite) ─────────────────────────
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

                // Layout : [Paramètres] │ [Calcul] │ [À propos]
                panel.AddItem(parametresData);
                panel.AddSeparator();
                _calculButton = panel.AddItem(calculData) as PushButton;
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
                string path = File.Exists(SkylightningTheme.LogoRibbonIconPath)
                    ? SkylightningTheme.LogoRibbonIconPath
                    : SkylightningTheme.LogoV21Path;
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

        // ── Icône Calcul : éclair (16px). Gris/vide tant que le paramétrage
        //    n'est pas fait, bleu rempli une fois configuré. ──────────────────
        private static BitmapSource CreateCalcIcon(bool ready) => BuildCalcIcon(16, ready);

        // ── Icône Calcul grand (32px) ─────────────────────────────────────────
        private static BitmapSource CreateCalcIconLarge(bool ready) => BuildCalcIcon(32, ready);

        private static BitmapSource BuildCalcIcon(int size, bool ready)
        {
            double s = size;
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
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

                if (ready)
                {
                    var blue = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8
                    dc.DrawGeometry(blue, null, geom);
                }
                else
                {
                    var gray = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // gris neutre
                    var outlinePen = new System.Windows.Media.Pen(gray, Math.Max(1.0, s * 0.06));
                    dc.DrawGeometry(null, outlinePen, geom);
                }
            }
            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        // ── Icône Paramètres : roue dentée bleue ──────────────────────────────
        private static BitmapSource CreateGearIcon()      => BuildGearIcon(16);
        private static BitmapSource CreateGearIconLarge() => BuildGearIcon(32);

        private static BitmapSource BuildGearIcon(int size)
        {
            double s  = size;
            double cx = s / 2.0, cy = s / 2.0;
            double rBody  = s * 0.30; // rayon du corps circulaire (base des dents)
            double rOuter = s * 0.42; // rayon jusqu'au sommet des dents
            double rInner = s * 0.15; // rayon du trou central
            double toothW = s * 0.14;
            double toothH = rOuter - rBody;
            const int teethCount = 8;

            var blue = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8

            var body = new System.Windows.Media.EllipseGeometry(new System.Windows.Point(cx, cy), rBody, rBody);
            var teethGroup = new System.Windows.Media.GeometryGroup { FillRule = System.Windows.Media.FillRule.Nonzero };

            for (int i = 0; i < teethCount; i++)
            {
                double angle = i * (360.0 / teethCount);
                var rect = new System.Windows.Media.RectangleGeometry(
                    new System.Windows.Rect(-toothW / 2, -(rBody + toothH), toothW, toothH + 2));

                var transform = new System.Windows.Media.TransformGroup();
                transform.Children.Add(new System.Windows.Media.RotateTransform(angle));
                transform.Children.Add(new System.Windows.Media.TranslateTransform(cx, cy));
                rect.Transform = transform;

                teethGroup.Children.Add(rect);
            }

            var gearOutline = new System.Windows.Media.CombinedGeometry(
                System.Windows.Media.GeometryCombineMode.Union, body, teethGroup);
            var hole = new System.Windows.Media.EllipseGeometry(new System.Windows.Point(cx, cy), rInner, rInner);
            var finalGear = new System.Windows.Media.CombinedGeometry(
                System.Windows.Media.GeometryCombineMode.Exclude, gearOutline, hole);

            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawGeometry(blue, null, finalGear);
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
                    new System.Windows.Point(cx, cx), s * 0.36, s * 0.36); // réduit (avant : 0.45)
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
                    tf, s * 0.48, white); // réduit proportionnellement (avant : 0.58)
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
