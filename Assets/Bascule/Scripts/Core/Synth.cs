using System;

namespace Bascule.Core
{
    /// <summary>
    /// Bruitages fabriqués par le code. Tous sont accordés dans la tonalité de la musique (do majeur, gamme pentatonique
    /// do-ré-mi-sol-la) : une fusion ou un combo « joue avec » la musique au lieu de jurer avec elle.
    /// Timbres doux (bois, bulles, cloches) et courts, pour rester sous la musique sans la couvrir.
    /// Chaque méthode renvoie des échantillons mono entre -1 et 1.
    /// </summary>
    public static class Synth
    {
        public const int SampleRate = 44100;

        /// <summary>Notes de la gamme pentatonique de do, à partir de do4 (262 Hz).</summary>
        public static float Pentatonic(int degree)
        {
            int[] steps = { 0, 2, 4, 7, 9 };
            int octave = degree >= 0 ? degree / 5 : (degree - 4) / 5;
            int d = degree - octave * 5;
            return 261.63f * (float)Math.Pow(2, octave + steps[d] / 12.0);
        }

        /// <summary>Atterrissage d'un poids : « toc » de bois, plus grave et plus sourd pour les gros poids.</summary>
        public static float[] Thud(int value)
        {
            int idx = WeightCatalog.IndexOf(value);
            float f0 = 196f * (float)Math.Pow(2, -idx / 7.0); // sol3 vers sol2
            var rng = new GameRng(7u + (uint)idx);
            float lp = 0f;
            return Render(0.14f + idx * 0.012f, (t, i) =>
            {
                float env = (float)Math.Exp(-t * (34f - idx * 2.5f));
                float f = f0 * (1f + 0.25f * (float)Math.Exp(-t * 60f));
                float body = (float)Math.Sin(2 * Math.PI * f * t) + 0.3f * (float)Math.Sin(2 * Math.PI * f * 2.01 * t) * (float)Math.Exp(-t * 50f);
                // Petit bruit filtré au contact (le bois qui claque).
                float n = rng.NextFloat() * 2f - 1f;
                lp += (n - lp) * 0.25f;
                float knock = lp * (float)Math.Exp(-t * 120f) * 0.6f;
                return (body * 0.75f + knock) * env;
            });
        }

        /// <summary>Fusion : « bloup » de bulle sur une note de la gamme ; plus le poids créé est gros, plus c'est aigu.</summary>
        public static float[] Pop(int createdValue)
        {
            int idx = WeightCatalog.IndexOf(createdValue);
            float f0 = Pentatonic(5 + idx); // do5 pour le 2, puis ré5, mi5, sol5…
            return Render(0.28f, (t, i) =>
            {
                // Attaque qui « glisse » d'un demi-ton vers la note (effet bulle), puis tenue juste.
                float f = f0 * (1f - 0.06f * (float)Math.Exp(-t * 40f));
                float env = Math.Min(1f, t / 0.003f) * (float)Math.Exp(-t * 11f);
                double ph = 2 * Math.PI * f * t;
                return (float)(Math.Sin(ph) * 0.7 + Math.Sin(ph * 2) * 0.18 * Math.Exp(-t * 20) + Math.Sin(ph * 3) * 0.06) * env;
            });
        }

        /// <summary>Le 128 s'envole : arpège montant de do majeur en cloches, avec un souffle léger.</summary>
        public static float[] Lift()
        {
            float dur = 1.3f;
            int[] degrees = { 5, 7, 8, 10, 12, 13, 15 };
            var rng = new GameRng(11);
            float lp = 0f;
            return Render(dur, (t, i) =>
            {
                float s = 0f;
                for (int n = 0; n < degrees.Length; n++)
                {
                    float start = n * 0.085f;
                    if (t < start) continue;
                    float lt = t - start;
                    float f = Pentatonic(degrees[n]);
                    s += (float)(Math.Sin(2 * Math.PI * f * lt) + 0.25 * Math.Sin(2 * Math.PI * f * 2.76 * lt) * Math.Exp(-lt * 8)) * 0.16f * (float)Math.Exp(-lt * 3.5f);
                }
                float nz = rng.NextFloat() * 2f - 1f;
                lp += (nz - lp) * 0.05f;
                s += lp * 0.25f * (float)Math.Sin(Math.PI * Math.Min(1f, t / dur));
                return s;
            });
        }

        /// <summary>Défaite : trois notes qui descendent (sol, mi, do), un peu tristes mais pas criardes.</summary>
        public static float[] Fall()
        {
            float[] notes = { Pentatonic(3), Pentatonic(2), Pentatonic(0) };
            float dur = 1.1f;
            return Render(dur, (t, i) =>
            {
                int n = Math.Min(2, (int)(t / 0.22f));
                float lt = t - n * 0.22f;
                float f = notes[n] * (n == 2 ? 1f - 0.03f * Math.Min(1f, lt / 0.6f) : 1f);
                double ph = 2 * Math.PI * f * t;
                float tone = (float)(Math.Sin(ph) * 0.6 + Math.Sin(ph * 2) * 0.15 + Math.Sin(ph * 3) * 0.08);
                float env = Math.Min(1f, lt / 0.01f) * (float)Math.Exp(-lt * (n == 2 ? 2.5f : 6f));
                return tone * env * 0.7f;
            });
        }

        /// <summary>Chaîne de fusions : une cloche qui monte la gamme à chaque fusion enchaînée.</summary>
        public static float[] Chime(int step)
        {
            float f = Pentatonic(8 + Math.Min(step, 8)); // à partir de sol5
            return Render(0.5f, (t, i) =>
                (float)(Math.Sin(2 * Math.PI * f * t) * 0.45 + Math.Sin(2 * Math.PI * f * 2.76 * t) * 0.12 * Math.Exp(-t * 12)) * (float)Math.Exp(-t * 6.5f) * Math.Min(1f, t / 0.002f));
        }

        /// <summary>Sauvetage de justesse : « ouf », sol puis do au-dessus.</summary>
        public static float[] Phew()
        {
            return Render(0.55f, (t, i) =>
            {
                bool second = t >= 0.14f;
                float f = second ? Pentatonic(5) : Pentatonic(3);
                float lt = second ? t - 0.14f : t;
                return (float)(Math.Sin(2 * Math.PI * f * t) * 0.45 + Math.Sin(4 * Math.PI * f * t) * 0.1) * (float)Math.Exp(-lt * 6f) * Math.Min(1f, lt / 0.004f);
            });
        }

        /// <summary>Poids perdu : « bloup » qui descend (mi puis do grave), net mais pas agressif.</summary>
        public static float[] Lost()
        {
            return Render(0.42f, (t, i) =>
            {
                bool second = t >= 0.12f;
                float f = second ? Pentatonic(-5) : Pentatonic(2);
                float lt = second ? t - 0.12f : t;
                float bend = 1f - 0.08f * Math.Min(1f, lt / 0.2f);
                return (float)(Math.Sin(2 * Math.PI * f * bend * t) * 0.55 + Math.Sin(4 * Math.PI * f * bend * t) * 0.12) * (float)Math.Exp(-lt * 8f) * Math.Min(1f, lt / 0.004f);
            });
        }

        /// <summary>Bouton : petit « tic » de bois.</summary>
        public static float[] Click() =>
            Render(0.05f, (t, i) => (float)(Math.Sin(2 * Math.PI * 1568 * t) * 0.3 + Math.Sin(2 * Math.PI * 784 * t) * 0.2) * (float)Math.Exp(-t * 110f));

        static float[] Render(float seconds, Func<float, int, float> f)
        {
            int n = (int)(seconds * SampleRate);
            var data = new float[n];
            int tail = Math.Min(600, n / 4);
            for (int i = 0; i < n; i++)
            {
                float v = f(i / (float)SampleRate, i);
                if (i > n - tail) v *= (n - i) / (float)tail; // fondu de sortie : pas de clic
                data[i] = v > 1f ? 1f : (v < -1f ? -1f : v);
            }
            return data;
        }
    }
}
