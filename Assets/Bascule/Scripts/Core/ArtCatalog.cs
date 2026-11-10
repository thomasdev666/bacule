using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bascule.Core
{
    /// <summary>
    /// Liste de toutes les images du jeu, avec leur nom de cache. Elles sont dessinées à l'avance par
    /// Tools/ArtPack (dans Assets/Bascule/Resources/ArtPack) : au lancement, le jeu les charge au lieu de les
    /// dessiner, d'où un démarrage instantané et fluide (surtout en version web, sans fil d'arrière-plan).
    /// Les noms doivent rester identiques à ceux qu'utilise Runtime/Game/ArtCache.cs et Backdrop.cs.
    /// </summary>
    public static class ArtCatalog
    {
        public const int HeadSize = 384;
        public const int PortraitSize = 384;
        public const int WeightTexture = 256;
        /// <summary>Finesse de l'interface : textures 2 fois plus grandes que la référence 1080 px.</summary>
        public const float UiDensity = 2f;

        public struct Job
        {
            public string Key;
            public Func<Raster> Draw;
            public Job(string key, Func<Raster> draw) { Key = key; Draw = draw; }
        }

        /// <summary>Planche de l'écran de démarrage (BootCurtain), avec ses rebords : prête dans l'ArtPack.</summary>
        public const string BootPlankKey = "boot_plank";
        public const float BootPlankPpu = 200f;
        public static Raster BootPlank() => SceneArtist.PlankPreview(PlankCatalog.Get(0), 6.6f, 0.3f, BootPlankPpu, 0.12f, 0.22f);

        public static IEnumerable<Job> All()
        {
            foreach (var j in Shapes()) yield return j;
            foreach (var j in Ui()) yield return j;
            foreach (var j in Gameplay()) yield return j;
            foreach (var j in Heads()) yield return j;
            for (int i = 0; i < Theme.All.Length; i++) foreach (var j in Scene(i)) yield return j;
            foreach (var skin in Skins()) foreach (var j in Mascot(skin, out _)) yield return j;
        }

        public static IEnumerable<Job> Shapes()
        {
            yield return new Job("circle", () => ShapeArtist.Circle(128));
            yield return new Job("softdisc", () => ShapeArtist.SoftDisc(128));
            yield return new Job("capsule", () => ShapeArtist.Capsule(256, 64));
            yield return new Job("rounded", () => ShapeArtist.RoundedSquare(128, 0.25f));
            yield return new Job("spark", () => ShapeArtist.Spark(64));
            yield return new Job("ring", () => ShapeArtist.Ring(128));
            yield return new Job("puff", () => ShapeArtist.Puff(96));
            yield return new Job("sweat", () => ShapeArtist.SweatDrop(64));
            yield return new Job("confetti", () => ShapeArtist.Confetti(32));
            yield return new Job("balloon", () => ShapeArtist.Balloon(256, WeightCatalog.Color(128)));
        }

        public static IEnumerable<Job> Ui()
        {
            const float k = UiDensity;
            yield return new Job("ui_button", () => UiArtist.Button(192, 200, k));
            yield return new Job("ui_panel", () => UiArtist.Panel(200, k));
            yield return new Job("ui_pill", () => UiArtist.Pill(160, 96, k));
            yield return new Job("ui_glass", () => UiArtist.Glass(160, 96, k));
            yield return new Job("ui_gloss", () => UiArtist.ButtonGloss(192, 200, k));
            yield return new Job("ui_level", () => UiArtist.LevelTube(640, 96, k));
            yield return new Job("scene_" + BootPlankKey, BootPlank);
            yield return new Job("ui_chest_base", () => UiArtist.ChestBase(512));
            yield return new Job("ui_chest_lid", () => UiArtist.ChestLid(512));
            yield return new Job("ui_bubble", () => UiArtist.Bubble((int)(96 * k)));
            yield return new Job("ui_vignette", () => UiArtist.Vignette());
            yield return new Job("ui_rays", () => UiArtist.Rays());
            foreach (Icon icon in Enum.GetValues(typeof(Icon)))
            {
                var ic = icon;
                yield return new Job("icon_" + ic, () => UiArtist.DrawIcon(ic, (int)(128 * UiDensity)));
            }
        }

        public static IEnumerable<Job> Gameplay()
        {
            foreach (var v in WeightCatalog.Values)
            {
                int value = v;
                yield return new Job("weight" + value, () => WeightArtist.Draw(value, WeightTexture));
            }
            foreach (Expression e in Enum.GetValues(typeof(Expression)))
            {
                var ex = e;
                yield return new Job("face_" + ex, () => HeadArtist.Face(ex, HeadSize));
            }
            yield return new Job("flush", () => HeadArtist.Flush(HeadSize));
        }

        public static IEnumerable<Job> Heads()
        {
            foreach (var h in HeadCatalog.All)
            {
                var head = h;
                yield return new Job("headback" + head.Id, () => HeadArtist.Back(head, HeadSize));
                yield return new Job("headfront" + head.Id, () => HeadArtist.Front(head, HeadSize));
                yield return new Job("portrait" + head.Id, () => HeadArtist.Portrait(head, PortraitSize));
                yield return new Job("locked" + head.Id, () => HeadArtist.Portrait(head, PortraitSize).Silhouette(Rgba.FromHex("#2B2D42", 80)));
            }
        }

        /// <summary>Images d'une ambiance du décor (mêmes noms et tailles que Backdrop.SceneItems).</summary>
        public static IEnumerable<Job> Scene(int index)
        {
            var t = Theme.All[index];
            string k = "scene_t" + index;
            yield return new Job(k + "sky", () => SceneArtist.Sky(t, 1024));
            yield return new Job(k + "sun", () => SceneArtist.Sun(t, 512));
            if (t.Night) yield return new Job("scene_stars", () => SceneArtist.Stars());
            for (int i = 0; i < 3 && !t.Space; i++)
            {
                int ci = i;
                yield return new Job(k + "cloud" + ci, () => SceneArtist.Cloud(ci, t, 512, 256));
            }
            yield return new Job(k + "far", () => SceneArtist.Hills(t, true, 1536, 480));
            yield return new Job(k + "near", () => SceneArtist.Hills(t, false, 1536, 480));
            yield return new Job(k + "ground", () => SceneArtist.Ground(t, 1536, 540));
        }

        /// <summary>Toutes les couleurs de peau des têtes (le corps de Basile prend celle de la tête portée).</summary>
        public static IEnumerable<string> Skins()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in HeadCatalog.All) if (seen.Add(h.BodyKey)) yield return h.BodyKey;
        }

        public static string MascotKey(string skinHex, string part) => "mascot_" + skinHex.TrimStart('#') + "_" + part;

        /// <summary>Parties du corps pour une peau ; meta reçoit les lignes « partie pivotX pivotY ppu » (fichier .meta).</summary>
        public static IEnumerable<Job> Mascot(string skinHex, out List<string> meta)
        {
            var jobs = new List<Job>();
            meta = new List<string>();
            foreach (var kv in MascotArtist.Build(skinHex))
            {
                var p = kv.Value;
                var img = p.Image;
                jobs.Add(new Job(MascotKey(skinHex, kv.Key), () => img));
                meta.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", kv.Key, p.PivotX / p.Image.Width, p.PivotY / p.Image.Height, p.PixelsPerUnit));
            }
            return jobs;
        }
    }
}
