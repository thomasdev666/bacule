using System;

namespace Bascule.Core
{
    public enum Icon
    {
        Pause, Play, Home, Trophy, Gear, Face, Share, Replay, Film, Flame, Star, Lock, Check, Back, Crown, Sound, SoundOff, Vibrate, Calendar, Close, Swords, Heart,
        Coin, Gift, Bag, Target, Hand, Bell, Plus, Flag,
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
                    s = Shape.Union(Shape.Capsule(c - 26 * u, c - 26 * u, c + 26 * u, c + 26 * u, 6 * u), Shape.Capsule(c + 26 * u, c - 26 * u, c - 26 * u, c + 26 * u, 6 * u),
                        Shape.Capsule(c - 30 * u, c - 14 * u, c - 14 * u, c - 30 * u, 6 * u), Shape.Capsule(c + 30 * u, c - 14 * u, c + 14 * u, c - 30 * u, 6 * u)); break;
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
                default:
                    s = Shape.Union(Shape.Capsule(c - 24 * u, c - 24 * u, c + 24 * u, c + 24 * u, 8 * u), Shape.Capsule(c - 24 * u, c + 24 * u, c + 24 * u, c - 24 * u, 8 * u)); break;
            }
            img.Fill(s.Grow(6 * u), Ink);
            img.Fill(s, Paint.Linear(Rgba.White, 0, n, Rgba.FromHex("#E3E6F0"), 0, 0));
            Toon.DropShadow(img, 0, -(int)(4 * u), (int)(3 * u) + 1, 0.35f);
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
