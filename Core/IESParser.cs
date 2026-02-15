using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Parser pour fichiers IES (IESNA LM-63 standard)
    /// Extrait toutes les données photométriques d'un fichier IES
    /// </summary>
    public class IESParser
    {
        /// <summary>
        /// Résultat du parsing d'un fichier IES
        /// </summary>
        public class IESData
        {
            // Métadonnées
            public string Manufacturer { get; set; }
            public string CatalogNumber { get; set; }
            public string LuminaireName { get; set; }
            public string LampCatalogNumber { get; set; }
            public string TestLaboratory { get; set; }
            public string TestReport { get; set; }
            public DateTime? TestDate { get; set; }

            // Données photométriques principales
            public int NumberOfLamps { get; set; }
            public double LumensPerLamp { get; set; }
            public double TotalLumens { get; set; }
            public double InputWatts { get; set; }
            public double Efficacy { get; set; } // lm/W

            // Dimensions
            public double Width { get; set; }
            public double Length { get; set; }
            public double Height { get; set; }

            // Facteurs
            public double BallistFactor { get; set; }
            public double BallastLampPhotometricFactor { get; set; }

            // Distribution lumineuse
            public int NumberOfVerticalAngles { get; set; }
            public int NumberOfHorizontalAngles { get; set; }
            public PhotometricType PhotometricType { get; set; }
            public UnitsType UnitsType { get; set; }

            // Courbe photométrique
            public List<double> VerticalAngles { get; set; }
            public List<double> HorizontalAngles { get; set; }
            public List<List<double>> CandelaValues { get; set; }

            // Statistiques calculées
            public double MaxCandela { get; set; }
            public double MinCandela { get; set; }
            public double AverageCandela { get; set; }

            // Fichier source
            public string FilePath { get; set; }
            public string FileName { get; set; }

            public IESData()
            {
                VerticalAngles = new List<double>();
                HorizontalAngles = new List<double>();
                CandelaValues = new List<List<double>>();
            }

            /// <summary>
            /// Résumé des informations principales
            /// </summary>
            public override string ToString()
            {
                return $"{Manufacturer} - {LuminaireName}\n" +
                       $"Flux: {TotalLumens:F0} lm | Puissance: {InputWatts:F0} W | Efficacité: {Efficacy:F1} lm/W\n" +
                       $"Catalogue: {CatalogNumber}";
            }
        }

        public enum PhotometricType
        {
            TypeC = 1,  // Standard horizontal
            TypeB = 2,  // Standard vertical
            TypeA = 3   // Standard goniométrique
        }

        public enum UnitsType
        {
            Feet = 1,
            Meters = 2
        }

        /// <summary>
        /// Parse un fichier IES et retourne toutes les données
        /// </summary>
        public static IESData ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Fichier IES introuvable : {filePath}");
            }

            var data = new IESData
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                int lineIndex = 0;

                // PHASE 1 : Lire les métadonnées (lignes commençant par des mots-clés)
                lineIndex = ParseMetadata(lines, lineIndex, data);

                // PHASE 2 : Lire la ligne TILT (inclination)
                lineIndex = ParseTiltLine(lines, lineIndex);

                // PHASE 3 : Lire les données photométriques principales
                lineIndex = ParsePhotometricData(lines, lineIndex, data);

                // PHASE 4 : Lire les courbes photométriques
                ParseCandelaData(lines, lineIndex, data);

                // PHASE 5 : Calculer les statistiques
                CalculateStatistics(data);

                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du parsing du fichier IES '{filePath}' : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parse les métadonnées du header IES
        /// </summary>
        private static int ParseMetadata(string[] lines, int startIndex, IESData data)
        {
            int i = startIndex;

            while (i < lines.Length)
            {
                string line = lines[i].Trim();

                // Si la ligne commence par TILT, on a fini les métadonnées
                if (line.StartsWith("TILT", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }

                // Parser les mots-clés standards
                if (line.StartsWith("["))
                {
                    int endBracket = line.IndexOf(']');
                    if (endBracket > 0)
                    {
                        string keyword = line.Substring(1, endBracket - 1).Trim().ToUpper();
                        string value = line.Substring(endBracket + 1).Trim();

                        switch (keyword)
                        {
                            case "MANUFAC":
                            case "MANUFACTURER":
                                data.Manufacturer = value;
                                break;
                            case "LUMCAT":
                            case "LUMINAIRE CATALOG":
                                data.CatalogNumber = value;
                                break;
                            case "LUMINAIRE":
                                data.LuminaireName = value;
                                break;
                            case "LAMPCAT":
                            case "LAMP CATALOG":
                                data.LampCatalogNumber = value;
                                break;
                            case "TEST":
                            case "TESTLAB":
                                data.TestLaboratory = value;
                                break;
                            case "TESTDATE":
                            case "DATE":
                                if (DateTime.TryParse(value, out DateTime testDate))
                                {
                                    data.TestDate = testDate;
                                }
                                break;
                            case "TESTRPT":
                            case "REPORT":
                                data.TestReport = value;
                                break;
                            case "_ABSOLUTELUMENS":
                                // Format: "2368 ABSOLUTE LUMENS DELIVERED"
                                // Extraire le premier nombre avec Regex
                                var matches = Regex.Matches(value, @"[\d\.]+");
                                if (matches.Count > 0 && double.TryParse(matches[0].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double absoluteLumens))
                                {
                                    data.LumensPerLamp = absoluteLumens;
                                }
                                break;
                        }
                    }
                }

                i++;
            }

            return i;
        }

        /// <summary>
        /// Parse la ligne TILT
        /// </summary>
        private static int ParseTiltLine(string[] lines, int startIndex)
        {
            int i = startIndex;

            while (i < lines.Length)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("TILT", StringComparison.OrdinalIgnoreCase))
                {
                    // TILT=NONE ou TILT=INCLUDE
                    if (line.Contains("INCLUDE"))
                    {
                        // Ignorer les données TILT pour l'instant (rarement utilisé)
                        i++;
                        // Sauter les lignes de données TILT jusqu'à la prochaine section
                        while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                        {
                            i++;
                        }
                    }
                    return i + 1;
                }
                i++;
            }

            return i;
        }

        /// <summary>
        /// Parse les données photométriques principales (10 lignes de valeurs)
        /// </summary>
        private static int ParsePhotometricData(string[] lines, int startIndex, IESData data)
        {
            int i = startIndex;
            var values = new List<double>();

            // Lire toutes les valeurs numériques jusqu'à avoir au moins 10 valeurs
            while (i < lines.Length && values.Count < 13)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    continue;
                }

                // Parser tous les nombres sur cette ligne
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                    {
                        values.Add(value);
                    }
                }

                i++;
            }

            // Mapper les valeurs selon le format IES standard
            if (values.Count >= 10)
            {
                data.NumberOfLamps = (int)values[0];

                // Si LumensPerLamp n'a pas été défini par _ABSOLUTELUMENS, utiliser values[1]
                if (data.LumensPerLamp <= 0)
                {
                    data.LumensPerLamp = values[1];
                }

                double multiplier = values[2]; // Multiplicateur de candela
                data.NumberOfVerticalAngles = (int)values[3];
                data.NumberOfHorizontalAngles = (int)values[4];
                data.PhotometricType = (PhotometricType)(int)values[5];
                data.UnitsType = (UnitsType)(int)values[6];
                data.Width = values[7];
                data.Length = values[8];
                data.Height = values[9];

                // Valeurs optionnelles
                if (values.Count >= 11)
                    data.BallistFactor = values[10];
                if (values.Count >= 12)
                    data.BallastLampPhotometricFactor = values[11];
                if (values.Count >= 13)
                    data.InputWatts = values[12];

                // Calculer le flux total
                // Si NumberOfLamps est invalide (-1 ou 0), utiliser LumensPerLamp directement
                if (data.NumberOfLamps > 0)
                {
                    data.TotalLumens = data.LumensPerLamp * data.NumberOfLamps;
                }
                else if (data.LumensPerLamp > 0)
                {
                    data.TotalLumens = data.LumensPerLamp;
                    data.NumberOfLamps = 1; // Corriger pour affichage cohérent
                }

                // Calculer l'efficacité
                if (data.InputWatts > 0)
                {
                    data.Efficacy = data.TotalLumens / data.InputWatts;
                }
            }

            return i;
        }

        /// <summary>
        /// Parse les courbes photométriques (angles et valeurs de candela)
        /// </summary>
        private static void ParseCandelaData(string[] lines, int startIndex, IESData data)
        {
            int i = startIndex;
            var allValues = new List<double>();

            // Lire toutes les valeurs restantes
            while (i < lines.Length)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts)
                    {
                        if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                        {
                            allValues.Add(value);
                        }
                    }
                }
                i++;
            }

            int valueIndex = 0;

            // Lire les angles verticaux
            for (int v = 0; v < data.NumberOfVerticalAngles && valueIndex < allValues.Count; v++)
            {
                data.VerticalAngles.Add(allValues[valueIndex++]);
            }

            // Lire les angles horizontaux
            for (int h = 0; h < data.NumberOfHorizontalAngles && valueIndex < allValues.Count; h++)
            {
                data.HorizontalAngles.Add(allValues[valueIndex++]);
            }

            // Lire les valeurs de candela (matrice)
            for (int h = 0; h < data.NumberOfHorizontalAngles; h++)
            {
                var candelaColumn = new List<double>();
                for (int v = 0; v < data.NumberOfVerticalAngles && valueIndex < allValues.Count; v++)
                {
                    candelaColumn.Add(allValues[valueIndex++]);
                }
                data.CandelaValues.Add(candelaColumn);
            }
        }

        /// <summary>
        /// Calcule les statistiques sur les valeurs de candela
        /// </summary>
        private static void CalculateStatistics(IESData data)
        {
            if (data.CandelaValues.Count == 0)
                return;

            var allCandela = data.CandelaValues.SelectMany(col => col).ToList();

            if (allCandela.Count > 0)
            {
                data.MaxCandela = allCandela.Max();
                data.MinCandela = allCandela.Min();
                data.AverageCandela = allCandela.Average();
            }
        }

        /// <summary>
        /// Vérifie si un fichier est un fichier IES valide
        /// </summary>
        public static bool IsValidIESFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                string[] firstLines = File.ReadLines(filePath).Take(20).ToArray();

                // Un fichier IES doit contenir "TILT" dans les premières lignes
                return firstLines.Any(line => line.ToUpper().Contains("TILT"));
            }
            catch
            {
                return false;
            }
        }
    }
}