using System;
using System.Text;

namespace Bascule.Core
{
    /// <summary>
    /// Code de défi partagé sur la carte de score : c'est la graine de la partie. Un ami qui le saisit
    /// reçoit exactement la même suite de poids (duel simple, sans serveur). Format : 8 caractères,
    /// alphabet sans lettres ambiguës (pas de O/0, I/1), affiché « ABCD-EFGH ».
    /// </summary>
    public static class ChallengeCode
    {
        const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // 32 symboles = 5 bits
        const int Length = 8; // 40 bits : 32 pour la graine + 8 de contrôle

        public static string Encode(uint seed)
        {
            ulong value = ((ulong)seed << 8) | Checksum(seed);
            var sb = new StringBuilder(Length + 1);
            for (int i = Length - 1; i >= 0; i--)
            {
                sb.Append(Alphabet[(int)((value >> (i * 5)) & 31)]);
                if (i == 4) sb.Append('-');
            }
            return sb.ToString();
        }

        public static bool TryDecode(string code, out uint seed)
        {
            seed = 0;
            if (string.IsNullOrEmpty(code)) return false;
            ulong value = 0;
            int count = 0;
            foreach (char c in code.ToUpperInvariant())
            {
                if (c == '-' || c == ' ') continue;
                int idx = Alphabet.IndexOf(c);
                if (idx < 0) return false;
                value = (value << 5) | (uint)idx;
                count++;
            }
            if (count != Length) return false;
            uint s = (uint)(value >> 8);
            if ((byte)(value & 0xFF) != Checksum(s)) return false;
            seed = s;
            return true;
        }

        static byte Checksum(uint seed)
        {
            uint h = seed * 2654435761u;
            return (byte)((h >> 24) ^ (h >> 16) ^ 0x5A);
        }
    }
}
