using System;
using System.IO;
using System.IO.Compression;
using Bascule.Core;

/// <summary>
/// Logos des partenaires pour les outils (ArtPack, ArtPreview) : le même fichier que le jeu,
/// Assets/Bascule/Resources/Sponsors/&lt;clé&gt;_logo.bytes (un PNG), donné au dessin avant les tenues.
/// </summary>
static class SponsorLogos
{
    public static void Register(string root)
    {
        foreach (var promo in PromoCodes.All)
        {
            if (string.IsNullOrEmpty(promo.Patch)) continue;
            var path = Path.Combine(root, "Assets", "Bascule", "Resources", "Sponsors", promo.Patch + "_logo.bytes");
            if (!File.Exists(path)) { Console.WriteLine("Logo absent : " + path); continue; }
            MascotArtist.RegisterPatch(promo.Patch, PngReader.Load(path));
        }
    }

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
