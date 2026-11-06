namespace Bascule.Core
{
    /// <summary>
    /// Style d'illustration de Bascule : contour sombre coloré, volume par dégradé doux, ombre propre côté
    /// bas-droite, liseré de lumière côté haut-gauche, reflet spéculaire. Lumière venant d'en haut à gauche.
    /// </summary>
    public static class Toon
    {
        public sealed class Style
        {
            /// <summary>Épaisseur du contour, en part de la taille de référence.</summary>
            public float Outline = 0.035f;
            /// <summary>Décalage de l'ombre propre (part de la taille).</summary>
            public float Shade = 0.16f;
            /// <summary>Douceur du bord de l'ombre (part de la taille).</summary>
            public float Soft = 0.09f;
            public float ShadeStrength = 0.8f;
            public float Rim = 0.05f;
            public float RimOpacity = 0.55f;
            public bool Specular = true;
            public float SpecSize = 0.12f;
            public Rgba? OutlineColor;
        }

        public static readonly Style Default = new Style();
        public static readonly Style Flat = new Style { Shade = 0.1f, Rim = 0.03f, Specular = false };
        public static readonly Style NoLine = new Style { Outline = 0f };

        /// <param name="size">Taille de référence de l'objet en pixels (sert à doser contour et ombres).</param>
        public static void Part(Raster img, Shape s, Rgba color, float size, Style st = null)
        {
            st = st ?? Default;
            float o = st.Outline * size;
            if (o > 0.01f) img.Fill(s.Grow(o), st.OutlineColor ?? color.Outline());

            // Volume : léger dégradé du haut (clair) vers le bas.
            img.Fill(s, Paint.Linear(color.Light(1.07f), 0, s.Y1, color.Shade(0.97f), 0, s.Y0));

            // Ombre propre : ce qui n'est pas « vu » par la lumière venant d'en haut à gauche.
            float k = st.Shade * size;
            img.FillClipped(s, s.Move(-k * 0.55f, k).Not(), color.Shadow(st.ShadeStrength), st.Soft * size);

            // Liseré de lumière en haut à gauche.
            if (st.Rim > 0)
            {
                float r = st.Rim * size;
                img.FillClipped(s, s.Move(r * 0.8f, -r).Not(), color.Light(1.35f), st.Soft * size * 0.5f, st.RimOpacity);
            }

            // Reflet spéculaire.
            if (st.Specular)
            {
                float w = s.X1 - s.X0, h = s.Y1 - s.Y0;
                float sx = s.X0 + w * 0.3f, sy = s.Y0 + h * 0.74f;
                float rs = st.SpecSize * size;
                img.FillClipped(s, Shape.Ellipse(sx, sy, rs, rs * 0.6f, 25f), Rgba.White.WithAlpha(0.55f), rs * 0.8f);
            }
        }

        /// <summary>Ombre portée douce sous un dessin (copie floutée, sombre, décalée).</summary>
        public static void DropShadow(Raster img, int dx, int dy, int blur, float opacity = 0.35f)
        {
            var shadow = img.Colorize(new Rgba(30, 20, 60), opacity).Offset(dx, dy);
            shadow.Blur(blur, 2);
            img.Underlay(shadow);
        }
    }
}
