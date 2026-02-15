using System;
using System.IO;
using System.Linq;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Analyseur de fichiers IES pour extraire les données photométriques
    /// </summary>
    public class PhotometricAnalyzer
    {
        /// <summary>
        /// Analyse un fichier IES et extrait les données photométriques
        /// </summary>
        public PhotometricData AnalyzeIESFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var data = new PhotometricData();
                var lines = File.ReadAllLines(filePath);

                // Parser les métadonnées
                ParseMetadata(lines, data);

                // Parser les données photométriques
                ParsePhotometricData(lines, data);

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur analyse IES {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse les métadonnées du fichier IES
        /// </summary>
        private void ParseMetadata(string[] lines, PhotometricData data)
        {
            foreach (var line in lines)
            {
                if (line.StartsWith("[MANUFAC]", StringComparison.OrdinalIgnoreCase))
                {
                    data.Manufacturer = line.Substring(9).Trim();
                }
                else if (line.StartsWith("[LUMCAT]", StringComparison.OrdinalIgnoreCase))
                {
                    data.Reference = line.Substring(8).Trim();
                }
                else if (line.StartsWith("[LUMINAIRE]", StringComparison.OrdinalIgnoreCase))
                {
                    data.LuminaireType = line.Substring(11).Trim();
                }
            }
        }

        /// <summary>
        /// Parse les données photométriques
        /// </summary>
        private void ParsePhotometricData(string[] lines, PhotometricData data)
        {
            // Recherche de la ligne TILT
            int tiltIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().Equals("TILT=NONE", StringComparison.OrdinalIgnoreCase))
                {
                    tiltIndex = i;
                    break;
                }
            }

            if (tiltIndex == -1)
                return;

            try
            {
                // Ligne après TILT contient les paramètres principaux
                if (tiltIndex + 1 >= lines.Length)
                    return;

                string[] params1 = lines[tiltIndex + 1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (params1.Length >= 3)
                {
                    // Nombre de lampes, lumens par lampe, etc.
                    int numLamps = 1;
                    double lumensPerLamp = 0;

                    if (int.TryParse(params1[0], out int lamps))
                        numLamps = lamps;

                    if (double.TryParse(params1[1], out double lumens))
                        lumensPerLamp = lumens;

                    data.TotalFlux = numLamps * lumensPerLamp;
                }

                // Calculer la distribution vers le haut/bas (simplifié)
                data.DownwardFlux = data.TotalFlux * 0.92; // Approximation
                data.UpwardFlux = data.TotalFlux * 0.08;

                // Angles approximatifs
                data.Angle50 = 65.0;
                data.Angle10 = 82.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur parsing données photométriques: {ex.Message}");
            }
        }

        /// <summary>
        /// Calcule l'efficacité lumineuse
        /// </summary>
        public double CalculateEfficiency(double totalFlux, double power)
        {
            if (power <= 0)
                return 0;

            return totalFlux / power;
        }

        /// <summary>
        /// Vérifie si un luminaire est adapté pour un usage donné
        /// </summary>
        public bool IsAdaptedFor(PhotometricData data, string usage)
        {
            if (data == null)
                return false;

            // Logique simplifiée basée sur le flux et la distribution
            switch (usage.ToLower())
            {
                case "bureau":
                    return data.TotalFlux >= 2000 && data.DownwardFlux / data.TotalFlux > 0.8;

                case "commerce":
                    return data.TotalFlux >= 1500;

                case "industrie":
                    return data.TotalFlux >= 5000;

                default:
                    return true;
            }
        }
    }
}