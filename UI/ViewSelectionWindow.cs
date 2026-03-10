using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Grid = System.Windows.Controls.Grid;
using Autodesk.Revit.DB.Architecture;
using WpfColor = System.Windows.Media.Color;

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
            SkyLightTheme.ApplyDarkWindow(this, 760, 520);
            Title = "Sélection des Vues";

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(0);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerDock = SkyLightTheme.BuildDarkHeader(
                "Sélection des Vues", "Vues 2D/3D à inclure dans le rapport", this);
            Grid.SetRow(headerDock, 0);
            mainGrid.Children.Add(headerDock);

            // ── Tableau des pièces ──
            var tableContainer = new Border
            {
                Margin = new Thickness(15, 12, 15, 0),
                BorderBrush = new SolidColorBrush(WpfColor.FromArgb(80, 0, 185, 255)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(WpfColor.FromArgb(30, 0, 60, 120))
            };
            tableContainer.Child = BuildTable();
            Grid.SetRow(tableContainer, 1);
            mainGrid.Children.Add(tableContainer);

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
            SkyLightTheme.StyleButton(btnOk, true);
            btnOk.Click += OnOkClick;
            btnPanel.Children.Add(btnOk);

            var btnCancel = new Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 30,
                IsCancel = true
            };
            SkyLightTheme.StyleButton(btnCancel, false);
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            btnPanel.Children.Add(btnCancel);

            mainGrid.Children.Add(btnPanel);

            Content = SkyLightTheme.BuildDarkShell(mainGrid, 730, 490);
        }

        /// <summary>Construit un tableau header+lignes sans ListView/GridView pour garantir l'alignement des colonnes.</summary>
        private Grid BuildTable()
        {
            _rows = new List<RoomViewRow>();
            var planOptions = BuildPlanOptions();
            var view3dOptions = Build3DOptions();

            var table = new Grid();
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                          // en-tête
            table.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });     // lignes

            // ── En-tête ──
            var headerBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(70, 130, 180)),
                Height = 32
            };
            var headerGrid = MakeRowGrid();
            AddHeaderCell(headerGrid, "Pièce",         0);
            AddHeaderCell(headerGrid, "Vue Plan (2D)", 1);
            AddHeaderCell(headerGrid, "Vue 3D",        2);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            table.Children.Add(headerBorder);

            // ── Lignes de données ──
            var rowsPanel = new StackPanel();

            bool alternate = false;
            foreach (var room in _rooms)
            {
                var row = new RoomViewRow
                {
                    RoomId    = room.Id,
                    RoomLabel = $"{room.Name}  ({room.Number})"
                };

                var cbPlan = new ComboBox { Margin = new Thickness(4, 2, 4, 2), VerticalAlignment = VerticalAlignment.Center };
                foreach (var opt in planOptions) cbPlan.Items.Add(opt);
                cbPlan.SelectedIndex = 0;
                row.ComboPlan = cbPlan;

                var cb3D = new ComboBox { Margin = new Thickness(4, 2, 4, 2), VerticalAlignment = VerticalAlignment.Center };
                foreach (var opt in view3dOptions) cb3D.Items.Add(opt);
                cb3D.SelectedIndex = 0;
                row.Combo3D = cb3D;

                _rows.Add(row);

                var rowGrid = MakeRowGrid();
                rowGrid.Height = 36;

                var lblRoom = new TextBlock
                {
                    Text = row.RoomLabel,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 4, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(lblRoom,  0);
                Grid.SetColumn(cbPlan,   1);
                Grid.SetColumn(cb3D,     2);
                rowGrid.Children.Add(lblRoom);
                rowGrid.Children.Add(cbPlan);
                rowGrid.Children.Add(cb3D);

                var rowBorder = new Border
                {
                    Child = rowGrid,
                    Background = alternate
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 248, 252))
                        : System.Windows.Media.Brushes.White,
                    BorderBrush     = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };
                rowsPanel.Children.Add(rowBorder);
                alternate = !alternate;
            }

            var scroll = new ScrollViewer
            {
                Content = rowsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 1);
            table.Children.Add(scroll);

            return table;
        }

        // Crée un Grid à 3 colonnes proportionnelles partagé entre header et lignes
        private static Grid MakeRowGrid()
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2,   GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2,   GridUnitType.Star) });
            return g;
        }

        private static void AddHeaderCell(Grid g, string text, int col)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 4, 0)
            };
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
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
