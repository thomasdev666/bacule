using System;

namespace Bascule.Core
{
    /// <summary>
    /// Têtes de la mascotte en trois calques :
    /// arrière (cheveux de derrière, oreilles, visage, nez : dépend de la peau),
    /// visage (yeux, sourcils, bouche : commun à toutes les têtes, change selon l'humeur),
    /// avant (cheveux de devant, chapeaux, lunettes…).
    /// </summary>
    public static class HeadArtist
    {
        public static readonly Rgba Ink = Rgba.FromHex("#2B2D42");
        static readonly Rgba Brow = Rgba.FromHex("#3B2A2A");
        static readonly Rgba MouthIn = Rgba.FromHex("#6E1F2B");
        static readonly Rgba Tongue = Rgba.FromHex("#FF6F7F");
        static readonly Rgba Iris = Rgba.FromHex("#6B4226");
        static readonly Rgba Blush = Rgba.FromHex("#FF6F8A");
        static readonly Rgba Sweat = Rgba.FromHex("#8FD3FF");
        static readonly Rgba Gold = Rgba.FromHex("#FFC93C");
        static readonly Rgba Accent = Rgba.FromHex("#FF4F5E");
        static readonly Rgba Teal = Rgba.FromHex("#12B5A6");
        static readonly Rgba Blue = Rgba.FromHex("#3A86FF");
        static readonly Rgba Pink = Rgba.FromHex("#F25CA2");

        /// <summary>Centre et rayon de la tête dans une texture carrée de côté n.</summary>
        public static void Geometry(int n, out float cx, out float cy, out float r)
        {
            cx = n * 0.5f; cy = n * 0.44f; r = n * 0.29f;
        }

        static Toon.Style Line(float outline = 0.03f, float shade = 0.16f, bool spec = false) =>
            new Toon.Style { Outline = outline, Shade = shade, Soft = 0.12f, Rim = 0.04f, RimOpacity = 0.45f, Specular = spec, SpecSize = 0.1f, OutlineColor = null };

        // ================================================================== Calque arrière

        public static Raster Back(HeadDef h, int n)
        {
            var img = new Raster(n, n);
            Geometry(n, out float cx, out float cy, out float r);
            var hair = Rgba.FromHex(h.HairColor);
            var skin = Rgba.FromHex(h.Skin);
            float sz = r * 2f;

            switch (h.Hair)
            {
                case HairStyle.Cascade:
                    CascadeBack(img, cx, cy, r, hair);
                    break;
                case HairStyle.Long:
                    Toon.Part(img, Shape.Smooth(Shape.Box(cx, cy - 0.2f * r, 1.12f * r, 0.95f * r, 0.55f * r), Shape.Circle(cx, cy + 0.2f * r, 1.12f * r), 0.2f * r), hair, sz, Line());
                    break;
                case HairStyle.Afro:
                    Toon.Part(img, Puff(cx, cy + 0.28f * r, 1.36f * r, 14, 0.24f * r), hair, sz, Line());
                    break;
                case HairStyle.Bun:
                    Toon.Part(img, Shape.Circle(cx, cy + 1.22f * r, 0.36f * r), hair, sz * 0.4f, Line(0.07f, 0.2f));
                    break;
                case HairStyle.Pigtails:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Toon.Part(img, Puff(cx + s * 1.12f * r, cy + 0.05f * r, 0.36f * r, 7, 0.1f * r), hair, sz * 0.45f, Line(0.06f, 0.22f));
                        Toon.Part(img, Shape.Circle(cx + s * 0.92f * r, cy + 0.3f * r, 0.1f * r), Pink, sz * 0.15f, Line(0.12f, 0.2f));
                    }
                    break;
            }
            if (h.Accessory == Accessory.Headphones)
                Toon.Part(img, Shape.Arc(cx, cy + 0.08f * r, 1.12f * r, 5f, 175f, 0.16f * r), Ink.Light(1.6f), sz, Line(0.02f, 0.1f));

            // Oreilles.
            for (int s = -1; s <= 1; s += 2)
            {
                var ear = Shape.Ellipse(cx + s * 0.97f * r, cy - 0.02f * r, 0.21f * r, 0.27f * r);
                Toon.Part(img, ear, skin, sz * 0.5f, Line(0.06f, 0.2f));
                img.FillClipped(ear, Shape.Ellipse(cx + s * 0.99f * r, cy - 0.03f * r, 0.1f * r, 0.15f * r), skin.Shadow(0.72f), r * 0.06f);
            }

            // Visage : un peu plus large en bas (joues), pour un air sympathique.
            var face = FaceShape(cx, cy, r);
            Toon.Part(img, face, skin, sz, new Toon.Style { Outline = 0.032f, Shade = 0.2f, Soft = 0.16f, ShadeStrength = 0.82f, Rim = 0.04f, RimOpacity = 0.4f, Specular = false });
            // Ombre portée des cheveux sur le front.
            if (h.Hair != HairStyle.None && h.Hair != HairStyle.Mohawk)
                img.FillClipped(face, Shape.Ellipse(cx, cy + 1.08f * r, 1.05f * r, 0.42f * r), skin.Shadow(0.78f), r * 0.12f, 0.7f);

            // Nez.
            var nose = Shape.Ellipse(cx + 0.03f * r, cy - 0.13f * r, 0.15f * r, 0.12f * r);
            img.Fill(Shape.Ellipse(cx + 0.06f * r, cy - 0.2f * r, 0.13f * r, 0.06f * r), skin.Shadow(0.7f).WithAlpha(0.6f), r * 0.06f);
            Toon.Part(img, nose, skin.Shade(1.03f), sz * 0.3f, new Toon.Style { Outline = 0f, Shade = 0.35f, Soft = 0.3f, ShadeStrength = 0.82f, Rim = 0f, SpecSize = 0.12f });

            if (h.Accessory == Accessory.Freckles)
                foreach (var (fx, fy) in new[] { (-0.6f, -0.12f), (-0.48f, -0.24f), (-0.7f, -0.28f), (0.6f, -0.12f), (0.48f, -0.24f), (0.7f, -0.28f) })
                    img.Fill(Shape.Circle(cx + fx * r, cy + fy * r, 0.035f * r), Rgba.FromHex("#B5653A").WithAlpha(0.8f));
            return img;
        }

        /// <summary>
        /// Rougeur de l'effort : le visage entier en rouge, plus dense sur les joues et le front.
        /// Posée sur la tête avec une opacité qui monte avec le score et la charge.
        /// </summary>
        public static Raster Flush(int n)
        {
            var img = new Raster(n, n);
            Geometry(n, out float cx, out float cy, out float r);
            var face = FaceShape(cx, cy, r);
            var red = Rgba.FromHex("#FF2E2E");
            img.Fill(face, Paint.Radial(red.WithAlpha(0.75f), cx, cy - 0.15f * r, red.WithAlpha(0.95f), r * 1.1f));
            img.FillClipped(face, Shape.Ellipse(cx - 0.6f * r, cy - 0.22f * r, 0.3f * r, 0.2f * r), Rgba.FromHex("#E0102A"), r * 0.25f, 0.6f);
            img.FillClipped(face, Shape.Ellipse(cx + 0.6f * r, cy - 0.22f * r, 0.3f * r, 0.2f * r), Rgba.FromHex("#E0102A"), r * 0.25f, 0.6f);
            return img;
        }

        static Shape FaceShape(float cx, float cy, float r) =>
            Shape.Smooth(Shape.Circle(cx, cy + 0.04f * r, r * 0.98f), Shape.Ellipse(cx, cy - 0.36f * r, 0.84f * r, 0.62f * r), 0.25f * r);

        /// <summary>Forme « nuage » (afro, boucles) : cercle bordé de petits cercles.</summary>

        /// <summary>Étoile à 5 branches, légèrement tournée de <paramref name="rotDeg"/> degrés.</summary>
        static Shape StarShape(float cx, float cy, float ro, float ri, float rotDeg)
        {
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                double a = Math.PI / 2 + i * Math.PI / 5 + rotDeg * Math.PI / 180;
                float rr = i % 2 == 0 ? ro : ri;
                pts[i * 2] = cx + (float)Math.Cos(a) * rr; pts[i * 2 + 1] = cy + (float)Math.Sin(a) * rr;
            }
            return Shape.Poly(pts);
        }
        static Shape Puff(float cx, float cy, float radius, int bumps, float bump)
        {
            var s = Shape.Circle(cx, cy, radius - bump);
            for (int i = 0; i < bumps; i++)
            {
                double a = i * 2 * Math.PI / bumps;
                s = Shape.Union(s, Shape.Circle(cx + (float)Math.Cos(a) * (radius - bump), cy + (float)Math.Sin(a) * (radius - bump), bump * 1.25f));
            }
            return s;
        }

        static readonly Rgba Honey = Rgba.FromHex("#C98B4A");

        /// <summary>
        /// Une vague de cheveux : mèche ovale inclinée, cernée, avec un creux sombre sur le bas et un reflet brillant
        /// sur le haut. Empilées en zigzag, elles forment de grosses ondulations.
        /// </summary>
        static void WaveLock(Raster img, float x, float y, float rx, float ry, float rot, Rgba c)
        {
            var lockShape = Shape.Ellipse(x, y, rx, ry, rot);
            Toon.Part(img, lockShape, c, ry * 2.2f, Line(0.03f, 0.12f));
            img.FillClipped(lockShape, lockShape.Move(0.03f * rx, 0.22f * ry).Not(), c.Shade(0.62f).WithAlpha(0.6f), 0.18f * ry);
            double a = (90 + rot) * Math.PI / 180;
            float hx = x - (float)Math.Cos(a) * 0.05f * ry - 0.28f * rx, hy = y + (float)Math.Sin(a) * 0.32f * ry;
            img.FillClipped(lockShape, Shape.Ellipse(hx, hy, 0.16f * rx, 0.5f * ry, rot), c.Light(1.7f).WithAlpha(0.55f), 0.12f * rx);
        }

        /// <summary>Couleur d'une mèche selon sa hauteur : brun en haut, miel aux pointes (balayage).</summary>
        static Rgba Balayage(Rgba hair, float yRel) => Rgba.Lerp(hair, Honey, (0.35f - yRel) / 1.45f);

        /// <summary>
        /// Coiffure « cascade » (calque arrière) : tout est ramené sur le côté droit (à droite vue de face) en une grosse
        /// tresse qui tombe sur l'épaule, du brun aux pointes miel (balayage), tenue par une bague dorée et finie par
        /// une pointe ondulée ; à gauche, les cheveux sont lissés derrière l'oreille (la créole reste visible).
        /// </summary>
        static void CascadeBack(Raster img, float cx, float cy, float r, Rgba hair)
        {
            float sz = r * 2f;
            // La tresse part derrière l'oreille droite et retombe vers l'avant, sur l'épaule (elle reste visible quand
            // les bras sont levés).
            var flow = Shape.Tapered(cx + 1.0f * r, cy + 0.35f * r, 0.26f * r, cx + 0.86f * r, cy - 1.0f * r, 0.12f * r);
            var s = Shape.Smooth(Shape.Circle(cx + 0.04f * r, cy + 0.2f * r, 1.08f * r), flow, 0.2f * r);
            s = Shape.Union(s, Shape.Ellipse(cx - 0.82f * r, cy + 0.2f * r, 0.32f * r, 0.5f * r, -15f));
            Toon.Part(img, s, hair, sz, Line());
            img.FillClipped(s, s, Paint.Linear(Honey.WithAlpha(0f), 0, cy - 0.1f * r, Honey.WithAlpha(0.9f), 0, cy - 1.1f * r), 1f);
            // Pointe ondulée sous la bague.
            float tx = cx + 0.8f * r, ty = cy - 1.3f * r;
            WaveLock(img, tx - 0.05f * r, ty - 0.1f * r, 0.08f * r, 0.09f * r, 10f, Balayage(hair, -1.4f));
            WaveLock(img, tx, ty, 0.12f * r, 0.13f * r, -30f, Balayage(hair, -1.3f));
            // La tresse : des maillons inclinés une fois à gauche, une fois à droite, du bas vers le haut
            // (chaque maillon passe devant celui du dessous), de plus en plus fins vers la pointe.
            const int links = 5;
            for (int i = links - 1; i >= 0; i--)
            {
                float t = i / (float)(links - 1);
                int side = i % 2 == 0 ? 1 : -1;
                float x = 1.14f - 0.34f * (float)Math.Pow(t, 1.3) + side * 0.06f, y = 0.3f - 1.26f * t;
                WaveLock(img, cx + x * r, cy + y * r, (0.27f - 0.05f * t) * r, (0.3f - 0.04f * t) * r, side * 28f - 15f * t, Balayage(hair, y));
            }
            // Bague dorée qui tient la tresse.
            var cuff = Shape.Box(tx + 0.02f * r, cy - 1.17f * r, 0.12f * r, 0.065f * r, 0.03f * r, -15f);
            Toon.Part(img, cuff, Gold, 0.4f * r, new Toon.Style { Outline = 0.05f, Shade = 0.15f, Soft = 0.12f, Rim = 0.08f, RimOpacity = 0.7f, Specular = true, SpecSize = 0.15f });
            img.FillClipped(cuff, Shape.Box(tx + 0.02f * r, cy - 1.17f * r, 0.13f * r, 0.012f * r, 0f, -15f), Gold.Shade(0.75f), 1f);
        }

        // ================================================================== Calque visage

        enum EyeKind { Open, Wide, Half, Worried, HappyArc, Closed, Squeeze }

        public static Raster Face(Expression e, int n)
        {
            var img = new Raster(n, n);
            Geometry(n, out float cx, out float cy, out float r);
            float ex = 0.36f * r, ey = cy + 0.12f * r, my = cy - 0.43f * r;

            void Eyes(EyeKind left, EyeKind right, float lookX = 0f, float lookY = 0.035f)
            {
                Eye(img, cx - ex, ey, r, left, lookX, lookY, -1);
                Eye(img, cx + ex, ey, r, right, lookX, lookY, 1);
            }
            void Brows(float lift, float tilt, float tiltR = float.NaN)
            {
                // tilt > 0 : intérieur relevé (inquiet) ; tilt < 0 : froncé.
                if (float.IsNaN(tiltR)) tiltR = tilt;
                BrowAt(img, cx - ex, ey + (0.38f + lift) * r, r, tilt, -1);
                BrowAt(img, cx + ex, ey + (0.38f + lift) * r, r, tiltR, 1);
            }
            void Cheeks(float strength = 0.4f)
            {
                img.Fill(Shape.Ellipse(cx - 0.6f * r, cy - 0.22f * r, 0.17f * r, 0.11f * r), Blush.WithAlpha(strength), r * 0.14f);
                img.Fill(Shape.Ellipse(cx + 0.6f * r, cy - 0.22f * r, 0.17f * r, 0.11f * r), Blush.WithAlpha(strength), r * 0.14f);
            }

            switch (e)
            {
                case Expression.Calm:
                    Eyes(EyeKind.Open, EyeKind.Open); Brows(0.02f, 0.1f); Cheeks(0.35f);
                    OpenMouth(img, cx, my + 0.04f * r, 0.26f * r, 0.17f * r, r, false, true);
                    break;
                case Expression.Grin:
                    Eyes(EyeKind.Open, EyeKind.Open); Brows(0.08f, 0.15f); Cheeks(0.45f);
                    OpenMouth(img, cx, my, 0.4f * r, 0.27f * r, r, true, true);
                    break;
                case Expression.Wink:
                    Eyes(EyeKind.Open, EyeKind.HappyArc); Brows(0.04f, 0.05f, -0.1f); Cheeks(0.45f);
                    OpenMouth(img, cx + 0.04f * r, my + 0.03f * r, 0.3f * r, 0.19f * r, r, true, true);
                    break;
                case Expression.Surprised:
                    Eyes(EyeKind.Wide, EyeKind.Wide, 0, 0.04f); Brows(0.2f, 0.35f);
                    Toon.Part(img, Shape.Ellipse(cx, my - 0.02f * r, 0.13f * r, 0.17f * r), MouthIn, r * 0.6f, new Toon.Style { Outline = 0.05f, OutlineColor = Ink, Shade = 0.1f, Rim = 0, Specular = false });
                    break;
                case Expression.Tense:
                    Eyes(EyeKind.Worried, EyeKind.Worried, 0, 0.07f); Brows(0.04f, 0.55f);
                    WavyMouth(img, cx, my + 0.06f * r, r);
                    Drop(img, cx + 0.86f * r, cy + 0.55f * r, r, 1f);
                    break;
                case Expression.Grimace:
                    Eyes(EyeKind.Squeeze, EyeKind.Squeeze); Brows(-0.04f, -0.5f); Cheeks(0.55f);
                    Teeth(img, cx, my + 0.02f * r, 0.36f * r, 0.17f * r, r);
                    Drop(img, cx + 0.88f * r, cy + 0.5f * r, r, 1.1f);
                    Drop(img, cx - 0.9f * r, cy + 0.3f * r, r, 0.8f);
                    break;
                case Expression.Effort:
                    Eyes(EyeKind.Closed, EyeKind.Closed); Brows(-0.06f, -0.7f); Cheeks(0.75f);
                    Teeth(img, cx, my + 0.02f * r, 0.3f * r, 0.14f * r, r);
                    Drop(img, cx + 0.9f * r, cy + 0.58f * r, r, 1.2f);
                    Drop(img, cx - 0.86f * r, cy + 0.45f * r, r, 0.9f);
                    // Veine de l'effort sur le front.
                    img.Fill(Shape.Arc(cx + 0.5f * r, cy + 0.68f * r, 0.08f * r, 200f, 340f, 0.04f * r), Accent.WithAlpha(0.8f));
                    img.Fill(Shape.Arc(cx + 0.5f * r, cy + 0.56f * r, 0.08f * r, 20f, 160f, 0.04f * r), Accent.WithAlpha(0.8f));
                    break;
                case Expression.Happy:
                    Eyes(EyeKind.HappyArc, EyeKind.HappyArc); Brows(0.1f, 0.2f); Cheeks(0.6f);
                    OpenMouth(img, cx, my, 0.4f * r, 0.28f * r, r, true, true);
                    break;
                case Expression.Sleepy:
                    Eyes(EyeKind.Half, EyeKind.Half, 0, -0.03f); Brows(-0.02f, 0.05f); Cheeks(0.3f);
                    img.Fill(Shape.Arc(cx, my + 0.2f * r, 0.18f * r, 215f, 325f, 0.06f * r), Ink);
                    break;
                case Expression.Tongue:
                    Eyes(EyeKind.Open, EyeKind.HappyArc); Brows(0.06f, 0.1f); Cheeks(0.45f);
                    img.Fill(Shape.Arc(cx, my + 0.22f * r, 0.3f * r, 210f, 330f, 0.07f * r), Ink);
                    var tongue = Shape.Ellipse(cx + 0.07f * r, my - 0.06f * r, 0.13f * r, 0.17f * r);
                    Toon.Part(img, Shape.Subtract(tongue, Shape.HalfPlane(0, -1, -(my + 0.08f * r))), Tongue, r * 0.5f, new Toon.Style { Outline = 0.06f, OutlineColor = Ink, Shade = 0.2f, Rim = 0, Specular = true, SpecSize = 0.1f });
                    img.Fill(Shape.Capsule(cx + 0.07f * r, my - 0.12f * r, cx + 0.07f * r, my, 0.012f * r), MouthIn.WithAlpha(0.5f));
                    break;
                case Expression.Smirk:
                    Eyes(EyeKind.Half, EyeKind.Open); Brows(0.02f, -0.15f, 0.35f);
                    img.Fill(Shape.Arc(cx + 0.12f * r, my + 0.24f * r, 0.28f * r, 245f, 330f, 0.065f * r), Ink);
                    img.Fill(Shape.Capsule(cx - 0.2f * r, my + 0.02f * r, cx + 0.05f * r, my - 0.04f * r, 0.032f * r), Ink);
                    break;
                case Expression.Sad:
                    Eyes(EyeKind.Worried, EyeKind.Worried, 0, -0.04f); Brows(0.02f, 0.7f);
                    img.Fill(Shape.Arc(cx, my - 0.2f * r, 0.24f * r, 35f, 145f, 0.07f * r), Ink);
                    Drop(img, cx - 0.42f * r, cy - 0.12f * r, r, 0.7f);
                    break;
            }
            return img;
        }

        static void Eye(Raster img, float x, float y, float r, EyeKind kind, float lookX, float lookY, int side)
        {
            float line = 0.045f * r;
            switch (kind)
            {
                case EyeKind.HappyArc:
                    img.Fill(Shape.Arc(x, y - 0.07f * r, 0.15f * r, 20f, 160f, line * 1.5f), Ink);
                    return;
                case EyeKind.Closed:
                    img.Fill(Shape.Arc(x, y + 0.08f * r, 0.15f * r, 205f, 335f, line * 1.5f), Ink);
                    img.Fill(Shape.Capsule(x - side * 0.2f * r, y - 0.07f * r, x - side * 0.1f * r, y - 0.02f * r, line * 0.5f), Ink);
                    return;
                case EyeKind.Squeeze:
                {
                    float d = -side; // les pointes vers le nez
                    img.Fill(Shape.Capsule(x - d * 0.16f * r, y + 0.11f * r, x + d * 0.12f * r, y, line * 0.8f), Ink);
                    img.Fill(Shape.Capsule(x - d * 0.16f * r, y - 0.11f * r, x + d * 0.12f * r, y, line * 0.8f), Ink);
                    return;
                }
            }

            float rx = 0.2f * r, ry = 0.26f * r;
            if (kind == EyeKind.Wide) { rx *= 1.18f; ry *= 1.15f; }
            Shape sclera = Shape.Ellipse(x, y, rx, ry);
            if (kind == EyeKind.Half) sclera = Shape.Intersect(sclera, Shape.HalfPlane(0, 1, y + 0.02f * r));
            if (kind == EyeKind.Worried) sclera = Shape.Intersect(sclera, Shape.HalfPlane(side * 0.45f, 1, (side * 0.45f) * x + y + 0.1f * r));

            img.Fill(sclera.Grow(line), Ink);
            img.Fill(sclera, Paint.Linear(Rgba.FromHex("#E4E7F2"), 0, y + ry, Rgba.White, 0, y - ry * 0.2f));
            float ix = x + lookX * r, iy = y + lookY * r - (kind == EyeKind.Half ? 0.06f * r : 0f);
            float ir = (kind == EyeKind.Wide ? 0.1f : 0.14f) * r;
            var iris = Shape.Circle(ix, iy, ir);
            img.FillClipped(sclera, iris, Paint.Radial(Iris.Light(1.4f), ix, iy - ir * 0.5f, Iris, ir * 1.2f), 1f);
            img.FillClipped(sclera, Shape.Circle(ix, iy, ir * 0.55f), Ink, 1f);
            // Ombre de la paupière en haut de l'œil.
            img.FillClipped(sclera, sclera.Move(0, -0.045f * r).Not(), Rgba.FromHex("#9AA0C0").WithAlpha(0.35f), 0.05f * r);
            img.FillClipped(sclera, Shape.Circle(ix + ir * 0.35f, iy + ir * 0.4f, ir * 0.32f), Rgba.White, 1f);
            img.FillClipped(sclera, Shape.Circle(ix - ir * 0.35f, iy - ir * 0.4f, ir * 0.14f), Rgba.White.WithAlpha(0.85f), 1f);
            if (kind == EyeKind.Half || kind == EyeKind.Worried)
            {
                // Trait de paupière épais.
                var lidLine = Shape.Subtract(sclera.Grow(line * 1.6f), sclera.Move(0, -line * 1.4f).Grow(line * 0.2f));
                img.Fill(Shape.Intersect(lidLine, Shape.HalfPlane(0, -1, -(y - 0.04f * r))), Ink);
            }
        }

        static void BrowAt(Raster img, float x, float y, float r, float tilt, int side)
        {
            // L'extrémité intérieure (vers le nez) monte si tilt > 0.
            float inner = x - side * 0.15f * r, outer = x + side * 0.15f * r;
            var brow = Shape.Tapered(inner, y + tilt * 0.1f * r, 0.055f * r, outer, y - tilt * 0.1f * r + 0.02f * r, 0.04f * r);
            img.Fill(brow, Brow);
        }

        static void OpenMouth(Raster img, float cx, float cy, float w, float h, float r, bool tongue, bool teeth)
        {
            var mouth = Shape.Intersect(Shape.Ellipse(cx, cy, w, h), Shape.HalfPlane(0, 1, cy + h * 0.25f));
            mouth = Shape.Smooth(mouth, Shape.Box(cx, cy + h * 0.2f, w * 0.96f, h * 0.06f, h * 0.06f), h * 0.2f);
            img.Fill(mouth.Grow(0.045f * r), Ink);
            img.Fill(mouth, Paint.Linear(MouthIn.Shade(0.8f), 0, cy + h, MouthIn, 0, cy - h));
            if (teeth) img.FillClipped(mouth, Shape.Box(cx, cy + h * 0.28f, w * 0.7f, h * 0.17f, h * 0.06f), Rgba.White, 1f);
            if (tongue)
            {
                var t = Shape.Ellipse(cx + w * 0.1f, cy - h * 0.75f, w * 0.55f, h * 0.5f);
                img.FillClipped(mouth, t, Paint.Linear(Tongue.Light(1.15f), 0, cy - h * 0.3f, Tongue.Shade(0.88f), 0, cy - h), 1f);
            }
        }

        static void Teeth(Raster img, float cx, float cy, float w, float h, float r)
        {
            var box = Shape.Box(cx, cy, w, h, h * 0.55f);
            img.Fill(box.Grow(0.045f * r), Ink);
            img.Fill(box, Paint.Linear(Rgba.White, 0, cy + h, Rgba.FromHex("#DDE1EE"), 0, cy - h));
            img.FillClipped(box, Shape.Box(cx, cy, w, h * 0.06f), MouthIn, 1f);
            for (int i = -2; i <= 2; i++)
                img.FillClipped(box, Shape.Box(cx + i * w * 0.36f, cy, 0.012f * r, h), Rgba.FromHex("#A9AEC6"), 1f);
        }

        static void WavyMouth(Raster img, float cx, float cy, float r)
        {
            float w = 0.24f * r;
            for (int i = 0; i < 4; i++)
            {
                float x0 = cx - w + i * w * 0.5f, x1 = x0 + w * 0.5f;
                float y0 = cy + (i % 2 == 0 ? 0.03f : -0.03f) * r, y1 = cy + (i % 2 == 0 ? -0.03f : 0.03f) * r;
                img.Fill(Shape.Capsule(x0, y0, x1, y1, 0.035f * r), Ink);
            }
        }

        static void Drop(Raster img, float x, float y, float r, float s)
        {
            var drop = Shape.Smooth(Shape.Circle(x, y, 0.1f * r * s), Shape.Poly(x - 0.07f * r * s, y + 0.04f * r * s, x + 0.07f * r * s, y + 0.04f * r * s, x, y + 0.24f * r * s), 0.04f * r * s);
            Toon.Part(img, drop, Sweat, 0.5f * r * s, new Toon.Style { Outline = 0.06f, OutlineColor = Rgba.FromHex("#3D7FB8"), Shade = 0.15f, Rim = 0, Specular = true, SpecSize = 0.12f });
        }

        // ================================================================== Calque avant

        public static Raster Front(HeadDef h, int n)
        {
            var img = new Raster(n, n);
            Geometry(n, out float cx, out float cy, out float r);
            var hair = Rgba.FromHex(h.HairColor);
            float sz = r * 2f;
            float ex = 0.36f * r, ey = cy + 0.12f * r;
            var hairStyle = new Toon.Style { Outline = 0.03f, Shade = 0.14f, Soft = 0.1f, Rim = 0.05f, RimOpacity = 0.6f, Specular = true, SpecSize = 0.09f };

            Shape Cap(float drop = 0f) =>
                Shape.Smooth(Shape.Ellipse(cx, cy + (0.9f - drop) * r, 1.04f * r, 0.42f * r), Shape.Box(cx, cy + (1.02f - drop) * r, 0.88f * r, 0.24f * r, 0.22f * r), 0.2f * r);

            void Fringe(Shape s) => Toon.Part(img, s, hair, sz, hairStyle);

            switch (h.Hair)
            {
                case HairStyle.Short:
                {
                    var s = Cap();
                    for (int i = 0; i < 3; i++)
                        s = Shape.Smooth(s, Shape.Tapered(cx - 0.5f * r + i * 0.36f * r, cy + 0.9f * r, 0.2f * r, cx - 0.58f * r + i * 0.4f * r, cy + 0.6f * r, 0.06f * r), 0.1f * r);
                    s = Shape.Union(s, Shape.Tapered(cx - 0.97f * r, cy + 0.62f * r, 0.13f * r, cx - 0.97f * r, cy + 0.2f * r, 0.07f * r));
                    s = Shape.Union(s, Shape.Tapered(cx + 0.97f * r, cy + 0.62f * r, 0.13f * r, cx + 0.97f * r, cy + 0.2f * r, 0.07f * r));
                    Fringe(s);
                    break;
                }
                case HairStyle.Long:
                case HairStyle.Pigtails:
                case HairStyle.Bun:
                {
                    var s = Shape.Smooth(Cap(0.02f), Shape.Tapered(cx + 0.15f * r, cy + 1.0f * r, 0.3f * r, cx + 0.72f * r, cy + 0.55f * r, 0.1f * r), 0.12f * r);
                    s = Shape.Smooth(s, Shape.Tapered(cx - 0.2f * r, cy + 0.98f * r, 0.28f * r, cx - 0.78f * r, cy + 0.58f * r, 0.09f * r), 0.12f * r);
                    Fringe(s);
                    break;
                }
                case HairStyle.Swoosh:
                {
                    var s = Shape.Smooth(Cap(), Shape.Ellipse(cx + 0.3f * r, cy + 1.16f * r, 0.66f * r, 0.28f * r, -12f), 0.15f * r);
                    s = Shape.Smooth(s, Shape.Circle(cx + 0.86f * r, cy + 0.9f * r, 0.22f * r), 0.1f * r);
                    Fringe(s);
                    break;
                }
                case HairStyle.Spiky:
                {
                    var s = Cap();
                    for (int i = 0; i < 7; i++)
                    {
                        double a = (162 - i * 24) * Math.PI / 180;
                        float bx = cx + (float)Math.Cos(a) * 0.8f * r, by = cy + 0.1f * r + (float)Math.Sin(a) * 0.8f * r;
                        float tx = cx + (float)Math.Cos(a) * 1.42f * r, ty = cy + (float)Math.Sin(a) * 1.42f * r;
                        s = Shape.Smooth(s, Shape.Tapered(bx, by, 0.24f * r, tx, ty, 0.02f * r), 0.08f * r);
                    }
                    Fringe(s);
                    break;
                }
                case HairStyle.Curly:
                    Fringe(Shape.Union(Puff(cx, cy + 0.82f * r, 0.95f * r, 11, 0.24f * r), Shape.Ellipse(cx, cy + 0.8f * r, 0.95f * r, 0.36f * r)));
                    break;
                case HairStyle.Curls:
                {
                    // Cheveux courts et bouclés : une calotte près du crâne, bordée de petites boucles serrées,
                    // quelques boucles sur le front, côtés courts.
                    var s = Cap(0.06f);
                    for (int i = 0; i < 9; i++)
                    {
                        double a = (165 - i * 18.75) * Math.PI / 180;
                        s = Shape.Union(s, Shape.Circle(cx + (float)Math.Cos(a) * 0.92f * r, cy + 0.78f * r + (float)Math.Sin(a) * 0.4f * r, 0.12f * r));
                    }
                    for (int i = 0; i < 4; i++)
                        s = Shape.Union(s, Shape.Circle(cx - 0.36f * r + i * 0.24f * r, cy + 0.62f * r - (i == 1 || i == 2 ? 0.03f : 0f) * r, 0.1f * r));
                    s = Shape.Union(s, Shape.Tapered(cx - 0.97f * r, cy + 0.6f * r, 0.1f * r, cx - 0.96f * r, cy + 0.34f * r, 0.05f * r));
                    s = Shape.Union(s, Shape.Tapered(cx + 0.97f * r, cy + 0.6f * r, 0.1f * r, cx + 0.96f * r, cy + 0.34f * r, 0.05f * r));
                    Fringe(s);
                    // Petites boucles en reflets.
                    var shine = hair.Light(2.2f).WithAlpha(0.5f);
                    for (int row = 0; row < 2; row++)
                        for (int i = 0; i < 6 - row; i++)
                        {
                            float x = cx - (0.6f - row * 0.12f) * r + i * 0.24f * r, y = cy + (0.82f + row * 0.17f) * r;
                            img.FillClipped(s, Shape.Arc(x, y, 0.06f * r, 20f + row * 40f + i * 25f, 220f + row * 40f + i * 25f, 0.02f * r), shine, 1f);
                        }
                    break;
                }
                case HairStyle.Mohawk:
                {
                    var s = Shape.Tapered(cx, cy + 0.72f * r, 0.2f * r, cx + 0.05f * r, cy + 1.42f * r, 0.08f * r);
                    for (int i = 0; i < 3; i++)
                        s = Shape.Smooth(s, Shape.Tapered(cx, cy + (0.85f + i * 0.2f) * r, 0.16f * r, cx + 0.32f * r, cy + (1.02f + i * 0.2f) * r, 0.03f * r), 0.06f * r);
                    Fringe(s);
                    break;
                }
                case HairStyle.Afro:
                    Fringe(Shape.Union(Puff(cx, cy + 0.86f * r, 0.92f * r, 10, 0.22f * r), Shape.Ellipse(cx, cy + 0.85f * r, 0.9f * r, 0.32f * r)));
                    break;
                case HairStyle.Cascade:
                {
                    // Raie profonde à gauche : une grande vague brillante balayée sur le front vers la droite, vers la
                    // tresse ; à gauche, une mèche lisse jusqu'à l'oreille.
                    var left = Shape.Smooth(Shape.Ellipse(cx - 0.64f * r, cy + 0.84f * r, 0.36f * r, 0.24f * r, 30f),
                        Shape.Tapered(cx - 0.8f * r, cy + 0.66f * r, 0.15f * r, cx - 0.98f * r, cy + 0.28f * r, 0.07f * r), 0.1f * r);
                    var sweep = Shape.Smooth(Shape.Ellipse(cx + 0.12f * r, cy + 0.94f * r, 0.84f * r, 0.32f * r, -10f),
                        Shape.Tapered(cx + 0.45f * r, cy + 0.86f * r, 0.32f * r, cx + 0.94f * r, cy + 0.44f * r, 0.2f * r), 0.12f * r);
                    var s = Shape.Union(left, sweep);
                    Fringe(s);
                    // Brillance de la vague : un large reflet qui suit le balayage, et le creux de l'ondulation.
                    img.FillClipped(s, Shape.Arc(cx + 0.18f * r, cy + 0.42f * r, 0.74f * r, 38f, 100f, 0.09f * r), hair.Light(1.9f).WithAlpha(0.4f), 3f);
                    img.FillClipped(s, Shape.Arc(cx + 0.1f * r, cy + 0.5f * r, 0.6f * r, 28f, 118f, 0.03f * r), hair.Shade(0.6f).WithAlpha(0.7f), 1.5f);
                    img.FillClipped(s, Shape.Arc(cx - 0.62f * r, cy + 0.84f * r, 0.22f * r, 60f, 150f, 0.03f * r), hair.Light(1.9f).WithAlpha(0.45f), 1.5f);
                    // Une mèche fine qui s'échappe de la tresse et encadre le visage à droite.
                    var strand = Shape.Smooth(Shape.Tapered(cx + 0.84f * r, cy + 0.52f * r, 0.1f * r, cx + 0.99f * r, cy + 0.06f * r, 0.065f * r),
                        Shape.Tapered(cx + 0.99f * r, cy + 0.06f * r, 0.065f * r, cx + 0.9f * r, cy - 0.32f * r, 0.025f * r), 0.04f * r);
                    Toon.Part(img, strand, hair, sz * 0.3f, hairStyle);
                    img.FillClipped(strand, strand, Paint.Linear(Honey.WithAlpha(0f), 0, cy + 0.2f * r, Honey.WithAlpha(0.9f), 0, cy - 0.3f * r), 1f);
                    break;
                }
            }
            // Mèches de lumière.
            if (h.Hair != HairStyle.None && h.Hair != HairStyle.Mohawk)
            {
                img.Fill(Shape.Arc(cx - 0.18f * r, cy + 0.72f * r, 0.48f * r, 105f, 150f, 0.07f * r), hair.Light(1.45f).WithAlpha(0.75f));
                img.Fill(Shape.Arc(cx - 0.08f * r, cy + 0.7f * r, 0.62f * r, 92f, 118f, 0.05f * r), hair.Light(1.45f).WithAlpha(0.6f));
            }

            // Cils recourbés au coin extérieur des yeux (posés sur le bord de l'œil ouvert, et dans le prolongement
            // d'un œil fermé en sourire).
            if (h.Lashes)
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    float lx = cx + sd * ex, ly = ey;
                    foreach (var (a, len, tilt) in new[] { (12f, 0.12f, 28f), (36f, 0.11f, 48f), (60f, 0.09f, 68f) })
                    {
                        double ar = a * Math.PI / 180, tr = tilt * Math.PI / 180;
                        float px = lx + sd * (float)Math.Cos(ar) * 0.2f * r, py = ly + (float)Math.Sin(ar) * 0.26f * r;
                        float qx = px + sd * (float)Math.Cos(tr) * len * r, qy = py + (float)Math.Sin(tr) * len * r;
                        img.Fill(Shape.Tapered(px, py, 0.026f * r, qx, qy, 0.008f * r), Ink);
                    }
                }

            var acc = new Toon.Style { Outline = 0.03f, Shade = 0.15f, Soft = 0.1f, Rim = 0.04f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.08f };
            switch (h.Accessory)
            {
                case Accessory.Glasses:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var lens = Shape.Circle(cx + s * ex, ey, 0.25f * r);
                        img.Fill(lens, Rgba.FromHex("#CFE8FF").WithAlpha(0.25f));
                        img.Fill(Shape.Subtract(lens.Grow(0.06f * r), lens), Ink);
                        img.FillClipped(lens, Shape.Capsule(cx + s * ex - 0.12f * r, ey + 0.02f * r, cx + s * ex - 0.02f * r, ey + 0.14f * r, 0.03f * r), Rgba.White.WithAlpha(0.7f), 1f);
                    }
                    img.Fill(Shape.Arc(cx, ey + 0.02f * r, 0.12f * r, 30f, 150f, 0.05f * r), Ink);
                    break;
                case Accessory.Clover:
                {
                    // Chanceux : un trèfle à quatre feuilles piqué dans les cheveux, au-dessus de l'oreille gauche.
                    float tx = cx - 0.62f * r, ty = cy + 0.86f * r, lr = 0.17f * r;
                    var green = Rgba.FromHex("#52B788");
                    Toon.Part(img, Shape.Capsule(tx, ty, tx - 0.12f * r, ty - 0.34f * r, 0.035f * r), Rgba.FromHex("#2D6A4F"), sz * 0.2f, acc);
                    for (int i = 0; i < 4; i++)
                    {
                        double a = Math.PI / 4 + i * Math.PI / 2;
                        float lx = tx + (float)Math.Cos(a) * lr * 0.95f, ly = ty + (float)Math.Sin(a) * lr * 0.95f;
                        var leaf = Shape.Smooth(Shape.Circle(lx + (float)Math.Cos(a + 0.6) * lr * 0.35f, ly + (float)Math.Sin(a + 0.6) * lr * 0.35f, lr * 0.62f),
                            Shape.Circle(lx + (float)Math.Cos(a - 0.6) * lr * 0.35f, ly + (float)Math.Sin(a - 0.6) * lr * 0.35f, lr * 0.62f), lr * 0.2f);
                        Toon.Part(img, leaf, green, sz * 0.25f, acc);
                    }
                    img.Fill(Shape.Circle(tx, ty, lr * 0.28f), Rgba.FromHex("#95D5B2"));
                    break;
                }
                case Accessory.StarGlasses:
                {
                    // Superstar : lunettes en étoile, monture rose, verres violet nuit, un éclat sur chaque verre.
                    var frame = Rgba.FromHex("#FF4FA3");
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float lx = cx + s * ex, ly = ey + 0.01f * r;
                        var lens = StarShape(lx, ly, 0.33f * r, 0.16f * r, s * 6f);
                        img.Fill(lens.Grow(0.075f * r), Ink);
                        img.Fill(lens.Grow(0.05f * r), frame);
                        img.Fill(lens, Paint.Linear(Rgba.FromHex("#9D4EDD"), 0, ly + 0.3f * r, Rgba.FromHex("#240046"), 0, ly - 0.3f * r));
                        img.FillClipped(lens, Shape.Capsule(lx - 0.1f * r, ly + 0.06f * r, lx - 0.01f * r, ly + 0.15f * r, 0.035f * r), Rgba.White.WithAlpha(0.7f), 1f);
                    }
                    img.Fill(Shape.Arc(cx, ey + 0.04f * r, 0.11f * r, 25f, 155f, 0.055f * r), frame);
                    // Petite étincelle dorée au coin de la monture.
                    float sx = cx + ex + 0.36f * r, sy = ey + 0.3f * r, k = 0.11f * r;
                    img.Fill(Shape.Union(Shape.Poly(sx, sy - k, sx + k * 0.24f, sy, sx, sy + k, sx - k * 0.24f, sy),
                        Shape.Poly(sx - k, sy, sx, sy + k * 0.24f, sx + k, sy, sx, sy - k * 0.24f)), Rgba.FromHex("#FFD60A"), 1f);
                    break;
                }
                case Accessory.Sunglasses:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var lens = Shape.Box(cx + s * ex, ey - 0.01f * r, 0.27f * r, 0.18f * r, 0.13f * r);
                        img.Fill(lens.Grow(0.04f * r), Ink);
                        img.Fill(lens, Paint.Linear(Rgba.FromHex("#4B4F7A"), 0, ey + 0.18f * r, Rgba.FromHex("#14152A"), 0, ey - 0.18f * r));
                        img.FillClipped(lens, Shape.Capsule(cx + s * ex - 0.16f * r, ey + 0.05f * r, cx + s * ex + 0.02f * r, ey + 0.11f * r, 0.035f * r), Rgba.White.WithAlpha(0.55f), 1f);
                    }
                    img.Fill(Shape.Box(cx, ey + 0.08f * r, 0.12f * r, 0.03f * r), Ink);
                    break;
                case Accessory.Cap:
                {
                    var dome = Shape.Intersect(Shape.Ellipse(cx, cy + 0.6f * r, 1.06f * r, 0.75f * r), Shape.HalfPlane(0, -1, -(cy + 0.52f * r)));
                    Toon.Part(img, Shape.Ellipse(cx + 0.72f * r, cy + 0.54f * r, 0.62f * r, 0.13f * r, -5f), Accent.Shade(0.85f), sz, acc);
                    Toon.Part(img, dome, Accent, sz, acc);
                    img.FillClipped(dome, Shape.Box(cx, cy + 1.0f * r, 0.08f * r, 0.5f * r), Rgba.White.WithAlpha(0.9f), 1f);
                    Toon.Part(img, Shape.Circle(cx, cy + 1.35f * r, 0.08f * r), Accent.Shade(0.8f), sz * 0.2f, acc);
                    break;
                }
                case Accessory.Beanie:
                {
                    var hat = Shape.Smooth(Shape.Intersect(Shape.Ellipse(cx, cy + 0.55f * r, 1.06f * r, 0.95f * r), Shape.HalfPlane(0, -1, -(cy + 0.52f * r))), Shape.Box(cx, cy + 0.62f * r, 1.08f * r, 0.12f * r, 0.08f * r), 0.05f * r);
                    Toon.Part(img, hat, Teal, sz, acc);
                    for (int i = -4; i <= 4; i++)
                        img.FillClipped(hat, Shape.Box(cx + i * 0.22f * r, cy + 0.9f * r, 0.025f * r, 0.6f * r), Teal.Shadow(0.82f).WithAlpha(0.6f), 2f);
                    Toon.Part(img, Shape.Box(cx, cy + 0.62f * r, 1.1f * r, 0.15f * r, 0.12f * r), Teal.Shade(0.85f), sz, acc);
                    Toon.Part(img, Puff(cx, cy + 1.52f * r, 0.2f * r, 8, 0.06f * r), Rgba.FromHex("#FFF6E5"), sz * 0.3f, acc);
                    break;
                }
                case Accessory.Crown:
                {
                    var crown = Shape.Poly(cx - 0.62f * r, cy + 0.8f * r, cx + 0.62f * r, cy + 0.8f * r, cx + 0.7f * r, cy + 1.42f * r, cx + 0.35f * r, cy + 1.12f * r,
                        cx, cy + 1.5f * r, cx - 0.35f * r, cy + 1.12f * r, cx - 0.7f * r, cy + 1.42f * r);
                    Toon.Part(img, crown, Gold, sz, acc);
                    Toon.Part(img, Shape.Circle(cx, cy + 0.98f * r, 0.1f * r), Accent, sz * 0.2f, acc);
                    Toon.Part(img, Shape.Circle(cx - 0.4f * r, cy + 0.95f * r, 0.07f * r), Blue, sz * 0.15f, acc);
                    Toon.Part(img, Shape.Circle(cx + 0.4f * r, cy + 0.95f * r, 0.07f * r), Blue, sz * 0.15f, acc);
                    break;
                }
                case Accessory.Headband:
                    Toon.Part(img, Shape.Box(cx, cy + 0.66f * r, 1.0f * r, 0.12f * r, 0.1f * r), Accent, sz, acc);
                    Toon.Part(img, Shape.Ellipse(cx + 1.02f * r, cy + 0.5f * r, 0.1f * r, 0.24f * r, 15f), Accent.Shade(0.85f), sz * 0.3f, acc);
                    img.Fill(Shape.Box(cx, cy + 0.66f * r, 0.98f * r, 0.025f * r), Rgba.White.WithAlpha(0.9f));
                    break;
                case Accessory.Mustache:
                {
                    var m = Shape.Smooth(Shape.Ellipse(cx - 0.18f * r, cy - 0.24f * r, 0.22f * r, 0.1f * r, 12f), Shape.Ellipse(cx + 0.18f * r, cy - 0.24f * r, 0.22f * r, 0.1f * r, -12f), 0.05f * r);
                    Toon.Part(img, m, hair.Shade(0.85f), sz * 0.5f, acc);
                    break;
                }
                case Accessory.ThinMustache:
                {
                    // Fine moustache bien taillée.
                    var m = Shape.Smooth(Shape.Ellipse(cx - 0.14f * r, cy - 0.235f * r, 0.16f * r, 0.042f * r, 10f), Shape.Ellipse(cx + 0.14f * r, cy - 0.235f * r, 0.16f * r, 0.042f * r, -10f), 0.04f * r);
                    img.Fill(m, hair.WithAlpha(0.9f), 0.012f * r);
                    break;
                }
                case Accessory.Bow:
                {
                    float bx = cx + 0.62f * r, by = cy + 0.95f * r;
                    var bow = Shape.Union(Shape.Poly(bx, by, bx - 0.38f * r, by + 0.22f * r, bx - 0.38f * r, by - 0.22f * r).Grow(0.04f * r),
                        Shape.Poly(bx, by, bx + 0.38f * r, by + 0.22f * r, bx + 0.38f * r, by - 0.22f * r).Grow(0.04f * r));
                    Toon.Part(img, bow, Pink, sz * 0.6f, acc);
                    Toon.Part(img, Shape.Circle(bx, by, 0.1f * r), Pink.Shade(0.85f), sz * 0.2f, acc);
                    break;
                }
                case Accessory.Halo:
                {
                    var halo = Shape.Subtract(Shape.Ellipse(cx, cy + 1.36f * r, 0.66f * r, 0.2f * r), Shape.Ellipse(cx, cy + 1.36f * r, 0.5f * r, 0.1f * r));
                    var glow = new Raster(img.Width, img.Height);
                    glow.Fill(halo.Grow(0.08f * r), Gold.WithAlpha(0.7f));
                    glow.Blur((int)(0.06f * r) + 1, 2);
                    img.Composite(glow);
                    Toon.Part(img, halo, Gold, sz * 0.6f, acc);
                    break;
                }
                case Accessory.Headphones:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Toon.Part(img, Shape.Box(cx + s * 1.04f * r, cy, 0.18f * r, 0.3f * r, 0.16f * r), Ink.Light(1.6f), sz * 0.5f, acc);
                        Toon.Part(img, Shape.Box(cx + s * 1.0f * r, cy, 0.09f * r, 0.2f * r, 0.08f * r), Accent, sz * 0.2f, acc);
                    }
                    break;
                case Accessory.Antennas:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Toon.Part(img, Shape.Tapered(cx + s * 0.32f * r, cy + 0.85f * r, 0.06f * r, cx + s * 0.6f * r, cy + 1.42f * r, 0.035f * r), Rgba.FromHex(h.Skin).Shade(0.8f), sz * 0.3f, acc);
                        Toon.Part(img, Shape.Circle(cx + s * 0.62f * r, cy + 1.48f * r, 0.13f * r), Gold, sz * 0.3f, acc);
                    }
                    break;
                case Accessory.TopHat:
                {
                    var brim = Shape.Ellipse(cx, cy + 0.82f * r, 0.95f * r, 0.16f * r, -4f);
                    var tube = Shape.Box(cx + 0.03f * r, cy + 1.28f * r, 0.56f * r, 0.46f * r, 0.08f * r, -4f);
                    Toon.Part(img, brim, Ink.Light(1.25f), sz, acc);
                    Toon.Part(img, tube, Ink.Light(1.25f), sz, acc);
                    img.FillClipped(tube, Shape.Box(cx + 0.01f * r, cy + 0.98f * r, 0.6f * r, 0.1f * r, 0f, -4f), Accent, 1f);
                    break;
                }
                case Accessory.Pirate:
                {
                    var red = Rgba.FromHex("#D7263D");
                    var scarf = Shape.Smooth(Shape.Intersect(Shape.Ellipse(cx, cy + 0.62f * r, 1.05f * r, 0.78f * r), Shape.HalfPlane(0, -1, -(cy + 0.5f * r))),
                        Shape.Tapered(cx + 0.9f * r, cy + 0.72f * r, 0.16f * r, cx + 1.32f * r, cy + 0.3f * r, 0.06f * r), 0.08f * r);
                    Toon.Part(img, scarf, red, sz, acc);
                    for (int i = -3; i <= 3; i++)
                        img.FillClipped(scarf, Shape.Circle(cx + i * 0.3f * r, cy + (i % 2 == 0 ? 0.95f : 0.72f) * r, 0.06f * r), Rgba.White.WithAlpha(0.85f), 1f);
                    // Bandeau sur l'œil.
                    img.Fill(Shape.Capsule(cx - 0.95f * r, cy + 0.5f * r, cx + 0.92f * r, cy + 0.05f * r, 0.035f * r), Ink);
                    Toon.Part(img, Shape.Ellipse(cx + ex, ey - 0.01f * r, 0.27f * r, 0.23f * r), Ink.Light(1.2f), sz * 0.3f, acc);
                    break;
                }
                case Accessory.Viking:
                {
                    var steel = Rgba.FromHex("#AAB4C3");
                    var horn = Rgba.FromHex("#FFF1D6");
                    for (int sd = -1; sd <= 1; sd += 2)
                        Toon.Part(img, Shape.Tapered(cx + sd * 0.82f * r, cy + 0.85f * r, 0.2f * r, cx + sd * 1.32f * r, cy + 1.55f * r, 0.03f * r), horn, sz * 0.5f, acc);
                    var helmet = Shape.Intersect(Shape.Ellipse(cx, cy + 0.6f * r, 1.04f * r, 0.88f * r), Shape.HalfPlane(0, -1, -(cy + 0.55f * r)));
                    Toon.Part(img, helmet, steel, sz, acc);
                    Toon.Part(img, Shape.Box(cx, cy + 0.6f * r, 1.08f * r, 0.11f * r, 0.08f * r), Gold.Shade(0.9f), sz, acc);
                    Toon.Part(img, Shape.Box(cx, cy + 1.05f * r, 0.08f * r, 0.42f * r, 0.06f * r), Gold.Shade(0.9f), sz * 0.3f, acc);
                    for (int i = -3; i <= 3; i++)
                        img.Fill(Shape.Circle(cx + i * 0.28f * r, cy + 0.6f * r, 0.035f * r), Gold.Light(1.2f));
                    break;
                }
                case Accessory.Chef:
                {
                    var white = Rgba.FromHex("#FFFDF7");
                    var toque = Shape.Union(Puff(cx, cy + 1.3f * r, 0.5f * r, 7, 0.13f * r), Shape.Box(cx, cy + 0.95f * r, 0.6f * r, 0.26f * r, 0.08f * r));
                    Toon.Part(img, toque, white, sz, acc);
                    img.Fill(Shape.Box(cx, cy + 0.78f * r, 0.62f * r, 0.03f * r), Rgba.FromHex("#D9D3C4"));
                    for (int i = -1; i <= 1; i++)
                        img.FillClipped(toque, Shape.Capsule(cx + i * 0.3f * r, cy + 0.82f * r, cx + i * 0.32f * r, cy + 1.5f * r, 0.02f * r), Rgba.FromHex("#E4DECF"), 1.5f);
                    break;
                }
                case Accessory.CatEars:
                {
                    var fur = h.Hair == HairStyle.None ? Rgba.FromHex("#3B3B4F") : hair;
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        float bx = cx + sd * 0.6f * r, by = cy + 0.82f * r;
                        var ear = Shape.Poly(bx - sd * 0.32f * r, by - 0.05f * r, bx + sd * 0.34f * r, by - 0.12f * r, bx + sd * 0.16f * r, by + 0.72f * r).Grow(0.05f * r);
                        Toon.Part(img, ear, fur, sz * 0.5f, acc);
                        img.FillClipped(ear, Shape.Poly(bx - sd * 0.12f * r, by + 0.02f * r, bx + sd * 0.2f * r, by - 0.02f * r, bx + sd * 0.12f * r, by + 0.48f * r), Pink.Light(1.15f), 2f);
                    }
                    // Moustaches de chat.
                    for (int sd = -1; sd <= 1; sd += 2)
                        for (int k = -1; k <= 1; k++)
                            img.Fill(Shape.Capsule(cx + sd * 0.42f * r, cy - 0.2f * r + k * 0.06f * r, cx + sd * 0.9f * r, cy - 0.16f * r + k * 0.14f * r, 0.012f * r), Ink.WithAlpha(0.8f));
                    break;
                }
                case Accessory.Flower:
                {
                    float fx = cx + 0.62f * r, fy = cy + 0.92f * r;
                    var petal = Rgba.FromHex("#FF8FB8");
                    for (int i = 0; i < 6; i++)
                    {
                        double a = i * Math.PI / 3;
                        Toon.Part(img, Shape.Circle(fx + (float)Math.Cos(a) * 0.2f * r, fy + (float)Math.Sin(a) * 0.2f * r, 0.15f * r), petal, sz * 0.25f, acc);
                    }
                    Toon.Part(img, Shape.Circle(fx, fy, 0.12f * r), Gold, sz * 0.15f, acc);
                    Toon.Part(img, Shape.Ellipse(fx - 0.36f * r, fy - 0.12f * r, 0.16f * r, 0.07f * r, 30f), Rgba.FromHex("#4CB944"), sz * 0.2f, acc);
                    break;
                }
                case Accessory.PartyHat:
                {
                    float tx = cx + 0.12f * r, ty = cy + 1.66f * r;
                    var cone = Shape.Poly(cx - 0.42f * r, cy + 0.82f * r, cx + 0.5f * r, cy + 0.86f * r, tx, ty).Grow(0.03f * r);
                    Toon.Part(img, cone, Blue, sz, acc);
                    for (int i = 0; i < 3; i++)
                        img.FillClipped(cone, Shape.Capsule(cx - 0.6f * r, cy + (0.9f + i * 0.3f) * r, cx + 0.7f * r, cy + (1.12f + i * 0.3f) * r, 0.06f * r), Gold, 1f);
                    Toon.Part(img, Puff(tx, ty, 0.12f * r, 7, 0.04f * r), Accent, sz * 0.2f, acc);
                    break;
                }
                case Accessory.Wizard:
                {
                    var robe = Rgba.FromHex("#5B3FD9");
                    var brim = Shape.Ellipse(cx, cy + 0.82f * r, 1.12f * r, 0.18f * r);
                    var cone = Shape.Smooth(Shape.Poly(cx - 0.66f * r, cy + 0.84f * r, cx + 0.66f * r, cy + 0.84f * r, cx + 0.12f * r, cy + 1.7f * r),
                        Shape.Tapered(cx + 0.12f * r, cy + 1.62f * r, 0.08f * r, cx + 0.62f * r, cy + 1.78f * r, 0.03f * r), 0.06f * r);
                    Toon.Part(img, brim, robe.Shade(0.85f), sz, acc);
                    Toon.Part(img, cone, robe, sz, acc);
                    foreach (var (sx, sy, sr) in new[] { (-0.2f, 1.1f, 0.09f), (0.22f, 1.3f, 0.07f), (0.02f, 1.5f, 0.05f) })
                        Toon.Part(img, Shape.Poly(cx + sx * r, cy + (sy + sr * 1.6f) * r, cx + (sx + sr) * r, cy + sy * r, cx + sx * r, cy + (sy - sr * 1.6f) * r, cx + (sx - sr) * r, cy + sy * r), Gold, sz * 0.1f, acc);
                    break;
                }
                case Accessory.Astronaut:
                {
                    var glass = Shape.Circle(cx, cy + 0.08f * r, 1.32f * r);
                    img.Fill(glass, Paint.Radial(Rgba.FromHex("#BFE6FF").WithAlpha(0.18f), cx - 0.4f * r, cy + 0.6f * r, Rgba.FromHex("#7FB8FF").WithAlpha(0.3f), 1.4f * r));
                    img.Fill(Shape.Subtract(glass.Grow(0.07f * r), glass), Rgba.FromHex("#E8EEF7"));
                    img.Fill(Shape.Subtract(glass.Grow(0.09f * r), glass.Grow(0.06f * r)), Ink.WithAlpha(0.6f));
                    img.Fill(Shape.Arc(cx, cy + 0.08f * r, 1.12f * r, 110f, 150f, 0.1f * r), Rgba.White.WithAlpha(0.75f));
                    img.Fill(Shape.Arc(cx, cy + 0.08f * r, 1.12f * r, 158f, 168f, 0.08f * r), Rgba.White.WithAlpha(0.6f));
                    break;
                }
                case Accessory.Unicorn:
                {
                    var horn = Shape.Tapered(cx, cy + 0.9f * r, 0.17f * r, cx + 0.12f * r, cy + 1.78f * r, 0.02f * r);
                    Toon.Part(img, horn, Gold.Light(1.1f), sz * 0.6f, acc);
                    for (int i = 0; i < 4; i++)
                        img.FillClipped(horn, Shape.Capsule(cx - 0.3f * r, cy + (0.95f + i * 0.2f) * r, cx + 0.3f * r, cy + (1.08f + i * 0.2f) * r, 0.02f * r), Gold.Shade(0.8f), 1f);
                    foreach (var (fx, fy, c) in new[] { (-0.55f, 0.92f, "#FF8FB8"), (-0.28f, 1.02f, "#9B5DE5"), (0.34f, 1.0f, "#3A86FF") })
                        Toon.Part(img, Shape.Circle(cx + fx * r, cy + fy * r, 0.12f * r), Rgba.FromHex(c), sz * 0.15f, acc);
                    break;
                }
                case Accessory.DevilHorns:
                    for (int sd = -1; sd <= 1; sd += 2)
                        Toon.Part(img, Shape.Smooth(Shape.Tapered(cx + sd * 0.52f * r, cy + 0.82f * r, 0.17f * r, cx + sd * 0.8f * r, cy + 1.25f * r, 0.08f * r),
                            Shape.Tapered(cx + sd * 0.8f * r, cy + 1.25f * r, 0.08f * r, cx + sd * 0.66f * r, cy + 1.55f * r, 0.015f * r), 0.04f * r), Rgba.FromHex("#C1121F"), sz * 0.4f, acc);
                    break;
            }
            return img;
        }

        /// <summary>Tête complète avec son expression propre (collection, carte de score).</summary>
        public static Raster Portrait(HeadDef h, int n, Expression? expression = null)
        {
            var img = Back(h, n);
            img.Composite(Face(expression ?? h.Expression, n));
            img.Composite(Front(h, n));
            return img;
        }
    }

    /// <summary>Formes de base et petits éléments de décor.</summary>
    public static class ShapeArtist
    {
        public static Raster Circle(int n)
        {
            var img = new Raster(n, n);
            img.Fill(Shape.Circle(n * 0.5f, n * 0.5f, n * 0.5f - 1), Rgba.White);
            return img;
        }

        /// <summary>Carré aux coins arrondis (rayon en part du côté). Prévu pour le 9-slice.</summary>
        public static Raster RoundedSquare(int n, float radiusRatio)
        {
            var img = new Raster(n, n);
            img.Fill(Shape.Box(n * 0.5f, n * 0.5f, n * 0.5f - 1, n * 0.5f - 1, n * radiusRatio), Rgba.White);
            return img;
        }

        public static Raster Capsule(int w, int h)
        {
            var img = new Raster(w, h);
            float r = Math.Min(w, h) * 0.5f - 1;
            if (w >= h) img.Fill(Shape.Capsule(r + 1, h * 0.5f, w - r - 1, h * 0.5f, r), Rgba.White);
            else img.Fill(Shape.Capsule(w * 0.5f, r + 1, w * 0.5f, h - r - 1, r), Rgba.White);
            return img;
        }

        /// <summary>Halo doux (lueurs, ombre au sol).</summary>
        public static Raster SoftDisc(int n, float hardness = 0.2f)
        {
            var img = new Raster(n, n);
            img.Fill(Shape.Circle(n * 0.5f, n * 0.5f, n * 0.5f * (0.5f + hardness * 0.5f)), Rgba.White, n * 0.5f * (1f - hardness));
            return img;
        }

        /// <summary>Montgolfière dessinée pour le 128 qui s'envole.</summary>
        public static Raster Balloon(int n, Rgba color)
        {
            var img = new Raster(n, n);
            float cx = n * 0.5f;
            var gold = Rgba.FromHex("#FFC93C");
            var st = new Toon.Style { Outline = 0.025f, Shade = 0.22f, Soft = 0.15f, Rim = 0.04f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.1f };
            for (int s = -1; s <= 1; s += 2)
                img.Fill(Shape.Capsule(cx + s * n * 0.13f, n * 0.2f, cx + s * n * 0.07f, n * 0.36f, n * 0.008f), HeadArtist.Ink);
            Toon.Part(img, Shape.Box(cx, n * 0.15f, n * 0.13f, n * 0.08f, n * 0.03f), Rgba.FromHex("#B8743F"), n * 0.4f, st);
            var envelope = Shape.Smooth(Shape.Circle(cx, n * 0.64f, n * 0.31f), Shape.Poly(cx - n * 0.17f, n * 0.55f, cx + n * 0.17f, n * 0.55f, cx + n * 0.08f, n * 0.34f, cx - n * 0.08f, n * 0.34f), n * 0.08f);
            Toon.Part(img, envelope, color, n * 0.7f, st);
            for (int i = -1; i <= 1; i++)
                img.FillClipped(envelope, Shape.Ellipse(cx + i * n * 0.2f, n * 0.62f, n * 0.05f, n * 0.4f), gold.WithAlpha(0.8f), 2f);
            return img;
        }

        /// <summary>Étoile d'éclat pour les fusions.</summary>
        public static Raster Spark(int n)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            var star = Shape.Smooth(Shape.Poly(c, n - 1, c + n * 0.1f, c, c, 1, c - n * 0.1f, c), Shape.Poly(n - 1, c, c, c + n * 0.1f, 1, c, c, c - n * 0.1f), n * 0.05f);
            img.Fill(star, Rgba.White);
            return img;
        }

        /// <summary>Anneau d'onde de choc.</summary>
        public static Raster Ring(int n)
        {
            var img = new Raster(n, n);
            img.Fill(Shape.Subtract(Shape.Circle(n * 0.5f, n * 0.5f, n * 0.47f), Shape.Circle(n * 0.5f, n * 0.5f, n * 0.38f)), Rgba.White, n * 0.04f);
            return img;
        }

        /// <summary>Petit nuage de poussière.</summary>
        public static Raster Puff(int n)
        {
            var img = new Raster(n, n);
            var c = Rgba.FromHex("#FFFFFF");
            var s = Shape.Union(Shape.Circle(n * 0.5f, n * 0.45f, n * 0.28f), Shape.Circle(n * 0.32f, n * 0.38f, n * 0.2f), Shape.Circle(n * 0.68f, n * 0.38f, n * 0.2f), Shape.Circle(n * 0.5f, n * 0.62f, n * 0.2f));
            Toon.Part(img, s, c, n, new Toon.Style { Outline = 0f, Shade = 0.12f, Soft = 0.15f, ShadeStrength = 0.86f, Rim = 0f, Specular = false });
            return img;
        }

        /// <summary>Goutte de sueur qui s'envole.</summary>
        public static Raster SweatDrop(int n)
        {
            var img = new Raster(n, n);
            float x = n * 0.5f, y = n * 0.38f;
            var drop = Shape.Smooth(Shape.Circle(x, y, n * 0.25f), Shape.Poly(x - n * 0.18f, y + n * 0.1f, x + n * 0.18f, y + n * 0.1f, x, y + n * 0.58f), n * 0.1f);
            Toon.Part(img, drop, Rgba.FromHex("#8FD3FF"), n, new Toon.Style { Outline = 0.06f, OutlineColor = Rgba.FromHex("#3D7FB8"), Shade = 0.15f, Rim = 0f, Specular = true, SpecSize = 0.14f });
            return img;
        }

        /// <summary>Confetti (rectangle arrondi blanc, teinté au moment de l'effet).</summary>
        public static Raster Confetti(int n)
        {
            var img = new Raster(n, n / 2);
            img.Fill(Shape.Box(n * 0.5f, n * 0.25f, n * 0.45f, n * 0.2f, n * 0.08f), Rgba.White);
            return img;
        }
    }
}
