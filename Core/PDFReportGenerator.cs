using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using iTextSharp.text;
using iTextSharp.text.pdf;
using RevitLightingPlugin.Models;
using PdfFont = iTextSharp.text.Font;
using PdfElement = iTextSharp.text.Element;
using PdfRectangle = iTextSharp.text.Rectangle;

namespace RevitLightingPlugin.Core
{
    public class PDFReportGenerator
    {
        private iTextSharp.text.Document pdfDocument;
        private PdfWriter writer;
        private PdfFont titleFont, headerFont, normalFont, boldFont, italicFont;
        private UIDocument uidoc;
        private string tempImageFolder;
        private BaseColor primaryColor = new BaseColor(41, 128, 185);
        private BaseColor successColor = new BaseColor(39, 174, 96);
        private BaseColor dangerColor = new BaseColor(231, 76, 60);
        private BaseColor darkGray = new BaseColor(52, 73, 94);

        // Pour le footer
        private string projectName;
        private string reportDate;

        // Pour la table des matières dynamique
        private Dictionary<string, int> sectionPageNumbers = new Dictionary<string, int>();
        private Dictionary<string, PdfTemplate> tocTemplates = new Dictionary<string, PdfTemplate>();

        public PDFReportGenerator(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            tempImageFolder = Path.Combine(Path.GetTempPath(), "RevitLightingImages_" + DateTime.Now.Ticks);
            Directory.CreateDirectory(tempImageFolder);
            InitializeFonts();
        }

        private void InitializeFonts()
        {
            try
            {
                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                BaseFont bfb = BaseFont.CreateFont(BaseFont.HELVETICA_BOLD, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                BaseFont bfi = BaseFont.CreateFont(BaseFont.HELVETICA_OBLIQUE, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                titleFont = new PdfFont(bfb, 24, PdfFont.BOLD, BaseColor.WHITE);
                headerFont = new PdfFont(bfb, 16, PdfFont.BOLD, primaryColor);
                normalFont = new PdfFont(bf, 10, PdfFont.NORMAL, BaseColor.BLACK);
                boldFont = new PdfFont(bfb, 10, PdfFont.BOLD, BaseColor.BLACK);
                italicFont = new PdfFont(bfi, 9, PdfFont.ITALIC, darkGray);
            }
            catch { titleFont = headerFont = normalFont = boldFont = italicFont = new PdfFont(PdfFont.FontFamily.HELVETICA, 10); }
        }

        public void GenerateReport(
            List<CalculationResult> results,
            string projectName,
            string reference,
            string client,
            string engineer,
            string studyOffice,
            string outputPath)
        {
            this.projectName = projectName;
            this.reportDate = DateTime.Now.ToString("dd/MM/yyyy");

            try
            {
                pdfDocument = new iTextSharp.text.Document(PageSize.A4, 40, 40, 60, 60);
                writer = PdfWriter.GetInstance(pdfDocument, new FileStream(outputPath, FileMode.Create));

                // Ajouter les events pour le footer
                writer.PageEvent = new PDFPageEventHelper(projectName, reportDate);

                pdfDocument.Open();
                AddCoverPage(projectName, reference, client, engineer, studyOffice, results);
                pdfDocument.NewPage();
                AddTableOfContents(results);
                pdfDocument.NewPage();
                AddExecutiveSummary(results);
                pdfDocument.NewPage();
                foreach (var result in results) { AddRoomDetailedAnalysis(result); pdfDocument.NewPage(); }
                AddLuminaireCatalog(results);
                pdfDocument.NewPage();
                AddEnergyAnalysis(results);
                pdfDocument.NewPage();
                AddRecommendations(results);

                // Remplir les templates de la table des matières avec les vrais numéros de page
                FillTocTemplates();

                pdfDocument.Close();
            }
            catch (Exception ex) { if (pdfDocument != null && pdfDocument.IsOpen()) pdfDocument.Close(); throw new Exception($"Erreur PDF : {ex.Message}", ex); }
            finally { CleanupTempImages(); }
        }

        private void AddCoverPage(string projectName, string reference, string client, string engineer, string studyOffice, List<CalculationResult> results)
        {
            PdfPTable ht = new PdfPTable(1) { WidthPercentage = 100 };
            PdfPCell hc = new PdfPCell(new Phrase("RAPPORT D'ANALYSE D'ÉCLAIRAGE", titleFont)) { BackgroundColor = primaryColor, HorizontalAlignment = PdfElement.ALIGN_CENTER, Padding = 20, Border = PdfRectangle.NO_BORDER };
            ht.AddCell(hc);
            pdfDocument.Add(ht);
            pdfDocument.Add(new Paragraph("\n"));
            bool isCompliant = results.All(r => r.EstConforme);
            PdfFont sf = new PdfFont(boldFont.BaseFont, 18, PdfFont.BOLD, isCompliant ? successColor : dangerColor);
            pdfDocument.Add(new Paragraph(isCompliant ? "PROJET CONFORME EN 12464-1" : "PROJET NON CONFORME EN 12464-1", sf) { Alignment = PdfElement.ALIGN_CENTER });
            pdfDocument.Add(new Paragraph("\n\n"));
            PdfPTable it = new PdfPTable(2) { WidthPercentage = 100 };
            it.SetWidths(new float[] { 1f, 2f });
            AddInfoRow(it, "Nom de l'affaire :", projectName);
            AddInfoRow(it, "Référence :", reference);
            AddInfoRow(it, "Client :", client);
            AddInfoRow(it, "Date du rapport :", this.reportDate);
            AddInfoRow(it, "Bureau d'études :", studyOffice);
            AddInfoRow(it, "Ingénieur :", engineer);
            pdfDocument.Add(it);
        }

        private void AddInfoRow(PdfPTable table, string label, string value)
        {
            table.AddCell(new PdfPCell(new Phrase(label, boldFont)) { Border = PdfRectangle.NO_BORDER, Padding = 5 });
            table.AddCell(new PdfPCell(new Phrase(value ?? "-", normalFont)) { Border = PdfRectangle.NO_BORDER, Padding = 5 });
        }

        private void AddTableOfContents(List<CalculationResult> results)
        {
            AddSectionTitle("TABLE DES MATIÈRES");
            pdfDocument.Add(new Paragraph("Ce rapport contient les sections suivantes :\n\n", normalFont));

            PdfPTable t = new PdfPTable(2) { WidthPercentage = 100 };
            t.SetWidths(new float[] { 4f, 1f });
            t.AddCell(CreateTableHeader("Section"));
            t.AddCell(CreateTableHeader("Page"));

            AddTocEntry(t, "Résumé Exécutif", "TOC_SUMMARY");
            foreach (var r in results)
            {
                string roomKey = !string.IsNullOrEmpty(r.RoomId) ? r.RoomId : r.RoomName;
                AddTocEntry(t, "  • " + r.RoomName, "TOC_ROOM_" + roomKey);
            }
            AddTocEntry(t, "Catalogue des Luminaires", "TOC_CATALOG");
            AddTocEntry(t, "Analyse Énergétique", "TOC_ENERGY");
            AddTocEntry(t, "Recommandations", "TOC_RECOMMENDATIONS");

            pdfDocument.Add(t);
        }

        private void AddTocEntry(PdfPTable table, string sectionName, string templateKey)
        {
            table.AddCell(CreateTableCell(sectionName, false));

            // Créer un template placeholder pour le numéro de page
            PdfTemplate template = writer.DirectContent.CreateTemplate(50, 15);
            tocTemplates[templateKey] = template;

            // Créer une Image à partir du template pour l'ajouter à la cellule
            iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(template);
            PdfPCell cell = new PdfPCell(img);
            cell.HorizontalAlignment = PdfElement.ALIGN_CENTER;
            cell.VerticalAlignment = PdfElement.ALIGN_MIDDLE;
            cell.Padding = 5;
            table.AddCell(cell);
        }

        /// <summary>
        /// Enregistre le numéro de page actuel pour une section donnée
        /// </summary>
        private void RecordSectionPage(string sectionKey)
        {
            if (writer != null)
            {
                sectionPageNumbers[sectionKey] = writer.PageNumber;
            }
        }

        /// <summary>
        /// Remplit tous les templates de la table des matières avec les vrais numéros de page
        /// </summary>
        private void FillTocTemplates()
        {
            if (writer == null) return;

            foreach (var kvp in tocTemplates)
            {
                string templateKey = kvp.Key;
                PdfTemplate template = kvp.Value;

                // Récupérer le numéro de page enregistré pour cette section
                if (sectionPageNumbers.ContainsKey(templateKey))
                {
                    int pageNumber = sectionPageNumbers[templateKey];

                    // Créer une police pour le numéro de page
                    BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                    // Dessiner le numéro de page sur le template
                    PdfContentByte cb = template;
                    cb.BeginText();
                    cb.SetFontAndSize(bf, 10);
                    cb.SetColorFill(BaseColor.BLACK);
                    cb.ShowTextAligned(PdfContentByte.ALIGN_CENTER, pageNumber.ToString(), 25, 5, 0);
                    cb.EndText();
                }
            }
        }

        private void AddExecutiveSummary(List<CalculationResult> results)
        {
            RecordSectionPage("TOC_SUMMARY");
            AddSectionTitle("RÉSUMÉ EXÉCUTIF");
            AddSubsectionTitle("Vue d'ensemble du projet");
            double ts = results.Sum(r => r.RoomArea), tp = results.Sum(r => r.PuissanceTotale);
            int cr = results.Count(r => r.EstConforme);
            PdfPTable st = new PdfPTable(2) { WidthPercentage = 100 };
            st.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(st, "Nombre de pièces analysées", results.Count.ToString());
            AddSummaryRow(st, "Pièces conformes", $"{cr} / {results.Count}");
            AddSummaryRow(st, "Surface totale", $"{ts:F2} m²");
            AddSummaryRow(st, "Puissance installée totale", $"{tp:F0} W");
            AddSummaryRow(st, "Densité de puissance moyenne", $"{(ts > 0 ? tp / ts : 0):F2} W/m²");
            pdfDocument.Add(st);
            pdfDocument.Add(new Paragraph("\n"));
            AddSubsectionTitle("Résultats par pièce");
            PdfPTable rt = new PdfPTable(5) { WidthPercentage = 100 };
            rt.SetWidths(new float[] { 3f, 2f, 2f, 2f, 1f });
            rt.AddCell(CreateTableHeader("Pièce"));
            rt.AddCell(CreateTableHeader("Éclairement\nmoyen"));
            rt.AddCell(CreateTableHeader("Uniformité"));
            rt.AddCell(CreateTableHeader("Requis"));
            rt.AddCell(CreateTableHeader("Statut"));
            foreach (var r in results)
            {
                rt.AddCell(CreateTableCell(r.RoomName, false));
                rt.AddCell(CreateTableCell($"{r.AverageIlluminance:F0} lux", false));
                rt.AddCell(CreateTableCell($"{r.Uniformity:F2}", false));
                rt.AddCell(CreateTableCell($"{r.EclairementRequis:F0} lux", false));
                PdfFont stf = new PdfFont(boldFont.BaseFont, 14, PdfFont.BOLD, r.EstConforme ? successColor : dangerColor);
                rt.AddCell(new PdfPCell(new Phrase(r.EstConforme ? "✓" : "✗", stf)) { HorizontalAlignment = PdfElement.ALIGN_CENTER, Padding = 5 });
            }
            pdfDocument.Add(rt);
            var ncr = results.Where(r => !r.EstConforme).ToList();
            if (ncr.Any())
            {
                pdfDocument.Add(new Paragraph("\n"));
                AddSubsectionTitle("Points de non-conformité");
                foreach (var r in ncr) pdfDocument.Add(new Paragraph($"• {r.RoomName} : Éclairement insuffisant ({r.AverageIlluminance:F0} lux < {r.EclairementRequis:F0} lux requis)", normalFont));
            }
        }

        private void AddRoomDetailedAnalysis(CalculationResult result)
        {
            string roomKey = !string.IsNullOrEmpty(result.RoomId) ? result.RoomId : result.RoomName;
            RecordSectionPage("TOC_ROOM_" + roomKey);
            AddSectionTitle($"LUMINAIRES : {result.RoomName}");

            foreach (var lum in result.LuminairesUtilises)
            {
                AddLuminaireInfo(lum);
                pdfDocument.Add(new Paragraph("\n"));
            }

            // ===== PAGE PLEINE : CARTE DE GRILLE 2D =====
            pdfDocument.NewPage();
            AddSectionTitle("CARTE DE GRILLE D'ÉCLAIRAGE");
            pdfDocument.Add(new Paragraph($"Répartition des niveaux d'éclairage - {result.RoomName}\n", normalFont));

            // Afficher la hauteur du plan de travail
            if (result.HeightResults != null && result.HeightResults.Count > 0)
            {
                double heightUsed = result.HeightResults[0].WorkPlaneHeight;
                pdfDocument.Add(new Paragraph($"Hauteur du plan de travail : {heightUsed:F2} m\n", boldFont));
            }

            pdfDocument.Add(new Paragraph($"Espacement de la grille : {result.GridSpacing:F2} m\n\n", italicFont));

            // Ajouter la carte de grille en pleine page
            if (!string.IsNullOrEmpty(result.GridMapPath) && File.Exists(result.GridMapPath))
            {
                try
                {
                    iTextSharp.text.Image gridImg = iTextSharp.text.Image.GetInstance(result.GridMapPath);
                    // Dimensions maximales pour une page A4 (520x720 pour garder des marges)
                    gridImg.ScaleToFit(520f, 600f);
                    gridImg.Alignment = PdfElement.ALIGN_CENTER;
                    pdfDocument.Add(gridImg);
                }
                catch { pdfDocument.Add(new Paragraph("Erreur chargement carte de grille", italicFont)); }
            }
            else
            {
                pdfDocument.Add(new Paragraph("Carte de grille non disponible", italicFont));
            }

            // ===== PAGE PLEINE : CARTE THERMIQUE 3D =====
            pdfDocument.NewPage();
            AddSectionTitle("CARTE THERMIQUE 3D");
            pdfDocument.Add(new Paragraph($"Visualisation 3D de la distribution des niveaux d'éclairage - {result.RoomName}\n", normalFont));

            // Afficher la hauteur du plan de travail
            if (result.HeightResults != null && result.HeightResults.Count > 0)
            {
                double heightUsed = result.HeightResults[0].WorkPlaneHeight;
                pdfDocument.Add(new Paragraph($"Hauteur du plan de travail : {heightUsed:F2} m\n\n", boldFont));
            }
            else
            {
                pdfDocument.Add(new Paragraph("\n"));
            }

            // Ajouter le heatmap 3D en pleine page (récupéré de la première hauteur s'il y en a plusieurs)
            string heatmap3DPath = null;
            if (result.HeightResults != null && result.HeightResults.Count > 0)
            {
                heatmap3DPath = result.HeightResults[0].Heatmap3DPath;
            }

            if (!string.IsNullOrEmpty(heatmap3DPath) && File.Exists(heatmap3DPath))
            {
                try
                {
                    iTextSharp.text.Image heatmapImg = iTextSharp.text.Image.GetInstance(heatmap3DPath);
                    // Dimensions maximales pour une page A4 (520x720 pour garder des marges)
                    heatmapImg.ScaleToFit(520f, 600f);
                    heatmapImg.Alignment = PdfElement.ALIGN_CENTER;
                    pdfDocument.Add(heatmapImg);
                }
                catch { pdfDocument.Add(new Paragraph("Erreur chargement carte thermique 3D", italicFont)); }
            }
            else
            {
                pdfDocument.Add(new Paragraph("Carte thermique 3D non disponible", italicFont));
            }

            // ===== PAGE : VUES DE LA PIÈCE =====
            pdfDocument.NewPage();
            AddSectionTitle("VUES DE LA PIÈCE");
            pdfDocument.Add(new Paragraph($"Vues 2D et 3D avec luminaires - {result.RoomName}\n\n", normalFont));
            AddRoomViews(result);
            pdfDocument.NewPage();

            AddSectionTitle($"ANALYSE : {result.RoomName}");
            AddSubsectionTitle("Caractéristiques de la pièce");
            PdfPTable ct = new PdfPTable(2) { WidthPercentage = 100 };
            ct.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(ct, "Surface", $"{result.RoomArea:F2} m²");
            AddSummaryRow(ct, "Hauteur sous plafond", $"{result.HauteurPiece:F2} m");
            if (result.LuminaireCalculatedHeightMeters > 0)
            {
                AddSummaryRow(ct, "Hauteur source lumineuse", $"{result.LuminaireCalculatedHeightMeters:F2} m");
            }
            AddSummaryRow(ct, "Norme appliquée", $"{result.TypeActivite} - {result.EclairementRequis:F0} lux");
            pdfDocument.Add(ct);
            pdfDocument.Add(new Paragraph("\n"));

            AddSubsectionTitle("Résultats d'analyse");
            PdfPTable req = new PdfPTable(2) { WidthPercentage = 100 };
            req.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(req, "Éclairement requis", $"{result.EclairementRequis:F0} lux");
            AddSummaryRow(req, "Uniformité requise", $"{result.UniformiteRequise:F2}");
            pdfDocument.Add(req);
            pdfDocument.Add(new Paragraph("\n"));

            // Afficher les résultats par hauteur si plusieurs hauteurs
            if (result.HeightResults != null && result.HeightResults.Count > 1)
            {
                AddSubsectionTitle("Résultats par hauteur de plan de travail");

                foreach (var heightResult in result.HeightResults)
                {
                    pdfDocument.Add(new Paragraph($"\n➤ Hauteur {heightResult.WorkPlaneHeight:F2} m\n", boldFont));

                    PdfPTable htable = new PdfPTable(3) { WidthPercentage = 100 };
                    htable.SetWidths(new float[] { 3f, 2f, 1f });
                    htable.AddCell(CreateTableHeader("Paramètre"));
                    htable.AddCell(CreateTableHeader("Valeur mesurée"));
                    htable.AddCell(CreateTableHeader("Statut"));
                    AddResultRow(htable, "Éclairement moyen", $"{heightResult.AverageIlluminance:F0} lux", heightResult.AverageIlluminance >= result.EclairementRequis);
                    AddResultRow(htable, "Éclairement minimum", $"{heightResult.MinIlluminance:F0} lux", true);
                    AddResultRow(htable, "Éclairement maximum", $"{heightResult.MaxIlluminance:F0} lux", true);
                    AddResultRow(htable, "Uniformité globale (U₀)", $"{heightResult.Uniformity:F2}", heightResult.Uniformity >= result.UniformiteRequise);
                    // P4: Uniformité locale pour chaque hauteur
                    AddResultRow(htable, "Uniformité locale (Uₕ)", $"{heightResult.LocalUniformity:F2}", heightResult.LocalUniformity >= 0.60);
                    pdfDocument.Add(htable);

                    // Grille 2D en pleine page pour cette hauteur
                    if (!string.IsNullOrEmpty(heightResult.GridMapPath) && File.Exists(heightResult.GridMapPath))
                    {
                        try
                        {
                            pdfDocument.NewPage();
                            AddSectionTitle($"GRILLE 2D - Hauteur {heightResult.WorkPlaneHeight:F2} m");
                            pdfDocument.Add(new Paragraph($"{result.RoomName}\n\n", normalFont));
                            iTextSharp.text.Image hGridImg = iTextSharp.text.Image.GetInstance(heightResult.GridMapPath);
                            hGridImg.ScaleToFit(520f, 600f);
                            hGridImg.Alignment = PdfElement.ALIGN_CENTER;
                            pdfDocument.Add(hGridImg);
                        }
                        catch { }
                    }

                    // Heatmap 3D en pleine page pour cette hauteur
                    if (!string.IsNullOrEmpty(heightResult.Heatmap3DPath) && File.Exists(heightResult.Heatmap3DPath))
                    {
                        try
                        {
                            pdfDocument.NewPage();
                            AddSectionTitle($"HEATMAP 3D - Hauteur {heightResult.WorkPlaneHeight:F2} m");
                            pdfDocument.Add(new Paragraph($"{result.RoomName}\n\n", normalFont));
                            iTextSharp.text.Image heatmap3DImg = iTextSharp.text.Image.GetInstance(heightResult.Heatmap3DPath);
                            heatmap3DImg.ScaleToFit(520f, 600f);
                            heatmap3DImg.Alignment = PdfElement.ALIGN_CENTER;
                            pdfDocument.Add(heatmap3DImg);
                        }
                        catch { }
                    }

                    // Icône conformité
                    PdfFont conformFont = new PdfFont(boldFont.BaseFont, 12, PdfFont.BOLD, heightResult.MeetsStandard ? successColor : dangerColor);
                    pdfDocument.Add(new Paragraph(heightResult.MeetsStandard ? "\n✓ Conforme" : "\n✗ Non conforme", conformFont));
                    pdfDocument.Add(new Paragraph("\n"));
                }
            }
            else
            {
                // Affichage classique pour une seule hauteur
                PdfPTable res = new PdfPTable(3) { WidthPercentage = 100 };
                res.SetWidths(new float[] { 3f, 2f, 1f });
                res.AddCell(CreateTableHeader("Paramètre"));
                res.AddCell(CreateTableHeader("Valeur mesurée"));
                res.AddCell(CreateTableHeader("Statut"));
                AddResultRow(res, "Éclairement moyen", $"{result.AverageIlluminance:F0} lux", result.AverageIlluminance >= result.EclairementRequis);
                AddResultRow(res, "Éclairement minimum", $"{result.MinIlluminance:F0} lux", true);
                AddResultRow(res, "Éclairement maximum", $"{result.MaxIlluminance:F0} lux", true);
                AddResultRow(res, "Uniformité globale (U₀)", $"{result.Uniformity:F2}", result.Uniformity >= result.UniformiteRequise);
                // P4: Affichage uniformité locale selon EN 12464-1 section 4.3
                AddResultRow(res, "Uniformité locale (Uₕ)", $"{result.LocalUniformity:F2}", result.LocalUniformity >= 0.60);
                pdfDocument.Add(res);
                pdfDocument.Add(new Paragraph("\n"));
            }

            AddSubsectionTitle("Données énergétiques");
            PdfPTable et = new PdfPTable(2) { WidthPercentage = 100 };
            et.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(et, "Puissance installée", $"{result.PuissanceTotale:F0} W");
            AddSummaryRow(et, "Densité de puissance", $"{result.DensitePuissance:F2} W/m²");
            pdfDocument.Add(et);
        }

        private void AddRoomViews(CalculationResult result)
        {
            try
            {
                // VUE EN PLAN - Page complète
                AddSubsectionTitle("Vue en plan");
                pdfDocument.Add(new Paragraph($"Affichage réaliste avec photométries - {result.RoomName}\n\n", normalFont));

                if (!string.IsNullOrEmpty(result.PlanImagePath) && File.Exists(result.PlanImagePath))
                {
                    try
                    {
                        iTextSharp.text.Image planImg = iTextSharp.text.Image.GetInstance(result.PlanImagePath);
                        planImg.ScaleToFit(520f, 600f);
                        planImg.Alignment = PdfElement.ALIGN_CENTER;
                        pdfDocument.Add(planImg);
                    }
                    catch { pdfDocument.Add(new Paragraph("Erreur chargement vue en plan", italicFont)); }
                }
                else
                {
                    pdfDocument.Add(new Paragraph("Vue en plan non générée", italicFont));
                }

                // VUE 3D - Nouvelle page
                pdfDocument.NewPage();
                AddSubsectionTitle("Vue 3D");
                pdfDocument.Add(new Paragraph($"Affichage réaliste avec photométries - {result.RoomName}\n\n", normalFont));

                if (!string.IsNullOrEmpty(result.View3DImagePath) && File.Exists(result.View3DImagePath))
                {
                    try
                    {
                        iTextSharp.text.Image view3DImg = iTextSharp.text.Image.GetInstance(result.View3DImagePath);
                        view3DImg.ScaleToFit(520f, 600f);
                        view3DImg.Alignment = PdfElement.ALIGN_CENTER;
                        pdfDocument.Add(view3DImg);
                    }
                    catch { pdfDocument.Add(new Paragraph("Erreur chargement vue 3D", italicFont)); }
                }
                else
                {
                    pdfDocument.Add(new Paragraph("Vue 3D non générée", italicFont));
                }
            }
            catch (Exception ex) { pdfDocument.Add(new Paragraph($"Erreur vues : {ex.Message}", italicFont)); }
        }

        private void AddLuminaireInfo(LuminaireUsageInfo lum)
        {
            PdfFont ltf = new PdfFont(boldFont.BaseFont, 12, PdfFont.BOLD, primaryColor);
            pdfDocument.Add(new Paragraph($"{lum.Fabricant} - {lum.TypeName}\n\n", ltf));
            PdfPTable it = new PdfPTable(2) { WidthPercentage = 100 };
            it.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(it, "Fabricant", lum.Fabricant ?? "-");
            AddSummaryRow(it, "Type", lum.TypeName ?? "-");
            AddSummaryRow(it, "Référence", lum.Reference ?? "-");
            AddSummaryRow(it, "Quantité", lum.Quantity.ToString());
            AddSummaryRow(it, "Flux unitaire", $"{lum.FluxLumineux:F0} lm");
            AddSummaryRow(it, "Flux total", $"{lum.TotalFlux:F0} lm");
            AddSummaryRow(it, "Puissance unitaire", $"{lum.Puissance:F0} W");
            AddSummaryRow(it, "Puissance totale", $"{lum.TotalPower:F0} W");
            if (lum.FluxLumineux > 0 && lum.Puissance > 0) AddSummaryRow(it, "Efficacité", $"{lum.FluxLumineux / lum.Puissance:F0} lm/W");
            AddSummaryRow(it, "Température couleur", $"{lum.TemperatureCouleur:F0} K");
            pdfDocument.Add(it);
        }

        private void AddLuminaireCatalog(List<CalculationResult> results)
        {
            RecordSectionPage("TOC_CATALOG");
            AddSectionTitle("CATALOGUE DES LUMINAIRES");
            pdfDocument.Add(new Paragraph("Liste complète des luminaires :\n\n", normalFont));

            Dictionary<string, CatalogEntry> cat = new Dictionary<string, CatalogEntry>();
            foreach (var r in results)
            {
                foreach (var l in r.LuminairesUtilises)
                {
                    string key = $"{l.Fabricant}_{l.TypeName}";
                    if (!cat.ContainsKey(key))
                        cat[key] = new CatalogEntry { Lum = l, TotalQty = 0, Rooms = new List<string>() };
                    cat[key].TotalQty += l.Quantity;
                    if (!cat[key].Rooms.Contains(r.RoomName)) cat[key].Rooms.Add(r.RoomName);
                }
            }

            foreach (var e in cat.Values)
            {
                AddLuminaireInfo(e.Lum);
                pdfDocument.Add(new Paragraph($"Quantité totale projet : {e.TotalQty}\n", boldFont));
                pdfDocument.Add(new Paragraph($"Pièces : {string.Join(", ", e.Rooms)}\n\n", normalFont));
            }
        }

        private class CatalogEntry { public LuminaireUsageInfo Lum; public int TotalQty; public List<string> Rooms; }

        private void AddEnergyAnalysis(List<CalculationResult> results)
        {
            RecordSectionPage("TOC_ENERGY");
            AddSectionTitle("ANALYSE ÉNERGÉTIQUE");
            double tp = results.Sum(r => r.PuissanceTotale);
            AddSubsectionTitle("Installation LED actuelle");
            double ac = tp * 10 * 250 / 1000, cost = ac * 0.150;
            PdfPTable lt = new PdfPTable(2) { WidthPercentage = 100 };
            lt.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(lt, "Puissance installée", $"{tp:F0} W");
            AddSummaryRow(lt, "Consommation annuelle", $"{ac:F0} kWh/an");
            AddSummaryRow(lt, "Coût annuel", $"{cost:F2} €/an");
            pdfDocument.Add(lt);
            pdfDocument.Add(new Paragraph("\n"));
            AddSubsectionTitle("Économies LED vs Halogène");
            double hp = tp * 5, hac = hp * 10 * 250 / 1000, hcost = hac * 0.150;
            PdfPTable st = new PdfPTable(2) { WidthPercentage = 100 };
            st.SetWidths(new float[] { 3f, 2f });
            AddSummaryRow(st, "Économie de puissance", $"{hp - tp:F0} W (-80%)");
            AddSummaryRow(st, "Économie annuelle", $"{hac - ac:F0} kWh/an");
            AddSummaryRow(st, "Économie financière", $"{hcost - cost:F2} €/an");
            pdfDocument.Add(st);
        }

        private void AddRecommendations(List<CalculationResult> results)
        {
            RecordSectionPage("TOC_RECOMMENDATIONS");
            AddSectionTitle("RECOMMANDATIONS");

            // Séparer les pièces non conformes (éclairement insuffisant) et celles avec uniformité à améliorer
            var nonCompliant = results.Where(r => !r.EstConforme).ToList();
            var compliantButUniformityIssues = results.Where(r =>
                r.EstConforme &&
                r.Uniformity < r.UniformiteRequise).ToList();

            // Si tout est parfait
            if (nonCompliant.Count == 0 && compliantButUniformityIssues.Count == 0)
            {
                pdfDocument.Add(new Paragraph("Toutes les pièces sont conformes aux normes EN 12464-1.", normalFont));
                return;
            }

            // Section 1 : Pièces NON CONFORMES (éclairement insuffisant)
            if (nonCompliant.Count > 0)
            {
                AddSubsectionTitle("Actions correctives prioritaires");
                pdfDocument.Add(new Paragraph("Pièces non conformes (éclairement insuffisant) :\n\n", normalFont));
                int c = 1;
                foreach (var r in nonCompliant)
                {
                    var issues = new List<string>();

                    // Vérifier éclairement
                    if (r.AverageIlluminance < r.EclairementRequis)
                    {
                        double def = r.EclairementRequis - r.AverageIlluminance;
                        double defp = (def / r.EclairementRequis) * 100;
                        double avg = r.LuminairesUtilises.Any() ? r.LuminairesUtilises.Average(l => l.FluxLumineux) : 0;
                        int add = avg > 0 ? (int)Math.Ceiling((def * r.RoomArea) / avg) : 0;
                        issues.Add($"Éclairement insuffisant ({r.AverageIlluminance:F0} lux < {r.EclairementRequis:F0} lux, déficit {defp:F0}%). Suggestion : ajouter ~{add} luminaire(s)");
                    }

                    // Vérifier uniformité aussi pour ces pièces
                    if (r.Uniformity < r.UniformiteRequise)
                    {
                        double defU = (r.UniformiteRequise - r.Uniformity) * 100;
                        issues.Add($"Uniformité insuffisante ({r.Uniformity:F2} < {r.UniformiteRequise:F2}, déficit {defU:F0}%). Suggestion : mieux répartir les luminaires");
                    }

                    if (issues.Any())
                    {
                        pdfDocument.Add(new Paragraph($"{c}. {r.RoomName} :\n  • " + string.Join("\n  • ", issues) + "\n\n", normalFont));
                        c++;
                    }
                }
            }

            // Section 2 : Pièces CONFORMES mais uniformité à améliorer
            if (compliantButUniformityIssues.Count > 0)
            {
                pdfDocument.Add(new Paragraph("\n", normalFont));
                AddSubsectionTitle("Améliorations recommandées");
                pdfDocument.Add(new Paragraph("Pièces conformes mais avec uniformité à améliorer :\n\n", normalFont));
                int c = 1;
                foreach (var r in compliantButUniformityIssues)
                {
                    double defU = (r.UniformiteRequise - r.Uniformity) * 100;
                    pdfDocument.Add(new Paragraph(
                        $"{c}. {r.RoomName} :\n" +
                        $"  • Éclairement moyen : {r.AverageIlluminance:F0} lux ✓ (conforme)\n" +
                        $"  • Uniformité : {r.Uniformity:F2} (requis : {r.UniformiteRequise:F2})\n" +
                        $"  • Suggestion : mieux répartir les luminaires pour améliorer l'uniformité\n\n",
                        normalFont));
                    c++;
                }
            }
        }

        private void CleanupTempImages() { try { if (Directory.Exists(tempImageFolder)) Directory.Delete(tempImageFolder, true); } catch { } }
        private void AddSectionTitle(string t) { pdfDocument.Add(new Paragraph("\n")); pdfDocument.Add(new Paragraph(t, headerFont)); PdfPTable sep = new PdfPTable(1) { WidthPercentage = 100 }; sep.AddCell(new PdfPCell { FixedHeight = 3, BackgroundColor = primaryColor, Border = PdfRectangle.NO_BORDER }); pdfDocument.Add(sep); pdfDocument.Add(new Paragraph("\n")); }
        private void AddSubsectionTitle(string t) { PdfFont ssf = new PdfFont(boldFont.BaseFont, 12, PdfFont.BOLD, darkGray); pdfDocument.Add(new Paragraph(t + "\n\n", ssf)); }
        private void AddSummaryRow(PdfPTable t, string l, string v) { t.AddCell(new PdfPCell(new Phrase(l, normalFont)) { Border = PdfRectangle.NO_BORDER, Padding = 3 }); t.AddCell(new PdfPCell(new Phrase(v, boldFont)) { Border = PdfRectangle.NO_BORDER, Padding = 3, HorizontalAlignment = PdfElement.ALIGN_RIGHT }); }
        private PdfPCell CreateTableHeader(string t) { PdfFont hf = new PdfFont(boldFont.BaseFont, 10, PdfFont.BOLD, BaseColor.WHITE); return new PdfPCell(new Phrase(t, hf)) { BackgroundColor = primaryColor, HorizontalAlignment = PdfElement.ALIGN_CENTER, Padding = 8 }; }
        private PdfPCell CreateTableCell(string t, bool c) { PdfPCell cell = new PdfPCell(new Phrase(t, normalFont)) { Padding = 5 }; if (c) cell.HorizontalAlignment = PdfElement.ALIGN_CENTER; return cell; }
        private void AddResultRow(PdfPTable t, string p, string v, bool ok) { t.AddCell(CreateTableCell(p, false)); t.AddCell(CreateTableCell(v, false)); PdfFont sf = new PdfFont(boldFont.BaseFont, 14, PdfFont.BOLD, ok ? successColor : dangerColor); t.AddCell(new PdfPCell(new Phrase(ok ? "✓" : "✗", sf)) { HorizontalAlignment = PdfElement.ALIGN_CENTER, Padding = 5 }); }
    }
}