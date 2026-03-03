using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitLightingPlugin.UI
{
    /// <summary>
    /// Sélection manuelle des vues 2D/3D à exporter pour chaque pièce.
    /// </summary>
    public class ViewSelectionWindow : Window
    {
        private readonly Document _doc;
        private readonly List<Room> _rooms;

        // Résultat : clé = RoomId, valeur = (PlanViewId, View3DId)
        // null        => Automatique (le plugin choisit)
        // InvalidElementId => Aucune (on n'exporte pas cette vue)
        // autre       => vue choisie par l'utilisateur
        public Dictionary<ElementId, RoomViewSelection> Selections { get; private set; }

        // Liste des vues disponibles peuplées une seule fois
        private List<ViewPlanItem> _availablePlanViews;
        private List<View3DItem> _available3DViews;

        // Les lignes du tableau
        private List<RoomViewRow> _rows;

        public ViewSelectionWindow(Document doc, List<Room> rooms)
        {
            _doc = doc;
            _rooms = rooms;
            Selections = new Dictionary<ElementId, RoomViewSelection>();

            BuildAvailableViews();
            InitializeComponent();
        }

        // ─────────────────────────────────────────────────────────────
        //  Construction de la liste des vues disponibles
        // ─────────────────────────────────────────────────────────────

        private void BuildAvailableViews()
        {
            // Vues plan existantes (non-template, FloorPlan)
            _availablePlanViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                .OrderBy(v => v.Name)
                .Select(v => new ViewPlanItem { Id = v.Id, Name = v.Name })
                .ToList();

            // Vues 3D existantes (non-template)
            _available3DViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(v => !v.IsTemplate)
                .OrderBy(v => v.Name)
                .Select(v => new View3DItem { Id = v.Id, Name = v.Name })
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────
        //  Construction de l'interface
        // ─────────────────────────────────────────────────────────────

        private void InitializeComponent()
        {
            Title = "Sélection des Vues";
            Width = 760;
            Height = 520;
            MinWidth = 600;
            MinHeight = 380;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(0);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });          // header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // liste
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });          // boutons

            // ── Header bicolore ──
            var headerDock = new DockPanel { LastChildFill = true };

            string logoPath = @"C:\Users\JEDI-Lee\Documents\Projets Plugin\Logo\Logo SkyLight.jpg";
            if (System.IO.File.Exists(logoPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(logoPath);
                bmp.DecodePixelHeight = 90;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                var logoImg = new Image
                {
                    Source = bmp,
                    Height = 90,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Margin = new Thickness(8),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var logoBorder = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    Child = logoImg
                };
                DockPanel.SetDock(logoBorder, Dock.Left);
                headerDock.Children.Add(logoBorder);
            }

            var titleZone = new StackPanel
            {
                Background = System.Windows.Media.Brushes.LightSteelBlue,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0)
            };
            titleZone.Children.Add(new TextBlock
            {
                Text = "Sélection des Vues",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(15, 22, 15, 4),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleZone.Children.Add(new TextBlock
            {
                Text = "Choisissez les vues à inclure dans le rapport pour chaque pièce",
                FontSize = 11,
                Margin = new Thickness(15, 0, 15, 10),
                Foreground = System.Windows.Media.Brushes.DimGray
            });
            headerDock.Children.Add(titleZone);

            Grid.SetRow(headerDock, 0);
            mainGrid.Children.Add(headerDock);

            // ── Tableau des pièces ──
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(15, 12, 15, 0)
            };

            var listView = BuildListView();
            scroll.Content = listView;

            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            // ── Boutons ──
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 12)
            };
            Grid.SetRow(btnPanel, 2);

            var btnOk = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            btnOk.Click += OnOkClick;
            btnPanel.Children.Add(btnOk);

            var btnCancel = new Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 30,
                IsCancel = true
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            btnPanel.Children.Add(btnCancel);

            mainGrid.Children.Add(btnPanel);

            Content = mainGrid;
        }

        private ListView BuildListView()
        {
            _rows = new List<RoomViewRow>();

            // Options communes pour les ComboBoxes
            var planOptions = BuildPlanOptions();
            var view3dOptions = Build3DOptions();

            var listView = new ListView();

            var gridView = new GridView();

            // Colonne Pièce
            var colRoom = new GridViewColumn
            {
                Header = "Pièce",
                Width = 200
            };
            colRoom.CellTemplate = MakeTextTemplate("RoomLabel");
            gridView.Columns.Add(colRoom);

            // Colonne Vue Plan
            var colPlan = new GridViewColumn
            {
                Header = "Vue Plan (2D)",
                Width = 250
            };
            gridView.Columns.Add(colPlan);

            // Colonne Vue 3D
            var col3D = new GridViewColumn
            {
                Header = "Vue 3D",
                Width = 250
            };
            gridView.Columns.Add(col3D);

            listView.View = gridView;

            // Remplir les lignes
            foreach (var room in _rooms)
            {
                var row = new RoomViewRow
                {
                    RoomId = room.Id,
                    RoomLabel = $"{room.Name}  ({room.Number})"
                };

                var cbPlan = new ComboBox { Width = 230, Margin = new Thickness(2) };
                foreach (var opt in planOptions)
                    cbPlan.Items.Add(opt);
                cbPlan.SelectedIndex = 0; // Automatique
                row.ComboPlan = cbPlan;

                var cb3D = new ComboBox { Width = 230, Margin = new Thickness(2) };
                foreach (var opt in view3dOptions)
                    cb3D.Items.Add(opt);
                cb3D.SelectedIndex = 0; // Automatique
                row.Combo3D = cb3D;

                _rows.Add(row);

                // Chaque ligne est un Grid dans le ListViewItem
                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });

                var lblRoom = new TextBlock
                {
                    Text = row.RoomLabel,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(lblRoom, 0);
                rowGrid.Children.Add(lblRoom);

                Grid.SetColumn(cbPlan, 1);
                rowGrid.Children.Add(cbPlan);

                Grid.SetColumn(cb3D, 2);
                rowGrid.Children.Add(cb3D);

                var item = new ListViewItem
                {
                    Content = rowGrid,
                    Height = 36
                };
                listView.Items.Add(item);
            }

            return listView;
        }

        // ─────────────────────────────────────────────────────────────
        //  Construction des options pour les ComboBoxes
        // ─────────────────────────────────────────────────────────────

        private List<ViewComboItem> BuildPlanOptions()
        {
            var list = new List<ViewComboItem>
            {
                new ViewComboItem { Label = "Automatique", ViewId = null },
                new ViewComboItem { Label = "Aucune", ViewId = ElementId.InvalidElementId }
            };
            foreach (var v in _availablePlanViews)
                list.Add(new ViewComboItem { Label = v.Name, ViewId = v.Id });
            return list;
        }

        private List<ViewComboItem> Build3DOptions()
        {
            var list = new List<ViewComboItem>
            {
                new ViewComboItem { Label = "Automatique", ViewId = null },
                new ViewComboItem { Label = "Aucune", ViewId = ElementId.InvalidElementId }
            };
            foreach (var v in _available3DViews)
                list.Add(new ViewComboItem { Label = v.Name, ViewId = v.Id });
            return list;
        }

        // Helper : CellTemplate affichant une propriété texte (non utilisé pour les ComboBoxes mais gardé pour la colonne Pièce)
        private DataTemplate MakeTextTemplate(string bindingPath)
        {
            // Non utilisé finalement (on construit les cellules manuellement), retourne null
            return null;
        }

        // ─────────────────────────────────────────────────────────────
        //  Validation et collecte des résultats
        // ─────────────────────────────────────────────────────────────

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            Selections.Clear();
            foreach (var row in _rows)
            {
                var planItem = row.ComboPlan.SelectedItem as ViewComboItem;
                var v3dItem  = row.Combo3D.SelectedItem as ViewComboItem;

                Selections[row.RoomId] = new RoomViewSelection
                {
                    RoomId     = row.RoomId,
                    PlanViewId = planItem?.ViewId,
                    View3DId   = v3dItem?.ViewId
                };
            }
            DialogResult = true;
            Close();
        }

        // ─────────────────────────────────────────────────────────────
        //  Classes internes
        // ─────────────────────────────────────────────────────────────

        private class RoomViewRow
        {
            public ElementId RoomId   { get; set; }
            public string    RoomLabel { get; set; }
            public ComboBox  ComboPlan { get; set; }
            public ComboBox  Combo3D   { get; set; }
        }

        private class ViewComboItem
        {
            public string    Label  { get; set; }
            public ElementId ViewId { get; set; } // null = auto, InvalidElementId = aucune
            public override string ToString() => Label;
        }

        private class ViewPlanItem
        {
            public ElementId Id   { get; set; }
            public string    Name { get; set; }
        }

        private class View3DItem
        {
            public ElementId Id   { get; set; }
            public string    Name { get; set; }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Modèle de sélection exposé à la commande principale
    // ─────────────────────────────────────────────────────────────

    public class RoomViewSelection
    {
        public ElementId RoomId    { get; set; }
        /// <summary>null = Automatique | InvalidElementId = Aucune | autre = vue choisie</summary>
        public ElementId PlanViewId { get; set; }
        /// <summary>null = Automatique | InvalidElementId = Aucune | autre = vue choisie</summary>
        public ElementId View3DId  { get; set; }
    }
}
