using System;

namespace Bascule.Core
{
    /// <summary>
    /// Mondes du décor au-delà de la prairie : plage, forêt, désert, montagne, ville, volcan, banquise, Lune.
    /// Mêmes tailles d'images que la prairie (Backdrop les place pareil) : seul le dessin change.
    /// Le sol garde son bord supérieur à 82 % de la hauteur (pivot du sprite).
    /// </summary>
    public static partial class SceneArtist
    {
        static readonly Toon.Style Flat = new Toon.Style { Outline = 0f, Shade = 0.04f, Soft = 0.06f, ShadeStrength = 0.88f, Rim = 0.01f, RimOpacity = 0.35f, Specular = false };
        static readonly Toon.Style Prop = new Toon.Style { Outline = 0.04f, Shade = 0.2f, Soft = 0.2f, Rim = 0.06f, RimOpacity = 0.4f, Specular = false };

        static Raster BiomeHills(Theme t, bool far, int w, int h)
        {
            var img = new Raster(w, h);
            var color = far ? t.FarHills : t.NearHills;
            var rng = new GameRng((uint)(far ? 101 : 211) + (uint)t.Biome * 17u);
            switch (t.Biome)
            {
                case Biome.Beach: if (far) Sea(img, t, rng, w, h); else Dunes(img, t, rng, w, h, true); break;
                case Biome.Desert: if (far) Mesas(img, color, rng, w, h); else Dunes(img, t, rng, w, h, false); break;
                case Biome.Forest: Forest(img, t, color, rng, w, h, far); break;
                case Biome.Mountains: if (far) Peaks(img, color, rng, w, h); else Forest(img, t, color, rng, w, h, false, snow: true); break;
                case Biome.City: Skyline(img, t, color, rng, w, h, far); break;
                case Biome.Volcano: if (far) Volcano(img, t, color, rng, w, h); else Rocks(img, t, color, rng, w, h); break;
                case Biome.Ice: if (far) Icebergs(img, t, rng, w, h); else Drifts(img, t, color, rng, w, h); break;
                case Biome.Moon: MoonHills(img, color, rng, w, h, far); break;
            }
            return img;
        }

        /// <summary>Bande de relief doux (collines arrondies) en guise de base.</summary>
        static Shape Rolling(GameRng rng, int w, int h, float level, float amp, int bumps)
        {
            Shape s = Shape.Box(w * 0.5f, h * level * 0.5f, w * 0.6f, h * level * 0.5f);
            for (int i = 0; i <= bumps; i++)
            {
                float x = w * (i / (float)bumps) + (rng.NextFloat() - 0.5f) * w * 0.1f;
                float r = w * 0.5f / bumps * (1f + rng.NextFloat() * 0.6f);
                s = Shape.Smooth(s, Shape.Ellipse(x, h * level + h * amp * rng.NextFloat() - r * 0.5f, r * 1.4f, r * 0.8f), w * 0.05f);
            }
            return s;
        }

        // ------------------------------------------------------------------ Plage

        static void Sea(Raster img, Theme t, GameRng rng, int w, int h)
        {
            float top = h * 0.3f;
            var sea = Shape.Box(w * 0.5f, top * 0.5f, w * 0.6f, top * 0.5f);
            img.Fill(sea, Paint.Custom((x, y) => Rgba.Lerp(t.FarHills.Shade(0.8f), t.FarHills.Light(1.12f), y / top)));
            // Reflets et écume.
            for (int i = 0; i < 26; i++)
            {
                float x = rng.NextFloat() * w, y = top * (0.15f + rng.NextFloat() * 0.8f), l = w * (0.01f + rng.NextFloat() * 0.03f);
                img.Fill(Shape.Capsule(x - l, y, x + l, y, h * 0.004f), Rgba.White.WithAlpha(0.55f), 1.5f);
            }
            // Petit voilier à l'horizon.
            float bx = w * 0.72f, by = top;
            Toon.Part(img, Shape.Poly(bx - w * 0.035f, by + h * 0.01f, bx + w * 0.035f, by + h * 0.01f, bx + w * 0.022f, by - h * 0.02f, bx - w * 0.022f, by - h * 0.02f), Rgba.FromHex("#E63946"), w * 0.05f, Prop);
            Toon.Part(img, Shape.Poly(bx, by + h * 0.02f, bx, by + h * 0.13f, bx + w * 0.03f, by + h * 0.025f), Rgba.White, w * 0.05f, Prop);
        }

        static void Dunes(Raster img, Theme t, GameRng rng, int w, int h, bool palms)
        {
            var dunes = Rolling(rng, w, h, 0.22f, 0.1f, 3);
            Toon.Part(img, dunes, t.NearHills, w, Flat);
            // Rides du sable.
            for (int i = 0; i < 12; i++)
            {
                float x = rng.NextFloat() * w, y = TopOf(dunes, x, h) - h * (0.03f + rng.NextFloat() * 0.08f);
                img.FillClipped(dunes, Shape.Arc(x, y - w * 0.06f, w * 0.06f, 60f, 120f, h * 0.004f), t.NearHills.Shade(0.88f).WithAlpha(0.6f), 1.5f);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = w * (0.12f + i * 0.36f + rng.NextFloat() * 0.08f);
                float top = TopOf(dunes, x, h);
                float s = h * (0.2f + rng.NextFloat() * 0.06f);
                if (palms) Palm(img, x, top, s, rng.NextFloat() > 0.5f ? 1 : -1);
                else Cactus(img, x, top, s * 0.75f);
            }
        }

        static void Palm(Raster img, float x, float y, float s, int lean)
        {
            var trunk = Shape.Tapered(x, y, s * 0.07f, x + lean * s * 0.25f, y + s, s * 0.045f);
            Toon.Part(img, trunk, Rgba.FromHex("#A5713F"), s, Prop);
            for (int k = 1; k < 6; k++)
            {
                float f = k / 6f;
                img.FillClipped(trunk, Shape.Arc(x + lean * s * 0.25f * f * f, y + s * f - s * 0.06f, s * 0.08f, 60f, 120f, s * 0.008f), Rgba.FromHex("#6E4A2A"), 1.5f);
            }
            float tx = x + lean * s * 0.25f, ty = y + s;
            var leaf = Rgba.FromHex("#3FA34D");
            for (int k = 0; k < 6; k++)
            {
                double a = Math.PI * (0.08 + k * 0.17);
                float ex = tx + (float)Math.Cos(a) * s * 0.55f, ey = ty + (float)Math.Sin(a) * s * 0.28f - s * 0.12f;
                Toon.Part(img, Shape.Tapered(tx, ty, s * 0.07f, ex, ey, s * 0.02f), k % 2 == 0 ? leaf : leaf.Shade(0.85f), s, Prop);
            }
            Toon.Part(img, Shape.Circle(tx - s * 0.04f, ty - s * 0.06f, s * 0.05f), Rgba.FromHex("#6E4A2A"), s * 0.3f, Prop);
            Toon.Part(img, Shape.Circle(tx + s * 0.05f, ty - s * 0.07f, s * 0.05f), Rgba.FromHex("#6E4A2A"), s * 0.3f, Prop);
        }

        // ------------------------------------------------------------------ Désert

        static void Mesas(Raster img, Rgba color, GameRng rng, int w, int h)
        {
            Shape s = Shape.Box(w * 0.5f, h * 0.06f, w * 0.6f, h * 0.08f);
            for (int i = 0; i < 4; i++)
            {
                float x = w * (0.1f + i * 0.27f + rng.NextFloat() * 0.06f), hw = w * (0.06f + rng.NextFloat() * 0.06f), top = h * (0.28f + rng.NextFloat() * 0.18f);
                s = Shape.Union(s, Shape.Poly(x - hw * 1.6f, 0, x + hw * 1.6f, 0, x + hw, top, x - hw, top));
            }
            Toon.Part(img, s, color, w, Flat);
            // Strates de roche.
            for (int k = 1; k < 4; k++)
                img.FillClipped(s, Shape.Box(w * 0.5f, h * 0.07f * k + h * 0.05f, w, h * 0.008f), color.Shade(0.85f).WithAlpha(0.7f), 2f);
        }

        static void Cactus(Raster img, float x, float y, float s)
        {
            var green = Rgba.FromHex("#4F9D5A");
            var body = Shape.Union(
                Shape.Capsule(x, y, x, y + s, s * 0.11f),
                Shape.Union(Shape.Capsule(x - s * 0.1f, y + s * 0.42f, x - s * 0.3f, y + s * 0.42f, s * 0.07f), Shape.Capsule(x - s * 0.3f, y + s * 0.42f, x - s * 0.3f, y + s * 0.72f, s * 0.07f)),
                Shape.Union(Shape.Capsule(x + s * 0.1f, y + s * 0.55f, x + s * 0.27f, y + s * 0.55f, s * 0.07f), Shape.Capsule(x + s * 0.27f, y + s * 0.55f, x + s * 0.27f, y + s * 0.8f, s * 0.07f)));
            Toon.Part(img, body, green, s, Prop);
            img.FillClipped(body, Shape.Box(x, y + s * 0.5f, s * 0.01f, s), green.Shade(0.75f), 1.5f);
            Toon.Part(img, Shape.Circle(x, y + s + s * 0.08f, s * 0.05f), Rgba.FromHex("#F25CA2"), s * 0.2f, Prop);
        }

        // ------------------------------------------------------------------ Forêt et montagne

        static void Forest(Raster img, Theme t, Rgba color, GameRng rng, int w, int h, bool far, bool snow = false)
        {
            var hills = Rolling(rng, w, h, far ? 0.3f : 0.2f, 0.12f, far ? 4 : 3);
            Toon.Part(img, hills, color, w, Flat);
            int n = far ? 22 : 9;
            for (int i = 0; i < n; i++)
            {
                float x = w * (i + 0.2f + rng.NextFloat() * 0.6f) / n;
                float top = TopOf(hills, x, h);
                float s = h * (far ? 0.14f + rng.NextFloat() * 0.1f : 0.2f + rng.NextFloat() * 0.1f);
                Pine(img, x, top - s * 0.08f, s, far ? color.Shade(0.86f) : color.Shade(0.8f), far, snow);
            }
        }

        static void Pine(Raster img, float x, float y, float s, Rgba c, bool flat, bool snow)
        {
            if (!flat) Toon.Part(img, Shape.Box(x, y + s * 0.08f, s * 0.05f, s * 0.12f), Rgba.FromHex("#6B4226"), s, Prop);
            for (int k = 0; k < 3; k++)
            {
                float b = y + s * (0.15f + k * 0.24f), hw = s * (0.32f - k * 0.08f);
                var tier = Shape.Poly(x - hw, b, x + hw, b, x, b + s * 0.38f).Grow(s * 0.015f);
                Toon.Part(img, tier, c, s, flat ? Flat : Prop);
                if (snow) img.FillClipped(tier, Shape.Poly(x - hw * 0.45f, b + s * 0.21f, x + hw * 0.45f, b + s * 0.21f, x, b + s * 0.4f), Rgba.FromHex("#F4F8FF"), 1.5f);
            }
        }

        static void Peaks(Raster img, Rgba color, GameRng rng, int w, int h)
        {
            Shape s = Shape.Box(w * 0.5f, h * 0.08f, w * 0.6f, h * 0.1f);
            var tops = new float[5 * 3];
            for (int i = 0; i < 5; i++)
            {
                float x = w * (0.05f + i * 0.23f + rng.NextFloat() * 0.05f), hw = w * (0.13f + rng.NextFloat() * 0.06f), top = h * (0.55f + rng.NextFloat() * 0.35f);
                s = Shape.Union(s, Shape.Poly(x - hw, 0, x + hw, 0, x + hw * 0.12f, top, x - hw * 0.1f, top * 0.97f));
                tops[i * 3] = x; tops[i * 3 + 1] = top; tops[i * 3 + 2] = hw;
            }
            Toon.Part(img, s, color, w, Flat);
            // Face à l'ombre, neiges éternelles.
            for (int i = 0; i < 5; i++)
            {
                float x = tops[i * 3], top = tops[i * 3 + 1], hw = tops[i * 3 + 2];
                img.FillClipped(s, Shape.Poly(x + hw * 0.12f, top, x + hw, 0, x + hw * 0.3f, 0), color.Shade(0.86f).WithAlpha(0.8f), 2f);
                float sy = top * 0.72f;
                var cap = Shape.Poly(x - hw * 0.3f, sy, x - hw * 0.15f, sy + top * 0.04f, x, sy - top * 0.02f, x + hw * 0.15f, sy + top * 0.05f, x + hw * 0.32f, sy, x + hw * 0.12f, top * 1.02f, x - hw * 0.1f, top);
                img.FillClipped(s, cap, Rgba.FromHex("#F7FBFF"), 1.5f);
            }
        }

        // ------------------------------------------------------------------ Ville

        static void Skyline(Raster img, Theme t, Rgba color, GameRng rng, int w, int h, bool far)
        {
            int n = far ? 14 : 8;
            float x = -w * 0.02f;
            var lit = Rgba.FromHex("#FFD66B");
            while (x < w)
            {
                float bw = w * (far ? 0.05f + rng.NextFloat() * 0.04f : 0.09f + rng.NextFloat() * 0.06f);
                float bh = h * (far ? 0.3f + rng.NextFloat() * 0.5f : 0.22f + rng.NextFloat() * 0.3f);
                var tint = color.Shade(0.88f + rng.NextFloat() * 0.18f);
                var b = Shape.Box(x + bw * 0.5f, bh * 0.5f, bw * 0.5f, bh * 0.5f);
                Toon.Part(img, b, tint, bw * 2, far ? Flat : Prop);
                // Toits : antenne, réservoir d'eau ou toit pointu.
                float r = rng.NextFloat();
                if (r < 0.3f) img.Fill(Shape.Box(x + bw * 0.5f, bh + h * 0.04f, w * 0.002f, h * 0.04f), tint.Shade(0.8f));
                else if (r < 0.5f && !far)
                {
                    Toon.Part(img, Shape.Box(x + bw * 0.7f, bh + h * 0.03f, bw * 0.12f, h * 0.025f, h * 0.01f), Rgba.FromHex("#8B5A2B"), bw, Prop);
                    img.Fill(Shape.Box(x + bw * 0.7f, bh + h * 0.005f, bw * 0.1f, h * 0.006f), Rgba.FromHex("#5C3A21"));
                }
                // Fenêtres (allumées la nuit, reflets le jour).
                float ww = bw * (far ? 0.12f : 0.14f), wh = h * (far ? 0.018f : 0.03f);
                for (float wy = h * 0.05f; wy < bh - wh * 2; wy += wh * 2.4f)
                    for (float wx = x + bw * 0.18f; wx < x + bw * 0.86f; wx += ww * 2.2f)
                    {
                        bool on = t.Night ? rng.NextFloat() < 0.55f : rng.NextFloat() < 0.85f;
                        if (!on) continue;
                        var wc = t.Night ? lit.WithAlpha(0.6f + rng.NextFloat() * 0.4f) : Rgba.Lerp(tint, Rgba.FromHex("#DDF4FF"), 0.55f);
                        img.FillClipped(b, Shape.Box(wx + ww * 0.5f, wy + wh * 0.5f, ww * 0.5f, wh * 0.5f), wc, 1f);
                    }
                x += bw + w * (far ? 0.004f : 0.02f + rng.NextFloat() * 0.03f);
            }
            if (!far)
            {
                // Arbres de l'avenue devant les immeubles.
                for (int i = 0; i < 5; i++)
                {
                    float tx = w * (0.1f + i * 0.2f), s = h * 0.09f;
                    Toon.Part(img, Shape.Box(tx, s * 0.5f, s * 0.08f, s * 0.5f), Rgba.FromHex("#5C3A21"), s, Prop);
                    Toon.Part(img, Shape.Circle(tx, s * 1.15f, s * 0.55f), (t.Night ? Rgba.FromHex("#2F5A4A") : Rgba.FromHex("#58B56A")), s, Prop);
                }
            }
        }

        // ------------------------------------------------------------------ Volcan

        static void Volcano(Raster img, Theme t, Rgba color, GameRng rng, int w, int h)
        {
            float cx = w * 0.62f, top = h * 0.82f;
            var cone = Shape.Smooth(Shape.Box(w * 0.5f, h * 0.06f, w * 0.6f, h * 0.08f),
                Shape.Poly(cx - w * 0.36f, 0, cx + w * 0.36f, 0, cx + w * 0.07f, top, cx - w * 0.07f, top), w * 0.04f);
            Toon.Part(img, cone, color, w, Flat);
            var lava = Rgba.FromHex("#FF7A2F");
            // Cratère rougeoyant et coulées de lave.
            img.Fill(Shape.Ellipse(cx, top, w * 0.07f, h * 0.025f), Rgba.FromHex("#FFC15E"), 3f);
            img.Fill(Shape.Ellipse(cx, top + h * 0.03f, w * 0.12f, h * 0.08f), lava.WithAlpha(0.35f), h * 0.08f);
            for (int k = 0; k < 3; k++)
            {
                float sx = cx + (k - 1) * w * 0.035f, ex = cx + (k - 1) * w * 0.12f + (rng.NextFloat() - 0.5f) * w * 0.05f;
                img.FillClipped(cone, Shape.Tapered(sx, top, w * 0.012f, ex, h * (0.2f + rng.NextFloat() * 0.2f), w * 0.004f), lava, 2f);
            }
            // Panache de fumée.
            for (int k = 0; k < 4; k++)
                img.Fill(Shape.Circle(cx - w * 0.02f * k, top + h * 0.05f + k * h * 0.035f, w * (0.03f + k * 0.008f)), Rgba.FromHex("#5A4A50").WithAlpha(0.55f), 4f);
        }

        static void Rocks(Raster img, Theme t, Rgba color, GameRng rng, int w, int h)
        {
            Shape s = Shape.Box(w * 0.5f, h * 0.08f, w * 0.6f, h * 0.1f);
            for (int i = 0; i < 9; i++)
            {
                float x = w * (i + rng.NextFloat()) / 9f, hw = w * (0.05f + rng.NextFloat() * 0.05f), top = h * (0.18f + rng.NextFloat() * 0.18f);
                s = Shape.Union(s, Shape.Poly(x - hw, 0, x + hw, 0, x + hw * 0.4f, top, x - hw * 0.2f, top * 1.08f, x - hw * 0.6f, top * 0.7f));
            }
            Toon.Part(img, s, color, w, Flat);
            var lava = Rgba.FromHex("#FF7A2F");
            for (int i = 0; i < 8; i++)
            {
                float x = rng.NextFloat() * w, y = h * (0.03f + rng.NextFloat() * 0.12f);
                img.FillClipped(s, Shape.Capsule(x, y, x + w * (rng.NextFloat() - 0.5f) * 0.08f, y + h * 0.04f, h * 0.005f), lava.WithAlpha(0.85f), 2f);
            }
        }

        // ------------------------------------------------------------------ Banquise

        static void Icebergs(Raster img, Theme t, GameRng rng, int w, int h)
        {
            float top = h * 0.12f;
            img.Fill(Shape.Box(w * 0.5f, top * 0.5f, w * 0.6f, top * 0.5f), Rgba.FromHex("#0E3A5C"));
            for (int i = 0; i < 4; i++)
            {
                float x = w * (0.1f + i * 0.26f + rng.NextFloat() * 0.06f), hw = w * (0.06f + rng.NextFloat() * 0.05f), ih = h * (0.25f + rng.NextFloat() * 0.25f);
                var berg = Shape.Poly(x - hw, top * 0.6f, x + hw, top * 0.6f, x + hw * 0.6f, ih * 0.7f, x + hw * 0.1f, ih, x - hw * 0.4f, ih * 0.8f);
                Toon.Part(img, berg, t.FarHills, w, Flat);
                img.FillClipped(berg, Shape.Poly(x + hw * 0.1f, ih, x + hw, top * 0.6f, x + hw * 0.2f, top * 0.6f), t.FarHills.Shade(0.82f), 2f);
            }
            for (int i = 0; i < 18; i++)
            {
                float x = rng.NextFloat() * w, y = top * rng.NextFloat();
                img.Fill(Shape.Capsule(x - w * 0.015f, y, x + w * 0.015f, y, h * 0.003f), Rgba.FromHex("#7FD8E8").WithAlpha(0.5f), 1.5f);
            }
        }

        static void Drifts(Raster img, Theme t, Rgba color, GameRng rng, int w, int h)
        {
            var snow = Rolling(rng, w, h, 0.2f, 0.1f, 3);
            Toon.Part(img, snow, color, w, Flat);
            // Igloo.
            float ix = w * 0.24f, iy = TopOf(snow, ix, h) - h * 0.02f, r = h * 0.14f;
            var dome = Shape.Intersect(Shape.Circle(ix, iy, r), Shape.HalfPlane(0, -1, -iy));
            Toon.Part(img, dome, Rgba.FromHex("#F4FBFF"), r * 2, Prop);
            for (int k = 1; k < 3; k++) img.FillClipped(dome, Shape.Box(ix, iy + r * k * 0.33f, r, r * 0.012f), Rgba.FromHex("#9CC9DD"), 1.5f);
            img.Fill(Shape.Intersect(Shape.Circle(ix + r * 0.55f, iy, r * 0.35f), Shape.HalfPlane(0, -1, -iy)), Rgba.FromHex("#1B3A52"));
            // Pingouin.
            float px = w * 0.7f, py = TopOf(snow, px, h) - h * 0.01f, s = h * 0.1f;
            Toon.Part(img, Shape.Ellipse(px, py + s * 0.5f, s * 0.3f, s * 0.5f), Rgba.FromHex("#1B1B24"), s, Prop);
            img.Fill(Shape.Ellipse(px + s * 0.05f, py + s * 0.42f, s * 0.18f, s * 0.36f), Rgba.White);
            img.Fill(Shape.Circle(px + s * 0.08f, py + s * 0.82f, s * 0.04f), Rgba.White);
            img.Fill(Shape.Poly(px + s * 0.22f, py + s * 0.74f, px + s * 0.38f, py + s * 0.7f, px + s * 0.22f, py + s * 0.66f), Rgba.FromHex("#FFB703"));
        }

        // ------------------------------------------------------------------ Lune

        static void MoonHills(Raster img, Rgba color, GameRng rng, int w, int h, bool far)
        {
            var ground = Rolling(rng, w, h, far ? 0.3f : 0.2f, 0.08f, far ? 3 : 4);
            Toon.Part(img, ground, color, w, Flat);
            for (int i = 0; i < (far ? 10 : 7); i++)
            {
                float x = rng.NextFloat() * w, y = TopOf(ground, x, h) * (0.3f + rng.NextFloat() * 0.5f), r = w * (0.015f + rng.NextFloat() * 0.03f);
                img.FillClipped(ground, Shape.Ellipse(x, y, r, r * 0.4f), color.Shade(0.8f), 2f);
                img.FillClipped(ground, Shape.Ellipse(x + r * 0.1f, y - r * 0.08f, r * 0.85f, r * 0.3f), color.Light(1.1f), 2f);
            }
            if (!far)
            {
                // Drapeau planté.
                float fx = w * 0.8f, fy = TopOf(ground, fx, h) - h * 0.01f, s = h * 0.2f;
                img.Fill(Shape.Box(fx, fy + s * 0.5f, w * 0.0025f, s * 0.5f), Rgba.FromHex("#DDE1E8"));
                Toon.Part(img, Shape.Box(fx + s * 0.22f, fy + s * 0.84f, s * 0.22f, s * 0.13f), Rgba.FromHex("#FFC93C"), s, Prop);
                img.Fill(Shape.Circle(fx + s * 0.22f, fy + s * 0.84f, s * 0.06f), Rgba.FromHex("#E63946"));
            }
        }

        /// <summary>La Terre vue de la Lune (à la place du soleil).</summary>
        static Raster Earth(Theme t, int n)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, r = n * 0.17f;
            var glow = new Raster(n, n);
            glow.Fill(Shape.Circle(c, c, n * 0.24f), t.Sun.WithAlpha(0.4f), n * 0.2f);
            img.Composite(glow);
            var disc = Shape.Circle(c, c, r);
            img.Fill(disc, Paint.Radial(Rgba.FromHex("#7FD3FF"), c - r * 0.3f, c + r * 0.3f, Rgba.FromHex("#1E5FB8"), r * 1.3f));
            var land = Rgba.FromHex("#5BB865");
            img.FillClipped(disc, Shape.Smooth(Shape.Ellipse(c - r * 0.35f, c + r * 0.25f, r * 0.35f, r * 0.25f), Shape.Ellipse(c - r * 0.2f, c - r * 0.2f, r * 0.15f, r * 0.35f), r * 0.1f), land, 2f);
            img.FillClipped(disc, Shape.Ellipse(c + r * 0.45f, c + r * 0.05f, r * 0.25f, r * 0.4f, 20f), land, 2f);
            img.FillClipped(disc, Shape.Capsule(c - r * 0.6f, c - r * 0.55f, c + r * 0.1f, c - r * 0.45f, r * 0.07f), Rgba.White.WithAlpha(0.85f), 2f);
            img.FillClipped(disc, Shape.Capsule(c - r * 0.1f, c + r * 0.6f, c + r * 0.5f, c + r * 0.55f, r * 0.06f), Rgba.White.WithAlpha(0.85f), 2f);
            // Ombre de la nuit sur la Terre.
            img.FillClipped(disc, Shape.Circle(c + r * 0.55f, c - r * 0.35f, r), Rgba.FromHex("#05050F").WithAlpha(0.45f), r * 0.3f);
            return img;
        }

        // ================================================================== Sols

        static Raster BiomeGround(Theme t, int w, int h)
        {
            var img = new Raster(w, h);
            float top = h * 0.82f;
            var rng = new GameRng(91u + (uint)t.Biome * 13u);
            var c = t.Ground;
            // Bord supérieur légèrement ondulé (pavé : bien droit).
            Shape body = Shape.Box(w * 0.5f, top * 0.5f, w * 0.6f, top * 0.5f + 2);
            if (t.Biome != Biome.City)
            {
                int bumps = 9;
                for (int i = 0; i <= bumps; i++)
                {
                    float x = w * i / (float)bumps + (rng.NextFloat() - 0.5f) * w * 0.04f, r = h * (0.08f + rng.NextFloat() * 0.06f);
                    body = Shape.Smooth(body, Shape.Ellipse(x, top - r * 0.75f, r * 2.2f, r), h * 0.03f);
                }
            }
            float scale = t.Biome == Biome.City ? 0f : 0.5f;
            img.Fill(body, Paint.Custom((x, y) =>
            {
                float nn = Noise.Fbm(x / w * 10f, y / h * 14.4f, 3);
                var baseC = Rgba.Lerp(c.Shade(0.84f), c.Light(1.04f), y / top);
                return Rgba.Lerp(baseC, c.Shade(0.9f), nn * scale);
            }));
            img.FillClipped(body, body.Move(0, -h * 0.025f).Not(), c.Light(1.25f), h * 0.02f, 0.8f);

            switch (t.Biome)
            {
                case Biome.Beach:
                case Biome.Desert:
                    for (int i = 0; i < 60; i++)
                    {
                        float x = rng.NextFloat() * w, y = top * (0.1f + rng.NextFloat() * 0.8f);
                        img.FillClipped(body, Shape.Circle(x, y, h * 0.004f), c.Shade(0.78f).WithAlpha(0.6f), 1f);
                    }
                    if (t.Biome == Biome.Beach)
                        for (int i = 0; i < 6; i++)
                        {
                            float x = w * (0.08f + i * 0.16f + rng.NextFloat() * 0.06f), y = top * (0.3f + rng.NextFloat() * 0.5f), s = h * 0.025f;
                            if (i % 2 == 0) img.Fill(StarShape(x, y, s, s * 0.45f), Rgba.FromHex("#FF8C69"));
                            else
                            {
                                var shell = Shape.Intersect(Shape.Circle(x, y, s), Shape.HalfPlane(0, -1, -y));
                                Toon.Part(img, shell, Rgba.FromHex("#FFE3EC"), s * 2, new Toon.Style { Outline = 0.08f, Shade = 0.15f, Rim = 0f, Specular = false });
                            }
                        }
                    else
                        for (int i = 0; i < 7; i++)
                        {
                            float x = rng.NextFloat() * w, y = top * (0.3f + rng.NextFloat() * 0.5f), s = h * (0.015f + rng.NextFloat() * 0.02f);
                            Toon.Part(img, Shape.Ellipse(x, y, s * 1.4f, s), c.Shade(0.7f), s * 2, new Toon.Style { Outline = 0.06f, Shade = 0.2f, Rim = 0.05f, RimOpacity = 0.4f, Specular = false });
                        }
                    break;
                case Biome.Mountains:
                case Biome.Ice:
                    for (int i = 0; i < 14; i++)
                    {
                        float x = rng.NextFloat() * w, y = top * (0.2f + rng.NextFloat() * 0.6f);
                        img.FillClipped(body, Shape.Ellipse(x, y, w * 0.05f, h * 0.012f), Rgba.FromHex("#B7D3EE").WithAlpha(0.6f), 4f);
                    }
                    for (int i = 0; i < 30; i++)
                    {
                        float x = rng.NextFloat() * w, y = top * (0.1f + rng.NextFloat() * 0.85f);
                        img.Fill(Shape.Circle(x, y, h * 0.004f), Rgba.White, 1f);
                    }
                    break;
                case Biome.City:
                    // Bordure du trottoir et dalles.
                    img.FillClipped(body, Shape.Box(w * 0.5f, top - h * 0.03f, w, h * 0.03f), c.Light(1.15f), 1f);
                    img.FillClipped(body, Shape.Box(w * 0.5f, top - h * 0.062f, w, h * 0.004f), c.Shade(0.7f), 1f);
                    for (float y = top - h * 0.2f; y > 0; y -= h * 0.17f)
                        img.FillClipped(body, Shape.Box(w * 0.5f, y, w, h * 0.004f), c.Shade(0.75f).WithAlpha(0.7f), 1f);
                    for (float x = 0; x < w; x += w * 0.08f)
                        img.FillClipped(body, Shape.Box(x, (top - h * 0.065f) * 0.5f, h * 0.004f, (top - h * 0.065f) * 0.5f), c.Shade(0.75f).WithAlpha(0.7f), 1f);
                    break;
                case Biome.Volcano:
                    var lava = Rgba.FromHex("#FF7A2F");
                    for (int i = 0; i < 9; i++)
                    {
                        float x = rng.NextFloat() * w, y = top * (0.2f + rng.NextFloat() * 0.6f), l = w * (0.03f + rng.NextFloat() * 0.05f);
                        var crack = Shape.Union(Shape.Capsule(x - l, y, x, y + h * 0.02f, h * 0.005f), Shape.Capsule(x, y + h * 0.02f, x + l, y - h * 0.01f, h * 0.005f));
                        img.Fill(crack.Grow(h * 0.01f), lava.WithAlpha(0.3f), h * 0.02f);
                        img.FillClipped(body, crack, Rgba.FromHex("#FFC15E"), 1.5f);
                    }
                    break;
                case Biome.Moon:
                    for (int i = 0; i < 10; i++)
                    {
                        float x = rng.NextFloat() * w, y = top * (0.2f + rng.NextFloat() * 0.6f), r = w * (0.02f + rng.NextFloat() * 0.03f);
                        img.FillClipped(body, Shape.Ellipse(x, y, r, r * 0.35f), c.Shade(0.75f), 2f);
                        img.FillClipped(body, Shape.Ellipse(x + r * 0.08f, y - r * 0.06f, r * 0.85f, r * 0.26f), c.Light(1.08f), 2f);
                    }
                    break;
            }
            return img;
        }

        static Shape StarShape(float cx, float cy, float ro, float ri)
        {
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                double a = Math.PI / 2 + i * Math.PI / 5;
                float r = i % 2 == 0 ? ro : ri;
                pts[i * 2] = cx + (float)Math.Cos(a) * r; pts[i * 2 + 1] = cy + (float)Math.Sin(a) * r;
            }
            return Shape.Poly(pts);
        }
    }
}
