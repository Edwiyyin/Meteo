using MeteoApp.Models;
using MeteoApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MeteoApp.Forms
{
    public class BufferedPanel : Panel
    {
        public BufferedPanel() { DoubleBuffered = true; }
    }

    public class MainForm : Form
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
            ("Alger", 30.0444, 3.0588), ("Beyrouth", 36.7538, 3.0588)
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
            catch { }
        }
        private void BuildUI()
        {
            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 28 };
            _statusBar.Paint += (s, e) =>
            {
                using var br = new LinearGradientBrush(_statusBar.ClientRectangle,
                    Color.FromArgb(8, 10, 20), Color.FromArgb(13, 16, 32), 0f);
                e.Graphics.FillRectangle(br, _statusBar.ClientRectangle);
            };
            _lblStatut = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 130, 190),
                Padding = new Padding(12, 5, 0, 0),
                Text = "  Prêt  ·  Entrez une ville pour commencer",
                BackColor = Color.Transparent
            };
            _statusBar.Controls.Add(_lblStatut);
            _header = new Panel { Dock = DockStyle.Top, Height = 72 };
            _header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var r = _header.ClientRectangle;
                using var br = new LinearGradientBrush(r, Color.FromArgb(8, 12, 30), Color.FromArgb(16, 24, 58), 0f);
                g.FillRectangle(br, r);
                using var lineBr = new LinearGradientBrush(new Rectangle(0, r.Height - 2, r.Width, 2),
                    Color.FromArgb(0, 99, 179, 255), Color.FromArgb(200, 99, 179, 255), 0f);
                g.FillRectangle(lineBr, 0, r.Height - 2, r.Width, 2);
            };

            new Label
            {
                Text = "◈  MétéoPro",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 18),
                AutoSize = true,
                BackColor = Color.Transparent,
                Parent = _header
            };

            _lblHeure = new Label
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 140, 210),
                Location = new Point(168, 25),
                AutoSize = true,
                BackColor = Color.Transparent,
                Parent = _header
            };

            _cmbVille = new ComboBox
            {
                Location = new Point(240, 20),
                Width = 230,
                Height = 34,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 30, 60),
                ForeColor = Color.FromArgb(80, 100, 150),
                Parent = _header,
                DropDownHeight = 300,
                MaxDropDownItems = 12,
                IntegralHeight = true,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cmbVille.Text = "Rechercher une ville...";
            _cmbVille.GotFocus += (s, e) =>
            {
                if (_cmbVille.Text == "Rechercher une ville...") { _cmbVille.Text = ""; _cmbVille.ForeColor = Color.FromArgb(180, 200, 255); }
            };
            _cmbVille.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_cmbVille.Text)) { _cmbVille.Text = "Rechercher une ville..."; _cmbVille.ForeColor = Color.FromArgb(80, 100, 150); }
            };
            _cmbVille.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LancerRechercheListeVilles(); } };
            _cmbVille.SelectionChangeCommitted += (s, e) => { if (_cmbVille.SelectedIndex >= 0) LancerRecherche(); };

            _searchDebounceTimer = new Timer { Interval = 600 };
            _searchDebounceTimer.Tick += (s, e) => { _searchDebounceTimer.Stop(); LancerRechercheListeVilles(); };

            _cmbVille.TextChanged += (s, e) => 
            {
                if (_cmbVille.ContainsFocus && _cmbVille.Text != "Rechercher une ville..." && _cmbVille.Text.Length > 2)
                {
                    _searchDebounceTimer.Stop();
                    _searchDebounceTimer.Start();
                }
            };

            _btnRechercher = CreerBtnHeader("🔍  Rechercher", 482, 20, 140, Color.FromArgb(99, 179, 255), Color.FromArgb(5, 15, 40));
            _btnRechercher.Click += (s, e) => LancerRechercheListeVilles();

            _btnFavori = CreerBtnHeader("★  Favori", 632, 20, 100, Color.FromArgb(255, 196, 57), Color.FromArgb(30, 20, 5));
            _btnFavori.Enabled = false;
            _btnFavori.Click += BtnFavori_Click;

            _btnDarkMode = CreerBtnHeader("☀", 742, 20, 42, Color.FromArgb(45, 52, 88), Color.FromArgb(180, 200, 255));
            _btnDarkMode.Font = new Font("Segoe UI", 13);
            _btnDarkMode.Click += (s, e) => { _modeSombre = !_modeSombre; AppliquerTheme(); };

            _panelCentral = new Panel { Dock = DockStyle.Fill };
            _panelCentral.Paint += (s, e) =>
            {
                using var br = new LinearGradientBrush(_panelCentral.ClientRectangle, GradTop, GradBot, 90f);
                e.Graphics.FillRectangle(br, _panelCentral.ClientRectangle);
            };

            _lblVille = new Label
            {
                Text = "Bienvenue sur MétéoPro  ◈",
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 10),
                AutoSize = true,
                BackColor = Color.Transparent,
                Parent = _panelCentral
            };

            _lblCoord = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 140, 200),
                Location = new Point(18, 40),
                AutoSize = true,
                BackColor = Color.Transparent,
                Parent = _panelCentral
            };

            _panelCards = new Panel
            {
                Location = new Point(14, 58),
                Size = new Size(860, 240),
                BackColor = Color.Transparent,
                Parent = _panelCentral
            };

            _panelSpinner = new Panel
            {
                Location = new Point(14, 58),
                Size = new Size(860, 240),
                BackColor = Color.Transparent,
                Visible = false,
                Parent = _panelCentral
            };
            _panelSpinner.Paint += Spinner_Paint;

            _panelDetail = new Panel
            {
                Location = new Point(14, 308),
                Size = new Size(860, 210),
                BackColor = Color.Transparent,
                Visible = false,
                Parent = _panelCentral
            };
            _panelDetail.Paint += Detail_Paint;
            _panelDetail.MouseMove += (s, e) =>
            {
                if (_chartZones == null) return;
                int hIdx = -1;
                for (int i = 0; i < _chartZones.Length; i++)
                {
                    if (_chartZones[i].Contains(e.Location)) { hIdx = i; break; }
                }
                if (_hoveredChartIndex != hIdx)
                {
                    _hoveredChartIndex = hIdx;
                    _panelDetail.Invalidate();
                }
            };
            _panelDetail.MouseLeave += (s, e) =>
            {
                if (_hoveredChartIndex != -1)
                {
                    _hoveredChartIndex = -1;
                    _panelDetail.Invalidate();
                }
            };
            _panelDetail.MouseClick += (s, e) =>
            {
                if (_hoveredChartIndex >= 0 && _hoveredChartIndex < (_prevActuelles?.Count ?? 0))
                {
                    _jourDetail = _prevActuelles[_hoveredChartIndex];
                    _panelDetail.Invalidate();
                    foreach (Control c in _panelCards.Controls) c.Invalidate();
                    DateTime dt = DateTime.Parse(_jourDetail.Date);
                    SetStatut($"📅  Détails : {dt.ToString("dddd dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"))}");
                }
            };

            _panelCarte = new BufferedPanel
            {
                Location = new Point(882, 58),
                Size = new Size(340, 460),
                BackColor = Color.Transparent,
                Parent = _panelCentral
            };
            _panelCarte.Paint += Carte_Paint;
            _panelCarte.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingGlobe = true;
                    _lastMousePos = e.Location;
                }
            };
            _panelCarte.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) 
                {
                    _isDraggingGlobe = false;
                    _mapExploreTimer?.Stop(); _mapExploreTimer?.Start();
                }
            };
            _panelCarte.MouseEnter += (s, e) => _panelCarte.Focus();
            _panelCarte.MouseWheel += (s, e) =>
            {
                if (e.Delta > 0) _globeZoom *= 1.25;
                else if (e.Delta < 0) _globeZoom /= 1.25;
                if (_globeZoom < 1.0) _globeZoom = 1.0;
                if (_globeZoom > 30.0) _globeZoom = 30.0;
                _panelCarte.Invalidate();
                _mapExploreTimer?.Stop(); _mapExploreTimer?.Start();
            };
            _panelCarte.MouseMove += (s, e) =>
            {
                if (_isDraggingGlobe)
                {
                    double dFactor = 0.6 / _globeZoom;
                    double dx = e.X - _lastMousePos.X;
                    double dy = e.Y - _lastMousePos.Y;
                    _globeCenterLon -= dx * dFactor;
                    _globeCenterLat += dy * dFactor;
                    if (_globeCenterLat > 90) _globeCenterLat = 90;
                    if (_globeCenterLat < -90) _globeCenterLat = -90;
                    if (_globeCenterLon > 180) _globeCenterLon -= 360;
                    if (_globeCenterLon < -180) _globeCenterLon += 360;

                    _lastMousePos = e.Location;
                    _panelCarte.Invalidate();
                    return;
                }

                string hover = null;
                int W = _panelCarte.Width; int H = _panelCarte.Height;
                var list = new Dictionary<string, PointF>();

                if (_favoris != null) foreach (var f in _favoris) { var p = ProjectGlobe(f.Latitude, f.Longitude, W, H); if (p.HasValue) list[f.Nom] = p.Value; }
                if (_villeActuelle != null) { var p = ProjectGlobe(_villeActuelle.Latitude, _villeActuelle.Longitude, W, H); if (p.HasValue) list[_villeActuelle.Nom] = p.Value; }

                if (_globeZoom >= 1.2)
                {
                    foreach (var c in _capitales)
                    {
                        if (!list.ContainsKey(c.Nom))
                        {
                            var p = ProjectGlobe(c.Lat, c.Lon, W, H);
                            if (p.HasValue) list[c.Nom] = p.Value;
                        }
                    }
                }

                if (_globeZoom >= 1.5 && _villesExplorees != null)
                {
                    foreach (var exp in _villesExplorees)
                    {
                        if (!list.ContainsKey(exp.Nom))
                        {
                            var p = ProjectGlobe(exp.Latitude, exp.Longitude, W, H);
                            if (p.HasValue) list[exp.Nom] = p.Value;
                        }
                    }
                }

                foreach (var kvp in list)
                {
                    if (Math.Pow(e.X - kvp.Value.X, 2) + Math.Pow(e.Y - kvp.Value.Y, 2) <= 144)
                    {
                        hover = kvp.Key;
                        break;
                    }
                }
                if (_hoveredVille != hover)
                {
                    _hoveredVille = hover;
                    _panelCarte.Invalidate();
                    _panelCarte.Cursor = hover != null ? Cursors.Hand : Cursors.Default;
                }
            };
            _panelCarte.MouseClick += (s, e) =>
            {
                if (_hoveredVille != null)
                {
                    _cmbVille.Text = _hoveredVille;
                    var exp = _villesExplorees?.FirstOrDefault(v => string.Equals(v.Nom, _hoveredVille, StringComparison.OrdinalIgnoreCase));
                    if (exp != null) 
                    {
                        _cmbVille.SelectedItem = exp;
                    }
                    else
                    {
                        var cap = _capitales.FirstOrDefault(c => string.Equals(c.Nom, _hoveredVille, StringComparison.OrdinalIgnoreCase));
                        if (cap.Nom != null)
                        {
                            _cmbVille.SelectedItem = new VilleRecherche { Nom = cap.Nom, Latitude = cap.Lat, Longitude = cap.Lon, Pays = "" };
                        }
                        else
                        {
                            var fav = _favoris?.FirstOrDefault(v => string.Equals(v.Nom, _hoveredVille, StringComparison.OrdinalIgnoreCase));
                            if (fav != null) _cmbVille.SelectedItem = new VilleRecherche { Nom = fav.Nom, Latitude = fav.Latitude, Longitude = fav.Longitude, Pays = fav.Pays };
                        }
                    }
                    LancerRecherche();
                }
            };

            Controls.Add(_panelCentral);
            Controls.Add(_header);
            Controls.Add(_statusBar);
        }

        private void Spinner_Paint(object sender, PaintEventArgs e)
        {
            if (!_chargement) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = _panelSpinner.Width / 2, cy = _panelSpinner.Height / 2;

            using var ovBr = new SolidBrush(Color.FromArgb(140, 13, 15, 28));
            g.FillRectangle(ovBr, _panelSpinner.ClientRectangle);

            int r2 = 30;
            for (int i = 0; i < 8; i++)
            {
                int alpha = (int)(255 * (i + 1) / 8.0);
                double rad = ((_spinnerAngle + i * 45) % 360) * Math.PI / 180;
                float px = cx + (float)(r2 * Math.Cos(rad));
                float py = cy + (float)(r2 * Math.Sin(rad));
                using var dotBr = new SolidBrush(Color.FromArgb(alpha, Accent));
                g.FillEllipse(dotBr, px - 5, py - 5, 10, 10);
            }

            using var fLoad = new Font("Segoe UI", 11, FontStyle.Bold);
            string txt = "Chargement...";
            var sz = g.MeasureString(txt, fLoad);
            g.DrawString(txt, fLoad, new SolidBrush(Color.FromArgb(180, 200, 255)), cx - sz.Width / 2, cy + 48);
        }

        private void Detail_Paint(object sender, PaintEventArgs e)
        {
            if (_jourDetail == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var j = _jourDetail;
            int W = _panelDetail.Width, H = _panelDetail.Height;

            using var path = RoundedRect(1, 1, W - 2, H - 2, 14);
            using var bgBr = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                _modeSombre ? Color.FromArgb(22, 28, 56) : Color.FromArgb(240, 248, 255),
                _modeSombre ? Color.FromArgb(15, 18, 38) : Color.FromArgb(220, 235, 255), 90f);
            g.FillPath(bgBr, path);
            using var borderPen = new Pen(Color.FromArgb(80, Accent), 1.5f);
            g.DrawPath(borderPen, path);

            string titre = DateTime.Parse(j.Date).ToString("dddd dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
            titre = "Détails — " + char.ToUpper(titre[0]) + titre.Substring(1);
            using var fTitle = new Font("Segoe UI", 11, FontStyle.Bold);
            g.DrawString(titre, fTitle, new SolidBrush(TextMain), 16, 12);
            using var sepPen = new Pen(CardBorder, 1);
            g.DrawLine(sepPen, 16, 36, W - 16, 36);

            DrawTempChart(g, 16, 44, 415, 148);

            int xs = 455, ys = 44;
            DrawStatBox(g, xs, ys, "🌡", "Ressenti", $"{j.FeelLike:F1}°C");
            DrawStatBox(g, xs, ys + 44, "📊", "Pression", $"{j.Pression:F0} hPa");
            DrawStatBox(g, xs, ys + 88, "👁", "Visibilité", $"{j.Visibilite:F1} km");
            DrawStatBox(g, xs, ys + 132, "☁", "Nuages", $"{j.Nuages}%");
            DrawStatBox(g, xs + 195, ys, "🌅", "Lever", j.Lever);
            DrawStatBox(g, xs + 195, ys + 44, "🌇", "Coucher", j.Coucher);
            DrawStatBox(g, xs + 195, ys + 88, "💧", "Humidité", $"{j.Humidite}%");
            DrawStatBox(g, xs + 195, ys + 132, "🌬", "Vent", $"{j.VitesseVent:F0} km/h");

            DrawWindCompass(g, W - 90, H / 2 - 10, 70, j.VitesseVent);
        }

        private void DrawStatBox(Graphics g, int x, int y, string icon, string label, string valeur)
        {
            using var fIcon = new Font("Segoe UI", 10);
            using var fLabel = new Font("Segoe UI", 7.5f);
            using var fVal = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            g.DrawString(icon, fIcon, new SolidBrush(Accent), x, y);
            g.DrawString(label, fLabel, new SolidBrush(TextMuted), x + 22, y + 2);
            g.DrawString(valeur, fVal, new SolidBrush(TextMain), x + 22, y + 17);
        }

        private void DrawWindCompass(Graphics g, int cx, int cy, int size, double vitesse)
        {
            int r = size / 2;
            using var circleBr = new SolidBrush(_modeSombre ? Color.FromArgb(15, 18, 38) : Color.FromArgb(225, 238, 255));
            g.FillEllipse(circleBr, cx - r, cy - r, size, size);
            using var circlePen = new Pen(CardBorder, 1.5f);
            g.DrawEllipse(circlePen, cx - r, cy - r, size, size);

            using var fCard = new Font("Segoe UI", 7, FontStyle.Bold);
            g.DrawString("N", fCard, new SolidBrush(Accent), cx - 5, cy - r - 14);
            g.DrawString("S", fCard, new SolidBrush(TextMuted), cx - 4, cy + r + 2);
            g.DrawString("E", fCard, new SolidBrush(TextMuted), cx + r + 3, cy - 6);
            g.DrawString("O", fCard, new SolidBrush(TextMuted), cx - r - 14, cy - 6);

            double angle = (vitesse * 7) % 360;
            double rad = (angle - 90) * Math.PI / 180;
            float ax = cx + (float)((r - 8) * Math.Cos(rad));
            float ay = cy + (float)((r - 8) * Math.Sin(rad));
            using var arrowPen = new Pen(AccentGold, 2.5f) { EndCap = LineCap.ArrowAnchor };
            g.DrawLine(arrowPen, cx, cy, ax, ay);
            g.FillEllipse(new SolidBrush(Accent), cx - 3, cy - 3, 6, 6);

            using var fSpeed = new Font("Segoe UI", 7, FontStyle.Bold);
            string sv = $"{vitesse:F0}";
            var szv = g.MeasureString(sv, fSpeed);
            g.DrawString(sv, fSpeed, new SolidBrush(AccentGold), cx - szv.Width / 2, cy + 8);
        }

        private void DrawTempChart(Graphics g, int x, int y, int w, int h)
        {
            if (_prevActuelles == null || _prevActuelles.Count < 2) return;
            var prevs = _prevActuelles;
            double minT = prevs.Min(p => p.TempMin) - 3;
            double maxT = prevs.Max(p => p.TempMax) + 3;
            double range = Math.Max(maxT - minT, 1);
            int n = prevs.Count;

            using var gridPen = new Pen(Color.FromArgb(25, Accent), 1) { DashStyle = DashStyle.Dash };
            using var fGrid = new Font("Segoe UI", 6.5f);
            for (int i = 0; i <= 4; i++)
            {
                float yl = y + h - (float)(i / 4.0 * h);
                g.DrawLine(gridPen, x, yl, x + w, yl);
                double temp = minT + (maxT - minT) * i / 4;
                g.DrawString($"{temp:F0}°", fGrid, new SolidBrush(TextMuted), x - 24, yl - 6);
            }

            var ptMax = new PointF[n];
            var ptMin = new PointF[n];
            _chartZones = new RectangleF[n];

            float step = (float)w / (n - 1);
            for (int i = 0; i < n; i++)
            {
                float cx2 = x + i * step;
                ptMax[i] = new PointF(cx2, y + h - (float)((prevs[i].TempMax - minT) / range * h));
                ptMin[i] = new PointF(cx2, y + h - (float)((prevs[i].TempMin - minT) / range * h));

                _chartZones[i] = new RectangleF(cx2 - step / 2f, y, step, h + 30);
            }
            using var pathFill = new GraphicsPath();
            pathFill.AddCurve(ptMax);
            var pathMin = new GraphicsPath();
            pathMin.AddCurve(ptMin);
            pathMin.Reverse();
            pathFill.AddPath(pathMin, true);
            pathFill.CloseFigure();

            using var fillBr = new LinearGradientBrush(new Rectangle(x, y, w, h), 
                Color.FromArgb(60, 255, 100, 80), 
                Color.FromArgb(60, Accent), 90f);
            g.FillPath(fillBr, pathFill);

            using var penMax = new Pen(Color.FromArgb(255, 100, 80), 2.5f) { LineJoin = LineJoin.Round };
            using var penMin = new Pen(Accent, 2.5f) { LineJoin = LineJoin.Round };
            g.DrawCurve(penMax, ptMax);
            g.DrawCurve(penMin, ptMin);

            using var fSmall = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var fDay = new Font("Segoe UI", 7.5f);
            for (int i = 0; i < n; i++)
            {
                bool isHovered = (i == _hoveredChartIndex);
                bool isActive = (_jourDetail == prevs[i]);

                if (isHovered || isActive) 
                {
                    using var hlPen = new Pen(Color.FromArgb(20, Accent), this._chartZones[i].Width * 0.8f);
                    g.DrawLine(hlPen, ptMax[i].X, y, ptMax[i].X, y + h);
                }

                int pSize = (isHovered || isActive) ? 12 : 8;
                int offset = pSize / 2;

                g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), ptMax[i].X - offset, ptMax[i].Y - offset, pSize, pSize);
                g.FillEllipse(new SolidBrush(Accent), ptMin[i].X - offset, ptMin[i].Y - offset, pSize, pSize);

                if (isHovered || isActive) 
                {
                    g.DrawString($"{prevs[i].TempMax:F0}°", fSmall, new SolidBrush(Color.FromArgb(255, 130, 110)), ptMax[i].X - 10, ptMax[i].Y - 20);
                    g.DrawString($"{prevs[i].TempMin:F0}°", fSmall, new SolidBrush(Accent), ptMin[i].X - 10, ptMin[i].Y + 8);
                }
                else 
                {
                    g.DrawString($"{prevs[i].TempMax:F0}°", fDay, new SolidBrush(TextMuted), ptMax[i].X - 10, ptMax[i].Y - 18);
                    g.DrawString($"{prevs[i].TempMin:F0}°", fDay, new SolidBrush(Color.FromArgb(140, Accent)), ptMin[i].X - 10, ptMin[i].Y + 6);
                }

                string jl = DateTime.Parse(prevs[i].Date).ToString("ddd", new System.Globalization.CultureInfo("fr-FR"));
                var txtBrush = (isHovered || isActive) ? new SolidBrush(TextMain) : new SolidBrush(TextMuted);
                g.DrawString(jl, (isHovered || isActive) ? fSmall : fDay, txtBrush, ptMax[i].X - 9, y + h + 4);
                txtBrush.Dispose();
            }

            using var fLeg = new Font("Segoe UI", 7.5f);
            g.FillEllipse(new SolidBrush(Color.FromArgb(255, 110, 90)), x, y + h + 20, 8, 8);
            g.DrawString("Max", fLeg, new SolidBrush(TextMuted), x + 12, y + h + 19);
            g.FillEllipse(new SolidBrush(Accent), x + 55, y + h + 20, 8, 8);
            g.DrawString("Min", fLeg, new SolidBrush(TextMuted), x + 67, y + h + 19);
        }

        private void Carte_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int W = _panelCarte.Width, H = _panelCarte.Height;

            using var path = RoundedRect(0, 0, W - 1, H - 1, 14);
            using var bgBr = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                _modeSombre ? Color.FromArgb(16, 20, 40) : Color.FromArgb(220, 235, 255),
                _modeSombre ? Color.FromArgb(8, 12, 28) : Color.FromArgb(195, 215, 255), 90f);
            g.FillPath(bgBr, path);
            using var borderPen = new Pen(Color.FromArgb(70, Accent), 1.5f);
            g.DrawPath(borderPen, path);
            float globR = (float)(Math.Min(W, H) / 2.0 * 0.85);
            using var globeBgBr = new SolidBrush(_modeSombre ? Color.FromArgb(40, 20, 30, 60) : Color.FromArgb(40, 150, 185, 240));
            g.FillEllipse(globeBgBr, W/2f - globR, H/2f - globR, globR*2, globR*2);
            using var globeBorderPen = new Pen(Color.FromArgb(100, Accent), 1f);
            g.DrawEllipse(globeBorderPen, W/2f - globR, H/2f - globR, globR*2, globR*2);

            using var gridPen = new Pen(Color.FromArgb(35, Accent), 1f);
            for (int lat = -80; lat <= 80; lat += 20)
            {
                var pts = new List<PointF>();
                for (int lon = -180; lon <= 180; lon += 5)
                {
                    var p = ProjectGlobe(lat, lon, W, H);
                    if (p.HasValue) pts.Add(p.Value);
                    else if (pts.Count > 0) { if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray()); pts.Clear(); }
                }
                if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray());
            }
            for (int lon = -180; lon <= 180; lon += 20)
            {
                var pts = new List<PointF>();
                for (int lat = -90; lat <= 90; lat += 5)
                {
                    var p = ProjectGlobe(lat, lon, W, H);
                    if (p.HasValue) pts.Add(p.Value);
                    else if (pts.Count > 0) { if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray()); pts.Clear(); }
                }
                if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray());
            }
            using var mapPen = new Pen(_modeSombre ? Color.FromArgb(100, 150, 220) : Color.FromArgb(80, 130, 200), _mapDetalise ? 1.2f : 2f) { LineJoin = LineJoin.Round };

            foreach (var coords in _mapPolygons)
            {
                if (_mapDetalise)
                {
                    var pts = new List<PointF>();
                    for (int i = 0; i < coords.Length - 1; i += 2)
                    {
                        var p = ProjectGlobe(coords[i], coords[i + 1], W, H);
                        if (p.HasValue) pts.Add(p.Value);
                        else if (pts.Count > 0)
                        {
                            if (pts.Count > 1) g.DrawLines(mapPen, pts.ToArray());
                            pts.Clear();
                        }
                    }
                    if (pts.Count > 1) g.DrawLines(mapPen, pts.ToArray());
                }
                else
                {
                    for (int i = 0; i < coords.Length - 2; i += 2)
                    {
                        float lat1 = coords[i], lon1 = coords[i + 1];
                        float lat2 = coords[i + 2], lon2 = coords[i + 3];
                        double dist = Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
                        int steps = Math.Max(1, (int)(dist / 4.0));
                        PointF? lastP = ProjectGlobe(lat1, lon1, W, H);
                        for (int s = 1; s <= steps; s++)
                        {
                            float clat = lat1 + (lat2 - lat1) * s / steps;
                            float clon = lon1 + (lon2 - lon1) * s / steps;
                            PointF? p = ProjectGlobe(clat, clon, W, H);
                            if (lastP.HasValue && p.HasValue)
                            {
                                g.DrawLine(mapPen, lastP.Value, p.Value);
                            }
                            lastP = p;
                        }
                    }
                }
            }

            using var fTitle = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            g.DrawString("⛯  Globe Favoris (Glisser/Zoomer)", fTitle, new SolidBrush(Accent), 12, 12);

            var toDraw = new Dictionary<string, (double lat, double lon, bool favori, bool isExp, bool isCap)>();
            if (_favoris != null)
                foreach (var f in _favoris) toDraw[f.Nom] = (f.Latitude, f.Longitude, true, false, false);
            if (_villeActuelle != null && !toDraw.ContainsKey(_villeActuelle.Nom))
                toDraw[_villeActuelle.Nom] = (_villeActuelle.Latitude, _villeActuelle.Longitude, false, false, false);

            if (_globeZoom >= 1.2)
            {
                foreach (var c in _capitales)
                {
                    if (!toDraw.ContainsKey(c.Nom))
                        toDraw[c.Nom] = (c.Lat, c.Lon, false, false, true);
                }
            }

            if (_globeZoom >= 1.5 && _villesExplorees != null)
            {
                foreach (var exp in _villesExplorees)
                {
                    if (!toDraw.ContainsKey(exp.Nom))
                        toDraw[exp.Nom] = (exp.Latitude, exp.Longitude, false, true, false);
                }
            }

            using var fCity = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            foreach (var kvp in toDraw)
            {
                var nom = kvp.Key;
                var ptInfo = ProjectGlobe(kvp.Value.lat, kvp.Value.lon, W, H);
                if (!ptInfo.HasValue) continue; 

                var pt = ptInfo.Value;

                bool isActive = _villeActuelle != null && string.Equals(_villeActuelle.Nom, nom, StringComparison.OrdinalIgnoreCase);
                bool isHovered = string.Equals(_hoveredVille, nom, StringComparison.OrdinalIgnoreCase);
                bool isFav = kvp.Value.favori;
                bool isExp = kvp.Value.isExp;
                bool isCap = kvp.Value.isCap;

                if (isActive || isHovered)
                {
                    int pa = isActive ? _pulseAlpha : 150;
                    Color glow = isHovered ? Color.White : AccentGold;
                    using var pulseBr = new SolidBrush(Color.FromArgb(pa, glow));
                    g.FillEllipse(pulseBr, pt.X - 15, pt.Y - 15, 30, 30);
                    using var pulsePen2 = new Pen(Color.FromArgb(pa, glow), 2);
                    g.DrawEllipse(pulsePen2, pt.X - 13, pt.Y - 13, 26, 26);
                    g.FillEllipse(new SolidBrush(glow), pt.X - 6, pt.Y - 6, 12, 12);

                    using var fActive = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    g.DrawString(nom, fActive, new SolidBrush(glow), pt.X + 12, pt.Y - 8);
                }
                else
                {
                    if (isExp) continue;

                    Color dotColor = isFav ? AccentGold : (_modeSombre ? Color.FromArgb(85, 130, 210) : Color.FromArgb(50, 90, 180));
                    if (isExp && !isFav) dotColor = _modeSombre ? Color.FromArgb(90, 120, 150) : Color.FromArgb(120, 150, 180);
                    if (isCap && !isFav) dotColor = _modeSombre ? Color.FromArgb(180, 200, 230) : Color.FromArgb(80, 110, 150);

                    g.FillEllipse(new SolidBrush(dotColor), pt.X - 4, pt.Y - 4, 8, 8);
                    g.FillEllipse(Brushes.White, pt.X - 1, pt.Y - 1, 2, 2);
                    g.DrawString(nom, isCap ? new Font("Segoe UI", 8.5f, FontStyle.Bold) : fCity, new SolidBrush(TextMuted), pt.X + 8, pt.Y - 6);
                }
            }

            if (_villeActuelle != null)
            {
                using var fInfo = new Font("Segoe UI", 8.5f);
                using var fInfoB = new Font("Segoe UI", 10, FontStyle.Bold);
                g.DrawString(_villeActuelle.Nom, fInfoB, new SolidBrush(AccentGold), 12, H - 45);
                g.DrawString($"{_villeActuelle.Pays}  ·  {_villeActuelle.Latitude:F2}°  ·  {_villeActuelle.Longitude:F2}°",
                    fInfo, new SolidBrush(TextMuted), 12, H - 25);
            }
        }
        private async void LancerRecherche()
        {
            string villeStr = (_cmbVille.Text ?? "").Trim();
            if (string.IsNullOrEmpty(villeStr) || villeStr == "Rechercher une ville...") return;

            SetChargement(true);
            _btnRechercher.Enabled = false;
            _panelDetail.Visible = false;
            _jourDetail = null;

            try
            {
                VilleInfo info;
                List<MeteoJour> previsions;

                if (_cmbVille.SelectedItem is VilleRecherche vr)
                {
                    (info, previsions) = await _meteo.GetPrevisionsByCoords(vr.Latitude, vr.Longitude);
                    info.Nom = vr.Nom; 
                    if (!string.IsNullOrEmpty(vr.Pays)) info.Pays = vr.Pays;
                }
                else
                {
                    string queryStr = villeStr;
                    int pIdx = queryStr.IndexOf('(');
                    if (pIdx > 0)
                    {
                        string country = queryStr.Substring(pIdx + 1).TrimEnd(')');
                        string nameState = queryStr.Substring(0, pIdx).Trim();
                        queryStr = $"{nameState},{country}";
                    }
                    (info, previsions) = await _meteo.GetPrevisions(queryStr);
                }

                _villeActuelle = info;
                _prevActuelles = previsions;
                _globeCenterLat = info.Latitude;
                _globeCenterLon = info.Longitude;

                _lblVille.Text = $"📍  {info.Nom}   ({info.Pays})";
                _lblCoord.Text = $"lat {info.Latitude:F3}°  ·  lon {info.Longitude:F3}°  ·  {previsions.Count} jours";
                _btnFavori.Enabled = true;

                _historique.Remove((_cmbVille.SelectedItem as string) ?? villeStr);
                _historique.Insert(0, (_cmbVille.SelectedItem as string) ?? villeStr);
                if (_historique.Count > 10) _historique.RemoveAt(10);
                _cmbVille.Items.Clear();
                _historique.ForEach(h => _cmbVille.Items.Add(h));

                UpdateBtnFavori();
                AfficherCardsAvecAnimation(previsions);
                _panelCarte.Invalidate();
                SetStatut($"✓  {info.Nom} ({info.Pays})  ·  {previsions.Count} jours  ·  {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                string msg = ex.Message.Contains("API") ? ex.Message : "Ville introuvable ou erreur réseau.";
                MessageBox.Show(msg, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SetStatut("✗  Erreur lors de la recherche");
            }
            finally
            {
                SetChargement(false);
                _btnRechercher.Enabled = true;
            }
        }

        private async void LancerRechercheListeVilles()
        {
            string query = (_cmbVille.Text ?? "").Trim();
            if (string.IsNullOrEmpty(query) || query == "Rechercher une ville...") return;

            _btnRechercher.Enabled = false;
            SetStatut($"Recherche de la ville : {query}...");

            try
            {
                var resultats = await _meteo.RechercherVilles(query);

                _cmbVille.Items.Clear();
                if (resultats.Count > 0)
                {
                    _cmbVille.BeginUpdate();
                    foreach (var v in resultats)
                    {
                        _cmbVille.Items.Add(v);
                    }
                    _cmbVille.EndUpdate();
                    _cmbVille.DroppedDown = true;
                    _cmbVille.ForeColor = Color.FromArgb(180, 200, 255);
                    _cmbVille.SelectionStart = _cmbVille.Text.Length;

                    SetStatut($"✓ {resultats.Count} résultats pour '{query}'.");
                }
                else
                {
                    SetStatut($"Aucun résultat pour '{query}'.");
                }
            }
            catch
            {
                SetStatut("Erreur lors de la recherche de villes.");
            }
            finally
            {
                _btnRechercher.Enabled = true;
            }
        }
        private void AfficherCardsAvecAnimation(List<MeteoJour> previsions)
        {
            _panelCards.Controls.Clear();
            int cardW = 162, gap = 10;

            for (int i = 0; i < previsions.Count; i++)
            {
                var card = CreerCard(previsions[i], i);
                int targetX = i * (cardW + gap);
                card.Location = new Point(targetX, _panelCards.Height + 30);
                _panelCards.Controls.Add(card);
                int delay = i * 85;
                var t = new Timer { Interval = Math.Max(1, delay) };
                t.Tick += (s, e) =>
                {
                    t.Stop();
                    if (!card.IsDisposed) AnimerCard(card, 5);
                };
                t.Start();
            }
        }

        private void AnimerCard(Control card, int toY)
        {
            var slideTimer = new Timer { Interval = 16 };
            slideTimer.Tick += (s, e) =>
            {
                if (card.IsDisposed) { slideTimer.Stop(); return; }
                int diff = toY - card.Top;
                if (Math.Abs(diff) <= 2) { card.Top = toY; slideTimer.Stop(); return; }
                card.Top += diff / 5 + (diff > 0 ? 1 : -1);
            };
            slideTimer.Start();
        }

        private Panel CreerCard(MeteoJour jour, int index)
        {
            var card = new Panel { Size = new Size(162, 235), BackColor = BgCard, Cursor = Cursors.Hand, Tag = jour };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isSelected = _jourDetail == jour;

                using var path = RoundedRect(1, 1, card.Width - 2, card.Height - 2, 12);
                using var bgBr = new LinearGradientBrush(card.ClientRectangle,
                    _modeSombre ? Color.FromArgb(26, 30, 60) : Color.FromArgb(255, 255, 255),
                    _modeSombre ? Color.FromArgb(18, 22, 44) : Color.FromArgb(238, 248, 255), 90f);
                g.FillPath(bgBr, path);

                Color borderCol = isSelected ? Accent : CardBorder;
                float borderW = isSelected ? 2f : 1f;
                using var pen = new Pen(borderCol, borderW);
                g.DrawPath(pen, path);

                if (index == 0)
                {
                    using var todayPath = RoundedRect(2, 2, card.Width - 4, 22, 8);
                    using var todayBr = new LinearGradientBrush(new Rectangle(2, 2, card.Width, 22),
                        Accent, Color.FromArgb(60, Accent.R, Accent.G, Accent.B), 0f);
                    g.FillPath(todayBr, todayPath);
                    using var fT = new Font("Segoe UI", 7, FontStyle.Bold);
                    g.DrawString("AUJOURD'HUI", fT, Brushes.White, 32, 6);
                }
            };

            int yOff = index == 0 ? 27 : 6;
            DateTime dt = DateTime.Parse(jour.Date);
            string nomJour = dt.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));
            nomJour = char.ToUpper(nomJour[0]) + nomJour.Substring(1);

            new Label { Text = nomJour, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = TextMain, Location = new Point(5, yOff), Size = new Size(152, 20), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };
            new Label { Text = dt.ToString("dd MMM", new System.Globalization.CultureInfo("fr-FR")), Font = new Font("Segoe UI", 7.5f), ForeColor = TextMuted, Location = new Point(5, yOff + 18), Size = new Size(152, 16), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };

            var pic = new PictureBox { Location = new Point(31, yOff + 36), Size = new Size(100, 72), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Parent = card };
            ChargerIcone(pic, jour.Icone);

            new Label { Text = $"{jour.Temperature:F0}°C", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = CouleurTemp(jour.Temperature), Location = new Point(5, yOff + 110), Size = new Size(152, 34), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };
            new Label { Text = $"↓{jour.TempMin:F0}°   ↑{jour.TempMax:F0}°", Font = new Font("Segoe UI", 7.5f), ForeColor = TextMuted, Location = new Point(5, yOff + 144), Size = new Size(152, 15), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };
            new Label { Text = jour.Description, Font = new Font("Segoe UI", 7.5f, FontStyle.Italic), ForeColor = TextMuted, Location = new Point(5, yOff + 160), Size = new Size(152, 14), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };

            var barBg = new Panel { Location = new Point(14, yOff + 178), Size = new Size(134, 5), BackColor = CardBorder, Parent = card };
            new Panel { Location = new Point(0, 0), Size = new Size((int)(134 * jour.Humidite / 100.0), 5), BackColor = Accent, Parent = barBg };
            new Label { Text = $"💧{jour.Humidite}%  💨{jour.VitesseVent:F0}  ☁{jour.Nuages}%", Font = new Font("Segoe UI", 6.8f), ForeColor = TextMuted, Location = new Point(4, yOff + 186), Size = new Size(154, 14), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Parent = card };

            Action onClick = () =>
            {
                _jourDetail = jour;
                _panelDetail.Visible = true;
                _panelDetail.Invalidate();
                foreach (Control c in _panelCards.Controls) c.Invalidate();
                SetStatut($"📅  Détails : {dt.ToString("dddd dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"))}");
            };

            card.Click += (s, e) => onClick();
            foreach (Control c in card.Controls) c.Click += (s, e) => onClick();

            return card;
        }
        private void DemarrerTimers()
        {
            _mapExploreTimer = new Timer { Interval = 600 };
            _mapExploreTimer.Tick += async (s, e) =>
            {
                _mapExploreTimer.Stop();
                if (_globeZoom >= 1.5)
                {
                    var villes = await _meteo.ObtenirVillesProches(_globeCenterLat, _globeCenterLon);
                    _villesExplorees = villes;
                    if (_panelCarte != null && !_panelCarte.IsDisposed) _panelCarte.Invalidate();
                }
                else
                {
                    _villesExplorees?.Clear();
                    if (_panelCarte != null && !_panelCarte.IsDisposed) _panelCarte.Invalidate();
                }
            };

            _spinnerTimer = new Timer { Interval = 40 };
            _spinnerTimer.Tick += (s, e) =>
            {
                _spinnerAngle = (_spinnerAngle + 15) % 360;
                if (_chargement) _panelSpinner.Invalidate();
                _pulseAlpha += _pulseUp ? 10 : -10;
                if (_pulseAlpha >= 175) _pulseUp = false;
                if (_pulseAlpha <= 15) _pulseUp = true;
                if (_panelCarte != null) _panelCarte.Invalidate();
            };
            _spinnerTimer.Start();

            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { if (_lblHeure != null && !_lblHeure.IsDisposed) _lblHeure.Text = DateTime.Now.ToString("HH:mm"); };
            _clockTimer.Start();
        }

        private void SetChargement(bool val)
        {
            _chargement = val;
            _panelSpinner.Visible = val;
            if (val) _panelSpinner.BringToFront();
        }
        private void AppliquerTheme()
        {
            BackColor = BgMain;
            _lblVille.ForeColor = TextMain;
            _lblCoord.ForeColor = Color.FromArgb(100, 140, 200);
            _btnDarkMode.Text = _modeSombre ? "☀" : "🌙";
            _panelCentral.Invalidate();
            _header.Invalidate();
            _statusBar.Invalidate();
            _panelCarte?.Invalidate();
            _panelDetail.Invalidate();
            if (_prevActuelles != null) AfficherCardsAvecAnimation(_prevActuelles);
        }
        private void UpdateBtnFavori()
        {
            if (_villeActuelle == null) { _btnFavori.Enabled = false; return; }
            _btnFavori.Enabled = true;
            bool isFav = _favoris.Any(f => string.Equals(f.Nom, _villeActuelle.Nom, StringComparison.OrdinalIgnoreCase));
            if (isFav)
            {
                _btnFavori.Text = "★ Retirer";
                _btnFavori.ForeColor = Color.FromArgb(255, 100, 100);
            }
            else
            {
                _btnFavori.Text = "★ Favori";
                _btnFavori.ForeColor = AccentGold;
            }
        }

        private void BtnFavori_Click(object sender, EventArgs e)
        {
            if (_villeActuelle == null) return;
            bool isFav = _favoris.Any(f => string.Equals(f.Nom, _villeActuelle.Nom, StringComparison.OrdinalIgnoreCase));
            if (isFav)
            {
                _favorisService.Supprimer(_favoris, _villeActuelle.Nom);
                SetStatut($"✕  {_villeActuelle.Nom} retiré des favoris.");
            }
            else
            {
                _favorisService.Ajouter(_favoris, _villeActuelle.Nom, _villeActuelle.Pays, _villeActuelle.Latitude, _villeActuelle.Longitude);
                SetStatut($"★  {_villeActuelle.Nom} ajouté aux favoris.");
            }
            UpdateBtnFavori();
            if (_panelCarte != null) _panelCarte.Invalidate();
        }
        private async void ChargerIcone(PictureBox pic, string code)
        {
            try
            {
                using var client = new HttpClient();
                byte[] data = await client.GetByteArrayAsync(MeteoService.GetIconeUrl(code));
                using var ms = new System.IO.MemoryStream(data);
                if (!pic.IsDisposed) pic.Image = Image.FromStream(ms);
            }
            catch { }
        }

        private Color CouleurTemp(double t)
        {
            if (t <= 0) return Color.FromArgb(120, 190, 255);
            if (t <= 10) return Color.FromArgb(80, 155, 255);
            if (t <= 18) return Color.FromArgb(52, 211, 153);
            if (t <= 25) return Color.FromArgb(255, 196, 57);
            if (t <= 32) return Color.FromArgb(255, 140, 50);
            return Color.FromArgb(255, 80, 60);
        }

        private void SetStatut(string msg) => _lblStatut.Text = "  " + msg;

        private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
        {
            var p = new GraphicsPath();
            p.AddArc(x, y, r * 2, r * 2, 180, 90);
            p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        private Button CreerBtnHeader(string text, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 32), BackColor = bg, FlatStyle = FlatStyle.Flat, ForeColor = fg, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand, Parent = _header };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(bg, 0.15f);
            b.MouseLeave += (s, e) => b.BackColor = bg;
            return b;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_panelCards == null) return;
            int cw = ClientSize.Width - 28;
            _panelCards.Width = Math.Min(cw - 360, 870);
            _panelSpinner.Size = _panelCards.Size;
            _panelSpinner.Location = _panelCards.Location;
            _panelDetail.Width = _panelCards.Width;
            _panelCarte.Left = _panelCards.Right + 18;
            _panelCarte.Width = cw - _panelCards.Width - 18;
            _panelCarte.Height = _panelDetail.Bottom - _panelCards.Top;
            _panelCarte.Invalidate();
            _panelDetail.Invalidate();
        }
    }
}