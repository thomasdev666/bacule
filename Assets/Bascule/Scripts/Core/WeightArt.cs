using System;

namespace Bascule.Core
{
    /// <summary>
    /// Les poids façon « poids en fonte de dessin animé » : bloc trapézoïdal émaillé, anse en fer,
    /// plaque gravée pour le chiffre, rivets et dorures qui deviennent plus riches avec la valeur.
    /// Les proportions (en part de la largeur de la texture) servent aussi au collider physique.
    /// </summary>
    public static class WeightArtist
    {
        // Géométrie du corps, en part du côté de la texture (origine en bas à gauche).
        public const float BodyBottom = 0.07f, BodyTop = 0.69f;
        public const float BottomHalfWidth = 0.465f, TopHalfWidth = 0.36f;
        public const float PlateCenterY = 0.395f;
        /// <summary>Hauteur du chiffre (part du côté) pour 1, 2 et 3 chiffres.</summary>
        public static float DigitHeight(int digits) => digits >= 3 ? 0.17f : digits == 2 ? 0.24f : 0.28f;
        /// <summary>Centre du corps : c'est le pivot du sprite et l'origine du collider.</summary>
        public const float BodyCenterY = (BodyBottom + BodyTop) * 0.5f;

        static readonly Rgba Iron = Rgba.FromHex("#4A4E69");
        static readonly Rgba Gold = Rgba.FromHex("#FFC93C");
        static readonly Rgba Steel = Rgba.FromHex("#C9CCD6");

        /// <summary>Sommets du collider (trapèze), en part de la largeur, relatifs au centre du corps.</summary>
        public static float[] ColliderPoints() => new[]
        {
            -BottomHalfWidth, BodyBottom - BodyCenterY,
            BottomHalfWidth, BodyBottom - BodyCenterY,
            TopHalfWidth, BodyTop - BodyCenterY,
            -TopHalfWidth, BodyTop - BodyCenterY,
        };

        public static Raster Draw(int value, int n)
        {
            value = WeightCatalog.Normalize(value);
            int tier = WeightCatalog.IndexOf(value);
            var img = new Raster(n, n);
            float cx = n * 0.5f;
            var color = WeightCatalog.Color(value);

            // Anse en fer (derrière le corps), posée sur deux petits plots.
            var ironStyle = new Toon.Style { Outline = 0.022f, Shade = 0.05f, Soft = 0.03f, SpecSize = 0.03f };
            var handleColor = value >= 128 ? Gold : Iron;
            var handle = Shape.Arc(cx, n * (BodyTop + 0.005f), n * 0.12f, 0f, 180f, n * 0.07f);
            Toon.Part(img, handle, handleColor, n, ironStyle);
            Toon.Part(img, Shape.Union(Shape.Box(cx - n * 0.12f, n * (BodyTop + 0.01f), n * 0.055f, n * 0.03f, n * 0.012f),
                Shape.Box(cx + n * 0.12f, n * (BodyTop + 0.01f), n * 0.055f, n * 0.03f, n * 0.012f)), handleColor.Shade(0.85f), n, ironStyle);

            // Corps trapézoïdal aux coins arrondis.
            float r = n * 0.045f;
            var body = Shape.Poly(
                cx - n * BottomHalfWidth + r, n * BodyBottom + r,
                cx + n * BottomHalfWidth - r, n * BodyBottom + r,
                cx + n * TopHalfWidth - r, n * BodyTop - r,
                cx - n * TopHalfWidth + r, n * BodyTop - r).Grow(r);
            var st = new Toon.Style { Outline = 0.026f, Shade = 0.2f, Soft = 0.12f, ShadeStrength = 0.78f, Rim = 0.035f, RimOpacity = 0.6f, Specular = false };
            Toon.Part(img, body, color, n, st);

            // Faces latérales en biseau : gauche éclairée, droite dans l'ombre (aspect bloc de fonte).
            float inset = n * 0.07f;
            var left = Shape.Poly(cx - n * BottomHalfWidth - n, n * BodyBottom, cx - n * BottomHalfWidth + inset, n * BodyBottom,
                cx - n * TopHalfWidth + inset * 0.8f, n * BodyTop, cx - n * TopHalfWidth - n, n * BodyTop);
            var right = Shape.Poly(cx + n * BottomHalfWidth - inset, n * BodyBottom, cx + n * BottomHalfWidth + n, n * BodyBottom,
                cx + n * TopHalfWidth + n, n * BodyTop, cx + n * TopHalfWidth - inset * 0.8f, n * BodyTop);
            img.FillClipped(body, left, color.Light(1.18f), n * 0.012f, 0.75f);
            img.FillClipped(body, right, color.Shadow(0.7f), n * 0.012f, 0.85f);

            // Biseau du dessus (face supérieure éclairée) et socle plus sombre.
            img.FillClipped(body, Shape.HalfPlane(0, -1, -n * (BodyTop - 0.085f)), color.Light(1.22f), n * 0.02f, 0.85f);
            img.FillClipped(body, Shape.HalfPlane(0, 1, n * (BodyBottom + 0.075f)), color.Shadow(0.68f), n * 0.02f, 0.9f);

            // Dorures pour les grosses valeurs.
            if (tier >= 5)
                img.FillClipped(body, Shape.Box(cx, n * (BodyBottom + 0.105f), n, n * 0.022f), Gold, 1f);
            if (tier >= 6)
                img.FillClipped(body, Shape.Box(cx, n * (BodyTop - 0.115f), n, n * 0.018f), Gold, 1f);

            // Plaque gravée (en creux) où s'inscrit le chiffre.
            float pw = n * 0.25f, ph = n * 0.19f;
            var plate = Shape.Box(cx, n * PlateCenterY, pw, ph, n * 0.06f);
            img.Fill(plate.Grow(n * 0.012f), color.Outline().WithAlpha(0.55f));
            img.Fill(plate, Paint.Linear(Rgba.Lerp(color, Rgba.White, 0.72f), 0, n * (PlateCenterY + 0.16f), Rgba.Lerp(color, Rgba.White, 0.9f), 0, n * (PlateCenterY - 0.16f)));
            img.FillClipped(plate, plate.Move(0, -n * 0.035f).Not(), color.Shadow(0.6f).WithAlpha(0.55f), n * 0.03f);
            img.FillClipped(plate, plate.Move(0, n * 0.02f).Not(), Rgba.White.WithAlpha(0.7f), n * 0.012f);

            // Rivets dès le 8.
            if (tier >= 3)
            {
                float ry = n * (BodyBottom + 0.12f), ry2 = n * (BodyTop - 0.055f);
                foreach (var (x, y) in new[] { (cx - n * 0.345f, ry), (cx + n * 0.345f, ry), (cx - n * 0.24f, ry2), (cx + n * 0.24f, ry2) })
                    Toon.Part(img, Shape.Circle(x, y, n * 0.028f), tier >= 5 ? Gold : Steel, n * 0.2f,
                        new Toon.Style { Outline = 0.08f, Shade = 0.25f, Soft = 0.2f, SpecSize = 0.25f });
            }

            // Étoile sur le 64 et le 128.
            if (tier >= 6)
            {
                float sx = cx, sy = n * (BodyTop - 0.06f), sr = n * 0.045f;
                var pts = new float[20];
                for (int i = 0; i < 10; i++)
                {
                    double a = Math.PI / 2 + i * Math.PI / 5;
                    float rr = i % 2 == 0 ? sr : sr * 0.45f;
                    pts[2 * i] = sx + (float)Math.Cos(a) * rr;
                    pts[2 * i + 1] = sy + (float)Math.Sin(a) * rr;
                }
                Toon.Part(img, Shape.Poly(pts), Gold, n * 0.3f, new Toon.Style { Outline = 0.05f, Shade = 0.1f, Soft = 0.1f, Specular = false });
            }

            // Reflet brillant d'émail.
            img.FillClipped(body, Shape.Ellipse(cx - n * 0.24f, n * (BodyTop - 0.17f), n * 0.05f, n * 0.12f, -20f), Rgba.White.WithAlpha(0.5f), n * 0.035f);
            img.FillClipped(body, Shape.Circle(cx - n * 0.255f, n * (BodyTop - 0.06f), n * 0.018f), Rgba.White.WithAlpha(0.8f), 2f);

            Toon.DropShadow(img, Math.Max(1, n / 80), -Math.Max(1, n / 50), Math.Max(1, n / 70), 0.3f);
            return img;
        }
    }
}
