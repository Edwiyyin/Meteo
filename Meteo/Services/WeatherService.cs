using MeteoApp.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
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

private async Task<(VilleInfo ville, List<MeteoJour> previsions)> FetchPrevisions(string url)
{
    string json = await _client.GetStringAsync(url);

    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;
    var cityEl = root.GetProperty("city");

    var ville = new VilleInfo
    {
        Nom = cityEl.GetProperty("name").GetString(),
        Pays = cityEl.GetProperty("country").GetString(),
        Latitude = cityEl.GetProperty("coord").GetProperty("lat").GetDouble(),
        Longitude = cityEl.GetProperty("coord").GetProperty("lon").GetDouble()
    };

    long sunriseTs = cityEl.GetProperty("sunrise").GetInt64();
    long sunsetTs = cityEl.GetProperty("sunset").GetInt64();
    string lever = DateTimeOffset.FromUnixTimeSeconds(sunriseTs).ToLocalTime().ToString("HH:mm");
    string coucher = DateTimeOffset.FromUnixTimeSeconds(sunsetTs).ToLocalTime().ToString("HH:mm");

    var joursVus = new HashSet<string>();
    var previsions = new List<MeteoJour>();

    foreach (var item in root.GetProperty("list").EnumerateArray())
    {
        string dtTxt = item.GetProperty("dt_txt").GetString();
        string date = dtTxt.Substring(0, 10);
        string heure = dtTxt.Substring(11, 5);

        if (joursVus.Contains(date) && heure != "12:00") continue;
        if (joursVus.Contains(date)) continue;
        joursVus.Add(date);

        var main = item.GetProperty("main");
        var weather = item.GetProperty("weather")[0];
        var wind = item.GetProperty("wind");
        var clouds = item.GetProperty("clouds");

        double visibilite = 10.0;
        if (item.TryGetProperty("visibility", out var vis))
            visibilite = Math.Round(vis.GetDouble() / 1000.0, 1);

        int pop = 0;
        if (item.TryGetProperty("pop", out var popProp))
            pop = (int)(popProp.GetDouble() * 100);

        previsions.Add(new MeteoJour
        {
            Date = date,
            Description = Capitaliser(weather.GetProperty("description").GetString()),
            Icone = weather.GetProperty("icon").GetString(),
            Temperature = Math.Round(main.GetProperty("temp").GetDouble(), 1),
            TempMin = Math.Round(main.GetProperty("temp_min").GetDouble(), 1),
            TempMax = Math.Round(main.GetProperty("temp_max").GetDouble(), 1),
            FeelLike = Math.Round(main.GetProperty("feels_like").GetDouble(), 1),
            Humidite = main.GetProperty("humidity").GetInt32(),
            Pression = main.GetProperty("pressure").GetDouble(),
            VitesseVent = Math.Round(wind.GetProperty("speed").GetDouble() * 3.6, 1),
            Nuages = clouds.GetProperty("all").GetInt32(),
            Visibilite = visibilite,
            Lever = lever,
            Coucher = coucher,
            ProbaPluie = pop
        });

        if (previsions.Count >= 5) break;
    }

    return (ville, previsions);
}

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
        public static string GetIconeUrl(string code)
            => $"https://openweathermap.org/img/wn/{code}@2x.png";

        private static string Capitaliser(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}