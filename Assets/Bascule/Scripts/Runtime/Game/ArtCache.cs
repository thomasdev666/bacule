using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule
{
    /// <summary>
    /// Transforme les dessins de Bascule.Core (Raster) en sprites Unity, à la demande, et les garde en cache.
    /// Tous les visuels du jeu viennent d'ici : aucune image à importer.
    /// </summary>
    public sealed class ArtCache
    {
        public const int HeadSize = ArtCatalog.HeadSize;
        public const int PortraitSize = ArtCatalog.PortraitSize;
        /// <summary>
        /// Finesse de l'interface : textures dessinées 2 fois plus grandes que leur taille de référence (1080 px de large),
        /// pour rester nettes sur les écrans haute densité (iPhone Pro, iPad). Rien n'est compressé.
        /// </summary>
        public const float UiDensity = ArtCatalog.UiDensity;
        public const int WeightTexture = ArtCatalog.WeightTexture;

        public Sprite Circle { get; private set; }
        public Sprite SoftDisc { get; private set; }
        public Sprite Capsule { get; private set; }
        public Sprite Rounded { get; private set; }
        public Sprite Spark { get; private set; }
        public Sprite Ring { get; private set; }
        public Sprite Puff { get; private set; }
        public Sprite Sweat { get; private set; }
        public Sprite Confetti { get; private set; }
        public Sprite Balloon { get; private set; }
        public Sprite Pixel { get; private set; }

        // Interface
        public Sprite Button { get; private set; }
        public Sprite Panel { get; private set; }
        public Sprite Pill { get; private set; }
        /// <summary>Verre liquide (à afficher en blanc) et reflet liquide des boutons.</summary>
        public Sprite Glass { get; private set; }
        public Sprite Gloss { get; private set; }
        public Sprite LevelTube { get; private set; }
        public Sprite Bubble { get; private set; }
        public Sprite Vignette { get; private set; }
        public Sprite Rays { get; private set; }
        public Sprite ChestBase { get; private set; }
        public Sprite ChestLid { get; private set; }

        readonly Dictionary<int, Sprite> _weights = new Dictionary<int, Sprite>();
        readonly Dictionary<int, Sprite> _headBack = new Dictionary<int, Sprite>();
        readonly Dictionary<int, Sprite> _headFront = new Dictionary<int, Sprite>();
        readonly Dictionary<int, Sprite> _portrait = new Dictionary<int, Sprite>();
        readonly Dictionary<int, Sprite> _locked = new Dictionary<int, Sprite>();
        readonly Dictionary<Expression, Sprite> _faces = new Dictionary<Expression, Sprite>();
        readonly Dictionary<Icon, Sprite> _icons = new Dictionary<Icon, Sprite>();
        readonly Dictionary<string, Dictionary<string, Sprite>> _mascot = new Dictionary<string, Dictionary<string, Sprite>>();
        readonly Dictionary<string, Sprite> _scene = new Dictionary<string, Sprite>();

        public void Build()
        {
            Circle = Cached("circle", () => ShapeArtist.Circle(128), 128, Vector4.zero);
            SoftDisc = Cached("softdisc", () => ShapeArtist.SoftDisc(128), 128, Vector4.zero);
            // Capsule horizontale 9-slice : les bouts restent ronds quand on l'étire.
            Capsule = Cached("capsule", () => ShapeArtist.Capsule(256, 64), 64, new Vector4(32, 0, 32, 0));
            Rounded = Cached("rounded", () => ShapeArtist.RoundedSquare(128, 0.25f), 128, new Vector4(40, 40, 40, 40));
            Spark = Cached("spark", () => ShapeArtist.Spark(64), 64, Vector4.zero);
            Ring = Cached("ring", () => ShapeArtist.Ring(128), 128, Vector4.zero);
            Puff = Cached("puff", () => ShapeArtist.Puff(96), 96, Vector4.zero);
            Sweat = Cached("sweat", () => ShapeArtist.SweatDrop(64), 64 / 0.22f, Vector4.zero);
            Confetti = Cached("confetti", () => ShapeArtist.Confetti(32), 32 / 0.18f, Vector4.zero);
            Balloon = Cached("balloon", () => ShapeArtist.Balloon(256, WeightCatalog.Color(128)), 256 / 2.8f, Vector4.zero);
            var px = new Raster(4, 4); px.Fill(Rgba.White);
            Pixel = ToSprite(px, 4, Vector4.zero);

            // Les sprites d'interface sont en « pixels de référence » (Canvas 1080 de large) : 100 px par unité.
            const float k = UiDensity, ppu = 100 * UiDensity;
            Button = Cached("ui_button", () => UiArtist.Button(192, 200, k), ppu, new Vector4(64, 66, 64, 64) * k);
            Panel = Cached("ui_panel", () => UiArtist.Panel(200, k), ppu, new Vector4(70, 70, 70, 70) * k);
            Pill = Cached("ui_pill", () => UiArtist.Pill(160, 96, k), ppu, new Vector4(46, 46, 46, 46) * k);
            Glass = Cached("ui_glass", () => UiArtist.Glass(160, 96, k), ppu, new Vector4(46, 46, 46, 46) * k);
            Gloss = Cached("ui_gloss", () => UiArtist.ButtonGloss(192, 200, k), ppu, new Vector4(64, 66, 64, 64) * k);
            LevelTube = Cached("ui_level", () => UiArtist.LevelTube(640, 96, k), ppu, Vector4.zero);
            Bubble = Cached("ui_bubble", () => UiArtist.Bubble((int)(96 * k)), ppu, Vector4.zero);
            Vignette = Cached("ui_vignette", () => UiArtist.Vignette(), 100, Vector4.zero);
            Rays = Cached("ui_rays", () => UiArtist.Rays(), 100, Vector4.zero);
            ChestBase = Cached("ui_chest_base", () => UiArtist.ChestBase(512), 100, Vector4.zero);
            ChestLid = Cached("ui_chest_lid", () => UiArtist.ChestLid(512), 100, Vector4.zero, new Vector2(0.5f, 0.03f / 0.42f));
        }

        // ------------------------------------------------------------------ Poids

        /// <summary>Sprite du poids : 1 unité = largeur de la texture ; pivot au centre du corps.</summary>
        public Sprite Weight(int value)
        {
            value = WeightCatalog.Normalize(value);
            if (_weights.TryGetValue(value, out var s)) return s;
            s = Cached("weight" + value, () => WeightArtist.Draw(value, WeightTexture), WeightTexture, Vector4.zero, new Vector2(0.5f, WeightArtist.BodyCenterY));
            _weights[value] = s;
            return s;
        }

        // ------------------------------------------------------------------ Têtes

        float HeadPpu => HeadSize / MascotRig.HeadSize;

        public Sprite HeadBack(HeadDef h) => Get(_headBack, "headback", h.Id, () => HeadArtist.Back(h, HeadSize), HeadPpu);
        public Sprite HeadFront(HeadDef h) => Get(_headFront, "headfront", h.Id, () => HeadArtist.Front(h, HeadSize), HeadPpu);
        /// <summary>Portrait déjà en mémoire, ou null (la collection l'affichera dès qu'il sera prêt, sans bloquer).</summary>
        public Sprite PortraitIfLoaded(HeadDef h, bool locked) => (locked ? _locked : _portrait).TryGetValue(h.Id, out var s) ? s : null;

        /// <summary>Prêt à charger sans dessin (en mémoire ou dans le cache disque).</summary>
        public bool PortraitReady(HeadDef h, bool locked) => PortraitIfLoaded(h, locked) != null || IsReady((locked ? "locked" : "portrait") + h.Id);

        public Sprite Portrait(HeadDef h) => Get(_portrait, "portrait", h.Id, () => HeadArtist.Portrait(h, PortraitSize), 100 * PortraitSize / 256f);
        public Sprite Locked(HeadDef h) => Get(_locked, "locked", h.Id, () => HeadArtist.Portrait(h, PortraitSize).Silhouette(Rgba.FromHex("#2B2D42", 80)), 100 * PortraitSize / 256f);

        public Sprite Flush => _flush ?? (_flush = Cached("flush", () => HeadArtist.Flush(HeadSize), HeadPpu, Vector4.zero));
        Sprite _flush;

        public Sprite Face(Expression e)
        {
            if (_faces.TryGetValue(e, out var s)) return s;
            s = Cached("face_" + e, () => HeadArtist.Face(e, HeadSize), HeadPpu, Vector4.zero);
            _faces[e] = s;
            return s;
        }

        Sprite Get(Dictionary<int, Sprite> cache, string kind, int id, System.Func<Raster> draw, float ppu)
        {
            if (cache.TryGetValue(id, out var s)) return s;
            s = Cached(kind + id, draw, ppu, Vector4.zero);
            cache[id] = s;
            return s;
        }

        // ------------------------------------------------------------------ Mascotte

        /// <summary>Pièces du corps pour une couleur de peau (bras, jambes, torse…), pivot sur l'articulation.</summary>
        public Dictionary<string, Sprite> MascotParts(string skinHex)
        {
            if (_mascot.TryGetValue(skinHex, out var parts)) return parts;
            parts = new Dictionary<string, Sprite>();
            string skin = skinHex.TrimStart('#');
            // Images dessinées à l'avance, avec leurs pivots.
            var pivots = Resources.Load<TextAsset>("ArtPack/mascot_" + skin + "_pivots");
            if (pivots != null)
            {
                foreach (var line in pivots.text.Split('\n'))
                {
                    var f = line.Trim().Split(' ');
                    if (f.Length < 4) continue;
                    var ci = System.Globalization.CultureInfo.InvariantCulture;
                    var s = FromPack(ArtCatalog.MascotKey(skinHex, f[0]), float.Parse(f[3], ci), Vector4.zero, new Vector2(float.Parse(f[1], ci), float.Parse(f[2], ci)));
                    if (s == null) { parts.Clear(); break; }
                    parts[f[0]] = s;
                }
                if (parts.Count > 0) { _mascot[skinHex] = parts; return parts; }
            }
            // Déjà dessiné (fichiers et pivots en cache) : on recharge sans rien redessiner.
            string meta = System.IO.Path.Combine(CacheDir, "mascot_" + skin + ".meta");
            try
            {
                if (System.IO.File.Exists(meta))
                {
                    foreach (var line in System.IO.File.ReadAllLines(meta))
                    {
                        var f = line.Split(' ');
                        if (f.Length < 4) continue;
                        float px = float.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture);
                        float py = float.Parse(f[2], System.Globalization.CultureInfo.InvariantCulture);
                        float ppu = float.Parse(f[3], System.Globalization.CultureInfo.InvariantCulture);
                        var s = TryLoad("mascot_" + skin + "_" + f[0], ppu, Vector4.zero, new Vector2(px, py));
                        if (s == null) { parts = null; break; }
                        parts[f[0]] = s;
                    }
                    if (parts != null && parts.Count > 0) { _mascot[skinHex] = parts; return parts; }
                    parts = new Dictionary<string, Sprite>();
                }
            }
            catch (System.Exception) { parts = new Dictionary<string, Sprite>(); }

            var lines = new List<string>();
            foreach (var kv in MascotArtist.Build(Rgba.FromHex(skinHex)))
            {
                var p = kv.Value;
                var pivot = new Vector2(p.PivotX / p.Image.Width, p.PivotY / p.Image.Height);
                parts[kv.Key] = Cached("mascot_" + skin + "_" + kv.Key, () => p.Image, p.PixelsPerUnit, Vector4.zero, pivot);
                lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1} {2} {3}", kv.Key, pivot.x, pivot.y, p.PixelsPerUnit));
            }
            try { System.IO.File.WriteAllLines(meta, lines); } catch (System.Exception) { }
            _mascot[skinHex] = parts;
            return parts;
        }

        // ------------------------------------------------------------------ Préparation en arrière-plan

        System.Threading.Tasks.Task _worker = System.Threading.Tasks.Task.CompletedTask;
        int _pendingJobs;

        /// <summary>Vrai tant que des images sont en train d'être dessinées en arrière-plan.</summary>
        public bool Preparing => System.Threading.Volatile.Read(ref _pendingJobs) > 0;

        /// <summary>
        /// Dessine en arrière-plan (sur un autre cœur du processeur) les images pas encore en cache, et les enregistre.
        /// Ensuite, les charger ne coûte presque rien : plus de calcul d'image au milieu d'une partie.
        /// </summary>
        /// <param name="urgent">Vrai : sur un fil à part, sans attendre la file principale (ce que l'écran affiché attend).</param>
        public void PrepareInBackground(IEnumerable<KeyValuePair<string, System.Func<Raster>>> items, bool urgent = false)
        {
            string dir = CacheDir;
#if UNITY_WEBGL && !UNITY_EDITOR
            return; // pas de fils d'exécution sur le web : les images se dessinent à la demande
#else
            var todo = new List<KeyValuePair<string, System.Func<Raster>>>();
            foreach (var it in items)
                if (!InPack(it.Key) && !System.IO.File.Exists(System.IO.Path.Combine(dir, it.Key + ".rgbz"))) todo.Add(it);
            if (todo.Count == 0) return;
            System.Threading.Interlocked.Add(ref _pendingJobs, todo.Count);
            System.Action work = () =>
            {
                foreach (var it in todo)
                {
                    try
                    {
                        string file = System.IO.Path.Combine(dir, it.Key + ".rgbz");
                        if (!System.IO.File.Exists(file))
                        {
                            var r = it.Value();
                            r.BleedEdges();
                            WriteRaw(file, r.Width, r.Height, ToBytes(r));
                        }
                    }
                    catch (System.Exception) { /* l'image sera dessinée à la demande */ }
                    finally { System.Threading.Interlocked.Decrement(ref _pendingJobs); }
                }
            };
            if (urgent) System.Threading.Tasks.Task.Run(work);
            else _worker = _worker.ContinueWith(_ => work(), System.Threading.Tasks.TaskScheduler.Default);
#endif
        }

        /// <summary>Les images de la partie : poids, expressions, rougeur (dessins indépendants de la tête portée).</summary>
        public IEnumerable<KeyValuePair<string, System.Func<Raster>>> GameplayJobs()
        {
            foreach (var v in WeightCatalog.Values)
            {
                int value = v;
                yield return new KeyValuePair<string, System.Func<Raster>>("weight" + value, () => WeightArtist.Draw(value, WeightTexture));
            }
            foreach (Expression e in System.Enum.GetValues(typeof(Expression)))
            {
                var ex = e;
                yield return new KeyValuePair<string, System.Func<Raster>>("face_" + ex, () => HeadArtist.Face(ex, HeadSize));
            }
            yield return new KeyValuePair<string, System.Func<Raster>>("flush", () => HeadArtist.Flush(HeadSize));
        }

        public static KeyValuePair<string, System.Func<Raster>> PortraitJob(HeadDef head, bool locked) => locked
            ? new KeyValuePair<string, System.Func<Raster>>("locked" + head.Id, () => HeadArtist.Portrait(head, PortraitSize).Silhouette(Rgba.FromHex("#2B2D42", 80)))
            : new KeyValuePair<string, System.Func<Raster>>("portrait" + head.Id, () => HeadArtist.Portrait(head, PortraitSize));

        public IEnumerable<KeyValuePair<string, System.Func<Raster>>> HeadJobs(bool portraitsOnly)
        {
            foreach (var h in HeadCatalog.All)
            {
                var head = h;
                yield return new KeyValuePair<string, System.Func<Raster>>("portrait" + head.Id, () => HeadArtist.Portrait(head, PortraitSize));
                yield return new KeyValuePair<string, System.Func<Raster>>("locked" + head.Id, () => HeadArtist.Portrait(head, PortraitSize).Silhouette(Rgba.FromHex("#2B2D42", 80)));
                if (portraitsOnly) continue;
                yield return new KeyValuePair<string, System.Func<Raster>>("headback" + head.Id, () => HeadArtist.Back(head, HeadSize));
                yield return new KeyValuePair<string, System.Func<Raster>>("headfront" + head.Id, () => HeadArtist.Front(head, HeadSize));
            }
        }

        /// <summary>Charge en mémoire une image déjà en cache (null si absente).</summary>
        static Sprite TryLoad(string key, float ppu, Vector4 border, Vector2? pivot)
        {
            if (!IsReady(key)) return null;
            return Cached(key, () => null, ppu, border, pivot);
        }

        // ------------------------------------------------------------------ Décor

        public Sprite Scene(string key, System.Func<Raster> draw, float ppu, Vector2? pivot = null, Vector4? border = null)
        {
            if (_scene.TryGetValue(key, out var s)) return s;
            s = Cached("scene_" + key, draw, ppu, border ?? Vector4.zero, pivot);
            _scene[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ Icônes

        public Sprite IconSprite(Icon icon)
        {
            if (_icons.TryGetValue(icon, out var s)) return s;
            s = Cached("icon_" + icon, () => UiArtist.DrawIcon(icon, (int)(128 * UiDensity)), 100 * UiDensity, Vector4.zero);
            _icons[icon] = s;
            return s;
        }

        // ------------------------------------------------------------------ Cache disque

        /// <summary>
        /// À augmenter à chaque modification des dessins (Core/*Art*.cs) : les images en cache sont alors redessinées.
        /// </summary>
        public const int ArtVersion = 14;

        static string _cacheDir;

        public static string CacheDir
        {
            get
            {
                if (_cacheDir != null) return _cacheDir;
                _cacheDir = System.IO.Path.Combine(Application.persistentDataPath, "art_v" + ArtVersion);
                try
                {
                    System.IO.Directory.CreateDirectory(_cacheDir);
                    // Nettoie les anciennes versions.
                    foreach (var dir in System.IO.Directory.GetDirectories(Application.persistentDataPath, "art_v*"))
                        if (dir != _cacheDir) System.IO.Directory.Delete(dir, true);
                }
                catch (System.Exception e) { Debug.LogWarning("[Bascule] Cache d'images indisponible : " + e.Message); }
                return _cacheDir;
            }
        }

        /// <summary>
        /// Dessine une image une seule fois : au premier lancement elle est calculée puis enregistrée en PNG,
        /// ensuite elle est simplement rechargée (lancement rapide même sur un téléphone d'entrée de gamme).
        /// </summary>
        // ------------------------------------------------------------------ Images dessinées à l'avance (ArtPack)
        // Tools/ArtPack dessine toutes les images une fois pour toutes dans Resources/ArtPack. Le jeu les charge
        // directement : aucun dessin au lancement (démarrage instantané, pas de pic de mémoire, même sur le web).

        static HashSet<string> _pack;

        static HashSet<string> Pack
        {
            get
            {
                if (_pack != null) return _pack;
                _pack = new HashSet<string>();
                var manifest = Resources.Load<TextAsset>("ArtPack/manifest");
                if (manifest != null)
                    foreach (var line in manifest.text.Split('\n'))
                    {
                        var k = line.Trim();
                        if (k.Length > 0) _pack.Add(k);
                    }
                return _pack;
            }
        }

        public static bool InPack(string key) => Pack.Contains(key);

        static Sprite FromPack(string key, float ppu, Vector4 border, Vector2? pivot)
        {
            if (!InPack(key)) return null;
            var tex = Resources.Load<Texture2D>("ArtPack/" + key);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        }

        public static Sprite Cached(string key, System.Func<Raster> draw, float pixelsPerUnit, Vector4 border, Vector2? pivot = null)
        {
            var packed = FromPack(key, pixelsPerUnit, border, pivot);
            if (packed != null) return packed;
            Texture2D tex = null;
            string file = FileOf(key);
            try
            {
                if (System.IO.File.Exists(file) && ReadRaw(file, out int w, out int h, out var bytes))
                {
                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(bytes);
                    tex.Apply(false, true);
                }
            }
            catch (System.Exception) { tex = null; }

            if (tex == null)
            {
                var r = draw();
                if (r == null) return null;
                r.BleedEdges();
                var bytes = ToBytes(r);
                tex = new Texture2D(r.Width, r.Height, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(bytes);
                tex.Apply(false, true);
                WriteRaw(file, r.Width, r.Height, bytes);
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "Bascule_" + key;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot ?? new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        }

        // ------------------------------------------------------------------ Fichiers du cache
        // Format sans perte : largeur, hauteur, puis les pixels RVBA compressés (Deflate). Lecture et écriture en C# pur :
        // utilisable depuis un fil d'arrière-plan (aucune fonction de Unity), et rechargement sans décodage d'image.

        public static string FileOf(string key) => System.IO.Path.Combine(CacheDir, key + ".rgbz");

        public static bool IsReady(string key) => InPack(key) || System.IO.File.Exists(FileOf(key));

        static byte[] ToBytes(Raster r)
        {
            var bytes = new byte[r.Pixels.Length * 4];
            for (int i = 0; i < r.Pixels.Length; i++)
            {
                var p = r.Pixels[i];
                bytes[4 * i] = p.r; bytes[4 * i + 1] = p.g; bytes[4 * i + 2] = p.b; bytes[4 * i + 3] = p.a;
            }
            return bytes;
        }

        static void WriteRaw(string file, int w, int h, byte[] bytes)
        {
            string tmp = file + "." + System.Threading.Thread.CurrentThread.ManagedThreadId + ".tmp";
            try
            {
                using (var fs = System.IO.File.Create(tmp))
                {
                    var header = new byte[8];
                    System.BitConverter.GetBytes(w).CopyTo(header, 0);
                    System.BitConverter.GetBytes(h).CopyTo(header, 4);
                    fs.Write(header, 0, 8);
                    using (var z = new System.IO.Compression.DeflateStream(fs, System.IO.Compression.CompressionLevel.Fastest))
                        z.Write(bytes, 0, bytes.Length);
                }
                if (System.IO.File.Exists(file)) System.IO.File.Delete(tmp);
                else System.IO.File.Move(tmp, file);
            }
            catch (System.Exception)
            {
                try { if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp); } catch (System.Exception) { }
            }
        }

        static bool ReadRaw(string file, out int w, out int h, out byte[] bytes)
        {
            w = h = 0; bytes = null;
            using (var fs = System.IO.File.OpenRead(file))
            {
                var header = new byte[8];
                if (fs.Read(header, 0, 8) != 8) return false;
                w = System.BitConverter.ToInt32(header, 0);
                h = System.BitConverter.ToInt32(header, 4);
                if (w <= 0 || h <= 0 || w > 8192 || h > 8192) return false;
                bytes = new byte[w * h * 4];
                using (var z = new System.IO.Compression.DeflateStream(fs, System.IO.Compression.CompressionMode.Decompress))
                {
                    int read = 0;
                    while (read < bytes.Length)
                    {
                        int n = z.Read(bytes, read, bytes.Length - read);
                        if (n <= 0) return false;
                        read += n;
                    }
                }
            }
            return true;
        }

        static Color32[] ToColors(Raster r)
        {
            var px = new Color32[r.Pixels.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var p = r.Pixels[i];
                px[i] = new Color32(p.r, p.g, p.b, p.a);
            }
            return px;
        }

        // ------------------------------------------------------------------ Conversion

        public static Texture2D ToTexture(Raster r)
        {
            r.BleedEdges();
            var tex = new Texture2D(r.Width, r.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Bascule_Generated",
            };
            var px = new Color32[r.Pixels.Length];
            for (int i = 0; i < px.Length; i++)
            {
                var p = r.Pixels[i];
                px[i] = new Color32(p.r, p.g, p.b, p.a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true); // libère la copie mémoire CPU
            return tex;
        }

        public static Sprite ToSprite(Raster r, float pixelsPerUnit, Vector4 border, Vector2? pivot = null)
        {
            var tex = ToTexture(r);
            return Sprite.Create(tex, new Rect(0, 0, r.Width, r.Height), pivot ?? new Vector2(0.5f, 0.5f), pixelsPerUnit, 0,
                SpriteMeshType.FullRect, border);
        }

        public static Color ToColor(Rgba c) => new Color32(c.r, c.g, c.b, c.a);

        public static Color Hex(string hex) => ToColor(Rgba.FromHex(hex));
    }
}
