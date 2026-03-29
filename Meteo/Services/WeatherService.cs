using MeteoApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MeteoApp.Services
{
    public partial class MeteoService
    {
        private static readonly string API_KEY = Environment.GetEnvironmentVariable("API_KEY") ?? "";
        private const string BASE_URL = "https://api.openweathermap.org/data/2.5";

        private static readonly HttpClient _client = new HttpClient();

        public async Task<(VilleInfo ville, List<MeteoJour> previsions)> GetPrevisions(string recherche)
        {
            if (string.IsNullOrEmpty(API_KEY))
                throw new InvalidOperationException(
                    "Clé API manquante.\n\nCréez un fichier .env à côté de l'exécutable avec :\nAPI_KEY=votre_clé_openweathermap");

            string url = $"{BASE_URL}/forecast?q={Uri.EscapeDataString(recherche)}&appid={API_KEY}&units=metric&lang=fr&cnt=40";
            return await FetchPrevisions(url);
        }

        public async Task<(VilleInfo ville, List<MeteoJour> previsions)> GetPrevisionsByCoords(double lat, double lon)
        {
            if (string.IsNullOrEmpty(API_KEY))
                throw new InvalidOperationException(
                    "Clé API manquante.\n\nCréez un fichier .env à côté de l'exécutable avec :\nAPI_KEY=votre_clé_openweathermap");

            string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string url = $"{BASE_URL}/forecast?lat={latStr}&lon={lonStr}&appid={API_KEY}&units=metric&lang=fr&cnt=40";
            return await FetchPrevisions(url);
        }

        public static string GetIconeUrl(string code)
            => $"https://openweathermap.org/img/wn/{code}@2x.png";

        private static string Capitaliser(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}