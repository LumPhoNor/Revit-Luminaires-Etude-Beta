using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Système d'association automatique entre familles Revit et fichiers IES
    /// </summary>
    public static class AutoMatcher
    {
        /// <summary>
        /// Associe automatiquement les familles Revit avec les fichiers IES
        /// </summary>
        public static Dictionary<string, string> MatchFiles(List<string> familyFiles, List<string> iesFiles)
        {
            var matches = new Dictionary<string, string>();

            foreach (var familyPath in familyFiles)
            {
                string familyName = Path.GetFileNameWithoutExtension(familyPath);
                string normalizedFamilyName = NormalizeName(familyName);

                // Chercher un fichier IES correspondant
                var matchingIES = iesFiles.FirstOrDefault(iesPath =>
                {
                    string iesName = Path.GetFileNameWithoutExtension(iesPath);
                    string normalizedIESName = NormalizeName(iesName);

                    return IsSimilar(normalizedFamilyName, normalizedIESName);
                });

                if (matchingIES != null)
                {
                    matches[familyPath] = matchingIES;
                }
            }

            return matches;
        }

        /// <summary>
        /// Normalise un nom de fichier pour la comparaison
        /// </summary>
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            // Convertir en minuscules
            name = name.ToLower();

            // Supprimer les caractères spéciaux
            name = name.Replace("_", "")
                       .Replace("-", "")
                       .Replace(" ", "")
                       .Replace(".", "");

            return name;
        }

        /// <summary>
        /// Détermine si deux noms sont similaires
        /// </summary>
        private static bool IsSimilar(string name1, string name2)
        {
            // Correspondance exacte
            if (name1 == name2)
                return true;

            // L'un contient l'autre
            if (name1.Contains(name2) || name2.Contains(name1))
                return true;

            // Calcul de similarité par distance de Levenshtein (simplifiée)
            int distance = LevenshteinDistance(name1, name2);
            int maxLength = Math.Max(name1.Length, name2.Length);

            // Similaire si moins de 20% de différence
            double similarity = 1.0 - ((double)distance / maxLength);
            return similarity >= 0.8;
        }

        /// <summary>
        /// Calcule la distance de Levenshtein entre deux chaînes
        /// </summary>
        private static int LevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// Trouve le meilleur fichier IES pour une famille Revit
        /// </summary>
        public static string FindBestMatch(string familyPath, List<string> iesFiles)
        {
            string familyName = Path.GetFileNameWithoutExtension(familyPath);
            string normalizedFamilyName = NormalizeName(familyName);

            string bestMatch = null;
            double bestSimilarity = 0;

            foreach (var iesPath in iesFiles)
            {
                string iesName = Path.GetFileNameWithoutExtension(iesPath);
                string normalizedIESName = NormalizeName(iesName);

                int distance = LevenshteinDistance(normalizedFamilyName, normalizedIESName);
                int maxLength = Math.Max(normalizedFamilyName.Length, normalizedIESName.Length);
                double similarity = 1.0 - ((double)distance / maxLength);

                if (similarity > bestSimilarity && similarity >= 0.8)
                {
                    bestSimilarity = similarity;
                    bestMatch = iesPath;
                }
            }

            return bestMatch;
        }
    }
}