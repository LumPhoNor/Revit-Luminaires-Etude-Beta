using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingPoint = System.Drawing.Point;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Générateur de graphiques pour les rapports
    /// </summary>
    public class ChartGenerator
    {
        /// <summary>
        /// Génère un graphique en barres comparant LED vs Halogène
        /// </summary>
        public static byte[] GenerateEnergyComparisonChart(double ledPower, double halogenPower, int width = 600, int height = 400)
        {
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Marges
                int marginLeft = 80;
                int marginRight = 40;
                int marginTop = 60;
                int marginBottom = 80;

                int chartWidth = width - marginLeft - marginRight;
                int chartHeight = height - marginTop - marginBottom;

                // Titre
                using (Font titleFont = new Font(FontFamily.GenericSansSerif, 14, DrawingFontStyle.Bold))
                using (Brush titleBrush = new SolidBrush(Color.FromArgb(33, 37, 41)))
                {
                    string title = "Comparaison énergétique : LED vs Halogène";
                    SizeF titleSize = g.MeasureString(title, titleFont);
                    g.DrawString(title, titleFont, titleBrush, (width - titleSize.Width) / 2, 20);
                }

                // Trouver la valeur maximale pour l'échelle
                double maxValue = Math.Max(ledPower, halogenPower);
                double scale = chartHeight / (maxValue * 1.2);

                // Largeur des barres
                int barWidth = 120;
                int spacing = 100;

                // Position des barres
                int ledX = marginLeft + spacing;
                int halogenX = ledX + barWidth + spacing;

                // Hauteur des barres
                int ledHeight = (int)(ledPower * scale);
                int halogenHeight = (int)(halogenPower * scale);

                // Position Y de base
                int baseY = marginTop + chartHeight;

                // Dessiner les barres
                using (Brush ledBrush = new SolidBrush(Color.FromArgb(40, 167, 69))) // Vert
                using (Brush halogenBrush = new SolidBrush(Color.FromArgb(220, 53, 69))) // Rouge
                using (Pen borderPen = new Pen(Color.Black, 2))
                {
                    // Barre LED
                    Rectangle ledRect = new Rectangle(ledX, baseY - ledHeight, barWidth, ledHeight);
                    g.FillRectangle(ledBrush, ledRect);
                    g.DrawRectangle(borderPen, ledRect);

                    // Barre Halogène
                    Rectangle halogenRect = new Rectangle(halogenX, baseY - halogenHeight, barWidth, halogenHeight);
                    g.FillRectangle(halogenBrush, halogenRect);
                    g.DrawRectangle(borderPen, halogenRect);
                }

                // Valeurs au-dessus des barres
                using (Font valueFont = new Font(FontFamily.GenericSansSerif, 12, DrawingFontStyle.Bold))
                using (Brush valueBrush = new SolidBrush(Color.Black))
                {
                    string ledValue = $"{ledPower:F0} W";
                    string halogenValue = $"{halogenPower:F0} W";

                    SizeF ledSize = g.MeasureString(ledValue, valueFont);
                    SizeF halogenSize = g.MeasureString(halogenValue, valueFont);

                    g.DrawString(ledValue, valueFont, valueBrush,
                        ledX + (barWidth - ledSize.Width) / 2, baseY - ledHeight - 25);
                    g.DrawString(halogenValue, valueFont, valueBrush,
                        halogenX + (barWidth - halogenSize.Width) / 2, baseY - halogenHeight - 25);
                }

                // Labels sous les barres
                using (Font labelFont = new Font(FontFamily.GenericSansSerif, 11, DrawingFontStyle.Regular))
                using (Brush labelBrush = new SolidBrush(Color.Black))
                {
                    string ledLabel = "LED";
                    string halogenLabel = "Halogène";

                    SizeF ledLabelSize = g.MeasureString(ledLabel, labelFont);
                    SizeF halogenLabelSize = g.MeasureString(halogenLabel, labelFont);

                    g.DrawString(ledLabel, labelFont, labelBrush,
                        ledX + (barWidth - ledLabelSize.Width) / 2, baseY + 10);
                    g.DrawString(halogenLabel, labelFont, labelBrush,
                        halogenX + (barWidth - halogenLabelSize.Width) / 2, baseY + 10);
                }

                // Économie en %
                double savings = ((halogenPower - ledPower) / halogenPower) * 100;
                using (Font savingsFont = new Font(FontFamily.GenericSansSerif, 10, DrawingFontStyle.Italic))
                using (Brush savingsBrush = new SolidBrush(Color.FromArgb(40, 167, 69)))
                {
                    string savingsText = $"Économie : {savings:F0}%";
                    SizeF savingsSize = g.MeasureString(savingsText, savingsFont);
                    g.DrawString(savingsText, savingsFont, savingsBrush,
                        (width - savingsSize.Width) / 2, baseY + 40);
                }

                // Convertir en byte array
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Génère un indicateur de conformité visuel
        /// </summary>
        public static byte[] GenerateComplianceIndicator(bool isCompliant, int size = 200)
        {
            using (Bitmap bitmap = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Cercle de fond
                Color circleColor = isCompliant ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69);
                using (Brush circleBrush = new SolidBrush(circleColor))
                {
                    g.FillEllipse(circleBrush, 20, 20, size - 40, size - 40);
                }

                // Icône
                using (Pen iconPen = new Pen(Color.White, 15))
                {
                    iconPen.StartCap = LineCap.Round;
                    iconPen.EndCap = LineCap.Round;

                    if (isCompliant)
                    {
                        // Checkmark
                        DrawingPoint[] checkPoints = new DrawingPoint[]
                        {
                            new DrawingPoint(size / 3, size / 2),
                            new DrawingPoint(size / 2 - 10, size * 2 / 3),
                            new DrawingPoint(size * 2 / 3 + 10, size / 3)
                        };
                        g.DrawLines(iconPen, checkPoints);
                    }
                    else
                    {
                        // X
                        g.DrawLine(iconPen, size / 3, size / 3, size * 2 / 3, size * 2 / 3);
                        g.DrawLine(iconPen, size * 2 / 3, size / 3, size / 3, size * 2 / 3);
                    }
                }

                // Convertir en byte array
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }
}