using MeteoApp.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeteoApp.Services
{
    public partial class MeteoService
    {
        public async Task<List<VilleRecherche>> ObtenirVillesProches(double lat, double lon)
        {
            try
            {
                if (string.IsNullOrEmpty(API_KEY)) return new List<VilleRecherche>();
                string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string url = $"{BASE_URL}/find?lat={latStr}&lon={lonStr}&cnt=15&appid={API_KEY}";
                string json = await _client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var resultats = new List<VilleRecherche>();
                if (doc.RootElement.TryGetProperty("list", out var list))
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        var coord = item.GetProperty("coord");
                        var v = new VilleRecherche
                        {
                            Nom = item.GetProperty("name").GetString(),
                            Pays = item.TryGetProperty("sys", out var sys) && sys.TryGetProperty("country", out var c) ? c.GetString() : "",
                            Latitude = coord.GetProperty("lat").GetDouble(),
                            Longitude = coord.GetProperty("lon").GetDouble()
                        };
                        resultats.Add(v);
                    }
                }

                return resultats;
            }
            catch
            {
                return new List<VilleRecherche>();
            }
        }

        public async Task<List<VilleRecherche>> RechercherVilles(string query)
        {
            try
            {
                string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=50&language=fr&format=json";
                string json = await _client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var resultats = new List<VilleRecherche>();
                if (!doc.RootElement.TryGetProperty("results", out var results)) return resultats;

                foreach (var item in results.EnumerateArray())
                {
                    var v = new VilleRecherche
                    {
                        Nom = item.GetProperty("name").GetString(),
                        Pays = item.TryGetProperty("country", out var country) ? country.GetString() : "",
                        Etat = item.TryGetProperty("admin1", out var state) ? state.GetString() : "",
                        Latitude = item.GetProperty("latitude").GetDouble(),
                        Longitude = item.GetProperty("longitude").GetDouble()
                    };
                    resultats.Add(v);
                }

                return resultats;
            }
            catch
            {
                return new List<VilleRecherche>();
            }
        }
    }
}