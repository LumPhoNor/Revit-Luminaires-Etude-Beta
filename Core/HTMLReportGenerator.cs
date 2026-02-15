using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    public class HTMLReportGenerator
    {
        public static void GenerateReport(List<CalculationResult> results, string filePath)
        {
            try
            {
                StringBuilder html = new StringBuilder();

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='UTF-8'>");
                html.AppendLine("<title>Rapport d'Analyse d'Éclairage</title>");
                html.AppendLine("<style>");
                html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
                html.AppendLine(".container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
                html.AppendLine("h1 { color: #007bff; border-bottom: 3px solid #007bff; padding-bottom: 10px; }");
                html.AppendLine("h2 { color: #333; margin-top: 30px; }");
                html.AppendLine(".summary { background-color: #e7f3ff; padding: 20px; border-radius: 5px; margin: 20px 0; }");
                html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }");
                html.AppendLine("th { background-color: #007bff; color: white; font-weight: bold; }");
                html.AppendLine("tr:nth-child(even) { background-color: #f8f9fa; }");
                html.AppendLine("tr:hover { background-color: #e9ecef; }");
                html.AppendLine(".conforme { color: #28a745; font-weight: bold; }");
                html.AppendLine(".non-conforme { color: #dc3545; font-weight: bold; }");
                html.AppendLine(".footer { text-align: center; margin-top: 40px; color: #6c757d; font-size: 12px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("<div class='container'>");

                html.AppendLine("<h1>📊 Rapport d'Analyse d'Éclairage</h1>");
                html.AppendLine($"<p><strong>Date :</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
                html.AppendLine($"<p><strong>Norme appliquée :</strong> EN 12464-1</p>");

                html.AppendLine("<div class='summary'>");
                html.AppendLine("<h2>Résumé Global</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Indicateur</th><th>Valeur</th></tr>");
                html.AppendLine($"<tr><td>Nombre de pièces analysées</td><td><strong>{results.Count}</strong></td></tr>");
                html.AppendLine($"<tr><td>Pièces conformes</td><td class='conforme'><strong>{results.Count(r => r.MeetsStandard)}</strong></td></tr>");
                html.AppendLine($"<tr><td>Pièces non conformes</td><td class='non-conforme'><strong>{results.Count(r => !r.MeetsStandard)}</strong></td></tr>");
                html.AppendLine($"<tr><td>Surface totale</td><td><strong>{results.Sum(r => r.RoomArea):F2} m²</strong></td></tr>");
                html.AppendLine($"<tr><td>Nombre total de luminaires</td><td><strong>{results.Sum(r => r.LuminaireCount)}</strong></td></tr>");
                html.AppendLine($"<tr><td>Éclairement moyen global</td><td><strong>{results.Average(r => r.AverageIlluminance):F0} lux</strong></td></tr>");
                html.AppendLine($"<tr><td>Puissance totale installée</td><td><strong>{results.Sum(r => r.PuissanceTotale):F0} W</strong></td></tr>");
                html.AppendLine("</table>");
                html.AppendLine("</div>");

                html.AppendLine("<h2>Détails par Pièce</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr>");
                html.AppendLine("<th>Pièce</th>");
                html.AppendLine("<th>Numéro</th>");
                html.AppendLine("<th>Surface<br/>(m²)</th>");
                html.AppendLine("<th>Luminaires</th>");
                html.AppendLine("<th>Écl. moyen<br/>(lux)</th>");
                html.AppendLine("<th>Écl. min<br/>(lux)</th>");
                html.AppendLine("<th>Écl. max<br/>(lux)</th>");
                html.AppendLine("<th>Uniformité</th>");
                html.AppendLine("<th>Puissance<br/>(W)</th>");
                html.AppendLine("<th>Conforme</th>");
                html.AppendLine("</tr>");

                foreach (var result in results)
                {
                    string conformeClass = result.MeetsStandard ? "conforme" : "non-conforme";
                    string conformeText = result.MeetsStandard ? "✓ OUI" : "✗ NON";

                    html.AppendLine("<tr>");
                    html.AppendLine($"<td><strong>{result.RoomName}</strong></td>");
                    html.AppendLine($"<td>{result.RoomNumber}</td>");
                    html.AppendLine($"<td>{result.RoomArea:F2}</td>");
                    html.AppendLine($"<td>{result.LuminaireCount}</td>");
                    html.AppendLine($"<td>{result.AverageIlluminance:F0}</td>");
                    html.AppendLine($"<td>{result.MinIlluminance:F0}</td>");
                    html.AppendLine($"<td>{result.MaxIlluminance:F0}</td>");
                    html.AppendLine($"<td>{result.Uniformity:F2}</td>");
                    html.AppendLine($"<td>{result.PuissanceTotale:F0}</td>");
                    html.AppendLine($"<td class='{conformeClass}'>{conformeText}</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table>");

                // Recommandations si pièces non conformes
                var nonCompliantRooms = results.Where(r => !r.MeetsStandard).ToList();
                if (nonCompliantRooms.Count > 0)
                {
                    html.AppendLine("<h2>⚠️ Recommandations</h2>");
                    html.AppendLine("<div style='background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0;'>");
                    html.AppendLine($"<p><strong>{nonCompliantRooms.Count} pièce(s) nécessite(nt) des actions correctives :</strong></p>");
                    html.AppendLine("<ul>");
                    foreach (var room in nonCompliantRooms)
                    {
                        html.AppendLine($"<li><strong>{room.RoomName}</strong> : {room.Remarques}</li>");
                    }
                    html.AppendLine("</ul>");
                    html.AppendLine("</div>");
                }

                html.AppendLine("<div class='footer'>");
                html.AppendLine("<hr/>");
                html.AppendLine("<p>Rapport généré par <strong>RevitLightingPlugin</strong></p>");
                html.AppendLine($"<p>© {DateTime.Now.Year} - Tous droits réservés</p>");
                html.AppendLine("</div>");

                html.AppendLine("</div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                File.WriteAllText(filePath, html.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de la génération du rapport HTML : {ex.Message}", ex);
            }
        }
    }
}