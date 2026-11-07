using System;
using System.Diagnostics;
using System.IO;
using Bascule.Core;

// Exporte la musique et les bruitages en WAV pour les écouter sans Unity.
// dotnet run --project Tools/AudioPreview -- Exports/audio
string outDir = args.Length > 0 ? args[0] : "Exports/audio";
Directory.CreateDirectory(outDir);
var sw = Stopwatch.StartNew();
var calm = Music.CalmLoop();
var tense = Music.TenseLoop();
var heart = Music.Heartbeat();
Console.WriteLine($"Musique calculée en {sw.ElapsedMilliseconds} ms ({Music.LoopSeconds:0.0} s par boucle)");
Wav.Save(Path.Combine(outDir, "musique_douce.wav"), calm, Music.SampleRate);
Wav.Save(Path.Combine(outDir, "musique_tendue.wav"), tense, Music.SampleRate);

// Démo : 50 s de calme, puis le danger monte pendant 25 s (couche tendue, tempo, cœur sur les temps), puis retombe.
int sr = Music.SampleRate;
float total = 95f;
var demo = new float[(int)(total * sr)];
double pos = 0; // position dans la boucle, en échantillons (avance plus vite quand le tempo monte)
double nextBeat = 0;
float beatSamples = sr * 60f / Music.Bpm;
for (int i = 0; i < demo.Length; i++)
{
    float t = i / (float)sr;
    float inten = t < 50 ? 0 : t < 75 ? (t - 50) / 25f : Math.Max(0, 1 - (t - 75) / 6f);
    float pitch = 1f + 0.14f * inten * inten;
    int p0 = (int)pos % calm.Length;
    float tenseVol = inten < 0.15f ? 0 : Math.Min(1, (inten - 0.15f) / 0.75f);
    demo[i] = calm[p0] * 0.5f * (1 - 0.4f * inten) + tense[p0] * 0.55f * tenseVol * tenseVol * (3 - 2 * tenseVol);
    pos += pitch;
    if (inten >= 0.35f && pos >= nextBeat)
    {
        int every = inten > 0.7f ? 1 : 2;
        float k = (inten - 0.35f) / 0.65f;
        for (int h = 0; h < heart.Length && i + h < demo.Length; h++) demo[i + h] += heart[h] * 0.9f * (0.35f + 0.65f * k);
        nextBeat = (Math.Floor(pos / beatSamples / every) + 1) * every * beatSamples;
    }
    else if (inten < 0.35f) nextBeat = (Math.Floor(pos / beatSamples) + 1) * beatSamples;
}
float peak = 0; foreach (var v in demo) peak = Math.Max(peak, Math.Abs(v));
for (int i = 0; i < demo.Length; i++) demo[i] = demo[i] * 0.92f / peak;
Wav.Save(Path.Combine(outDir, "demo_partie.wav"), demo, sr);

// Bruitages, enchaînés avec un petit silence (fusions 2 → 128, combo, chute…).
var list = new System.Collections.Generic.List<float>();
void Put(float[] s) { list.AddRange(s); list.AddRange(new float[Synth.SampleRate / 4]); }
foreach (var v in new[] { 1, 8, 64 }) Put(Synth.Thud(v));
foreach (var v in WeightCatalog.Values) if (v > 1) Put(Synth.Pop(v));
for (int c = 1; c <= 6; c++) Put(Synth.Chime(c));
Put(Synth.Lost()); Put(Synth.Phew()); Put(Synth.Lift()); Put(Synth.Fall()); Put(Synth.Click());
Wav.Save(Path.Combine(outDir, "bruitages.wav"), list.ToArray(), Synth.SampleRate);
Wav.Save(Path.Combine(outDir, "rafale.wav"), Synth.Wind(), Synth.SampleRate);
Console.WriteLine("Exporté dans " + Path.GetFullPath(outDir) + $" (pic démo {peak:0.00})");

static class Wav
{
    public static void Save(string path, float[] data, int rate)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data.Length * 2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(data.Length * 2);
        foreach (var v in data) w.Write((short)(Math.Clamp(v, -1f, 1f) * 32767));
    }
}
