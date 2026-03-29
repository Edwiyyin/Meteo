namespace MeteoApp.Models
{
    public class MeteoJour
    {
        public string Date { get; set; }
        public string Description { get; set; }
        public string Icone { get; set; }
        public double Temperature { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Humidite { get; set; }
        public int Nuages { get; set; }
        public double VitesseVent { get; set; }
        public double Pression { get; set; }      
        public double Visibilite { get; set; }    
        public double FeelLike { get; set; }      
        public int UvIndex { get; set; }
        public string Lever { get; set; }
        public string Coucher { get; set; }
    }

    public class VilleFavorite
    {
        public string Nom { get; set; }
        public string Pays { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public override string ToString() => $"{Nom} ({Pays})";
    }

    public class VilleInfo
    {
        public string Nom { get; set; }
        public string Pays { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class VilleRecherche
    {
        public string Nom { get; set; }
        public string Pays { get; set; }
        public string Etat { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            string desc = Nom;
            if (!string.IsNullOrEmpty(Etat) && Etat != Nom) desc += $", {Etat}";
            if (!string.IsNullOrEmpty(Pays)) desc += $" ({Pays})";
            return desc;
        }
    }
}