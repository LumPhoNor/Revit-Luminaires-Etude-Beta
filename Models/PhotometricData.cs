using System.Collections.Generic;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Données photométriques extraites d'un fichier IES
    /// </summary>
    public class PhotometricData
    {
        public string Manufacturer { get; set; }
        public string Reference { get; set; }
        public double TotalFlux { get; set; }
        public string LuminaireType { get; set; }
        public int CRI { get; set; }
        public int ColorTemperature { get; set; }
        public double DownwardFlux { get; set; }
        public double UpwardFlux { get; set; }
        public double Angle50 { get; set; }
        public double Angle10 { get; set; }
        public List<double[]> PolarData { get; set; } = new List<double[]>();

        /// <summary>
        /// Pourcentage de flux vers le bas
        /// </summary>
        public double DownwardPercentage
        {
            get
            {
                if (TotalFlux <= 0) return 0;
                return (DownwardFlux / TotalFlux) * 100;
            }
        }

        /// <summary>
        /// Pourcentage de flux vers le haut
        /// </summary>
        public double UpwardPercentage
        {
            get
            {
                if (TotalFlux <= 0) return 0;
                return (UpwardFlux / TotalFlux) * 100;
            }
        }
    }
}