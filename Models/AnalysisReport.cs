using System;
using System.Collections.Generic;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Modèle complet d'un rapport d'analyse d'éclairage
    /// </summary>
    public class AnalysisReport
    {
        // Informations générales
        public string ProjectName { get; set; }
        public string ProjectReference { get; set; }
        public string ClientName { get; set; }
        public DateTime ReportDate { get; set; }
        public string EngineeringFirm { get; set; }
        public string EngineerName { get; set; }

        // Résultats d'analyse
        public List<RoomAnalysisResult> RoomResults { get; set; } = new List<RoomAnalysisResult>();

        // Conformité globale
        public bool IsCompliant { get; set; }
        public List<string> NonComplianceReasons { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();

        // Analyse énergétique
        public EnergyAnalysis EnergyData { get; set; }

        // Luminaires utilisés
        public Dictionary<int, LuminaireUsage> LuminairesUsed { get; set; } = new Dictionary<int, LuminaireUsage>();
    }

    /// <summary>
    /// Résultat d'analyse pour une pièce
    /// </summary>
    public class RoomAnalysisResult
    {
        public string RoomName { get; set; }
        public double RoomArea { get; set; }
        public double RoomHeight { get; set; }

        // Résultats d'éclairement
        public double AverageIlluminance { get; set; }
        public double MinIlluminance { get; set; }
        public double MaxIlluminance { get; set; }
        public double Uniformity { get; set; }

        // Norme appliquée
        public string StandardName { get; set; }
        public int RequiredIlluminance { get; set; }
        public double RequiredUniformity { get; set; }

        // Conformité
        public bool IsIlluminanceCompliant { get; set; }
        public bool IsUniformityCompliant { get; set; }
        public bool IsFullyCompliant { get; set; }

        // Luminaires dans cette pièce
        public List<LuminaireInstance> LuminaireInstances { get; set; } = new List<LuminaireInstance>();

        // Calculs énergétiques
        public double TotalPower { get; set; }
        public double PowerDensity { get; set; }
    }

    /// <summary>
    /// Instance d'un luminaire dans une pièce
    /// </summary>
    public class LuminaireInstance
    {
        public int LuminaireId { get; set; }
        public string LuminaireName { get; set; }
        public int Quantity { get; set; }
        public double TotalFlux { get; set; }
        public double TotalPower { get; set; }
    }

    /// <summary>
    /// Utilisation d'un luminaire dans le projet
    /// </summary>
    public class LuminaireUsage
    {
        public LuminaireInfo Luminaire { get; set; }
        public int TotalQuantity { get; set; }
        public List<string> RoomsUsedIn { get; set; } = new List<string>();
        public double TotalPower { get; set; }
        public double TotalFlux { get; set; }
    }

    /// <summary>
    /// Analyse énergétique du projet
    /// </summary>
    public class EnergyAnalysis
    {
        // Consommation
        public double TotalInstalledPower { get; set; } // W
        public double AnnualConsumption { get; set; } // kWh/an
        public double AnnualCost { get; set; } // €/an

        // Comparaison
        public double HalogenEquivalentPower { get; set; } // W
        public double HalogenAnnualConsumption { get; set; } // kWh/an
        public double HalogenAnnualCost { get; set; } // €/an

        // Économies
        public double PowerSavings { get; set; } // W
        public double EnergySavings { get; set; } // kWh/an
        public double CostSavings { get; set; } // €/an
        public double CO2Savings { get; set; } // kg CO2/an

        // ROI
        public double InitialInvestment { get; set; } // €
        public double PaybackPeriod { get; set; } // années

        // Paramètres de calcul
        public double HoursPerDay { get; set; } = 10;
        public double DaysPerYear { get; set; } = 250;
        public double ElectricityCost { get; set; } = 0.15; // €/kWh
    }
}