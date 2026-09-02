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
    /// Fournit le thème Skylightning — aspect Revit neutre (gris) + boutons bleu site.
    /// </summary>
    internal static class SkylightningTheme
    {
        // ── Chemin du logo ────────────────────────────────────────────────────
        // Résolu relativement au dossier du plugin (Assets\Logo, copié au build) :
        // un chemin absolu vers le poste du développeur ne suit pas le plugin sur
        // une autre machine.
        private static readonly string AssemblyDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static readonly string LogoV21Path =
            System.IO.Path.Combine(AssemblyDir, "Assets", "Logo", "Nouveau icone Skylightning.png");

        // Version recadrée (marge grise retirée) : le dessin remplit mieux les
        // petites icônes du ruban Revit (32/16 px), où le logo complet paraissait minuscule.
        public static readonly string LogoRibbonIconPath =
            System.IO.Path.Combine(AssemblyDir, "Assets", "Logo", "Nouveau icone Skylightning - Ruban.png");

        // Police du logo texte "Skylightning" (identique au site vitrine : Playfair
        // Display, graisse 900/Black).
        public static readonly string PlayfairDisplayBlackPath =
            System.IO.Path.Combine(AssemblyDir, "Assets", "Fonts", "PlayfairDisplay-Black.ttf");

        // ── Palette gris neutre (aspect Revit) ───────────────────────────────
        // Fonds fenêtres
        public static readonly Color NavyDark  = Color.FromRgb(245, 246, 248);  // fond principal
        public static readonly Color NavyMid   = Color.FromRgb(235, 237, 241);  // fond header/panel
        public static readonly Color NavyLight = Color.FromRgb(255, 255, 255);  // fond inputs blanc
        public static readonly Color InputBg   = Color.FromRgb(255, 255, 255);  // inputs blanc
        // Boutons — bleu site (UNIQUEMENT sur boutons)
        public static readonly Color AccentBlue = Color.FromRgb( 29,  78, 216); // #1D4ED8 btn-primary
        public static readonly Color AccentHover = Color.FromRgb(30,  64, 175); // #1E40AF btn-hover
        public static readonly Color AccentGold  = Color.FromRgb(255, 185,  30); // icône (logo uniquement)
        // Textes
        public static readonly Color TextWhite = Color.FromRgb( 17,  24,  39);  // #111827 texte principal
        public static readonly Color TextCyan  = Color.FromRgb( 55,  65,  81);  // #374151 texte secondaire
        public static readonly Color TextGray  = Color.FromRgb(107, 114, 128);  // #6B7280 texte léger
        // Bordures
        public static readonly Color BorderGray = Color.FromRgb(209, 213, 219); // #D1D5DB
        // Conformité (lisible sur fond clair)
        public static readonly Color GreenOk   = Color.FromRgb( 22, 163,  74);  // #16A34A vert
        public static readonly Color RedWarn   = Color.FromRgb(220,  38,  38);  // #DC2626 rouge

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
            return new Border
            {
                Margin       = new Thickness(10),
                CornerRadius = new CornerRadius(8),
                Background   = new SolidColorBrush(NavyDark),
                ClipToBounds = true,
                BorderBrush     = new SolidColorBrush(BorderGray),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color       = Color.FromRgb(150, 150, 155),
                    BlurRadius  = 16,
                    ShadowDepth = 2,
                    Opacity     = 0.20
                },
                Child = innerContent
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
                BorderBrush     = new SolidColorBrush(BorderGray),
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
                Background      = new SolidColorBrush(NavyMid),
                BorderBrush     = new SolidColorBrush(BorderGray),
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
            tb.BorderBrush      = new SolidColorBrush(BorderGray);
            tb.BorderThickness  = new Thickness(1);
            tb.CaretBrush       = new SolidColorBrush(AccentBlue);
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
            var borderBrush = new SolidColorBrush(BorderGray);

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
            arrowTb.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(TextCyan));
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
                ? new SolidColorBrush(AccentBlue)                           // #1D4ED8 btn-primary site
                : new SolidColorBrush(Color.FromArgb(  0,   0,   0,   0)); // transparent
            btn.Foreground      = new SolidColorBrush(TextWhite);
            btn.BorderBrush     = new SolidColorBrush(AccentBlue);          // #1D4ED8
            btn.BorderThickness = new Thickness(primary ? 0 : 1.5);
        }

        public static void StyleCheckBox(CheckBox cb)
        {
            cb.Foreground = new SolidColorBrush(TextWhite);
        }

        public static void StyleListView(ListView lv)
        {
            lv.Background  = new SolidColorBrush(Color.FromRgb(25, 48, 76));
            lv.Foreground  = new SolidColorBrush(TextWhite);
            lv.BorderBrush = new SolidColorBrush(BorderGray);
        }

        public static void StyleDataGrid(DataGrid dg)
        {
            dg.Background               = new SolidColorBrush(Color.FromRgb(25, 48, 76));
            dg.Foreground               = new SolidColorBrush(TextWhite);
            dg.BorderBrush              = new SolidColorBrush(BorderGray);
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
            // Conservé pour compatibilité mais non utilisé (BuildDarkShell utilise SolidColorBrush)
            var b = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            b.GradientStops.Add(new GradientStop(NavyLight, 0.0));
            b.GradientStops.Add(new GradientStop(NavyDark,  1.0));
            return b;
        }

        static Canvas BuildGridCanvas(double w, double h)   => new Canvas { IsHitTestVisible = false };
        static Canvas BuildCornersCanvas(double w, double h) => new Canvas { IsHitTestVisible = false };

        /// <summary>
        /// Génère l'icône Skylightning : vue en coupe d'une pièce avec luminaire LED au plafond,
        /// cône de lumière ambré dégradé, plan de travail lumineux, points grille photométrique.
        /// Formes pleines pour lisibilité optimale à toutes tailles (16–300 px).
        /// </summary>
        public static BitmapSource CreateSkylightningIcon(int size)
        {
            double s        = (double)size;
            bool withText   = size >= 200;
            bool withDetail = size >= 28;

            // ── Géométrie pièce ──────────────────────────────────────────────
            double brdW = Math.Max(1.2, s * 0.028);
            double cr   = s * 0.07;
            double mg   = s * 0.025;

            // Zone scène (plus haute si pas de texte)
            double rL = s * 0.09, rR = s * 0.91;
            double rT = s * 0.11;
            double rB = withText ? s * 0.64 : s * 0.88;

            double wT  = Math.Max(2.0, (rR - rL) * 0.055);  // épaisseur mur
            double cT  = Math.Max(1.5, (rB - rT) * 0.055);  // plafond
            double fT  = Math.Max(1.5, (rB - rT) * 0.045);  // sol

            double iL = rL + wT, iR = rR - wT;
            double iT = rT + cT, iB = rB - fT;
            double iW = iR - iL;

            // Luminaire (70 % de la largeur intérieure)
            double lW  = iW * 0.70;
            double lH  = Math.Max(2.0, (rB - rT) * 0.060);
            double lCX = (iL + iR) * 0.5;
            double lL  = lCX - lW * 0.5, lR = lCX + lW * 0.5;
            double lT  = rT, lB = rT + lH;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // ── 1. Fond bleu dégradé + bordure dorée ─────────────────────
                var bg = new LinearGradientBrush();
                bg.StartPoint = new Point(0.2, 0.0);
                bg.EndPoint   = new Point(0.8, 1.0);
                bg.GradientStops.Add(new GradientStop(Color.FromRgb(12,  28,  65), 0.00));
                bg.GradientStops.Add(new GradientStop(Color.FromRgb(30,  65, 130), 0.50));
                bg.GradientStops.Add(new GradientStop(Color.FromRgb(12,  35,  80), 1.00));
                dc.DrawRoundedRectangle(bg,
                    new Pen(new SolidColorBrush(Color.FromArgb(230, 255, 190, 30)), brdW),
                    new Rect(mg, mg, s - 2*mg, s - 2*mg), cr, cr);

                // ── 2. Cône lumineux (avant parois pour qu'elles le masquent) ─
                var coneGeo = new StreamGeometry();
                using (var sgc = coneGeo.Open())
                {
                    sgc.BeginFigure(new Point(lL, lB), true, true);
                    sgc.LineTo(new Point(lR,  lB), true, false);
                    sgc.LineTo(new Point(iR,  iB), true, false);
                    sgc.LineTo(new Point(iL,  iB), true, false);
                }
                coneGeo.Freeze();
                var cone = new LinearGradientBrush();
                cone.StartPoint = new Point(0.5, 0.0);
                cone.EndPoint   = new Point(0.5, 1.0);
                cone.GradientStops.Add(new GradientStop(Color.FromArgb(210, 255, 235,  50), 0.00));
                cone.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 185,  10), 0.40));
                cone.GradientStops.Add(new GradientStop(Color.FromArgb( 10, 255, 120,   0), 1.00));
                dc.DrawGeometry(cone, null, coneGeo);

                // ── 3. Parois ─────────────────────────────────────────────────
                var wall = new SolidColorBrush(Color.FromArgb(195, 200, 225, 255));
                dc.DrawRectangle(wall, null, new Rect(rL,      rT,      rR - rL,    cT));   // plafond
                dc.DrawRectangle(wall, null, new Rect(rL,      rT,      wT,  rB - rT));     // mur G
                dc.DrawRectangle(wall, null, new Rect(rR - wT, rT,      wT,  rB - rT));     // mur D
                dc.DrawRectangle(wall, null, new Rect(rL,      rB - fT, rR - rL,    fT));   // sol

                // ── 4. Tache lumineuse sur le sol ─────────────────────────────
                var fg = new RadialGradientBrush();
                fg.Center = fg.GradientOrigin = new Point(0.5, 0.25);
                fg.GradientStops.Add(new GradientStop(Color.FromArgb(220, 255, 248, 110), 0.00));
                fg.GradientStops.Add(new GradientStop(Color.FromArgb( 80, 255, 200,  30), 0.50));
                fg.GradientStops.Add(new GradientStop(Color.FromArgb(  0, 255, 150,   0), 1.00));
                dc.DrawEllipse(fg, null,
                    new Point((iL + iR) * 0.5, iB - s * 0.018),
                    iW * 0.46, Math.Max(2.0, s * 0.042));

                // ── 5. Luminaire LED ──────────────────────────────────────────
                var lumF = new LinearGradientBrush();
                lumF.StartPoint = new Point(0.5, 0.0);
                lumF.EndPoint   = new Point(0.5, 1.0);
                lumF.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 245), 0.0));
                lumF.GradientStops.Add(new GradientStop(Color.FromRgb(255, 215,  70), 1.0));
                double lCR = Math.Max(1.0, lH * 0.35);
                dc.DrawRoundedRectangle(lumF, null, new Rect(lL, lT, lW, lH), lCR, lCR);

                // Liseré brillant et séparateurs internes LED
                var shine = new LinearGradientBrush(
                    Color.FromArgb(200, 255, 255, 255), Color.FromArgb(0, 255, 255, 255),
                    new Point(0, 0), new Point(0, 1));
                dc.DrawRoundedRectangle(shine, null,
                    new Rect(lL + lW * 0.06, lT, lW * 0.88, lH * 0.42), lCR * 0.8, lCR * 0.8);
                if (withDetail)
                {
                    var ledPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 200)), Math.Max(0.5, s * 0.008));
                    for (int i = 1; i < 5; i++)
                    {
                        double x = lL + lW * i / 5.0;
                        dc.DrawLine(ledPen, new Point(x, lT + lH * 0.15), new Point(x, lB - lH * 0.15));
                    }
                }

                // ── 6. Points grille photométrique ────────────────────────────
                if (withDetail)
                {
                    int nDots = size >= 48 ? 5 : 3;
                    double dotR = Math.Max(0.9, s * 0.024);
                    double dotY = iB - dotR * 1.4;
                    for (int di = 0; di < nDots; di++)
                    {
                        double t  = Math.Abs(di - (nDots - 1) / 2.0) / ((nDots - 1) / 2.0 + 0.001);
                        byte   al = (byte)(255 - t * 130);
                        var db = new RadialGradientBrush(
                            Color.FromArgb(al, 255, 248, 150),
                            Color.FromArgb(  0, 255, 200,  50));
                        double dotX = iL + iW * (di + 1.0) / (nDots + 1.0);
                        dc.DrawEllipse(db, null, new Point(dotX, dotY), dotR, dotR);
                    }
                }

                // ── 7. Texte "Skylightning" + "Initium" (size >= 200) ─────────
                if (withText)
                {
                    var boldFace = new Typeface(new FontFamily("Arial"),
                        FontStyles.Normal, FontWeights.Black, FontStretches.Normal);
                    var italFace = new Typeface(new FontFamily("Arial"),
                        FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);

                    double skyFz  = 0.108 * s;
                    double initFz = 0.058 * s;
                    double textY  = rB + (s - mg - rB) * 0.10;
                    double shOff  = 0.007 * s;

                    var bSky  = new SolidColorBrush(Color.FromRgb(175, 100,  35));
                    var bLght = new SolidColorBrush(Color.FromRgb(255, 182,  22));
                    var bInit = new SolidColorBrush(Color.FromRgb(172, 200, 218));
                    var bShad = new SolidColorBrush(Color.FromArgb( 90,   0,   0,   0));

#pragma warning disable CS0618
                    var ftSky  = new FormattedText("Sky",       System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bSky);
                    var ftLght = new FormattedText("lightning", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bLght);
                    var ftInit = new FormattedText("Initium",   System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, italFace, initFz, bInit);
                    var ftSkSh = new FormattedText("Sky",       System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bShad);
                    var ftLiSh = new FormattedText("lightning", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldFace, skyFz,  bShad);
#pragma warning restore CS0618

                    double totalW = ftSky.Width + ftLght.Width;
                    double startX = (s - totalW) / 2.0;

                    dc.DrawText(ftSkSh, new Point(startX + shOff,                textY + shOff));
                    dc.DrawText(ftLiSh, new Point(startX + ftSky.Width + shOff,  textY + shOff));
                    dc.DrawText(ftSky,  new Point(startX,                textY));
                    dc.DrawText(ftLght, new Point(startX + ftSky.Width,  textY));

                    double initX = startX + ftSky.Width + ftLght.Width * 0.08;
                    dc.DrawText(ftInit, new Point(initX, textY + ftSky.Height * 0.88));
                }
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
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
