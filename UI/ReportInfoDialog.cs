using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfGrid = System.Windows.Controls.Grid;

namespace RevitLightingPlugin.UI
{
    /// <summary>
    /// Dialogue de saisie des informations pour le rapport PDF
    /// </summary>
    public class ReportInfoDialog : Window
    {
        private TextBox _projectNameTextBox;
        private TextBox _projectReferenceTextBox;
        private TextBox _clientNameTextBox;
        private TextBox _engineeringFirmTextBox;
        private TextBox _engineerNameTextBox;

        public string ProjectName { get; private set; }
        public string ProjectReference { get; private set; }
        public string ClientName { get; private set; }
        public string EngineeringFirm { get; private set; }
        public string EngineerName { get; private set; }

        public ReportInfoDialog()
        {
            SkyLightTheme.ApplyDarkWindow(this, 550, 550);
            Title = "Informations du Rapport";

            InitializeUI();
            LoadDefaults();
        }

        private void InitializeUI()
        {
            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });

            var headerDock = SkyLightTheme.BuildDarkHeader(
                "Informations du Rapport", "Ces informations apparaîtront sur la page de garde", this);
            WpfGrid.SetRow(headerDock, 0);
            mainGrid.Children.Add(headerDock);

            // Formulaire
            var formPanel = new StackPanel
            {
                Margin = new Thickness(20, 15, 20, 15)
            };
            SkyLightTheme.SetPanelForeground(formPanel);

            // Nom de l'affaire
            formPanel.Children.Add(new TextBlock
            {
                Text = "Nom de l'affaire * :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            });
            _projectNameTextBox = new TextBox
            {
                Height = 28,
                Padding = new Thickness(5, 5, 5, 5),
                FontSize = 12
            };
            SkyLightTheme.StyleTextBox(_projectNameTextBox);
            formPanel.Children.Add(_projectNameTextBox);

            // Référence du projet
            formPanel.Children.Add(new TextBlock
            {
                Text = "Référence de l'affaire :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 5)
            });
            _projectReferenceTextBox = new TextBox
            {
                Height = 28,
                Padding = new Thickness(5, 5, 5, 5),
                FontSize = 12
            };
            SkyLightTheme.StyleTextBox(_projectReferenceTextBox);
            formPanel.Children.Add(_projectReferenceTextBox);

            // Client
            formPanel.Children.Add(new TextBlock
            {
                Text = "Nom du client * :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 5)
            });
            _clientNameTextBox = new TextBox
            {
                Height = 28,
                Padding = new Thickness(5, 5, 5, 5),
                FontSize = 12
            };
            SkyLightTheme.StyleTextBox(_clientNameTextBox);
            formPanel.Children.Add(_clientNameTextBox);

            // Bureau d'études
            formPanel.Children.Add(new TextBlock
            {
                Text = "Bureau d'études * :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 5)
            });
            _engineeringFirmTextBox = new TextBox
            {
                Height = 28,
                Padding = new Thickness(5, 5, 5, 5),
                FontSize = 12
            };
            SkyLightTheme.StyleTextBox(_engineeringFirmTextBox);
            formPanel.Children.Add(_engineeringFirmTextBox);

            // Ingénieur
            formPanel.Children.Add(new TextBlock
            {
                Text = "Nom de l'ingénieur :",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 5)
            });
            _engineerNameTextBox = new TextBox
            {
                Height = 28,
                Padding = new Thickness(5, 5, 5, 5),
                FontSize = 12
            };
            SkyLightTheme.StyleTextBox(_engineerNameTextBox);
            formPanel.Children.Add(_engineerNameTextBox);

            // Note
            var noteText = new TextBlock
            {
                Text = "* Champs obligatoires",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(SkyLightTheme.TextGray),
                Margin = new Thickness(0, 15, 0, 0)
            };
            formPanel.Children.Add(noteText);

            WpfGrid.SetRow(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            // Boutons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 15)
            };

            var okButton = new Button
            {
                Content = "✓ Générer le PDF",
                Width = 130,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };
            SkyLightTheme.StyleButton(okButton, true);
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Content = "✗ Annuler",
                Width = 100,
                Height = 32
            };
            SkyLightTheme.StyleButton(cancelButton, false);
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            WpfGrid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = SkyLightTheme.BuildDarkShell(mainGrid, 520, 520);
        }

        private void LoadDefaults()
        {
            _projectNameTextBox.Text = "Projet Éclairage";
            _projectReferenceTextBox.Text = $"REF-{DateTime.Now:yyyyMMdd}";
            _clientNameTextBox.Text = "";
            _engineeringFirmTextBox.Text = "Mon Bureau d'Études";
            _engineerNameTextBox.Text = Environment.UserName;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_projectNameTextBox.Text))
            {
                MessageBox.Show(
                    "Le nom de l'affaire est obligatoire.",
                    "Champ obligatoire",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                _projectNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_clientNameTextBox.Text))
            {
                MessageBox.Show(
                    "Le nom du client est obligatoire.",
                    "Champ obligatoire",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                _clientNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_engineeringFirmTextBox.Text))
            {
                MessageBox.Show(
                    "Le nom du bureau d'études est obligatoire.",
                    "Champ obligatoire",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                _engineeringFirmTextBox.Focus();
                return;
            }

            // Récupérer les valeurs
            ProjectName = _projectNameTextBox.Text.Trim();
            ProjectReference = _projectReferenceTextBox.Text.Trim();
            ClientName = _clientNameTextBox.Text.Trim();
            EngineeringFirm = _engineeringFirmTextBox.Text.Trim();
            EngineerName = _engineerNameTextBox.Text.Trim();

            DialogResult = true;
            Close();
        }
    }
}