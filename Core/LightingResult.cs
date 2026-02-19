using RevitLightingPlugin.Models;
using System.Collections.Generic;
using System.Net;

namespace RevitLightingPlugin.Core
{
    public class LightingResult
    {
        public string RoomName { get; set; }
        public string RoomNumber { get; set; }
        public double RoomArea { get; set; }
        public int LuminaireCount { get; set; }
        public double AverageIlluminance { get; set; }
        public double UniformityRatio { get; set; } // U₀ (uniformité globale)
        public double LocalUniformity { get; set; } // P4: Uₕ (uniformité locale EN 12464-1)
        public double TotalPower { get; set; }
        public bool IsCompliant { get; set; }
        public string Recommendation { get; set; }
        public List<LuminaireInfo> Luminaires { get; set; }
        public List<GridPoint> GridPoints { get; set; }
        public double MinIlluminance { get; set; }
        public double MaxIlluminance { get; set; }
        public double WorkPlaneHeight { get; set; }
        public double LuminaireCalculatedHeightMeters { get; set; }

        public LightingResult()
        {
            Luminaires = new List<LuminaireInfo>();
            GridPoints = new List<GridPoint>();
        }
    }
}