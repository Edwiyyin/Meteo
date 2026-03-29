using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace MeteoApp.Forms
{
    public partial class MainForm
    {
        private void Spinner_Paint(object sender, PaintEventArgs e)
        {
            if (!_chargement) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = _panelSpinner.Width / 2;
            int cy = _panelSpinner.Height / 2;

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
            int W = _panelDetail.Width;
            int H = _panelDetail.Height;

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

            int xs = 455;
            int ys = 44;
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
                bool isHovered = i == _hoveredChartIndex;
                bool isActive = _jourDetail == prevs[i];

                if (isHovered || isActive)
                {
                    using var hlPen = new Pen(Color.FromArgb(20, Accent), _chartZones[i].Width * 0.8f);
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
            int W = _panelCarte.Width;
            int H = _panelCarte.Height;

            using var path = RoundedRect(0, 0, W - 1, H - 1, 14);
            using var bgBr = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                _modeSombre ? Color.FromArgb(16, 20, 40) : Color.FromArgb(220, 235, 255),
                _modeSombre ? Color.FromArgb(8, 12, 28) : Color.FromArgb(195, 215, 255), 90f);
            g.FillPath(bgBr, path);
            using var borderPen = new Pen(Color.FromArgb(70, Accent), 1.5f);
            g.DrawPath(borderPen, path);
            float globR = (float)(Math.Min(W, H) / 2.0 * 0.85);
            using var globeBgBr = new SolidBrush(_modeSombre ? Color.FromArgb(40, 20, 30, 60) : Color.FromArgb(40, 150, 185, 240));
            g.FillEllipse(globeBgBr, W / 2f - globR, H / 2f - globR, globR * 2, globR * 2);
            using var globeBorderPen = new Pen(Color.FromArgb(100, Accent), 1f);
            g.DrawEllipse(globeBorderPen, W / 2f - globR, H / 2f - globR, globR * 2, globR * 2);

            using var gridPen = new Pen(Color.FromArgb(35, Accent), 1f);
            for (int lat = -80; lat <= 80; lat += 20)
            {
                var pts = new List<PointF>();
                for (int lon = -180; lon <= 180; lon += 5)
                {
                    var p = ProjectGlobe(lat, lon, W, H);
                    if (p.HasValue) pts.Add(p.Value);
                    else if (pts.Count > 0)
                    {
                        if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray());
                        pts.Clear();
                    }
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
                    else if (pts.Count > 0)
                    {
                        if (pts.Count > 1) g.DrawLines(gridPen, pts.ToArray());
                        pts.Clear();
                    }
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
                        float lat1 = coords[i];
                        float lon1 = coords[i + 1];
                        float lat2 = coords[i + 2];
                        float lon2 = coords[i + 3];
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
            {
                foreach (var f in _favoris) toDraw[f.Nom] = (f.Latitude, f.Longitude, true, false, false);
            }

            if (_villeActuelle != null && !toDraw.ContainsKey(_villeActuelle.Nom))
            {
                toDraw[_villeActuelle.Nom] = (_villeActuelle.Latitude, _villeActuelle.Longitude, false, false, false);
            }

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

                float dxAura = pt.X - _cursorAuraPos.X;
                float dyAura = pt.Y - _cursorAuraPos.Y;
                bool isInAura = !_showCursorAura || ((dxAura * dxAura) + (dyAura * dyAura) <= (58f * 58f));

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
                    if (!isInAura) continue;
                    if (isExp && !isInAura) continue;

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

            if (_showCursorAura)
            {
                bool hoveringCity = !string.IsNullOrEmpty(_hoveredVille);
                Color auraColor = hoveringCity ? AccentGold : Accent;
                int outerSize = hoveringCity ? 84 : 74;
                int pulse = 70 + (_pulseAlpha / 3);

                var outerRect = new Rectangle(
                    _cursorAuraPos.X - outerSize / 2,
                    _cursorAuraPos.Y - outerSize / 2,
                    outerSize,
                    outerSize);

                using var auraPath = new GraphicsPath();
                auraPath.AddEllipse(outerRect);
                using var auraBrush = new PathGradientBrush(auraPath)
                {
                    CenterColor = Color.FromArgb(pulse, auraColor),
                    SurroundColors = new[] { Color.FromArgb(0, auraColor) }
                };
                g.FillEllipse(auraBrush, outerRect);

                using var auraPen = new Pen(Color.FromArgb(130, auraColor), hoveringCity ? 1.8f : 1.3f);
                g.DrawEllipse(auraPen, outerRect);
            }
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
    }
}
