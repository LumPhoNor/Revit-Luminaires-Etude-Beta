namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Données extraites d'un fichier IES
    /// </summary>
    public class IESData
    {
        public string FileName { get; set; }
        public string Manufacturer { get; set; }
        public string CatalogNumber { get; set; }
        public string LuminaireDescription { get; set; }
        public string Tilt { get; set; }
        public int NumberOfLamps { get; set; }
        public double LumensPerLamp { get; set; }
        public double TotalLumens { get; set; }
        public double InputWatts { get; set; }

        public IESData()
        {
            FileName = "";
            Manufacturer = "";
            CatalogNumber = "";
            LuminaireDescription = "";
            Tilt = "TILT=NONE";
            NumberOfLamps = 1;
            LumensPerLamp = 0;
            TotalLumens = 0;
            InputWatts = 0;
        }
    }
}