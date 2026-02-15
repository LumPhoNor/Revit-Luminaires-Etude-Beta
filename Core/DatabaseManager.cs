using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using RevitLightingPlugin.Models;

namespace RevitLightingPlugin.Core
{
    public class DatabaseManager
    {
        private string _connectionString;
        private const int CURRENT_DB_VERSION = 2; // VERSION 2.0

        public DatabaseManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string pluginFolder = Path.Combine(appDataPath, "RevitLightingPlugin");

            if (!Directory.Exists(pluginFolder))
            {
                Directory.CreateDirectory(pluginFolder);
            }

            string dbPath = Path.Combine(pluginFolder, "LuminairesCatalog.db");
            _connectionString = $"Data Source={dbPath};Version=3;";

            InitializeDatabase();
            MigrateDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Table de version (pour les migrations)
                string createVersionTable = @"
                    CREATE TABLE IF NOT EXISTS DatabaseVersion (
                        Version INTEGER PRIMARY KEY
                    )";
                using (var command = new SQLiteCommand(createVersionTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Table principale des luminaires (VERSION 2.0)
                string createTable = @"
                    CREATE TABLE IF NOT EXISTS Luminaires (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Fabricant TEXT NOT NULL,
                        Nom TEXT NOT NULL,
                        Reference TEXT NOT NULL,
                        FluxLumineux INTEGER NOT NULL,
                        Puissance INTEGER NOT NULL,
                        Efficacite INTEGER NOT NULL,
                        TemperatureCouleur INTEGER NOT NULL,
                        CheminFamilleRevit TEXT,
                        CheminFichierIES TEXT,
                        TypeLuminaire TEXT,
                        CategorieUsage TEXT,
                        IndiceProtection TEXT,
                        DateCreation TEXT,
                        DateModification TEXT
                    )";

                using (var command = new SQLiteCommand(createTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Vérifier si la base est vide
                string checkEmpty = "SELECT COUNT(*) FROM Luminaires";
                using (var command = new SQLiteCommand(checkEmpty, connection))
                {
                    long count = (long)command.ExecuteScalar();
                    if (count == 0)
                    {
                        InsertDefaultLuminaires(connection);
                    }
                }
            }
        }

        /// <summary>
        /// Migre la base de données vers la version 2.0
        /// </summary>
        private void MigrateDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                int currentVersion = GetDatabaseVersion(connection);

                if (currentVersion < CURRENT_DB_VERSION)
                {
                    // Migration vers version 2
                    if (currentVersion < 2)
                    {
                        MigrateToVersion2(connection);
                    }

                    // Mettre à jour la version
                    UpdateDatabaseVersion(connection, CURRENT_DB_VERSION);
                }
            }
        }

        /// <summary>
        /// Migration vers la version 2.0 (ajout des nouvelles colonnes)
        /// </summary>
        private void MigrateToVersion2(SQLiteConnection connection)
        {
            try
            {
                // Vérifier si les colonnes existent déjà
                var existingColumns = GetTableColumns(connection, "Luminaires");

                // Ajouter les nouvelles colonnes si elles n'existent pas
                if (!existingColumns.Contains("CheminFamilleRevit"))
                {
                    string addColumn1 = "ALTER TABLE Luminaires ADD COLUMN CheminFamilleRevit TEXT";
                    using (var command = new SQLiteCommand(addColumn1, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("CheminFichierIES"))
                {
                    string addColumn2 = "ALTER TABLE Luminaires ADD COLUMN CheminFichierIES TEXT";
                    using (var command = new SQLiteCommand(addColumn2, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("TypeLuminaire"))
                {
                    string addColumn3 = "ALTER TABLE Luminaires ADD COLUMN TypeLuminaire TEXT";
                    using (var command = new SQLiteCommand(addColumn3, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("CategorieUsage"))
                {
                    string addColumn4 = "ALTER TABLE Luminaires ADD COLUMN CategorieUsage TEXT";
                    using (var command = new SQLiteCommand(addColumn4, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("IndiceProtection"))
                {
                    string addColumn5 = "ALTER TABLE Luminaires ADD COLUMN IndiceProtection TEXT";
                    using (var command = new SQLiteCommand(addColumn5, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("DateCreation"))
                {
                    string addColumn6 = "ALTER TABLE Luminaires ADD COLUMN DateCreation TEXT";
                    using (var command = new SQLiteCommand(addColumn6, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                if (!existingColumns.Contains("DateModification"))
                {
                    string addColumn7 = "ALTER TABLE Luminaires ADD COLUMN DateModification TEXT";
                    using (var command = new SQLiteCommand(addColumn7, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                System.Diagnostics.Debug.WriteLine("Migration vers version 2.0 réussie !");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la migration : {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère la liste des colonnes d'une table
        /// </summary>
        private List<string> GetTableColumns(SQLiteConnection connection, string tableName)
        {
            var columns = new List<string>();

            string query = $"PRAGMA table_info({tableName})";
            using (var command = new SQLiteCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(1)); // Colonne "name"
                }
            }

            return columns;
        }

        /// <summary>
        /// Récupère la version actuelle de la base de données
        /// </summary>
        private int GetDatabaseVersion(SQLiteConnection connection)
        {
            try
            {
                string query = "SELECT MAX(Version) FROM DatabaseVersion";
                using (var command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Met à jour la version de la base de données
        /// </summary>
        private void UpdateDatabaseVersion(SQLiteConnection connection, int version)
        {
            string query = "INSERT OR REPLACE INTO DatabaseVersion (Version) VALUES (@Version)";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Version", version);
                command.ExecuteNonQuery();
            }
        }

        private void InsertDefaultLuminaires(SQLiteConnection connection)
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var luminaires = new[]
            {
                new { Fabricant = "Osram", Nom = "LED Downlight 20W", Reference = "DL-20", Flux = 2000, Puissance = 20, Efficacite = 100, Temperature = 3000, Type = "Encastré", Categorie = "Bureau,Commerce", IP = "IP20" },
                new { Fabricant = "Philips", Nom = "LED Panel 600x600", Reference = "RC132V", Flux = 3600, Puissance = 36, Efficacite = 100, Temperature = 4000, Type = "Plafonnier", Categorie = "Bureau", IP = "IP20" },
                new { Fabricant = "Philips", Nom = "Tube LED T8 18W", Reference = "MASTER-18", Flux = 1800, Puissance = 18, Efficacite = 100, Temperature = 4000, Type = "Tube", Categorie = "Industrie,Commerce", IP = "IP20" },
                new { Fabricant = "Osram", Nom = "Spot LED 10W", Reference = "PAR30-10", Flux = 800, Puissance = 10, Efficacite = 80, Temperature = 3000, Type = "Spot", Categorie = "Commerce", IP = "IP20" },
                new { Fabricant = "Philips", Nom = "Plafonnier LED 40W", Reference = "DN135B", Flux = 4000, Puissance = 40, Efficacite = 100, Temperature = 4000, Type = "Plafonnier", Categorie = "Bureau,Industrie", IP = "IP44" },
                new { Fabricant = "Osram", Nom = "Réglette LED 36W", Reference = "LN236-36", Flux = 3600, Puissance = 36, Efficacite = 100, Temperature = 6500, Type = "Réglette", Categorie = "Industrie", IP = "IP65" },
                new { Fabricant = "Zumtobel", Nom = "Suspension LED 50W", Reference = "PANOS-50", Flux = 5000, Puissance = 50, Efficacite = 100, Temperature = 4000, Type = "Suspension", Categorie = "Commerce", IP = "IP20" },
                new { Fabricant = "Reggiani", Nom = "Applique LED 15W", Reference = "WL-15", Flux = 1500, Puissance = 15, Efficacite = 100, Temperature = 3000, Type = "Applique", Categorie = "Commerce", IP = "IP20" },
                new { Fabricant = "iGuzzini", Nom = "Encastré LED 25W", Reference = "FRAME-25", Flux = 2500, Puissance = 25, Efficacite = 100, Temperature = 4000, Type = "Encastré", Categorie = "Bureau", IP = "IP20" },
                new { Fabricant = "Philips", Nom = "Projecteur LED 100W", Reference = "CORELINE-100", Flux = 10000, Puissance = 100, Efficacite = 100, Temperature = 5000, Type = "Projecteur", Categorie = "Industrie,Extérieur", IP = "IP65" }
            };

            foreach (var lum in luminaires)
            {
                string insert = @"
                    INSERT INTO Luminaires 
                    (Fabricant, Nom, Reference, FluxLumineux, Puissance, Efficacite, TemperatureCouleur, 
                     TypeLuminaire, CategorieUsage, IndiceProtection, DateCreation, DateModification)
                    VALUES 
                    (@Fabricant, @Nom, @Reference, @Flux, @Puissance, @Efficacite, @Temperature,
                     @Type, @Categorie, @IP, @Date, @Date)";

                using (var command = new SQLiteCommand(insert, connection))
                {
                    command.Parameters.AddWithValue("@Fabricant", lum.Fabricant);
                    command.Parameters.AddWithValue("@Nom", lum.Nom);
                    command.Parameters.AddWithValue("@Reference", lum.Reference);
                    command.Parameters.AddWithValue("@Flux", lum.Flux);
                    command.Parameters.AddWithValue("@Puissance", lum.Puissance);
                    command.Parameters.AddWithValue("@Efficacite", lum.Efficacite);
                    command.Parameters.AddWithValue("@Temperature", lum.Temperature);
                    command.Parameters.AddWithValue("@Type", lum.Type);
                    command.Parameters.AddWithValue("@Categorie", lum.Categorie);
                    command.Parameters.AddWithValue("@IP", lum.IP);
                    command.Parameters.AddWithValue("@Date", currentDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<LuminaireInfo> GetAllLuminaires()
        {
            var luminaires = new List<LuminaireInfo>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM Luminaires ORDER BY Id";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        luminaires.Add(new LuminaireInfo
                        {
                            Id = reader.GetInt32(0),
                            Fabricant = reader.GetString(1),
                            Nom = reader.GetString(2),
                            Reference = reader.GetString(3),
                            FluxLumineux = reader.GetInt32(4),
                            Puissance = reader.GetInt32(5),
                            Efficacite = reader.GetInt32(6),
                            TemperatureCouleur = reader.GetInt32(7),
                            CheminFamilleRevit = reader.IsDBNull(8) ? "" : reader.GetString(8),
                            CheminFichierIES = reader.IsDBNull(9) ? "" : reader.GetString(9),
                            TypeLuminaire = reader.IsDBNull(10) ? "" : reader.GetString(10),
                            CategorieUsage = reader.IsDBNull(11) ? "" : reader.GetString(11),
                            IndiceProtection = reader.IsDBNull(12) ? "" : reader.GetString(12)
                        });
                    }
                }
            }

            return luminaires;
        }

        public void AddLuminaire(LuminaireInfo luminaire)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string currentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string insert = @"
                    INSERT INTO Luminaires 
                    (Fabricant, Nom, Reference, FluxLumineux, Puissance, Efficacite, TemperatureCouleur,
                     CheminFamilleRevit, CheminFichierIES, TypeLuminaire, CategorieUsage, IndiceProtection,
                     DateCreation, DateModification)
                    VALUES 
                    (@Fabricant, @Nom, @Reference, @Flux, @Puissance, @Efficacite, @Temperature,
                     @CheminFamille, @CheminIES, @Type, @Categorie, @IP, @Date, @Date)";

                using (var command = new SQLiteCommand(insert, connection))
                {
                    command.Parameters.AddWithValue("@Fabricant", luminaire.Fabricant);
                    command.Parameters.AddWithValue("@Nom", luminaire.Nom);
                    command.Parameters.AddWithValue("@Reference", luminaire.Reference);
                    command.Parameters.AddWithValue("@Flux", luminaire.FluxLumineux);
                    command.Parameters.AddWithValue("@Puissance", luminaire.Puissance);
                    command.Parameters.AddWithValue("@Efficacite", luminaire.Efficacite);
                    command.Parameters.AddWithValue("@Temperature", luminaire.TemperatureCouleur);
                    command.Parameters.AddWithValue("@CheminFamille", luminaire.CheminFamilleRevit ?? "");
                    command.Parameters.AddWithValue("@CheminIES", luminaire.CheminFichierIES ?? "");
                    command.Parameters.AddWithValue("@Type", luminaire.TypeLuminaire ?? "");
                    command.Parameters.AddWithValue("@Categorie", luminaire.CategorieUsage ?? "");
                    command.Parameters.AddWithValue("@IP", luminaire.IndiceProtection ?? "");
                    command.Parameters.AddWithValue("@Date", currentDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateLuminaire(LuminaireInfo luminaire)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string currentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string update = @"
                    UPDATE Luminaires 
                    SET Fabricant = @Fabricant,
                        Nom = @Nom,
                        Reference = @Reference,
                        FluxLumineux = @Flux,
                        Puissance = @Puissance,
                        Efficacite = @Efficacite,
                        TemperatureCouleur = @Temperature,
                        CheminFamilleRevit = @CheminFamille,
                        CheminFichierIES = @CheminIES,
                        TypeLuminaire = @Type,
                        CategorieUsage = @Categorie,
                        IndiceProtection = @IP,
                        DateModification = @Date
                    WHERE Id = @Id";

                using (var command = new SQLiteCommand(update, connection))
                {
                    command.Parameters.AddWithValue("@Id", luminaire.Id);
                    command.Parameters.AddWithValue("@Fabricant", luminaire.Fabricant);
                    command.Parameters.AddWithValue("@Nom", luminaire.Nom);
                    command.Parameters.AddWithValue("@Reference", luminaire.Reference);
                    command.Parameters.AddWithValue("@Flux", luminaire.FluxLumineux);
                    command.Parameters.AddWithValue("@Puissance", luminaire.Puissance);
                    command.Parameters.AddWithValue("@Efficacite", luminaire.Efficacite);
                    command.Parameters.AddWithValue("@Temperature", luminaire.TemperatureCouleur);
                    command.Parameters.AddWithValue("@CheminFamille", luminaire.CheminFamilleRevit ?? "");
                    command.Parameters.AddWithValue("@CheminIES", luminaire.CheminFichierIES ?? "");
                    command.Parameters.AddWithValue("@Type", luminaire.TypeLuminaire ?? "");
                    command.Parameters.AddWithValue("@Categorie", luminaire.CategorieUsage ?? "");
                    command.Parameters.AddWithValue("@IP", luminaire.IndiceProtection ?? "");
                    command.Parameters.AddWithValue("@Date", currentDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteLuminaire(int id)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string delete = "DELETE FROM Luminaires WHERE Id = @Id";

                using (var command = new SQLiteCommand(delete, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}