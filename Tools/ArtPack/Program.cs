using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Bascule.Core;

// Dessine à l'avance toutes les images du jeu (Core/ArtCatalog.cs) en PNG sans perte, dans Assets/Bascule/Resources/ArtPack.
// Au lancement, le jeu les charge directement : plus aucun dessin au démarrage.
// À relancer après toute modification d'un dessin :  dotnet run --project Tools/ArtPack -c Release
string root = args.Length > 0 ? args[0] : FindRoot();
string outDir = Path.Combine(root, "Assets/Bascule/Resources/ArtPack");
Directory.CreateDirectory(outDir);
foreach (var f in Directory.GetFiles(outDir)) if (!f.EndsWith(".meta")) File.Delete(f);

// Logos des partenaires (écusson ou broche sur la tenue) : à donner au dessin avant de dessiner les tenues.
SponsorLogos.Register(root);
var jobs = ArtCatalog.All().ToList();
var sw = System.Diagnostics.Stopwatch.StartNew();
Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, j =>
{
    var r = j.Draw();
    r.BleedEdges();
    Png.Save(r, Path.Combine(outDir, j.Key + ".png"));
});
// Pivots des parties du corps (une liste par couleur de peau).
foreach (var skin in ArtCatalog.Skins())
{
    ArtCatalog.Mascot(skin, out var meta);
    File.WriteAllLines(Path.Combine(outDir, "mascot_" + skin.TrimStart('#') + "_pivots.txt"), meta);
}
// Liste des images présentes (le jeu sait ainsi, sans rien charger, ce qui est déjà prêt).
File.WriteAllLines(Path.Combine(outDir, "manifest.txt"), jobs.Select(j => j.Key).OrderBy(k => k));
long bytes = Directory.GetFiles(outDir).Where(f => !f.EndsWith(".meta")).Sum(f => new FileInfo(f).Length);
Console.WriteLine($"{jobs.Count} images en {sw.Elapsed.TotalSeconds:0.0} s, {bytes / 1024 / 1024.0:0.0} Mo -> {outDir}");

static string FindRoot()
{
    var d = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets"))) d = d.Parent;
    return d?.FullName ?? Directory.GetCurrentDirectory();
}

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
