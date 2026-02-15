using System;
using iTextSharp.text;
using iTextSharp.text.pdf;

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

        public PDFPageEventHelper(string projectName, string reportDate)
        {
            this.projectName = projectName;
            this.reportDate = reportDate;
            BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            footerFont = new Font(bf, 8, Font.NORMAL, BaseColor.GRAY);
        }

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);

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
    }
}