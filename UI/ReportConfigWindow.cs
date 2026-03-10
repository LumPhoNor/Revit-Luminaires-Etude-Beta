using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

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
            SkyLightTheme.ApplyDarkWindow(this, 550, 500);
            Title = "Configuration du Rapport";

            var mainGrid = new Grid();
            mainGrid.Margin = new Thickness(0);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerDock = SkyLightTheme.BuildDarkHeader(
                "Configuration du Rapport", "Informations de la page de garde PDF", this);
            Grid.SetRow(headerDock, 0);
            mainGrid.Children.Add(headerDock);

            // Form fields
            var formStack = new StackPanel { Margin = new Thickness(20, 15, 20, 10) };
            SkyLightTheme.SetPanelForeground(formStack);
            Grid.SetRow(formStack, 1);

            _projectNameBox  = AddFormField(formStack, "Nom de l'affaire :",  "Mon Projet");
            _projectRefBox   = AddFormField(formStack, "Référence :",          "REF-" + DateTime.Now.ToString("yyyyMMdd"));
            _clientNameBox   = AddFormField(formStack, "Client :",             "");
            _engineerNameBox = AddFormField(formStack, "Ingénieur :",          Environment.UserName);
            _companyNameBox  = AddFormField(formStack, "Bureau d'études :",    "Mon Bureau d'Études");

            mainGrid.Children.Add(formStack);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 10, 20, 15)
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            SkyLightTheme.StyleButton(okButton, true);
            okButton.Click += OnOkClick;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 30,
                IsCancel = true
            };
            SkyLightTheme.StyleButton(cancelButton, false);
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            mainGrid.Children.Add(buttonPanel);

            Content = SkyLightTheme.BuildDarkShell(mainGrid, 520, 470);
        }

        private TextBox AddFormField(StackPanel panel, string label, string defaultValue)
        {
            var fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            fieldPanel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var textBox = new TextBox
            {
                Text = defaultValue,
                Padding = new Thickness(5),
                Height = 30
            };
            SkyLightTheme.StyleTextBox(textBox);
            fieldPanel.Children.Add(textBox);

            panel.Children.Add(fieldPanel);
            return textBox;
        }

        private void LoadDefaultValues()
        {
            ProjectName = "Mon Projet";
            ProjectReference = "REF-" + DateTime.Now.ToString("yyyyMMdd");
            ClientName = "";
            EngineerName = Environment.UserName;
            CompanyName = "Mon Bureau d'Études";
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            ProjectName      = _projectNameBox.Text;
            ProjectReference = _projectRefBox.Text;
            ClientName       = _clientNameBox.Text;
            EngineerName     = _engineerNameBox.Text;
            CompanyName      = _companyNameBox.Text;

            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Le nom de l'affaire est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CompanyName))
            {
                MessageBox.Show("Le nom du bureau d'études est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
