using MeteoApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MeteoApp.Forms
{
    public partial class MainForm
    {
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
                    _mapExploreTimer?.Stop();
                    _mapExploreTimer?.Start();
                }
            };
            _panelCarte.MouseEnter += (s, e) =>
            {
                _panelCarte.Focus();
                _showCursorAura = true;
            };
            _panelCarte.MouseLeave += (s, e) =>
            {
                _showCursorAura = false;
                if (_hoveredVille != null)
                {
                    _hoveredVille = null;
                    _panelCarte.Cursor = Cursors.Default;
                }

                _panelCarte.Invalidate();
            };
            _panelCarte.MouseWheel += (s, e) =>
            {
                if (e.Delta > 0) _globeZoom *= 1.25;
                else if (e.Delta < 0) _globeZoom /= 1.25;
                if (_globeZoom < 1.0) _globeZoom = 1.0;
                if (_globeZoom > 30.0) _globeZoom = 30.0;
                _panelCarte.Invalidate();
                _mapExploreTimer?.Stop();
                _mapExploreTimer?.Start();
            };
            _panelCarte.MouseMove += (s, e) =>
            {
                _cursorAuraPos = e.Location;
                _showCursorAura = true;

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
                int W = _panelCarte.Width;
                int H = _panelCarte.Height;
                var list = new Dictionary<string, PointF>();

                if (_favoris != null)
                {
                    foreach (var f in _favoris)
                    {
                        var p = ProjectGlobe(f.Latitude, f.Longitude, W, H);
                        if (p.HasValue) list[f.Nom] = p.Value;
                    }
                }

                if (_villeActuelle != null)
                {
                    var p = ProjectGlobe(_villeActuelle.Latitude, _villeActuelle.Longitude, W, H);
                    if (p.HasValue) list[_villeActuelle.Nom] = p.Value;
                }

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

        private Button CreerBtnHeader(string text, int x, int y, int w, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 32),
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                ForeColor = fg,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Parent = _header
            };
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
