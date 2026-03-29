using MeteoApp.Models;
using MeteoApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MeteoApp.Forms
{
    public class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; }
    }

    public partial class MainForm : Form
    {
        private readonly MeteoService _meteo = new MeteoService();
        private readonly FavorisService _favorisService = new FavorisService();
        private List<VilleFavorite> _favoris;
        private VilleInfo _villeActuelle;
        private List<MeteoJour> _prevActuelles;
        private MeteoJour _jourDetail;
        private bool _modeSombre = true;
        private List<string> _historique = new List<string>();
        private int _hoveredChartIndex = -1;
        private RectangleF[] _chartZones;
        private string _hoveredVille = null;
        private double _globeCenterLat = 46.0;
        private double _globeCenterLon = 2.0;
        private double _globeZoom = 1.0;
        private bool _isDraggingGlobe = false;
        private Point _lastMousePos;
        private List<float[]> _mapPolygons = new List<float[]>();
        private bool _mapDetalise = false;
        private List<VilleRecherche> _villesExplorees = new List<VilleRecherche>();
        private bool _showCursorAura = false;
        private Point _cursorAuraPos;

        private List<(string Nom, double Lat, double Lon)> _capitales = new List<(string, double, double)>
        {
            ("Paris", 48.8566, 2.3522), ("Tokyo", 35.6762, 139.6503), ("New York", 40.7128, -74.0060),
            ("Londres", 51.5074, -0.1278), ("Pékin", 39.9042, 116.4074), ("Moscou", 55.7558, 37.6173),
            ("Sydney", -33.8688, 151.2093), ("Le Caire", 30.0444, 31.2357), ("Buenos Aires", -34.6037, -58.3816),
            ("Los Angeles", 34.0522, -118.2437), ("Toronto", 43.6510, -79.3470), ("Le Cap", -33.9249, 18.4241),
            ("Séoul", 37.5665, 126.9780), ("Istanbul", 41.0082, 28.9784), ("Berlin", 52.5200, 13.4050),
            ("Madrid", 40.4168, -3.7038), ("Rome", 41.9028, 12.4964), ("Bangkok", 13.7563, 100.5018),
            ("Dubaï", 25.2048, 55.2708), ("Singapour", 1.3521, 103.8198), ("Mumbai", 19.0760, 72.8777),
            ("Mexico", 19.4326, -99.1332), ("São Paulo", -23.5505, -46.6333), ("Shanghai", 31.2304, 121.4737),
            ("Delhi", 28.7041, 77.1025), ("Nairobi", -1.2864, 36.8172), ("Lagos", 6.5244, 3.3792),
            ("Alger", 36.7538, 3.0588), ("Beyrouth", 33.8938, 35.5018)
        };

        private Timer _spinnerTimer;
        private Timer _clockTimer;
        private Timer _mapExploreTimer;
        private bool _chargement = false;
        private int _spinnerAngle = 0;
        private int _pulseAlpha = 20;
        private bool _pulseUp = true;
        private Panel _header, _panelCentral, _statusBar;
        private Panel _panelCards, _panelDetail, _panelCarte, _panelSpinner;
        private ComboBox _cmbVille;
        private Button _btnRechercher, _btnFavori, _btnDarkMode;
        private Label _lblVille, _lblCoord, _lblStatut, _lblHeure;
        private Timer _searchDebounceTimer;

        private Color BgMain => _modeSombre ? Color.FromArgb(13, 15, 28) : Color.FromArgb(235, 242, 255);
        private Color BgCard => _modeSombre ? Color.FromArgb(22, 25, 45) : Color.FromArgb(255, 255, 255);
        private Color TextMain => _modeSombre ? Color.FromArgb(220, 228, 255) : Color.FromArgb(10, 26, 60);
        private Color TextMuted => _modeSombre ? Color.FromArgb(90, 105, 150) : Color.FromArgb(110, 125, 160);
        private Color Accent => Color.FromArgb(99, 179, 255);
        private Color AccentGold => Color.FromArgb(255, 196, 57);
        private Color CardBorder => _modeSombre ? Color.FromArgb(35, 42, 75) : Color.FromArgb(200, 215, 245);
        private Color GradTop => _modeSombre ? Color.FromArgb(20, 28, 68) : Color.FromArgb(55, 95, 195);
        private Color GradBot => _modeSombre ? Color.FromArgb(13, 15, 28) : Color.FromArgb(235, 242, 255);

        private PointF? ProjectGlobe(double lat, double lon, int w, int h)
        {
            double R = Math.Min(w, h) / 2.0 * 0.85 * _globeZoom;
            double cLat = _globeCenterLat * Math.PI / 180.0;
            double cLon = _globeCenterLon * Math.PI / 180.0;

            double pLat = lat * Math.PI / 180.0;
            double pLon = lon * Math.PI / 180.0;

            double dLon = pLon - cLon;
            double x = Math.Cos(pLat) * Math.Sin(dLon);
            double y = Math.Cos(cLat) * Math.Sin(pLat) - Math.Sin(cLat) * Math.Cos(pLat) * Math.Cos(dLon);
            double z = Math.Sin(cLat) * Math.Sin(pLat) + Math.Cos(cLat) * Math.Cos(pLat) * Math.Cos(dLon);

            if (z < 0) return null;

            return new PointF(w / 2f + (float)(x * R), h / 2f - (float)(y * R));
        }

        public MainForm()
        {
            Text = "MétéoPro";
            Size = new Size(1340, 780);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9);
            DoubleBuffered = true;
            _favoris = _favorisService.Charger();
            InitFallbackMap();
            BuildUI();
            AppliquerTheme();
            DemarrerTimers();
            LoadDetailedMapAsync();
        }

        private void InitFallbackMap()
        {
            _mapPolygons = new List<float[]> {
                new float[] { 80,-160, 70,-160, 70,-140, 73,-120, 75,-70, 60,-60, 50,-55, 40,-70, 30,-80, 15,-90, 8,-75, 12,-85, 20,-95, 30,-115, 45,-125, 60,-140, 60,-165, 70,-160, 80,-160 },
                new float[] { 15,-75, 12,-70, 5,-50, -5,-35, -20,-40, -30,-50, -40,-60, -55,-70, -40,-75, -20,-70, -5,-80, 15,-75 },
                new float[] { 70,-10, 75,20, 75,80, 70,120, 65,170, 50,140, 40,120, 30,120, 25,110, 15,110, 10,105, 10,80, 20,60, 10,45, 20,40, 35,35, 40,20, 35,-10, 45,-5, 55,0, 60,10, 70,-10 },
                new float[] { 35,-10, 35,30, 35,45, 25,40, 10,50, 0,40, -10,40, -20,35, -35,25, -20,15, -10,10, 5,5, 5,-15, 20,-15, 35,-10 },
                new float[] { -15,115, -15,140, -20,150, -25,155, -35,150, -40,145, -35,115, -20,115, -15,115 },
                new float[] { 50,-5, 58,-5, 58,2, 50,2, 50,-5 },
                new float[] { 31,130, 35,135, 40,140, 45,142, 45,144, 40,142, 35,140, 31,132, 31,130 }
            };
        }

        private async void LoadDetailedMapAsync()
        {
            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync("https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_coastline.geojson");
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var list = new List<float[]>();
                foreach (var feature in doc.RootElement.GetProperty("features").EnumerateArray())
                {
                    var geom = feature.GetProperty("geometry");
                    var type = geom.GetProperty("type").GetString();
                    if (type == "LineString")
                    {
                        var arr = new List<float>();
                        foreach (var pt in geom.GetProperty("coordinates").EnumerateArray())
                        {
                            arr.Add(pt[1].GetSingle());
                            arr.Add(pt[0].GetSingle());
                        }

                        list.Add(arr.ToArray());
                    }
                    else if (type == "MultiLineString")
                    {
                        foreach (var line in geom.GetProperty("coordinates").EnumerateArray())
                        {
                            var arr = new List<float>();
                            foreach (var pt in line.EnumerateArray())
                            {
                                arr.Add(pt[1].GetSingle());
                                arr.Add(pt[0].GetSingle());
                            }

                            list.Add(arr.ToArray());
                        }
                    }
                }

                _mapPolygons = list;
                _mapDetalise = true;
                _panelCarte?.Invalidate();
            }
            catch
            {
            }
        }
    }
}
