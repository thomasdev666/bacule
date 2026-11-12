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
    public static partial class MascotArtist
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

        /// <summary>
        /// Corps pour une clé « peau » ou « peau_suit » (HeadDef.BodyKey) : débardeur de Basile ou costume croisé.
        /// </summary>
        public static Dictionary<string, Part> Build(string bodyKey)
        {
            bool suit = bodyKey.EndsWith("_suit", StringComparison.OrdinalIgnoreCase);
            int cut = bodyKey.IndexOf('_');
            var skin = Rgba.FromHex(cut >= 0 ? bodyKey.Substring(0, cut) : bodyKey);
            // « peau_h12 » : la tenue de la tête 12.
            int h = bodyKey.IndexOf("_h", StringComparison.Ordinal);
            if (h >= 0 && int.TryParse(bodyKey.Substring(h + 2), out int headId)) return BuildOutfit(skin, OutfitCatalog.For(headId));
            return suit ? BuildSuit(skin) : Build(skin);
        }

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

        static Part Torso(Rgba skin) => TankTorso(skin, Tank, Rgba.White, Emblem.B, true, Motif.None, Rgba.White);

        /// <summary>Débardeur (couleur, liseré, écusson, ceinture, motif).</summary>
        static Part TankTorso(Rgba skin, Rgba Tank, Rgba trim, Emblem emblem, bool withBelt, Motif motif, Rgba accent)
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
            img.FillClipped(tank, collar, trim, 1f);
            ApplyMotif(img, tank, motif, Tank, accent, false);
            // Plis et volume des pectoraux.
            img.FillClipped(tank, Shape.Arc(cx - 0.3f * u, cy + 0.02f * u, 0.32f * u, 200f, 330f, 0.03f * u), Tank.Shadow(0.7f).WithAlpha(0.6f), 2f);
            img.FillClipped(tank, Shape.Arc(cx + 0.3f * u, cy + 0.02f * u, 0.32f * u, 210f, 340f, 0.03f * u), Tank.Shadow(0.7f).WithAlpha(0.6f), 2f);
            img.FillClipped(tank, Shape.Arc(cx, cy - 0.35f * u, 0.5f * u, 230f, 310f, 0.025f * u), Tank.Shadow(0.75f).WithAlpha(0.45f), 2f);

            // Emblème : un « B » dans un rond blanc.
            float ey = cy - 0.18f * u;
            if (emblem != Emblem.B && emblem != Emblem.Patch) DrawEmblem(img, emblem, cx, ey, 0.29f * u, trim, Tank);
            if (emblem == Emblem.B) {
            Toon.Part(img, Shape.Circle(cx, ey, 0.29f * u), trim, 0.6f * u, new Toon.Style { Outline = 0.05f, OutlineColor = Tank.Outline(), Shade = 0.15f, Soft = 0.2f, Rim = 0f, Specular = false });
            var stem = Shape.Box(cx - 0.07f * u, ey, 0.045f * u, 0.17f * u, 0.015f * u);
            var bowlTop = Shape.Subtract(Shape.Box(cx + 0.0f * u, ey + 0.08f * u, 0.12f * u, 0.09f * u, 0.08f * u), Shape.Box(cx - 0.0f * u, ey + 0.08f * u, 0.045f * u, 0.025f * u, 0.02f * u));
            var bowlBot = Shape.Subtract(Shape.Box(cx + 0.01f * u, ey - 0.075f * u, 0.135f * u, 0.095f * u, 0.085f * u), Shape.Box(cx + 0.005f * u, ey - 0.075f * u, 0.055f * u, 0.03f * u, 0.025f * u));
            var letter = Shape.Union(stem, Shape.Intersect(Shape.Union(bowlTop, bowlBot), Shape.HalfPlane(-1, 0, -(cx - 0.11f * u))));
            img.Fill(letter, Tank.Shade(0.95f));
            }

            if (withBelt) LifterBelt(img, cx, cy - 0.84f * u);
            part.PivotY = cy;
            return part;
        }

        /// <summary>Ceinture d'haltérophile en cuir, boucle dorée.</summary>
        static void LifterBelt(Raster img, float cx, float by)
        {
            float u = U;
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
        }

        static Part Shorts() => Shorts(ShortsColor);

        static Part Shorts(Rgba ShortsColor)
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

        static Part Shin(Rgba skin) => Shin(skin, Sock, Tank);

        static Part Shin(Rgba skin, Rgba Sock, Rgba Tank) =>
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

        static Part Forearm(Rgba skin) => Forearm(skin, Tank, Rgba.White);

        static Part Forearm(Rgba skin, Rgba Tank, Rgba stripe) =>
            Segment(MascotRig.Forearm, 0.19f, 0.15f, skin, (img, limb, ax, bx) =>
            {
                float u = U;
                float x0 = ax + (bx - ax) * 0.78f, x1 = ax + (bx - ax) * 0.98f;
                var band = Shape.Intersect(limb.Grow(0.02f * u), Shape.Intersect(Shape.HalfPlane(-1, 0, -x0), Shape.HalfPlane(1, 0, x1)));
                Toon.Part(img, band, Tank, 0.4f * u, Limb);
                img.FillClipped(band, Shape.Box((x0 + x1) * 0.5f, 0, 0.022f * u, 10 * u), stripe, 1f);
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

        static Part Shoe() => Shoe(ShoeColor);

        static Part Shoe(Rgba ShoeColor)
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

        // ================================================================== Costume croisé (tête Challenger)

        public static readonly Rgba Navy = Rgba.FromHex("#1F2645");
        static readonly Rgba Pinstripe = Rgba.FromHex("#B7C1EE").WithAlpha(0.34f);
        static readonly Rgba Shirt = Rgba.FromHex("#F8F7F2");
        static readonly Rgba DressShoe = Rgba.FromHex("#1C1B22");
        static readonly Rgba ButtonColor = Rgba.FromHex("#10131F");
        const float StripeGap = 0.085f;

        /// <summary>
        /// Costume croisé bleu marine à fines rayures, chemise blanche col ouvert, pantalon assorti, chaussures de ville
        /// noires. Mêmes tailles et mêmes pivots que le corps de Basile : le squelette ne change pas.
        /// </summary>
        public static Dictionary<string, Part> BuildSuit(Rgba skin)
        {
            return new Dictionary<string, Part>
            {
                ["torso"] = SuitTorso(skin, Navy, Pinstripe),
                ["shorts"] = SuitHips(Navy, Pinstripe),
                ["thigh"] = Segment(MascotRig.Thigh, 0.3f, 0.25f, Navy, (img, limb, ax, bx) => Stripes(img, limb, true, Pinstripe), 0),
                ["shin"] = Segment(MascotRig.Shin, 0.25f, 0.21f, Navy, (img, limb, ax, bx) => Stripes(img, limb, true, Pinstripe), 0),
                ["upperArm"] = Segment(MascotRig.UpperArm, 0.26f, 0.2f, Navy, (img, limb, ax, bx) => Stripes(img, limb, true, Pinstripe), 0),
                ["forearm"] = SuitForearm(Navy, Pinstripe),
                ["hand"] = Hand(skin),
                ["shoe"] = DressShoes(DressShoe),
                ["cushion"] = Cushion(),
            };
        }

        /// <summary>Fines rayures claires : verticales sur le buste, dans la longueur sur les membres (dessinés à l'horizontale).</summary>
        /// <summary>Fines rayures ; une couleur transparente (alpha 0) n'en dessine aucune.</summary>
        static void Stripes(Raster img, Shape clip, bool alongX, Rgba Pinstripe)
        {
            if (Pinstripe.a == 0) return;
            float u = U, gap = StripeGap * u;
            int n = (int)Math.Ceiling((alongX ? img.Height : img.Width) / gap) + 1;
            for (int i = 0; i < n; i++)
            {
                float p = i * gap + gap * 0.5f;
                var line = alongX ? Shape.Box(img.Width * 0.5f, p, img.Width, 0.0075f * u) : Shape.Box(p, img.Height * 0.5f, 0.0075f * u, img.Height);
                img.FillClipped(clip, line, Pinstripe, 1f);
            }
        }

        static Part SuitTorso(Rgba skin, Rgba Navy, Rgba Pinstripe)
        {
            var part = Canvas(2.3f, 2.2f, out var img, out float cx, out float cy);
            float u = U;

            // Cou (peau).
            Toon.Part(img, Shape.Tapered(cx, cy + 0.5f * u, 0.26f * u, cx, cy + 0.95f * u, 0.24f * u), skin, u, Body);
            img.Fill(Shape.Ellipse(cx, cy + 0.82f * u, 0.3f * u, 0.12f * u), skin.Shadow(0.7f).WithAlpha(0.6f), 0.08f * u);

            // Chemise blanche, col ouvert : on voit un peu de peau en V sous le cou.
            var shirt = Shape.Poly(cx - 0.34f * u, cy + 0.7f * u, cx + 0.34f * u, cy + 0.7f * u, cx + 0.12f * u, cy - 0.3f * u, cx - 0.12f * u, cy - 0.3f * u);
            Toon.Part(img, shirt, Shirt, u, Body);
            var skinV = Shape.Poly(cx - 0.13f * u, cy + 0.72f * u, cx + 0.13f * u, cy + 0.72f * u, cx, cy + 0.36f * u).Grow(0.02f * u);
            img.FillClipped(shirt, skinV, skin, 1.5f);
            img.FillClipped(shirt, Shape.Capsule(cx, cy + 0.36f * u, cx, cy - 0.3f * u, 0.012f * u), Shirt.Shade(0.8f), 1f); // patte de boutonnage
            // Pointes du col, posées sur le revers.
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var collar = Shape.Poly(cx + sgn * 0.13f * u, cy + 0.74f * u, cx + sgn * 0.34f * u, cy + 0.66f * u, cx + sgn * 0.24f * u, cy + 0.42f * u, cx + sgn * 0.06f * u, cy + 0.5f * u).Grow(0.02f * u);
                Toon.Part(img, collar, Shirt, 0.5f * u, new Toon.Style { Outline = 0.05f, Shade = 0.18f, Soft = 0.2f, Rim = 0f, Specular = false });
            }

            // Veste : épaules carrées (épaulettes), buste, bas de veste qui couvre la taille.
            var shoulders = Shape.Smooth(
                Shape.Smooth(Shape.Circle(cx - 0.8f * u, cy + 0.55f * u, 0.31f * u), Shape.Circle(cx + 0.8f * u, cy + 0.55f * u, 0.31f * u), 0.2f * u),
                Shape.Poly(cx - 0.86f * u, cy + 0.7f * u, cx + 0.86f * u, cy + 0.7f * u, cx + 0.66f * u, cy + 0.05f * u, cx - 0.66f * u, cy + 0.05f * u).Grow(0.08f * u), 0.25f * u);
            var body = Shape.Poly(cx - 0.68f * u, cy + 0.4f * u, cx + 0.68f * u, cy + 0.4f * u, cx + 0.66f * u, cy - 0.98f * u, cx - 0.66f * u, cy - 0.98f * u).Grow(0.06f * u);
            var jacket = Shape.Smooth(shoulders, body, 0.2f * u);
            // Ouverture en V jusqu'au boutonnage croisé.
            var opening = Shape.Poly(cx - 0.36f * u, cy + 0.8f * u, cx + 0.36f * u, cy + 0.8f * u, cx + 0.03f * u, cy - 0.3f * u, cx - 0.03f * u, cy - 0.3f * u);
            jacket = Shape.Subtract(jacket, opening);
            Toon.Part(img, jacket, Navy, 2 * u, Body);
            Stripes(img, jacket, false, Pinstripe);

            // Revers à cran pointu (« peak lapels »), un peu plus sombres, avec leur couture.
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var lapel = Shape.Poly(
                    cx + sgn * 0.36f * u, cy + 0.8f * u,
                    cx + sgn * 0.52f * u, cy + 0.5f * u,
                    cx + sgn * 0.62f * u, cy + 0.56f * u, // pointe du revers
                    cx + sgn * 0.5f * u, cy + 0.32f * u,
                    cx + sgn * 0.04f * u, cy - 0.32f * u,
                    cx + sgn * 0.03f * u, cy - 0.3f * u);
                var l = Shape.Intersect(lapel.Grow(0.01f * u), jacket.Grow(0.02f * u));
                Toon.Part(img, l, Navy.Shade(0.86f), u, new Toon.Style { Outline = 0.03f, Shade = 0.12f, Soft = 0.2f, Rim = 0.05f, RimOpacity = 0.35f, Specular = false });
                Stripes(img, l, false, Pinstripe);
            }

            // Plis du tissu.
            img.FillClipped(jacket, Shape.Arc(cx - 0.42f * u, cy - 0.1f * u, 0.32f * u, 200f, 300f, 0.022f * u), Navy.Outline().WithAlpha(0.5f), 2f);
            img.FillClipped(jacket, Shape.Arc(cx + 0.42f * u, cy - 0.1f * u, 0.32f * u, 240f, 340f, 0.022f * u), Navy.Outline().WithAlpha(0.5f), 2f);

            // Poche poitrine et pochette blanche (côté gauche du personnage, à droite de l'image).
            float px = cx + 0.4f * u, py = cy + 0.05f * u;
            img.FillClipped(jacket, Shape.Box(px, py, 0.14f * u, 0.014f * u, 0.005f * u, -6f), Navy.Outline().WithAlpha(0.8f), 1f);
            Toon.Part(img, Shape.Poly(px - 0.1f * u, py + 0.02f * u, px - 0.02f * u, py + 0.11f * u, px + 0.04f * u, py + 0.04f * u, px + 0.1f * u, py + 0.1f * u, px + 0.12f * u, py + 0.02f * u),
                Shirt, 0.3f * u, new Toon.Style { Outline = 0.06f, Shade = 0.12f, Soft = 0.2f, Rim = 0f, Specular = false });
            // Poches basses à rabat.
            for (int sgn = -1; sgn <= 1; sgn += 2)
                img.FillClipped(jacket, Shape.Box(cx + sgn * 0.42f * u, cy - 0.66f * u, 0.17f * u, 0.016f * u, 0.006f * u), Navy.Outline().WithAlpha(0.75f), 1f);

            // Six boutons du croisé (deux colonnes de trois).
            for (int row = 0; row < 3; row++)
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    float bx = cx + sgn * (0.2f - row * 0.015f) * u, by = cy - (0.34f + row * 0.19f) * u;
                    Toon.Part(img, Shape.Circle(bx, by, 0.042f * u), ButtonColor, 0.12f * u,
                        new Toon.Style { Outline = 0.08f, Shade = 0.2f, Soft = 0.2f, Rim = 0.1f, RimOpacity = 0.6f, Specular = true, SpecSize = 0.12f });
                }
            // Bord croisé : le pan droit passe sur le pan gauche.
            img.FillClipped(jacket, Shape.Capsule(cx + 0.03f * u, cy - 0.3f * u, cx + 0.05f * u, cy - 0.98f * u, 0.014f * u), Navy.Outline().WithAlpha(0.85f), 1f);

            part.PivotY = cy;
            return part;
        }

        /// <summary>Haut du pantalon (à la place du short).</summary>
        static Part SuitHips(Rgba Navy, Rgba Pinstripe)
        {
            var part = Canvas(1.9f, 1.2f, out var img, out float cx, out float cy);
            float u = U;
            var waist = Shape.Box(cx, cy + 0.2f * u, 0.66f * u, 0.22f * u, 0.1f * u);
            var legL = Shape.Box(cx - 0.36f * u, cy - 0.12f * u, 0.33f * u, 0.32f * u, 0.1f * u, -8f);
            var legR = Shape.Box(cx + 0.36f * u, cy - 0.12f * u, 0.33f * u, 0.32f * u, 0.1f * u, 8f);
            var trousers = Shape.Smooth(waist, Shape.Union(legL, legR), 0.15f * u);
            Toon.Part(img, trousers, Navy, 1.4f * u, Body);
            Stripes(img, trousers, false, Pinstripe);
            img.FillClipped(trousers, Shape.Capsule(cx, cy - 0.05f * u, cx, cy - 0.45f * u, 0.025f * u), Navy.Outline(), 2f);
            part.PivotY = cy;
            return part;
        }

        /// <summary>Manche de veste, poignet de chemise blanc qui dépasse.</summary>
        static Part SuitForearm(Rgba Navy, Rgba Pinstripe) =>
            Segment(MascotRig.Forearm, 0.21f, 0.17f, Navy, (img, limb, ax, bx) =>
            {
                float u = U;
                Stripes(img, limb, true, Pinstripe);
                float x0 = ax + (bx - ax) * 0.86f;
                var cuff = Shape.Intersect(limb.Grow(0.01f * u), Shape.HalfPlane(-1, 0, -x0));
                Toon.Part(img, cuff, Shirt, 0.4f * u, Limb);
                img.FillClipped(cuff, Shape.Circle(x0 + 0.06f * u, 0.5f * img.Height - 0.05f * u, 0.022f * u), Rgba.FromHex("#C9CED8"), 1f); // bouton de manchette
            }, 0);

        /// <summary>Chaussures de ville noires, bout fin et brillant.</summary>
        static Part DressShoes(Rgba DressShoe)
        {
            var part = Canvas(1.0f, 0.7f, out var img, out float cx, out float cy);
            float u = U;
            var sole = Shape.Box(cx + 0.05f * u, cy - 0.17f * u, 0.4f * u, 0.05f * u, 0.05f * u);
            var upper = Shape.Smooth(Shape.Ellipse(cx + 0.12f * u, cy - 0.06f * u, 0.36f * u, 0.15f * u), Shape.Ellipse(cx - 0.1f * u, cy + 0.04f * u, 0.22f * u, 0.18f * u), 0.12f * u);
            Toon.Part(img, upper, DressShoe, 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.2f, Soft = 0.18f, Rim = 0.08f, RimOpacity = 0.55f, Specular = true, SpecSize = 0.09f });
            Toon.Part(img, sole, Rgba.FromHex("#3A2A20"), 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.1f, Soft = 0.1f, Rim = 0f, Specular = false });
            img.FillClipped(upper, Shape.Arc(cx - 0.02f * u, cy + 0.02f * u, 0.14f * u, 300f, 420f, 0.012f * u), Rgba.White.WithAlpha(0.25f), 1f); // couture
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
