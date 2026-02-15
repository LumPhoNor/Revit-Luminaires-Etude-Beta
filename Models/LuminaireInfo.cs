namespace RevitLightingPlugin.Models
{
    /// <summary>
    /// Informations sur un luminaire
    /// </summary>
    public class LuminaireInfo
    {
        public int Id { get; set; }
        public string Fabricant { get; set; }
        public string Nom { get; set; }
        public string Reference { get; set; }
        public int FluxLumineux { get; set; }
        public int Puissance { get; set; }
        public int Efficacite { get; set; }
        public int TemperatureCouleur { get; set; }

        // Nouvelles propriétés VERSION 2.0
        public string CheminFamilleRevit { get; set; }
        public string CheminFichierIES { get; set; }
        public string TypeLuminaire { get; set; }
        public string CategorieUsage { get; set; }
        public string IndiceProtection { get; set; }

        /// <summary>
        /// Indique si le luminaire a une famille Revit associée
        /// </summary>
        public bool HasFamilyFile
        {
            get { return !string.IsNullOrEmpty(CheminFamilleRevit); }
        }

        /// <summary>
        /// Indique si le luminaire a un fichier IES associé
        /// </summary>
        public bool HasIESFile
        {
            get { return !string.IsNullOrEmpty(CheminFichierIES); }
        }

        /// <summary>
        /// Retourne une description complète du luminaire
        /// </summary>
        public string Description
        {
            get { return $"{Fabricant} - {Nom} ({Reference})"; }
        }
    }
}