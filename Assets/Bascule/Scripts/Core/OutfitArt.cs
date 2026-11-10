using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    /// <summary>
    /// Tenues des personnages (une par tête, Core/Outfits.cs) : t-shirt, pull à col roulé, sweat à capuche, maillot,
    /// chemise, blouson ouvert, costume, robe de mage ou de roi, combinaison spatiale ; short, pantalon, jean ou
    /// jogging ; baskets, montantes, bottes ou chaussures de ville. Mêmes tailles et mêmes pivots que le corps de
    /// Basile : le squelette ne change jamais, seule l'apparence change.
    /// </summary>
    public static partial class MascotArtist
    {
        static readonly Rgba Glove = Rgba.FromHex("#E9ECEF");
        static readonly Rgba Metal = Rgba.FromHex("#ADB5BD");
        static readonly Rgba Stitch = Rgba.FromHex("#E9A23B");
        static readonly Rgba Brass = Rgba.FromHex("#C9A227");
        static readonly Rgba PinLight = Rgba.FromHex("#B7C1EE").WithAlpha(0.34f);

        static bool LongSleeves(Top t) => t != Top.Tank && t != Top.TShirt && t != Top.Jersey;

        /// <summary>Le corps complet habillé de la tenue <paramref name="o"/>.</summary>
        public static Dictionary<string, Part> BuildOutfit(Rgba skin, OutfitDef o)
        {
            var main = Rgba.FromHex(o.Main);
            var accent = Rgba.FromHex(o.Accent);
            var bottom = Rgba.FromHex(o.BottomColor);
            var pin = o.Top == Top.Suit && o.Motif == Motif.Pinstripe ? PinLight : new Rgba(0, 0, 0, 0);
            var limbMotif = o.Motif == Motif.Stars ? Motif.None : o.Motif;

            Part upper, fore;
            if (o.Top == Top.Tank)
            {
                upper = Segment(MascotRig.UpperArm, 0.24f, 0.18f, skin, null, 1);
                fore = Forearm(skin, main, accent);
            }
            else if (!LongSleeves(o.Top))
            {
                // Manche courte sur le haut du bras, bras musclé dessous.
                upper = Segment(MascotRig.UpperArm, 0.24f, 0.18f, skin, (img, limb, ax, bx) =>
                {
                    float u = U, x1 = ax + (bx - ax) * 0.42f;
                    var sleeve = Shape.Intersect(limb.Grow(0.035f * u), Shape.HalfPlane(1, 0, x1));
                    Toon.Part(img, sleeve, main, 0.5f * u, Limb);
                    ApplyMotif(img, sleeve, limbMotif, main, accent, true);
                    var hem = Shape.Intersect(sleeve.Grow(0.005f * u), Shape.HalfPlane(-1, 0, -(x1 - 0.07f * u)));
                    img.FillClipped(sleeve.Grow(0.005f * u), hem, Contrast(accent, main), 1f);
                }, 1);
                fore = Forearm(skin, main, accent);
            }
            else if (o.Top == Top.Suit)
            {
                upper = Segment(MascotRig.UpperArm, 0.26f, 0.2f, main, (img, limb, ax, bx) => Stripes(img, limb, true, pin), 0);
                fore = SuitForearm(main, pin);
            }
            else
            {
                upper = Segment(MascotRig.UpperArm, 0.26f, 0.2f, main, (img, limb, ax, bx) =>
                {
                    ApplyMotif(img, limb, limbMotif, main, accent, true);
                    if (o.Top == Top.Space)
                    {
                        float u = U, x = ax + (bx - ax) * 0.55f;
                        img.FillClipped(limb.Grow(0.01f * u), Shape.Box(x, img.Height * 0.5f, 0.06f * u, img.Height), accent, 1f);
                    }
                }, 0);
                fore = LongForearm(o.Top, main, accent, limbMotif);
            }

            return new Dictionary<string, Part>
            {
                ["torso"] = OutfitTorso(skin, o, main, accent, pin),
                ["shorts"] = o.Top == Top.Robe ? RobeSkirt(main, accent, o.Motif) : Hips(o.Bottom, bottom, accent, pin),
                ["thigh"] = o.Bottom == Bottom.Shorts && o.Top != Top.Robe
                    ? Segment(MascotRig.Thigh, 0.29f, 0.22f, skin, null, 0)
                    : LegPart(MascotRig.Thigh, 0.3f, 0.25f, o.Bottom, bottom, pin, false),
                ["shin"] = o.Bottom == Bottom.Shorts && o.Top != Top.Robe
                    ? Shin(skin, Rgba.FromHex(o.Sock), main)
                    : LegPart(MascotRig.Shin, 0.25f, 0.21f, o.Bottom, bottom, pin, true),
                ["upperArm"] = upper,
                ["forearm"] = fore,
                ["hand"] = Hand(o.Top == Top.Space ? Glove : skin),
                ["shoe"] = ShoePart(o.Shoe, Rgba.FromHex(o.ShoeColor)),
                ["cushion"] = Cushion(),
            };
        }

        // ================================================================== Haut

        static Part OutfitTorso(Rgba skin, OutfitDef o, Rgba main, Rgba accent, Rgba pin)
        {
            if (o.Top == Top.Tank) return TankTorso(skin, main, accent, o.Emblem, o.Belt, o.Motif, accent);
            if (o.Top == Top.Suit)
            {
                var suit = SuitTorso(skin, main, pin);
                // Épinglette au revers (Légende…).
                if (o.Emblem != Emblem.None)
                {
                    float sx = suit.Image.Width * 0.5f, sy = suit.Image.Height * 0.5f;
                    DrawEmblem(suit.Image, o.Emblem, sx - 0.42f * U, sy + 0.3f * U, 0.1f * U, accent, main);
                }
                return suit;
            }

            var part = Canvas(2.3f, 2.2f, out var img, out float cx0, out float cy0);
            float u = U, cx = cx0, cy = cy0;

            // Cou (peau) et ombre du menton.
            Toon.Part(img, Shape.Tapered(cx, cy + 0.5f * u, 0.26f * u, cx, cy + 0.95f * u, 0.24f * u), skin, u, Body);
            img.Fill(Shape.Ellipse(cx, cy + 0.82f * u, 0.3f * u, 0.12f * u), skin.Shadow(0.7f).WithAlpha(0.6f), 0.08f * u);

            // Silhouette commune : épaules couvertes, buste jusqu'à la taille (la robe descend un peu plus bas).
            float hemY = o.Top == Top.Robe ? -1.0f : -0.92f;
            var shoulders = Shape.Smooth(
                Shape.Smooth(Shape.Circle(cx - 0.8f * u, cy + 0.55f * u, 0.31f * u), Shape.Circle(cx + 0.8f * u, cy + 0.55f * u, 0.31f * u), 0.2f * u),
                Shape.Poly(cx - 0.86f * u, cy + 0.68f * u, cx + 0.86f * u, cy + 0.68f * u, cx + 0.66f * u, cy + 0.05f * u, cx - 0.66f * u, cy + 0.05f * u).Grow(0.08f * u), 0.25f * u);
            var body = Shape.Poly(cx - 0.66f * u, cy + 0.4f * u, cx + 0.66f * u, cy + 0.4f * u, cx + 0.64f * u, cy + hemY * u, cx - 0.64f * u, cy + hemY * u).Grow(0.05f * u);
            var cloth = Shape.Smooth(shoulders, body, 0.2f * u);
            var trim = Contrast(accent, main);

            switch (o.Top)
            {
                case Top.TShirt:
                {
                    cloth = Shape.Subtract(cloth, Shape.Ellipse(cx, cy + 0.68f * u, 0.28f * u, 0.24f * u));
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    var rib = Shape.Subtract(Shape.Ellipse(cx, cy + 0.68f * u, 0.35f * u, 0.31f * u), Shape.Ellipse(cx, cy + 0.68f * u, 0.28f * u, 0.24f * u));
                    img.FillClipped(cloth, rib, trim, 1f);
                    Folds(img, cloth, main, cx, cy);
                    Badge(img, o.Emblem, cx, cy - 0.12f * u, 0.25f * u, accent, main);
                    break;
                }
                case Top.Jersey:
                {
                    var vOut = Shape.Poly(cx - 0.3f * u, cy + 0.9f * u, cx + 0.3f * u, cy + 0.9f * u, cx, cy + 0.22f * u);
                    var vIn = Shape.Poly(cx - 0.22f * u, cy + 0.9f * u, cx + 0.22f * u, cy + 0.9f * u, cx, cy + 0.34f * u);
                    cloth = Shape.Subtract(cloth, vIn);
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    img.FillClipped(cloth, Shape.Subtract(vOut, vIn), trim, 1f);
                    // Bandes de côté et liseré des épaules.
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        img.FillClipped(cloth, Shape.Box(cx + sgn * 0.66f * u, cy - 0.3f * u, 0.09f * u, 0.75f * u), trim, 1f);
                        img.FillClipped(cloth, Shape.Capsule(cx + sgn * 0.42f * u, cy + 0.7f * u, cx + sgn * 1.0f * u, cy + 0.5f * u, 0.035f * u), trim, 1f);
                    }
                    Folds(img, cloth, main, cx, cy);
                    Badge(img, o.Emblem, cx, cy - 0.18f * u, 0.27f * u, accent, main);
                    break;
                }
                case Top.Sweater:
                {
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    Ribs(img, cloth, Shape.Box(cx, cy + (hemY + 0.06f) * u, 0.75f * u, 0.07f * u), main, cx);
                    // Col roulé.
                    var roll = Shape.Box(cx, cy + 0.6f * u, 0.27f * u, 0.17f * u, 0.12f * u);
                    Toon.Part(img, roll, main.Shade(0.95f), 0.6f * u, Body);
                    Ribs(img, roll, roll, main, cx);
                    Folds(img, cloth, main, cx, cy);
                    Badge(img, o.Emblem, cx, cy - 0.15f * u, 0.25f * u, accent, main);
                    break;
                }
                case Top.Hoodie:
                {
                    cloth = Shape.Subtract(cloth, Shape.Ellipse(cx, cy + 0.68f * u, 0.28f * u, 0.22f * u));
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    Ribs(img, cloth, Shape.Box(cx, cy + (hemY + 0.06f) * u, 0.75f * u, 0.07f * u), main, cx);
                    // Capuche tombée autour du cou.
                    var hood = Shape.Subtract(Shape.Ellipse(cx, cy + 0.6f * u, 0.5f * u, 0.3f * u), Shape.Ellipse(cx, cy + 0.68f * u, 0.29f * u, 0.22f * u));
                    Toon.Part(img, hood, main.Shade(0.9f), 0.8f * u, Body);
                    img.FillClipped(hood, Shape.Arc(cx, cy + 0.66f * u, 0.36f * u, 200f, 340f, 0.02f * u), main.Outline().WithAlpha(0.5f), 1.5f);
                    // Poche kangourou.
                    var pocket = Shape.Box(cx, cy - 0.58f * u, 0.4f * u, 0.17f * u, 0.1f * u);
                    img.FillClipped(cloth, pocket, main.Shade(0.92f), 1.5f);
                    img.FillClipped(cloth, Shape.Subtract(pocket.Grow(0.015f * u), pocket), main.Outline().WithAlpha(0.6f), 1f);
                    Folds(img, cloth, main, cx, cy);
                    Badge(img, o.Emblem, cx + 0.36f * u, cy + 0.05f * u, 0.15f * u, accent, main);
                    // Cordons.
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        float x = cx + sgn * 0.13f * u;
                        var cord = Shape.Capsule(x, cy + 0.42f * u, x + sgn * 0.02f * u, cy + 0.02f * u, 0.022f * u);
                        Toon.Part(img, cord, trim, 0.1f * u, Limb);
                        Toon.Part(img, Shape.Capsule(x + sgn * 0.02f * u, cy + 0.04f * u, x + sgn * 0.02f * u, cy - 0.04f * u, 0.03f * u), Metal, 0.1f * u, Limb);
                    }
                    break;
                }
                case Top.Shirt:
                {
                    var skinV = Shape.Poly(cx - 0.15f * u, cy + 0.78f * u, cx + 0.15f * u, cy + 0.78f * u, cx, cy + 0.38f * u);
                    cloth = Shape.Subtract(cloth, skinV);
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    // Patte de boutonnage et boutons.
                    img.FillClipped(cloth, Shape.Capsule(cx, cy + 0.38f * u, cx, cy + (hemY + 0.02f) * u, 0.012f * u), main.Outline().WithAlpha(0.6f), 1f);
                    for (int i = 0; i < 5; i++)
                        Toon.Part(img, Shape.Circle(cx + 0.04f * u, cy + (0.25f - i * 0.26f) * u, 0.032f * u), Rgba.FromHex("#F8F9FA"), 0.1f * u, Limb);
                    // Col à pointes.
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        var collar = Shape.Poly(cx + sgn * 0.15f * u, cy + 0.8f * u, cx + sgn * 0.36f * u, cy + 0.7f * u, cx + sgn * 0.26f * u, cy + 0.44f * u, cx + sgn * 0.05f * u, cy + 0.42f * u).Grow(0.02f * u);
                        Toon.Part(img, collar, main.Light(1.08f), 0.5f * u, new Toon.Style { Outline = 0.05f, Shade = 0.18f, Soft = 0.2f, Rim = 0f, Specular = false });
                    }
                    // Poche poitrine.
                    float px = cx + 0.38f * u, py = cy + 0.0f * u;
                    var pocket = Shape.Box(px, py, 0.14f * u, 0.15f * u, 0.03f * u);
                    img.FillClipped(cloth, Shape.Subtract(pocket.Grow(0.014f * u), pocket), main.Outline().WithAlpha(0.55f), 1f);
                    img.FillClipped(cloth, Shape.Box(px, py + 0.1f * u, 0.15f * u, 0.012f * u), main.Outline().WithAlpha(0.55f), 1f);
                    Folds(img, cloth, main, cx, cy);
                    break;
                }
                case Top.Jacket:
                {
                    // T-shirt dessous (couleur d'accent), col rond.
                    var tee = Shape.Subtract(Shape.Poly(cx - 0.34f * u, cy + 0.72f * u, cx + 0.34f * u, cy + 0.72f * u, cx + 0.3f * u, cy + hemY * u, cx - 0.3f * u, cy + hemY * u),
                        Shape.Ellipse(cx, cy + 0.7f * u, 0.27f * u, 0.22f * u));
                    Toon.Part(img, tee, accent, u, Body);
                    img.FillClipped(tee, Shape.Subtract(Shape.Ellipse(cx, cy + 0.7f * u, 0.32f * u, 0.27f * u), Shape.Ellipse(cx, cy + 0.7f * u, 0.27f * u, 0.22f * u)), accent.Shade(0.85f), 1f);
                    // Blouson ouvert.
                    var opening = Shape.Poly(cx - 0.24f * u, cy + 0.9f * u, cx + 0.24f * u, cy + 0.9f * u, cx + 0.2f * u, cy - 1.1f * u, cx - 0.2f * u, cy - 1.1f * u);
                    var jacket = Shape.Subtract(cloth, opening);
                    Toon.Part(img, jacket, main, 2 * u, Body);
                    ApplyMotif(img, jacket, o.Motif, main, accent, false);
                    // Fermeture éclair le long des bords, col montant.
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        img.FillClipped(jacket, Shape.Capsule(cx + sgn * 0.235f * u, cy + 0.62f * u, cx + sgn * 0.205f * u, cy + (hemY - 0.05f) * u, 0.018f * u), Metal, 1f);
                        var collar = Shape.Poly(cx + sgn * 0.24f * u, cy + 0.6f * u, cx + sgn * 0.5f * u, cy + 0.72f * u, cx + sgn * 0.44f * u, cy + 0.92f * u, cx + sgn * 0.2f * u, cy + 0.82f * u).Grow(0.02f * u);
                        Toon.Part(img, collar, main.Shade(0.9f), 0.5f * u, Body);
                        img.FillClipped(jacket, Shape.Box(cx + sgn * 0.44f * u, cy - 0.5f * u, 0.15f * u, 0.016f * u, 0.006f * u, sgn * 25f), main.Outline().WithAlpha(0.7f), 1f);
                    }
                    Ribs(img, jacket, Shape.Box(cx, cy + (hemY + 0.06f) * u, 0.75f * u, 0.07f * u), main, cx);
                    Folds(img, jacket, main, cx, cy);
                    break;
                }
                case Top.Robe:
                {
                    // Robe croisée : encolure en V bordée, ceinture d'étoffe.
                    var skinV = Shape.Poly(cx - 0.2f * u, cy + 0.82f * u, cx + 0.2f * u, cy + 0.82f * u, cx, cy + 0.3f * u);
                    cloth = Shape.Subtract(cloth, skinV);
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    ApplyMotif(img, cloth, o.Motif, main, accent, false);
                    var border = Shape.Union(
                        Shape.Capsule(cx - 0.2f * u, cy + 0.82f * u, cx + 0.02f * u, cy + 0.3f * u, 0.06f * u),
                        Shape.Capsule(cx + 0.2f * u, cy + 0.82f * u, cx - 0.02f * u, cy + 0.3f * u, 0.06f * u),
                        Shape.Capsule(cx + 0.02f * u, cy + 0.3f * u, cx + 0.1f * u, cy + (hemY - 0.1f) * u, 0.06f * u));
                    img.FillClipped(cloth, border, trim, 1f);
                    var sash = Shape.Box(cx, cy - 0.56f * u, 0.72f * u, 0.08f * u, 0.03f * u);
                    img.FillClipped(cloth, sash, trim.Shade(0.9f), 1f);
                    Toon.Part(img, Shape.Ellipse(cx - 0.3f * u, cy - 0.56f * u, 0.1f * u, 0.08f * u), trim.Shade(0.9f), 0.2f * u, Limb);
                    Toon.Part(img, Shape.Capsule(cx - 0.32f * u, cy - 0.6f * u, cx - 0.38f * u, cy - 0.84f * u, 0.04f * u), trim.Shade(0.9f), 0.2f * u, Limb);
                    Folds(img, cloth, main, cx, cy);
                    Badge(img, o.Emblem, cx - 0.36f * u, cy - 0.05f * u, 0.17f * u, accent, main);
                    break;
                }
                case Top.Space:
                {
                    Toon.Part(img, cloth, main, 2 * u, Body);
                    // Coutures de la combinaison.
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                        img.FillClipped(cloth, Shape.Capsule(cx + sgn * 0.5f * u, cy + 0.45f * u, cx + sgn * 0.48f * u, cy + hemY * u, 0.012f * u), main.Outline().WithAlpha(0.35f), 1f);
                    // Anneau du col (attache du casque).
                    var ring = Shape.Subtract(Shape.Ellipse(cx, cy + 0.62f * u, 0.44f * u, 0.22f * u), Shape.Ellipse(cx, cy + 0.66f * u, 0.3f * u, 0.14f * u));
                    Toon.Part(img, ring, Metal, 0.6f * u, new Toon.Style { Outline = 0.05f, Shade = 0.2f, Soft = 0.15f, Rim = 0.08f, RimOpacity = 0.6f, Specular = true, SpecSize = 0.1f });
                    // Boîtier de commande sur la poitrine.
                    var panel = Shape.Box(cx, cy - 0.28f * u, 0.3f * u, 0.19f * u, 0.05f * u);
                    Toon.Part(img, panel, Ink, 0.6f * u, new Toon.Style { Outline = 0.05f, Shade = 0.15f, Soft = 0.2f, Rim = 0.05f, RimOpacity = 0.4f, Specular = false });
                    string[] lights = { o.Accent, "#FFD60A", "#06D6A0" };
                    for (int i = 0; i < 3; i++)
                        Toon.Part(img, Shape.Circle(cx + (i - 1) * 0.16f * u, cy - 0.22f * u, 0.045f * u), Rgba.FromHex(lights[i]), 0.1f * u, Limb);
                    img.Fill(Shape.Box(cx, cy - 0.38f * u, 0.2f * u, 0.025f * u, 0.01f * u), Rgba.FromHex("#4CC9F0"));
                    // Ceinture technique.
                    img.FillClipped(cloth, Shape.Box(cx, cy - 0.78f * u, 0.8f * u, 0.07f * u), accent.Shade(0.8f), 1f);
                    Badge(img, o.Emblem, cx + 0.42f * u, cy + 0.12f * u, 0.14f * u, accent, main);
                    break;
                }
            }

            if (o.Belt) LifterBelt(img, cx, cy - 0.84f * u);
            part.PivotY = cy;
            return part;
        }

        /// <summary>Plis du tissu sur la poitrine.</summary>
        static void Folds(Raster img, Shape clip, Rgba main, float cx, float cy)
        {
            float u = U;
            var c = main.Shadow(0.7f).WithAlpha(0.5f);
            img.FillClipped(clip, Shape.Arc(cx - 0.36f * u, cy - 0.1f * u, 0.32f * u, 200f, 300f, 0.022f * u), c, 2f);
            img.FillClipped(clip, Shape.Arc(cx + 0.36f * u, cy - 0.1f * u, 0.32f * u, 240f, 340f, 0.022f * u), c, 2f);
        }

        /// <summary>Bord côtelé (bas du pull, col roulé, poignets) : fines lignes verticales.</summary>
        static void Ribs(Raster img, Shape clip, Shape band, Rgba main, float cx)
        {
            float u = U;
            var region = Shape.Intersect(clip, band);
            img.FillClipped(clip, band, main.Shade(0.9f), 1f);
            for (int i = -14; i <= 14; i++)
                img.FillClipped(region, Shape.Box(cx + i * 0.06f * u, img.Height * 0.5f, 0.008f * u, img.Height), main.Outline().WithAlpha(0.35f), 1f);
        }

        /// <summary>Avant-bras à manche longue, poignet selon le vêtement.</summary>
        static Part LongForearm(Top top, Rgba main, Rgba accent, Motif motif) =>
            Segment(MascotRig.Forearm, 0.21f, 0.17f, main, (img, limb, ax, bx) =>
            {
                float u = U;
                ApplyMotif(img, limb, motif, main, accent, true);
                float x0 = ax + (bx - ax) * 0.84f;
                var cuff = Shape.Intersect(limb.Grow(0.015f * u), Shape.HalfPlane(-1, 0, -x0));
                switch (top)
                {
                    case Top.Shirt:
                        Toon.Part(img, cuff, main.Light(1.08f), 0.4f * u, Limb);
                        img.FillClipped(cuff, Shape.Circle(x0 + 0.07f * u, img.Height * 0.5f - 0.05f * u, 0.022f * u), Rgba.FromHex("#F8F9FA"), 1f);
                        break;
                    case Top.Robe:
                        // Manche évasée bordée.
                        var flare = Shape.Intersect(Shape.Tapered(x0 - 0.1f * u, img.Height * 0.5f, 0.16f * u, bx + 0.08f * u, img.Height * 0.5f, 0.24f * u), Shape.HalfPlane(-1, 0, -x0));
                        Toon.Part(img, flare, Contrast(accent, main), 0.4f * u, Limb);
                        break;
                    case Top.Space:
                        Toon.Part(img, cuff, accent, 0.4f * u, Limb);
                        img.FillClipped(cuff, Shape.Box(x0 + 0.04f * u, img.Height * 0.5f, 0.012f * u, img.Height), Metal, 1f);
                        break;
                    default:
                        Toon.Part(img, cuff, main.Shade(0.9f), 0.4f * u, Limb);
                        for (int i = 0; i < 4; i++)
                            img.FillClipped(cuff, Shape.Box(img.Width * 0.5f, img.Height * 0.5f + (i - 1.5f) * 0.06f * u, img.Width, 0.008f * u), main.Outline().WithAlpha(0.35f), 1f);
                        break;
                }
            }, 0);

        // ================================================================== Bas

        static Shape TrouserShape(float cx, float cy)
        {
            float u = U;
            var waist = Shape.Box(cx, cy + 0.2f * u, 0.66f * u, 0.22f * u, 0.1f * u);
            var legL = Shape.Box(cx - 0.36f * u, cy - 0.12f * u, 0.33f * u, 0.32f * u, 0.1f * u, -8f);
            var legR = Shape.Box(cx + 0.36f * u, cy - 0.12f * u, 0.33f * u, 0.32f * u, 0.1f * u, 8f);
            return Shape.Smooth(waist, Shape.Union(legL, legR), 0.15f * u);
        }

        /// <summary>Haut du bas (à la place du short) : short, pantalon, jean ou jogging.</summary>
        static Part Hips(Bottom kind, Rgba color, Rgba accent, Rgba pin)
        {
            if (kind == Bottom.Shorts) return Shorts(color);
            if (pin.a > 0) return SuitHips(color, pin);
            var part = Canvas(1.9f, 1.2f, out var img, out float cx, out float cy);
            float u = U;
            var pants = TrouserShape(cx, cy);
            Toon.Part(img, pants, color, 1.4f * u, Body);
            img.FillClipped(pants, Shape.Capsule(cx, cy - 0.05f * u, cx, cy - 0.45f * u, 0.025f * u), color.Outline(), 2f);
            switch (kind)
            {
                case Bottom.Trousers:
                    // Ceinture fine et boucle.
                    img.FillClipped(pants, Shape.Box(cx, cy + 0.36f * u, 0.7f * u, 0.05f * u), Rgba.FromHex("#3A2A20"), 1f);
                    Toon.Part(img, Shape.Subtract(Shape.Box(cx, cy + 0.36f * u, 0.07f * u, 0.06f * u, 0.015f * u), Shape.Box(cx, cy + 0.36f * u, 0.035f * u, 0.025f * u)), Metal, 0.2f * u, Limb);
                    break;
                case Bottom.Jeans:
                    img.FillClipped(pants, Shape.Box(cx, cy + 0.3f * u, 0.7f * u, 0.006f * u), Stitch, 1f);
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                        img.FillClipped(pants, Shape.Arc(cx + sgn * 0.62f * u, cy + 0.4f * u, 0.26f * u, sgn < 0 ? 270f : 180f, sgn < 0 ? 360f : 270f, 0.008f * u), Stitch, 1f);
                    img.FillClipped(pants, Shape.Arc(cx + 0.06f * u, cy + 0.06f * u, 0.12f * u, 180f, 300f, 0.008f * u), Stitch, 1f);
                    Toon.Part(img, Shape.Circle(cx, cy + 0.36f * u, 0.04f * u), Brass, 0.12f * u, Limb);
                    break;
                case Bottom.Joggers:
                    var band = Shape.Box(cx, cy + 0.34f * u, 0.7f * u, 0.08f * u);
                    img.FillClipped(pants, band, color.Shade(0.88f), 1f);
                    for (int i = -11; i <= 11; i++)
                        img.FillClipped(Shape.Intersect(pants, band), Shape.Box(cx + i * 0.06f * u, cy + 0.34f * u, 0.008f * u, 0.1f * u), color.Outline().WithAlpha(0.3f), 1f);
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        Toon.Part(img, Shape.Capsule(cx + sgn * 0.06f * u, cy + 0.3f * u, cx + sgn * 0.09f * u, cy + 0.08f * u, 0.018f * u), Rgba.White, 0.1f * u, Limb);
                        img.FillClipped(pants, Shape.Capsule(cx + sgn * 0.64f * u, cy + 0.26f * u, cx + sgn * 0.74f * u, cy - 0.4f * u, 0.035f * u), Contrast(Rgba.White, color), 1f);
                    }
                    break;
            }
            part.PivotY = cy;
            return part;
        }

        /// <summary>Bas de la robe : jupe évasée qui couvre le haut des cuisses.</summary>
        static Part RobeSkirt(Rgba main, Rgba accent, Motif motif)
        {
            var part = Canvas(1.9f, 1.2f, out var img, out float cx, out float cy);
            float u = U;
            var skirt = Shape.Poly(cx - 0.6f * u, cy + 0.44f * u, cx + 0.6f * u, cy + 0.44f * u, cx + 0.84f * u, cy - 0.48f * u, cx - 0.84f * u, cy - 0.48f * u).Grow(0.06f * u);
            Toon.Part(img, skirt, main, 1.6f * u, Body);
            ApplyMotif(img, skirt, motif, main, accent, false);
            var trim = Contrast(accent, main);
            img.FillClipped(skirt, Shape.Box(cx, cy - 0.5f * u, u, 0.06f * u), trim, 1f);
            img.FillClipped(skirt, Shape.Capsule(cx + 0.1f * u, cy + 0.5f * u, cx + 0.16f * u, cy - 0.6f * u, 0.06f * u), trim, 1f);
            for (int sgn = -1; sgn <= 1; sgn += 2)
                img.FillClipped(skirt, Shape.Capsule(cx + sgn * 0.3f * u, cy + 0.2f * u, cx + sgn * 0.48f * u, cy - 0.45f * u, 0.018f * u), main.Outline().WithAlpha(0.4f), 2f);
            part.PivotY = cy;
            return part;
        }

        /// <summary>Cuisse ou tibia couvert par le pantalon (dessiné à l'horizontale, pivot à l'articulation de départ).</summary>
        static Part LegPart(float length, float ra, float rb, Bottom kind, Rgba color, Rgba pin, bool shin) =>
            Segment(length, ra, rb, color, (img, limb, ax, bx) =>
            {
                float u = U, mid = img.Height * 0.5f;
                Stripes(img, limb, true, pin);
                switch (kind)
                {
                    case Bottom.Jeans:
                        img.FillClipped(limb, Shape.Box(img.Width * 0.5f, mid + rb * 0.55f * u, img.Width, 0.006f * u), Stitch, 1f);
                        if (shin)
                        {
                            var hem = Shape.Intersect(limb.Grow(0.02f * u), Shape.HalfPlane(-1, 0, -(ax + (bx - ax) * 0.88f)));
                            Toon.Part(img, hem, color.Light(1.15f), 0.3f * u, Limb);
                        }
                        break;
                    case Bottom.Joggers:
                        img.FillClipped(limb, Shape.Box(img.Width * 0.5f, mid + rb * 0.7f * u, img.Width, 0.03f * u), Contrast(Rgba.White, color), 1f);
                        if (shin)
                        {
                            var cuff = Shape.Intersect(limb.Grow(0.01f * u), Shape.HalfPlane(-1, 0, -(ax + (bx - ax) * 0.86f)));
                            Toon.Part(img, cuff, color.Shade(0.88f), 0.3f * u, Limb);
                            for (int i = 0; i < 4; i++)
                                img.FillClipped(cuff, Shape.Box(img.Width * 0.5f, mid + (i - 1.5f) * 0.06f * u, img.Width, 0.008f * u), color.Outline().WithAlpha(0.35f), 1f);
                        }
                        break;
                    default:
                        // Pli du pantalon.
                        img.FillClipped(limb, Shape.Box(img.Width * 0.5f, mid + 0.02f * u, img.Width, 0.008f * u), color.Outline().WithAlpha(0.35f), 1f);
                        break;
                }
            }, 0);

        // ================================================================== Chaussures

        static Part ShoePart(Core.Shoe kind, Rgba color)
        {
            switch (kind)
            {
                case Core.Shoe.HighTops: return HighTops(color);
                case Core.Shoe.Boots: return Boots(color);
                case Core.Shoe.Dress: return DressShoes(color);
                default: return Shoe(color);
            }
        }

        /// <summary>Baskets montantes : tige haute, bout blanc, pastille ronde sur la cheville.</summary>
        static Part HighTops(Rgba color)
        {
            var part = Canvas(1.0f, 0.7f, out var img, out float cx, out float cy);
            float u = U;
            var sole = Shape.Box(cx + 0.04f * u, cy - 0.17f * u, 0.4f * u, 0.07f * u, 0.06f * u);
            var upper = Shape.Smooth(Shape.Smooth(Shape.Ellipse(cx + 0.1f * u, cy - 0.06f * u, 0.34f * u, 0.16f * u), Shape.Ellipse(cx - 0.1f * u, cy + 0.04f * u, 0.22f * u, 0.2f * u), 0.12f * u),
                Shape.Box(cx - 0.1f * u, cy + 0.14f * u, 0.19f * u, 0.18f * u, 0.07f * u), 0.08f * u);
            var st = new Toon.Style { Outline = 0.05f, Shade = 0.22f, Soft = 0.18f, Rim = 0.05f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.06f };
            Toon.Part(img, upper, color, 0.8f * u, st);
            img.FillClipped(upper, Shape.Ellipse(cx + 0.36f * u, cy - 0.1f * u, 0.14f * u, 0.12f * u), Rgba.White, 1f);
            Toon.Part(img, sole, Rgba.White, 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.1f, Soft = 0.1f, Rim = 0f, Specular = false });
            img.FillClipped(sole, Shape.Box(cx + 0.04f * u, cy - 0.21f * u, u, 0.02f * u), Rgba.FromHex("#C9CCD6"), 1f);
            for (int i = 0; i < 4; i++)
                img.FillClipped(upper, Shape.Capsule(cx - 0.12f * u + i * 0.07f * u, cy + 0.24f * u - i * 0.07f * u, cx + 0.0f * u + i * 0.07f * u, cy + 0.26f * u - i * 0.07f * u, 0.02f * u), Rgba.White, 1f);
            Toon.Part(img, Shape.Circle(cx - 0.18f * u, cy + 0.12f * u, 0.065f * u), Rgba.White, 0.15f * u, Limb);
            img.Fill(Star(cx - 0.18f * u, cy + 0.12f * u, 0.045f * u, 0.02f * u), color);
            part.PivotX = cx - 0.08f * u; part.PivotY = cy + 0.1f * u;
            return part;
        }

        /// <summary>Bottes en cuir : tige haute, semelle épaisse et talon, crochets dorés.</summary>
        static Part Boots(Rgba color)
        {
            var part = Canvas(1.0f, 0.7f, out var img, out float cx, out float cy);
            float u = U;
            var sole = Shape.Union(Shape.Box(cx + 0.06f * u, cy - 0.18f * u, 0.38f * u, 0.06f * u, 0.04f * u), Shape.Box(cx - 0.22f * u, cy - 0.2f * u, 0.12f * u, 0.07f * u, 0.02f * u));
            var upper = Shape.Smooth(Shape.Ellipse(cx + 0.12f * u, cy - 0.06f * u, 0.34f * u, 0.15f * u),
                Shape.Box(cx - 0.12f * u, cy + 0.1f * u, 0.2f * u, 0.22f * u, 0.05f * u), 0.12f * u);
            Toon.Part(img, upper, color, 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.22f, Soft = 0.18f, Rim = 0.07f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.07f });
            Toon.Part(img, sole, Rgba.FromHex("#2B1D14"), 0.8f * u, new Toon.Style { Outline = 0.05f, Shade = 0.1f, Soft = 0.1f, Rim = 0f, Specular = false });
            img.FillClipped(upper, Shape.Box(cx - 0.12f * u, cy + 0.29f * u, 0.22f * u, 0.035f * u), color.Shade(0.75f), 1f);
            for (int i = 0; i < 3; i++)
                img.Fill(Shape.Circle(cx - 0.02f * u + i * 0.03f * u, cy + 0.2f * u - i * 0.09f * u, 0.022f * u), Brass);
            part.PivotX = cx - 0.08f * u; part.PivotY = cy + 0.1f * u;
            return part;
        }

        // ================================================================== Motifs et écussons

        /// <summary>Couleur d'accent, remplacée par du blanc ou un ton foncé si elle se distingue mal du tissu.</summary>
        static Rgba Contrast(Rgba accent, Rgba main)
        {
            if (Math.Abs(Luma(accent) - Luma(main)) > 0.12f || ColorDistance(accent, main) > 0.3f) return accent;
            return Luma(main) > 0.6f ? main.Shade(0.6f) : Rgba.White;
        }

        static float Luma(Rgba c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
        static float ColorDistance(Rgba a, Rgba b) => (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)) / 765f;

        /// <summary>Motif du tissu : sur le buste à la verticale, sur les membres (dessinés à l'horizontale) dans la longueur.</summary>
        static void ApplyMotif(Raster img, Shape clip, Motif motif, Rgba main, Rgba accent, bool limb)
        {
            if (motif == Motif.None) return;
            float u = U, w = img.Width, h = img.Height;
            var c = Contrast(accent, main);
            switch (motif)
            {
                case Motif.Stripes:
                    // Rayures de marinière : en travers du buste, en travers des membres.
                    for (float p = 0.1f * u; p < (limb ? w : h); p += 0.2f * u)
                        img.FillClipped(clip, limb ? Shape.Box(p, h * 0.5f, 0.035f * u, h) : Shape.Box(w * 0.5f, p, w, 0.035f * u), c, 1f);
                    break;
                case Motif.Pinstripe:
                    Stripes(img, clip, limb, c.WithAlpha(0.34f));
                    break;
                case Motif.Dots:
                {
                    int row = 0;
                    for (float y = 0.1f * u; y < h; y += 0.18f * u, row++)
                        for (float x = (row % 2) * 0.1f * u + 0.05f * u; x < w; x += 0.2f * u)
                            img.FillClipped(clip, Shape.Circle(x, y, 0.035f * u), c, 1f);
                    break;
                }
                case Motif.Stars:
                {
                    int row = 0;
                    for (float y = 0.15f * u; y < h; y += 0.32f * u, row++)
                        for (float x = (row % 2) * 0.17f * u + 0.1f * u; x < w; x += 0.34f * u)
                            img.FillClipped(clip, Star(x, y, 0.065f * u, 0.028f * u), c.WithAlpha(0.9f), 1f);
                    break;
                }
                case Motif.Plaid:
                    for (float p = 0.12f * u; p < Math.Max(w, h); p += 0.3f * u)
                    {
                        img.FillClipped(clip, Shape.Box(w * 0.5f, p, w, 0.05f * u), c.WithAlpha(0.35f), 1f);
                        img.FillClipped(clip, Shape.Box(p, h * 0.5f, 0.05f * u, h), c.WithAlpha(0.35f), 1f);
                        img.FillClipped(clip, Shape.Box(w * 0.5f, p + 0.12f * u, w, 0.008f * u), main.Outline().WithAlpha(0.4f), 1f);
                        img.FillClipped(clip, Shape.Box(p + 0.12f * u, h * 0.5f, 0.008f * u, h), main.Outline().WithAlpha(0.4f), 1f);
                    }
                    break;
            }
        }

        static void Badge(Raster img, Emblem e, float x, float y, float r, Rgba accent, Rgba main)
        {
            if (e != Core.Emblem.None) DrawEmblem(img, e, x, y, r, accent, main);
        }

        /// <summary>Écusson rond (couleur d'accent) avec un symbole de la couleur du tissu.</summary>
        static void DrawEmblem(Raster img, Emblem e, float x, float y, float r, Rgba badge, Rgba symbol)
        {
            badge = Contrast(badge, symbol);
            Toon.Part(img, Shape.Circle(x, y, r), badge, 2 * r, new Toon.Style { Outline = 0.05f, OutlineColor = symbol.Outline(), Shade = 0.15f, Soft = 0.2f, Rim = 0f, Specular = false });
            Shape s;
            switch (e)
            {
                case Core.Emblem.Star: s = Star(x, y, 0.72f * r, 0.3f * r); break;
                case Core.Emblem.Heart:
                    s = Shape.Smooth(Shape.Union(Shape.Circle(x - 0.28f * r, y + 0.14f * r, 0.32f * r), Shape.Circle(x + 0.28f * r, y + 0.14f * r, 0.32f * r)),
                        Shape.Poly(x - 0.56f * r, y + 0.04f * r, x + 0.56f * r, y + 0.04f * r, x, y - 0.6f * r), 0.06f * r);
                    break;
                case Core.Emblem.Bolt:
                    s = Shape.Poly(x + 0.18f * r, y + 0.72f * r, x - 0.42f * r, y - 0.06f * r, x - 0.02f * r, y - 0.06f * r, x - 0.2f * r, y - 0.72f * r, x + 0.42f * r, y + 0.1f * r, x + 0.02f * r, y + 0.1f * r);
                    break;
                case Core.Emblem.Moon: s = Shape.Subtract(Shape.Circle(x, y, 0.6f * r), Shape.Circle(x + 0.32f * r, y + 0.2f * r, 0.5f * r)); break;
                case Core.Emblem.Crown:
                    s = Shape.Poly(x - 0.6f * r, y - 0.42f * r, x + 0.6f * r, y - 0.42f * r, x + 0.62f * r, y + 0.45f * r, x + 0.3f * r, y + 0.05f * r,
                        x, y + 0.56f * r, x - 0.3f * r, y + 0.05f * r, x - 0.62f * r, y + 0.45f * r);
                    break;
                case Core.Emblem.Ring: s = Shape.Subtract(Shape.Circle(x, y, 0.56f * r), Shape.Circle(x, y, 0.32f * r)); break;
                default:
                {
                    // « B » du jeu.
                    float k = r / 0.29f;
                    var stem = Shape.Box(x - 0.07f * k, y, 0.045f * k, 0.17f * k, 0.015f * k);
                    var top = Shape.Subtract(Shape.Box(x, y + 0.08f * k, 0.12f * k, 0.09f * k, 0.08f * k), Shape.Box(x, y + 0.08f * k, 0.045f * k, 0.025f * k, 0.02f * k));
                    var bot = Shape.Subtract(Shape.Box(x + 0.01f * k, y - 0.075f * k, 0.135f * k, 0.095f * k, 0.085f * k), Shape.Box(x + 0.005f * k, y - 0.075f * k, 0.055f * k, 0.03f * k, 0.025f * k));
                    s = Shape.Union(stem, Shape.Intersect(Shape.Union(top, bot), Shape.HalfPlane(-1, 0, -(x - 0.11f * k))));
                    break;
                }
            }
            img.Fill(s, symbol.Shade(0.95f));
        }

        /// <summary>Étoile à 5 branches.</summary>
        static Shape Star(float cx, float cy, float ro, float ri)
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
