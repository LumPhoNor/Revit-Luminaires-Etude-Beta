using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.Core;
using WpfGrid = System.Windows.Controls.Grid;
using WpfVisibility = System.Windows.Visibility;

namespace RevitLightingPlugin.UI
{
    public class LightingAnalysisWindow : Window
    {
        private Document _document;
        private ComboBox _standardComboBox;
        private ComboBox _activityComboBox;
        private TextBox _maintenanceFactorTextBox;
        private CheckBox _advancedOptionsCheckBox;
        private StackPanel _advancedPanel;
        private DatabaseManager _database;

        // Nouveaux contrôles avancés
        private TextBox _gridSpacingTextBox;
        private List<TextBox> _heightTextBoxes;
        private StackPanel _heightsPanel;

        // P2: Contrôles pour flux indirect
        private CheckBox _includeIndirectCheckBox;
        private TextBox _ceilingReflectanceTextBox;
        private TextBox _wallReflectanceTextBox;
        private TextBox _floorReflectanceTextBox;

        // P3: Contrôles pour facteurs de maintenance variables
        private ComboBox _environmentComboBox;
        private ComboBox _luminaireEnclosureComboBox;
        private TextBlock _calculatedMaintenanceFactorText;

        public AnalysisSettings Settings { get; private set; }
        public double MaintenanceFactor { get; private set; }

        public LightingAnalysisWindow(Document document)
        {
            _document = document;
            _database = new DatabaseManager();
            InitializeUI();
            LoadDefaults();
        }

        private void InitializeUI()
        {
            Title = "Analyse d'Éclairage";
            Width = 500;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _heightTextBoxes = new List<TextBox>();

            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            // En-tête
            var headerPanel = new StackPanel
            {
                Background = System.Windows.Media.Brushes.LightSteelBlue,
                Margin = new Thickness(0)
            };

            var titleText = new TextBlock
            {
                Text = "💡 Analyse d'Éclairage",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 0)
            };

            var subtitleText = new TextBlock
            {
                Text = "Configuration des paramètres d'analyse selon EN 12464-1",
                FontSize = 12,
                Margin = new Thickness(10, 0, 10, 10)
            };

            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(subtitleText);

            WpfGrid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Formulaire
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(20, 10, 20, 10)
            };

            var formPanel = new StackPanel();

            // Norme
            formPanel.Children.Add(new TextBlock
            {
                Text = "Norme :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5)
            });

            _standardComboBox = new ComboBox { Height = 25 };
            _standardComboBox.Items.Add("EN 12464-1 (Bureaux)");
            _standardComboBox.Items.Add("EN 12464-1 (Commerce)");
            _standardComboBox.Items.Add("EN 12464-1 (Industrie)");
            _standardComboBox.SelectedIndex = 0;
            _standardComboBox.SelectionChanged += StandardComboBox_SelectionChanged;
            formPanel.Children.Add(_standardComboBox);

            // Type d'activité
            formPanel.Children.Add(new TextBlock
            {
                Text = "Type d'activité :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 15, 0, 5)
            });

            _activityComboBox = new ComboBox { Height = 25 };
            formPanel.Children.Add(_activityComboBox);

            // Facteur de maintenance
            formPanel.Children.Add(new TextBlock
            {
                Text = "Facteur de maintenance :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 15, 0, 5)
            });

            _maintenanceFactorTextBox = new TextBox
            {
                Height = 25,
                Text = "0.90"
            };
            formPanel.Children.Add(_maintenanceFactorTextBox);

            var maintenanceHelp = new TextBlock
            {
                Text = "Valeur recommandée : 0.80 à 0.90 (selon EN 12464-1)",
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            };
            formPanel.Children.Add(maintenanceHelp);

            // Options avancées (cochée par défaut)
            _advancedOptionsCheckBox = new CheckBox
            {
                Content = "Options avancées",
                Margin = new Thickness(0, 20, 0, 10),
                FontWeight = FontWeights.Bold,
                IsChecked = true
            };
            _advancedOptionsCheckBox.Checked += (s, e) => _advancedPanel.Visibility = WpfVisibility.Visible;
            _advancedOptionsCheckBox.Unchecked += (s, e) => _advancedPanel.Visibility = WpfVisibility.Collapsed;
            formPanel.Children.Add(_advancedOptionsCheckBox);

            // Panneau options avancées (visible par défaut pour accéder aux hauteurs multiples)
            _advancedPanel = new StackPanel
            {
                Visibility = WpfVisibility.Visible,
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var advancedLabel = new TextBlock
            {
                Text = "Paramètres avancés",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 5)
            };
            _advancedPanel.Children.Add(advancedLabel);

            // Espacement de la grille
            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Espacement de la grille (m) :",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10, 10, 10, 2),
                FontSize = 11
            });

            _gridSpacingTextBox = new TextBox
            {
                Text = "1.00",
                Height = 25,
                Margin = new Thickness(10, 0, 10, 2)
            };
            _advancedPanel.Children.Add(_gridSpacingTextBox);

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Plus petit = plus précis mais plus lent (0.25 à 3.0)",
                FontSize = 9,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(10, 2, 10, 10)
            });

            // Hauteurs de plan de travail
            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Hauteurs de plan de travail (m) :",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10, 10, 10, 5),
                FontSize = 11
            });

            _heightsPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 10, 5)
            };
            _advancedPanel.Children.Add(_heightsPanel);

            // Ajouter la première hauteur par défaut
            AddHeightTextBox("0.00", false);

            // Bouton pour ajouter une hauteur
            var addHeightButton = new Button
            {
                Content = "+ Ajouter une hauteur",
                Height = 25,
                Margin = new Thickness(10, 5, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 150
            };
            addHeightButton.Click += AddHeightButton_Click;
            _advancedPanel.Children.Add(addHeightButton);

            // P2: Section Flux Indirect (Réflexions)
            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Éclairement indirect (réflexions) :",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10, 15, 10, 5),
                FontSize = 11
            });

            _includeIndirectCheckBox = new CheckBox
            {
                Content = "Inclure l'éclairement indirect",
                Margin = new Thickness(10, 5, 10, 5),
                IsChecked = true,
                ToolTip = "Ajoute les réflexions des surfaces (plafond, murs, sol) - Améliore la précision"
            };
            _advancedPanel.Children.Add(_includeIndirectCheckBox);

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Réflectance plafond (0.0 - 1.0) :",
                Margin = new Thickness(20, 5, 10, 2),
                FontSize = 10
            });

            _ceilingReflectanceTextBox = new TextBox
            {
                Text = "0.70",
                Height = 25,
                Margin = new Thickness(20, 0, 10, 2),
                ToolTip = "Plafond blanc: 0.70-0.80, clair: 0.50-0.70, foncé: 0.10-0.30"
            };
            _advancedPanel.Children.Add(_ceilingReflectanceTextBox);

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Réflectance murs (0.0 - 1.0) :",
                Margin = new Thickness(20, 5, 10, 2),
                FontSize = 10
            });

            _wallReflectanceTextBox = new TextBox
            {
                Text = "0.50",
                Height = 25,
                Margin = new Thickness(20, 0, 10, 2),
                ToolTip = "Murs clairs: 0.50-0.70, moyens: 0.30-0.50, foncés: 0.10-0.30"
            };
            _advancedPanel.Children.Add(_wallReflectanceTextBox);

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Réflectance sol (0.0 - 1.0) :",
                Margin = new Thickness(20, 5, 10, 2),
                FontSize = 10
            });

            _floorReflectanceTextBox = new TextBox
            {
                Text = "0.20",
                Height = 25,
                Margin = new Thickness(20, 0, 10, 10),
                ToolTip = "Sol clair: 0.20-0.40, moyen: 0.10-0.20, foncé: 0.05-0.10"
            };
            _advancedPanel.Children.Add(_floorReflectanceTextBox);

            // P3: Facteurs de maintenance variables (EN 12464-1 Annexe B)
            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "⚙ Facteur de Maintenance Variable (EN 12464-1 Annexe B)",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 15, 10, 10),
                FontSize = 11
            });

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Type d'environnement :",
                Margin = new Thickness(20, 5, 10, 2),
                FontSize = 10
            });

            _environmentComboBox = new ComboBox
            {
                Height = 25,
                Margin = new Thickness(20, 0, 10, 2)
            };
            _environmentComboBox.Items.Add(new ComboBoxItem { Content = "Très propre (Bureau, résidentiel)", Tag = MaintenanceCategory.VeryClean });
            _environmentComboBox.Items.Add(new ComboBoxItem { Content = "Propre (Commerce)", Tag = MaintenanceCategory.Clean, IsSelected = true });
            _environmentComboBox.Items.Add(new ComboBoxItem { Content = "Normal (Industrie propre)", Tag = MaintenanceCategory.Normal });
            _environmentComboBox.Items.Add(new ComboBoxItem { Content = "Sale (Atelier, production)", Tag = MaintenanceCategory.Dirty });
            _environmentComboBox.Items.Add(new ComboBoxItem { Content = "Très sale (Environnement hostile)", Tag = MaintenanceCategory.VeryDirty });
            _environmentComboBox.SelectionChanged += OnMaintenanceParametersChanged;
            _advancedPanel.Children.Add(_environmentComboBox);

            _advancedPanel.Children.Add(new TextBlock
            {
                Text = "Type de boîtier luminaire :",
                Margin = new Thickness(20, 5, 10, 2),
                FontSize = 10
            });

            _luminaireEnclosureComboBox = new ComboBox
            {
                Height = 25,
                Margin = new Thickness(20, 0, 10, 2)
            };
            _luminaireEnclosureComboBox.Items.Add(new ComboBoxItem { Content = "Fermé étanche (IP65+)", Tag = LuminaireEnclosure.SealedIP65, IsSelected = true });
            _luminaireEnclosureComboBox.Items.Add(new ComboBoxItem { Content = "Semi-fermé (IP54)", Tag = LuminaireEnclosure.EnclosedIP54 });
            _luminaireEnclosureComboBox.Items.Add(new ComboBoxItem { Content = "Ouvert (IP20)", Tag = LuminaireEnclosure.OpenIP20 });
            _luminaireEnclosureComboBox.SelectionChanged += OnMaintenanceParametersChanged;
            _advancedPanel.Children.Add(_luminaireEnclosureComboBox);

            _calculatedMaintenanceFactorText = new TextBlock
            {
                Text = "➜ Facteur de maintenance calculé : 0.88",
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.Green,
                Margin = new Thickness(20, 5, 10, 10),
                FontSize = 11
            };
            _advancedPanel.Children.Add(_calculatedMaintenanceFactorText);

            formPanel.Children.Add(_advancedPanel);

            scrollViewer.Content = formPanel;
            WpfGrid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Boutons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 15)
            };

            var analyzeButton = new Button
            {
                Content = "🔍 Analyser",
                Width = 120,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };
            analyzeButton.Click += AnalyzeButton_Click;

            var cancelButton = new Button
            {
                Content = "❌ Annuler",
                Width = 100,
                Height = 35
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(analyzeButton);
            buttonPanel.Children.Add(cancelButton);

            WpfGrid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void LoadDefaults()
        {
            UpdateActivityList();
        }

        private void StandardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateActivityList();
        }

        private void UpdateActivityList()
        {
            _activityComboBox.Items.Clear();

            var selectedStandard = _standardComboBox.SelectedIndex;

            switch (selectedStandard)
            {
                case 0: // Bureaux
                    _activityComboBox.Items.Add("Écriture, lecture, traitement de données (500 lux)");
                    _activityComboBox.Items.Add("Dessin technique (750 lux)");
                    _activityComboBox.Items.Add("Postes de travail CAO (500 lux)");
                    _activityComboBox.Items.Add("Salles de conférence (500 lux)");
                    _activityComboBox.Items.Add("Réception (300 lux)");
                    break;

                case 1: // Commerce
                    _activityComboBox.Items.Add("Zones de vente générales (300 lux)");
                    _activityComboBox.Items.Add("Caisses (500 lux)");
                    _activityComboBox.Items.Add("Vitrines (750 lux)");
                    _activityComboBox.Items.Add("Stockage (200 lux)");
                    break;

                case 2: // Industrie
                    _activityComboBox.Items.Add("Assemblage de précision (750 lux)");
                    _activityComboBox.Items.Add("Travail mécanique moyen (500 lux)");
                    _activityComboBox.Items.Add("Travail grossier (300 lux)");
                    _activityComboBox.Items.Add("Circulation (150 lux)");
                    break;
            }

            _activityComboBox.SelectedIndex = 0;
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Valider le facteur de maintenance
                if (!double.TryParse(_maintenanceFactorTextBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double maintenanceFactor))
                {
                    MessageBox.Show(
                        "Le facteur de maintenance doit être un nombre valide (utilisez le point comme séparateur décimal, ex: 0.80).",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                if (maintenanceFactor <= 0 || maintenanceFactor > 1)
                {
                    MessageBox.Show(
                        "Le facteur de maintenance doit être entre 0 et 1.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Valider l'espacement de grille
                double gridSpacing = 1.0;
                if (!double.TryParse(_gridSpacingTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out gridSpacing))
                {
                    MessageBox.Show(
                        "L'espacement de la grille doit être un nombre valide.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                if (gridSpacing < 0.25 || gridSpacing > 3.0)
                {
                    MessageBox.Show(
                        "L'espacement de la grille doit être entre 0.25 et 3.0 mètres.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // Récupérer les hauteurs de plan de travail
                var workPlaneHeights = GetWorkPlaneHeights();
                if (workPlaneHeights.Count == 0)
                {
                    MessageBox.Show(
                        "Au moins une hauteur de plan de travail valide est requise.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // P2: Valider les réflectances
                bool includeIndirect = _includeIndirectCheckBox.IsChecked ?? true;
                double ceilingReflectance = 0.70;
                double wallReflectance = 0.50;
                double floorReflectance = 0.20;

                if (includeIndirect)
                {
                    if (!double.TryParse(_ceilingReflectanceTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out ceilingReflectance) ||
                        ceilingReflectance < 0 || ceilingReflectance > 1)
                    {
                        MessageBox.Show("La réflectance du plafond doit être entre 0.0 et 1.0", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!double.TryParse(_wallReflectanceTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out wallReflectance) ||
                        wallReflectance < 0 || wallReflectance > 1)
                    {
                        MessageBox.Show("La réflectance des murs doit être entre 0.0 et 1.0", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!double.TryParse(_floorReflectanceTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out floorReflectance) ||
                        floorReflectance < 0 || floorReflectance > 1)
                    {
                        MessageBox.Show("La réflectance du sol doit être entre 0.0 et 1.0", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Stocker le facteur de maintenance séparément
                MaintenanceFactor = maintenanceFactor;

                // P3: Récupérer les paramètres de maintenance variables
                var envItem = _environmentComboBox.SelectedItem as ComboBoxItem;
                var encItem = _luminaireEnclosureComboBox.SelectedItem as ComboBoxItem;
                MaintenanceCategory environment = envItem != null ? (MaintenanceCategory)envItem.Tag : MaintenanceCategory.Clean;
                LuminaireEnclosure enclosureType = encItem != null ? (LuminaireEnclosure)encItem.Tag : LuminaireEnclosure.SealedIP65;

                // Créer les paramètres avec les nouvelles propriétés
                Settings = new AnalysisSettings
                {
                    StandardName = _standardComboBox.SelectedItem?.ToString() ?? "EN 12464-1",
                    MinimumIlluminance = GetRequiredIlluminance(),
                    MinimumUniformity = 0.4,
                    GridSpacing = gridSpacing,
                    WorkPlaneHeights = workPlaneHeights,
                    UseIESData = true,
                    // P1: Facteur de maintenance configurable (legacy, conservé pour compatibilité)
                    MaintenanceFactor = maintenanceFactor,
                    // P2: Paramètres flux indirect
                    IncludeIndirectLight = includeIndirect,
                    CeilingReflectance = ceilingReflectance,
                    WallReflectance = wallReflectance,
                    FloorReflectance = floorReflectance,
                    // P3: Paramètres maintenance variables
                    Environment = environment,
                    LuminaireEnclosureType = enclosureType
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la validation :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private int GetRequiredIlluminance()
        {
            var activity = _activityComboBox.SelectedItem?.ToString() ?? "";

            // Extraire la valeur entre parenthèses
            var startIndex = activity.IndexOf('(');
            var endIndex = activity.IndexOf(' ', startIndex);

            if (startIndex > 0 && endIndex > startIndex)
            {
                var luxString = activity.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (int.TryParse(luxString, out int lux))
                {
                    return lux;
                }
            }

            return 500; // Valeur par défaut
        }

        private void AddHeightTextBox(string defaultValue, bool canRemove)
        {
            var heightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var heightTextBox = new TextBox
            {
                Text = defaultValue,
                Width = 100,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _heightTextBoxes.Add(heightTextBox);
            heightPanel.Children.Add(heightTextBox);

            if (canRemove)
            {
                var removeButton = new Button
                {
                    Content = "✕",
                    Width = 25,
                    Height = 25,
                    Foreground = System.Windows.Media.Brushes.Red
                };
                removeButton.Click += (s, e) => RemoveHeightTextBox(heightPanel, heightTextBox);
                heightPanel.Children.Add(removeButton);
            }

            _heightsPanel.Children.Add(heightPanel);
        }

        private void RemoveHeightTextBox(StackPanel heightPanel, TextBox heightTextBox)
        {
            _heightTextBoxes.Remove(heightTextBox);
            _heightsPanel.Children.Remove(heightPanel);
        }

        private void AddHeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (_heightTextBoxes.Count >= 3)
            {
                MessageBox.Show(
                    "Maximum 3 hauteurs de plan de travail.",
                    "Limite atteinte",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            AddHeightTextBox("1.20", true);
        }

        /// <summary>
        /// P3: Gestionnaire pour mise à jour du facteur de maintenance calculé
        /// </summary>
        private void OnMaintenanceParametersChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var envItem = _environmentComboBox.SelectedItem as ComboBoxItem;
                var encItem = _luminaireEnclosureComboBox.SelectedItem as ComboBoxItem;

                if (envItem != null && encItem != null)
                {
                    var environment = (MaintenanceCategory)envItem.Tag;
                    var enclosure = (LuminaireEnclosure)encItem.Tag;

                    // Créer un settings temporaire pour calculer le facteur
                    var tempSettings = new AnalysisSettings
                    {
                        Environment = environment,
                        LuminaireEnclosureType = enclosure
                    };

                    double factor = tempSettings.GetMaintenanceFactor();
                    _calculatedMaintenanceFactorText.Text = $"➜ Facteur de maintenance calculé : {factor:F2}";
                }
            }
            catch { }
        }

        private List<double> GetWorkPlaneHeights()
        {
            var heights = new List<double>();

            foreach (var textBox in _heightTextBoxes)
            {
                if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double height))
                {
                    if (height > 0 && height <= 10)
                    {
                        heights.Add(height);
                    }
                }
            }

            // Si aucune hauteur valide, utiliser la valeur par défaut
            if (heights.Count == 0)
            {
                heights.Add(0.0);
            }

            return heights;
        }
    }
}