using System;

namespace Bascule.Core
{
    /// <summary>
    /// Petit village en bois des premiers décors (la prairie, de 0 à 5 500 points) : chalets en rondins au toit rouge,
    /// cheminée qui fume, fenêtres allumées la nuit, barrière de bois, et au loin quelques cabanes et un moulin.
    /// Les chalets restent sur les côtés : le centre de l'écran (Basile et sa planche) reste dégagé.
    /// </summary>
    public static partial class SceneArtist
    {
        /// <summary>Position (fraction de la largeur) des chalets des collines proches.</summary>
        static readonly float[] HouseAt = { 0.09f, 0.24f, 0.78f, 0.93f };

        static bool NearHouse(float fx)
        {
            foreach (var hx in HouseAt) if (Math.Abs(fx - hx) < 0.07f) return true;
            return false;
        }

        /// <summary>Couleurs du village selon l'heure : bois et toits prennent un peu la teinte du ciel, et s'assombrissent la nuit.</summary>
        static void VillageColors(Theme t, out Rgba wood, out Rgba roof, out Rgba stone)
        {
            wood = Rgba.Lerp(Rgba.FromHex("#C08552"), t.NearHills, 0.15f);
            roof = Rgba.Lerp(Rgba.FromHex("#C8553D"), t.SkyHorizon, 0.12f);
            stone = Rgba.Lerp(Rgba.FromHex("#A8A39A"), t.NearHills, 0.15f);
            if (t.Night) { wood = wood.Shade(0.5f); roof = roof.Shade(0.5f); stone = stone.Shade(0.5f); }
        }

        static void NearVillage(Raster img, Theme t, Shape hills, int w, int h)
        {
            VillageColors(t, out var wood, out var roof, out var stone);
            float[] size = { 0.26f, 0.19f, 0.21f, 0.27f };
            // Barrière en bois entre les chalets de chaque côté.
            for (int side = 0; side < 2; side++)
            {
                float x0 = w * (side == 0 ? 0.0f : 0.72f), x1 = w * (side == 0 ? 0.3f : 1.0f);
                var post = wood.Shade(0.85f);
                for (float x = x0; x <= x1; x += h * 0.045f)
                {
                    float y = TopOf(hills, x, h) - h * 0.03f;
                    Toon.Part(img, Shape.Box(x, y + h * 0.03f, h * 0.006f, h * 0.03f, h * 0.003f), post, h * 0.06f, Village);
                }
                for (int r = 0; r < 2; r++)
                    for (float x = x0; x < x1; x += h * 0.045f)
                    {
                        float ya = TopOf(hills, x, h) - h * 0.03f + h * (0.022f + r * 0.022f), yb = TopOf(hills, x + h * 0.045f, h) - h * 0.03f + h * (0.022f + r * 0.022f);
                        img.Fill(Shape.Capsule(x, ya, x + h * 0.045f, yb, h * 0.004f), post.Shade(0.9f), 1f);
                    }
            }
            for (int i = 0; i < HouseAt.Length; i++)
            {
                float x = w * HouseAt[i];
                Chalet(img, x, TopOf(hills, x, h) - h * 0.025f, h * size[i], wood, roof, stone, t.Night, i % 2 == 1, i);
            }
        }

        static readonly Toon.Style Village = new Toon.Style { Outline = 0.05f, Shade = 0.18f, Soft = 0.2f, Rim = 0.05f, RimOpacity = 0.4f, Specular = false };

        /// <summary>Chalet en rondins : base au sol en (x, y), hauteur des murs environ s.</summary>
        static void Chalet(Raster img, float x, float y, float s, Rgba wood, Rgba roof, Rgba stone, bool night, bool small, int seed)
        {
            float hw = s * (small ? 0.42f : 0.55f), wallH = s * 0.62f;
            // Cheminée (derrière le toit).
            float chx = x + hw * 0.5f;
            Toon.Part(img, Shape.Box(chx, y + wallH + s * 0.32f, s * 0.07f, s * 0.2f, s * 0.01f), stone, s, Village);
            // Fumée (le jour) : trois boules qui montent.
            if (!night)
                for (int k = 0; k < 3; k++)
                    img.Fill(Shape.Circle(chx + k * s * 0.08f, y + wallH + s * (0.62f + k * 0.18f), s * (0.06f + k * 0.025f)), Rgba.White.WithAlpha(0.55f - k * 0.13f), s * 0.03f);

            // Murs en rondins, bouts de rondins qui dépassent aux coins.
            var walls = Shape.Box(x, y + wallH * 0.5f, hw, wallH * 0.5f, s * 0.02f);
            Toon.Part(img, walls, wood, s * 2, Village);
            int rows = 5;
            for (int r = 1; r < rows; r++)
                img.FillClipped(walls, Shape.Box(x, y + wallH * r / rows, hw, s * 0.008f), wood.Shade(0.7f), 1f);
            for (int r = 0; r < rows; r++)
            {
                float ly = y + wallH * (r + 0.5f) / rows;
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    Toon.Part(img, Shape.Circle(x + sgn * (hw + s * 0.02f), ly, wallH / rows * 0.45f), wood.Light(1.1f), s * 0.2f, Village);
                    img.Fill(Shape.Circle(x + sgn * (hw + s * 0.02f), ly, wallH / rows * 0.16f), wood.Shade(0.75f));
                }
            }

            // Toit à deux pans, avec bardeaux.
            float eave = y + wallH - s * 0.02f, peak = y + wallH + s * (small ? 0.42f : 0.5f);
            var roofShape = Shape.Poly(x - hw - s * 0.14f, eave, x + hw + s * 0.14f, eave, x, peak).Grow(s * 0.025f);
            Toon.Part(img, roofShape, roof, s * 2, Village);
            for (int r = 1; r < 4; r++)
                img.FillClipped(roofShape, Shape.Box(x, eave + (peak - eave) * r / 4f, hw * 2, s * 0.008f), roof.Shade(0.75f), 1f);
            img.FillClipped(roofShape, Shape.Box(x, eave + s * 0.01f, hw * 2, s * 0.022f), roof.Light(1.2f), 1f);

            // Porte et fenêtre (allumée la nuit).
            float dx = small ? x : x - hw * 0.45f;
            var door = Shape.Box(dx, y + s * 0.2f, s * 0.1f, s * 0.2f, s * 0.04f);
            Toon.Part(img, door, wood.Shade(0.6f), s, Village);
            img.Fill(Shape.Circle(dx + s * 0.05f, y + s * 0.2f, s * 0.014f), Rgba.FromHex("#FFC93C"));
            if (!small)
            {
                float wx = x + hw * 0.4f, wy = y + wallH * 0.55f;
                var glass = night ? Rgba.FromHex("#FFD66B") : Rgba.FromHex("#BDE6FF");
                if (night) img.Fill(Shape.Circle(wx, wy, s * 0.22f), Rgba.FromHex("#FFC15E").WithAlpha(0.35f), s * 0.18f);
                var win = Shape.Box(wx, wy, s * 0.12f, s * 0.1f, s * 0.015f);
                Toon.Part(img, win, glass, s * 0.5f, new Toon.Style { Outline = 0.08f, OutlineColor = wood.Shade(0.45f), Shade = 0.08f, Rim = 0f, Specular = false });
                img.FillClipped(win, Shape.Box(wx, wy, s * 0.008f, s * 0.12f), wood.Shade(0.55f), 1f);
                img.FillClipped(win, Shape.Box(wx, wy, s * 0.14f, s * 0.008f), wood.Shade(0.55f), 1f);
                // Jardinière fleurie sous la fenêtre.
                img.Fill(Shape.Box(wx, wy - s * 0.13f, s * 0.14f, s * 0.025f, s * 0.01f), wood.Shade(0.65f));
                if (!night)
                    for (int k = -2; k <= 2; k++)
                        img.Fill(Shape.Circle(wx + k * s * 0.055f, wy - s * 0.095f, s * 0.022f), k % 2 == 0 ? Rgba.FromHex("#F25CA2") : Rgba.FromHex("#FFC93C"));
            }
            else if (night)
            {
                // Petite lucarne allumée sous le toit.
                img.Fill(Shape.Circle(x, y + wallH + s * 0.16f, s * 0.05f), Rgba.FromHex("#FFD66B"));
            }
        }

        static void FarVillage(Raster img, Theme t, Shape hills, int w, int h)
        {
            VillageColors(t, out var wood, out var roof, out _);
            // Au loin : couleurs plus pâles, mêlées à la colline (perspective).
            wood = Rgba.Lerp(wood, t.FarHills, 0.4f);
            roof = Rgba.Lerp(roof, t.FarHills, 0.35f);
            float[] at = { 0.06f, 0.15f, 0.29f, 0.7f, 0.84f };
            for (int i = 0; i < at.Length; i++)
            {
                float x = w * at[i], y = TopOf(hills, x, h) - h * 0.01f, s = h * (0.08f + (i % 2) * 0.02f);
                img.Fill(Shape.Box(x, y + s * 0.3f, s * 0.42f, s * 0.3f), wood);
                img.Fill(Shape.Poly(x - s * 0.55f, y + s * 0.58f, x + s * 0.55f, y + s * 0.58f, x, y + s * 0.98f), roof);
                if (t.Night) img.Fill(Shape.Box(x + s * 0.15f, y + s * 0.3f, s * 0.07f, s * 0.07f), Rgba.FromHex("#FFD66B"));
            }
            // Moulin à vent.
            float mx = w * 0.2f, my = TopOf(hills, mx, h) - h * 0.01f, ms = h * 0.2f;
            img.Fill(Shape.Poly(mx - ms * 0.16f, my, mx + ms * 0.16f, my, mx + ms * 0.1f, my + ms * 0.6f, mx - ms * 0.1f, my + ms * 0.6f), Rgba.Lerp(wood, Rgba.White, 0.35f));
            img.Fill(Shape.Poly(mx - ms * 0.14f, my + ms * 0.58f, mx + ms * 0.14f, my + ms * 0.58f, mx, my + ms * 0.76f), roof);
            float hx = mx, hy = my + ms * 0.62f;
            for (int k = 0; k < 4; k++)
            {
                double a = Math.PI / 4 + k * Math.PI / 2;
                float ex = hx + (float)Math.Cos(a) * ms * 0.42f, ey = hy + (float)Math.Sin(a) * ms * 0.42f;
                img.Fill(Shape.Capsule(hx, hy, ex, ey, ms * 0.012f), wood.Shade(0.7f));
                float px = (float)-Math.Sin(a), py = (float)Math.Cos(a);
                img.Fill(Shape.Poly(hx + (ex - hx) * 0.25f, hy + (ey - hy) * 0.25f, ex, ey, ex + px * ms * 0.08f, ey + py * ms * 0.08f, hx + (ex - hx) * 0.25f + px * ms * 0.08f, hy + (ey - hy) * 0.25f + py * ms * 0.08f),
                    Rgba.Lerp(Rgba.White, t.FarHills, 0.3f).WithAlpha(0.9f));
            }
            img.Fill(Shape.Circle(hx, hy, ms * 0.03f), wood.Shade(0.6f));
        }
    }
}
