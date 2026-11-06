using System;

namespace Bascule.Core
{
    /// <summary>Couleur RGBA 8 bits, indépendante de Unity (convertie en Color32 côté Unity).</summary>
    public struct Rgba
    {
        public byte r, g, b, a;

        public Rgba(byte r, byte g, byte b, byte a = 255) { this.r = r; this.g = g; this.b = b; this.a = a; }

        public static readonly Rgba Clear = new Rgba(0, 0, 0, 0);
        public static readonly Rgba White = new Rgba(255, 255, 255);

        public static Rgba FromHex(string hex, byte alpha = 255)
        {
            if (string.IsNullOrEmpty(hex)) return new Rgba(255, 0, 255, alpha);
            string h = hex.TrimStart('#');
            if (h.Length < 6) return new Rgba(255, 0, 255, alpha);
            byte P(int i) => Convert.ToByte(h.Substring(i, 2), 16);
            return new Rgba(P(0), P(2), P(4), h.Length >= 8 ? P(6) : alpha);
        }

        public Rgba WithAlpha(byte alpha) => new Rgba(r, g, b, alpha);

        public Rgba WithAlpha(float alpha) => new Rgba(r, g, b, B(alpha * 255f));

        public static Rgba Lerp(Rgba x, Rgba y, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Rgba(B(x.r + (y.r - x.r) * t), B(x.g + (y.g - x.g) * t), B(x.b + (y.b - x.b) * t), B(x.a + (y.a - x.a) * t));
        }

        /// <summary>Ombre « froide » : plus sombre et légèrement bleutée, comme en illustration.</summary>
        public Rgba Shadow(float k = 0.78f) => Lerp(Shade(k), new Rgba(70, 60, 130, a), 0.12f);

        /// <summary>Lumière « chaude ».</summary>
        public Rgba Light(float k = 1.28f) => Lerp(Shade(k), new Rgba(255, 245, 220, a), 0.1f);

        /// <summary>Contour : version très sombre et saturée de la couleur.</summary>
        public Rgba Outline() => Lerp(Shade(0.42f), new Rgba(40, 25, 60, a), 0.35f);

        /// <summary>Assombrit (k &lt; 1) ou éclaircit vers le blanc (k &gt; 1).</summary>
        public Rgba Shade(float k)
        {
            if (k <= 1f) return new Rgba(B(r * k), B(g * k), B(b * k), a);
            float t = k - 1f;
            return new Rgba(B(r + (255 - r) * t), B(g + (255 - g) * t), B(b + (255 - b) * t), a);
        }

        public Rgba Grey()
        {
            byte l = B(0.3f * r + 0.59f * g + 0.11f * b);
            return new Rgba(l, l, l, a);
        }

        /// <summary>Luminance relative (WCAG), pour choisir un texte lisible.</summary>
        public float Luminance()
        {
            float C(byte c) { float v = c / 255f; return v <= 0.03928f ? v / 12.92f : (float)Math.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * C(r) + 0.7152f * C(g) + 0.0722f * C(b);
        }

        public static float Contrast(Rgba x, Rgba y)
        {
            float a = x.Luminance() + 0.05f, b = y.Luminance() + 0.05f;
            return a > b ? a / b : b / a;
        }

        static byte B(float v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v + 0.5f);
    }

    /// <summary>
    /// Petit moteur de dessin vectoriel anticrénelé (formes rondes, traits nets, aplats : la DA demandée).
    /// Sert à fabriquer au lancement tous les visuels (mascotte, 40 têtes, poids, boutons) sans fichier image.
    /// Coordonnées en pixels, origine en bas à gauche (convention des textures Unity).
    /// </summary>
    public sealed class Raster
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Rgba[] Pixels;

        public Raster(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Pixels = new Rgba[Width * Height];
        }

        public Rgba Get(int x, int y) => Pixels[y * Width + x];

        public void Fill(Rgba c) { for (int i = 0; i < Pixels.Length; i++) Pixels[i] = c; }

        // ---------- Formes ----------

        public void Circle(float cx, float cy, float r, Rgba c) =>
            DrawShape(cx - r, cy - r, cx + r, cy + r, c, (x, y) => Len(x - cx, y - cy) - r);

        public void Ellipse(float cx, float cy, float rx, float ry, Rgba c)
        {
            float m = Math.Min(rx, ry);
            DrawShape(cx - rx, cy - ry, cx + rx, cy + ry, c, (x, y) => (Len((x - cx) / rx, (y - cy) / ry) - 1f) * m);
        }

        public void Ring(float cx, float cy, float r, float thickness, Rgba c) =>
            DrawShape(cx - r - thickness, cy - r - thickness, cx + r + thickness, cy + r + thickness, c,
                (x, y) => Math.Abs(Len(x - cx, y - cy) - r) - thickness * 0.5f);

        /// <summary>Rectangle aux coins arrondis, centré, éventuellement tourné (degrés).</summary>
        public void RoundRect(float cx, float cy, float halfW, float halfH, float radius, Rgba c, float rotationDeg = 0f)
        {
            radius = Math.Min(radius, Math.Min(halfW, halfH));
            float cos = (float)Math.Cos(-rotationDeg * Math.PI / 180), sin = (float)Math.Sin(-rotationDeg * Math.PI / 180);
            float ext = Len(halfW, halfH);
            DrawShape(cx - ext, cy - ext, cx + ext, cy + ext, c, (x, y) =>
            {
                float px = x - cx, py = y - cy;
                float lx = px * cos - py * sin, ly = px * sin + py * cos;
                float qx = Math.Abs(lx) - (halfW - radius), qy = Math.Abs(ly) - (halfH - radius);
                return Len(Math.Max(qx, 0), Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - radius;
            });
        }

        /// <summary>Segment épais aux bouts ronds (bras, jambes, traits).</summary>
        public void Capsule(float ax, float ay, float bx, float by, float r, Rgba c) =>
            DrawShape(Math.Min(ax, bx) - r, Math.Min(ay, by) - r, Math.Max(ax, bx) + r, Math.Max(ay, by) + r, c,
                (x, y) => SegmentDistance(x, y, ax, ay, bx, by) - r);

        /// <summary>Arc de cercle épais. Angles en degrés, 0 = droite, sens trigonométrique.</summary>
        public void Arc(float cx, float cy, float radius, float startDeg, float endDeg, float thickness, Rgba c)
        {
            float a0 = (float)(startDeg * Math.PI / 180), a1 = (float)(endDeg * Math.PI / 180);
            if (a1 < a0) { var t = a0; a0 = a1; a1 = t; }
            float half = thickness * 0.5f;
            float e0x = cx + radius * (float)Math.Cos(a0), e0y = cy + radius * (float)Math.Sin(a0);
            float e1x = cx + radius * (float)Math.Cos(a1), e1y = cy + radius * (float)Math.Sin(a1);
            float ext = radius + thickness;
            DrawShape(cx - ext, cy - ext, cx + ext, cy + ext, c, (x, y) =>
            {
                float ang = (float)Math.Atan2(y - cy, x - cx);
                while (ang < a0) ang += (float)(2 * Math.PI);
                if (ang <= a1) return Math.Abs(Len(x - cx, y - cy) - radius) - half;
                return Math.Min(Len(x - e0x, y - e0y), Len(x - e1x, y - e1y)) - half;
            });
        }

        /// <summary>Polygone convexe (sommets dans un sens quelconque).</summary>
        public void Polygon(Rgba c, params float[] xy)
        {
            int n = xy.Length / 2;
            if (n < 3) return;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            float area = 0;
            for (int i = 0; i < n; i++)
            {
                float x0 = xy[2 * i], y0 = xy[2 * i + 1], x1 = xy[2 * ((i + 1) % n)], y1 = xy[2 * ((i + 1) % n) + 1];
                area += x0 * y1 - x1 * y0;
                minX = Math.Min(minX, x0); maxX = Math.Max(maxX, x0); minY = Math.Min(minY, y0); maxY = Math.Max(maxY, y0);
            }
            float sign = area >= 0 ? 1f : -1f;
            DrawShape(minX, minY, maxX, maxY, c, (x, y) =>
            {
                float d = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    float x0 = xy[2 * i], y0 = xy[2 * i + 1], x1 = xy[2 * ((i + 1) % n)], y1 = xy[2 * ((i + 1) % n) + 1];
                    float ex = x1 - x0, ey = y1 - y0, len = Len(ex, ey);
                    if (len < 1e-5f) continue;
                    // Distance signée à la droite (positive à l'extérieur).
                    float side = -sign * (ex * (y - y0) - ey * (x - x0)) / len;
                    d = Math.Max(d, side);
                }
                return d;
            });
        }

        /// <summary>Superpose une autre image de même taille (fusion « par-dessus »).</summary>
        public void Composite(Raster top)
        {
            if (top.Width != Width || top.Height != Height) throw new ArgumentException("Taille différente");
            for (int i = 0; i < Pixels.Length; i++)
            {
                var s = top.Pixels[i];
                if (s.a == 0) continue;
                Pixels[i] = Blend(Pixels[i], s, s.a / 255f);
            }
        }

        /// <summary>Copie retournée de gauche à droite (rebord droit de la planche).</summary>
        public Raster FlippedX()
        {
            var r = new Raster(Width, Height);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    r.Pixels[y * Width + x] = Pixels[y * Width + (Width - 1 - x)];
            return r;
        }

        /// <summary>Superpose une image plus petite, son coin bas-gauche posé en (dx, dy).</summary>
        public void CompositeAt(Raster top, int dx, int dy)
        {
            for (int y = 0; y < top.Height; y++)
            {
                int ty = y + dy;
                if (ty < 0 || ty >= Height) continue;
                for (int x = 0; x < top.Width; x++)
                {
                    int tx = x + dx;
                    if (tx < 0 || tx >= Width) continue;
                    var s = top.Pixels[y * top.Width + x];
                    if (s.a == 0) continue;
                    int i = ty * Width + tx;
                    Pixels[i] = Blend(Pixels[i], s, s.a / 255f);
                }
            }
        }

        /// <summary>Recopie la couleur des bords dans les pixels transparents voisins pour éviter les liserés sombres au filtrage.</summary>
        public void BleedEdges(int passes = 2)
        {
            var filled = new bool[Pixels.Length];
            for (int i = 0; i < Pixels.Length; i++) filled[i] = Pixels[i].a != 0;
            for (int p = 0; p < passes; p++)
            {
                var src = (Rgba[])Pixels.Clone();
                var was = (bool[])filled.Clone();
                for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (was[i]) continue;
                    int rs = 0, gs = 0, bs = 0, n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        int j = ny * Width + nx;
                        if (!was[j]) continue;
                        rs += src[j].r; gs += src[j].g; bs += src[j].b; n++;
                    }
                    if (n == 0) continue;
                    Pixels[i] = new Rgba((byte)(rs / n), (byte)(gs / n), (byte)(bs / n), 0);
                    filled[i] = true;
                }
            }
        }

        /// <summary>Version grisée (têtes verrouillées de la collection).</summary>
        public Raster Silhouette(Rgba color)
        {
            var r = new Raster(Width, Height);
            for (int i = 0; i < Pixels.Length; i++)
                r.Pixels[i] = Pixels[i].a == 0 ? Rgba.Clear : color.WithAlpha(Pixels[i].a);
            return r;
        }

        // ---------- Formes composées (Shape) ----------

        /// <summary>Remplit une forme. feather = largeur du fondu de bord en pixels (1 = net, anticrénelé).</summary>
        public void Fill(Shape s, Paint p, float feather = 1f, float opacity = 1f)
        {
            DrawShape(s.X0 - feather, s.Y0 - feather, s.X1 + feather, s.Y1 + feather, (x, y) =>
            {
                float d = s.D(x, y);
                return Coverage(d, feather);
            }, p, opacity);
        }

        /// <summary>Remplit la zone « region » (bord adouci de feather px) en restant strictement dans « clip » (bord net).</summary>
        public void FillClipped(Shape clip, Shape region, Paint p, float feather, float opacity = 1f)
        {
            DrawShape(clip.X0, clip.Y0, clip.X1, clip.Y1, (x, y) =>
            {
                float c = Coverage(clip.D(x, y), 1f);
                if (c <= 0f) return 0f;
                return c * Coverage(region.D(x, y), feather);
            }, p, opacity);
        }

        static float Coverage(float d, float feather)
        {
            if (feather <= 1f) { float c = 0.5f - d; return c < 0 ? 0 : c > 1 ? 1 : c; }
            float t = 0.5f - d / feather;
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            return t * t * (3 - 2 * t);
        }

        void DrawShape(float minX, float minY, float maxX, float maxY, Func<float, float, float> coverage, Paint p, float opacity)
        {
            int x0 = Math.Max(0, (int)Math.Floor(minX - 1)), y0 = Math.Max(0, (int)Math.Floor(minY - 1));
            int x1 = Math.Min(Width - 1, (int)Math.Ceiling(maxX + 1)), y1 = Math.Min(Height - 1, (int)Math.Ceiling(maxY + 1));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float cov = coverage(px, py);
                if (cov <= 0f) continue;
                var c = p.At(px, py);
                int i = y * Width + x;
                Pixels[i] = Blend(Pixels[i], c, cov * opacity * c.a / 255f);
            }
        }

        /// <summary>Flou (boîte, plusieurs passes ≈ gaussien) : ombres portées, halos.</summary>
        public void Blur(int radius, int passes = 3)
        {
            if (radius <= 0) return;
            var buf = new float[Pixels.Length * 4];
            for (int i = 0; i < Pixels.Length; i++)
            {
                float a = Pixels[i].a / 255f;
                buf[i * 4] = Pixels[i].r * a; buf[i * 4 + 1] = Pixels[i].g * a; buf[i * 4 + 2] = Pixels[i].b * a; buf[i * 4 + 3] = a;
            }
            var tmp = new float[buf.Length];
            for (int p = 0; p < passes; p++)
            {
                BoxPass(buf, tmp, Width, Height, radius, true);
                BoxPass(tmp, buf, Width, Height, radius, false);
            }
            for (int i = 0; i < Pixels.Length; i++)
            {
                float a = buf[i * 4 + 3];
                if (a <= 0.0001f) { Pixels[i] = Rgba.Clear; continue; }
                Pixels[i] = new Rgba(ToByte(buf[i * 4] / a), ToByte(buf[i * 4 + 1] / a), ToByte(buf[i * 4 + 2] / a), ToByte(a * 255f));
            }
        }

        static void BoxPass(float[] src, float[] dst, int w, int h, int r, bool horizontal)
        {
            int len = horizontal ? w : h, lines = horizontal ? h : w;
            float inv = 1f / (2 * r + 1);
            for (int l = 0; l < lines; l++)
            {
                for (int ch = 0; ch < 4; ch++)
                {
                    float acc = 0;
                    for (int k = -r; k <= r; k++) acc += src[Idx(Clamp(k, len), l, horizontal, w) * 4 + ch];
                    for (int k = 0; k < len; k++)
                    {
                        dst[Idx(k, l, horizontal, w) * 4 + ch] = acc * inv;
                        acc += src[Idx(Clamp(k + r + 1, len), l, horizontal, w) * 4 + ch] - src[Idx(Clamp(k - r, len), l, horizontal, w) * 4 + ch];
                    }
                }
            }
        }

        static int Clamp(int v, int len) => v < 0 ? 0 : v >= len ? len - 1 : v;
        static int Idx(int k, int l, bool horizontal, int w) => horizontal ? l * w + k : k * w + l;
        static byte ToByte(float v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v + 0.5f);

        public Raster Clone()
        {
            var r = new Raster(Width, Height);
            Array.Copy(Pixels, r.Pixels, Pixels.Length);
            return r;
        }

        /// <summary>Copie décalée (en pixels) de l'image, pour les ombres portées.</summary>
        public Raster Offset(int dx, int dy)
        {
            var r = new Raster(Width, Height);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int sx = x - dx, sy = y - dy;
                if (sx < 0 || sy < 0 || sx >= Width || sy >= Height) continue;
                r.Pixels[y * Width + x] = Pixels[sy * Width + sx];
            }
            return r;
        }

        /// <summary>Remplace toutes les couleurs par une seule en gardant l'opacité (multipliée par opacity).</summary>
        public Raster Colorize(Rgba c, float opacity = 1f)
        {
            var r = new Raster(Width, Height);
            for (int i = 0; i < Pixels.Length; i++)
                r.Pixels[i] = c.WithAlpha((byte)Math.Min(255, Pixels[i].a * opacity * c.a / 255f));
            return r;
        }

        /// <summary>Pose cette image sous une autre (pour glisser une ombre portée dessous).</summary>
        public void Underlay(Raster below)
        {
            for (int i = 0; i < Pixels.Length; i++)
            {
                var top = Pixels[i];
                var bottom = below.Pixels[i];
                if (bottom.a == 0) continue;
                Pixels[i] = Blend(bottom, top, top.a / 255f);
            }
        }

        // ---------- Interne ----------

        void DrawShape(float minX, float minY, float maxX, float maxY, Rgba c, Func<float, float, float> sdf)
        {
            int x0 = Math.Max(0, (int)Math.Floor(minX - 1)), y0 = Math.Max(0, (int)Math.Floor(minY - 1));
            int x1 = Math.Min(Width - 1, (int)Math.Ceiling(maxX + 1)), y1 = Math.Min(Height - 1, (int)Math.Ceiling(maxY + 1));
            float alpha = c.a / 255f;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = sdf(x + 0.5f, y + 0.5f);
                float cov = 0.5f - d;
                if (cov <= 0f) continue;
                if (cov > 1f) cov = 1f;
                int i = y * Width + x;
                Pixels[i] = Blend(Pixels[i], c, cov * alpha);
            }
        }

        static Rgba Blend(Rgba dst, Rgba src, float sa)
        {
            if (sa <= 0f) return dst;
            float da = dst.a / 255f;
            float oa = sa + da * (1f - sa);
            if (oa <= 0f) return Rgba.Clear;
            float k = da * (1f - sa);
            byte Ch(byte s, byte d) => (byte)Math.Min(255, (s * sa + d * k) / oa + 0.5f);
            return new Rgba(Ch(src.r, dst.r), Ch(src.g, dst.g), Ch(src.b, dst.b), (byte)Math.Min(255, oa * 255f + 0.5f));
        }

        static float Len(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float l2 = abx * abx + aby * aby;
            float t = l2 > 0 ? ((px - ax) * abx + (py - ay) * aby) / l2 : 0;
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return Len(px - (ax + t * abx), py - (ay + t * aby));
        }
    }
}
