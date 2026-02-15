using System.Collections.Generic;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Configuration de l'application
    /// </summary>
    public class AppSettings
    {
        public string Version { get; set; } = "2.0";
        public PathSettings Paths { get; set; } = new PathSettings();
        public OptionsSettings Options { get; set; } = new OptionsSettings();
        public StatisticsSettings Statistics { get; set; } = new StatisticsSettings();
    }

    public class PathSettings
    {
        /// <summary>
        /// Dossiers contenant les familles Revit (.rfa)
        /// </summary>
        public List<string> FamilyFolders { get; set; } = new List<string>();

        /// <summary>
        /// Dossiers contenant les fichiers IES (.ies)
        /// </summary>
        public List<string> IESFolders { get; set; } = new List<string>();

        /// <summary>
        /// Chemin de la base de données SQLite
        /// </summary>
        public string DatabasePath { get; set; } = "";
    }

    public class OptionsSettings
    {
        /// <summary>
        /// Scanner automatiquement au démarrage
        /// </summary>
        public bool AutoScanOnStartup { get; set; } = true;

        /// <summary>
        /// Associer automatiquement les fichiers IES avec les familles
        /// </summary>
        public bool AutoMatchFiles { get; set; } = true;

        /// <summary>
        /// Télécharger les fichiers IES manquants depuis les fabricants
        /// </summary>
        public bool DownloadIESIfMissing { get; set; } = false;

        /// <summary>
        /// Surveiller les dossiers pour détecter les nouveaux fichiers
        /// </summary>
        public bool WatchFolders { get; set; } = true;

        /// <summary>
        /// Langue de l'interface
        /// </summary>
        public string Language { get; set; } = "fr-FR";
    }

    public class StatisticsSettings
    {
        public int FamiliesFound { get; set; } = 0;
        public int IESFilesFound { get; set; } = 0;
        public int AutoMatchedFiles { get; set; } = 0;
        public string LastScanDate { get; set; } = "";
    }

    /// <summary>
    /// Résultat d'un scan de fichiers
    /// </summary>
    public class ScanResult
    {
        public List<string> FamilyFiles { get; set; } = new List<string>();
        public List<string> IESFiles { get; set; } = new List<string>();
        public Dictionary<string, string> AutoMatches { get; set; } = new Dictionary<string, string>();
        public List<string> CorruptedFiles { get; set; } = new List<string>();
        public int TotalScanned { get; set; } = 0;
        public string ScanDate { get; set; } = "";
    }
}