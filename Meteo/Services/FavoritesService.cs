using MeteoApp.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MeteoApp.Services
{
    public class FavorisService
    {
        private static readonly string FICHIER = "favoris.json";

        public List<VilleFavorite> Charger()
        {
            if (!File.Exists(FICHIER)) return new List<VilleFavorite>();
            string json = File.ReadAllText(FICHIER);
            return JsonSerializer.Deserialize<List<VilleFavorite>>(json) ?? new List<VilleFavorite>();
        }

        public void Sauvegarder(List<VilleFavorite> favoris)
        {
            string json = JsonSerializer.Serialize(favoris, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FICHIER, json);
        }

        public bool Ajouter(List<VilleFavorite> favoris, string nom, string pays, double lat = 0, double lon = 0)
        {
            if (favoris.Exists(f => f.Nom.ToLower() == nom.ToLower())) return false;
            favoris.Add(new VilleFavorite { Nom = nom, Pays = pays, Latitude = lat, Longitude = lon });
            Sauvegarder(favoris);
            return true;
        }

        public void Supprimer(List<VilleFavorite> favoris, string nom)
        {
            favoris.RemoveAll(f => f.Nom.ToLower() == nom.ToLower());
            Sauvegarder(favoris);
        }
    }
}