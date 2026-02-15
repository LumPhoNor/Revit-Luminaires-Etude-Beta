using System;
using System.Windows;
using System.Windows.Controls;

namespace RevitLightingPlugin.UI
{
    public partial class ReportConfigWindow : Window
    {
        public string ProjectName { get; private set; }
        public string ProjectReference { get; private set; }
        public string ClientName { get; private set; }
        public string EngineerName { get; private set; }
        public string CompanyName { get; private set; }

        private TextBox _projectNameBox;
        private TextBox _projectRefBox;
        private TextBox _clientNameBox;
        private TextBox _engineerNameBox;
        private TextBox _companyNameBox;

        public ReportConfigWindow()
        {
            InitializeComponent();
            LoadDefaultValues();
        }

        private void InitializeComponent()
        {
            Title = "Configuration du Rapport";
            Width = 550;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var titleText = new TextBlock
            {
                Text = "📋 Informations du Rapport",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titleText, 0);
            mainGrid.Children.Add(titleText);

            // Form fields
            var formStack = new StackPanel();
            Grid.SetRow(formStack, 1);

            _projectNameBox = AddFormField(formStack, "Nom de l'affaire :", "Mon Projet");
            _projectRefBox = AddFormField(formStack, "Référence :", "REF-" + DateTime.Now.ToString("yyyyMMdd"));
            _clientNameBox = AddFormField(formStack, "Client :", "");
            _engineerNameBox = AddFormField(formStack, "Ingénieur :", Environment.UserName);
            _companyNameBox = AddFormField(formStack, "Bureau d'études :", "Mon Bureau d'Études");

            mainGrid.Children.Add(formStack);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OnOkClick;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 30,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private TextBox AddFormField(StackPanel panel, string label, string defaultValue)
        {
            var fieldPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            var labelText = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            fieldPanel.Children.Add(labelText);

            var textBox = new TextBox
            {
                Text = defaultValue,
                Padding = new Thickness(5),
                Height = 30
            };
            fieldPanel.Children.Add(textBox);

            panel.Children.Add(fieldPanel);
            return textBox;
        }

        private void LoadDefaultValues()
        {
            // Charger les valeurs par défaut ou sauvegardées
            ProjectName = "Mon Projet";
            ProjectReference = "REF-" + DateTime.Now.ToString("yyyyMMdd");
            ClientName = "";
            EngineerName = Environment.UserName;
            CompanyName = "Mon Bureau d'Études";
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            ProjectName = _projectNameBox.Text;
            ProjectReference = _projectRefBox.Text;
            ClientName = _clientNameBox.Text;
            EngineerName = _engineerNameBox.Text;
            CompanyName = _companyNameBox.Text;

            // Validation
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show(
                    "Le nom de l'affaire est obligatoire.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                MessageBox.Show(
                    "Le nom du bureau d'études est obligatoire.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}