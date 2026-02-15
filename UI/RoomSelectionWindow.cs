using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitLightingPlugin.UI
{
    public partial class RoomSelectionWindow : Window
    {
        private Document _doc;
        public List<Room> SelectedRooms { get; private set; }
        public Dictionary<ElementId, RoomActivityType> RoomActivities { get; private set; }
        public bool AnalyzeAllRooms { get; private set; }

        private ListView RoomListView;
        private TextBox SearchTextBox;
        private ComboBox LevelFilterComboBox;
        private List<RoomViewModel> AllRooms;

        public RoomSelectionWindow(Document doc)
        {
            _doc = doc;
            SelectedRooms = new List<Room>();
            RoomActivities = new Dictionary<ElementId, RoomActivityType>();
            AnalyzeAllRooms = false;

            InitializeComponent();
            LoadRooms();
        }

        private void InitializeComponent()
        {
            Title = "Sélection des Pièces - Analyse d'Éclairement";
            Width = 950; // Augmenté pour la nouvelle colonne
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = CreateHeader();
            System.Windows.Controls.Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            var searchPanel = CreateSearchPanel();
            System.Windows.Controls.Grid.SetRow(searchPanel, 1);
            mainGrid.Children.Add(searchPanel);

            var listPanel = CreateListPanel();
            System.Windows.Controls.Grid.SetRow(listPanel, 2);
            mainGrid.Children.Add(listPanel);

            var filterPanel = CreateFilterPanel();
            System.Windows.Controls.Grid.SetRow(filterPanel, 3);
            mainGrid.Children.Add(filterPanel);

            var buttonPanel = CreateButtonPanel();
            System.Windows.Controls.Grid.SetRow(buttonPanel, 4);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private StackPanel CreateHeader()
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(15, 15, 15, 10),
                Background = System.Windows.Media.Brushes.LightSteelBlue
            };

            var title = new TextBlock
            {
                Text = "📋 Sélectionnez les pièces et leur type d'activité",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10)
            };

            var subtitle = new TextBlock
            {
                Text = "Choisissez le type d'activité pour chaque pièce selon la norme EN 12464-1",
                FontSize = 12,
                Margin = new Thickness(10, 0, 10, 10),
                Foreground = System.Windows.Media.Brushes.DarkSlateGray
            };

            panel.Children.Add(title);
            panel.Children.Add(subtitle);

            return panel;
        }

        private GroupBox CreateListPanel()
        {
            var groupBox = new GroupBox
            {
                Header = "Liste des Pièces",
                Margin = new Thickness(15, 5, 15, 5)
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            RoomListView = new ListView();

            var gridView = new GridView();
            
            // Colonne checkbox
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "✓",
                Width = 40,
                CellTemplate = CreateCheckBoxTemplate()
            });
            
            // Colonne Nom
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Nom",
                Width = 180,
                DisplayMemberBinding = new System.Windows.Data.Binding("Name")
            });
            
            // Colonne Numéro
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "N°",
                Width = 60,
                DisplayMemberBinding = new System.Windows.Data.Binding("Number")
            });
            
            // Colonne Niveau
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Niveau",
                Width = 100,
                DisplayMemberBinding = new System.Windows.Data.Binding("Level")
            });
            
            // Colonne Surface
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Surface (m²)",
                Width = 90,
                DisplayMemberBinding = new System.Windows.Data.Binding("AreaDisplay")
            });
            
            // Colonne Luminaires
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "💡",
                Width = 50,
                DisplayMemberBinding = new System.Windows.Data.Binding("LuminaireCount")
            });

            // NOUVELLE COLONNE : Type d'activité avec ComboBox
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Type d'activité (EN 12464-1)",
                Width = 320,
                CellTemplate = CreateActivityTypeComboBoxTemplate()
            });

            RoomListView.View = gridView;
            scrollViewer.Content = RoomListView;
            groupBox.Content = scrollViewer;

            return groupBox;
        }

        private DataTemplate CreateCheckBoxTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("IsSelected")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            });
            factory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            return new DataTemplate { VisualTree = factory };
        }

        private DataTemplate CreateActivityTypeComboBoxTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(ComboBox));
            
            // Binding pour la valeur sélectionnée
            factory.SetBinding(ComboBox.SelectedItemProperty, new System.Windows.Data.Binding("SelectedActivityType")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            });
            
            // Binding pour la liste des items
            factory.SetBinding(ComboBox.ItemsSourceProperty, new System.Windows.Data.Binding("AvailableActivityTypes"));
            
            // Template pour l'affichage des items
            factory.SetValue(ComboBox.DisplayMemberPathProperty, "DisplayName");
            
            factory.SetValue(ComboBox.MarginProperty, new Thickness(2));

            return new DataTemplate { VisualTree = factory };
        }

        private StackPanel CreateSearchPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 5, 15, 5)
            };

            panel.Children.Add(new TextBlock
            {
                Text = "🔍 Recherche:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            SearchTextBox = new TextBox
            {
                Width = 200,
                Margin = new Thickness(0, 0, 20, 0)
            };
            SearchTextBox.TextChanged += OnSearchTextChanged;
            panel.Children.Add(SearchTextBox);

            return panel;
        }

        private StackPanel CreateFilterPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 5, 15, 10),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            panel.Children.Add(new TextBlock
            {
                Text = "🏢 Filtrer par niveau:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            LevelFilterComboBox = new ComboBox
            {
                Width = 200
            };
            LevelFilterComboBox.SelectionChanged += OnLevelFilterChanged;
            panel.Children.Add(LevelFilterComboBox);

            return panel;
        }

        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15, 10, 15, 15)
            };

            var analyzeAllButton = new System.Windows.Controls.Button
            {
                Content = "📊 Analyser TOUTES les pièces",
                Width = 200,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.LightGreen
            };
            analyzeAllButton.Click += OnAnalyzeAllClick;
            panel.Children.Add(analyzeAllButton);

            var analyzeSelectedButton = new System.Windows.Controls.Button
            {
                Content = "✓ Analyser les pièces sélectionnées",
                Width = 220,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.LightBlue
            };
            analyzeSelectedButton.Click += OnAnalyzeSelectedClick;
            panel.Children.Add(analyzeSelectedButton);

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "✖ Annuler",
                Width = 100,
                Height = 35
            };
            cancelButton.Click += OnCancelClick;
            panel.Children.Add(cancelButton);

            return panel;
        }

        private void LoadRooms()
        {
            AllRooms = new List<RoomViewModel>();

            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            var rooms = collector.Cast<Room>().Where(r => r.Area > 0).ToList();

            var luminaires = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var room in rooms)
            {
                var level = _doc.GetElement(room.LevelId) as Level;
                var luminaireCount = CountLuminairesInRoom(room, luminaires);

                AllRooms.Add(new RoomViewModel
                {
                    Room = room,
                    Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Sans nom",
                    Number = room.Number ?? "",
                    Level = level?.Name ?? "Inconnu",
                    AreaDisplay = (room.Area * 0.092903).ToString("F2"),
                    LuminaireCount = luminaireCount,
                    IsSelected = true
                });
            }

            var levels = AllRooms.Select(r => r.Level).Distinct().OrderBy(l => l).ToList();
            LevelFilterComboBox.Items.Add("Tous les niveaux");
            foreach (var level in levels)
            {
                LevelFilterComboBox.Items.Add(level);
            }
            LevelFilterComboBox.SelectedIndex = 0;

            RoomListView.ItemsSource = AllRooms;
        }

        private int CountLuminairesInRoom(Room room, List<FamilyInstance> luminaires)
        {
            int count = 0;
            foreach (var luminaire in luminaires)
            {
                var location = luminaire.Location as LocationPoint;
                if (location != null && room.IsPointInRoom(location.Point))
                {
                    count++;
                }
            }
            return count;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RoomListView.ItemsSource = AllRooms;
            }
            else
            {
                var filtered = AllRooms.Where(r =>
                    r.Name.ToLower().Contains(searchText) ||
                    r.Number.ToLower().Contains(searchText) ||
                    r.Level.ToLower().Contains(searchText)
                ).ToList();
                RoomListView.ItemsSource = filtered;
            }
        }

        private void OnLevelFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedLevel = LevelFilterComboBox.SelectedItem as string;
            if (selectedLevel == "Tous les niveaux")
            {
                RoomListView.ItemsSource = AllRooms;
            }
            else
            {
                var filtered = AllRooms.Where(r => r.Level == selectedLevel).ToList();
                RoomListView.ItemsSource = filtered;
            }
        }

        private void OnAnalyzeAllClick(object sender, RoutedEventArgs e)
        {
            SelectedRooms = AllRooms.Select(r => r.Room).ToList();
            AnalyzeAllRooms = true;
            
            // Sauvegarder les types d'activité
            foreach (var roomVM in AllRooms)
            {
                RoomActivities[roomVM.Room.Id] = roomVM.SelectedActivityType;
            }
            
            DialogResult = true;
            Close();
        }

        private void OnAnalyzeSelectedClick(object sender, RoutedEventArgs e)
        {
            var selected = AllRooms.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins une pièce.", "Aucune sélection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedRooms = selected.Select(r => r.Room).ToList();
            AnalyzeAllRooms = false;
            
            // Sauvegarder les types d'activité
            foreach (var roomVM in selected)
            {
                RoomActivities[roomVM.Room.Id] = roomVM.SelectedActivityType;
            }
            
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class RoomViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected;
        private RoomActivityType _selectedActivityType;

        public Room Room { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
        public string Level { get; set; }
        public string AreaDisplay { get; set; }
        public int LuminaireCount { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Liste des types d'activité disponibles
        public List<RoomActivityType> AvailableActivityTypes { get; set; }

        // Type d'activité sélectionné
        public RoomActivityType SelectedActivityType
        {
            get => _selectedActivityType;
            set
            {
                _selectedActivityType = value;
                OnPropertyChanged(nameof(SelectedActivityType));
            }
        }

        public RoomViewModel()
        {
            // Initialiser la liste des types d'activité selon EN 12464-1
            AvailableActivityTypes = new List<RoomActivityType>
            {
                new RoomActivityType { Code = "OFFICE", DisplayName = "Bureau - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "MEETING", DisplayName = "Salle de réunion - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "CLASSROOM", DisplayName = "Salle de classe - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "CORRIDOR", DisplayName = "Couloir/Circulation - 100 lux", RequiredLux = 100, UniformityMin = 0.40 },
                new RoomActivityType { Code = "STAIR", DisplayName = "Escalier - 150 lux", RequiredLux = 150, UniformityMin = 0.40 },
                new RoomActivityType { Code = "RESTROOM", DisplayName = "Sanitaires - 200 lux", RequiredLux = 200, UniformityMin = 0.40 },
                new RoomActivityType { Code = "WAREHOUSE", DisplayName = "Entrepôt - 200 lux", RequiredLux = 200, UniformityMin = 0.40 },
                new RoomActivityType { Code = "WORKSHOP", DisplayName = "Atelier - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "TECHNICAL", DisplayName = "Local technique - 300 lux", RequiredLux = 300, UniformityMin = 0.40 },
                new RoomActivityType { Code = "SURGERY", DisplayName = "Salle d'opération - 1000 lux", RequiredLux = 1000, UniformityMin = 0.70 },
                new RoomActivityType { Code = "EXAM", DisplayName = "Salle d'examen médical - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "RECEPTION", DisplayName = "Accueil - 300 lux", RequiredLux = 300, UniformityMin = 0.60 },
                new RoomActivityType { Code = "ARCHIVE", DisplayName = "Archives - 200 lux", RequiredLux = 200, UniformityMin = 0.40 },
                new RoomActivityType { Code = "RETAIL", DisplayName = "Commerce - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "KITCHEN", DisplayName = "Cuisine - 500 lux", RequiredLux = 500, UniformityMin = 0.60 },
                new RoomActivityType { Code = "PARKING", DisplayName = "Parking - 75 lux", RequiredLux = 75, UniformityMin = 0.40 },
                new RoomActivityType { Code = "CUSTOM", DisplayName = "Personnalisé - Définir manuellement", RequiredLux = 300, UniformityMin = 0.60 }
            };

            // Sélection par défaut : Bureau
            _selectedActivityType = AvailableActivityTypes[0];
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Définit un type d'activité selon EN 12464-1
    /// </summary>
    public class RoomActivityType
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public int RequiredLux { get; set; }
        public double UniformityMin { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
