using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Scanner de fichiers pour détecter les familles Revit et fichiers IES
    /// </summary>
    public class FileScanner
    {
        /// <summary>
        /// Événement pour reporter la progression du scan
        /// </summary>
        public event Action<int, int, string> ProgressChanged;

        /// <summary>
        /// Scanne tous les dossiers configurés
        /// </summary>
        public ScanResult ScanAll()
        {
            var settings = SettingsManager.Instance.GetSettings();
            var result = new ScanResult
            {
                ScanDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };

            int totalFolders = settings.Paths.FamilyFolders.Count + settings.Paths.IESFolders.Count;
            int currentFolder = 0;

            // Scanner les dossiers de familles
            foreach (string folder in settings.Paths.FamilyFolders)
            {
                currentFolder++;
                ReportProgress(currentFolder, totalFolders, $"Scan des familles : {folder}");

                if (Directory.Exists(folder))
                {
                    var familyFiles = ScanFamilyFolder(folder);
                    result.FamilyFiles.AddRange(familyFiles);
                }
            }

            // Scanner les dossiers IES
            foreach (string folder in settings.Paths.IESFolders)
            {
                currentFolder++;
                ReportProgress(currentFolder, totalFolders, $"Scan des fichiers IES : {folder}");

                if (Directory.Exists(folder))
                {
                    var iesFiles = ScanIESFolder(folder);
                    result.IESFiles.AddRange(iesFiles);
                }
            }

            result.TotalScanned = result.FamilyFiles.Count + result.IESFiles.Count;

            // Association automatique si activée
            if (settings.Options.AutoMatchFiles)
            {
                result.AutoMatches = AutoMatcher.MatchFiles(result.FamilyFiles, result.IESFiles);
            }

            // Mettre à jour les statistiques
            SettingsManager.Instance.UpdateStatistics(
                result.FamilyFiles.Count,
                result.IESFiles.Count,
                result.AutoMatches.Count
            );

            return result;
        }

        /// <summary>
        /// Scanne un dossier à la recherche de familles Revit (.rfa)
        /// </summary>
        private List<string> ScanFamilyFolder(string folderPath)
        {
            var familyFiles = new List<string>();

            try
            {
                // Recherche récursive des fichiers .rfa
                var files = Directory.GetFiles(folderPath, "*.rfa", SearchOption.AllDirectories);
                familyFiles.AddRange(files);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur scan famille dans {folderPath}: {ex.Message}");
            }

            return familyFiles;
        }

        /// <summary>
        /// Scanne un dossier à la recherche de fichiers IES (.ies)
        /// </summary>
        private List<string> ScanIESFolder(string folderPath)
        {
            var iesFiles = new List<string>();

            try
            {
                // Recherche récursive des fichiers .ies
                var files = Directory.GetFiles(folderPath, "*.ies", SearchOption.AllDirectories);
                iesFiles.AddRange(files);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur scan IES dans {folderPath}: {ex.Message}");
            }

            return iesFiles;
        }

        /// <summary>
        /// Reporte la progression du scan
        /// </summary>
        private void ReportProgress(int current, int total, string message)
        {
            ProgressChanged?.Invoke(current, total, message);
        }

        /// <summary>
        /// Vérifie si un fichier IES est valide
        /// </summary>
        public bool IsValidIESFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Lire les premières lignes pour vérifier le format IES
                var lines = File.ReadLines(filePath).Take(10).ToList();

                // Un fichier IES doit commencer par "IESNA" ou "IESNA:"
                return lines.Any(line => line.Trim().StartsWith("IESNA", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie si une famille Revit est valide
        /// </summary>
        public bool IsValidFamilyFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Vérifier l'extension
                string extension = Path.GetExtension(filePath).ToLower();
                return extension == ".rfa";
            }
            catch
            {
                return false;
            }
        }
    }
}