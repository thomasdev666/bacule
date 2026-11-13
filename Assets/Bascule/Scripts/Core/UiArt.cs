using System;

namespace Bascule.Core
{
    public enum Icon
    {
        Pause, Play, Home, Trophy, Gear, Face, Share, Replay, Film, Flame, Star, Lock, Check, Back, Crown, Sound, SoundOff, Vibrate, Calendar, Close, Swords, Heart,
        Coin, Gift, Bag, Target, Hand, Bell, Plus, Flag,
        // 1.3 : roue de la chance (toujours ajouter à la fin)
        Wheel,
        // 1.5 : onglet « Têtes » du menu (une tête couronnée : des têtes à collectionner)
        Heads,
    }

    /// <summary>
    /// Interface façon jeu mobile : boutons « bonbon » en relief (teintés à l'usage), cartes, icônes,
    /// niveau à bulle pour la stabilité, halos et rayons de victoire.
    /// </summary>
    public static class UiArtist
    {
        public static readonly Rgba Ink = Rgba.FromHex("#2B2D42");
        public static readonly Rgba Cream = Rgba.FromHex("#FFF6E5");

        /// <summary>Hauteur du relief du bouton, en pixels de la texture (le bouton « s'enfonce » de cette valeur).</summary>
        public const int ButtonDepth = 14;

        /// <summary>
        /// Bouton en niveaux de gris : teinté par la couleur de l'Image, il garde son relief.
        /// Texture 192 × 200, 9-slice de 60 px.
        /// </summary>
        /// <param name="k">Finesse : 2 = texture deux fois plus grande (écrans haute densité), même dessin.</param>
        public static Raster Button(int w = 192, int h = 200, float k = 1f)
        {
            w = (int)(w * k); h = (int)(h * k);
            var img = new Raster(w, h);
            float r = 54f * k, o = 6f * k, d = ButtonDepth * k;
            var body = Shape.Box(w * 0.5f, h * 0.5f, w * 0.5f - o - 2, h * 0.5f - o - 2, r);
            var face = Shape.Box(w * 0.5f, h * 0.5f + d * 0.5f, w * 0.5f - o - 2, h * 0.5f - o - 2 - d * 0.5f, r);
            img.Fill(body.Grow(o), new Rgba(70, 70, 70));
            img.Fill(body, new Rgba(150, 150, 150));
            img.Fill(face, Paint.Linear(new Rgba(255, 255, 255), 0, h - o, new Rgba(214, 214, 214), 0, o + d));
            // Brillance en haut.
            var gloss = Shape.Intersect(face.Grow(-7f * k), Shape.HalfPlane(0, -1, -(h * 0.66f)));
            img.Fill(gloss, Rgba.White.WithAlpha(0.55f), 3f * k);
            img.Fill(Shape.Capsule(w * 0.24f, h - o - 26 * k, w * 0.34f, h - o - 26 * k, 7f * k), Rgba.White.WithAlpha(0.9f));
            return img;
        }

        /// <summary>Carte crème avec contour et ombre intérieure, 9-slice de 64 px.</summary>
        public static Raster Panel(int n = 200, float k = 1f)
        {
            n = (int)(n * k);
            var img = new Raster(n, n);
            float o = 6f * k, r = 52f * k;
            var card = Shape.Box(n * 0.5f, n * 0.5f, n * 0.5f - o - 4 * k, n * 0.5f - o - 4 * k, r);
            img.Fill(card.Grow(o), Rgba.FromHex("#3B2F4A"));
            img.Fill(card, Paint.Linear(Cream, 0, n, Rgba.FromHex("#FBE7C6"), 0, 0));
            img.FillClipped(card, card.Move(0, 10 * k).Not(), Rgba.FromHex("#E8C99A").WithAlpha(0.8f), 8f * k);
            img.FillClipped(card, card.Move(0, -6 * k).Not(), Rgba.White.WithAlpha(0.9f), 4f * k);
            return img;
        }

        /// <summary>Pilule sombre translucide (fond du score, étiquettes).</summary>
        public static Raster Pill(int w = 160, int h = 96, float k = 1f)
        {
            w = (int)(w * k); h = (int)(h * k);
            var img = new Raster(w, h);
            img.Fill(Shape.Box(w * 0.5f, h * 0.5f, w * 0.5f - 2 * k, h * 0.5f - 2 * k, h * 0.5f - 2 * k), Rgba.White);
            return img;
        }

        /// <summary>
        /// Verre liquide (pastilles, dock, barre de score, onglets) : verre bleu nuit translucide, liseré lumineux
        /// (vif en haut, discret en bas), reflet spéculaire en haut à gauche et reflet de lumière en bas.
        /// Dessiné avec ses propres couleurs : à afficher sans teinte (blanc). 9-slice de 46 px.
        /// </summary>
        public static Raster Glass(int w = 160, int h = 96, float k = 1f)
        {
            w = (int)(w * k); h = (int)(h * k);
            var img = new Raster(w, h);
            float m = 2f * k, r = h * 0.5f - m;
            var body = Shape.Box(w * 0.5f, h * 0.5f, w * 0.5f - m, h * 0.5f - m, r);
            // Verre bleu nuit, éclairci en douceur vers le haut (la lumière traverse le verre), sans démarcation.
            var low = new Rgba(22, 18, 58, 150);
            var high = new Rgba(70, 66, 140, 118);
            img.Fill(body, Paint.Custom((x, y) =>
            {
                float v = y / h;
                float lift = v * v * (3f - 2f * v); // courbe douce
                var c = Rgba.Lerp(low, high, lift);
                float shine = Math.Max(0f, (v - 0.55f) / 0.45f);
                return Rgba.Lerp(c, new Rgba(255, 255, 255, (byte)(c.a + 30)), shine * shine * 0.28f);
            }));
            // Liseré : un anneau fin, lumineux en haut.
            var rim = Shape.Subtract(body, body.Grow(-2.6f * k));
            img.Fill(rim, Paint.Linear(Rgba.White.WithAlpha(0.85f), 0, h, Rgba.White.WithAlpha(0.22f), 0, 0), 1.2f * k);
            // Reflet spéculaire : un petit trait arrondi en haut à gauche, entièrement dans le bout gauche de l'image
            // (les 46 px qui ne sont jamais étirés en 9-slice). Sinon il s'allonge en traînée sur les éléments larges.
            float sx = 30f * k, sy = h - m - 15f * k;
            img.Fill(Shape.Capsule(sx, sy, sx + 10f * k, sy + 3f * k, 3.2f * k), Rgba.White.WithAlpha(0.8f), 1.5f * k);
            // Reflet de lumière en bas.
            img.FillClipped(body.Grow(-4f * k), Shape.HalfPlane(0, 1, m + 12f * k), Rgba.White.WithAlpha(0.1f), 5f * k);
            return img;
        }

        /// <summary>
        /// Reflet « liquide » posé sur les boutons (même taille et mêmes bords que <see cref="Button"/>) : un voile
        /// blanc bombé sur le haut, un liseré lumineux et un point de lumière, en blanc pur (non teinté), pour que
        /// les boutons de couleur aient l'air de verre coloré.
        /// </summary>
        public static Raster ButtonGloss(int w = 192, int h = 200, float k = 1f)
        {
            w = (int)(w * k); h = (int)(h * k);
            var img = new Raster(w, h);
            float r = 54f * k, o = 6f * k, d = ButtonDepth * k;
            var face = Shape.Box(w * 0.5f, h * 0.5f + d * 0.5f, w * 0.5f - o - 2, h * 0.5f - o - 2 - d * 0.5f, r);
            // Voile bombé : une grande ellipse coupée par la face, plus claire en haut.
            var dome = Shape.Intersect(face.Grow(-5f * k), Shape.Ellipse(w * 0.5f, h * 1.02f, w * 0.62f, h * 0.42f));
            img.Fill(dome, Paint.Linear(Rgba.White.WithAlpha(0.42f), 0, h, Rgba.White.WithAlpha(0.06f), 0, h * 0.58f), 2.5f * k);
            // Liseré intérieur lumineux sur le haut de la face.
            var edge = Shape.Intersect(Shape.Subtract(face.Grow(-2f * k), face.Grow(-6f * k)), Shape.HalfPlane(0, -1, -(h * 0.8f)));
            img.Fill(edge, Rgba.White.WithAlpha(0.55f), 1.5f * k);
            // Point de lumière et reflet de lumière en bas de la face (le verre renvoie la lumière).
            img.Fill(Shape.Circle(w * 0.22f, h - o - 30f * k, 6f * k), Rgba.White.WithAlpha(0.95f), 1.5f * k);
            var low = Shape.Intersect(face.Grow(-9f * k), Shape.Ellipse(w * 0.5f, o + d + 10f * k, w * 0.36f, 12f * k));
            img.Fill(low, Rgba.White.WithAlpha(0.22f), 5f * k);
            return img;
        }

        /// <summary>Icônes du menu du bas de l'accueil (Têtes, Missions, Boutique, Classement, Duel).</summary>
        public static readonly Icon[] MenuIcons = { Core.Icon.Heads, Core.Icon.Target, Core.Icon.Bag, Core.Icon.Trophy, Core.Icon.Swords };
        /// <summary>Plus grande dimension du dessin d'une icône du menu (sans le contour), en part de l'image.</summary>
        public const float MenuGlyph = 0.72f;

        /// <summary>
        /// Recentre une forme au milieu d'une image n × n et la met à la taille <paramref name="extent"/> (sa plus grande
        /// dimension). La boîte réelle est mesurée sur une grille (les boîtes des formes combinées sont trop larges).
        /// </summary>
        public static Shape Fit(Shape s, int n, float extent)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            float step = n / 256f;
            for (float y = 0; y <= n; y += step)
                for (float x = 0; x <= n; x += step)
                    if (s.D(x, y) <= 0f)
                    {
                        if (x < x0) x0 = x; if (x > x1) x1 = x;
                        if (y < y0) y0 = y; if (y > y1) y1 = y;
                    }
            if (x1 < x0) return s;
            float c = n * 0.5f, mx = (x0 + x1) * 0.5f, my = (y0 + y1) * 0.5f;
            float k = extent / Math.Max(x1 - x0, y1 - y0);
            var d = s.D;
            return new Shape((x, y) => d(mx + (x - c) / k, my + (y - c) / k) * k, 0, 0, n, n);
        }

        /// <summary>Icône blanche cernée de sombre, avec ombre portée (128 px).</summary>
        public static Raster DrawIcon(Icon icon, int n = 128)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, u = n / 100f;
            Shape s;
            switch (icon)
            {
                case Core.Icon.Pause:
                    s = Shape.Union(Shape.Box(c - 14 * u, c, 9 * u, 28 * u, 5 * u), Shape.Box(c + 14 * u, c, 9 * u, 28 * u, 5 * u)); break;
                case Core.Icon.Play:
                    s = Shape.Poly(c - 18 * u, c - 28 * u, c + 30 * u, c, c - 18 * u, c + 28 * u).Grow(5 * u); break;
                case Core.Icon.Home:
                    s = Shape.Union(Shape.Poly(c - 34 * u, c + 4 * u, c, c + 34 * u, c + 34 * u, c + 4 * u).Grow(4 * u),
                        Shape.Subtract(Shape.Box(c, c - 14 * u, 24 * u, 22 * u, 4 * u), Shape.Box(c, c - 22 * u, 8 * u, 14 * u, 3 * u))); break;
                case Core.Icon.Trophy:
                    s = Shape.Union(Shape.Intersect(Shape.Circle(c, c + 14 * u, 26 * u), Shape.HalfPlane(0, -1, -(c + 2 * u))).Grow(0),
                        Shape.Box(c, c + 26 * u, 26 * u, 14 * u, 4 * u), Shape.Box(c, c - 14 * u, 5 * u, 14 * u, 2 * u), Shape.Box(c, c - 30 * u, 20 * u, 6 * u, 3 * u),
                        Shape.Arc(c - 26 * u, c + 22 * u, 10 * u, 90f, 270f, 6 * u), Shape.Arc(c + 26 * u, c + 22 * u, 10 * u, -90f, 90f, 6 * u)); break;
                case Core.Icon.Gear:
                {
                    s = Shape.Circle(c, c, 24 * u);
                    for (int i = 0; i < 8; i++)
                    {
                        double a = i * Math.PI / 4;
                        s = Shape.Union(s, Shape.Box(c + (float)Math.Cos(a) * 28 * u, c + (float)Math.Sin(a) * 28 * u, 8 * u, 8 * u, 3 * u, (float)(a * 180 / Math.PI)));
                    }
                    s = Shape.Subtract(s, Shape.Circle(c, c, 11 * u)); break;
                }
                case Core.Icon.Face:
                    s = Shape.Subtract(Shape.Circle(c, c, 34 * u), Shape.Union(Shape.Circle(c - 12 * u, c + 8 * u, 6 * u), Shape.Circle(c + 12 * u, c + 8 * u, 6 * u),
                        Shape.Arc(c, c - 2 * u, 18 * u, 205f, 335f, 7 * u))); break;
                case Core.Icon.Share:
                    s = Shape.Union(Shape.Subtract(Shape.Box(c, c - 12 * u, 30 * u, 24 * u, 8 * u), Shape.Box(c, c - 6 * u, 20 * u, 24 * u, 3 * u)),
                        Shape.Box(c, c + 6 * u, 6 * u, 26 * u, 3 * u), Shape.Poly(c - 20 * u, c + 20 * u, c + 20 * u, c + 20 * u, c, c + 40 * u).Grow(2 * u)); break;
                case Core.Icon.Replay:
                    s = Shape.Union(Shape.Arc(c, c, 24 * u, -200f, 110f, 11 * u), Shape.Poly(c - 34 * u, c + 22 * u, c - 6 * u, c + 34 * u, c - 10 * u, c + 6 * u).Grow(2 * u)); break;
                case Core.Icon.Film:
                    s = Shape.Union(Shape.Box(c - 8 * u, c, 26 * u, 22 * u, 6 * u), Shape.Poly(c + 20 * u, c - 2 * u, c + 38 * u, c - 16 * u, c + 38 * u, c + 16 * u, c + 20 * u, c + 2 * u).Grow(2 * u)); break;
                case Core.Icon.Flame:
                {
                    // Flamme à deux langues, cœur évidé.
                    var outer = Shape.Smooth(Shape.Smooth(Shape.Circle(c, c - 14 * u, 25 * u), Shape.Tapered(c + 2 * u, c - 6 * u, 18 * u, c + 6 * u, c + 40 * u, 1.5f * u), 9 * u),
                        Shape.Tapered(c - 14 * u, c - 10 * u, 12 * u, c - 26 * u, c + 20 * u, 1.5f * u), 8 * u);
                    var inner = Shape.Smooth(Shape.Circle(c + 1 * u, c - 20 * u, 11 * u), Shape.Tapered(c + 1 * u, c - 16 * u, 8 * u, c + 4 * u, c + 6 * u, 1f * u), 5 * u);
                    s = Shape.Subtract(outer, inner);
                    break;
                }
                case Core.Icon.Star:
                {
                    var pts = new float[20];
                    for (int i = 0; i < 10; i++)
                    {
                        double a = Math.PI / 2 + i * Math.PI / 5;
                        float rr = (i % 2 == 0 ? 38 : 17) * u;
                        pts[2 * i] = c + (float)Math.Cos(a) * rr; pts[2 * i + 1] = c + (float)Math.Sin(a) * rr;
                    }
                    s = Shape.Poly(pts).Grow(3 * u); break;
                }
                case Core.Icon.Lock:
                    s = Shape.Union(Shape.Box(c, c - 10 * u, 26 * u, 22 * u, 6 * u), Shape.Arc(c, c + 12 * u, 16 * u, 0f, 180f, 8 * u),
                        Shape.Box(c - 16 * u, c + 8 * u, 4 * u, 6 * u), Shape.Box(c + 16 * u, c + 8 * u, 4 * u, 6 * u)); break;
                case Core.Icon.Check:
                    s = Shape.Union(Shape.Capsule(c - 26 * u, c, c - 8 * u, c - 18 * u, 8 * u), Shape.Capsule(c - 8 * u, c - 18 * u, c + 28 * u, c + 22 * u, 8 * u)); break;
                case Core.Icon.Back:
                    s = Shape.Union(Shape.Capsule(c - 22 * u, c, c + 26 * u, c, 8 * u), Shape.Capsule(c - 24 * u, c, c - 4 * u, c + 20 * u, 8 * u), Shape.Capsule(c - 24 * u, c, c - 4 * u, c - 20 * u, 8 * u)); break;
                case Core.Icon.Crown:
                    s = Shape.Poly(c - 34 * u, c - 22 * u, c + 34 * u, c - 22 * u, c + 38 * u, c + 26 * u, c + 16 * u, c + 4 * u, c, c + 32 * u, c - 16 * u, c + 4 * u, c - 38 * u, c + 26 * u).Grow(2 * u); break;
                case Core.Icon.Sound:
                case Core.Icon.SoundOff:
                {
                    s = Shape.Union(Shape.Box(c - 22 * u, c, 10 * u, 12 * u, 2 * u), Shape.Poly(c - 16 * u, c - 12 * u, c + 6 * u, c - 30 * u, c + 6 * u, c + 30 * u, c - 16 * u, c + 12 * u).Grow(2 * u));
                    if (icon == Core.Icon.Sound) s = Shape.Union(s, Shape.Arc(c + 8 * u, c, 16 * u, -45f, 45f, 6 * u), Shape.Arc(c + 8 * u, c, 28 * u, -45f, 45f, 6 * u));
                    else s = Shape.Union(s, Shape.Capsule(c + 16 * u, c - 14 * u, c + 36 * u, c + 14 * u, 5 * u), Shape.Capsule(c + 16 * u, c + 14 * u, c + 36 * u, c - 14 * u, 5 * u));
                    break;
                }
                case Core.Icon.Vibrate:
                    s = Shape.Union(Shape.Subtract(Shape.Box(c, c, 16 * u, 28 * u, 6 * u), Shape.Box(c, c + 2 * u, 9 * u, 18 * u, 2 * u)),
                        Shape.Arc(c, c, 26 * u, 150f, 210f, 5 * u), Shape.Arc(c, c, 26 * u, -30f, 30f, 5 * u)); break;
                case Core.Icon.Calendar:
                    s = Shape.Union(Shape.Subtract(Shape.Box(c, c - 4 * u, 32 * u, 28 * u, 6 * u), Shape.Box(c, c - 10 * u, 24 * u, 16 * u, 2 * u)),
                        Shape.Box(c - 14 * u, c + 28 * u, 4 * u, 8 * u, 2 * u), Shape.Box(c + 14 * u, c + 28 * u, 4 * u, 8 * u, 2 * u)); break;
                case Core.Icon.Heart:
                    s = Shape.Smooth(Shape.Union(Shape.Circle(c - 17 * u, c + 10 * u, 20 * u), Shape.Circle(c + 17 * u, c + 10 * u, 20 * u)),
                        Shape.Poly(c - 34 * u, c + 2 * u, c + 34 * u, c + 2 * u, c, c - 34 * u).Grow(3 * u), 8 * u); break;
                case Core.Icon.Swords:
                    // Deux épées croisées (1.5) : lame qui s'affine en pointe, garde, poignée et pommeau.
                    s = Shape.Union(Sword(c, c - 1 * u, 42f, 92 * u), Sword(c, c - 1 * u, -42f, 92 * u)); break;
                case Core.Icon.Coin:
                    s = Shape.Union(Shape.Subtract(Shape.Circle(c, c, 36 * u), Shape.Subtract(Shape.Circle(c, c, 27 * u), Shape.Circle(c, c, 22 * u))),
                        Shape.Box(c, c, 5 * u, 14 * u, 3 * u)); break;
                case Core.Icon.Gift:
                    s = Shape.Subtract(Shape.Union(Shape.Box(c, c - 12 * u, 28 * u, 22 * u, 4 * u), Shape.Box(c, c + 16 * u, 34 * u, 8 * u, 3 * u),
                            Shape.Arc(c - 10 * u, c + 30 * u, 9 * u, -20f, 200f, 5 * u), Shape.Arc(c + 10 * u, c + 30 * u, 9 * u, -20f, 200f, 5 * u)),
                        Shape.Union(Shape.Box(c, c, 4 * u, 40 * u), Shape.Box(c, c + 7 * u, 40 * u, 2 * u))); break;
                case Core.Icon.Bag:
                    s = Shape.Union(Shape.Smooth(Shape.Box(c, c - 10 * u, 30 * u, 26 * u, 8 * u), Shape.Box(c, c - 30 * u, 33 * u, 6 * u, 4 * u), 4 * u),
                        Shape.Arc(c, c + 18 * u, 15 * u, 0f, 180f, 6 * u)); break;
                case Core.Icon.Target:
                    s = Shape.Union(Shape.Subtract(Shape.Circle(c, c, 36 * u), Shape.Circle(c, c, 27 * u)), Shape.Subtract(Shape.Circle(c, c, 20 * u), Shape.Circle(c, c, 12 * u)),
                        Shape.Circle(c, c, 6 * u)); break;
                case Core.Icon.Hand:
                    s = Shape.Smooth(Shape.Smooth(Shape.Box(c + 4 * u, c - 16 * u, 20 * u, 18 * u, 9 * u), Shape.Capsule(c - 6 * u, c - 4 * u, c - 6 * u, c + 34 * u, 8 * u), 4 * u),
                        Shape.Capsule(c - 22 * u, c - 6 * u, c - 30 * u, c + 8 * u, 7 * u), 4 * u); break;
                case Core.Icon.Bell:
                    s = Shape.Union(Shape.Smooth(Shape.Intersect(Shape.Circle(c, c + 2 * u, 26 * u), Shape.HalfPlane(0, -1, -(c - 4 * u))), Shape.Box(c, c - 12 * u, 30 * u, 8 * u, 4 * u), 6 * u),
                        Shape.Circle(c, c - 26 * u, 8 * u), Shape.Circle(c, c + 30 * u, 5 * u)); break;
                case Core.Icon.Flag:
                    // Petit drapeau de signalement : mât à gauche, fanion qui ondule.
                    s = Shape.Union(Shape.Capsule(c - 24 * u, c - 36 * u, c - 24 * u, c + 34 * u, 5 * u),
                        Shape.Poly(c - 22 * u, c + 34 * u, c + 4 * u, c + 26 * u, c + 32 * u, c + 32 * u, c + 30 * u, c - 2 * u,
                            c + 4 * u, c - 6 * u, c - 22 * u, c + 2 * u).Grow(2 * u)); break;
                case Core.Icon.Plus:
                    s = Shape.Union(Shape.Capsule(c - 28 * u, c, c + 28 * u, c, 9 * u), Shape.Capsule(c, c - 28 * u, c, c + 28 * u, 9 * u)); break;
                case Core.Icon.Wheel:
                {
                    // Roue : anneau, quatre rayons, moyeu, et le petit repère en haut.
                    float cy = c - 4 * u;
                    s = Shape.Union(Shape.Subtract(Shape.Circle(c, cy, 32 * u), Shape.Circle(c, cy, 24 * u)), Shape.Circle(c, cy, 8 * u));
                    for (int i = 0; i < 4; i++)
                    {
                        double a = i * Math.PI / 4;
                        float dx = (float)Math.Cos(a) * 24 * u, dy = (float)Math.Sin(a) * 24 * u;
                        s = Shape.Union(s, Shape.Capsule(c - dx, cy - dy, c + dx, cy + dy, 3.5f * u));
                    }
                    s = Shape.Union(s, Shape.Poly(c - 10 * u, c + 38 * u, c + 10 * u, c + 38 * u, c, c + 24 * u).Grow(2 * u));
                    break;
                }
                case Core.Icon.Heads:
                {
                    // Une tête souriante coiffée d'une couronne à trois pointes perlées : des têtes à collectionner.
                    float hx = c, hy = c - 10 * u, r = 27 * u;
                    s = Shape.Union(Shape.Circle(hx, hy, r), Shape.Circle(hx - r, hy - 2 * u, 6.5f * u), Shape.Circle(hx + r, hy - 2 * u, 6.5f * u));
                    s = Shape.Subtract(s, Shape.Union(Shape.Ellipse(hx - 9.5f * u, hy + 1 * u, 4 * u, 5.5f * u), Shape.Ellipse(hx + 9.5f * u, hy + 1 * u, 4 * u, 5.5f * u),
                        Shape.Arc(hx, hy - 4 * u, 11 * u, 205f, 335f, 4.5f * u)));
                    float cb = hy + 19 * u;
                    var crown = Shape.Union(
                        Shape.Poly(hx - 21 * u, cb, hx + 21 * u, cb, hx + 25 * u, cb + 21 * u, hx + 11 * u, cb + 10 * u, hx, cb + 25 * u, hx - 11 * u, cb + 10 * u, hx - 25 * u, cb + 21 * u).Grow(1.5f * u),
                        Shape.Circle(hx - 25 * u, cb + 22 * u, 4 * u), Shape.Circle(hx, cb + 27 * u, 4.5f * u), Shape.Circle(hx + 25 * u, cb + 22 * u, 4 * u));
                    // La couronne se détache de la tête par un liseré de la couleur du bouton ; une raie marque son bandeau.
                    s = Shape.Subtract(s, crown.Grow(5.5f * u));
                    s = Shape.Union(s, Shape.Subtract(crown, Shape.Box(hx, cb + 5 * u, 24 * u, 1.6f * u)));
                    break;
                }
                default:
                    s = Shape.Union(Shape.Capsule(c - 24 * u, c - 24 * u, c + 24 * u, c + 24 * u, 8 * u), Shape.Capsule(c - 24 * u, c + 24 * u, c + 24 * u, c - 24 * u, 8 * u)); break;
            }
            // Icônes du menu du bas : même taille de dessin et centre exact, pour un rang parfaitement aligné.
            if (Array.IndexOf(MenuIcons, icon) >= 0) s = Fit(s, n, MenuGlyph * n);
            img.Fill(s.Grow(6 * u), Ink);
            img.Fill(s, Paint.Linear(Rgba.White, 0, n, Rgba.FromHex("#E3E6F0"), 0, 0));
            Toon.DropShadow(img, 0, -(int)(4 * u), (int)(3 * u) + 1, 0.35f);
            return img;
        }

        /// <summary>
        /// Plateau de la roue de la chance (Core/LiveOps.cs, Wheel.Slots) : 8 parts de couleur séparées de liserés
        /// blancs, reflet doux, anneau doré avec 16 douilles d'ampoules (les ampoules qui clignotent sont dans
        /// l'interface). La part 0 est en haut, les suivantes dans le sens des aiguilles d'une montre.
        /// </summary>
        public static Raster WheelBoard(int n = 720)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, R = n * 0.5f - 4f, rim = n * 0.075f, r = R - rim;
            var gold = Rgba.FromHex("#FFC93C");
            img.Fill(Shape.Circle(c, c, R), Ink);
            img.Fill(Shape.Circle(c, c, R - 5f), Paint.Linear(Rgba.FromHex("#FFE27A"), 0, n, Rgba.FromHex("#D98E04"), 0, 0));
            img.Fill(Shape.Circle(c, c, r + 3f), Ink);
            int count = Wheel.Slots.Length;
            float seg = 360f / count;
            for (int i = 0; i < count; i++)
            {
                var col = Rgba.FromHex(Wheel.Slots[i].Color);
                // Angles mathématiques (0 = droite, sens inverse des aiguilles) : la part 0 centrée en haut (90°).
                float mid = 90f - i * seg;
                float a0 = (mid - seg * 0.5f) * (float)Math.PI / 180f, a1 = (mid + seg * 0.5f) * (float)Math.PI / 180f;
                var wedge = Shape.Intersect(Shape.Circle(c, c, r), Shape.Poly(c, c,
                    c + (float)Math.Cos(a0) * r * 1.5f, c + (float)Math.Sin(a0) * r * 1.5f,
                    c + (float)Math.Cos((a0 + a1) * 0.5f) * r * 1.6f, c + (float)Math.Sin((a0 + a1) * 0.5f) * r * 1.6f,
                    c + (float)Math.Cos(a1) * r * 1.5f, c + (float)Math.Sin(a1) * r * 1.5f));
                img.Fill(wedge, Paint.Custom((x, y) =>
                {
                    float d = (float)Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / r;
                    return Rgba.Lerp(col.Light(1.18f), col.Shade(0.86f), d);
                }), 1.2f);
                // Liseré blanc entre deux parts.
                img.Fill(Shape.Capsule(c, c, c + (float)Math.Cos(a0) * r, c + (float)Math.Sin(a0) * r, n * 0.006f), Rgba.FromHex("#FFF6E5"));
            }
            // Reflet en haut à gauche, comme du verre.
            img.FillClipped(Shape.Circle(c, c, r), Shape.Ellipse(c - r * 0.3f, c + r * 0.38f, r * 0.62f, r * 0.34f, 20f), Rgba.White.WithAlpha(0.16f), 6f);
            // Douilles des ampoules sur l'anneau.
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI / 8 + Math.PI / 16;
                float bx = c + (float)Math.Cos(a) * (r + rim * 0.5f), by = c + (float)Math.Sin(a) * (r + rim * 0.5f);
                img.Fill(Shape.Circle(bx, by, rim * 0.3f), Rgba.FromHex("#8A5A00"));
            }
            _ = gold;
            Toon.DropShadow(img, 0, -(int)(n * 0.012f), (int)(n * 0.01f) + 1, 0.35f);
            return img;
        }

        // ------------------------------------------------------------------ Pièces d'or et badges de l'accueil

        static readonly Rgba CoinGold = Rgba.FromHex("#FFC83D"), CoinEdge = Rgba.FromHex("#E08E0B"), CoinDeep = Rgba.FromHex("#B86A00");
        static readonly Toon.Style CoinStyle = new Toon.Style { Outline = 0.045f, Shade = 0.1f, Soft = 0.06f, ShadeStrength = 0.86f, Rim = 0.06f, RimOpacity = 0.8f, SpecSize = 0.12f };
        static readonly Toon.Style BadgeStyle = new Toon.Style { Outline = 0.03f, Shade = 0.07f, Soft = 0.045f, ShadeStrength = 0.86f, Rim = 0.04f, RimOpacity = 0.7f, SpecSize = 0.06f };

        /// <summary>Étoile à cinq branches (pointe vers le haut par défaut).</summary>
        public static Shape StarShape(float cx, float cy, float r, float inner = 0.48f, float rotDeg = 90f)
        {
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                double a = (rotDeg + i * 36) * Math.PI / 180;
                float rr = i % 2 == 0 ? r : r * inner;
                pts[i * 2] = cx + (float)Math.Cos(a) * rr;
                pts[i * 2 + 1] = cy + (float)Math.Sin(a) * rr;
            }
            return Shape.Poly(pts);
        }

        /// <summary>Une pièce d'or de face : tranche visible dessous, rebord en relief, étoile frappée, reflet.</summary>
        static void Coin(Raster img, float cx, float cy, float r)
        {
            float size = r * 2f;
            var edge = Shape.Circle(cx, cy - r * 0.17f, r);
            img.Fill(edge.Grow(size * 0.045f), CoinDeep.Outline());
            img.Fill(edge, Paint.Linear(CoinEdge, 0, cy, CoinDeep, 0, cy - r * 1.2f));
            // Crans sur la tranche.
            for (int i = -3; i <= 3; i++)
            {
                float x = cx + i * r * 0.27f;
                img.FillClipped(edge, Shape.Box(x, cy - r * 0.9f, r * 0.035f, r * 0.3f), CoinDeep.WithAlpha(0.55f), 1f);
            }
            var face = Shape.Circle(cx, cy, r);
            Toon.Part(img, face, CoinGold, size, CoinStyle);
            // Rebord intérieur en creux, puis étoile en relief (ombre en bas à droite, lumière en haut à gauche).
            img.Fill(Shape.Arc(cx, cy, r * 0.76f, 0f, 360f, r * 0.07f), CoinEdge.WithAlpha(0.85f));
            var star = StarShape(cx, cy - r * 0.02f, r * 0.46f);
            img.Fill(star.Move(r * 0.05f, -r * 0.06f), CoinDeep.WithAlpha(0.7f), 1.2f);
            img.Fill(star, Paint.Linear(Rgba.FromHex("#FFF0A8"), 0, cy + r * 0.5f, Rgba.FromHex("#FFC93C"), 0, cy - r * 0.5f), 1.2f);
            // Reflet en biais.
            img.FillClipped(face, Shape.Box(cx - r * 0.38f, cy + r * 0.2f, r * 0.09f, r * 0.9f, r * 0.05f, -38f), Rgba.White.WithAlpha(0.5f), 1.5f);
        }

        /// <summary>
        /// Tas de pièces d'or : 1, 2, 3 ou 6 pièces (plus le lot est gros, plus il y en a). Image n × n.
        /// </summary>
        public static Raster CoinPile(int count, int n = 256)
        {
            var img = new Raster(n, n);
            (float x, float y, float r)[] spots;
            switch (count)
            {
                case 1: spots = new[] { (0.5f, 0.54f, 0.34f) }; break;
                case 2: spots = new[] { (0.63f, 0.62f, 0.26f), (0.38f, 0.44f, 0.29f) }; break;
                case 3: spots = new[] { (0.5f, 0.66f, 0.24f), (0.29f, 0.4f, 0.25f), (0.71f, 0.4f, 0.25f) }; break;
                default:
                    spots = new[] { (0.3f, 0.7f, 0.18f), (0.7f, 0.7f, 0.18f), (0.5f, 0.76f, 0.19f), (0.22f, 0.4f, 0.2f), (0.78f, 0.4f, 0.2f), (0.5f, 0.42f, 0.23f) };
                    break;
            }
            foreach (var (x, y, r) in spots) Coin(img, x * n, y * n, r * n);
            Toon.DropShadow(img, 0, -(int)(n * 0.02f), (int)(n * 0.02f) + 1, 0.3f);
            return img;
        }

        /// <summary>
        /// Badge « Quête » de l'accueil : un bloc-notes sur sa planchette, pince en métal, trois cases dont deux cochées
        /// en vert, et une médaille dorée dans le coin. Image n × n.
        /// </summary>
        public static Raster QuestBadge(int n = 256)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, size = n * 0.8f;
            var board = Shape.Box(c - n * 0.03f, n * 0.47f, n * 0.3f, n * 0.39f, n * 0.06f, 4f);
            Toon.Part(img, board, Rgba.FromHex("#C9874A"), size, BadgeStyle);
            var paper = Shape.Box(c - n * 0.03f, n * 0.44f, n * 0.24f, n * 0.31f, n * 0.03f, 4f);
            Toon.Part(img, paper, Rgba.FromHex("#FFF8EA"), size, BadgeStyle);
            // Pince en métal en haut.
            var clip = Shape.Union(Shape.Box(c - n * 0.01f, n * 0.82f, n * 0.13f, n * 0.055f, n * 0.03f, 4f), Shape.Circle(c - n * 0.01f, n * 0.87f, n * 0.05f));
            Toon.Part(img, clip, Rgba.FromHex("#AEB8C6"), size, BadgeStyle);
            img.Fill(Shape.Circle(c - n * 0.01f, n * 0.875f, n * 0.02f), Rgba.FromHex("#5B6575"));
            // Trois lignes : case + trait ; les deux premières cochées.
            var green = Rgba.FromHex("#3BB54A");
            for (int i = 0; i < 3; i++)
            {
                float y = n * (0.63f - i * 0.17f) + (i - 1) * n * 0.012f;
                float bx = c - n * 0.18f + i * n * 0.012f;
                var box = Shape.Box(bx, y, n * 0.05f, n * 0.05f, n * 0.014f, 4f);
                img.Fill(box.Grow(n * 0.012f), UiArtist.Ink);
                img.Fill(box, Rgba.White);
                img.Fill(Shape.Capsule(bx + n * 0.08f, y, bx + n * 0.3f - i * n * 0.04f, y + n * 0.016f, n * 0.017f), Rgba.FromHex("#B9B2CC"));
                if (i < 2)
                {
                    var check = Shape.Union(Shape.Capsule(bx - n * 0.035f, y + n * 0.005f, bx - n * 0.005f, y - n * 0.035f, n * 0.022f),
                        Shape.Capsule(bx - n * 0.005f, y - n * 0.035f, bx + n * 0.06f, y + n * 0.065f, n * 0.022f));
                    img.Fill(check.Grow(n * 0.012f), green.Outline());
                    img.Fill(check, green);
                }
            }
            // Médaille dorée (étoile) en bas à droite : la récompense au bout.
            float mx = c + n * 0.25f, my = n * 0.2f;
            Toon.Part(img, Shape.Circle(mx, my, n * 0.13f), Rgba.FromHex("#FFC83D"), size, CoinStyle);
            img.Fill(Shape.Arc(mx, my, n * 0.095f, 0f, 360f, n * 0.012f), CoinEdge);
            img.Fill(StarShape(mx, my, n * 0.075f), Rgba.FromHex("#FFF3B0"), 1.2f);
            Toon.DropShadow(img, 0, -(int)(n * 0.02f), (int)(n * 0.02f) + 1, 0.3f);
            return img;
        }

        /// <summary>
        /// Une épée, centrée sur (cx, cy), penchée de <paramref name="tiltDeg"/> depuis la verticale (+ = vers la droite),
        /// pointe en haut : lame large à la garde et fine à la pointe, garde en travers, poignée, pommeau rond.
        /// </summary>
        public static Shape Sword(float cx, float cy, float tiltDeg, float length)
        {
            double a = tiltDeg * Math.PI / 180;
            float dx = (float)Math.Sin(a), dy = (float)Math.Cos(a); // le long de l'épée, vers la pointe
            float px = dy, py = -dx;                                // en travers
            float L = length;
            float P(float along, float across, bool x) => x ? cx + dx * along * L + px * across * L : cy + dy * along * L + py * across * L;
            var blade = Shape.Poly(
                P(-0.2f, -0.05f, true), P(-0.2f, -0.05f, false), P(0.36f, -0.042f, true), P(0.36f, -0.042f, false),
                P(0.52f, 0f, true), P(0.52f, 0f, false), P(0.36f, 0.042f, true), P(0.36f, 0.042f, false),
                P(-0.2f, 0.05f, true), P(-0.2f, 0.05f, false)).Grow(L * 0.008f);
            var guard = Shape.Capsule(P(-0.21f, -0.17f, true), P(-0.21f, -0.17f, false), P(-0.21f, 0.17f, true), P(-0.21f, 0.17f, false), L * 0.04f);
            var grip = Shape.Capsule(P(-0.23f, 0f, true), P(-0.23f, 0f, false), P(-0.4f, 0f, true), P(-0.4f, 0f, false), L * 0.036f);
            var pommel = Shape.Circle(P(-0.44f, 0f, true), P(-0.44f, 0f, false), L * 0.06f);
            return Shape.Union(blade, guard, grip, pommel);
        }

        static readonly Toon.Style CrestStyle = new Toon.Style { Outline = 0.028f, Shade = 0.09f, Soft = 0.05f, ShadeStrength = 0.84f, Rim = 0.045f, RimOpacity = 0.75f, SpecSize = 0.07f };

        /// <summary>
        /// Blason d'une division de la ligue (Core/League.cs : 0 Légende, 1 Élite, 2 Challenger, 3 Espoir). Un écu au bord
        /// de métal, de plus en plus orné : Espoir, un écu d'acier à chevrons ; Challenger, bleu, deux épées croisées ;
        /// Élite, violet, des ailes et une grande étoile ; Légende, or, des ailes, une couronne à pierres posée dessus.
        /// Image n × n.
        /// </summary>
        public static Raster LeagueCrest(int division, int n = 256)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, size = n * 0.8f;
            Rgba metal, metalDark, field, fieldLight, emblem;
            switch (division)
            {
                case 0: metal = Rgba.FromHex("#FFC83D"); metalDark = Rgba.FromHex("#D98A0B"); field = Rgba.FromHex("#4A2A8C"); fieldLight = Rgba.FromHex("#6A3FC0"); emblem = Rgba.FromHex("#FFD65C"); break;
                case 1: metal = Rgba.FromHex("#C9A2FF"); metalDark = Rgba.FromHex("#8A55E0"); field = Rgba.FromHex("#3F1F8A"); fieldLight = Rgba.FromHex("#5B33B8"); emblem = Rgba.FromHex("#FFE38A"); break;
                case 2: metal = Rgba.FromHex("#6FBBFF"); metalDark = Rgba.FromHex("#2E7FD6"); field = Rgba.FromHex("#154A94"); fieldLight = Rgba.FromHex("#1F66C4"); emblem = Rgba.FromHex("#F1F5FB"); break;
                default: metal = Rgba.FromHex("#B4C3D8"); metalDark = Rgba.FromHex("#7C8CA6"); field = Rgba.FromHex("#3D4A63"); fieldLight = Rgba.FromHex("#52627F"); emblem = Rgba.FromHex("#E4EAF3"); break;
            }
            bool wings = division <= 1, crown = division == 0;
            float cy = n * (crown ? 0.405f : division == 1 ? 0.46f : 0.5f);

            // Ailes (Élite et Légende) : trois plumes de chaque côté, derrière l'écu.
            if (wings)
                for (int sd = -1; sd <= 1; sd += 2)
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = 30f + i * 24f, len = n * (0.25f - i * 0.045f);
                        double r = ang * Math.PI / 180;
                        float bx = c + sd * n * 0.22f, by = cy + n * (0.08f - i * 0.1f);
                        float ex = bx + sd * (float)Math.Sin(r) * len, ey = by + (float)Math.Cos(r) * len;
                        // Plume : large à la base, arrondie au bout, relevée vers le haut.
                        var feather = Shape.Smooth(Shape.Tapered(bx, by, n * 0.06f, ex, ey, n * 0.045f), Shape.Circle(ex, ey + n * 0.02f, n * 0.04f), n * 0.02f);
                        Toon.Part(img, feather, i == 0 ? metal.Light(1.14f) : i == 1 ? metal : metal.Shade(0.94f), size, CrestStyle);
                    }

            // Écu : bord de métal, champ de couleur (moitié gauche plus claire), liseré intérieur.
            float w = n * 0.285f, top = cy + n * 0.26f, mid = cy - n * 0.07f, bottom = cy - n * 0.36f;
            var shield = Shape.Poly(c - w, top, c + w, top, c + w, mid, c, bottom, c - w, mid).Grow(n * 0.035f);
            Toon.Part(img, shield, metal, size, CrestStyle);
            var inner = shield.Grow(-n * 0.05f);
            img.Fill(inner.Grow(n * 0.012f), metalDark);
            img.Fill(inner, Paint.Linear(fieldLight, 0, top, field, 0, bottom));
            img.FillClipped(inner, Shape.HalfPlane(1, 0, c), field.Shade(0.86f).WithAlpha(0.55f), 1f);
            img.FillClipped(inner, Shape.Ellipse(c - n * 0.1f, top - n * 0.05f, n * 0.16f, n * 0.07f, 12f), Rgba.White.WithAlpha(0.22f), n * 0.04f);

            // Emblème.
            float ey0 = cy + n * 0.01f;
            switch (division)
            {
                case 0:
                {
                    // Grande étoile d'or sur le champ.
                    var star = StarShape(c, ey0, n * 0.17f, 0.5f);
                    Toon.Part(img, star, emblem, size, CoinStyle);
                    img.FillClipped(star, Shape.Circle(c - n * 0.04f, ey0 + n * 0.05f, n * 0.035f), Rgba.White.WithAlpha(0.6f), n * 0.02f);
                    break;
                }
                case 1:
                {
                    var star = StarShape(c, ey0, n * 0.16f, 0.5f);
                    Toon.Part(img, star, emblem, size, CoinStyle);
                    break;
                }
                case 2:
                {
                    var swords = Shape.Union(Sword(c, ey0 + n * 0.02f, 40f, n * 0.5f), Sword(c, ey0 + n * 0.02f, -40f, n * 0.5f));
                    Toon.Part(img, swords, emblem, size, CrestStyle);
                    break;
                }
                default:
                {
                    // Deux chevrons qui montent : le début du chemin.
                    for (int i = 0; i < 2; i++)
                    {
                        float y = ey0 - n * 0.1f + i * n * 0.12f;
                        var chevron = Shape.Union(Shape.Capsule(c - n * 0.12f, y, c, y + n * 0.1f, n * 0.03f), Shape.Capsule(c + n * 0.12f, y, c, y + n * 0.1f, n * 0.03f));
                        Toon.Part(img, chevron, emblem, size, CrestStyle);
                    }
                    break;
                }
            }

            // Légende : couronne à trois pointes posée sur l'écu, avec ses pierres.
            if (crown)
            {
                float by = top + n * 0.035f;
                var cr = Shape.Poly(c - n * 0.19f, by, c + n * 0.19f, by, c + n * 0.22f, by + n * 0.19f, c + n * 0.095f, by + n * 0.09f,
                    c, by + n * 0.23f, c - n * 0.095f, by + n * 0.09f, c - n * 0.22f, by + n * 0.19f).Grow(n * 0.012f);
                Toon.Part(img, cr, metal, size, CoinStyle);
                img.FillClipped(cr, Shape.Box(c, by + n * 0.02f, n * 0.2f, n * 0.022f), metalDark, 1f);
                var gems = new[] { (0f, Rgba.FromHex("#FF5A5F")), (-0.105f, Rgba.FromHex("#4AA8FF")), (0.105f, Rgba.FromHex("#3BD671")) };
                foreach (var (gx, gc) in gems)
                {
                    var gem = Shape.Circle(c + gx * n, by + n * 0.02f, n * 0.024f);
                    img.Fill(gem.Grow(n * 0.008f), metalDark.Shade(0.8f));
                    img.Fill(gem, gc);
                    img.Fill(Shape.Circle(c + gx * n - n * 0.007f, by + n * 0.027f, n * 0.008f), Rgba.White.WithAlpha(0.8f));
                }
                foreach (float tx in new[] { -0.22f, 0f, 0.22f })
                {
                    float ty = by + (tx == 0f ? 0.23f : 0.19f) * n;
                    Toon.Part(img, Shape.Circle(c + tx * n, ty + n * 0.012f, n * 0.026f), metal.Light(1.1f), size, CoinStyle);
                }
            }
            else if (division == 1)
            {
                // Élite : une petite étoile au-dessus de l'écu.
                Toon.Part(img, StarShape(c, top + n * 0.1f, n * 0.07f, 0.5f), emblem, size, CoinStyle);
            }
            Toon.DropShadow(img, 0, -(int)(n * 0.02f), (int)(n * 0.02f) + 1, 0.32f);
            return img;
        }

        /// <summary>
        /// Badge « Semaine » de l'accueil : une page de calendrier (bandeau rouge, anneaux), une grande étoile dorée
        /// au milieu (l'événement de la semaine) et trois points pour les paliers. Image n × n.
        /// </summary>
        public static Raster WeekBadge(int n = 256)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f, size = n * 0.8f;
            var page = Shape.Box(c, n * 0.45f, n * 0.36f, n * 0.34f, n * 0.07f);
            Toon.Part(img, page, Rgba.FromHex("#FFF8EA"), size, BadgeStyle);
            // Bandeau rouge du haut.
            var band = Shape.Intersect(page, Shape.HalfPlane(0, -1, -n * 0.64f));
            img.Fill(band, Paint.Linear(Rgba.FromHex("#FF6B6B"), 0, n * 0.79f, Rgba.FromHex("#E8434F"), 0, n * 0.64f));
            img.Fill(Shape.Box(c, n * 0.64f, n * 0.355f, n * 0.008f), Rgba.FromHex("#B8323C"));
            // Anneaux.
            foreach (float x in new[] { c - n * 0.18f, c + n * 0.18f })
            {
                var ring = Shape.Box(x, n * 0.8f, n * 0.035f, n * 0.08f, n * 0.035f);
                Toon.Part(img, ring, Rgba.FromHex("#AEB8C6"), size, BadgeStyle);
            }
            // Grande étoile dorée.
            var star = StarShape(c, n * 0.4f, n * 0.2f, 0.5f);
            Toon.Part(img, star, Rgba.FromHex("#FFC83D"), size, CoinStyle);
            img.FillClipped(star, Shape.Circle(c - n * 0.06f, n * 0.47f, n * 0.05f), Rgba.White.WithAlpha(0.55f), n * 0.03f);
            // Trois paliers sous l'étoile.
            for (int i = -1; i <= 1; i++)
                img.Fill(Shape.Circle(c + i * n * 0.08f, n * 0.165f, n * 0.022f), i < 1 ? Rgba.FromHex("#7C5CFF") : Rgba.FromHex("#D9D2EE"));
            Toon.DropShadow(img, 0, -(int)(n * 0.02f), (int)(n * 0.02f) + 1, 0.3f);
            return img;
        }

        /// <summary>Tube du niveau à bulle (jauge de stabilité) : vert au centre, orange puis rouge aux bouts.</summary>
        public static Raster LevelTube(int w = 640, int h = 96, float k = 1f)
        {
            w = (int)(w * k); h = (int)(h * k);
            var img = new Raster(w, h);
            float o = 6f * k;
            var frame = Shape.Box(w * 0.5f, h * 0.5f, w * 0.5f - o, h * 0.5f - o, h * 0.5f - o);
            var glass = Shape.Box(w * 0.5f, h * 0.5f, w * 0.5f - o - 12 * k, h * 0.5f - o - 12 * k, h * 0.5f - o - 12 * k);
            img.Fill(frame.Grow(o), Ink);
            img.Fill(frame, Paint.Linear(Rgba.FromHex("#FFD86B"), 0, h, Rgba.FromHex("#E0A030"), 0, 0));
            var green = Rgba.FromHex("#6FD38A"); var orange = Rgba.FromHex("#FFB547"); var red = Rgba.FromHex("#FF5A5F");
            img.Fill(glass, Paint.Custom((x, y) =>
            {
                float t = Math.Abs(x - w * 0.5f) / (w * 0.5f - o - 12 * k);
                var c = t < 0.55f ? green : t < 0.82f ? Rgba.Lerp(green, orange, (t - 0.55f) / 0.08f) : Rgba.Lerp(orange, red, (t - 0.82f) / 0.06f);
                return Rgba.Lerp(c.Light(1.15f), c.Shade(0.9f), 1f - y / h);
            }));
            // Repères au centre et aux limites.
            foreach (float t in new[] { -0.82f, -0.12f, 0.12f, 0.82f })
                img.FillClipped(glass, Shape.Box(w * 0.5f + t * (w * 0.5f - o - 12 * k), h * 0.5f, 2.5f * k, h), Ink.WithAlpha(0.55f), 1f);
            img.FillClipped(glass, Shape.Box(w * 0.5f, h * 0.68f, w, h * 0.08f, h * 0.06f), Rgba.White.WithAlpha(0.6f), 3f * k);
            return img;
        }

        /// <summary>Bulle du niveau.</summary>
        public static Raster Bubble(int n = 96)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            var b = Shape.Ellipse(c, c, n * 0.4f, n * 0.3f);
            img.Fill(b.Grow(n * 0.04f), Ink.WithAlpha(0.6f));
            img.Fill(b, Paint.Radial(Rgba.White, c - n * 0.1f, c + n * 0.08f, Rgba.FromHex("#DDF4FF"), n * 0.45f));
            img.Fill(Shape.Ellipse(c - n * 0.12f, c + n * 0.08f, n * 0.12f, n * 0.07f), Rgba.White, 2f);
            return img;
        }

        /// <summary>Voile rouge sur les bords de l'écran quand la mascotte est en danger.</summary>
        public static Raster Vignette(int n = 256)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            img.Fill(Shape.Box(c, c, c, c), Paint.Custom((x, y) =>
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                float a = Math.Max(0f, Math.Min(1f, (d - 0.55f) / 0.6f));
                return Rgba.FromHex("#FF2B3A").WithAlpha(a * a);
            }));
            return img;
        }

        /// <summary>Rayons de victoire (tournent derrière un record ou une nouvelle tête).</summary>
        // ------------------------------------------------------------------ Coffre

        static readonly Rgba ChestWood = Rgba.FromHex("#D98A46"), ChestTrim = Rgba.FromHex("#8A4A26"),
            ChestGold = Rgba.FromHex("#FFD447"), ChestGem = Rgba.FromHex("#FF4D6D"), ChestGemBlue = Rgba.FromHex("#3FB4FF");
        static readonly Toon.Style ChestFlat = new Toon.Style { Outline = 0.022f, Shade = 0.08f, Soft = 0.05f, Rim = 0.03f, Specular = false };
        static readonly Toon.Style ChestShiny = new Toon.Style { Outline = 0.022f, Shade = 0.07f, Soft = 0.04f, ShadeStrength = 0.86f, Rim = 0.05f, RimOpacity = 0.8f, SpecSize = 0.05f };

        /// <summary>
        /// Corps du coffre, vu de face : planches de bois verticales veinées, cadre de bois foncé, coins dorés rivetés,
        /// grosse serrure dorée avec une pierre. Sur le dessus, l'intérieur lumineux (caché par le couvercle fermé).
        /// Image n × 0,66 n ; la charnière (haut du corps) est à 0,53 n.
        /// </summary>
        public static Raster ChestBase(int n = 512)
        {
            var img = new Raster(n, (int)(n * 0.66f));
            float c = n * 0.5f, size = n * 0.8f;
            float x0 = c - n * 0.4f, x1 = c + n * 0.4f, y0 = n * 0.06f, y1 = n * 0.53f;
            // Ombre au sol.
            img.Fill(Shape.Ellipse(c, n * 0.045f, n * 0.43f, n * 0.04f), Rgba.FromHex("#1E1440").WithAlpha(0.35f), n * 0.03f);
            var body = Shape.Box(c, (y0 + y1) * 0.5f, n * 0.4f, (y1 - y0) * 0.5f, n * 0.035f);
            img.Fill(body.Grow(n * 0.022f), ChestWood.Outline());
            img.Fill(body, Planks(c, n, ChestWood));
            // Volume : ombre douce en bas à droite, lumière en haut à gauche.
            img.FillClipped(body, body.Move(-n * 0.03f, n * 0.05f).Not(), ChestWood.Shadow(0.7f), n * 0.06f, 0.7f);
            img.FillClipped(body, body.Move(n * 0.015f, -n * 0.02f).Not(), ChestWood.Light(1.3f), n * 0.02f, 0.5f);
            // Cadre de bois foncé : bandeau du haut, socle, montants des côtés.
            Toon.Part(img, Shape.Box(c, y1 - n * 0.035f, n * 0.405f, n * 0.04f, n * 0.02f), ChestTrim, size, ChestFlat);
            Toon.Part(img, Shape.Box(c, y0 + n * 0.035f, n * 0.415f, n * 0.042f, n * 0.022f), ChestTrim, size, ChestFlat);
            foreach (float x in new[] { x0 + n * 0.035f, x1 - n * 0.035f })
                Toon.Part(img, Shape.Box(x, (y0 + y1) * 0.5f, n * 0.035f, (y1 - y0) * 0.5f - n * 0.02f, n * 0.015f), ChestTrim, size, ChestFlat);
            // Bande d'or verticale au centre (sous la serrure), rivetée.
            var band = Shape.Box(c, (y0 + y1) * 0.5f, n * 0.06f, (y1 - y0) * 0.5f - n * 0.01f, n * 0.012f);
            Toon.Part(img, band, ChestGold, size, ChestShiny);
            img.FillClipped(band, Shape.Box(c - n * 0.035f, (y0 + y1) * 0.5f, n * 0.008f, n), Rgba.White.WithAlpha(0.45f), 1.5f);
            foreach (float y in new[] { y0 + n * 0.09f, y1 - n * 0.09f }) Rivet(img, c, y, n);
            // Coins dorés en équerre, avec rivets.
            foreach (var (cx, cy, sx, sy) in new[] { (x0, y1, 1f, -1f), (x1, y1, -1f, -1f), (x0, y0, 1f, 1f), (x1, y0, -1f, 1f) })
                Corner(img, cx, cy, sx, sy, n, size);
            // Pierres bleues serties sur les montants.
            foreach (float x in new[] { x0 + n * 0.035f, x1 - n * 0.035f })
            {
                Toon.Part(img, Shape.Circle(x, (y0 + y1) * 0.5f, n * 0.03f), ChestGold, size, ChestShiny);
                Toon.Part(img, Shape.Ellipse(x, (y0 + y1) * 0.5f, n * 0.02f, n * 0.024f), ChestGemBlue, size, ChestShiny);
                img.Fill(Shape.Circle(x - n * 0.007f, (y0 + y1) * 0.5f + n * 0.009f, n * 0.006f), Rgba.White.WithAlpha(0.9f));
            }
            // Intérieur : bord de bois foncé, fond sombre, lueur d'or.
            var rim = Shape.Ellipse(c, n * 0.555f, n * 0.39f, n * 0.07f);
            Toon.Part(img, rim, ChestTrim, size, ChestFlat);
            var hole = Shape.Ellipse(c, n * 0.56f, n * 0.355f, n * 0.05f);
            img.Fill(hole, Paint.Radial(Rgba.FromHex("#FFFBE0"), c, n * 0.56f, Rgba.FromHex("#FFB347"), n * 0.36f));
            // Serrure : écusson doré, trou de serrure, pierre rouge.
            var plate = Shape.Union(Shape.Box(c, n * 0.43f, n * 0.075f, n * 0.06f, n * 0.03f), Shape.Circle(c, n * 0.37f, n * 0.075f));
            Toon.Part(img, plate, ChestGold, size, ChestShiny);
            Toon.Part(img, Shape.Circle(c, n * 0.445f, n * 0.032f), ChestGem, size, ChestShiny);
            img.Fill(Shape.Circle(c - n * 0.01f, n * 0.455f, n * 0.009f), Rgba.White.WithAlpha(0.9f));
            img.Fill(Shape.Union(Shape.Circle(c, n * 0.37f, n * 0.022f), Shape.Poly(c - n * 0.016f, n * 0.37f, c + n * 0.016f, n * 0.37f, c + n * 0.024f, n * 0.315f, c - n * 0.024f, n * 0.315f)), Rgba.FromHex("#3A2210"));
            return img;
        }

        /// <summary>Couvercle bombé du coffre (planches, bord foncé, sangles dorées). Image n × 0,42 n ; charnière à 0,03 n.</summary>
        public static Raster ChestLid(int n = 512)
        {
            var img = new Raster(n, (int)(n * 0.42f));
            float c = n * 0.5f, size = n * 0.8f;
            var lid = Shape.Union(Shape.Box(c, n * 0.12f, n * 0.405f, n * 0.09f, n * 0.03f), Shape.Ellipse(c, n * 0.19f, n * 0.405f, n * 0.19f));
            lid = Shape.Intersect(lid, Shape.HalfPlane(0, -1, -n * 0.03f));
            img.Fill(lid.Grow(n * 0.022f), ChestWood.Outline());
            img.Fill(lid, Planks(c, n, ChestWood));
            img.FillClipped(lid, lid.Move(-n * 0.03f, n * 0.05f).Not(), ChestWood.Shadow(0.7f), n * 0.06f, 0.7f);
            img.FillClipped(lid, lid.Move(n * 0.015f, -n * 0.02f).Not(), ChestWood.Light(1.3f), n * 0.02f, 0.5f);
            // Reflet sur le dôme.
            img.FillClipped(lid, Shape.Ellipse(c - n * 0.12f, n * 0.33f, n * 0.16f, n * 0.03f, 8f), Rgba.White.WithAlpha(0.35f), n * 0.03f);
            // Sangles dorées près des bouts, avec rivets.
            foreach (float x in new[] { c - n * 0.29f, c + n * 0.29f })
            {
                Toon.Part(img, Shape.Intersect(lid, Shape.Box(x, n * 0.2f, n * 0.038f, n)), ChestGold, size, ChestShiny);
                foreach (float y in new[] { n * 0.1f, n * 0.22f }) Rivet(img, x, y, n);
            }
            // Bande d'or centrale et étoile dorée sertie d'une pierre : un coffre de trésor, pas une caisse.
            var lband = Shape.Intersect(lid, Shape.Box(c, n * 0.2f, n * 0.06f, n));
            Toon.Part(img, lband, ChestGold, size, ChestShiny);
            img.FillClipped(lband, Shape.Box(c - n * 0.035f, n * 0.2f, n * 0.008f, n), Rgba.White.WithAlpha(0.45f), 1.5f);
            float sy = n * 0.25f;
            var star = Star(c, sy, n * 0.085f, n * 0.04f);
            Toon.Part(img, star.Grow(n * 0.012f), ChestGold.Shade(0.6f), size, ChestFlat);
            Toon.Part(img, star, ChestGold, size, ChestShiny);
            Toon.Part(img, Shape.Circle(c, sy, n * 0.026f), ChestGemBlue, size, ChestShiny);
            img.Fill(Shape.Circle(c - n * 0.008f, sy + n * 0.008f, n * 0.007f), Rgba.White.WithAlpha(0.9f));
            // Bord bas en bois foncé, et le moraillon doré qui descend sur la serrure.
            Toon.Part(img, Shape.Box(c, n * 0.065f, n * 0.415f, n * 0.038f, n * 0.02f), ChestTrim, size, ChestFlat);
            Toon.Part(img, Shape.Box(c, n * 0.07f, n * 0.045f, n * 0.07f, n * 0.022f), ChestGold, size, ChestShiny);
            Rivet(img, c, n * 0.1f, n);
            return img;
        }

        /// <summary>Planches verticales légèrement différentes, rainures sombres et veinage.</summary>
        static Paint Planks(float c, int n, Rgba wood)
        {
            float pw = n * 0.135f;
            return Paint.Custom((x, y) =>
            {
                float u = (x - c) / pw + 0.5f;
                int k = (int)Math.Floor(u);
                float f = u - k;
                var col = Rgba.Lerp(wood, wood.Shade(0.9f), ((k * 37 + 11) & 3) / 6f);
                col = Rgba.Lerp(col, wood.Light(1.12f), Math.Max(0f, Math.Min(1f, (y - n * 0.1f) / (n * 0.5f))) * 0.4f); // plus clair en haut
                // Veinage : de fines lignes irrégulières, différentes sur chaque planche.
                // Veinage : fines lignes dans le sens de la planche, qui ondulent à peine.
                float g = (float)Math.Sin(f * (17f + 5f * (k & 1)) + Math.Sin(y * 0.018f + k * 1.9f) * 1.4f + k * 2.3f);
                if (g > 0.9f && f > 0.15f && f < 0.9f) col = Rgba.Lerp(col, wood.Shade(0.8f), (g - 0.9f) * 6f);
                if (f < 0.045f || f > 0.955f) col = wood.Shade(0.55f);
                else if (f < 0.12f) col = Rgba.Lerp(col, wood.Light(1.15f), 0.35f);
                return col;
            });
        }

        static void Corner(Raster img, float cx, float cy, float sx, float sy, int n, float size)
        {
            float a = n * 0.11f, t = n * 0.04f;
            var arm1 = Shape.Box(cx + sx * a * 0.5f, cy + sy * t * 0.5f, a * 0.5f, t * 0.5f + n * 0.008f, n * 0.012f);
            var arm2 = Shape.Box(cx + sx * t * 0.5f, cy + sy * a * 0.5f, t * 0.5f + n * 0.008f, a * 0.5f, n * 0.012f);
            Toon.Part(img, Shape.Union(arm1, arm2), ChestGold, size, ChestShiny);
            Rivet(img, cx + sx * t * 0.55f, cy + sy * t * 0.55f, n);
            Rivet(img, cx + sx * a * 0.75f, cy + sy * t * 0.55f, n);
            Rivet(img, cx + sx * t * 0.55f, cy + sy * a * 0.75f, n);
        }

        /// <summary>Étoile à 5 branches (rayons extérieur et intérieur).</summary>
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

        static void Rivet(Raster img, float x, float y, int n)
        {
            float r = n * 0.012f;
            img.Fill(Shape.Circle(x, y, r * 1.35f), ChestGold.Shade(0.55f));
            img.Fill(Shape.Circle(x, y, r), Paint.Radial(Rgba.FromHex("#FFF3C2"), x - r * 0.4f, y + r * 0.4f, ChestGold.Shade(0.85f), r * 1.2f));
        }

        public static Raster Rays(int n = 512)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            for (int i = 0; i < 12; i++)
            {
                double a0 = i * Math.PI * 2 / 12, a1 = a0 + Math.PI / 12;
                img.Fill(Shape.Poly(c, c, c + (float)Math.Cos(a0) * n, c + (float)Math.Sin(a0) * n, c + (float)Math.Cos(a1) * n, c + (float)Math.Sin(a1) * n),
                    Paint.Radial(Rgba.White.WithAlpha(0.9f), c, c, Rgba.White.WithAlpha(0f), c), 2f);
            }
            return img;
        }
    }
}

namespace Bascule.Core
{
    /// <summary>
    /// Icône de l'application : Basile qui tient sa planche en équilibre, sur un ciel ensoleillé.
    /// Carré plein, sans transparence (exigé par l'App Store ; Android et iOS arrondissent eux-mêmes les coins).
    /// Les éléments importants restent dans le cercle central (zone sûre des icônes adaptatives Android).
    /// </summary>
    public static class IconArtist
    {
        /// <summary>
        /// Basile, en plein effort, tient sur sa tête une planche qui penche, chargée de poids colorés,
        /// devant un soleil qui rayonne. Tout tient dans le cercle central (icônes rondes Android).
        /// </summary>
        public static Raster AppIcon(int n = 1024)
        {
            var img = new Raster(n, n);
            float c = n * 0.5f;
            // Fond : soleil chaud qui rayonne (donne de l'énergie, se voit de loin sur un écran d'accueil).
            img.Fill(Shape.Box(c, c, c + 2, c + 2), Paint.Radial(Rgba.FromHex("#FFE27A"), c, n * 0.52f, Rgba.FromHex("#FF8A3D"), n * 0.75f));
            var rays = UiArtist.Rays(n);
            for (int i = 0; i < rays.Pixels.Length; i++) { var p = rays.Pixels[i]; rays.Pixels[i] = new Rgba(255, 246, 214, (byte)(p.a * 0.35f)); }
            img.Composite(rays);

            // Tête de Basile, centrée : grand sourire (il adore ça), avec une goutte de sueur (ça penche !).
            int hs = (int)(n * 0.66f);
            var head = HeadArtist.Portrait(HeadCatalog.Get(0), hs, Expression.Grin);
            float headCx = c, headCy = n * 0.33f;
            img.CompositeAt(head, (int)(headCx - hs * 0.5f), (int)(headCy - hs * 0.44f));
            // Goutte de sueur.
            float r = hs * 0.29f;
            var drop = Shape.Smooth(Shape.Circle(headCx + r * 0.95f, headCy + r * 0.42f, r * 0.12f), Shape.Tapered(headCx + r * 0.95f, headCy + r * 0.48f, r * 0.11f, headCx + r * 0.95f, headCy + r * 0.72f, r * 0.01f), r * 0.05f);
            Toon.Part(img, drop, Rgba.FromHex("#8FD3FF"), n * 0.2f, new Toon.Style { Outline = 0.04f, Shade = 0.15f, Soft = 0.1f, Rim = 0f, Specular = true, SpecSize = 0.2f });

            // Planche qui penche, posée sur la tête.
            // Posée SUR le haut des cheveux (sommet de la coiffure à environ 1,3 rayon), pour ne pas la couper à plat.
            float tilt = -8f, th = n * 0.036f, py = headCy + r * 1.33f + th * 0.5f, half = n * 0.31f;
            var st = new Toon.Style { Outline = 0.022f, Shade = 0.2f, Soft = 0.1f, Rim = 0.05f, RimOpacity = 0.5f, Specular = true, SpecSize = 0.06f };
            Toon.Part(img, Shape.Box(c, py, half, th * 0.5f, th * 0.45f, tilt), Rgba.FromHex("#C4834F"), n, st);
            float slope = (float)Math.Tan(tilt * Math.PI / 180);
            float Top(float x) => py + th * 0.5f + (x - c) * slope;

            // Poids posés dessus, en couleurs froides pour ressortir sur le fond chaud : un gros bleu à gauche,
            // une pile violet + turquoise à droite.
            Put(img, 32, c - half * 0.55f, Top(c - half * 0.55f), n * 0.21f);
            float rx = c + half * 0.5f;
            float h2 = Put(img, 16, rx, Top(rx), n * 0.16f);
            Put(img, 64, rx + n * 0.005f, h2, n * 0.13f);
            return img;
        }

        /// <summary>Pose un poids (texture du jeu) avec le bas de son corps à la hauteur y ; renvoie la hauteur de son sommet.</summary>
        static float Put(Raster img, int value, float x, float y, float size)
        {
            int s = (int)size;
            var w = WeightArtist.Draw(value, s);
            img.CompositeAt(w, (int)(x - s * 0.5f), (int)(y - s * WeightArtist.BodyBottom));
            return y + s * (WeightArtist.BodyTop - WeightArtist.BodyBottom);
        }
    }
}
