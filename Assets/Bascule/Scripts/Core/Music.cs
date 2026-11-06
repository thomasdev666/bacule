using System;

namespace Bascule.Core
{
    /// <summary>
    /// Musique de fond composée par le code : un vrai morceau de 32 mesures (environ 1 min 20 à 96 battements par minute)
    /// avant de boucler, en 8 parties qui varient :
    ///   A  intro légère (marimba)          B  la mélodie entre          A' A avec mélodie et contre-chant
    ///   C  pont (autres accords, sans batterie)                         B' B plus dansant (claps)
    ///   D  montée (ii-V-I)                 A" final plein               E  respiration avant la reprise
    /// Deux couches de même longueur jouées ensemble :
    /// - douce : marimba, mélodie de flûte-cloche, nappe, basse ronde, petite batterie ;
    /// - tendue : basse pulsée, charleston, caisse claire, arpège nerveux, note qui grince ; elle monte avec le danger.
    /// Le battement de cœur est joué à part, calé sur les temps de la musique (MusicPlayer).
    /// Tout est en do majeur, comme les bruitages (Synth).
    /// </summary>
    public static class Music
    {
        public const int SampleRate = 22050;
        public const float Bpm = 96f;
        public const int Bars = 32;
        public static float BeatSeconds => 60f / Bpm;
        public static float BarSeconds => 4 * BeatSeconds;
        public static float LoopSeconds => Bars * BarSeconds;
        public static int SamplesPerBeat => (int)Math.Round(BeatSeconds * SampleRate);

        // Accords : fondamentale (MIDI) et notes de l'accord (MIDI).
        static readonly int[][] Progressions =
        {
            new[] { 0, 9, 5, 7 },   // A : do – la m – fa – sol
            new[] { 9, 5, 0, 7 },   // B : la m – fa – do – sol
            new[] { 5, 7, 4, 9 },   // C : fa – sol – mi m – la m
            new[] { 2, 7, 0, 0 },   // D : ré m – sol – do – do
            new[] { 5, 0, 5, 7 },   // E : fa – do – fa – sol
        };

        enum Sec { A, B, A2, C, B2, D, A3, E }
        static readonly Sec[] Form = { Sec.A, Sec.B, Sec.A2, Sec.C, Sec.B2, Sec.D, Sec.A3, Sec.E };

        static int ProgOf(Sec s) => s == Sec.A || s == Sec.A2 || s == Sec.A3 ? 0 : s == Sec.B || s == Sec.B2 ? 1 : s == Sec.C ? 2 : s == Sec.D ? 3 : 4;

        /// <summary>Notes de l'accord (en demi-tons depuis do) : triade majeure ou mineure selon le degré.</summary>
        static int[] Triad(int root)
        {
            bool minor = root == 9 || root == 2 || root == 4;
            return new[] { root, root + (minor ? 3 : 4), root + 7 };
        }

        static float Midi(int note) => 440f * (float)Math.Pow(2, (note - 69) / 12.0);
        static readonly int[] Scale = { 0, 2, 4, 5, 7, 9, 11 };

        static int N => (int)(LoopSeconds * SampleRate);

        /// <summary>Couche douce.</summary>
        public static float[] CalmLoop()
        {
            var buf = new float[N];
            var rng = new GameRng(2026);
            float beat = BeatSeconds, eighth = beat / 2f;
            int[] arp = { 0, 1, 2, 1, 2, 0, 1, 2 };
            for (int bar = 0; bar < Bars; bar++)
            {
                var sec = Form[bar / 4];
                int inSec = bar % 4;
                int root = Progressions[ProgOf(sec)][inSec];
                var chord = Triad(root);
                float t0 = bar * BarSeconds;
                bool drums = sec != Sec.C && sec != Sec.E;
                bool melody = sec == Sec.B || sec == Sec.A2 || sec == Sec.B2 || sec == Sec.A3 || sec == Sec.D;

                // Nappe.
                foreach (var n in chord) AddPad(buf, t0, BarSeconds, Midi(48 + n), sec == Sec.C ? 0.045f : 0.03f);

                // Marimba : arpège, plus serré dans les parties chargées.
                for (int i = 0; i < 8; i++)
                {
                    if (sec == Sec.E && i % 2 == 1) continue;
                    int note = 60 + chord[arp[(i + bar) % 8]] + (i >= 6 && inSec == 3 ? 12 : 0);
                    AddMarimba(buf, t0 + i * eighth, Midi(note + 12), i % 2 == 0 ? 0.11f : 0.075f);
                }

                // Basse ronde (motif qui change selon la partie).
                float bassF = Midi(36 + root);
                if (sec == Sec.B2 || sec == Sec.A3)
                {
                    AddBass(buf, t0, beat * 1.5f, bassF, 0.12f, false);
                    AddBass(buf, t0 + beat * 1.5f, beat * 0.5f, bassF * 2f, 0.07f, false);
                    AddBass(buf, t0 + beat * 2, beat * 1.5f, bassF, 0.11f, false);
                    AddBass(buf, t0 + beat * 3.5f, beat * 0.5f, Midi(36 + chord[2]), 0.08f, false);
                }
                else
                {
                    AddBass(buf, t0, beat * 2, bassF, 0.11f, false);
                    AddBass(buf, t0 + beat * 2, beat * 2, sec == Sec.C ? bassF : Midi(36 + chord[2] - 12 + 12), 0.08f, false);
                }

                // Batterie douce.
                if (drums)
                {
                    AddKick(buf, t0, 0.3f);
                    AddKick(buf, t0 + beat * 2, 0.22f);
                    if (sec == Sec.B2 || sec == Sec.A3) AddKick(buf, t0 + beat * 2.5f, 0.15f);
                    for (int i = 0; i < 8; i++) AddShaker(buf, t0 + i * eighth + eighth * 0.5f, 0.022f, (uint)(bar * 8 + i));
                    if (sec == Sec.B2 || sec == Sec.A3 || sec == Sec.D)
                    {
                        AddClap(buf, t0 + beat, 0.08f, (uint)(bar * 3));
                        AddClap(buf, t0 + beat * 3, 0.08f, (uint)(bar * 3 + 1));
                    }
                }
                // Petit roulement de toms avant chaque nouvelle partie.
                if (inSec == 3 && sec != Sec.E)
                    for (int k = 0; k < 4; k++) AddTom(buf, t0 + beat * 3 + k * beat / 4f, 110f - k * 12f, 0.12f);

                // Mélodie : motif de 2 mesures, répété puis varié, cadence sur la 4e.
                if (melody) Melody(buf, t0, bar, inSec, chord, sec, rng);
            }
            return Finish(buf, 0.9f);
        }

        static void Melody(float[] buf, float t0, int bar, int inSec, int[] chord, Sec sec, GameRng rng)
        {
            float beat = BeatSeconds;
            // Rythmes (en croches) : ils changent d'une partie à l'autre, le motif reste chantable.
            int[][] rhythms =
            {
                new[] { 0, 2, 3, 4, 6 },
                new[] { 0, 1, 2, 4, 5, 6 },
                new[] { 0, 3, 4, 6, 7 },
                new[] { 0, 2, 4 },
            };
            int[] rh = inSec == 3 ? rhythms[3] : rhythms[(bar / 4 + inSec) % 3];
            // Graine par partie : le motif des mesures 1 et 3 est le même (répétition), la 2 lui répond.
            var motifRng = new GameRng((uint)(1000 + (int)sec * 31 + (inSec == 2 ? 0 : inSec)));
            int prev = 2;
            for (int k = 0; k < rh.Length; k++)
            {
                bool strong = rh[k] % 2 == 0;
                int note;
                if (inSec == 3 && k == rh.Length - 1) note = 72 + chord[0] % 12; // cadence sur la fondamentale
                else if (strong)
                {
                    int pick = (int)(motifRng.NextFloat() * 3) % 3;
                    note = 72 + chord[pick] % 12 + (chord[pick] >= 12 ? 12 : 0);
                }
                else
                {
                    // Note de passage de la gamme, proche de la précédente.
                    int step = motifRng.NextFloat() < 0.5f ? -1 : 1;
                    prev = Math.Max(0, Math.Min(6, prev + step));
                    note = 72 + Scale[prev];
                }
                if (note > 84) note -= 12;
                float start = t0 + rh[k] * beat / 2f;
                float len = (k + 1 < rh.Length ? rh[k + 1] - rh[k] : 8 - rh[k]) * beat / 2f;
                float vol = sec == Sec.A3 ? 0.13f : 0.11f;
                AddLead(buf, start, Math.Min(len, beat * 1.5f), Midi(note), vol * (strong ? 1f : 0.8f));
                // Contre-chant une tierce sous la mélodie dans la reprise.
                if (sec == Sec.A2 && strong) AddLead(buf, start, Math.Min(len, beat), Midi(note - 4), 0.045f);
            }
        }

        /// <summary>Couche tendue : même grille, même longueur, pour rester synchronisée.</summary>
        public static float[] TenseLoop()
        {
            var buf = new float[N];
            float beat = BeatSeconds, eighth = beat / 2f, sixteenth = beat / 4f;
            for (int bar = 0; bar < Bars; bar++)
            {
                var sec = Form[bar / 4];
                int root = Progressions[ProgOf(sec)][bar % 4];
                var chord = Triad(root);
                float t0 = bar * BarSeconds;
                for (int i = 0; i < 8; i++) AddBass(buf, t0 + i * eighth, eighth * 0.85f, Midi(36 + root) * (i % 4 == 3 ? 2f : 1f), 0.14f, true);
                for (int i = 0; i < 16; i++) AddShaker(buf, t0 + i * sixteenth, i % 4 == 2 ? 0.05f : 0.028f, (uint)(5000 + bar * 16 + i));
                AddSnare(buf, t0 + beat, 0.2f, (uint)(bar * 2));
                AddSnare(buf, t0 + beat * 3, 0.2f, (uint)(bar * 2 + 1));
                AddKick(buf, t0, 0.32f);
                AddKick(buf, t0 + beat * 1.5f, 0.2f);
                AddKick(buf, t0 + beat * 2, 0.32f);
                // Arpège nerveux en doubles croches, une octave au-dessus.
                for (int i = 0; i < 16; i++)
                    AddPluck(buf, t0 + i * sixteenth, Midi(72 + chord[i % 3] + (i % 8 >= 6 ? 12 : 0)), 0.035f);
                // Note qui grince (demi-ton au-dessus de la quinte) sur le dernier temps.
                AddPad(buf, t0 + beat * 3, beat, Midi(60 + chord[2] + 1), 0.028f);
            }
            return Finish(buf, 0.85f);
        }

        /// <summary>Battement de cœur « poum-poum ».</summary>
        public static float[] Heartbeat()
        {
            int n = (int)(0.45f * SampleRate);
            var buf = new float[n];
            Thump(buf, 0f, 62f, 1f);
            Thump(buf, 0.16f, 54f, 0.7f);
            return Finish(buf, 0.95f);
        }

        // ------------------------------------------------------------------ Instruments

        /// <summary>Ajoute une note ; ce qui dépasse de la fin revient au début (la boucle reste continue).</summary>
        static void Add(float[] buf, float start, float duration, Func<float, float> f)
        {
            int i0 = (int)(start * SampleRate), n = (int)(duration * SampleRate);
            for (int k = 0; k < n; k++)
            {
                int i = (i0 + k) % buf.Length;
                buf[i] += f(k / (float)SampleRate);
            }
        }

        static void AddMarimba(float[] buf, float start, float freq, float vol) =>
            Add(buf, start, 0.5f, t =>
            {
                float env = (float)Math.Exp(-t * 8f) * Math.Min(1f, t / 0.002f);
                double ph = 2 * Math.PI * freq * t;
                return (float)(Math.Sin(ph) + 0.22 * Math.Sin(ph * 4) * Math.Exp(-t * 35)) * env * vol;
            });

        /// <summary>Mélodie : flûte douce avec un léger vibrato et une petite cloche à l'attaque.</summary>
        static void AddLead(float[] buf, float start, float duration, float freq, float vol) =>
            Add(buf, start, duration + 0.25f, t =>
            {
                float a = Math.Min(1f, t / 0.02f);
                float r = t > duration ? (float)Math.Exp(-(t - duration) * 14f) : 1f;
                float vib = 1f + 0.004f * (float)Math.Sin(2 * Math.PI * 5.5 * t) * Math.Min(1f, t / 0.3f);
                double ph = 2 * Math.PI * freq * vib * t;
                float tone = (float)(Math.Sin(ph) * 0.8 + Math.Sin(ph * 2) * 0.12 + Math.Sin(ph * 3) * 0.05);
                float bell = (float)(Math.Sin(2 * Math.PI * freq * 2.76 * t) * Math.Exp(-t * 18)) * 0.25f;
                return (tone + bell) * a * r * vol * (1f - 0.25f * Math.Min(1f, t / 1.2f));
            });

        static void AddPluck(float[] buf, float start, float freq, float vol) =>
            Add(buf, start, 0.18f, t =>
            {
                double ph = 2 * Math.PI * freq * t;
                float saw = (float)(Math.Sin(ph) + 0.5 * Math.Sin(2 * ph) + 0.33 * Math.Sin(3 * ph));
                return saw * (float)Math.Exp(-t * 22f) * vol;
            });

        static void AddPad(float[] buf, float start, float duration, float freq, float vol) =>
            Add(buf, start, duration, t =>
            {
                float a = Math.Min(1f, t / 0.35f) * Math.Min(1f, (duration - t) / 0.35f);
                double ph = 2 * Math.PI * freq * t;
                return (float)(Math.Sin(ph) + 0.5 * Math.Sin(ph * 1.004 + 1.0) + 0.2 * Math.Sin(ph * 2.002)) * a * vol;
            });

        static void AddBass(float[] buf, float start, float duration, float freq, float vol, bool punchy) =>
            Add(buf, start, duration, t =>
            {
                float env = punchy ? (float)Math.Exp(-t * 9f) : Math.Min(1f, t / 0.015f) * Math.Min(1f, (duration - t) / 0.05f);
                double ph = 2 * Math.PI * freq * t;
                // Harmoniques pour qu'on l'entende sur un haut-parleur de téléphone.
                return (float)(Math.Sin(ph) + 0.4 * Math.Sin(ph * 2) + 0.15 * Math.Sin(ph * 3)) * env * vol;
            });

        static void AddKick(float[] buf, float start, float vol) =>
            Add(buf, start, 0.25f, t =>
            {
                float f = 45f + 85f * (float)Math.Exp(-t * 30f);
                return (float)Math.Sin(2 * Math.PI * f * t) * (float)Math.Exp(-t * 13f) * vol;
            });

        static void AddTom(float[] buf, float start, float freq, float vol) =>
            Add(buf, start, 0.2f, t =>
            {
                float f = freq * (1f + 0.4f * (float)Math.Exp(-t * 25f));
                return (float)Math.Sin(2 * Math.PI * f * t) * (float)Math.Exp(-t * 16f) * vol;
            });

        static void AddSnare(float[] buf, float start, float vol, uint seed)
        {
            var rng = new GameRng(seed * 7919u + 13u);
            Add(buf, start, 0.18f, t => ((rng.NextFloat() * 2f - 1f) * 0.75f + (float)Math.Sin(2 * Math.PI * 185 * t) * 0.4f) * (float)Math.Exp(-t * 22f) * vol);
        }

        static void AddClap(float[] buf, float start, float vol, uint seed)
        {
            var rng = new GameRng(seed * 104723u + 3u);
            float lp = 0f;
            Add(buf, start, 0.15f, t =>
            {
                float n = rng.NextFloat() * 2f - 1f;
                float hp = n - lp; lp += (n - lp) * 0.3f;
                // Trois petites claques rapprochées, comme des mains.
                float env = (float)(Math.Exp(-t * 30) + (t > 0.01f ? 0.6 * Math.Exp(-(t - 0.01f) * 40) : 0) + (t > 0.02f ? 0.4 * Math.Exp(-(t - 0.02f) * 40) : 0));
                return hp * env * vol;
            });
        }

        static void AddShaker(float[] buf, float start, float vol, uint seed)
        {
            var rng = new GameRng(seed * 104729u + 7u);
            float prev = 0f;
            Add(buf, start, 0.05f, t =>
            {
                float n = rng.NextFloat() * 2f - 1f;
                float hp = n - prev; // passe-haut grossier : son plus « métallique »
                prev = n;
                return hp * (float)Math.Exp(-t * 70f) * vol;
            });
        }

        static void Thump(float[] buf, float start, float freq, float vol)
        {
            int i0 = (int)(start * SampleRate);
            for (int i = i0; i < buf.Length; i++)
            {
                float t = (i - i0) / (float)SampleRate;
                float f = freq * (1f + 0.6f * (float)Math.Exp(-t * 25f));
                // Fondamentale grave + harmoniques et petit claquement : audible même sur un haut-parleur de téléphone.
                double ph = 2 * Math.PI * f * t;
                float body = (float)(Math.Sin(ph) + 0.7 * Math.Sin(2 * ph) + 0.45 * Math.Sin(3 * ph) + 0.25 * Math.Sin(4 * ph));
                float knock = (float)Math.Sin(2 * Math.PI * 180 * t) * (float)Math.Exp(-t * 60f) * 0.5f;
                buf[i] += (body * (float)Math.Exp(-t * 16f) + knock) * vol;
            }
        }

        /// <summary>Normalise sous le maximum voulu, avec une compression douce pour garder du volume sans saturer.</summary>
        static float[] Finish(float[] buf, float ceiling)
        {
            float peak = 0f;
            foreach (var v in buf) peak = Math.Max(peak, Math.Abs(v));
            float k = peak > 0 ? 1.25f / peak : 1f;
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i] * k;
                buf[i] = (float)Math.Tanh(v) * ceiling;
            }
            return buf;
        }
    }
}
