using System;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Template de mise en forme pour les rapports PDF
    /// </summary>
    public static class ReportTemplate
    {
        // Couleurs du thème
        public static readonly BaseColor ColorPrimary = new BaseColor(41, 128, 185);    // Bleu
        public static readonly BaseColor ColorSecondary = new BaseColor(52, 73, 94);     // Gris foncé
        public static readonly BaseColor ColorSuccess = new BaseColor(39, 174, 96);      // Vert
        public static readonly BaseColor ColorDanger = new BaseColor(231, 76, 60);       // Rouge
        public static readonly BaseColor ColorWarning = new BaseColor(243, 156, 18);     // Orange
        public static readonly BaseColor ColorLight = new BaseColor(236, 240, 241);      // Gris clair

        // Polices standards (pas besoin de fichiers externes)
        private static Font _titleFont;
        private static Font _headerFont;
        private static Font _subHeaderFont;
        private static Font _normalFont;
        private static Font _boldFont;
        private static Font _smallFont;

        static ReportTemplate()
        {
            InitializeFonts();
        }

        private static void InitializeFonts()
        {
            try
            {
                // Utilisation des polices standards intégrées (pas besoin de fichiers .ttf)
                _titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, Font.BOLD, ColorPrimary);
                _headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, Font.BOLD, ColorSecondary);
                _subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, Font.BOLD, ColorSecondary);
                _normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL, BaseColor.BLACK);
                _boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Font.BOLD, BaseColor.BLACK);
                _smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, Font.NORMAL, BaseColor.DARK_GRAY);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'initialisation des polices : {ex.Message}", ex);
            }
        }

        // Propriétés publiques pour accéder aux polices
        public static Font TitleFont => _titleFont;
        public static Font HeaderFont => _headerFont;
        public static Font SubHeaderFont => _subHeaderFont;
        public static Font NormalFont => _normalFont;
        public static Font BoldFont => _boldFont;
        public static Font SmallFont => _smallFont;

        /// <summary>
        /// Crée un en-tête de page
        /// </summary>
        public static Paragraph CreatePageHeader(string text)
        {
            Paragraph header = new Paragraph(text, HeaderFont);
            header.SpacingBefore = 10;
            header.SpacingAfter = 15;
            return header;
        }

        /// <summary>
        /// Crée un sous-titre de section
        /// </summary>
        public static Paragraph CreateSection(string text)
        {
            Paragraph section = new Paragraph(text, SubHeaderFont);
            section.SpacingBefore = 15;
            section.SpacingAfter = 10;
            return section;
        }

        /// <summary>
        /// Crée un sous-titre de section avec description
        /// </summary>
        public static IElement CreateSection(string title, string description)
        {
            // Créer un paragraphe combiné
            Paragraph combined = new Paragraph();
            combined.Add(new Chunk(title + "\n", SubHeaderFont));
            combined.Add(new Chunk(description, NormalFont));
            combined.SpacingBefore = 15;
            combined.SpacingAfter = 10;
            return combined;
        }

        /// <summary>
        /// Crée un sous-titre de section avec espacement personnalisé
        /// </summary>
        public static Paragraph CreateSection(string text, float spacingAfter)
        {
            Paragraph section = new Paragraph(text, SubHeaderFont);
            section.SpacingBefore = 15;
            section.SpacingAfter = spacingAfter;
            return section;
        }

        /// <summary>
        /// Crée un tableau stylisé avec colonnes
        /// </summary>
        public static PdfPTable CreateStyledTable(int numColumns)
        {
            PdfPTable table = new PdfPTable(numColumns);
            table.WidthPercentage = 100;
            table.SpacingBefore = 10;
            table.SpacingAfter = 15;
            return table;
        }

        /// <summary>
        /// Crée un tableau stylisé avec colonnes et largeur de page personnalisée
        /// </summary>
        public static PdfPTable CreateStyledTable(int numColumns, float widthPercentage)
        {
            PdfPTable table = new PdfPTable(numColumns);
            table.WidthPercentage = widthPercentage;
            table.SpacingBefore = 10;
            table.SpacingAfter = 15;
            return table;
        }

        /// <summary>
        /// Crée un tableau stylisé avec nombre de colonnes et largeurs relatives
        /// </summary>
        public static PdfPTable CreateStyledTable(int numColumns, float[] columnWidths)
        {
            PdfPTable table = new PdfPTable(numColumns);
            table.WidthPercentage = 100;
            table.SpacingBefore = 10;
            table.SpacingAfter = 15;
            if (columnWidths != null && columnWidths.Length == numColumns)
            {
                table.SetWidths(columnWidths);
            }
            return table;
        }

        /// <summary>
        /// Crée un tableau stylisé avec largeurs de colonnes personnalisées
        /// </summary>
        public static PdfPTable CreateStyledTable(float[] columnWidths)
        {
            PdfPTable table = new PdfPTable(columnWidths.Length);
            table.WidthPercentage = 100;
            table.SpacingBefore = 10;
            table.SpacingAfter = 15;
            table.SetWidths(columnWidths);
            return table;
        }

        /// <summary>
        /// Crée une cellule de statut (conforme/non conforme)
        /// </summary>
        public static PdfPCell CreateStatusCell(bool isCompliant)
        {
            string text = isCompliant ? "✓ Conforme" : "✗ Non conforme";
            BaseColor color = isCompliant ? ColorSuccess : ColorDanger;

            Font statusFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Font.BOLD, color);
            PdfPCell cell = new PdfPCell(new Phrase(text, statusFont));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            cell.BackgroundColor = isCompliant ? new BaseColor(220, 255, 220) : new BaseColor(255, 220, 220);

            return cell;
        }

        /// <summary>
        /// Crée une cellule d'en-tête de tableau
        /// </summary>
        public static PdfPCell CreateTableHeaderCell(string text)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, BoldFont));
            cell.BackgroundColor = ColorPrimary;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 8;
            cell.Border = Rectangle.NO_BORDER;

            // Créer une police blanche pour l'en-tête
            Font whiteFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Font.BOLD, BaseColor.WHITE);
            cell.Phrase = new Phrase(text, whiteFont);

            return cell;
        }

        /// <summary>
        /// Crée une cellule de tableau normale
        /// </summary>
        public static PdfPCell CreateTableCell(string text, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, NormalFont));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5;
            return cell;
        }

        /// <summary>
        /// Crée une cellule de tableau avec couleur de fond
        /// </summary>
        public static PdfPCell CreateColoredCell(string text, BaseColor backgroundColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, BoldFont));
            cell.BackgroundColor = backgroundColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            return cell;
        }

        /// <summary>
        /// Crée une ligne de séparation
        /// </summary>
        public static Chunk CreateSeparatorLine()
        {
            return new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, ColorPrimary, Element.ALIGN_CENTER, -5));
        }
    }
}