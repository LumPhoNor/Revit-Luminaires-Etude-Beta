using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RevitLightingPlugin.UI
{
    /// <summary>
    /// Fenêtre de chargement SkyLight — même thème bleu acier + or ambre que les autres fenêtres.
    /// Thread STA séparé pour ne pas bloquer Revit.
    /// </summary>
    public class LoadingWindow : Window
    {
        private TextBlock _statusText;

        private const string LogoPath =
            @"C:\Users\User\Documents\Projets Plugin\Logo\Logo symbole V3 sans fond .jpg";

        private const double HoloW = 340;
        private const double HoloH = 320;

        public LoadingWindow()
        {
            Width  = 400;
            Height = 420;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            BuildUI();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Interface
        // ─────────────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            // Shell principal — même border arrondie + halo doré que les autres fenêtres
            var mainBorder = new Border
            {
                Margin       = new Thickness(15),
                CornerRadius = new CornerRadius(12),
                Background   = BuildSteelGradient(),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    Color       = Color.FromRgb(190, 140, 10),   // halo doré (logo)
                    BlurRadius  = 30,
                    ShadowDepth = 0,
                    Opacity     = 0.65
                }
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainBorder.Child = root;

            // ── Zone hologramme ──────────────────────────────────────────────
            var holoArea = new Grid
            {
                Width        = HoloW,
                Height       = HoloH,
                ClipToBounds = true,
                Margin       = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            holoArea.Children.Add(BuildGridOverlay());   // grille or subtile
            holoArea.Children.Add(BuildScanLine());      // ligne scan ambre
            holoArea.Children.Add(BuildCenterGlow());    // lueur centrale dorée
            holoArea.Children.Add(BuildLogoImage());     // logo
            holoArea.Children.Add(BuildCorners());       // coins HUD or

            Grid.SetRow(holoArea, 0);
            root.Children.Add(holoArea);

            // ── Barre de statut ──────────────────────────────────────────────
            var statusBar = new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromArgb(100, 255, 185, 30)),  // ligne or
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding         = new Thickness(15, 10, 15, 14)
            };
            _statusText = new TextBlock
            {
                Text                = "Initialisation...",
                FontSize            = 12,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = new SolidColorBrush(SkyLightTheme.TextCyan),  // or ambre
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment       = TextAlignment.Center
            };
            statusBar.Child = _statusText;
            Grid.SetRow(statusBar, 1);
            root.Children.Add(statusBar);

            Content = mainBorder;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Fond et effets
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Gradient bleu acier — même palette que SkyLightTheme.BuildNavyGradient().</summary>
        private static LinearGradientBrush BuildSteelGradient()
        {
            var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.4, 1) };
            b.GradientStops.Add(new GradientStop(SkyLightTheme.NavyLight, 0.0));
            b.GradientStops.Add(new GradientStop(SkyLightTheme.NavyMid,   0.5));
            b.GradientStops.Add(new GradientStop(SkyLightTheme.NavyDark,  1.0));
            return b;
        }

        /// <summary>Grille orthogonale or subtile (pas 30 px).</summary>
        private static Canvas BuildGridOverlay()
        {
            var c   = new Canvas { IsHitTestVisible = false };
            var pen = new SolidColorBrush(Color.FromArgb(18, 255, 185, 30));  // or très discret

            for (double x = 0; x <= 400; x += 30)
                c.Children.Add(new Line { X1=x, Y1=0, X2=x, Y2=400, Stroke=pen, StrokeThickness=0.5 });
            for (double y = 0; y <= 400; y += 30)
                c.Children.Add(new Line { X1=0, Y1=y, X2=400, Y2=y, Stroke=pen, StrokeThickness=0.5 });

            return c;
        }

        /// <summary>Ligne de scan ambrée qui balaie de haut en bas (2,8 s).</summary>
        private static Canvas BuildScanLine()
        {
            var c = new Canvas { IsHitTestVisible=false, ClipToBounds=true };

            var glow = new Rectangle
            {
                Width  = 400,
                Height = 16,
                Fill   = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb( 0, 255, 185, 30), 0.0),
                        new GradientStop(Color.FromArgb(45, 255, 185, 30), 0.5),
                        new GradientStop(Color.FromArgb( 0, 255, 185, 30), 1.0)
                    },
                    new Point(0, 0), new Point(0, 1))
            };
            var core = new Rectangle
            {
                Width  = 400,
                Height = 1.5,
                Fill   = new SolidColorBrush(Color.FromArgb(90, 255, 200, 50))
            };

            Canvas.SetLeft(glow, 0); Canvas.SetTop(glow, 0);
            Canvas.SetLeft(core, 0); Canvas.SetTop(core, 7);

            var tt = new TranslateTransform(0, -16);
            glow.RenderTransform = tt;
            core.RenderTransform = tt;
            c.Children.Add(glow);
            c.Children.Add(core);

            tt.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-16, 420, new Duration(TimeSpan.FromMilliseconds(2800)))
                { RepeatBehavior = RepeatBehavior.Forever });

            return c;
        }

        /// <summary>Lueur radiale dorée pulsante centrée.</summary>
        private static Ellipse BuildCenterGlow()
        {
            var e = new Ellipse
            {
                Width   = 260,
                Height  = 200,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                IsHitTestVisible    = false,
                Fill = new RadialGradientBrush(
                    Color.FromArgb(40, 255, 185, 30),    // or ambre au centre
                    Color.FromArgb( 0, 180, 120,  0))    // transparent en périphérie
            };
            e.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0.3, 1.0, new Duration(TimeSpan.FromMilliseconds(1800)))
                {
                    AutoReverse    = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                });
            return e;
        }

        /// <summary>Logo centré — symbole SkyLight généré programmatiquement.</summary>
        private static Image BuildLogoImage()
        {
            var img = new Image
            {
                Source              = SkyLightTheme.CreateSkyLightIcon(200),
                Stretch             = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(20)
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            return img;
        }

        /// <summary>Coins HUD or calés sur HoloW × HoloH.</summary>
        private static Canvas BuildCorners()
        {
            var c = new Canvas { Width = HoloW, Height = HoloH, IsHitTestVisible = false };
            var b = new SolidColorBrush(Color.FromArgb(220, 255, 185, 30));  // or ambre
            const double t = 1.5, len = 22, m = 8;
            double w = HoloW, h = HoloH;

            void L(double x1, double y1, double x2, double y2) =>
                c.Children.Add(new Line { X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=b, StrokeThickness=t });

            L(m,   m,     m+len, m    ); L(m,   m,    m,   m+len  );
            L(w-m, m,     w-m-len, m  ); L(w-m, m,    w-m, m+len  );
            L(m,   h-m,   m+len, h-m  ); L(m,   h-m,  m,   h-m-len);
            L(w-m, h-m,   w-m-len,h-m ); L(w-m, h-m,  w-m, h-m-len);

            return c;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Suppression du fond damier du logo
        // ─────────────────────────────────────────────────────────────────────

        private static BitmapSource LoadLogoTransparent(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var src = new BitmapImage();
                src.BeginInit();
                src.UriSource   = new Uri(path);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                src.Freeze();

                var conv   = new FormatConvertedBitmap(src, PixelFormats.Bgra32, null, 0);
                int w      = conv.PixelWidth;
                int h      = conv.PixelHeight;
                int stride = w * 4;
                byte[] px  = new byte[h * stride];
                conv.CopyPixels(px, stride, 0);

                for (int i = 0; i < px.Length; i += 4)
                {
                    int bl  = px[i];
                    int g   = px[i + 1];
                    int r   = px[i + 2];
                    int max = r > g ? (r > bl ? r : bl) : (g > bl ? g : bl);
                    int min = r < g ? (r < bl ? r : bl) : (g < bl ? g : bl);

                    // Damier : faible saturation, valeur moyenne (50–150) → transparent
                    if ((max - min) < 25 && min > 50 && max < 150)
                    { px[i + 3] = 0; continue; }

                    // Boost jaune/orange (rayons lumineux) → rendre plus vifs
                    if (r > 150 && bl < 120 && (r - bl) > 70)
                    {
                        px[i + 2] = (byte)Math.Min(255, (int)(r * 1.30));
                        px[i + 1] = (byte)Math.Min(255, (int)(g * 1.15));
                    }
                }

                var result = BitmapSource.Create(w, h, 96, 96,
                    PixelFormats.Bgra32, null, px, stride);
                result.Freeze();
                return result;
            }
            catch { return null; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  API publique
        // ─────────────────────────────────────────────────────────────────────

        public void SetStatus(string status)
        {
            if (Dispatcher.CheckAccess())
                _statusText.Text = status;
            else
                Dispatcher.Invoke(() => _statusText.Text = status);
        }

        public void CloseWindow()
        {
            Dispatcher.Invoke(() =>
            {
                Close();
                System.Windows.Threading.Dispatcher.CurrentDispatcher
                    .BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Background);
            });
        }

        // ─── Factory ─────────────────────────────────────────────────────────

        private static LoadingWindow _instance;
        private static Thread        _uiThread;

        public static LoadingWindow ShowLoading()
        {
            var ready = new ManualResetEventSlim(false);
            _uiThread = new Thread(() =>
            {
                _instance = new LoadingWindow();
                _instance.Show();
                ready.Set();
                System.Windows.Threading.Dispatcher.Run();
            });
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = true;
            _uiThread.Start();
            ready.Wait(3000);
            return _instance;
        }

        public static void CloseInstance()
        {
            try { _instance?.CloseWindow(); } catch { }
            _instance = null;
        }
    }
}
