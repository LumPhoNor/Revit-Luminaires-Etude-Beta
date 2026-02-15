using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Génère des graphiques de courbes photométriques IES
    /// </summary>
    public class IESChartGenerator
    {
        /// <summary>
        /// Génère un graphique polaire de la courbe photométrique
        /// </summary>
        public static string GeneratePolarChart(IESParser.IESData iesData, string outputPath)
        {
            if (iesData == null || iesData.CandelaValues.Count == 0)
                return null;

            try
            {
                int width = 600;
                int height = 600;
                int centerX = width / 2;
                int centerY = height / 2;
                int radius = Math.Min(width, height) / 2 - 40;

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.White);

                    // Dessiner les cercles concentriques (0%, 25%, 50%, 75%, 100%)
                    DrawConcentricCircles(g, centerX, centerY, radius, iesData.MaxCandela);

                    // Dessiner les axes (0°, 90°, 180°, 270°)
                    DrawAxes(g, centerX, centerY, radius);

                    // Dessiner la courbe polaire principale (plan vertical à 0°)
                    if (iesData.CandelaValues.Count > 0)
                    {
                        DrawPolarCurve(g, centerX, centerY, radius,
                            iesData.VerticalAngles.ToArray(),
                            iesData.CandelaValues[0].ToArray(),
                            iesData.MaxCandela,
                            Color.FromArgb(41, 128, 185), 3);
                    }

                    // Si distribution symétrique, dessiner aussi le plan à 90°
                    if (iesData.CandelaValues.Count > 1)
                    {
                        int midIndex = iesData.CandelaValues.Count / 2;
                        DrawPolarCurve(g, centerX, centerY, radius,
                            iesData.VerticalAngles.ToArray(),
                            iesData.CandelaValues[midIndex].ToArray(),
                            iesData.MaxCandela,
                            Color.FromArgb(231, 76, 60), 2);
                    }

                    // Ajouter les labels
                    DrawLabels(g, centerX, centerY, radius, iesData);

                    // Légende
                    DrawLegend(g, iesData);

                    // Sauvegarder
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    bitmap.Save(outputPath, ImageFormat.Png);
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur génération graphique IES : {ex.Message}");
                return null;
            }
        }

        private static void DrawConcentricCircles(Graphics g, int centerX, int centerY, int maxRadius, double maxValue)
        {
            using (Pen gridPen = new Pen(Color.FromArgb(200, 200, 200), 1))
            using (Pen borderPen = new Pen(Color.Black, 2))
            using (Font font = new Font("Arial", 8))
            using (Brush textBrush = new SolidBrush(Color.Gray))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int r = maxRadius * i / 4;
                    g.DrawEllipse(i == 4 ? borderPen : gridPen,
                        centerX - r, centerY - r, r * 2, r * 2);

                    // Labels des valeurs
                    if (i > 0)
                    {
                        double value = maxValue * i / 4;
                        string label = $"{value:F0} cd";
                        SizeF size = g.MeasureString(label, font);
                        g.DrawString(label, font, textBrush,
                            centerX + r - size.Width / 2, centerY - 15);
                    }
                }
            }
        }

        private static void DrawAxes(Graphics g, int centerX, int centerY, int radius)
        {
            using (Pen axisPen = new Pen(Color.Gray, 1))
            using (Font font = new Font("Arial", 9, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // Axe vertical (0° - 180°)
                g.DrawLine(axisPen, centerX, centerY - radius, centerX, centerY + radius);

                // Axe horizontal (90° - 270°)
                g.DrawLine(axisPen, centerX - radius, centerY, centerX + radius, centerY);

                // Labels des angles
                g.DrawString("0°", font, textBrush, centerX - 10, centerY - radius - 20);
                g.DrawString("90°", font, textBrush, centerX + radius + 5, centerY - 10);
                g.DrawString("180°", font, textBrush, centerX - 15, centerY + radius + 5);
                g.DrawString("270°", font, textBrush, centerX - radius - 35, centerY - 10);
            }
        }

        private static void DrawPolarCurve(Graphics g, int centerX, int centerY, int maxRadius,
            double[] angles, double[] candelaValues, double maxCandela, Color color, int thickness)
        {
            if (angles.Length == 0 || candelaValues.Length == 0)
                return;

            List<PointF> points = new List<PointF>();

            for (int i = 0; i < Math.Min(angles.Length, candelaValues.Length); i++)
            {
                double angle = angles[i];
                double candela = candelaValues[i];

                // Normaliser la valeur
                double normalizedValue = maxCandela > 0 ? candela / maxCandela : 0;
                double r = maxRadius * normalizedValue;

                // Convertir angle polaire en coordonnées cartésiennes
                // (0° = haut, rotation horaire)
                double radians = (angle - 90) * Math.PI / 180.0;
                float x = centerX + (float)(r * Math.Cos(radians));
                float y = centerY + (float)(r * Math.Sin(radians));

                points.Add(new PointF(x, y));
            }

            // Dessiner la courbe
            if (points.Count > 1)
            {
                using (Pen curvePen = new Pen(color, thickness))
                {
                    curvePen.LineJoin = LineJoin.Round;
                    g.DrawLines(curvePen, points.ToArray());
                }
            }
        }

        private static void DrawLabels(Graphics g, int centerX, int centerY, int radius, IESParser.IESData iesData)
        {
            using (Font titleFont = new Font("Arial", 12, FontStyle.Bold))
            using (Font subFont = new Font("Arial", 9))
            using (Brush textBrush = new SolidBrush(Color.Black))
            {
                // Titre
                string title = "Courbe Photométrique";
                SizeF titleSize = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, textBrush,
                    centerX - titleSize.Width / 2, 10);

                // Fabricant et modèle
                string info = $"{iesData.Manufacturer} - {iesData.CatalogNumber}";
                SizeF infoSize = g.MeasureString(info, subFont);
                g.DrawString(info, subFont, textBrush,
                    centerX - infoSize.Width / 2, 30);
            }
        }

        private static void DrawLegend(Graphics g, IESParser.IESData iesData)
        {
            using (Font font = new Font("Arial", 8))
            using (Brush textBrush = new SolidBrush(Color.Black))
            using (Pen bluePen = new Pen(Color.FromArgb(41, 128, 185), 3))
            using (Pen redPen = new Pen(Color.FromArgb(231, 76, 60), 2))
            {
                int legendX = 20;
                int legendY = 500;

                // Flux et puissance
                g.DrawString($"Flux total: {iesData.TotalLumens:F0} lm", font, textBrush, legendX, legendY);
                g.DrawString($"Puissance: {iesData.InputWatts:F0} W", font, textBrush, legendX, legendY + 15);
                g.DrawString($"Efficacité: {iesData.Efficacy:F1} lm/W", font, textBrush, legendX, legendY + 30);

                // Courbes
                if (iesData.CandelaValues.Count > 0)
                {
                    g.DrawLine(bluePen, legendX, legendY + 50, legendX + 30, legendY + 50);
                    g.DrawString("Plan vertical 0°", font, textBrush, legendX + 35, legendY + 45);
                }

                if (iesData.CandelaValues.Count > 1)
                {
                    g.DrawLine(redPen, legendX, legendY + 70, legendX + 30, legendY + 70);
                    g.DrawString("Plan vertical 90°", font, textBrush, legendX + 35, legendY + 65);
                }
            }
        }

        /// <summary>
        /// Génère un tableau de valeurs de candela
        /// </summary>
        public static string GenerateCandelaTable(IESParser.IESData iesData, string outputPath)
        {
            if (iesData == null)
                return null;

            try
            {
                int width = 800;
                int height = 600;

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);

                    using (Font titleFont = new Font("Arial", 14, FontStyle.Bold))
                    using (Font headerFont = new Font("Arial", 10, FontStyle.Bold))
                    using (Font dataFont = new Font("Arial", 9))
                    using (Brush textBrush = new SolidBrush(Color.Black))
                    using (Pen gridPen = new Pen(Color.LightGray, 1))
                    {
                        // Titre
                        g.DrawString("Distribution d'intensité lumineuse (Candela)", titleFont, textBrush, 20, 20);

                        int startY = 60;
                        int cellHeight = 25;
                        int cellWidth = 80;

                        // En-têtes des colonnes (angles horizontaux)
                        g.DrawString("Angle V°", headerFont, textBrush, 20, startY);

                        int maxCols = Math.Min(8, iesData.HorizontalAngles.Count);
                        for (int h = 0; h < maxCols; h++)
                        {
                            string header = $"{iesData.HorizontalAngles[h]:F0}°";
                            g.DrawString(header, headerFont, textBrush, 120 + h * cellWidth, startY);
                        }

                        // Lignes de données
                        int maxRows = Math.Min(15, iesData.VerticalAngles.Count);
                        for (int v = 0; v < maxRows; v++)
                        {
                            int y = startY + cellHeight + v * cellHeight;

                            // Angle vertical
                            g.DrawString($"{iesData.VerticalAngles[v]:F0}°", dataFont, textBrush, 20, y);

                            // Valeurs de candela
                            for (int h = 0; h < maxCols && h < iesData.CandelaValues.Count; h++)
                            {
                                if (v < iesData.CandelaValues[h].Count)
                                {
                                    double candela = iesData.CandelaValues[h][v];
                                    g.DrawString($"{candela:F0}", dataFont, textBrush, 120 + h * cellWidth, y);
                                }
                            }

                            // Ligne de grille
                            g.DrawLine(gridPen, 10, y + cellHeight - 2, width - 10, y + cellHeight - 2);
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    bitmap.Save(outputPath, ImageFormat.Png);
                    return outputPath;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}