using RevitLightingPlugin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RevitLightingPlugin.Core
{
    public class ExportManager
    {
        public static void ExportToCsv(List<CalculationResult> results, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // En-tête
                    writer.WriteLine("Pièce;Numéro;Surface (m²);Luminaires;Éclairement moyen (lux);Éclairement min (lux);Éclairement max (lux);Uniformité;Conforme");

                    // Données
                    foreach (var result in results)
                    {
                        writer.WriteLine($"{result.RoomName};{result.RoomNumber};{result.RoomArea:F2};{result.LuminaireCount};{result.AverageIlluminance:F0};{result.MinIlluminance:F0};{result.MaxIlluminance:F0};{result.Uniformity:F2};{(result.MeetsStandard ? "Oui" : "Non")}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'export CSV : {ex.Message}", ex);
            }
        }

        public static void ExportSummary(List<CalculationResult> results, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("RÉSUMÉ DE L'ANALYSE D'ÉCLAIRAGE");
                    writer.WriteLine("================================");
                    writer.WriteLine();
                    writer.WriteLine($"Date : {DateTime.Now:dd/MM/yyyy HH:mm}");
                    writer.WriteLine($"Nombre de pièces analysées : {results.Count}");
                    writer.WriteLine($"Pièces conformes : {results.Count(r => r.MeetsStandard)}");
                    writer.WriteLine($"Surface totale : {results.Sum(r => r.RoomArea):F2} m²");
                    writer.WriteLine($"Nombre total de luminaires : {results.Sum(r => r.LuminaireCount)}");
                    writer.WriteLine();
                    writer.WriteLine("DÉTAILS PAR PIÈCE");
                    writer.WriteLine("=================");
                    writer.WriteLine();

                    foreach (var result in results)
                    {
                        writer.WriteLine($"Pièce : {result.RoomName} ({result.RoomNumber})");
                        writer.WriteLine($"  Surface : {result.RoomArea:F2} m²");
                        writer.WriteLine($"  Luminaires : {result.LuminaireCount}");
                        writer.WriteLine($"  Éclairement moyen : {result.AverageIlluminance:F0} lux");
                        writer.WriteLine($"  Éclairement min : {result.MinIlluminance:F0} lux");
                        writer.WriteLine($"  Éclairement max : {result.MaxIlluminance:F0} lux");
                        writer.WriteLine($"  Uniformité : {result.Uniformity:F2}");
                        writer.WriteLine($"  Conforme : {(result.MeetsStandard ? "OUI" : "NON")}");
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'export du résumé : {ex.Message}", ex);
            }
        }
    }
}