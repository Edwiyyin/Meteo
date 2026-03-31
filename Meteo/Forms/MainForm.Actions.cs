using MeteoApp.Models;
using MeteoApp.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace MeteoApp.Forms
{
    public partial class MainForm
    {
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
            int cardW = 162;
            int gap = 10;

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
            catch
            {
            }
        }

        private void SetStatut(string msg) => _lblStatut.Text = "  " + msg;
    }
}
