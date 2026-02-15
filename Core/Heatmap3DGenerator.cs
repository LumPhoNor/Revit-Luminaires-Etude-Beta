using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Génère une visualisation 3D isométrique de la distribution d'éclairage
    /// </summary>
    public static class Heatmap3DGenerator
    {
        public static string GenerateHeatmap3D(
            List<GridPoint> gridPoints,
            string roomName,
            double requiredLux,
            string outputPath,
            int imgWidth = 1600,
            int imgHeight = 1200)
        {
            if (gridPoints == null || gridPoints.Count == 0)
                return null;

            try
            {
                // Construire une grille 2D structurée à partir des GridPoints
                var (grid, cols, rows, minX, minY, maxX, maxY) = BuildStructuredGrid(gridPoints);

                // Calculer les paramètres de projection isométrique
                const double isoAngle = Math.PI / 6; // 30 degrés
                double cosA = Math.Cos(isoAngle);
                double sinA = Math.Sin(isoAngle);

                // Calculer les dimensions de la grille en coordonnées Revit
                double gridWidth = maxX - minX;
                double gridHeight = maxY - minY;

                // Normalisation de la hauteur (max ~80px pour la hauteur max)
                double maxLux = gridPoints.Max(p => p.Illuminance);
                double heightScale = maxLux > 0 ? 80.0 / maxLux : 0;
                double maxHeight = maxLux * heightScale;

                // Échelle pour que la grille rentre dans l'image avec marges
                int marginX = 200;
                int marginY = 120;
                int titleSpace = 100;
                int legendSpace = 200; // Espace pour la légende à droite

                int availableWidth = imgWidth - 2 * marginX - legendSpace;
                int availableHeight = imgHeight - 2 * marginY - titleSpace;

                // Calculer l'échelle isométrique
                double projectedWidth = (gridWidth + gridHeight) * cosA;
                double projectedHeight = (gridWidth + gridHeight) * sinA + maxHeight;

                double scaleX = availableWidth / (projectedWidth > 0 ? projectedWidth : 1);
                double scaleY = availableHeight / (projectedHeight > 0 ? projectedHeight : 1);

                // Utiliser l'échelle la plus petite pour respecter les proportions
                double scale = Math.Min(scaleX, scaleY);

                // Calculer la taille réelle du contenu projeté
                double actualWidth = projectedWidth * scale;
                double actualHeight = projectedHeight * scale;

                // Centrer le contenu dans l'espace disponible
                int centerX = marginX + (int)((availableWidth - actualWidth) / 2 + actualWidth / 2);
                int centerY = titleSpace + marginY + (int)((availableHeight - actualHeight) / 2 + actualHeight / 2);

                using (Bitmap bitmap = new Bitmap(imgWidth, imgHeight))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;

                    // Fond dégradé léger
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, imgWidth, imgHeight),
                        Color.White,
                        Color.FromArgb(240, 240, 240),
                        LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(bgBrush, 0, 0, imgWidth, imgHeight);
                    }

                    // Fonction de projection
                    PointF Project(double gx, double gy, double height)
                    {
                        double relX = gx - minX;
                        double relY = gy - minY;
                        float sx = (float)((relX - relY) * cosA * scale) + centerX;
                        float sy = (float)((relX + relY) * sinA * scale) - (float)height + centerY;
                        return new PointF(sx, sy);
                    }

                    // Dessiner le plan de base (sol gris clair)
                    DrawBasePlane(g, Project, gridWidth, gridHeight, minX, minY);

                    // Dessiner les axes
                    DrawAxes(g, Project, gridWidth, gridHeight, minX, minY);

                    // Rendu back-to-front (algorithme du peintre)
                    for (int y = rows - 2; y >= 0; y--)
                    {
                        for (int x = 0; x < cols - 1; x++)
                        {
                            // Obtenir les 4 coins de la cellule
                            double lux00 = grid[x, y];
                            double lux10 = grid[x + 1, y];
                            double lux11 = grid[x + 1, y + 1];
                            double lux01 = grid[x, y + 1];

                            // Si tous les coins ont des données valides
                            if (!double.IsNaN(lux00) && !double.IsNaN(lux10) &&
                                !double.IsNaN(lux11) && !double.IsNaN(lux01))
                            {
                                // Calculer les coordonnées des coins
                                double cellWidth = gridWidth / (cols - 1);
                                double cellHeight = gridHeight / (rows - 1);

                                double gx0 = minX + x * cellWidth;
                                double gx1 = minX + (x + 1) * cellWidth;
                                double gy0 = minY + y * cellHeight;
                                double gy1 = minY + (y + 1) * cellHeight;

                                // Hauteurs normalisées
                                double h00 = lux00 * heightScale;
                                double h10 = lux10 * heightScale;
                                double h11 = lux11 * heightScale;
                                double h01 = lux01 * heightScale;

                                // Projeter les 4 coins
                                PointF p00 = Project(gx0, gy0, h00);
                                PointF p10 = Project(gx1, gy0, h10);
                                PointF p11 = Project(gx1, gy1, h11);
                                PointF p01 = Project(gx0, gy1, h01);

                                // Calculer la couleur moyenne
                                double avgLux = (lux00 + lux10 + lux11 + lux01) / 4.0;
                                Color color = GetColorForLux(avgLux, requiredLux);

                                // Dessiner le quadrilatère rempli
                                PointF[] points = { p00, p10, p11, p01 };
                                using (SolidBrush brush = new SolidBrush(color))
                                {
                                    g.FillPolygon(brush, points);
                                }

                                // Dessiner les bords avec couleur plus foncée semi-transparente
                                Color edgeColor = Color.FromArgb(80, 0, 0, 0);
                                using (Pen pen = new Pen(edgeColor, 1))
                                {
                                    g.DrawPolygon(pen, points);
                                }
                            }
                        }
                    }

                    // Barre de légende verticale (côté droit)
                    DrawVerticalLegend(g, requiredLux, imgWidth, imgHeight);

                    // Titre en haut
                    using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
                    using (SolidBrush textBrush = new SolidBrush(Color.Black))
                    {
                        string title = $"Distribution 3D - {roomName}";
                        SizeF titleSize = g.MeasureString(title, titleFont);
                        g.DrawString(title, titleFont, textBrush,
                            (imgWidth - titleSize.Width) / 2, 20);
                    }

                    // Statistiques
                    using (Font font = new Font("Arial", 10))
                    using (SolidBrush brush = new SolidBrush(Color.DarkGray))
                    {
                        double avg = gridPoints.Average(p => p.Illuminance);
                        double min = gridPoints.Min(p => p.Illuminance);
                        double max = gridPoints.Max(p => p.Illuminance);

                        string stats = $"Moy: {avg:F0} lux | Min: {min:F0} lux | Max: {max:F0} lux";
                        SizeF statsSize = g.MeasureString(stats, font);
                        g.DrawString(stats, font, brush,
                            (imgWidth - statsSize.Width) / 2, 50);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    bitmap.Save(outputPath, ImageFormat.Png);
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur génération heatmap 3D : {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Construit une grille 2D structurée à partir des GridPoints non-ordonnés
        /// </summary>
        private static (double[,] grid, int cols, int rows, double minX, double minY, double maxX, double maxY) BuildStructuredGrid(List<GridPoint> gridPoints)
        {
            // Extraire les valeurs X et Y distinctes, triées
            var distinctX = gridPoints.Select(p => Math.Round(p.X, 3)).Distinct().OrderBy(x => x).ToList();
            var distinctY = gridPoints.Select(p => Math.Round(p.Y, 3)).Distinct().OrderBy(y => y).ToList();

            int cols = distinctX.Count;
            int rows = distinctY.Count;

            double minX = distinctX.First();
            double maxX = distinctX.Last();
            double minY = distinctY.First();
            double maxY = distinctY.Last();

            // Créer le tableau 2D
            double[,] grid = new double[cols, rows];

            // Initialiser avec NaN
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    grid[x, y] = double.NaN;
                }
            }

            // Remplir à partir des GridPoints
            foreach (var point in gridPoints)
            {
                double roundedX = Math.Round(point.X, 3);
                double roundedY = Math.Round(point.Y, 3);

                int xIdx = distinctX.IndexOf(roundedX);
                int yIdx = distinctY.IndexOf(roundedY);

                if (xIdx >= 0 && yIdx >= 0)
                {
                    grid[xIdx, yIdx] = point.Illuminance;
                }
            }

            return (grid, cols, rows, minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Dessine le plan de base (sol gris clair) sous la surface 3D
        /// </summary>
        private static void DrawBasePlane(Graphics g, Func<double, double, double, PointF> project,
            double gridWidth, double gridHeight, double minX, double minY)
        {
            PointF p00 = project(minX, minY, 0);
            PointF p10 = project(minX + gridWidth, minY, 0);
            PointF p11 = project(minX + gridWidth, minY + gridHeight, 0);
            PointF p01 = project(minX, minY + gridHeight, 0);

            PointF[] planePoints = { p00, p10, p11, p01 };

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(100, 200, 200, 200)))
            {
                g.FillPolygon(brush, planePoints);
            }

            using (Pen pen = new Pen(Color.Gray, 2))
            {
                g.DrawPolygon(pen, planePoints);
            }
        }

        /// <summary>
        /// Dessine les axes X et Y en isométrique
        /// </summary>
        private static void DrawAxes(Graphics g, Func<double, double, double, PointF> project,
            double gridWidth, double gridHeight, double minX, double minY)
        {
            PointF origin = project(minX, minY, 0);

            // Axe X
            PointF xEnd = project(minX + gridWidth, minY, 0);
            using (Pen pen = new Pen(Color.FromArgb(150, 255, 0, 0), 2))
            {
                pen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(pen, origin, xEnd);
            }

            using (Font font = new Font("Arial", 9, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.Red))
            {
                g.DrawString("X (m)", font, brush, xEnd.X + 5, xEnd.Y - 10);
            }

            // Axe Y
            PointF yEnd = project(minX, minY + gridHeight, 0);
            using (Pen pen = new Pen(Color.FromArgb(150, 0, 150, 0), 2))
            {
                pen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(pen, origin, yEnd);
            }

            using (Font font = new Font("Arial", 9, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.Green))
            {
                g.DrawString("Y (m)", font, brush, yEnd.X - 35, yEnd.Y - 5);
            }
        }

        /// <summary>
        /// Dessine une barre de légende verticale (côté droit)
        /// </summary>
        private static void DrawVerticalLegend(Graphics g, double requiredLux, int imgWidth, int imgHeight)
        {
            int legendX = imgWidth - 180;
            int legendY = 150;
            int barWidth = 25;
            int barHeight = 250;

            // Fond blanc semi-transparent
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                g.FillRectangle(bgBrush, legendX - 10, legendY - 40, 160, barHeight + 50);
            }

            // Titre de la légende
            using (Font titleFont = new Font("Arial", 10, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString("Éclairement", titleFont, textBrush, legendX, legendY - 30);
                g.DrawString("(lux)", titleFont, textBrush, legendX, legendY - 15);
            }

            // Dessiner la barre de gradient verticale
            for (int i = 0; i < barHeight; i++)
            {
                // Ratio inversé : i=0 → haut (200%), i=barHeight → bas (0%)
                double ratio = 2.0 * (1.0 - (double)i / barHeight);
                double luxValue = ratio * requiredLux;

                Color color = GetColorForLux(luxValue, requiredLux);

                using (Pen pen = new Pen(color, 1))
                {
                    g.DrawLine(pen, legendX, legendY + i, legendX + barWidth, legendY + i);
                }
            }

            // Bordure autour de la barre
            using (Pen borderPen = new Pen(Color.Black, 2))
            {
                g.DrawRectangle(borderPen, legendX, legendY, barWidth, barHeight);
            }

            // Étiquettes et marques de graduation
            using (Font font = new Font("Arial", 8))
            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            using (Pen tickPen = new Pen(Color.Black, 2))
            {
                var gradations = new[]
                {
                    (2.0, "200%", $"{requiredLux * 2:F0}"),
                    (1.5, "150%", $"{requiredLux * 1.5:F0}"),
                    (1.2, "120%", $"{requiredLux * 1.2:F0}"),
                    (1.0, "100%", $"{requiredLux:F0}"),
                    (0.8, "80%", $"{requiredLux * 0.8:F0}"),
                    (0.5, "50%", $"{requiredLux * 0.5:F0}"),
                    (0.0, "0%", "0")
                };

                foreach (var (ratio, percent, luxLabel) in gradations)
                {
                    // Position sur la barre (inversée)
                    int y = legendY + (int)((1.0 - ratio / 2.0) * barHeight);

                    // Marque de graduation
                    g.DrawLine(tickPen, legendX + barWidth, y, legendX + barWidth + 5, y);

                    // Texte avec pourcentage et valeur lux
                    string label = $"{percent} ({luxLabel})";
                    SizeF textSize = g.MeasureString(label, font);
                    g.DrawString(label, font, textBrush, legendX + barWidth + 8, y - textSize.Height / 2);

                    // Ligne de référence pour 100% (requis)
                    if (Math.Abs(ratio - 1.0) < 0.01)
                    {
                        using (Pen refPen = new Pen(Color.Black, 1))
                        {
                            refPen.DashStyle = DashStyle.Dash;
                            g.DrawLine(refPen, legendX - 10, y, legendX, y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calcule une couleur avec dégradé continu selon le ratio lux / requiredLux
        /// Identique au schéma de GridMapGenerator
        /// </summary>
        private static Color GetColorForLux(double lux, double requiredLux)
        {
            double ratio = Math.Max(0, Math.Min(2.0, lux / requiredLux));
            int r, g, b;

            if (ratio < 0.5)
            {
                // Rouge foncé → Rouge
                double t = ratio / 0.5;
                r = (int)(139 + t * 116);  // 139→255
                g = (int)(t * 69);         // 0→69
                b = 0;
            }
            else if (ratio < 0.8)
            {
                // Rouge → Orange
                double t = (ratio - 0.5) / 0.3;
                r = 255;
                g = (int)(69 + t * 131);   // 69→200
                b = 0;
            }
            else if (ratio < 1.0)
            {
                // Orange → Jaune-vert
                double t = (ratio - 0.8) / 0.2;
                r = (int)(255 - t * 105);  // 255→150
                g = (int)(200 + t * 55);   // 200→255
                b = 0;
            }
            else if (ratio < 1.3)
            {
                // Jaune-vert → Vert
                double t = (ratio - 1.0) / 0.3;
                r = (int)(150 - t * 110);  // 150→40
                g = (int)(255 - t * 55);   // 255→200
                b = (int)(t * 40);         // 0→40
            }
            else
            {
                // Vert → Vert foncé
                double t = Math.Min(1.0, (ratio - 1.3) / 0.7);
                r = (int)(40 - t * 20);    // 40→20
                g = (int)(200 - t * 50);   // 200→150
                b = (int)(40 + t * 20);    // 40→60
            }

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b)));
        }
    }
}
