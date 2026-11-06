using System;

namespace Bascule.Core
{
    /// <summary>Ambiance du décor : le ciel avance dans la journée à mesure que le score monte.</summary>
    public sealed class Theme
    {
        public string Name;
        public Rgba SkyTop, SkyHorizon, Sun, FarHills, NearHills, Ground;
        public bool Night;

        public static readonly Theme[] All =
        {
            new Theme { Name = "Matin", SkyTop = Rgba.FromHex("#5DB8F0"), SkyHorizon = Rgba.FromHex("#DDF4FF"), Sun = Rgba.FromHex("#FFE58A"), FarHills = Rgba.FromHex("#A5DDB0"), NearHills = Rgba.FromHex("#6CC47E"), Ground = Rgba.FromHex("#5BB865") },
            new Theme { Name = "Midi", SkyTop = Rgba.FromHex("#3B9FE8"), SkyHorizon = Rgba.FromHex("#C4ECFF"), Sun = Rgba.FromHex("#FFF4B8"), FarHills = Rgba.FromHex("#93D49A"), NearHills = Rgba.FromHex("#58B56A"), Ground = Rgba.FromHex("#4DAA58") },
            new Theme { Name = "Doré", SkyTop = Rgba.FromHex("#F0A35A"), SkyHorizon = Rgba.FromHex("#FFE6B0"), Sun = Rgba.FromHex("#FFD36E"), FarHills = Rgba.FromHex("#D9B873"), NearHills = Rgba.FromHex("#A9AE4F"), Ground = Rgba.FromHex("#94A443") },
            new Theme { Name = "Couchant", SkyTop = Rgba.FromHex("#5E4FC9"), SkyHorizon = Rgba.FromHex("#FF9A7A"), Sun = Rgba.FromHex("#FF7A59"), FarHills = Rgba.FromHex("#9B76C2"), NearHills = Rgba.FromHex("#6E549E"), Ground = Rgba.FromHex("#5D4A8C") },
            new Theme { Name = "Nuit", SkyTop = Rgba.FromHex("#101A3D"), SkyHorizon = Rgba.FromHex("#3A4F8C"), Sun = Rgba.FromHex("#F4F1DE"), FarHills = Rgba.FromHex("#2F466F"), NearHills = Rgba.FromHex("#22375C"), Ground = Rgba.FromHex("#1D3050"), Night = true },
        };
    }

    /// <summary>Décor et planche : ciel, soleil, collines, prairie, nuages, planche en bois.</summary>
    public static class SceneArtist
    {
        static readonly Rgba Wood = Rgba.FromHex("#C4834F");
        static readonly Rgba WoodDark = Rgba.FromHex("#8E5530");

        /// <summary>Dégradé vertical du ciel (étiré en plein écran).</summary>
        public static Raster Sky(Theme t, int h = 512)
        {
            var img = new Raster(8, h);
            var mid = Rgba.Lerp(t.SkyTop, t.SkyHorizon, 0.55f);
            img.Fill(Shape.Box(4, h * 0.5f, 8, h), Paint.Custom((x, y) =>
            {
                float v = y / h; // 0 = bas
                return v > 0.5f ? Rgba.Lerp(mid, t.SkyTop, (v - 0.5f) * 2f) : Rgba.Lerp(t.SkyHorizon, mid, v * 2f);
            }));
            return img;
        }

        /// <summary>Soleil (ou lune la nuit) avec halo.</summary>
        public static Raster Sun(Theme t, int n = 384)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            var glow = new Raster(n, n);
            glow.Fill(Shape.Circle(c, c, n * 0.3f), t.Sun.WithAlpha(0.55f), n * 0.35f);
            img.Composite(glow);
            if (t.Night)
            {
                var moon = Shape.Subtract(Shape.Circle(c, c, n * 0.17f), Shape.Circle(c + n * 0.08f, c + n * 0.05f, n * 0.15f));
                img.Fill(moon, Paint.Radial(Rgba.White, c - n * 0.05f, c, t.Sun, n * 0.2f));
            }
            else
            {
                img.Fill(Shape.Circle(c, c, n * 0.17f), Paint.Radial(Rgba.White, c - n * 0.03f, c + n * 0.03f, t.Sun, n * 0.18f));
            }
            return img;
        }

        /// <summary>Étoiles scintillantes (nuit).</summary>
        public static Raster Stars(int w = 512, int h = 512)
        {
            var img = new Raster(w, h);
            var rng = new GameRng(4242);
            for (int i = 0; i < 70; i++)
            {
                float x = rng.NextFloat() * w, y = rng.NextFloat() * h, r = 1f + rng.NextFloat() * 2.2f;
                img.Fill(Shape.Circle(x, y, r), Rgba.White.WithAlpha(0.5f + rng.NextFloat() * 0.5f), 1.5f);
            }
            return img;
        }

        /// <summary>Collines (far = lointaines, plus claires et floues ; sinon proches avec arbres).</summary>
        public static Raster Hills(Theme t, bool far, int w = 1024, int h = 320)
        {
            var img = new Raster(w, h);
            var color = far ? t.FarHills : t.NearHills;
            var rng = new GameRng(far ? 11u : 23u);
            Shape hills = Shape.Box(w * 0.5f, h * 0.12f, w * 0.6f, h * 0.14f);
            int bumps = far ? 4 : 5;
            for (int i = 0; i <= bumps; i++)
            {
                float x = w * (i / (float)bumps) + (rng.NextFloat() - 0.5f) * w * 0.12f;
                float r = w * (far ? 0.2f : 0.17f) * (0.8f + rng.NextFloat() * 0.5f);
                float y = h * (far ? 0.38f : 0.28f) + rng.NextFloat() * h * 0.12f - r * 0.6f;
                hills = Shape.Smooth(hills, Shape.Ellipse(x, y, r * 1.3f, r), w * 0.08f);
            }
            var st = new Toon.Style { Outline = 0f, Shade = far ? 0.03f : 0.05f, Soft = 0.06f, ShadeStrength = 0.88f, Rim = 0.01f, RimOpacity = 0.35f, Specular = false };
            Toon.Part(img, hills, color, w, st);

            if (!far)
            {
                // Petits arbres ronds sur les collines proches.
                for (int i = 0; i < 6; i++)
                {
                    float x = w * (0.08f + i * 0.17f + rng.NextFloat() * 0.06f);
                    float top = TopOf(hills, x, h);
                    if (top < h * 0.25f) continue;
                    float s = h * (0.07f + rng.NextFloat() * 0.04f);
                    var trunk = Shape.Box(x, top + s * 0.4f, s * 0.12f, s * 0.5f, s * 0.05f);
                    Toon.Part(img, trunk, WoodDark.Shade(t.Night ? 0.5f : 0.9f), s * 2, new Toon.Style { Outline = 0.04f, Shade = 0.1f, Rim = 0f, Specular = false });
                    var crown = Shape.Union(Shape.Circle(x, top + s * 1.3f, s * 0.7f), Shape.Circle(x - s * 0.45f, top + s * 1.0f, s * 0.5f), Shape.Circle(x + s * 0.45f, top + s * 1.0f, s * 0.5f));
                    Toon.Part(img, crown, color.Shade(0.82f), s * 2, new Toon.Style { Outline = 0.04f, Shade = 0.2f, Soft = 0.2f, Rim = 0.06f, RimOpacity = 0.4f, Specular = false });
                }
            }
            return img;
        }

        static float TopOf(Shape s, float x, int h)
        {
            for (int y = h - 1; y >= 0; y--) if (s.D(x, y) < 0) return y;
            return 0;
        }

        /// <summary>Prairie au premier plan : herbe avec brins sur le bord et quelques fleurs.</summary>
        public static Raster Ground(Theme t, int w = 1024, int h = 360)
        {
            var img = new Raster(w, h);
            float top = h * 0.82f;
            var rng = new GameRng(77);
            // Bord supérieur en touffes arrondies (plus propre qu'une multitude de brins).
            var grass = Shape.Box(w * 0.5f, top * 0.5f, w * 0.6f, top * 0.5f + 2);
            int tufts = 22;
            for (int i = 0; i <= tufts; i++)
            {
                float x = w * i / (float)tufts + (rng.NextFloat() - 0.5f) * w * 0.02f;
                float r = h * (0.04f + rng.NextFloat() * 0.03f);
                grass = Shape.Union(grass, Shape.Circle(x, top - r * 0.2f, r));
            }
            var c = t.Ground;
            img.Fill(grass, Paint.Custom((x, y) =>
            {
                float n = Noise.Fbm(x / w * 10f, y / h * 14.4f, 3); // relatif à la taille : même rendu à toute résolution
                var baseC = Rgba.Lerp(c.Shade(0.8f), c.Light(1.06f), y / top);
                return Rgba.Lerp(baseC, c.Shade(0.88f), n * 0.5f);
            }));
            img.FillClipped(grass, grass.Move(0, -h * 0.025f).Not(), c.Light(1.3f), h * 0.02f, 0.8f);
            // Brins d'herbe dessinés en creux, sans contour.
            for (int i = 0; i < 40; i++)
            {
                float x = rng.NextFloat() * w, y = top * (0.3f + rng.NextFloat() * 0.65f), bh = h * (0.03f + rng.NextFloat() * 0.03f);
                img.FillClipped(grass, Shape.Tapered(x, y, w * 0.004f, x + (rng.NextFloat() - 0.5f) * bh, y + bh, 0.5f), c.Shadow(0.72f).WithAlpha(0.7f), 1.5f);
            }
            // Touffes plus sombres et fleurs.
            for (int i = 0; i < 18; i++)
            {
                float x = rng.NextFloat() * w, y = top * (0.25f + rng.NextFloat() * 0.6f);
                img.Fill(Shape.Ellipse(x, y, w * 0.025f, h * 0.012f), c.Shadow(0.75f).WithAlpha(0.6f), 3f);
            }
            if (!t.Night)
            {
                var petals = new[] { Rgba.White, Rgba.FromHex("#FFC93C"), Rgba.FromHex("#F25CA2") };
                for (int i = 0; i < 14; i++)
                {
                    float x = rng.NextFloat() * w, y = top * (0.35f + rng.NextFloat() * 0.6f), s = h * 0.012f;
                    var p = petals[i % petals.Length];
                    for (int k = 0; k < 5; k++)
                    {
                        double a = k * Math.PI * 2 / 5;
                        img.Fill(Shape.Circle(x + (float)Math.Cos(a) * s, y + (float)Math.Sin(a) * s, s * 0.8f), p);
                    }
                    img.Fill(Shape.Circle(x, y, s * 0.6f), Rgba.FromHex("#FFC93C").Shade(0.9f));
                }
            }
            return img;
        }

        /// <summary>Nuage gonflé (trois variantes).</summary>
        public static Raster Cloud(int variant, Theme t, int w = 512, int h = 256)
        {
            var img = new Raster(w, h);
            var rng = new GameRng((uint)(31 + variant * 7));
            float cx = w * 0.5f, by = h * 0.3f;
            var s = Shape.Box(cx, by + h * 0.06f, w * 0.38f, h * 0.1f, h * 0.1f);
            int blobs = 4 + variant;
            for (int i = 0; i < blobs; i++)
            {
                float x = cx + (i / (float)(blobs - 1) - 0.5f) * w * 0.62f;
                float r = h * (0.18f + rng.NextFloat() * 0.14f) * (1f - Math.Abs(i / (float)(blobs - 1) - 0.5f) * 0.7f);
                s = Shape.Union(s, Shape.Circle(x, by + r * 0.9f, r));
            }
            var white = t.Night ? Rgba.FromHex("#6F80B0") : Rgba.White;
            var shade = t.Night ? Rgba.FromHex("#4C5A8A") : Rgba.Lerp(t.SkyHorizon, t.SkyTop, 0.4f).Shade(1.05f);
            img.Fill(s, white.WithAlpha(0.96f));
            img.FillClipped(s, Shape.HalfPlane(0, 1, by + h * 0.12f), shade.WithAlpha(0.55f), h * 0.12f);
            img.FillClipped(s, s.Move(h * 0.03f, -h * 0.04f).Not(), Rgba.White, h * 0.05f, 0.9f);
            return img;
        }

        /// <summary>
        /// Planche en bois : veinage, nœuds, clous, chant éclairé, bouts plus sombres.
        /// Prévue pour le 9-slice horizontal : bordures gauche/droite = BorderPx.
        /// </summary>
        public static Raster Plank(float lengthUnits, float thicknessUnits, float pixelsPerUnit, out int borderPx) =>
            Plank(lengthUnits, thicknessUnits, pixelsPerUnit, out borderPx, PlankCatalog.Get(0));

        /// <summary>
        /// Planche, dans l'apparence choisie (Core/Cosmetics.cs). La forme et la taille ne changent jamais :
        /// seul le motif change (bois, bambou, skate, surf, marbre, sucre d'orge, or).
        /// </summary>
        public static Raster Plank(float lengthUnits, float thicknessUnits, float pixelsPerUnit, out int borderPx, PlankSkin skin)
        {
            skin = skin ?? PlankCatalog.Get(0);
            var baseC = skin.Base;
            var dark = skin.Dark;
            int pad = 6;
            int w = (int)Math.Ceiling(lengthUnits * pixelsPerUnit) + pad * 2, h = (int)Math.Ceiling(thicknessUnits * pixelsPerUnit) + pad * 2;
            var img = new Raster(w, h);
            float x0 = pad, x1 = w - pad, y0 = pad, y1 = h - pad;
            float th = y1 - y0;
            // Bouts arrondis comme les autres planches : le rebord doit reposer sur le dessus plat, pas sur la courbe.
            var board = Shape.Box(w * 0.5f, h * 0.5f, (x1 - x0) * 0.5f, th * 0.5f, th * CornerRatio(skin));
            var outline = skin.Pattern == PlankPattern.Grip ? Rgba.FromHex("#2A1E3D") : dark.Outline();
            img.Fill(board.Grow(3f), outline);
            var rng = new GameRng(5);
            switch (skin.Pattern)
            {
                case PlankPattern.Grain:
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        float grain = (float)Math.Sin((y * 0.55f + Noise.Fbm(x * 0.012f, y * 0.08f, 3) * 9f) * 1.3f);
                        var c = Rgba.Lerp(baseC.Shade(0.86f), baseC.Light(1.08f), v);
                        if (grain > 0.55f) c = Rgba.Lerp(c, dark, (grain - 0.55f) * 0.9f);
                        return c;
                    }));
                    break;
                case PlankPattern.Bamboo:
                {
                    float seg = th * 2.6f;
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        var c = Rgba.Lerp(baseC.Shade(0.82f), baseC.Light(1.12f), (float)Math.Sin(v * Math.PI) * 0.8f + v * 0.2f);
                        float fibre = (float)Math.Sin(y * 1.7f + Noise.Fbm(x * 0.02f, y * 0.3f, 2) * 3f);
                        if (fibre > 0.8f) c = Rgba.Lerp(c, dark, 0.18f);
                        return c;
                    }));
                    // Nœuds du bambou : un anneau sombre et un liseré clair à intervalles réguliers.
                    for (float nx = x0 + seg * 0.8f; nx < x1 - seg * 0.4f; nx += seg)
                    {
                        img.FillClipped(board, Shape.Box(nx, h * 0.5f, th * 0.09f, th), dark.WithAlpha(0.75f), 1.5f);
                        img.FillClipped(board, Shape.Box(nx + th * 0.16f, h * 0.5f, th * 0.05f, th), baseC.Light(1.3f).WithAlpha(0.8f), 1.5f);
                    }
                    break;
                }
                case PlankPattern.Grip:
                {
                    // Skate : antidérapant noir moucheté sur le dessus, couleur vive et bande sur la tranche.
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        if (v > 0.42f)
                        {
                            float n = Noise.Fbm(x * 0.35f, y * 0.35f, 2);
                            return Rgba.Lerp(dark, dark.Light(1.4f), n > 0.62f ? 0.35f : 0f);
                        }
                        var c = Rgba.Lerp(baseC.Shade(0.8f), baseC, v / 0.42f);
                        return c;
                    }));
                    img.FillClipped(board, Shape.Box(w * 0.5f, y0 + th * 0.2f, (x1 - x0) * 0.5f, th * 0.05f), Rgba.FromHex("#FFF6E5").WithAlpha(0.85f), 1.5f);
                    break;
                }
                case PlankPattern.Surf:
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        var c = Rgba.Lerp(baseC.Shade(0.88f), baseC, v);
                        return c;
                    }));
                    // Bandes de couleur au centre, dans la longueur, et un liseré de chaque côté.
                    img.FillClipped(board, Shape.Box(w * 0.5f, h * 0.5f, (x1 - x0) * 0.5f, th * 0.13f), dark, 1.5f);
                    img.FillClipped(board, Shape.Box(w * 0.5f, h * 0.5f + th * 0.26f, (x1 - x0) * 0.5f, th * 0.04f), Rgba.FromHex("#FF7A59"), 1.5f);
                    img.FillClipped(board, Shape.Box(w * 0.5f, h * 0.5f - th * 0.26f, (x1 - x0) * 0.5f, th * 0.04f), Rgba.FromHex("#FF7A59"), 1.5f);
                    break;
                case PlankPattern.Marble:
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        var c = Rgba.Lerp(baseC.Shade(0.9f), baseC, v);
                        float vein = Math.Abs((float)Math.Sin(x * 0.018f + Noise.Fbm(x * 0.008f, y * 0.05f, 4) * 7f));
                        if (vein < 0.09f) c = Rgba.Lerp(c, dark, (0.09f - vein) / 0.09f * 0.85f);
                        return c;
                    }));
                    break;
                case PlankPattern.Candy:
                {
                    var white = Rgba.FromHex("#FFF6F2");
                    float stripe = th * 1.1f;
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        float d = ((x + y * 1.2f) % (stripe * 2f) + stripe * 2f) % (stripe * 2f);
                        var c = d < stripe ? baseC : white;
                        return Rgba.Lerp(c.Shade(0.86f), c.Light(1.06f), v);
                    }));
                    break;
                }
                case PlankPattern.Gold:
                    img.Fill(board, Paint.Custom((x, y) =>
                    {
                        float v = (y - y0) / th;
                        float sheen = (float)Math.Sin(x * 0.012f) * 0.5f + 0.5f;
                        var c = Rgba.Lerp(baseC.Shade(0.75f), baseC.Light(1.18f), v * 0.7f + sheen * 0.3f);
                        return c;
                    }));
                    // Éclats : petites étoiles blanches.
                    for (int i = 0; i < 6; i++)
                    {
                        float sx = x0 + (x1 - x0) * (0.1f + i * 0.16f + rng.NextFloat() * 0.04f), sy = y0 + th * (0.45f + rng.NextFloat() * 0.3f);
                        float r = th * (0.18f + rng.NextFloat() * 0.1f), k = r * 0.22f;
                        // Étoile à quatre branches (deux losanges croisés).
                        var star = Shape.Union(Shape.Poly(sx, sy - r, sx + k, sy, sx, sy + r, sx - k, sy), Shape.Poly(sx - r, sy, sx, sy + k, sx + r, sy, sx, sy - k));
                        img.FillClipped(board, star, Rgba.FromHex("#FFFBEA").WithAlpha(0.95f), 1.2f);
                    }
                    break;
            }
            // Chant supérieur éclairé et dessous sombre (toutes les planches).
            var top = skin.Pattern == PlankPattern.Grip ? dark.Light(1.6f) : baseC.Light(1.32f);
            img.FillClipped(board, Shape.HalfPlane(0, -1, -(y1 - th * 0.22f)), top, 2f, skin.Pattern == PlankPattern.Grip ? 0.35f : 0.85f);
            img.FillClipped(board, Shape.HalfPlane(0, 1, y0 + th * 0.18f), dark.Shade(0.85f), 2f, 0.7f);
            if (skin.Pattern == PlankPattern.Grain)
            {
                // Nœuds.
                for (int i = 0; i < 3; i++)
                {
                    float kx = x0 + (x1 - x0) * (0.22f + i * 0.28f + rng.NextFloat() * 0.06f), ky = y0 + th * (0.35f + rng.NextFloat() * 0.3f);
                    img.FillClipped(board, Shape.Ellipse(kx, ky, th * 0.42f, th * 0.18f), dark.WithAlpha(0.55f), 2f);
                    img.FillClipped(board, Shape.Subtract(Shape.Ellipse(kx, ky, th * 0.62f, th * 0.27f), Shape.Ellipse(kx, ky, th * 0.52f, th * 0.21f)), dark.WithAlpha(0.35f), 1.5f);
                }
            }
            // Bouts et clous (vis de skate pour la planche de skate).
            borderPx = (int)(th * 1.2f) + pad;
            bool nails = skin.Pattern == PlankPattern.Grain || skin.Pattern == PlankPattern.Bamboo || skin.Pattern == PlankPattern.Grip;
            foreach (float ex in new[] { x0 + th * 0.55f, x1 - th * 0.55f })
            {
                if (skin.Pattern == PlankPattern.Grain) img.FillClipped(board, Shape.Box(ex, h * 0.5f, th * 0.55f, th), dark.WithAlpha(0.35f), 3f);
                if (!nails) continue;
                foreach (float ny in new[] { y0 + th * 0.3f, y0 + th * 0.7f })
                {
                    img.Fill(Shape.Circle(ex, ny, th * 0.1f), Rgba.FromHex("#5B6070"));
                    img.Fill(Shape.Circle(ex - th * 0.03f, ny + th * 0.03f, th * 0.04f), Rgba.FromHex("#D4D8E2"));
                }
            }
            return img;
        }

        /// <summary>Arrondi des bouts de planche, en fraction de l'épaisseur.</summary>
        public static float CornerRatio(PlankSkin skin) => skin != null && (skin.Pattern == PlankPattern.Surf || skin.Pattern == PlankPattern.Grip) ? 0.3f : 0.22f;

        /// <summary>
        /// Distance (unités) entre le bout de la planche et le bord extérieur du rebord : le rebord est posé là où le
        /// dessus de la planche devient plat (après l'arrondi du bout), jamais sur la courbe ni dans le vide.
        /// La zone qui retient les poids est placée au même endroit (GameSession).
        /// </summary>
        public static float LipInset(PlankSkin skin, float thickness) => thickness * CornerRatio(skin) + 0.012f;

        /// <summary>Marge (unités) autour du rebord dans son image : ombre, feuille, crosse, pommeau.</summary>
        public const float LipPad = 0.05f;
        /// <summary>Largeur et hauteur totales (unités) de l'image d'un rebord.</summary>
        public const float LipImageW = 0.38f, LipImageH = 0.48f;

        /// <summary>
        /// Rebord de la planche, assorti à la planche choisie : un petit objet décoratif posé au bout (rondin, nœud de
        /// bambou, plot à roue, plot à ballon de plage, colonne, crosse de sucre d'orge, pommeau doré). Seul le dessin
        /// change : la zone qui retient les poids (lw × lh) est la même pour toutes les planches.
        /// Le coin bas-extérieur du rebord est en (LipPad, LipPad) ; l'objet déborde vers l'intérieur et vers le haut.
        /// </summary>
        public static Raster PlankLip(PlankSkin skin, float lw, float lh, float ppu)
        {
            skin = skin ?? PlankCatalog.Get(0);
            float U = ppu;
            int W = (int)Math.Ceiling(LipImageW * U), H = (int)Math.Ceiling(LipImageH * U);
            var img = new Raster(W, H);
            float ax = LipPad * U, ay = LipPad * U, w = lw * U, h = lh * U;
            // Décalage vers l'intérieur pour les rebords plus larges que la zone (socle de colonne, roue, ballon) :
            // rien ne doit dépasser le bout de la planche, sinon le rebord a l'air posé à côté.
            float inset = skin.Pattern == PlankPattern.Marble ? 0.03f * U
                : skin.Pattern == PlankPattern.Surf ? (w * 0.72f - w * 0.5f) + 0.012f * U
                : skin.Pattern == PlankPattern.Grip ? (w * 0.62f - w * 0.5f) + 0.012f * U
                : skin.Pattern == PlankPattern.Gold ? 0.008f * U : 0f;
            ax += inset;
            float cx = ax + w * 0.5f;
            float o = 0.012f * U; // épaisseur du contour

            // Ombre posée sur la planche, au pied du rebord.
            img.Fill(Shape.Ellipse(cx + 0.025f * U, ay + 0.004f * U, w * 0.85f, 0.014f * U), new Rgba(30, 18, 10, 80), 2f);

            void Outlined(Shape sh, Paint fill, Rgba line)
            {
                img.Fill(sh.Grow(o), line);
                img.Fill(sh, fill);
            }
            void Leaf(float baseX, float baseY, Rgba col)
            {
                var leaf = Shape.Poly(baseX, baseY, baseX + 0.075f * U, baseY + 0.035f * U, baseX + 0.012f * U, baseY - 0.03f * U);
                Outlined(leaf, col, col.Outline());
                img.Fill(Shape.Capsule(baseX + 0.004f * U, baseY, baseX + 0.06f * U, baseY + 0.026f * U, 0.0035f * U), col.Shade(0.7f));
            }
            Paint Cylinder(Rgba c) => Paint.Custom((x, y) =>
            {
                float v = Math.Max(0f, Math.Min(1f, (x - ax) / Math.Max(1f, w)));
                return Rgba.Lerp(c.Shade(0.72f), c.Light(1.18f), (float)Math.Sin(v * Math.PI) * 0.9f + (1f - v) * 0.1f);
            });
            var white = Rgba.FromHex("#FFFFFF");

            switch (skin.Pattern)
            {
                case PlankPattern.Bamboo:
                {
                    var body = Shape.Box(cx, ay + h * 0.5f, w * 0.5f, h * 0.5f, w * 0.18f);
                    Outlined(body, Cylinder(skin.Base), skin.Dark.Outline());
                    img.FillClipped(body, Shape.Box(cx, ay + h * 0.45f, w * 0.6f, 0.012f * U), skin.Dark, 1.5f);
                    img.FillClipped(body, Shape.Box(cx, ay + h * 0.45f - 0.02f * U, w * 0.6f, 0.004f * U), skin.Base.Light(1.4f), 1.5f);
                    Outlined(Shape.Ellipse(cx, ay + h, w * 0.5f, w * 0.18f), skin.Base.Light(1.35f), skin.Dark.Outline());
                    img.Fill(Shape.Ellipse(cx, ay + h, w * 0.32f, w * 0.1f), skin.Dark);
                    img.Fill(Shape.Capsule(ax + w * 0.22f, ay + 0.03f * U, ax + w * 0.22f, ay + h - 0.05f * U, 0.007f * U), white.WithAlpha(0.4f));
                    Leaf(ax + w - 0.01f * U, ay + h * 0.68f, Rgba.FromHex("#5FA33A"));
                    break;
                }
                case PlankPattern.Marble:
                {
                    var line = Rgba.FromHex("#5E5A6B"); var grey = Rgba.FromHex("#B8B3C2");
                    Outlined(Shape.Box(cx, ay + 0.018f * U, w * 0.5f + 0.016f * U, 0.018f * U, 0.006f * U), skin.Base, line);
                    float sh0 = ay + 0.036f * U, sh1 = ay + h - 0.044f * U;
                    var shaft = Shape.Box(cx, (sh0 + sh1) * 0.5f, w * 0.5f - 0.012f * U, (sh1 - sh0) * 0.5f);
                    Outlined(shaft, Cylinder(skin.Base), line);
                    for (float fx = ax + 0.026f * U; fx < ax + w - 0.016f * U; fx += 0.02f * U)
                        img.FillClipped(shaft, Shape.Box(fx, (sh0 + sh1) * 0.5f, 0.0028f * U, (sh1 - sh0) * 0.45f), grey, 1f);
                    Outlined(Shape.Box(cx, ay + h - 0.022f * U, w * 0.5f + 0.02f * U, 0.022f * U, 0.008f * U), skin.Base, line);
                    foreach (float vx in new[] { ax - 0.012f * U, ax + w + 0.012f * U })
                    {
                        Outlined(Shape.Circle(vx, ay + h - 0.022f * U, 0.016f * U), grey, line);
                        img.Fill(Shape.Circle(vx, ay + h - 0.022f * U, 0.006f * U), skin.Base);
                    }
                    break;
                }
                case PlankPattern.Candy:
                {
                    float r = 0.024f * U, R = 0.045f * U, top = ay + h * 0.78f;
                    var cane = Shape.Union(Shape.Capsule(cx, ay, cx, top, r), Shape.Arc(cx + R, top, R, 0f, 180f, r * 2f),
                        Shape.Capsule(cx + 2 * R, top, cx + 2 * R, top - 0.03f * U, r));
                    float period = 0.034f * U;
                    var stripes = Paint.Custom((x, y) =>
                    {
                        float d = ((x + y) % (period * 2f) + period * 2f) % (period * 2f);
                        var c = d < period ? skin.Base : Rgba.FromHex("#FFF6F2");
                        return Rgba.Lerp(c.Shade(0.85f), c, Math.Min(1f, Math.Max(0f, (x - (cx - r)) / (2f * r))));
                    });
                    Outlined(cane, stripes, skin.Dark.Outline());
                    img.Fill(Shape.Capsule(cx - r * 0.45f, ay + 0.02f * U, cx - r * 0.45f, top - 0.01f * U, 0.005f * U), white.WithAlpha(0.45f));
                    break;
                }
                case PlankPattern.Gold:
                {
                    var line = Rgba.FromHex("#6E4A0A");
                    float postH = 0.13f * U, rr = 0.062f * U;
                    Outlined(Shape.Box(cx, ay + postH * 0.5f, 0.024f * U, postH * 0.5f, 0.01f * U), Cylinder(skin.Base), line);
                    float sy = ay + postH + rr * 0.92f;
                    var ball = Shape.Circle(cx, sy, rr);
                    Outlined(ball, Paint.Custom((x, y) =>
                    {
                        float dx = (x - (cx - rr * 0.35f)) / rr, dy = (y - (sy + rr * 0.35f)) / rr;
                        float t = Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) / 1.4f);
                        return Rgba.Lerp(skin.Base.Light(1.25f), skin.Dark, t * t);
                    }), line);
                    img.Fill(Shape.Ellipse(cx - rr * 0.38f, sy + rr * 0.42f, rr * 0.26f, rr * 0.18f), white.WithAlpha(0.9f), 1.5f);
                    float stx = cx + rr * 1.1f, sty = sy + rr * 0.9f, k = 0.022f * U;
                    img.Fill(Shape.Union(Shape.Poly(stx, sty - k, stx + k * 0.25f, sty, stx, sty + k, stx - k * 0.25f, sty),
                        Shape.Poly(stx - k, sty, stx, sty + k * 0.25f, stx + k, sty, stx, sty - k * 0.25f)), Rgba.FromHex("#FFFBEA"), 1f);
                    break;
                }
                case PlankPattern.Grip:
                {
                    // Plot noir à bande rose, surmonté d'une petite roue de skate.
                    float postH = h * 0.62f;
                    var post = Shape.Box(cx, ay + postH * 0.5f, w * 0.42f, postH * 0.5f, 0.01f * U);
                    Outlined(post, Cylinder(skin.Dark.Light(1.5f)), Rgba.FromHex("#1A1626"));
                    img.FillClipped(post, Shape.Box(cx, ay + postH * 0.55f, w, 0.014f * U), skin.Base, 1.5f);
                    float wr = w * 0.62f, wy = ay + postH + wr * 0.85f;
                    Outlined(Shape.Circle(cx, wy, wr), Cylinder(skin.Base), skin.Base.Outline());
                    img.Fill(Shape.Circle(cx, wy, wr * 0.42f), Rgba.FromHex("#C9D3E0"));
                    img.Fill(Shape.Circle(cx, wy, wr * 0.16f), Rgba.FromHex("#5B6070"));
                    img.Fill(Shape.Ellipse(cx - wr * 0.4f, wy + wr * 0.45f, wr * 0.22f, wr * 0.14f), white.WithAlpha(0.8f), 1.5f);
                    break;
                }
                case PlankPattern.Surf:
                {
                    // Plot turquoise à bande orange, surmonté d'un ballon de plage.
                    float postH = h * 0.6f;
                    var post = Shape.Box(cx, ay + postH * 0.5f, w * 0.38f, postH * 0.5f, 0.01f * U);
                    Outlined(post, Cylinder(skin.Dark), skin.Dark.Outline());
                    img.FillClipped(post, Shape.Box(cx, ay + postH * 0.5f, w, 0.012f * U), Rgba.FromHex("#FF7A59"), 1.5f);
                    float br = w * 0.72f, by = ay + postH + br * 0.88f;
                    var cols = new[] { skin.Dark, Rgba.FromHex("#FFFFFF"), Rgba.FromHex("#FF7A59"), Rgba.FromHex("#FFD447"), skin.Dark, Rgba.FromHex("#FFFFFF") };
                    Outlined(Shape.Circle(cx, by, br), Paint.Custom((x, y) =>
                    {
                        double a = Math.Atan2(y - by, x - cx) + Math.PI;
                        int seg = (int)(a / (Math.PI * 2) * cols.Length) % cols.Length;
                        float dx = (x - (cx - br * 0.35f)) / br, dy = (y - (by + br * 0.35f)) / br;
                        float t = Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) / 1.6f);
                        return Rgba.Lerp(cols[seg], cols[seg].Shade(0.7f), t * t);
                    }), skin.Dark.Outline());
                    img.Fill(Shape.Circle(cx, by, br * 0.14f), Rgba.FromHex("#FFFFFF"));
                    img.Fill(Shape.Ellipse(cx - br * 0.38f, by + br * 0.42f, br * 0.24f, br * 0.15f), white.WithAlpha(0.85f), 1.5f);
                    break;
                }
                default:
                {
                    // Bois : un rondin coupé (cernes sur le dessus, écorce), avec une petite pousse verte.
                    var body = Shape.Box(cx, ay + h * 0.5f, w * 0.5f, h * 0.5f, w * 0.16f);
                    Outlined(body, Cylinder(skin.Base), skin.Dark.Outline());
                    foreach (float bx in new[] { ax + w * 0.3f, ax + w * 0.68f })
                        img.FillClipped(body, Shape.Capsule(bx, ay + 0.03f * U, bx, ay + h - 0.05f * U, 0.0035f * U), skin.Dark.WithAlpha(0.6f), 1f);
                    float ty = ay + h;
                    Outlined(Shape.Ellipse(cx, ty, w * 0.5f, w * 0.19f), Rgba.FromHex("#E8B880"), skin.Dark.Outline());
                    img.Fill(Shape.Subtract(Shape.Ellipse(cx, ty, w * 0.34f, w * 0.12f), Shape.Ellipse(cx, ty, w * 0.28f, w * 0.095f)), skin.Base.Shade(0.85f), 1f);
                    img.Fill(Shape.Subtract(Shape.Ellipse(cx, ty, w * 0.18f, w * 0.065f), Shape.Ellipse(cx, ty, w * 0.12f, w * 0.04f)), skin.Base.Shade(0.85f), 1f);
                    img.Fill(Shape.Capsule(ax + w * 0.2f, ay + 0.03f * U, ax + w * 0.2f, ay + h - 0.05f * U, 0.006f * U), white.WithAlpha(0.3f));
                    Leaf(ax + w - 0.01f * U, ay + h * 0.7f, Rgba.FromHex("#5FA33A"));
                    break;
                }
            }
            return img;
        }

        /// <summary>Aperçu de boutique : la planche avec ses deux rebords (le rebord droit est retourné).</summary>
        public static Raster PlankPreview(PlankSkin skin, float length, float thickness, float ppu, float lw, float lh)
        {
            var plank = Plank(length, thickness, ppu, out _, skin);
            var lip = PlankLip(skin, lw, lh, ppu);
            int a = (int)(LipPad * ppu), sink = (int)(0.02f * ppu), pad = 6;
            int inset = (int)Math.Round(LipInset(skin, thickness) * ppu);
            var img = new Raster(plank.Width + 2 * a, plank.Height + lip.Height);
            img.CompositeAt(plank, a, 0);
            int top = plank.Height - pad - sink;
            img.CompositeAt(lip, a + pad + inset - a, top - a);
            var right = lip.FlippedX();
            img.CompositeAt(right, a + plank.Width - pad - inset - (right.Width - a), top - a);
            return img;
        }

        /// <summary>
        /// Vérification : nombre de colonnes de pixels où un rebord n'est pas posé sur le dessus plat de la planche
        /// (il pend dans le vide ou sur l'arrondi du bout). Doit valoir 0 pour toutes les planches (test).
        /// </summary>
        public static int LipUnsupportedColumns(PlankSkin skin, float length, float thickness, float ppu, float lw, float lh)
        {
            var plank = Plank(length, thickness, ppu, out _, skin);
            var lip = PlankLip(skin, lw, lh, ppu);
            int a = (int)(LipPad * ppu), sink = (int)(0.02f * ppu), pad = 6;
            int inset = (int)Math.Round(LipInset(skin, thickness) * ppu);
            int top = plank.Height - pad - sink;       // ligne du pied du rebord dans l'image de la planche
            int surface = plank.Height - pad - 3;      // juste sous le dessus de la planche
            int dx = pad + inset - a, bad = 0;
            for (int x = 0; x < lip.Width; x++)
            {
                bool solid = false;
                for (int y = 0; y < lip.Height && !solid; y++) solid = lip.Pixels[y * lip.Width + x].a >= 160;
                if (!solid) continue;
                int px = x + dx;
                if (px < 0 || px >= plank.Width || plank.Pixels[surface * plank.Width + px].a < 200) bad++;
            }
            return bad;
        }
    }
}
