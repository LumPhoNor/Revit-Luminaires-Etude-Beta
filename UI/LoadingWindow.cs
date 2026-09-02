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
using ShapePath = System.Windows.Shapes.Path;

namespace RevitLightingPlugin.UI
{
    /// <summary>
    /// Fenêtre de chargement SkyLight — même thème bleu acier + or ambre que les autres fenêtres.
    /// Thread STA séparé pour ne pas bloquer Revit.
    /// </summary>
    public class LoadingWindow : Window
    {
        private TextBlock _statusText;
        private ShapePath _bulbFillPath;
        private RectangleGeometry _bulbClip;

        // Icône ampoule (Material Design "lightbulb", viewBox 24x24)
        private const string BulbGeometryData =
            "M9,21c0,0.55,0.45,1,1,1h4c0.55,0,1-0.45,1-1v-1H9V21z " +
            "M12,2C8.14,2,5,5.14,5,9c0,2.38,1.19,4.47,3,5.74V17c0,0.55,0.45,1,1,1h6 " +
            "c0.55,0,1-0.45,1-1v-2.26c1.81-1.27,3-3.36,3-5.74C19,5.14,15.86,2,12,2z";

        private const double BulbSize = 20;

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
            var mainBorder = new Border
            {
                Margin          = new Thickness(10),
                CornerRadius    = new CornerRadius(8),
                Background      = new SolidColorBrush(Color.FromRgb(245, 246, 248)),
                ClipToBounds    = true,
                BorderBrush     = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color       = Color.FromRgb(150, 150, 155),
                    BlurRadius  = 16,
                    ShadowDepth = 2,
                    Opacity     = 0.20
                }
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainBorder.Child = root;

            // ── Logo occupant toute la zone disponible de la fenêtre ────────────
            var logoArea = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch
            };
            logoArea.Children.Add(BuildLogoImage());

            Grid.SetRow(logoArea, 0);
            root.Children.Add(logoArea);

            // ── Barre de statut ──────────────────────────────────────────────
            var statusBar = new Border
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Background      = new SolidColorBrush(Color.FromRgb(235, 237, 241)),
                Padding         = new Thickness(15, 10, 15, 14)
            };
            var statusContent = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            statusContent.Children.Add(BuildProgressBulb());

            _statusText = new TextBlock
            {
                Text                = "Initialisation...",
                FontSize            = 12,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                VerticalAlignment   = VerticalAlignment.Center,
                TextAlignment       = TextAlignment.Center
            };
            statusContent.Children.Add(_statusText);

            statusBar.Child = statusContent;
            Grid.SetRow(statusBar, 1);
            root.Children.Add(statusBar);

            Content = mainBorder;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Ampoule de progression (devant le titre de la pièce)
        // ─────────────────────────────────────────────────────────────────────

        private UIElement BuildProgressBulb()
        {
            Geometry geometry = Geometry.Parse(BulbGeometryData);
            geometry.Freeze();

            var outline = new ShapePath
            {
                Data    = geometry,
                Fill    = new SolidColorBrush(Color.FromRgb(209, 213, 219)), // gris clair : ampoule vide
                Stretch = Stretch.Uniform,
                Width   = BulbSize,
                Height  = BulbSize
            };

            _bulbClip = new RectangleGeometry(RectForFraction(0)); // rien de rempli au départ

            _bulbFillPath = new ShapePath
            {
                Data    = geometry,
                Fill    = new SolidColorBrush(Color.FromRgb(29, 78, 216)), // bleu Skylightning (#1D4ED8)
                Stretch = Stretch.Uniform,
                Width   = BulbSize,
                Height  = BulbSize,
                Clip    = _bulbClip
            };

            var container = new Grid
            {
                Width               = BulbSize,
                Height              = BulbSize,
                Margin              = new Thickness(0, 0, 8, 0),
                VerticalAlignment   = VerticalAlignment.Center
            };
            container.Children.Add(outline);
            container.Children.Add(_bulbFillPath);
            return container;
        }

        private static Rect RectForFraction(double fraction)
        {
            fraction = Math.Max(0, Math.Min(1, fraction));
            double filledHeight = BulbSize * fraction;
            return new Rect(0, BulbSize - filledHeight, BulbSize, filledHeight);
        }

        /// <summary>
        /// Cale immédiatement le remplissage de l'ampoule (0 = vide, 1 = plein), en arrêtant
        /// toute animation en cours. Utiliser pour les valeurs connues avec certitude
        /// (début de pièce = vide, fin de pièce/hauteur = valeur exacte).
        /// </summary>
        public void SetProgress(double fraction)
        {
            Rect target = RectForFraction(fraction);

            void Update()
            {
                if (_bulbClip == null) return;
                _bulbClip.BeginAnimation(RectangleGeometry.RectProperty, null); // stoppe l'animation en cours
                _bulbClip.Rect = target;
            }

            if (Dispatcher.CheckAccess())
                Update();
            else
                Dispatcher.Invoke(Update);
        }

        /// <summary>
        /// Anime le remplissage de l'ampoule vers <paramref name="targetFraction"/> sur une durée
        /// estimée. L'animation tourne sur le thread propre de la fenêtre : elle continue de
        /// progresser pendant que le calcul (bloquant) s'exécute sur le thread Revit. Si le calcul
        /// se termine avant la fin de l'animation, appeler <see cref="SetProgress"/> pour caler la
        /// valeur exacte ; si le calcul dure plus longtemps que prévu, le remplissage s'arrête
        /// simplement à <paramref name="targetFraction"/> en attendant.
        /// </summary>
        public void AnimateProgressTo(double targetFraction, TimeSpan estimatedDuration)
        {
            Rect target = RectForFraction(targetFraction);

            void Update()
            {
                if (_bulbClip == null) return;
                var animation = new RectAnimation(_bulbClip.Rect, target, new Duration(estimatedDuration))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                _bulbClip.BeginAnimation(RectangleGeometry.RectProperty, animation);
            }

            if (Dispatcher.CheckAccess())
                Update();
            else
                Dispatcher.Invoke(Update);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Fond et effets
        // ─────────────────────────────────────────────────────────────────────


        private static UIElement BuildLogoImage()
        {
            if (File.Exists(SkylightningTheme.LogoV21Path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = new Uri(SkylightningTheme.LogoV21Path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                var img = new Image
                {
                    Source              = bmp,
                    Stretch             = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment   = VerticalAlignment.Stretch
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                return img;
            }

            var fallback = new Image
            {
                Source              = SkylightningTheme.CreateSkylightningIcon(260),
                Stretch             = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch
            };
            RenderOptions.SetBitmapScalingMode(fallback, BitmapScalingMode.HighQuality);
            return fallback;
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
