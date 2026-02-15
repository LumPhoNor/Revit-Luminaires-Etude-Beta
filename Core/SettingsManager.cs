using System;
using System.IO;
using Newtonsoft.Json;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    /// <summary>
    /// Gestionnaire de configuration de l'application
    /// </summary>
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static readonly object _lock = new object();

        private string _settingsFilePath;
        private AppSettings _settings;

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SettingsManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private SettingsManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string pluginFolder = Path.Combine(appDataPath, "RevitLightingPlugin");

            if (!Directory.Exists(pluginFolder))
            {
                Directory.CreateDirectory(pluginFolder);
            }

            _settingsFilePath = Path.Combine(pluginFolder, "settings.json");
            LoadSettings();
        }

        /// <summary>
        /// Charge les paramètres depuis le fichier JSON
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json);
                }
                else
                {
                    // Créer les paramètres par défaut
                    _settings = CreateDefaultSettings();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors du chargement de la configuration :\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                _settings = CreateDefaultSettings();
            }
        }

        /// <summary>
        /// Sauvegarde les paramètres dans le fichier JSON
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erreur lors de la sauvegarde de la configuration :\n{ex.Message}",
                    "Erreur",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// Crée une configuration par défaut
        /// </summary>
        private AppSettings CreateDefaultSettings()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string databasePath = Path.Combine(appDataPath, "RevitLightingPlugin");

            var settings = new AppSettings();
            settings.Paths.DatabasePath = databasePath;

            // Ajouter des chemins par défaut si disponibles
            string revitUserPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Revit", "Families", "Lighting"
            );

            if (Directory.Exists(revitUserPath))
            {
                settings.Paths.FamilyFolders.Add(revitUserPath);
            }

            // Chemin ProgramData Autodesk
            string revitProgramDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Autodesk", "RVT 2026", "Libraries", "France", "Eclairage"
            );

            if (Directory.Exists(revitProgramDataPath))
            {
                settings.Paths.FamilyFolders.Add(revitProgramDataPath);
            }

            return settings;
        }

        /// <summary>
        /// Obtient les paramètres actuels
        /// </summary>
        public AppSettings GetSettings()
        {
            return _settings;
        }

        /// <summary>
        /// Met à jour les paramètres
        /// </summary>
        public void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
            SaveSettings();
        }

        /// <summary>
        /// Réinitialise les paramètres par défaut
        /// </summary>
        public void ResetToDefault()
        {
            _settings = CreateDefaultSettings();
            SaveSettings();
        }

        /// <summary>
        /// Ajoute un dossier de familles
        /// </summary>
        public void AddFamilyFolder(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                if (!_settings.Paths.FamilyFolders.Contains(path))
                {
                    _settings.Paths.FamilyFolders.Add(path);
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Supprime un dossier de familles
        /// </summary>
        public void RemoveFamilyFolder(string path)
        {
            if (_settings.Paths.FamilyFolders.Contains(path))
            {
                _settings.Paths.FamilyFolders.Remove(path);
                SaveSettings();
            }
        }

        /// <summary>
        /// Ajoute un dossier IES
        /// </summary>
        public void AddIESFolder(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                if (!_settings.Paths.IESFolders.Contains(path))
                {
                    _settings.Paths.IESFolders.Add(path);
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Supprime un dossier IES
        /// </summary>
        public void RemoveIESFolder(string path)
        {
            if (_settings.Paths.IESFolders.Contains(path))
            {
                _settings.Paths.IESFolders.Remove(path);
                SaveSettings();
            }
        }

        /// <summary>
        /// Met à jour les statistiques
        /// </summary>
        public void UpdateStatistics(int familiesFound, int iesFilesFound, int autoMatched)
        {
            _settings.Statistics.FamiliesFound = familiesFound;
            _settings.Statistics.IESFilesFound = iesFilesFound;
            _settings.Statistics.AutoMatchedFiles = autoMatched;
            _settings.Statistics.LastScanDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            SaveSettings();
        }
    }
}