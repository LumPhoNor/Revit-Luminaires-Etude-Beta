using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Génère une carte 2D de la grille d'éclairage avec heatmap rectangulaire
    /// </summary>
    public class GridMapGenerator
    {
        public static string GenerateGridMap(Room room, List<GridPoint> gridPoints, double requiredLux, string outputPath, double gridSpacingMeters = 1.0)
        {
            if (gridPoints == null || gridPoints.Count == 0)
                return null;

            try
            {
                // Calculer les bornes
                double minX = gridPoints.Min(p => p.X);
                double maxX = gridPoints.Max(p => p.X);
                double minY = gridPoints.Min(p => p.Y);
                double maxY = gridPoints.Max(p => p.Y);

                double rangeX = maxX - minX;
                double rangeY = maxY - minY;

                if (rangeX == 0) rangeX = 1;
                if (rangeY == 0) rangeY = 1;

                // Compter le nombre de points en X et Y pour dimensionner l'image
                var uniqueX = gridPoints.Select(p => Math.Round(p.X, 3)).Distinct().Count();
                var uniqueY = gridPoints.Select(p => Math.Round(p.Y, 3)).Distinct().Count();

                int pointsX = Math.Max(2, uniqueX);
                int pointsY = Math.Max(2, uniqueY);

                // Calculer le ratio réel de la pièce
                double roomRatio = rangeX / rangeY;

                // Dimensions de base
                int margin = 60;
                int legendWidth = 220;
                int titleHeight = 80; // Espace pour titre et stats

                // Dimensions maximales disponibles
                int maxDrawWidth = 2400 - legendWidth - 2 * margin;
                int maxDrawHeight = 1800 - titleHeight - 2 * margin;

                // Calculer les dimensions en respectant le ratio de la pièce
                int gridDrawWidth, gridDrawHeight;

                if (roomRatio > (double)maxDrawWidth / maxDrawHeight)
                {
                    // Limité par la largeur
                    gridDrawWidth = maxDrawWidth;
                    gridDrawHeight = (int)(gridDrawWidth / roomRatio);
                }
                else
                {
                    // Limité par la hauteur
                    gridDrawHeight = maxDrawHeight;
                    gridDrawWidth = (int)(gridDrawHeight * roomRatio);
                }

                // Dimensions minimales
                gridDrawWidth = Math.Max(400, gridDrawWidth);
                gridDrawHeight = Math.Max(300, gridDrawHeight);

                // Calculer dimensions totales de l'image
                int width = gridDrawWidth + legendWidth + 2 * margin;
                int height = gridDrawHeight + titleHeight + 2 * margin;

                // Zone de dessin réelle (sans légende)
                int drawWidth = width - legendWidth;

                using (Bitmap bitmap = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.Clear(System.Drawing.Color.White);

                    // Calculer la zone de dessin de la grille (utiliser les dimensions calculées)
                    int gridLeft = margin;
                    int gridRight = gridLeft + gridDrawWidth;
                    int gridTop = margin + titleHeight;
                    int gridBottom = gridTop + gridDrawHeight;

                    // Dessiner le contour de la pièce
                    DrawRoomOutline(g, gridLeft, gridRight, gridTop, gridBottom);

                    // Calculer la taille des cellules en pixels
                    double cellWidth = (double)gridDrawWidth / pointsX;
                    double cellHeight = (double)gridDrawHeight / pointsY;

                    // Taille auto du texte selon l'espace disponible
                    float fontSize = Math.Max(6f, Math.Min(10f, (float)(Math.Min(cellWidth, cellHeight) / 5)));

                    // Dessiner la grille avec rectangles colorés (heatmap)
                    foreach (var point in gridPoints)
                    {
                        // Convertir coordonnées Revit → Image
                        double normalizedX = (point.X - minX) / rangeX;
                        double normalizedY = (point.Y - minY) / rangeY;

                        int x = gridLeft + (int)(normalizedX * gridDrawWidth);
                        int y = gridBottom - (int)(normalizedY * gridDrawHeight);

                        // Couleur selon niveau de lux avec dégradé continu
                        System.Drawing.Color color = GetColorForLux(point.Illuminance, requiredLux);

                        // Calculer la taille du rectangle (85% de la cellule)
                        int rectWidth = (int)(cellWidth * 0.85);
                        int rectHeight = (int)(cellHeight * 0.85);

                        // Centrer le rectangle sur le point
                        int rectX = x - rectWidth / 2;
                        int rectY = y - rectHeight / 2;

                        // Dessiner le rectangle rempli
                        using (SolidBrush brush = new SolidBrush(color))
                        {
                            g.FillRectangle(brush, rectX, rectY, rectWidth, rectHeight);
                        }

                        // Bordure fine pour délimiter les cellules
                        using (Pen pen = new Pen(System.Drawing.Color.FromArgb(100, 0, 0, 0), 1))
                        {
                            g.DrawRectangle(pen, rectX, rectY, rectWidth, rectHeight);
                        }

                        // Afficher TOUTES les valeurs de lux avec fond semi-transparent
                        using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
                        using (SolidBrush textBrush = new SolidBrush(System.Drawing.Color.Black))
                        using (SolidBrush bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 255, 255, 255)))
                        {
                            string luxText = $"{point.Illuminance:F0}";
                            SizeF textSize = g.MeasureString(luxText, font);

                            float textX = x - textSize.Width / 2;
                            float textY = y - textSize.Height / 2;

                            // Fond blanc semi-transparent derrière le texte
                            g.FillRectangle(bgBrush, textX - 2, textY - 1, textSize.Width + 4, textSize.Height + 2);

                            // Texte
                            g.DrawString(luxText, font, textBrush, textX, textY);
                        }
                    }

                    // Dessiner les luminaires PAR-DESSUS la grille (après les valeurs de lux)
                    if (room != null)
                    {
                        DrawLuminaires(g, room, gridLeft, gridRight, gridTop, gridBottom, minX, maxX, minY, maxY, gridDrawWidth, gridDrawHeight);
                    }

                    // Légende avec barre de gradient
                    DrawGradientLegend(g, requiredLux, drawWidth, height);

                    // Titre
                    string roomName = room?.Name ?? "Pièce";
                    using (Font titleFont = new Font("Arial", 14, FontStyle.Bold))
                    using (SolidBrush brush = new SolidBrush(System.Drawing.Color.Black))
                    {
                        g.DrawString($"Grille d'éclairage - {roomName}", titleFont, brush, margin, 15);
                    }

                    // Statistiques améliorées
                    using (Font font = new Font("Arial", 9))
                    using (SolidBrush brush = new SolidBrush(System.Drawing.Color.Black))
                    {
                        double avg = gridPoints.Average(p => p.Illuminance);
                        double min = gridPoints.Min(p => p.Illuminance);
                        double max = gridPoints.Max(p => p.Illuminance);

                        g.DrawString($"Moy: {avg:F0} lux | Min: {min:F0} lux | Max: {max:F0} lux | Grille: {gridSpacingMeters:F2} m",
                            font, brush, margin, 38);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    bitmap.Save(outputPath, ImageFormat.Png);
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur génération carte : {ex.Message}");
                return null;
            }
        }

        private static void DrawRoomOutline(Graphics g, int left, int right, int top, int bottom)
        {
            using (Pen pen = new Pen(System.Drawing.Color.Gray, 2))
            {
                pen.DashStyle = DashStyle.Dash;
                g.DrawRectangle(pen, left, top, right - left, bottom - top);
            }
        }

        /// <summary>
        /// Calcule une couleur avec dégradé continu selon le ratio lux / requiredLux
        /// </summary>
        private static System.Drawing.Color GetColorForLux(double lux, double requiredLux)
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

            return System.Drawing.Color.FromArgb(
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b)));
        }

        /// <summary>
        /// Dessine une légende avec barre de gradient verticale
        /// </summary>
        private static void DrawGradientLegend(Graphics g, double requiredLux, int drawWidth, int height)
        {
            int legendX = drawWidth + 20;
            int legendY = 100;
            int barWidth = 25;
            int barHeight = 200;

            // Titre de la légende
            using (Font titleFont = new Font("Arial", 10, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(System.Drawing.Color.Black))
            {
                g.DrawString("Éclairement", titleFont, textBrush, legendX, legendY - 25);
                g.DrawString("(lux)", titleFont, textBrush, legendX, legendY - 10);
            }

            // Dessiner la barre de gradient verticale
            // Du bas (0 lux, rouge foncé) au haut (2x requis, vert foncé)
            for (int i = 0; i < barHeight; i++)
            {
                // Ratio inversé : i=0 → haut (200%), i=barHeight → bas (0%)
                double ratio = 2.0 * (1.0 - (double)i / barHeight);
                double luxValue = ratio * requiredLux;

                System.Drawing.Color color = GetColorForLux(luxValue, requiredLux);

                using (Pen pen = new Pen(color, 1))
                {
                    g.DrawLine(pen, legendX, legendY + i, legendX + barWidth, legendY + i);
                }
            }

            // Bordure autour de la barre
            using (Pen borderPen = new Pen(System.Drawing.Color.Black, 2))
            {
                g.DrawRectangle(borderPen, legendX, legendY, barWidth, barHeight);
            }

            // Étiquettes et marques de graduation
            using (Font font = new Font("Arial", 8))
            using (SolidBrush textBrush = new SolidBrush(System.Drawing.Color.Black))
            using (Pen tickPen = new Pen(System.Drawing.Color.Black, 2))
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
                        using (Pen refPen = new Pen(System.Drawing.Color.Black, 1))
                        {
                            refPen.DashStyle = DashStyle.Dash;
                            g.DrawLine(refPen, legendX - 10, y, legendX, y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dessine les luminaires sur la grille 2D
        /// </summary>
        private static void DrawLuminaires(Graphics g, Room room, int gridLeft, int gridRight, int gridTop, int gridBottom,
            double minX, double maxX, double minY, double maxY, int gridDrawWidth, int gridDrawHeight)
        {
            try
            {
                // Récupérer tous les luminaires de la pièce
                var doc = room.Document;
                var collector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_LightingFixtures)
                    .WhereElementIsNotElementType();

                // Récupérer le solide de la pièce pour tester l'intersection
                var roomSolid = GetRoomSolid(room);
                if (roomSolid == null) return;

                double rangeX = maxX - minX;
                double rangeY = maxY - minY;
                if (rangeX == 0) rangeX = 1;
                if (rangeY == 0) rangeY = 1;

                foreach (Element elem in collector)
                {
                    var familyInstance = elem as FamilyInstance;
                    if (familyInstance == null) continue;

                    // Vérifier si le luminaire est dans la pièce
                    LocationPoint loc = familyInstance.Location as LocationPoint;
                    if (loc == null) continue;

                    XYZ position = loc.Point;

                    // Vérifier si le point est dans le solide de la pièce
                    if (!IsPointInSolid(position, roomSolid)) continue;

                    // Convertir les coordonnées Revit → coordonnées image
                    double normalizedX = (position.X - minX) / rangeX;
                    double normalizedY = (position.Y - minY) / rangeY;

                    int x = gridLeft + (int)(normalizedX * gridDrawWidth);
                    int y = gridBottom - (int)(normalizedY * gridDrawHeight);

                    // Dessiner le marqueur du luminaire
                    DrawLuminaireMarker(g, x, y);
                }
            }
            catch
            {
                // Ignorer les erreurs de dessin des luminaires
            }
        }

        /// <summary>
        /// Dessine un marqueur pour un luminaire
        /// </summary>
        private static void DrawLuminaireMarker(Graphics g, int x, int y)
        {
            int markerSize = 12;
            int halfSize = markerSize / 2;

            // Cercle extérieur noir
            using (Pen blackPen = new Pen(System.Drawing.Color.Black, 2))
            {
                g.DrawEllipse(blackPen, x - halfSize, y - halfSize, markerSize, markerSize);
            }

            // Remplissage jaune/orange pour symboliser une lumière
            using (SolidBrush fillBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 255, 215, 0)))
            {
                g.FillEllipse(fillBrush, x - halfSize + 2, y - halfSize + 2, markerSize - 4, markerSize - 4);
            }

            // Croix au centre
            using (Pen crossPen = new Pen(System.Drawing.Color.Black, 1))
            {
                g.DrawLine(crossPen, x - 4, y, x + 4, y);
                g.DrawLine(crossPen, x, y - 4, x, y + 4);
            }
        }

        /// <summary>
        /// Récupère le solide de la pièce pour tester l'intersection
        /// </summary>
        private static Solid GetRoomSolid(Room room)
        {
            try
            {
                var options = new SpatialElementBoundaryOptions();
                var calculator = new SpatialElementGeometryCalculator(room.Document, options);
                var results = calculator.CalculateSpatialElementGeometry(room);
                return results?.GetGeometry();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Vérifie si un point est à l'intérieur d'un solide
        /// </summary>
        private static bool IsPointInSolid(XYZ point, Solid solid)
        {
            if (solid == null) return false;

            try
            {
                // Test simple : vérifier si le point est à l'intérieur de la bounding box
                var bbox = solid.GetBoundingBox();
                if (bbox == null) return false;

                return point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                       point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y &&
                       point.Z >= bbox.Min.Z && point.Z <= bbox.Max.Z;
            }
            catch
            {
                return false;
            }
        }
    }
}