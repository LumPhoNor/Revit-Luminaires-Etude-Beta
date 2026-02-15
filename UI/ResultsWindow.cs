using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RevitLightingPlugin.Models;
using RevitLightingPlugin.Core;

namespace RevitLightingPlugin.UI
{
    public partial class ResultsWindow : Window
    {
        private List<CalculationResult> _results;
        private Autodesk.Revit.UI.UIDocument _uidoc;

        public ResultsWindow(Autodesk.Revit.UI.UIDocument uidoc, List<CalculationResult> results)
        {
            _uidoc = uidoc;
            _results = results;
            InitializeComponent();
            DisplayResults();
        }

        private void InitializeComponent()
        {
            Title = "Résultats d'Analyse d'Éclairage";
            Width = 1000;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerPanel = new StackPanel
            {
                Background = System.Windows.Media.Brushes.LightSteelBlue,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var titleText = new TextBlock
            {
                Text = "📊 Résultats d'Analyse d'Éclairage",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(15, 10, 15, 10)
            };
            headerPanel.Children.Add(titleText);

            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Results area
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };

            var resultsPanel = new StackPanel();
            scrollViewer.Content = resultsPanel;

            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Store reference for later use
            resultsPanel.Tag = "ResultsPanel";

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15)
            };

            var exportButton = new System.Windows.Controls.Button
            {
                Content = "📄 Exporter en PDF",
                Width = 150,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.LightGreen
            };
            exportButton.Click += OnExportPdfClick;
            buttonPanel.Children.Add(exportButton);

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "Fermer",
                Width = 100,
                Height = 35
            };
            closeButton.Click += (s, e) => Close();
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 2);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void DisplayResults()
        {
            var mainGrid = Content as Grid;
            var scrollViewer = mainGrid.Children.OfType<ScrollViewer>().FirstOrDefault();
            var resultsPanel = scrollViewer?.Content as StackPanel;

            if (resultsPanel == null) return;

            resultsPanel.Children.Clear();

            // Summary
            AddSummarySection(resultsPanel);

            // Individual room results
            foreach (var result in _results)
            {
                AddRoomResult(resultsPanel, result);
            }
        }

        private void AddSummarySection(StackPanel panel)
        {
            var summaryBorder = new Border
            {
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(15),
                Background = System.Windows.Media.Brushes.WhiteSmoke
            };

            var summaryStack = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "📊 RÉSUMÉ GLOBAL",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            summaryStack.Children.Add(titleText);

            int compliantRooms = _results.Count(r => r.MeetsStandard);
            double totalArea = _results.Sum(r => r.RoomArea);
            double totalPower = _results.Sum(r => r.PuissanceTotale);
            double avgPowerDensity = totalArea > 0 ? totalPower / totalArea : 0;

            AddSummaryLine(summaryStack, "Nombre de pièces analysées", _results.Count.ToString());
            AddSummaryLine(summaryStack, "Pièces conformes EN 12464-1", $"{compliantRooms} / {_results.Count}");
            AddSummaryLine(summaryStack, "Surface totale", $"{totalArea:F2} m²");
            AddSummaryLine(summaryStack, "Puissance totale installée", $"{totalPower:F0} W");
            AddSummaryLine(summaryStack, "Densité de puissance moyenne", $"{avgPowerDensity:F2} W/m²");

            var statusText = new TextBlock
            {
                Text = compliantRooms == _results.Count ? "✅ PROJET CONFORME" : "⚠️ PROJET NON CONFORME",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = compliantRooms == _results.Count ?
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red,
                Margin = new Thickness(0, 15, 0, 0)
            };
            summaryStack.Children.Add(statusText);

            summaryBorder.Child = summaryStack;
            panel.Children.Add(summaryBorder);
        }

        private void AddSummaryLine(StackPanel panel, string label, string value)
        {
            var linePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var labelText = new TextBlock
            {
                Text = label + " : ",
                FontWeight = FontWeights.Bold,
                Width = 250
            };
            linePanel.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value
            };
            linePanel.Children.Add(valueText);

            panel.Children.Add(linePanel);
        }

        private void AddRoomResult(StackPanel panel, CalculationResult result)
        {
            var roomBorder = new Border
            {
                BorderBrush = result.MeetsStandard ?
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.OrangeRed,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15),
                Background = System.Windows.Media.Brushes.White
            };

            var roomStack = new StackPanel();

            // Room title
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var roomTitle = new TextBlock
            {
                Text = $"🏠 {result.RoomName}",
                FontSize = 16,
                FontWeight = FontWeights.Bold
            };
            titlePanel.Children.Add(roomTitle);

            var statusIcon = new TextBlock
            {
                Text = result.MeetsStandard ? " ✅" : " ❌",
                FontSize = 16,
                Margin = new Thickness(10, 0, 0, 0)
            };
            titlePanel.Children.Add(statusIcon);

            roomStack.Children.Add(titlePanel);

            // Activity type
            if (!string.IsNullOrEmpty(result.TypeActivite))
            {
                var activityText = new TextBlock
                {
                    Text = $"Type d'activité : {result.TypeActivite}",
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 10),
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray
                };
                roomStack.Children.Add(activityText);
            }

            // Create grid for data
            var dataGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            dataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Column 1
            AddDataPoint(dataGrid, 0, 0, "Surface", $"{result.RoomArea:F2} m²");
            AddDataPoint(dataGrid, 1, 0, "Éclairement moyen", $"{result.AverageIlluminance:F0} lux");
            AddDataPoint(dataGrid, 2, 0, "Uniformité", $"{result.Uniformity:F2}");
            AddDataPoint(dataGrid, 3, 0, "Nombre de luminaires", result.LuminaireCount.ToString());

            // Column 2
            AddDataPoint(dataGrid, 0, 1, "Éclairement requis", $"{result.EclairementRequis} lux");
            AddDataPoint(dataGrid, 1, 1, "Uniformité requise", $"{result.UniformiteRequise:F2}");
            AddDataPoint(dataGrid, 2, 1, "Puissance totale", $"{result.PuissanceTotale:F0} W");
            AddDataPoint(dataGrid, 3, 1, "Densité de puissance", $"{result.DensitePuissance:F2} W/m²");

            roomStack.Children.Add(dataGrid);

            // Luminaires
            if (result.LuminairesUtilises != null && result.LuminairesUtilises.Any())
            {
                var luminairesTitle = new TextBlock
                {
                    Text = "💡 Luminaires installés :",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                roomStack.Children.Add(luminairesTitle);

                foreach (var lum in result.LuminairesUtilises)
                {
                    var lumText = new TextBlock
                    {
                        Text = $"  • {lum.Quantity}x {lum.Fabricant} {lum.TypeName} ({lum.Puissance:F0}W, {lum.FluxLumineux:F0}lm)",
                        Margin = new Thickness(10, 2, 0, 2),
                        FontSize = 11
                    };
                    roomStack.Children.Add(lumText);
                }
            }

            // Height results (if multiple)
            if (result.HeightResults != null && result.HeightResults.Count > 1)
            {
                var heightsTitle = new TextBlock
                {
                    Text = "📐 Résultats par hauteur de plan de travail :",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 15, 0, 5)
                };
                roomStack.Children.Add(heightsTitle);

                foreach (var heightResult in result.HeightResults)
                {
                    var heightText = new TextBlock
                    {
                        Text = $"  Hauteur {heightResult.WorkPlaneHeight:F2} m : {heightResult.AverageIlluminance:F0} lux (moy), {heightResult.MinIlluminance:F0} lux (min), Uniformité {heightResult.Uniformity:F2}",
                        Margin = new Thickness(10, 2, 0, 2),
                        FontSize = 11,
                        Foreground = heightResult.MeetsStandard ?
                            System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.OrangeRed
                    };
                    roomStack.Children.Add(heightText);
                }
            }

            // Remarks
            if (!string.IsNullOrEmpty(result.Remarques))
            {
                var remarksTitle = new TextBlock
                {
                    Text = result.MeetsStandard ? "✅ Observations :" : "⚠️ Recommandations :",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = result.MeetsStandard ?
                        System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.OrangeRed
                };
                roomStack.Children.Add(remarksTitle);

                var remarksText = new TextBlock
                {
                    Text = result.Remarques,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray
                };
                roomStack.Children.Add(remarksText);
            }

            roomBorder.Child = roomStack;
            panel.Children.Add(roomBorder);
        }

        private void AddDataPoint(Grid grid, int row, int col, string label, string value)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(5)
            };

            var labelText = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray
            };
            panel.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 12
            };
            panel.Children.Add(valueText);

            Grid.SetRow(panel, row);
            Grid.SetColumn(panel, col);
            grid.Children.Add(panel);
        }

        private void OnExportPdfClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ouvrir la boîte de dialogue pour configurer le rapport
                var configWindow = new ReportConfigWindow();
                if (configWindow.ShowDialog() != true)
                {
                    return;
                }

                // Ouvrir la boîte de dialogue pour choisir l'emplacement du fichier
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Rapport_Eclairage_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var pdfGenerator = new PDFReportGenerator(_uidoc);

                    // Appel avec tous les paramètres dans le BON ORDRE
                    pdfGenerator.GenerateReport(
                        _results,                          // List<CalculationResult>
                        configWindow.ProjectName,          // projectName
                        configWindow.ProjectReference,     // reference
                        configWindow.ClientName,           // client
                        configWindow.EngineerName,         // engineer
                        configWindow.CompanyName,          // studyOffice
                        saveDialog.FileName                // outputPath (EN DERNIER!)
                    );

                    MessageBox.Show(
                        $"Le rapport PDF a été généré avec succès !\n\nEmplacement : {saveDialog.FileName}",
                        "Export réussi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Ouvrir le PDF automatiquement
                    try
                    {
                        System.Diagnostics.Process.Start(saveDialog.FileName);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la génération du PDF :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}