using System;

namespace Bascule.Core
{
    /// <summary>
    /// Générateur pseudo-aléatoire déterministe (xorshift32). Même graine = même suite sur tous les
    /// téléphones, indispensable pour le défi du jour et les duels. Ne pas remplacer par System.Random
    /// ni UnityEngine.Random, dont l'algorithme peut changer selon la plateforme ou la version.
    /// </summary>
    public sealed class GameRng
    {
        public uint State { get; private set; }

        public GameRng(uint seed) { State = seed == 0 ? 0x9E3779B9u : seed; }

        public uint NextUInt()
        {
            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }

        /// <summary>Flottant dans [0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        public void Restore(uint state) { State = state == 0 ? 0x9E3779B9u : state; }

        /// <summary>Hachage FNV-1a d'une chaîne : transforme « 2026-11-02 » en graine.</summary>
        public static uint HashSeed(string text)
        {
            uint hash = 2166136261u;
            foreach (char c in text ?? string.Empty)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return hash == 0 ? 1u : hash;
        }

        public static uint RandomSeed() => HashSeed(Guid.NewGuid().ToString());
    }
}
