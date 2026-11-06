using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    /// <summary>
    /// Squelette de la mascotte (unités monde, origine entre les pieds au sol).
    /// Partagé par le jeu Unity (MascotView) et les outils d'aperçu, pour que tout tombe juste.
    /// </summary>
    public static class MascotRig
    {
        public const float HipX = 0.34f, HipY = 1.62f;
        public const float Thigh = 0.7f, Shin = 0.68f;
        public const float AnkleX = 0.5f, AnkleY = 0.34f;
        public const float ShortsY = 1.72f;
        public const float TorsoY = 2.62f;
        public const float ShoulderX = 0.8f, ShoulderY = 3.2f;
        public const float UpperArm = 1.16f, Forearm = 1.12f;
        public const float HeadY = 4.14f;
        /// <summary>Côté du sprite de tête en unités monde.</summary>
        public const float HeadSize = 2.3f;
        public const float CushionY = 4.86f;
        /// <summary>Point d'appui de la planche (dessous de la planche), sans tassement.</summary>
        public const float PivotHeight = 4.96f;
        /// <summary>Distance des mains au pivot le long de la planche.</summary>
        public const float HandReach = 1.02f;
        public const float HandBelow = 0.05f;

        /// <summary>
        /// Cinématique inverse à deux os (épaule → coude → main, hanche → genou → cheville).
        /// Renvoie la position de l'articulation du milieu ; « bendSign » choisit de quel côté elle plie.
        /// </summary>
        public static void TwoBone(float ax, float ay, float tx, float ty, float l1, float l2, float bendSign, out float jx, out float jy)
        {
            float dx = tx - ax, dy = ty - ay;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            float maxReach = l1 + l2 - 1e-4f, minReach = Math.Abs(l1 - l2) + 1e-4f;
            float dc = d < minReach ? minReach : d > maxReach ? maxReach : d;
            float baseAngle = (float)Math.Atan2(dy, dx);
            float cos = (l1 * l1 + dc * dc - l2 * l2) / (2 * l1 * dc);
            cos = cos < -1 ? -1 : cos > 1 ? 1 : cos;
            float a = baseAngle + bendSign * (float)Math.Acos(cos);
            jx = ax + (float)Math.Cos(a) * l1;
            jy = ay + (float)Math.Sin(a) * l1;
        }
    }

    /// <summary>Un morceau de la mascotte : image, pivot (en pixels) et densité (pixels par unité).</summary>
    public sealed class Part
    {
        public Raster Image;
        public float PivotX, PivotY;
        public float PixelsPerUnit;
    }

    /// <summary>
    /// Corps de la mascotte : débardeur à l'emblème du jeu, ceinture d'haltérophile, short, chaussettes rayées,
    /// baskets, bras musclés avec bandeaux de poignet. Chaque membre est un sprite séparé articulé par MascotView.
    /// </summary>
    public static class MascotArtist
    {
        public const float U = 160f; // pixels par unité monde

        public static readonly Rgba Tank = Rgba.FromHex("#E63946");
        public static readonly Rgba ShortsColor = Rgba.FromHex("#2B3A67");
        public static readonly Rgba Leather = Rgba.FromHex("#8B5A2B");
        public static readonly Rgba Gold = Rgba.FromHex("#FFC93C");
        public static readonly Rgba ShoeColor = Rgba.FromHex("#3A86FF");
        public static readonly Rgba Sock = Rgba.FromHex("#FFFFFF");
        public static readonly Rgba Ink = Rgba.FromHex("#2B2D42");

        static readonly Toon.Style Body = new Toon.Style { Outline = 0.03f, Shade = 0.18f, Soft = 0.14f, ShadeStrength = 0.8f, Rim = 0.04f, RimOpacity = 0.45f, Specular = false };
        static readonly Toon.Style Limb = new Toon.Style { Outline = 0.06f, Shade = 0.3f, Soft = 0.25f, ShadeStrength = 0.8f, Rim = 0.08f, RimOpacity = 0.4f, Specular = false };

        public static Dictionary<string, Part> Build(Rgba skin)
        {
            return new Dictionary<string, Part>
            {
                ["torso"] = Torso(skin),
                ["shorts"] = Shorts(),
                ["thigh"] = Segment(MascotRig.Thigh, 0.29f, 0.22f, skin, null, 0),
                ["shin"] = Shin(skin),
                ["upperArm"] = Segment(MascotRig.UpperArm, 0.24f, 0.18f, skin, null, 1),
                ["forearm"] = Forearm(skin),
                ["hand"] = Hand(skin),
                ["shoe"] = Shoe(),
                ["cushion"] = Cushion(),
            };
        }

        static Part Canvas(float wUnits, float hUnits, out Raster img, out float cx, out float cy)
        {
            int w = (int)Math.Ceiling(wUnits * U), h = (int)Math.Ceiling(hUnits * U);
            img = new Raster(w, h);
            cx = w * 0.5f; cy = h * 0.5f;
            return new Part { Image = img, PivotX = cx, PivotY = cy, PixelsPerUnit = U };
        }

        static Part Torso(Rgba skin)
        {
            var part = Canvas(2.3f, 2.2f, out var img, out float cx, out float cy);
            float u = U;

            // Cou et épaules (peau).
            Toon.Part(img, Shape.Tapered(cx, cy + 0.5f * u, 0.26f * u, cx, cy + 0.95f * u, 0.24f * u), skin, u, Body);
            var shoulders = Shape.Smooth(
                Shape.Smooth(Shape.Circle(cx - 0.78f * u, cy + 0.55f * u, 0.3f * u), Shape.Circle(cx + 0.78f * u, cy + 0.55f * u, 0.3f * u), 0.2f * u),
                Shape.Poly(cx - 0.82f * u, cy + 0.66f * u, cx + 0.82f * u, cy + 0.66f * u, cx + 0.6f * u, cy + 0.05f * u, cx - 0.6f * u, cy + 0.05f * u).Grow(0.08f * u), 0.3f * u);
            Toon.Part(img, shoulders, skin, 2 * u, Body);
            // Ombre du menton sur le cou.
            img.Fill(Shape.Ellipse(cx, cy + 0.82f * u, 0.3f * u, 0.12f * u), skin.Shadow(0.7f).WithAlpha(0.6f), 0.08f * u);

            // Débardeur.
            var tank = Shape.Smooth(
                Shape.Poly(cx - 0.6f * u, cy + 0.45f * u, cx + 0.6f * u, cy + 0.45f * u, cx + 0.62f * u, cy - 0.86f * u, cx - 0.62f * u, cy - 0.86f * u).Grow(0.06f * u),
                Shape.Union(Shape.Capsule(cx - 0.42f * u, cy + 0.4f * u, cx - 0.46f * u, cy + 0.68f * u, 0.14f * u),
                    Shape.Capsule(cx + 0.42f * u, cy + 0.4f * u, cx + 0.46f * u, cy + 0.68f * u, 0.14f * u)), 0.12f * u);
            tank = Shape.Subtract(tank, Shape.Ellipse(cx, cy + 0.62f * u, 0.3f * u, 0.36f * u));
            Toon.Part(img, tank, Tank, 2 * u, Body);
            // Liseré blanc autour de l'encolure et des emmanchures.
            var collar = Shape.Subtract(Shape.Ellipse(cx, cy + 0.62f * u, 0.37f * u, 0.43f * u), Shape.Ellipse(cx, cy + 0.62f * u, 0.3f * u, 0.36f * u));
            img.FillClipped(tank, collar, Rgba.White, 1f);
            // Plis et volume des pectoraux.
            img.FillClipped(tank, Shape.Arc(cx - 0.3f * u, cy + 0.02f * u, 0.32f * u, 200f, 330f, 0.03f * u), Tank.Shadow(0.7f).WithAlpha(0.6f), 2f);
            img.FillClipped(tank, Shape.Arc(cx + 0.3f * u, cy + 0.02f * u, 0.32f * u, 210f, 340f, 0.03f * u), Tank.Shadow(0.7f).WithAlpha(0.6f), 2f);
            img.FillClipped(tank, Shape.Arc(cx, cy - 0.35f * u, 0.5f * u, 230f, 310f, 0.025f * u), Tank.Shadow(0.75f).WithAlpha(0.45f), 2f);

            // Emblème : un « B » dans un rond blanc.
            float ey = cy - 0.18f * u;
            Toon.Part(img, Shape.Circle(cx, ey, 0.29f * u), Rgba.White, 0.6f * u, new Toon.Style { Outline = 0.05f, OutlineColor = Tank.Outline(), Shade = 0.15f, Soft = 0.2f, Rim = 0f, Specular = false });
            var stem = Shape.Box(cx - 0.07f * u, ey, 0.045f * u, 0.17f * u, 0.015f * u);
            var bowlTop = Shape.Subtract(Shape.Box(cx + 0.0f * u, ey + 0.08f * u, 0.12f * u, 0.09f * u, 0.08f * u), Shape.Box(cx - 0.0f * u, ey + 0.08f * u, 0.045f * u, 0.025f * u, 0.02f * u));
            var bowlBot = Shape.Subtract(Shape.Box(cx + 0.01f * u, ey - 0.075f * u, 0.135f * u, 0.095f * u, 0.085f * u), Shape.Box(cx + 0.005f * u, ey - 0.075f * u, 0.055f * u, 0.03f * u, 0.025f * u));
            var letter = Shape.Union(stem, Shape.Intersect(Shape.Union(bowlTop, bowlBot), Shape.HalfPlane(-1, 0, -(cx - 0.11f * u))));
            img.Fill(letter, Tank.Shade(0.95f));

            // Ceinture d'haltérophile.
            float by = cy - 0.84f * u;
            var belt = Shape.Box(cx, by, 0.7f * u, 0.17f * u, 0.07f * u);
            Toon.Part(img, belt, Leather, u, new Toon.Style { Outline = 0.05f, Shade = 0.25f, Soft = 0.2f, Rim = 0.06f, RimOpacity = 0.5f, Specular = false });
            for (int i = -6; i <= 6; i++)
            {
                if (Math.Abs(i) <= 1) continue;
                img.FillClipped(belt, Shape.Box(cx + i * 0.1f * u, by + 0.11f * u, 0.025f * u, 0.008f * u), Leather.Light(1.4f).WithAlpha(0.8f), 1f);
                img.FillClipped(belt, Shape.Box(cx + i * 0.1f * u, by - 0.11f * u, 0.025f * u, 0.008f * u), Leather.Light(1.4f).WithAlpha(0.8f), 1f);
            }
            var buckle = Shape.Subtract(Shape.Box(cx, by, 0.2f * u, 0.15f * u, 0.04f * u), Shape.Box(cx, by, 0.11f * u, 0.07f * u, 0.02f * u));
            Toon.Part(img, buckle, Gold, 0.5f * u, new Toon.Style { Outline = 0.06f, Shade = 0.2f, Soft = 0.15f, Rim = 0.06f, RimOpacity = 0.6f, Specular = true, SpecSize = 0.1f });
            img.Fill(Shape.Box(cx + 0.02f * u, by, 0.12f * u, 0.02f * u, 0.01f * u), Gold.Shade(0.7f));

            part.PivotY = cy;
            return part;
        }

        static Part Shorts()
        {
            var part = Canvas(1.9f, 1.2f, out var img, out float cx, out float cy);
            float u = U;
            var waist = Shape.Box(cx, cy + 0.2f * u, 0.64f * u, 0.22f * u, 0.12f * u);
            var legL = Shape.Box(cx - 0.36f * u, cy - 0.12f * u, 0.32f * u, 0.3f * u, 0.12f * u, -10f);
            var legR = Shape.Box(cx + 0.36f * u, cy - 0.12f * u, 0.32f * u, 0.3f * u, 0.12f * u, 10f);
            var shorts = Shape.Smooth(waist, Shape.Union(legL, legR), 0.15f * u);
            Toon.Part(img, shorts, ShortsColor, 1.4f * u, Body);
            // Bandes blanches sur les côtés et pli entre les jambes.
            img.FillClipped(shorts, Shape.Capsule(cx - 0.62f * u, cy + 0.3f * u, cx - 0.74f * u, cy - 0.38f * u, 0.04f * u), Rgba.White, 1f);
            img.FillClipped(shorts, Shape.Capsule(cx + 0.62f * u, cy + 0.3f * u, cx + 0.74f * u, cy - 0.38f * u, 0.04f * u), Rgba.White, 1f);
            img.FillClipped(shorts, Shape.Capsule(cx, cy - 0.05f * u, cx, cy - 0.45f * u, 0.03f * u), ShortsColor.Outline(), 2f);
            part.PivotY = cy;
            return part;
        }

        /// <summary>Membre dessiné à l'horizontale, articulation de départ au pivot.</summary>
        static Part Segment(float length, float ra, float rb, Rgba skin, Action<Raster, Shape, float, float> extra, int bicep)
        {
            float pad = 0.08f;
            float w = length + ra + rb + pad * 2, h = Math.Max(ra, rb) * 2 + pad * 2 + (bicep > 0 ? 0.12f : 0f);
            var part = Canvas(w, h, out var img, out _, out float cy);
            float u = U;
            float ax = (pad + ra) * u, bx = ax + length * u;
            var limb = Shape.Tapered(ax, cy, ra * u, bx, cy, rb * u);
            if (bicep > 0) limb = Shape.Smooth(limb, Shape.Ellipse(ax + length * 0.42f * u, cy + 0.06f * u, 0.32f * u, ra * 0.95f * u), 0.12f * u);
            Toon.Part(img, limb, skin, ra * 2.4f * u, Limb);
            extra?.Invoke(img, limb, ax, bx);
            part.PivotX = ax; part.PivotY = cy;
            return part;
        }

        static Part Shin(Rgba skin) =>
            Segment(MascotRig.Shin, 0.22f, 0.16f, skin, (img, limb, ax, bx) =>
            {
                float u = U;
                var sock = Shape.Intersect(limb.Grow(0.01f * u), Shape.HalfPlane(-1, 0, -(ax + (bx - ax) * 0.45f)));
                Toon.Part(img, Shape.Intersect(limb, Shape.HalfPlane(-1, 0, -(ax + (bx - ax) * 0.45f))), Sock, 0.4f * u, Limb);
                for (int i = 0; i < 2; i++)
                {
                    float x = ax + (bx - ax) * (0.52f + i * 0.1f);
                    img.FillClipped(sock, Shape.Box(x, 0, 0.025f * u, 10 * u), Tank, 1f);
                }
            }, 0);

        static Part Forearm(Rgba skin) =>
            Segment(MascotRig.Forearm, 0.19f, 0.15f, skin, (img, limb, ax, bx) =>
            {
                float u = U;
                float x0 = ax + (bx - ax) * 0.78f, x1 = ax + (bx - ax) * 0.98f;
                var band = Shape.Intersect(limb.Grow(0.02f * u), Shape.Intersect(Shape.HalfPlane(-1, 0, -x0), Shape.HalfPlane(1, 0, x1)));
                Toon.Part(img, band, Tank, 0.4f * u, Limb);
                img.FillClipped(band, Shape.Box((x0 + x1) * 0.5f, 0, 0.022f * u, 10 * u), Rgba.White, 1f);
            }, 0);

        /// <summary>Main qui agrippe la planche (vue de face, poing fermé dessus).</summary>
        static Part Hand(Rgba skin)
        {
            var part = Canvas(0.7f, 0.6f, out var img, out float cx, out float cy);
            float u = U;
            var palm = Shape.Box(cx, cy - 0.02f * u, 0.2f * u, 0.17f * u, 0.12f * u);
            var st = new Toon.Style { Outline = 0.06f, Shade = 0.25f, Soft = 0.2f, ShadeStrength = 0.8f, Rim = 0.06f, RimOpacity = 0.4f, Specular = false };
            Toon.Part(img, palm, skin, 0.5f * u, st);
            for (int i = 0; i < 4; i++)
            {
                float fx = cx - 0.15f * u + i * 0.1f * u;
                Toon.Part(img, Shape.Capsule(fx, cy + 0.08f * u, fx, cy + 0.15f * u, 0.058f * u), skin, 0.18f * u, st);
            }
            Toon.Part(img, Shape.Capsule(cx - 0.2f * u, cy - 0.04f * u, cx - 0.08f * u, cy + 0.05f * u, 0.065f * u), skin.Shade(0.95f), 0.2f * u, st);
            return part;
        }

        static Part Shoe()
        {
            var part = Canvas(1.0f, 0.7f, out var img, out float cx, out float cy);
            float u = U;
            var sole = Shape.Box(cx + 0.04f * u, cy - 0.16f * u, 0.4f * u, 0.08f * u, 0.07f * u);
            var upper = Shape.Smooth(Shape.Ellipse(cx + 0.1f * u, cy - 0.04f * u, 0.34f * u, 0.18f * u), Shape.Ellipse(cx - 0.1f * u, cy + 0.06f * u, 0.22f * u, 0.2f * u), 0.12f * u);
            var st = new Toon.Style { Outline = 0.05f, Shade = 0.22f, Soft = 0.18f, Rim = 0.05f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.06f };
            Toon.Part(img, upper, ShoeColor, 0.8f * u, st);
            Toon.Part(img, sole, Rgba.White, 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.1f, Soft = 0.1f, Rim = 0f, Specular = false });
            img.FillClipped(sole, Shape.Box(cx + 0.04f * u, cy - 0.21f * u, u, 0.02f * u), Rgba.FromHex("#C9CCD6"), 1f);
            // Lacets et virgule.
            for (int i = 0; i < 3; i++)
                img.FillClipped(upper, Shape.Capsule(cx - 0.1f * u + i * 0.07f * u, cy + 0.12f * u - i * 0.05f * u, cx + 0.02f * u + i * 0.07f * u, cy + 0.14f * u - i * 0.05f * u, 0.022f * u), Rgba.White, 1f);
            img.FillClipped(upper, Shape.Arc(cx + 0.18f * u, cy + 0.06f * u, 0.16f * u, 200f, 300f, 0.035f * u), Rgba.White.WithAlpha(0.9f), 1f);
            part.PivotX = cx - 0.08f * u; part.PivotY = cy + 0.1f * u;
            return part;
        }

        /// <summary>Serviette roulée posée sur la tête, sous la planche.</summary>
        static Part Cushion()
        {
            var part = Canvas(1.2f, 0.4f, out var img, out float cx, out float cy);
            float u = U;
            var towel = Shape.Box(cx, cy, 0.5f * u, 0.1f * u, 0.1f * u);
            Toon.Part(img, towel, Rgba.FromHex("#F4F1EA"), 0.4f * u, new Toon.Style { Outline = 0.08f, Shade = 0.3f, Soft = 0.25f, Rim = 0.06f, RimOpacity = 0.5f, Specular = false });
            for (int i = -1; i <= 1; i += 2)
                img.FillClipped(towel, Shape.Box(cx + i * 0.3f * u, cy, 0.035f * u, u), Rgba.FromHex("#3A86FF"), 1f);
            return part;
        }
    }
}
