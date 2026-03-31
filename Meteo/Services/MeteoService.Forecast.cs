using MeteoApp.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeteoApp.Services
{
    public partial class MeteoService
    {
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
                    Coucher = coucher
                });

                if (previsions.Count >= 5) break;
            }

            return (ville, previsions);
        }
    }
}