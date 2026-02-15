using System.Collections.Generic;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Catégorie d'environnement pour facteur de maintenance (EN 12464-1 Annexe B)
    /// </summary>
    public enum MaintenanceCategory
    {
        VeryClean = 0,      // Bureau, résidentiel - Très propre
        Clean = 1,          // Commerce - Propre
        Normal = 2,         // Industrie propre - Normal
        Dirty = 3,          // Atelier, production - Sale
        VeryDirty = 4       // Environnement hostile - Très sale
    }

    /// <summary>
    /// Type de boîtier luminaire selon indice de protection (EN 12464-1 Annexe B)
    /// </summary>
    public enum LuminaireEnclosure
    {
        SealedIP65 = 0,     // Fermé étanche (IP65+)
        EnclosedIP54 = 1,   // Semi-fermé (IP54)
        OpenIP20 = 2        // Ouvert (IP20)
    }

    /// <summary>
    /// Paramètres de l'analyse d'éclairement
    /// </summary>
    public class AnalysisSettings
    {
        public double GridSpacing { get; set; }
        public List<double> WorkPlaneHeights { get; set; }

        // Propriété alias pour compatibilité
        public double WorkPlaneHeight
        {
            get { return WorkPlaneHeights != null && WorkPlaneHeights.Count > 0 ? WorkPlaneHeights[0] : 0.0; }
            set
            {
                if (WorkPlaneHeights == null)
                    WorkPlaneHeights = new List<double>();
                if (WorkPlaneHeights.Count > 0)
                    WorkPlaneHeights[0] = value;
                else
                    WorkPlaneHeights.Add(value);
            }
        }

        public bool UseIESData { get; set; }
        public string StandardName { get; set; }
        public double MinimumIlluminance { get; set; }
        public double MinimumUniformity { get; set; }

        // P1: Facteur de maintenance configurable (legacy - conservé pour compatibilité)
        public double MaintenanceFactor { get; set; }

        // P3: Facteurs de maintenance variables selon EN 12464-1 Annexe B
        public MaintenanceCategory Environment { get; set; }
        public LuminaireEnclosure LuminaireEnclosureType { get; set; }

        // Table des facteurs de maintenance EN 12464-1 Annexe B
        // Lignes : Type de boîtier (SealedIP65, EnclosedIP54, OpenIP20)
        // Colonnes : Catégorie d'environnement (VeryClean, Clean, Normal, Dirty, VeryDirty)
        private static readonly double[,] MaintenanceFactorTable = new double[,]
        {
            // VeryClean, Clean, Normal, Dirty, VeryDirty
            { 0.90, 0.88, 0.85, 0.80, 0.75 }, // SealedIP65
            { 0.87, 0.85, 0.82, 0.75, 0.70 }, // EnclosedIP54
            { 0.82, 0.80, 0.77, 0.70, 0.67 }  // OpenIP20
        };

        /// <summary>
        /// Obtient le facteur de maintenance selon EN 12464-1 Annexe B
        /// en fonction du type de luminaire et de l'environnement
        /// </summary>
        public double GetMaintenanceFactor()
        {
            return MaintenanceFactorTable[(int)LuminaireEnclosureType, (int)Environment];
        }

        // P2: Calcul du flux indirect
        public bool IncludeIndirectLight { get; set; }
        public double CeilingReflectance { get; set; }
        public double WallReflectance { get; set; }
        public double FloorReflectance { get; set; }

        public AnalysisSettings()
        {
            GridSpacing = 1.0;
            WorkPlaneHeights = new List<double> { 0.0 }; // 🚨 CORRECTION FINALE : Plan au sol (0m) au lieu de 0.8m
            UseIESData = true;
            StandardName = "EN 12464-1";
            MinimumIlluminance = 300;
            MinimumUniformity = 0.4;

            // Valeurs par défaut P1 et P2
            MaintenanceFactor = 0.9; // Par défaut 0.9 (legacy, remplacé par P3)
            IncludeIndirectLight = true;
            CeilingReflectance = 0.70; // Plafond blanc
            WallReflectance = 0.50;    // Murs clairs
            FloorReflectance = 0.20;   // Sol gris

            // 🚨 CORRECTION FINALE : Environnement très propre pour MF = 0.90 (au lieu de Clean = 0.88)
            Environment = MaintenanceCategory.VeryClean;
            LuminaireEnclosureType = LuminaireEnclosure.SealedIP65;
        }
    }
}