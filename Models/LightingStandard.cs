using System.Collections.Generic;

namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Normes d'éclairement selon EN 12464-1
    /// </summary>
    public class LightingStandard
    {
        public string StandardName { get; set; }
        public string RoomType { get; set; }
        public double MinimumIlluminance { get; set; }
        public double MinimumUniformity { get; set; }

        public LightingStandard()
        {
            StandardName = "EN 12464-1";
            RoomType = "";
            MinimumIlluminance = 300;
            MinimumUniformity = 0.4;
        }

        /// <summary>
        /// Obtenir les exigences pour un type de pièce
        /// </summary>
        public static LightingStandard GetStandard(string standardName, string roomType)
        {
            var standards = new Dictionary<string, LightingStandard>
            {
                { "Bureau", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Bureau", MinimumIlluminance = 500, MinimumUniformity = 0.6 } },
                { "Salle de réunion", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Salle de réunion", MinimumIlluminance = 500, MinimumUniformity = 0.6 } },
                { "Couloir", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Couloir", MinimumIlluminance = 100, MinimumUniformity = 0.4 } },
                { "Escalier", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Escalier", MinimumIlluminance = 150, MinimumUniformity = 0.4 } },
                { "Sanitaires", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Sanitaires", MinimumIlluminance = 200, MinimumUniformity = 0.4 } },
                { "Parking", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Parking", MinimumIlluminance = 75, MinimumUniformity = 0.4 } },
                { "Entrepôt", new LightingStandard { StandardName = "EN 12464-1", RoomType = "Entrepôt", MinimumIlluminance = 200, MinimumUniformity = 0.4 } }
            };

            if (standards.ContainsKey(roomType))
                return standards[roomType];

            // Par défaut
            return new LightingStandard
            {
                StandardName = standardName,
                RoomType = roomType,
                MinimumIlluminance = 300,
                MinimumUniformity = 0.4
            };
        }
    }
}