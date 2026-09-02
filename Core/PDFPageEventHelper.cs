using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using RevitLightingPlugin.UI;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Gestion des en-têtes et pieds de page du PDF
    /// </summary>
    public class PDFPageEventHelper : PdfPageEventHelper
    {
        private string projectName;
        private string reportDate;
        private Font footerFont;
        private BaseFont headerBaseFont;
        private static readonly BaseColor HeaderBlue = new BaseColor(29, 78, 216);  // #1D4ED8 (Skylightning — "lightning")
        private static readonly BaseColor HeaderInk  = new BaseColor(17, 24, 39);   // #111827 (Skylightning — "Sky", identique au site)

        public PDFPageEventHelper(string projectName, string reportDate)
        {
            this.projectName = projectName;
            this.reportDate = reportDate;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            footerFont = new Font(bf, 8, Font.NORMAL, BaseColor.GRAY);

            // Même police que le logo texte du site vitrine (Playfair Display, graisse
            // Black/900), embarquée dans le plugin. Repli sur Helvetica Bold si le
            // fichier de police est absent pour une raison quelconque.
            try
            {
                headerBaseFont = BaseFont.CreateFont(
                    SkylightningTheme.PlayfairDisplayBlackPath, BaseFont.CP1252, BaseFont.EMBEDDED);
            }
            catch
            {
                headerBaseFont = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            }
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

            DrawHeader(writer, document);

            PdfPTable footer = new PdfPTable(3) { TotalWidth = document.PageSize.Width - 80 };
            footer.SetWidths(new float[] { 1f, 1f, 1f });

            // Gauche : Nom projet
            PdfPCell leftCell = new PdfPCell(new Phrase(projectName, footerFont))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            footer.AddCell(leftCell);

            // Centre : Date
            PdfPCell centerCell = new PdfPCell(new Phrase(reportDate, footerFont))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            footer.AddCell(centerCell);

            // Droite : Page X/Y
            PdfPCell rightCell = new PdfPCell(new Phrase($"Page {writer.PageNumber}", footerFont))
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };
            footer.AddCell(rightCell);

            footer.WriteSelectedRows(0, -1, 40, 30, writer.DirectContent);
        }

        /// <summary>
        /// Dessine le logo Skylightning à gauche et le texte "Skylightning"
        /// (Sky en noir, lightning en bleu, police Playfair Display Black —
        /// identique au site vitrine) en en-tête de chaque page.
        /// </summary>
        private void DrawHeader(PdfWriter writer, Document document)
        {
            PdfContentByte cb = writer.DirectContent;
            float pageHeight = document.PageSize.Height;
            float marginLeft = document.LeftMargin;
            float topGap = 16f;
            float logoHeight = 40f;
            float logoWidth = logoHeight;
            float logoBottomY = pageHeight - topGap - logoHeight;

            try
            {
                if (File.Exists(SkylightningTheme.LogoRibbonIconPath))
                {
                    Image logo = Image.GetInstance(SkylightningTheme.LogoRibbonIconPath);
                    logoWidth = logo.Width * (logoHeight / logo.Height);
                    logo.ScaleToFit(logoWidth, logoHeight);
                    logo.SetAbsolutePosition(marginLeft, logoBottomY);
                    cb.AddImage(logo);
                }
            }
            catch { /* logo optionnel : le rapport reste valide sans lui */ }

            float fontSize = 24f;
            float textX = marginLeft + logoWidth + 10;
            float textY = logoBottomY + (logoHeight - fontSize) / 2f + 4f;

            cb.SaveState();
            cb.BeginText();
            cb.SetFontAndSize(headerBaseFont, fontSize);
            cb.SetColorFill(HeaderInk);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "Sky", textX, textY, 0);
            cb.EndText();

            float skyWidth = headerBaseFont.GetWidthPoint("Sky", fontSize);

            cb.BeginText();
            cb.SetFontAndSize(headerBaseFont, fontSize);
            cb.SetColorFill(HeaderBlue);
            cb.ShowTextAligned(Element.ALIGN_LEFT, "lightning", textX + skyWidth, textY, 0);
            cb.EndText();
            cb.RestoreState();
        }
    }
}