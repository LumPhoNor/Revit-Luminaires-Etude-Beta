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
    /// Fenêtre de chargement animée SkyLight Initium.
    /// Tourne sur un thread STA séparé pour ne pas bloquer le thread principal Revit.
    /// </summary>
    public class LoadingWindow : Window
    {
        private TextBlock _statusText;
        private Ellipse _glowOverlay;

        public LoadingWindow()
        {
            Width = 380;
            Height = 500;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;

            BuildUI();
            StartGlowAnimation();
        }

        private void BuildUI()
        {
            // Conteneur principal avec ombre portée
            Border mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 232, 235)),
                CornerRadius = new CornerRadius(14),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 30,
                    ShadowDepth = 10,
                    Opacity = 0.45
                }
            };

            StackPanel stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(24, 20, 24, 24)
            };

            // Conteneur du logo avec overlay de lueur
            Grid logoGrid = new Grid
            {
                Width = 300,
                Height = 370
            };

            // Image du logo SkyLight
            Image logoImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            string logoPath = @"C:\Users\JEDI-Lee\Documents\Projets Plugin\Logo\Logo SkyLight.jpg";
            if (File.Exists(logoPath))
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(logoPath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                logoImage.Source = bmp;
            }
            logoGrid.Children.Add(logoImage);

            // Overlay lumineux sur l'ampoule (zone supérieure centrée)
            _glowOverlay = new Ellipse
            {
                Width = 140,
                Height = 140,
                Fill = new RadialGradientBrush(
                    Color.FromArgb(170, 255, 225, 60),   // Centre : jaune vif
                    Color.FromArgb(0, 255, 200, 30)       // Bords : transparent
                ),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 45, 0, 0),
                Opacity = 0.0,
                IsHitTestVisible = false
            };
            logoGrid.Children.Add(_glowOverlay);

            stack.Children.Add(logoGrid);

            // Séparateur
            Border separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(200, 200, 205)),
                Margin = new Thickness(0, 4, 0, 10)
            };
            stack.Children.Add(separator);

            // Texte de statut
            _statusText = new TextBlock
            {
                Text = "Initialisation...",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(35, 75, 125)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            stack.Children.Add(_statusText);

            mainBorder.Child = stack;
            Content = mainBorder;
        }

        private void StartGlowAnimation()
        {
            DoubleAnimation glowAnim = new DoubleAnimation
            {
                From = 0.05,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(650)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            _glowOverlay.BeginAnimation(UIElement.OpacityProperty, glowAnim);
        }

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

        // ─── Factory : création sur thread STA séparé ───────────────────────

        private static LoadingWindow _instance;
        private static Thread _uiThread;

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
