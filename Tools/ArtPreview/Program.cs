using System;
using System.IO;
using System.IO.Compression;
using Bascule.Core;

// Exporte les visuels procéduraux de Bascule en PNG, pour les voir sans ouvrir Unity.
var outDir = args.Length > 0 ? args[0] : "Exports";
Directory.CreateDirectory(Path.Combine(outDir, "heads"));
Directory.CreateDirectory(Path.Combine(outDir, "layers"));
Directory.CreateDirectory(Path.Combine(outDir, "faces"));
const int N = 384;
foreach (var h in HeadCatalog.All)
{
    Png.Save(HeadArtist.Portrait(h, N), Path.Combine(outDir, "heads", $"{h.Id:00}.png"));
    Png.Save(HeadArtist.Back(h, N), Path.Combine(outDir, "layers", $"{h.Id:00}_back.png"));
    Png.Save(HeadArtist.Front(h, N), Path.Combine(outDir, "layers", $"{h.Id:00}_front.png"));
}
foreach (Expression e in Enum.GetValues(typeof(Expression)))
    Png.Save(HeadArtist.Face(e, N), Path.Combine(outDir, "faces", $"{e}.png"));
Directory.CreateDirectory(Path.Combine(outDir, "weights"));
foreach (var v in WeightCatalog.Values)
    Png.Save(WeightArtist.Draw(v, 256), Path.Combine(outDir, "weights", $"{v}.png"));
// Mascotte (pièces + pivots), décor, planche.
Directory.CreateDirectory(Path.Combine(outDir, "mascot"));
var pivots = new System.Text.StringBuilder("{");
foreach (var kv in MascotArtist.Build(Rgba.FromHex(HeadCatalog.Get(0).Skin)))
{
    Png.Save(kv.Value.Image, Path.Combine(outDir, "mascot", kv.Key + ".png"));
    pivots.Append($"\"{kv.Key}\":[{kv.Value.PivotX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.PivotY.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.Image.Width},{kv.Value.Image.Height}],");
}
pivots.Length--; pivots.Append("}");
File.WriteAllText(Path.Combine(outDir, "mascot", "pivots.json"), pivots.ToString());
// Corps habillé de chaque tête (une tenue par tête, Core/Outfits.cs) : mascot_h/<id>/.
foreach (var h in HeadCatalog.All)
{
    var dir = Path.Combine(outDir, "mascot_h", h.Id.ToString());
    Directory.CreateDirectory(dir);
    var pv = new System.Text.StringBuilder("{");
    foreach (var kv in MascotArtist.Build(h.BodyKey))
    {
        Png.Save(kv.Value.Image, Path.Combine(dir, kv.Key + ".png"));
        pv.Append($"\"{kv.Key}\":[{kv.Value.PivotX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.PivotY.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.Image.Width},{kv.Value.Image.Height}],");
    }
    pv.Length--; pv.Append("}");
    File.WriteAllText(Path.Combine(dir, "pivots.json"), pv.ToString());
}
Directory.CreateDirectory(Path.Combine(outDir, "scene"));
for (int ti = 0; ti < Theme.All.Length; ti++)
{
    var t = Theme.All[ti];
    Png.Save(SceneArtist.Sky(t), Path.Combine(outDir, "scene", $"sky{ti}.png"));
    Png.Save(SceneArtist.Sun(t), Path.Combine(outDir, "scene", $"sun{ti}.png"));
    Png.Save(SceneArtist.Hills(t, true, 768, 240), Path.Combine(outDir, "scene", $"far{ti}.png"));
    Png.Save(SceneArtist.Hills(t, false, 768, 240), Path.Combine(outDir, "scene", $"near{ti}.png"));
    Png.Save(SceneArtist.Ground(t, 512, 180), Path.Combine(outDir, "scene", $"ground{ti}.png"));
    for (int c = 0; c < 3; c++) Png.Save(SceneArtist.Cloud(c, t, 384, 192), Path.Combine(outDir, "scene", $"cloud{ti}_{c}.png"));
}
Png.Save(SceneArtist.Stars(), Path.Combine(outDir, "scene", "stars.png"));
Png.Save(SceneArtist.Plank(6.6f, 0.32f, 160f, out int plankBorder), Path.Combine(outDir, "scene", "plank.png"));
// Planches à collectionner (Core/Cosmetics.cs).
Directory.CreateDirectory(Path.Combine(outDir, "planches"));
foreach (var skin in PlankCatalog.All)
{
    Png.Save(SceneArtist.Plank(6.6f, 0.32f, 160f, out _, skin), Path.Combine(outDir, "planches", "planche_" + skin.Id + ".png"));
    Png.Save(SceneArtist.Plank(4.4f, 0.62f, 96f, out _, skin), Path.Combine(outDir, "planches", "apercu_" + skin.Id + ".png"));
    // Gros plan (bout de planche en haute définition) pour dessiner les rebords.
    Png.Save(SceneArtist.Plank(1.8f, 0.3f, 500f, out _, skin), Path.Combine(outDir, "planches", "zoom_" + skin.Id + ".png"));
    Png.Save(SceneArtist.PlankLip(skin, 0.12f, 0.22f, 500f), Path.Combine(outDir, "planches", "rebord_zoom_" + skin.Id + ".png"));
    Png.Save(SceneArtist.PlankLip(skin, 0.12f, 0.22f, 160f), Path.Combine(outDir, "planches", "rebord_" + skin.Id + ".png"));
    Png.Save(SceneArtist.PlankPreview(skin, 4.4f, 0.62f, 96f, 0.2f, 0.36f), Path.Combine(outDir, "planches", "apercu_rebords_" + skin.Id + ".png"));
    Png.Save(SceneArtist.PlankPreview(skin, 1.8f, 0.3f, 500f, 0.12f, 0.22f), Path.Combine(outDir, "planches", "jeu_zoom_" + skin.Id + ".png"));
    Png.Save(SceneArtist.PlankPreview(skin, 6.6f, 0.3f, 160f, 0.12f, 0.22f), Path.Combine(outDir, "planches", "jeu_" + skin.Id + ".png"));
}
File.WriteAllText(Path.Combine(outDir, "scene", "plank_border.txt"), plankBorder.ToString());
Directory.CreateDirectory(Path.Combine(outDir, "ui"));
Png.Save(UiArtist.Button(), Path.Combine(outDir, "ui", "button.png"));
Png.Save(UiArtist.WheelBoard(720), Path.Combine(outDir, "ui", "wheel.png"));
Png.Save(UiArtist.WheelBoard(256), Path.Combine(outDir, "ui", "wheel_small.png"));
Png.Save(UiArtist.QuestBadge(256), Path.Combine(outDir, "ui", "quest.png"));
Png.Save(UiArtist.WeekBadge(256), Path.Combine(outDir, "ui", "week.png"));
foreach (int count in ArtCatalog.CoinPiles) Png.Save(UiArtist.CoinPile(count, 256), Path.Combine(outDir, "ui", $"coins_{count}.png"));
Png.Save(UiArtist.Panel(), Path.Combine(outDir, "ui", "panel.png"));
Png.Save(UiArtist.Pill(), Path.Combine(outDir, "ui", "pill.png"));
Png.Save(UiArtist.Glass(), Path.Combine(outDir, "ui", "glass.png"));
Png.Save(UiArtist.ButtonGloss(), Path.Combine(outDir, "ui", "gloss.png"));
Png.Save(UiArtist.Glass(160, 96, 2f), Path.Combine(outDir, "ui", "glass@2x.png"));
Png.Save(UiArtist.ButtonGloss(192, 200, 2f), Path.Combine(outDir, "ui", "gloss@2x.png"));
// Icône de l'application (copiée dans Assets/Bascule/Icone par la commande indiquée dans CLAUDE.md).
Directory.CreateDirectory(Path.Combine(outDir, "icone"));
Png.Save(IconArtist.AppIcon(1024), Path.Combine(outDir, "icone", "icone_1024.png"));
// Versions haute densité utilisées par le jeu (ArtCache.UiDensity = 2).
Png.Save(UiArtist.Button(192, 200, 2f), Path.Combine(outDir, "ui", "button@2x.png"));
Png.Save(UiArtist.Panel(200, 2f), Path.Combine(outDir, "ui", "panel@2x.png"));
Png.Save(UiArtist.LevelTube(640, 96, 2f), Path.Combine(outDir, "ui", "level@2x.png"));
Png.Save(SceneArtist.Ground(Theme.All[0], 1536, 540), Path.Combine(outDir, "ui", "ground@3x.png"));
Png.Save(SceneArtist.Hills(Theme.All[0], false, 1536, 480), Path.Combine(outDir, "ui", "hills@2x.png"));
Png.Save(UiArtist.LevelTube(), Path.Combine(outDir, "ui", "level.png"));
Png.Save(UiArtist.Bubble(), Path.Combine(outDir, "ui", "bubble.png"));
Png.Save(UiArtist.Vignette(), Path.Combine(outDir, "ui", "vignette.png"));
Png.Save(UiArtist.Rays(), Path.Combine(outDir, "ui", "rays.png"));
Png.Save(UiArtist.ChestBase(512), Path.Combine(outDir, "ui", "chest_base.png"));
Png.Save(UiArtist.ChestLid(512), Path.Combine(outDir, "ui", "chest_lid.png"));
Png.Save(UiArtist.Pill(160, 96, 2f), Path.Combine(outDir, "ui", "pill@2x.png"));
foreach (Icon ic in Enum.GetValues(typeof(Icon))) Png.Save(UiArtist.DrawIcon(ic), Path.Combine(outDir, "ui", $"icon_{ic}.png"));
Png.Save(HeadArtist.Flush(N), Path.Combine(outDir, "layers", "flush.png"));
Png.Save(ShapeArtist.Balloon(256, WeightCatalog.Color(128)), Path.Combine(outDir, "balloon.png"));
// Partenaires : têtes en cours de validation (pas encore dans le jeu). Core ne lit pas de fichier : l'outil lui
// donne le logo (Tools/Sponsors), puis exporte la tête, ses calques et le corps habillé, pour les maquettes.
foreach (var sp in SponsorPreviews.All)
{
    var logoPath = Path.Combine(SponsorPreviews.Root(), "Tools", "Sponsors", sp.Patch + "_logo.png");
    if (!File.Exists(logoPath)) { Console.WriteLine("Logo absent : " + logoPath); continue; }
    MascotArtist.RegisterPatch(sp.Patch, PngReader.Load(logoPath));
    Directory.CreateDirectory(Path.Combine(outDir, "sponsors"));
    Png.Save(HeadArtist.Portrait(sp.Head, 768), Path.Combine(outDir, "sponsors", sp.Key + "_portrait.png"));
    Png.Save(HeadArtist.Back(sp.Head, N), Path.Combine(outDir, "layers", sp.Key + "_back.png"));
    Png.Save(HeadArtist.Front(sp.Head, N), Path.Combine(outDir, "layers", sp.Key + "_front.png"));
    var dir = Path.Combine(outDir, "mascot_h", sp.Key);
    Directory.CreateDirectory(dir);
    var pv = new System.Text.StringBuilder("{");
    foreach (var kv in MascotArtist.BuildOutfit(Rgba.FromHex(sp.Head.Skin), sp.Outfit))
    {
        Png.Save(kv.Value.Image, Path.Combine(dir, kv.Key + ".png"));
        pv.Append($"\"{kv.Key}\":[{kv.Value.PivotX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.PivotY.ToString(System.Globalization.CultureInfo.InvariantCulture)},{kv.Value.Image.Width},{kv.Value.Image.Height}],");
    }
    pv.Length--; pv.Append("}");
    File.WriteAllText(Path.Combine(dir, "pivots.json"), pv.ToString());
}
using (var w = new StreamWriter(Path.Combine(outDir, "heads.tsv")))
    foreach (var h in HeadCatalog.All)
        w.WriteLine($"{h.Id}\t{h.NameFr}\t{h.Unlock}\t{h.UnlockValue}\t{h.Expression}");
Console.WriteLine($"Exporté dans {Path.GetFullPath(outDir)}");

static class Png
{
    public static void Save(Raster r, string path)
    {
        // Lignes de haut en bas (le Raster a l'origine en bas à gauche).
        var raw = new byte[(r.Width * 4 + 1) * r.Height];
        int k = 0;
        for (int y = r.Height - 1; y >= 0; y--)
        {
            raw[k++] = 0;
            for (int x = 0; x < r.Width; x++)
            {
                var p = r.Get(x, y);
                raw[k++] = p.r; raw[k++] = p.g; raw[k++] = p.b; raw[k++] = p.a;
            }
        }
        using var fs = File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, r.Width); BE(ihdr, 4, r.Height); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(fs, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true)) z.Write(raw);
            Chunk(fs, "IDAT", ms.ToArray());
        }
        Chunk(fs, "IEND", Array.Empty<byte>());
    }

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t); s.Write(data);
        uint crc = Crc(t, 0xFFFFFFFF); crc = Crc(data, crc) ^ 0xFFFFFFFF;
        var c = new byte[4]; BE(c, 0, (int)crc); s.Write(c);
    }

    static uint Crc(byte[] d, uint c)
    {
        foreach (var b in d)
        {
            c ^= b;
            for (int i = 0; i < 8; i++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c;
    }
}

/// <summary>Têtes de partenaires à valider : dessinées pour les maquettes, pas encore ajoutées au catalogue du jeu.</summary>
static class SponsorPreviews
{
    public sealed class Entry { public string Key, Patch; public HeadDef Head; public OutfitDef Outfit; }

    public static readonly Entry[] All =
    {
        // BDE Samb'bach : une Brésilienne en coiffe de carnaval, maillot jaune et vert, logo du BDE cousu sur la poitrine.
        new Entry
        {
            Key = "sambbach", Patch = "sambbach",
            Head = new HeadDef
            {
                Id = 66, NameFr = "Samb'bach", NameEn = "Samb'bach", Unlock = UnlockKind.Default, Skin = "#C68C5D",
                Hair = HairStyle.Cascade, HairColor = "#3A1F14", Accessory = Accessory.Samba, Expression = Expression.Grin, Lashes = true,
            },
            // Robe de fête vert émeraude à volants jaunes, liseré bleu, logo du BDE en broche dorée, ballerines dorées.
            Outfit = new OutfitDef
            {
                Top = Top.Dress, Main = "#0E9F5B", Accent = "#FFD31A", Motif = Motif.None, Emblem = Emblem.Patch, Patch = "sambbach",
                Bottom = Bottom.Shorts, BottomColor = "#2F6FE0", Shoe = Shoe.Flats, ShoeColor = "#E8B23A",
            },
        },
    };

    /// <summary>Racine du dépôt (le dossier qui contient Assets/Bascule), quel que soit le dossier de lancement.</summary>
    public static string Root()
    {
        var d = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets", "Bascule"))) d = d.Parent;
        return d?.FullName ?? Directory.GetCurrentDirectory();
    }
}

/// <summary>Lecture d'un PNG 8 bits (RGB ou RGBA, non entrelacé) en Raster (origine en bas à gauche).</summary>
static class PngReader
{
    public static Raster Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        int pos = 8, w = 0, h = 0, colorType = 0, depth = 0, interlace = 0;
        using var idat = new MemoryStream();
        while (pos + 8 <= bytes.Length)
        {
            int len = (bytes[pos] << 24) | (bytes[pos + 1] << 16) | (bytes[pos + 2] << 8) | bytes[pos + 3];
            string type = System.Text.Encoding.ASCII.GetString(bytes, pos + 4, 4);
            int data = pos + 8;
            if (type == "IHDR")
            {
                w = (bytes[data] << 24) | (bytes[data + 1] << 16) | (bytes[data + 2] << 8) | bytes[data + 3];
                h = (bytes[data + 4] << 24) | (bytes[data + 5] << 16) | (bytes[data + 6] << 8) | bytes[data + 7];
                depth = bytes[data + 8]; colorType = bytes[data + 9]; interlace = bytes[data + 12];
            }
            else if (type == "IDAT") idat.Write(bytes, data, len);
            else if (type == "IEND") break;
            pos = data + len + 4;
        }
        if (depth != 8 || interlace != 0 || (colorType != 6 && colorType != 2)) throw new NotSupportedException($"PNG non pris en charge : {path}");
        int bpp = colorType == 6 ? 4 : 3, stride = w * bpp;
        idat.Position = 0;
        using var z = new ZLibStream(idat, CompressionMode.Decompress);
        var raw = new byte[(stride + 1) * h];
        int read = 0;
        while (read < raw.Length) { int n = z.Read(raw, read, raw.Length - read); if (n <= 0) break; read += n; }
        var cur = new byte[stride];
        var prev = new byte[stride];
        var img = new Raster(w, h);
        for (int y = 0; y < h; y++)
        {
            int f = raw[y * (stride + 1)];
            Array.Copy(raw, y * (stride + 1) + 1, cur, 0, stride);
            for (int i = 0; i < stride; i++)
            {
                int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                int v = cur[i];
                switch (f)
                {
                    case 1: v += a; break;
                    case 2: v += b; break;
                    case 3: v += (a + b) / 2; break;
                    case 4:
                    {
                        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                        v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                        break;
                    }
                }
                cur[i] = (byte)v;
            }
            for (int x = 0; x < w; x++)
            {
                int o = x * bpp;
                img.Pixels[(h - 1 - y) * w + x] = new Rgba(cur[o], cur[o + 1], cur[o + 2], bpp == 4 ? cur[o + 3] : (byte)255);
            }
            (prev, cur) = (cur, prev);
        }
        return img;
    }
}
