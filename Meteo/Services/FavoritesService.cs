using MeteoApp.Models;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;

namespace MeteoApp.Services
{
    public class FavorisService
    {
        private static readonly string DB_FILE = "favoris.db";
        private static readonly string CONNECTION_STRING = $"Data Source={DB_FILE}";

        public FavorisService()
        {
            using var connection = new SqliteConnection(CONNECTION_STRING);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Favoris (
                    Nom TEXT PRIMARY KEY,
                    Pays TEXT,
                    Latitude REAL,
                    Longitude REAL
                );
            ";
            command.ExecuteNonQuery();
        }

        public List<VilleFavorite> Charger()
        {
            var favoris = new List<VilleFavorite>();
            using var connection = new SqliteConnection(CONNECTION_STRING);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Nom, Pays, Latitude, Longitude FROM Favoris";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                favoris.Add(new VilleFavorite
                {
                    Nom = reader.GetString(0),
                    Pays = reader.GetString(1),
                    Latitude = reader.GetDouble(2),
                    Longitude = reader.GetDouble(3)
                });
            }
            return favoris;
        }

        public bool Ajouter(List<VilleFavorite> favoris, string nom, string pays, double lat = 0, double lon = 0)
        {
            if (favoris.Exists(f => f.Nom.ToLower() == nom.ToLower())) return false;

            try
            {
                using var connection = new SqliteConnection(CONNECTION_STRING);
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Favoris (Nom, Pays, Latitude, Longitude)
                    VALUES ($nom, $pays, $lat, $lon)
                ";
                command.Parameters.AddWithValue("$nom", nom);
                command.Parameters.AddWithValue("$pays", pays);
                command.Parameters.AddWithValue("$lat", lat);
                command.Parameters.AddWithValue("$lon", lon);
                command.ExecuteNonQuery();

                favoris.Add(new VilleFavorite { Nom = nom, Pays = pays, Latitude = lat, Longitude = lon });
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public void Supprimer(List<VilleFavorite> favoris, string nom)
        {
            using var connection = new SqliteConnection(CONNECTION_STRING);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Favoris WHERE LOWER(Nom) = LOWER($nom)";
            command.Parameters.AddWithValue("$nom", nom);
            command.ExecuteNonQuery();

            favoris.RemoveAll(f => f.Nom.ToLower() == nom.ToLower());
        }
    }
}