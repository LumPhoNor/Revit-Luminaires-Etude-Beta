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

            // ── Logo centré ──────────────────────────────────────────────────
            var logoArea = new Grid
            {
                Width               = HoloW,
                Height              = HoloH,
                HorizontalAlignment = HorizontalAlignment.Center
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
            _statusText = new TextBlock
            {
                Text                = "Initialisation...",
                FontSize            = 12,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
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
                    Stretch             = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Width               = 260,
                    Height              = 260,
                    Margin              = new Thickness(20)
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                return img;
            }

            var fallback = new Image
            {
                Source              = SkylightningTheme.CreateSkylightningIcon(260),
                Stretch             = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Margin              = new Thickness(20)
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
