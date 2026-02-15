using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.Core;
using WpfGrid = System.Windows.Controls.Grid;

namespace RevitLightingPlugin.UI
{
    public class LuminaireCatalogWindow : Window
    {
        private DataGrid _dataGrid;
        private DatabaseManager _database;
        private List<LuminaireInfo> _allLuminaires;
        private TextBlock _countText;

        public LuminaireCatalogWindow()
        {
            InitializeUI();
            _database = new DatabaseManager();
            LoadLuminaires();
        }

        private void InitializeUI()
        {
            Title = "Catalogue de Luminaires";
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            // En-tête
            var headerPanel = new StackPanel
            {
                Background = System.Windows.Media.Brushes.LightSteelBlue,
                Margin = new Thickness(0)
            };

            var titleText = new TextBlock
            {
                Text = "💡 Catalogue de Luminaires",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 0)
            };

            var subtitleText = new TextBlock
            {
                Text = "Gérez votre base de données de luminaires",
                FontSize = 12,
                Margin = new Thickness(10, 0, 10, 10)
            };

            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(subtitleText);

            WpfGrid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Boutons d'action
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 5)
            };

            var addButton = new Button
            {
                Content = "➕ Ajouter",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            addButton.Click += AddButton_Click;

            var editButton = new Button
            {
                Content = "✏️ Modifier",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            editButton.Click += EditButton_Click;

            var deleteButton = new Button
            {
                Content = "🗑️ Supprimer",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            deleteButton.Click += DeleteButton_Click;

            var importButton = new Button
            {
                Content = "📥 Importer IES",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            importButton.Click += ImportButton_Click;

            var refreshButton = new Button
            {
                Content = "🔄 Actualiser",
                Width = 100,
                Height = 30
            };
            refreshButton.Click += RefreshButton_Click;

            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(deleteButton);
            buttonPanel.Children.Add(importButton);
            buttonPanel.Children.Add(refreshButton);

            // DataGrid container
            var gridContainer = new WpfGrid();
            gridContainer.Margin = new Thickness(10, 0, 10, 0);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            _dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                GridLinesVisibility = DataGridGridLinesVisibility.All
            };

            // Colonnes
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("Id"),
                Width = 40
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Fabricant",
                Binding = new System.Windows.Data.Binding("Fabricant"),
                Width = 100
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Nom",
                Binding = new System.Windows.Data.Binding("Nom"),
                Width = 200
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Référence",
                Binding = new System.Windows.Data.Binding("Reference"),
                Width = 100
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Flux (lm)",
                Binding = new System.Windows.Data.Binding("FluxLumineux"),
                Width = 80
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Puissance (W)",
                Binding = new System.Windows.Data.Binding("Puissance"),
                Width = 100
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Efficacité (lm/W)",
                Binding = new System.Windows.Data.Binding("Efficacite"),
                Width = 110
            });

            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "T° Couleur (K)",
                Binding = new System.Windows.Data.Binding("TemperatureCouleur"),
                Width = 110
            });

            scrollViewer.Content = _dataGrid;
            gridContainer.Children.Add(scrollViewer);

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(buttonPanel);
            contentPanel.Children.Add(gridContainer);

            WpfGrid.SetRow(contentPanel, 1);
            mainGrid.Children.Add(contentPanel);

            // Pied de page
            var footerPanel = new StackPanel
            {
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                Margin = new Thickness(0)
            };

            var footerGrid = new WpfGrid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _countText = new TextBlock
            {
                Text = "Luminaires (0)",
                FontSize = 12,
                Margin = new Thickness(10, 10, 10, 10),
                VerticalAlignment = VerticalAlignment.Center
            };

            var closeButton = new Button
            {
                Content = "Fermer",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 5, 10, 5)
            };
            closeButton.Click += (s, e) => Close();

            WpfGrid.SetColumn(_countText, 0);
            WpfGrid.SetColumn(closeButton, 1);

            footerGrid.Children.Add(_countText);
            footerGrid.Children.Add(closeButton);

            footerPanel.Children.Add(footerGrid);

            WpfGrid.SetRow(footerPanel, 2);
            mainGrid.Children.Add(footerPanel);

            Content = mainGrid;
        }

        private void LoadLuminaires()
        {
            try
            {
                _allLuminaires = _database.GetAllLuminaires();
                _dataGrid.ItemsSource = _allLuminaires;

                // Mettre à jour le compteur
                UpdateCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des luminaires :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UpdateCount()
        {
            if (_countText != null && _allLuminaires != null)
            {
                _countText.Text = $"Luminaires ({_allLuminaires.Count})";
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LuminaireEditDialog();
            if (dialog.ShowDialog() == true)
            {
                var newLuminaire = dialog.Luminaire;
                _database.AddLuminaire(newLuminaire);
                LoadLuminaires();
                MessageBox.Show("Luminaire ajouté avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dataGrid.SelectedItem is LuminaireInfo selected)
            {
                var dialog = new LuminaireEditDialog(selected);
                if (dialog.ShowDialog() == true)
                {
                    _database.UpdateLuminaire(dialog.Luminaire);
                    LoadLuminaires();
                    MessageBox.Show("Luminaire modifié avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un luminaire à modifier.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dataGrid.SelectedItem is LuminaireInfo selected)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le luminaire '{selected.Nom}' ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _database.DeleteLuminaire(selected.Id);
                    LoadLuminaires();
                    MessageBox.Show("Luminaire supprimé avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un luminaire à supprimer.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Fonctionnalité d'import IES à venir dans la prochaine version !",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLuminaires();
        }
    }

    // Dialogue d'édition de luminaire
    public class LuminaireEditDialog : Window
    {
        private TextBox _fabricantTextBox;
        private TextBox _nomTextBox;
        private TextBox _referenceTextBox;
        private TextBox _fluxTextBox;
        private TextBox _puissanceTextBox;
        private TextBox _efficaciteTextBox;
        private TextBox _temperatureTextBox;

        public LuminaireInfo Luminaire { get; private set; }

        public LuminaireEditDialog(LuminaireInfo luminaire = null)
        {
            Title = luminaire == null ? "Ajouter un luminaire" : "Modifier un luminaire";
            Width = 400;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Luminaire = luminaire ?? new LuminaireInfo();

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Formulaire
            var formPanel = new StackPanel { Margin = new Thickness(20) };

            // Fabricant
            formPanel.Children.Add(new TextBlock { Text = "Fabricant :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _fabricantTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_fabricantTextBox);

            // Nom
            formPanel.Children.Add(new TextBlock { Text = "Nom :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _nomTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_nomTextBox);

            // Référence
            formPanel.Children.Add(new TextBlock { Text = "Référence :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _referenceTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_referenceTextBox);

            // Flux lumineux
            formPanel.Children.Add(new TextBlock { Text = "Flux lumineux (lm) :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _fluxTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_fluxTextBox);

            // Puissance
            formPanel.Children.Add(new TextBlock { Text = "Puissance (W) :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _puissanceTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_puissanceTextBox);

            // Efficacité
            formPanel.Children.Add(new TextBlock { Text = "Efficacité (lm/W) :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _efficaciteTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_efficaciteTextBox);

            // Température couleur
            formPanel.Children.Add(new TextBlock { Text = "Température couleur (K) :", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
            _temperatureTextBox = new TextBox { Height = 25 };
            formPanel.Children.Add(_temperatureTextBox);

            WpfGrid.SetRow(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            // Boutons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var saveButton = new Button
            {
                Content = "💾 Enregistrer",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Content = "❌ Annuler",
                Width = 100,
                Height = 30
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);

            WpfGrid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void LoadData()
        {
            _fabricantTextBox.Text = Luminaire.Fabricant;
            _nomTextBox.Text = Luminaire.Nom;
            _referenceTextBox.Text = Luminaire.Reference;
            _fluxTextBox.Text = Luminaire.FluxLumineux.ToString();
            _puissanceTextBox.Text = Luminaire.Puissance.ToString();
            _efficaciteTextBox.Text = Luminaire.Efficacite.ToString();
            _temperatureTextBox.Text = Luminaire.TemperatureCouleur.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(_fabricantTextBox.Text))
                {
                    MessageBox.Show("Le fabricant est requis.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_nomTextBox.Text))
                {
                    MessageBox.Show("Le nom est requis.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_referenceTextBox.Text))
                {
                    MessageBox.Show("La référence est requise.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Mise à jour des données
                Luminaire.Fabricant = _fabricantTextBox.Text.Trim();
                Luminaire.Nom = _nomTextBox.Text.Trim();
                Luminaire.Reference = _referenceTextBox.Text.Trim();
                Luminaire.FluxLumineux = int.Parse(_fluxTextBox.Text);
                Luminaire.Puissance = int.Parse(_puissanceTextBox.Text);
                Luminaire.Efficacite = int.Parse(_efficaciteTextBox.Text);
                Luminaire.TemperatureCouleur = int.Parse(_temperatureTextBox.Text);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la validation des données :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}