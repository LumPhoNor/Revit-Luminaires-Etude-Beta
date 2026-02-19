using System.Collections.Generic;
using RevitLightingPlugin.Core;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Résultat d'analyse pour une hauteur de plan de travail
    /// </summary>
    public class HeightAnalysisResult
    {
        public double WorkPlaneHeight { get; set; }
        public double AverageIlluminance { get; set; }
        public double MinIlluminance { get; set; }
        public double MaxIlluminance { get; set; }
        public double Uniformity { get; set; } // U₀ (uniformité globale)
        public double LocalUniformity { get; set; } // P4: Uₕ (uniformité locale)
        public bool MeetsStandard { get; set; }
        public string GridMapPath { get; set; }
        public string Heatmap3DPath { get; set; }
        public List<GridPoint> GridPoints { get; set; }

        public HeightAnalysisResult()
        {
            GridPoints = new List<GridPoint>();
        }
    }

    /// <summary>
    /// Résultat de calcul pour une pièce
    /// </summary>
    public class CalculationResult
    {
        // Identifiant de la pièce
        public string RoomId { get; set; }

        // PROPRIÉTÉS EN ANGLAIS (pour compatibilité avec anciens fichiers)
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public double RoomArea { get; set; }
        public int LuminaireCount { get; set; }
        public double AverageIlluminance { get; set; }
        public double MinIlluminance { get; set; }
        public double MaxIlluminance { get; set; }
        public double Uniformity { get; set; } // U₀ (uniformité globale)
        public double LocalUniformity { get; set; } // P4: Uₕ (uniformité locale EN 12464-1)
        public bool MeetsStandard { get; set; }



        // PROPRIÉTÉS EN FRANÇAIS (pour UI française)
        public string NomPiece
        {
            get { return RoomName; }
            set { RoomName = value; }
        }

        public double SurfacePiece
        {
            get { return RoomArea; }
            set { RoomArea = value; }
        }

        public double HauteurPiece { get; set; }

        public double EclairementMoyen
        {
            get { return AverageIlluminance; }
            set { AverageIlluminance = value; }
        }

        public double EclairementMin
        {
            get { return MinIlluminance; }
            set { MinIlluminance = value; }
        }

        public double EclairementMax
        {
            get { return MaxIlluminance; }
            set { MaxIlluminance = value; }
        }

        public double Uniformite
        {
            get { return Uniformity; }
            set { Uniformity = value; }
        }

        public string NormeAppliquee { get; set; }

        public int EclairementRequis { get; set; }

        // NOUVEAU : Uniformité requise selon EN 12464-1
        public double UniformiteRequise { get; set; }

        // NOUVEAU : Type d'activité selon EN 12464-1
        public string TypeActivite { get; set; }

        public int NombreLuminaires
        {
            get { return LuminaireCount; }
            set { LuminaireCount = value; }
        }

        public double PuissanceTotale { get; set; }

        public double DensitePuissance { get; set; }

        public bool EstConforme
        {
            get { return MeetsStandard; }
            set { MeetsStandard = value; }
        }

        public string EstConformeTexte
        {
            get { return EstConforme ? "✓ Oui" : "✗ Non"; }
        }

        public string Remarques { get; set; }

        // NOUVEAU : Chemins des images exportées (vues 2D/3D)
        public string PlanImagePath { get; set; }
        public string View3DImagePath { get; set; }
        public string GridMapPath { get; set; }

        // Liste des luminaires utilisés dans cette pièce
        public List<LuminaireUsageInfo> LuminairesUtilises { get; set; }

        // Hauteur calculée de la source lumineuse (en mètres)
        public double LuminaireCalculatedHeightMeters { get; set; }

        // NOUVEAU : Résultats par hauteur de plan de travail
        public List<HeightAnalysisResult> HeightResults { get; set; }

        // NOUVEAU : Espacement de la grille utilisé
        public double GridSpacing { get; set; }

        public CalculationResult()
        {
            LuminairesUtilises = new List<LuminaireUsageInfo>();
            HeightResults = new List<HeightAnalysisResult>();
            UniformiteRequise = 0.60; // Valeur par défaut
            TypeActivite = "Non spécifié";
        }
    }

    /// <summary>
    /// Informations d'utilisation d'un luminaire dans une pièce
    /// </summary>
    public class LuminaireUsageInfo
    {
        public string FamilyName { get; set; }
        public string TypeName { get; set; }
        public int Quantity { get; set; }
        public double FluxLumineux { get; set; }
        public double Puissance { get; set; }
        public string Fabricant { get; set; }
        public int TemperatureCouleur { get; set; }
        public string Reference { get; set; }
        public double CalculatedHeightMeters { get; set; }

        // NOUVEAU : Propriété pour le flux total
        public double TotalFlux
        {
            get { return FluxLumineux * Quantity; }
        }

        // NOUVEAU : Propriété pour la puissance totale
        public double TotalPower
        {
            get { return Puissance * Quantity; }
        }
    }
}