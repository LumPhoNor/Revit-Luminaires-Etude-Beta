using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RevitLightingPlugin.UI
{
    /// <summary>
    /// Fournit le thème SkyLight (bleu acier + or ambre) pour toutes les fenêtres du plugin.
    /// Palette inspirée du logo : fond bleu-acier, accents dorés (rayons lumineux), blanc.
    /// </summary>
    internal static class SkyLightTheme
    {
        // ── Chemin du logo ────────────────────────────────────────────────────
        public const string LogoV21Path =
            @"C:\Users\User\Documents\Projets Plugin\Logo\Logo SkyLight V2.1.jpg";

        // ── Palette — bleu acier + or ambre (logo SkyLight) ──────────────────
        public static readonly Color NavyDark  = Color.FromRgb( 42,  72, 108);  // #2A4870 fond foncé
        public static readonly Color NavyMid   = Color.FromRgb( 35,  60,  92);  // #233C5C fond milieu
        public static readonly Color NavyLight = Color.FromRgb( 58,  92, 135);  // #3A5C87 fond clair
        public static readonly Color InputBg   = Color.FromRgb( 25,  46,  74);  // #192E4A inputs
        public static readonly Color AccentBlue= Color.FromRgb(  0, 185, 255);  // #00B9FF cyan bordures
        public static readonly Color AccentGold= Color.FromRgb(255, 185,  30);  // #FFB91E or logo
        public static readonly Color TextCyan  = Color.FromRgb(255, 200,  70);  // #FFC846 or ambre labels
        public static readonly Color TextWhite = Color.FromRgb(230, 242, 255);  // blanc doux
        public static readonly Color TextGray  = Color.FromRgb(170, 200, 225);  // gris clair lisible
        public static readonly Color GreenOk   = Color.FromRgb( 80, 220, 120);
        public static readonly Color RedWarn   = Color.FromRgb(255,  95,  80);

        // ─────────────────────────────────────────────────────────────────────
        //  API publique
        // ─────────────────────────────────────────────────────────────────────

        public static void ApplyDarkWindow(Window w, double width, double height)
        {
            w.Width  = width;
            w.Height = height;
            w.WindowStyle = WindowStyle.None;
            w.AllowsTransparency = true;
            w.Background = Brushes.Transparent;
            w.ResizeMode = ResizeMode.NoResize;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        public static Border BuildDarkShell(UIElement innerContent, double shellW, double shellH)
        {
            var rootGrid = new Grid();
            rootGrid.Children.Add(BuildGridCanvas(shellW, shellH));
            rootGrid.Children.Add(innerContent);
            rootGrid.Children.Add(BuildCornersCanvas(shellW, shellH));

            return new Border
            {
                Margin       = new Thickness(15),
                CornerRadius = new CornerRadius(12),
                Background   = BuildNavyGradient(),
                ClipToBounds = true,
                Effect = new DropShadowEffect
                {
                    Color       = Color.FromRgb(190, 140,  10),   // halo doré
                    BlurRadius  = 30,
                    ShadowDepth = 0,
                    Opacity     = 0.60
                },
                Child = rootGrid
            };
        }

        public static Border BuildDarkHeader(string title, string subtitle, Window owner)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Logo col 0
            var logo = BuildLogoV21(56);
            Grid.SetColumn(logo, 0);
            g.Children.Add(logo);

            // Titre col 1
            var ts = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 8, 4, 8)
            };
            ts.Children.Add(new TextBlock
            {
                Text       = title,
                FontSize   = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(TextWhite)
            });
            if (!string.IsNullOrEmpty(subtitle))
                ts.Children.Add(new TextBlock
                {
                    Text       = subtitle,
                    FontSize   = 9.5,
                    Foreground = new SolidColorBrush(TextCyan),
                    Opacity    = 0.90
                });
            Grid.SetColumn(ts, 1);
            g.Children.Add(ts);

            // Bouton fermer col 2
            var closeBtn = new Button
            {
                Content         = "✕",
                Width           = 28,
                Height          = 28,
                Margin          = new Thickness(0, 0, 10, 0),
                Background      = Brushes.Transparent,
                BorderBrush     = new SolidColorBrush(Color.FromArgb(100, 255, 185, 30)),
                BorderThickness = new Thickness(1),
                Foreground      = new SolidColorBrush(TextCyan),
                FontSize        = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor          = Cursors.Hand
            };
            closeBtn.Click += (s, e) => { try { owner.DialogResult = false; } catch { } owner.Close(); };
            Grid.SetColumn(closeBtn, 2);
            g.Children.Add(closeBtn);

            var header = new Border
            {
                Background      = new SolidColorBrush(Color.FromArgb(55, 0, 0, 0)),        // voile sombre
                BorderBrush     = new SolidColorBrush(Color.FromArgb(120, 255, 185, 30)),   // ligne or
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child           = g
            };
            header.MouseLeftButtonDown += (s, e) => { try { owner.DragMove(); } catch { } };
            return header;
        }

        public static void SetPanelForeground(FrameworkElement panel)
        {
            TextElement.SetForeground(panel, new SolidColorBrush(TextCyan));
        }

        // ── Styles de contrôles ───────────────────────────────────────────────

        public static void StyleTextBox(TextBox tb)
        {
            tb.Background       = new SolidColorBrush(InputBg);
            tb.Foreground       = new SolidColorBrush(TextWhite);
            tb.BorderBrush      = new SolidColorBrush(Color.FromArgb(130, 255, 185, 30));   // bordure or
            tb.BorderThickness  = new Thickness(1);
            tb.CaretBrush       = new SolidColorBrush(AccentGold);
            tb.Padding          = new Thickness(5, 4, 5, 4);
        }

        /// <summary>
        /// Applique un ControlTemplate personnalisé sur le ComboBox — seule solution
        /// fiable car le template WPF par défaut utilise ses propres brushes internes
        /// qui ignorent cb.Background.
        /// </summary>
        public static void StyleComboBox(ComboBox cb)
        {
            cb.Foreground      = new SolidColorBrush(TextWhite);
            cb.BorderThickness = new Thickness(1);
            cb.Template        = BuildComboBoxTemplate();

            // Style des items dans le dropdown
            var bgBrush  = new SolidColorBrush(InputBg);
            var fgBrush  = new SolidColorBrush(TextWhite);

            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, bgBrush));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, fgBrush));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(6, 3, 6, 3)));

            var hoverTrigger = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(90, 255, 185, 30))));
            itemStyle.Triggers.Add(hoverTrigger);

            var selTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(150, 255, 185, 30))));
            itemStyle.Triggers.Add(selTrigger);

            cb.ItemContainerStyle = itemStyle;
        }

        /// <summary>
        /// Construit un ControlTemplate complet pour ComboBox :
        /// fond bleu acier, texte blanc, flèche dorée, popup sombre.
        /// </summary>
        private static ControlTemplate BuildComboBoxTemplate()
        {
            var bgBrush     = new SolidColorBrush(InputBg);
            var fgBrush     = new SolidColorBrush(TextWhite);
            var borderBrush = new SolidColorBrush(Color.FromArgb(130, 255, 185, 30));

            var template = new ControlTemplate(typeof(ComboBox));

            // ── Conteneur racine (border + popup côte à côte dans un Grid) ────
            var outerGrid = new FrameworkElementFactory(typeof(Grid));

            // ── Border principale (fond sombre visible en état fermé) ──────────
            var mainBorder = new FrameworkElementFactory(typeof(Border));
            mainBorder.SetValue(Border.BackgroundProperty, bgBrush);
            mainBorder.SetValue(Border.BorderBrushProperty, borderBrush);
            mainBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            mainBorder.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            // DockPanel : flèche à droite, texte remplit le reste
            var dock = new FrameworkElementFactory(typeof(DockPanel));
            dock.SetValue(DockPanel.LastChildFillProperty, true);

            // Bouton flèche (ToggleButton transparent)
            var toggle = new FrameworkElementFactory(typeof(ToggleButton));
            toggle.SetValue(DockPanel.DockProperty, Dock.Right);
            toggle.SetValue(FrameworkElement.WidthProperty, 22.0);
            toggle.SetValue(Control.FocusableProperty, false);
            toggle.SetBinding(ToggleButton.IsCheckedProperty,
                new Binding("IsDropDownOpen")
                {
                    RelativeSource = RelativeSource.TemplatedParent,
                    Mode = BindingMode.TwoWay
                });

            // Template du ToggleButton : juste la flèche, fond transparent
            var tgTemplate = new ControlTemplate(typeof(ToggleButton));
            var tgBorder   = new FrameworkElementFactory(typeof(Border));
            tgBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var arrowTb = new FrameworkElementFactory(typeof(TextBlock));
            arrowTb.SetValue(TextBlock.TextProperty, "▾");
            arrowTb.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(AccentGold));
            arrowTb.SetValue(TextBlock.FontSizeProperty, 11.0);
            arrowTb.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            arrowTb.SetValue(FrameworkElement.VerticalAlignmentProperty,   VerticalAlignment.Center);
            tgBorder.AppendChild(arrowTb);
            tgTemplate.VisualTree = tgBorder;
            toggle.SetValue(ToggleButton.TemplateProperty, tgTemplate);

            dock.AppendChild(toggle);

            // ContentPresenter : affiche l'item sélectionné en blanc
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.MarginProperty,           new Thickness(6, 2, 2, 2));
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            cp.SetValue(TextElement.ForegroundProperty,            fgBrush);
            cp.SetValue(UIElement.IsHitTestVisibleProperty,        false);
            cp.SetValue(UIElement.SnapsToDevicePixelsProperty,     true);
            cp.SetBinding(ContentPresenter.ContentProperty,
                new Binding("SelectionBoxItem") { RelativeSource = RelativeSource.TemplatedParent });
            cp.SetBinding(ContentPresenter.ContentTemplateProperty,
                new Binding("SelectionBoxItemTemplate") { RelativeSource = RelativeSource.TemplatedParent });
            dock.AppendChild(cp);

            mainBorder.AppendChild(dock);
            outerGrid.AppendChild(mainBorder);

            // ── Popup dropdown ────────────────────────────────────────────────
            var popup = new FrameworkElementFactory(typeof(Popup));
            popup.SetValue(Popup.PlacementProperty,        PlacementMode.Bottom);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.PopupAnimationProperty,   PopupAnimation.None);
            popup.SetBinding(Popup.IsOpenProperty,
                new Binding("IsDropDownOpen") { RelativeSource = RelativeSource.TemplatedParent });
            popup.SetBinding(Popup.PlacementTargetProperty,
                new Binding() { RelativeSource = RelativeSource.TemplatedParent });
            popup.SetBinding(FrameworkElement.MinWidthProperty,
                new Binding("ActualWidth") { RelativeSource = RelativeSource.TemplatedParent });

            var popupBorder = new FrameworkElementFactory(typeof(Border));
            popupBorder.SetValue(Border.BackgroundProperty,      bgBrush);
            popupBorder.SetValue(Border.BorderBrushProperty,     borderBrush);
            popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popupBorder.SetValue(Border.MaxHeightProperty,       200.0);
            popupBorder.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var sv = new FrameworkElementFactory(typeof(ScrollViewer));
            sv.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            sv.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
            popupBorder.AppendChild(sv);
            popup.AppendChild(popupBorder);
            outerGrid.AppendChild(popup);

            template.VisualTree = outerGrid;
            return template;
        }

        public static void StyleButton(Button btn, bool primary = true)
        {
            btn.Background      = primary
                ? new SolidColorBrush(Color.FromArgb(200, 175, 115,   0))  // bouton or ambre
                : new SolidColorBrush(Color.FromArgb( 60,  30,  60, 110)); // bouton discret
            btn.Foreground      = new SolidColorBrush(TextWhite);
            btn.BorderBrush     = new SolidColorBrush(Color.FromArgb(180, 255, 185, 30));
            btn.BorderThickness = new Thickness(1);
        }

        public static void StyleCheckBox(CheckBox cb)
        {
            cb.Foreground = new SolidColorBrush(TextWhite);
        }

        public static void StyleListView(ListView lv)
        {
            lv.Background  = new SolidColorBrush(Color.FromRgb(25, 48, 76));
            lv.Foreground  = new SolidColorBrush(TextWhite);
            lv.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 185, 30));
        }

        public static void StyleDataGrid(DataGrid dg)
        {
            dg.Background               = new SolidColorBrush(Color.FromRgb(25, 48, 76));
            dg.Foreground               = new SolidColorBrush(TextWhite);
            dg.BorderBrush              = new SolidColorBrush(Color.FromArgb(90, 255, 185, 30));
            dg.RowBackground            = new SolidColorBrush(Color.FromArgb(25, 255, 185, 30)); // ligne or subtil
            dg.AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(40,   0,  40,  90));
            dg.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromArgb(40, 255, 185, 30));
            dg.VerticalGridLinesBrush   = new SolidColorBrush(Color.FromArgb(40, 255, 185, 30));
        }

        // ── Constructeurs de TextBlock ─────────────────────────────────────────

        public static TextBlock MakeLabel(string text, bool bold = false, double size = 11)
            => new TextBlock
            {
                Text       = text,
                FontSize   = size,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush(TextCyan),
                Margin     = new Thickness(0, 8, 0, 4)
            };

        public static TextBlock MakeCaption(string text)
            => new TextBlock
            {
                Text       = text,
                FontSize   = 9,
                Foreground = new SolidColorBrush(TextGray),
                Margin     = new Thickness(0, 2, 0, 4)
            };

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers privés
        // ─────────────────────────────────────────────────────────────────────

        static LinearGradientBrush BuildNavyGradient()
        {
            // Gradient bleu acier — du plus clair (haut-gauche) au plus sombre (bas)
            // Correspond à la teinte de fond du logo SkyLight V2.1
            var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.4, 1) };
            b.GradientStops.Add(new GradientStop(NavyLight, 0.0));
            b.GradientStops.Add(new GradientStop(NavyMid,   0.5));
            b.GradientStops.Add(new GradientStop(NavyDark,  1.0));
            return b;
        }

        static Canvas BuildGridCanvas(double w, double h)
        {
            var c   = new Canvas { IsHitTestVisible = false };
            var pen = new SolidColorBrush(Color.FromArgb(18, 255, 185, 30));  // grille or subtile
            for (double x = 0; x <= w + 30; x += 30)
                c.Children.Add(new Line { X1=x, Y1=0, X2=x, Y2=h+30, Stroke=pen, StrokeThickness=0.5 });
            for (double y = 0; y <= h + 30; y += 30)
                c.Children.Add(new Line { X1=0, Y1=y, X2=w+30, Y2=y, Stroke=pen, StrokeThickness=0.5 });
            return c;
        }

        static Canvas BuildCornersCanvas(double w, double h)
        {
            var c = new Canvas
            {
                Width = w, Height = h,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment   = VerticalAlignment.Top
            };
            var b = new SolidColorBrush(Color.FromArgb(210, 255, 185, 30));  // coins or
            const double t = 1.5, len = 22, m = 6;
            void L(double x1, double y1, double x2, double y2) =>
                c.Children.Add(new Line { X1=x1, Y1=y1, X2=x2, Y2=y2, Stroke=b, StrokeThickness=t });
            L(m,   m,     m+len, m    ); L(m,   m,    m,   m+len  );
            L(w-m, m,     w-m-len, m  ); L(w-m, m,    w-m, m+len  );
            L(m,   h-m,   m+len, h-m  ); L(m,   h-m,  m,   h-m-len);
            L(w-m, h-m,   w-m-len,h-m ); L(w-m, h-m,  w-m, h-m-len);
            return c;
        }

        /// <summary>
        /// Génère l'icône SkyLight complète et professionnelle :
        /// bâtiment isométrique filaire 3D avec cloisons intérieures,
        /// soleil étoilé à 8 branches, rayons ambrés fléchés, lueur convergence au sol.
        /// Pour size >= 200 : texte "SkyLight" bicolore + sous-titre "Initium".
        /// </summary>
        public static BitmapSource CreateSkyLightIcon(int size)
        {
            double s       = (double)size;
            bool withText  = (size >= 200);
            bool withDash  = (size >= 48);
            double sc      = Math.Sqrt(s / 64.0);  // échelle épaisseurs (racine carrée)

            // ── Géométrie bâtiment (fractions × s) ───────────────────────────
            double fy_top  = withText ? 0.268 : 0.245;
            double fy_bot  = withText ? 0.705 : 0.758;
            double fx_left = 0.110, fx_right = 0.612;
            double dep_x   = 0.186, dep_y = -0.142;             // vecteur profondeur iso
            double fx_mid  = fx_left + (fx_right - fx_left) * 0.475;
            double bx_l    = fx_left  + dep_x;
            double bx_r    = fx_right + dep_x;
            double by_t    = fy_top   + dep_y;
            double by_b    = fy_bot   + dep_y;

            // ── Soleil ────────────────────────────────────────────────────────
            double sunX = 0.504, sunY = withText ? 0.158 : 0.138;
            double sunR = Math.Max(3.0, 0.052 * s);

            // ── Helper point ──────────────────────────────────────────────────
            Point P(double fx, double fy) => new Point(fx * s, fy * s);

            // ── Épaisseurs ────────────────────────────────────────────────────
            double wMain = Math.Max(1.0, 2.0 * sc);
            double wPart = Math.Max(0.7, 1.4 * sc);
            double wHide = Math.Max(0.5, 1.0 * sc);
            double wRay  = Math.Max(1.0, 2.1 * sc);
            double wBrd  = Math.Max(1.5, 2.2 * sc);

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // ── 1. Fond bleu acier + bordure dorée ───────────────────────
                var bgBrush = new LinearGradientBrush();
                bgBrush.StartPoint = new Point(0.15, 0);
                bgBrush.EndPoint   = new Point(0.55, 1);
                bgBrush.GradientStops.Add(new GradientStop(Color.FromRgb( 68, 112, 165), 0.00));
                bgBrush.GradientStops.Add(new GradientStop(Color.FromRgb( 42,  72, 115), 0.52));
                bgBrush.GradientStops.Add(new GradientStop(Color.FromRgb( 24,  48,  82), 1.00));
                double mg = Math.Max(1.5, 2.5 * s / 64.0);
                double cr = Math.Max(6.0, 10.0 * s / 64.0);
                dc.DrawRoundedRectangle(bgBrush,
                    new Pen(new SolidColorBrush(Color.FromArgb(218, 255, 185, 30)), wBrd),
                    new Rect(mg, mg, s - 2*mg, s - 2*mg), cr, cr);

                // ── 2. Grand halo derrière le soleil ─────────────────────────
                Point sunPt = P(sunX, sunY);
                var halo = new RadialGradientBrush();
                halo.Center = halo.GradientOrigin = new Point(0.5, 0.5);
                halo.GradientStops.Add(new GradientStop(Color.FromArgb(130, 255, 245,  90), 0.00));
                halo.GradientStops.Add(new GradientStop(Color.FromArgb( 55, 255, 185,  30), 0.42));
                halo.GradientStops.Add(new GradientStop(Color.FromArgb(  0, 255, 160,   0), 1.00));
                double glowR = sunR * 3.0;
                dc.DrawEllipse(halo, null, sunPt, glowR, glowR * 0.88);

                // ── 3. Rayons ambrés avec flèches ────────────────────────────
                var rayBrush = new SolidColorBrush(Color.FromArgb(228, 255, 200, 38));
                var rayEnds = new Point[]
                {
                    P(fx_left  + 0.024, fy_bot  - 0.013),
                    P(fx_mid   - 0.062, fy_bot  + 0.002),
                    P(fx_mid   + 0.076, fy_bot  - 0.008),
                    P(bx_r     - 0.018, by_b    + 0.010),
                };
                foreach (var ep in rayEnds)
                    DrawIconArrow(dc, sunPt, ep, wRay, rayBrush);

                // ── 4. Remplissage faces (effet 3D) ──────────────────────────
                // Face droite (ombre légère)
                var rfGeo = new StreamGeometry();
                using (var sgc = rfGeo.Open())
                {
                    sgc.BeginFigure(P(fx_right, fy_top), true, true);
                    sgc.LineTo(P(bx_r, by_t),   true, false);
                    sgc.LineTo(P(bx_r, by_b),   true, false);
                    sgc.LineTo(P(fx_right, fy_bot), true, false);
                }
                rfGeo.Freeze();
                dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(38, 0, 0, 0)), null, rfGeo);

                // Toit (lueur douce)
                var tfGeo = new StreamGeometry();
                using (var sgc = tfGeo.Open())
                {
                    sgc.BeginFigure(P(fx_left,  fy_top), true, true);
                    sgc.LineTo(P(fx_right, fy_top), true, false);
                    sgc.LineTo(P(bx_r,     by_t),   true, false);
                    sgc.LineTo(P(bx_l,     by_t),   true, false);
                }
                tfGeo.Freeze();
                dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(22, 200, 230, 255)), null, tfGeo);

                // ── 5. Wireframe bâtiment ─────────────────────────────────────
                var penMain = new Pen(new SolidColorBrush(Color.FromRgb(240, 248, 255)), wMain)
                    { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                var penPart = new Pen(new SolidColorBrush(Color.FromArgb(190, 205, 228, 255)), wPart)
                    { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                var penHide = new Pen(new SolidColorBrush(Color.FromArgb( 85, 180, 215, 255)), wHide)
                    {
                        DashStyle    = new DashStyle(new double[] { 3.5, 2.5 }, 0.0),
                        StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
                    };

                // Face avant
                dc.DrawLine(penMain, P(fx_left,  fy_top), P(fx_right, fy_top));
                dc.DrawLine(penMain, P(fx_left,  fy_bot), P(fx_right, fy_bot));
                dc.DrawLine(penMain, P(fx_left,  fy_top), P(fx_left,  fy_bot));
                dc.DrawLine(penMain, P(fx_right, fy_top), P(fx_right, fy_bot));
                // Toit
                dc.DrawLine(penMain, P(fx_left,  fy_top), P(bx_l, by_t));
                dc.DrawLine(penMain, P(fx_right, fy_top), P(bx_r, by_t));
                dc.DrawLine(penMain, P(bx_l,     by_t),   P(bx_r, by_t));
                // Face droite (arêtes visibles)
                dc.DrawLine(penMain, P(bx_r,     by_t),   P(bx_r,     by_b));
                dc.DrawLine(penMain, P(fx_right, fy_bot),  P(bx_r,     by_b));

                // Cloison gauche/droite — face avant (ligne verticale centrale)
                dc.DrawLine(penPart, P(fx_mid, fy_top), P(fx_mid, fy_bot));
                // Cloison gauche/droite — toit (diagonale vers l'arrière)
                dc.DrawLine(penPart, P(fx_mid, fy_top), P(fx_mid + dep_x, fy_top + dep_y));

                // Cloison avant/arrière (50 % profondeur) — toit
                dc.DrawLine(penPart,
                    P(fx_left  + dep_x*0.5, fy_top + dep_y*0.5),
                    P(fx_right + dep_x*0.5, fy_top + dep_y*0.5));
                // Cloison avant/arrière — face droite
                dc.DrawLine(penPart,
                    P(fx_right + dep_x*0.5, fy_top + dep_y*0.5),
                    P(fx_right + dep_x*0.5, fy_bot + dep_y*0.5));

                // Arêtes cachées en pointillés (résolution suffisante)
                if (withDash)
                {
                    dc.DrawLine(penHide, P(bx_l,     by_t),          P(bx_l, fy_bot + dep_y));
                    dc.DrawLine(penHide, P(fx_left,  fy_bot),         P(bx_l, fy_bot + dep_y));
                    dc.DrawLine(penHide, P(bx_l,     fy_bot + dep_y), P(bx_r, by_b));
                }

                // ── 6. Étoile soleil (8 pointes) ──────────────────────────────
                var starGlow = new RadialGradientBrush(
                    Color.FromArgb( 75, 255, 240, 120), Color.FromArgb(0, 255, 200, 50));
                dc.DrawEllipse(starGlow, null, sunPt, sunR * 1.95, sunR * 1.95);

                var starFill = new RadialGradientBrush(
                    Color.FromRgb(255, 255, 205), Color.FromRgb(255, 182, 12));
                starFill.Center = starFill.GradientOrigin = new Point(0.38, 0.30);
                DrawIconStar(dc, sunPt, sunR, sunR * 0.38, 8, starFill, null);

                // Éclat central
                var coreBrush = new RadialGradientBrush(
                    Color.FromRgb(255, 255, 255), Color.FromArgb(0, 255, 230, 100));
                dc.DrawEllipse(coreBrush, null, sunPt, sunR * 0.33, sunR * 0.33);

                // ── 7. Lueur de convergence au sol ────────────────────────────
                double spFx = fx_left + (fx_right - fx_left) * 0.490;
                double spFy = fy_bot  - 0.013;
                Point  spPt = P(spFx, spFy);
                double spR  = Math.Max(3.0, 0.052 * s);

                var spGlow = new RadialGradientBrush(
                    Color.FromArgb(150, 255, 242, 110), Color.FromArgb(0, 255, 200, 50));
                dc.DrawEllipse(spGlow, null, spPt, spR, spR * 0.55);
                DrawIconStar(dc, spPt, spR * 0.42, spR * 0.15, 6,
                    new SolidColorBrush(Color.FromArgb(195, 255, 242, 130)), null);

                // ── 8. Texte "SkyLight" + "Initium" (size >= 200) ─────────────
                if (withText)
                {
                    var boldFace = new Typeface(
                        new FontFamily("Arial"),
                        FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
                    var italFace = new Typeface(
                        new FontFamily("Arial"),
                        FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);

                    double skyFz  = 0.093 * s;
                    double initFz = 0.052 * s;
                    double textY  = 0.768 * s;
                    double shOff  = 0.007 * s;

                    var bSky    = new SolidColorBrush(Color.FromRgb(172, 102,  38));
                    var bLight  = new SolidColorBrush(Color.FromRgb(255, 185,  28));
                    var bInit   = new SolidColorBrush(Color.FromRgb(178, 205, 222));
                    var bShad   = new SolidColorBrush(Color.FromArgb( 95,   0,   0,   0));

#pragma warning disable CS0618
                    var ftSky   = new FormattedText("Sky",     System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bSky);
                    var ftLight = new FormattedText("Light",   System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bLight);
                    var ftInit  = new FormattedText("Initium", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, italFace, initFz, bInit);
                    var ftSkySh = new FormattedText("Sky",     System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bShad);
                    var ftLiSh  = new FormattedText("Light",   System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bShad);
#pragma warning restore CS0618

                    double totalW = ftSky.Width + ftLight.Width;
                    double startX = (s - totalW) / 2.0;

                    dc.DrawText(ftSkySh, new Point(startX + shOff,               textY + shOff));
                    dc.DrawText(ftLiSh,  new Point(startX + ftSky.Width + shOff, textY + shOff));
                    dc.DrawText(ftSky,   new Point(startX,               textY));
                    dc.DrawText(ftLight, new Point(startX + ftSky.Width, textY));

                    double initX = (s - ftInit.Width) / 2.0;
                    dc.DrawText(ftInit, new Point(initX, textY + ftSky.Height * 0.93));
                }
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        /// <summary>Dessine une étoile à N pointes centrée en <paramref name="center"/>.</summary>
        private static void DrawIconStar(DrawingContext dc, Point center,
            double outerR, double innerR, int points, Brush fill, Pen outline)
        {
            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                bool first = true;
                for (int i = 0; i < points * 2; i++)
                {
                    double angle = i * Math.PI / points - Math.PI / 2.0;
                    double r = (i % 2 == 0) ? outerR : innerR;
                    var pt = new Point(center.X + r * Math.Cos(angle),
                                       center.Y + r * Math.Sin(angle));
                    if (first) { sgc.BeginFigure(pt, true, true); first = false; }
                    else        sgc.LineTo(pt, true, false);
                }
            }
            sg.Freeze();
            dc.DrawGeometry(fill, outline, sg);
        }

        /// <summary>Dessine une ligne avec une tête de flèche remplie à l'extrémité.</summary>
        private static void DrawIconArrow(DrawingContext dc, Point from, Point to,
            double thickness, Brush brush)
        {
            dc.DrawLine(
                new Pen(brush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Flat },
                from, to);

            double angle    = Math.Atan2(to.Y - from.Y, to.X - from.X);
            double headLen  = Math.Max(4.0, thickness * 4.2);
            double halfAng  = Math.PI / 5.5;

            var t1 = new Point(to.X - headLen * Math.Cos(angle - halfAng),
                               to.Y - headLen * Math.Sin(angle - halfAng));
            var t2 = new Point(to.X - headLen * Math.Cos(angle + halfAng),
                               to.Y - headLen * Math.Sin(angle + halfAng));

            var sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                sgc.BeginFigure(to, true, true);
                sgc.LineTo(t1, true, false);
                sgc.LineTo(t2, true, false);
            }
            sg.Freeze();
            dc.DrawGeometry(brush, null, sg);
        }

        static Image BuildLogoV21(double height)
        {
            var img = new Image
            {
                Height = height,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 6, 6, 6)
            };
            if (File.Exists(LogoV21Path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource         = new Uri(LogoV21Path);
                bmp.DecodePixelHeight = (int)height;
                bmp.CacheOption       = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                img.Source = bmp;
            }
            return img;
        }
    }
}
