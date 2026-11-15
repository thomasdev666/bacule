using System;
using System.Collections.Generic;
using System.Text;

namespace Bascule.Core
{
    /// <summary>
    /// Pseudos visibles par tout le monde dans le classement : longueur, caractères autorisés et filtre de grossièretés
    /// (avec les déguisements habituels : majuscules, accents, chiffres à la place des lettres, lettres répétées).
    /// </summary>
    public static class PseudoFilter
    {
        public const int MinLength = 3, MaxLength = 16;

        // Racines refusées même au milieu d'un mot (elles n'apparaissent dans aucun mot courant).
        static readonly string[] Contains =
        {
            "connard", "connasse", "conasse", "salope", "salaud", "encule", "batard", "putain", "petasse", "couille",
            "branleur", "tapette", "gouine", "negre", "bougnoul", "youpin", "nazi", "hitler", "merde", "enfoire",
            "fuck", "bitch", "cunt", "pussy", "nigg", "whore", "slut", "porno",
        };

        // Mots courts refusés seulement s'ils forment un mot entier du pseudo (sinon « technique », « habite »,
        // « fichier » ou « computer » seraient bloqués à tort).
        static readonly string[] Words =
        {
            "con", "cons", "cul", "pd", "pede", "zob", "bite", "pute", "putes", "nique", "niquer", "ntm", "fdp", "chier",
            "chatte", "branle", "abruti", "debile", "mongol", "sexe", "sex", "porn", "shit", "dick", "fag", "tg",
        };

        /// <summary>Nettoie un pseudo ; renvoie null s'il est refusé.</summary>
        public static string Clean(string pseudo)
        {
            if (pseudo == null) return null;
            var sb = new StringBuilder();
            foreach (var c in pseudo.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            var s = sb.ToString();
            if (s.Length > MaxLength) s = s.Substring(0, MaxLength);
            if (s.Length < MinLength) return null;
            return IsAllowed(s) ? s : null;
        }

        /// <summary>
        /// Pseudo d'un autre joueur tel qu'on l'affiche (classement) : le filtre est appliqué aussi à la lecture, car un
        /// pseudo peut venir d'une ancienne version ou d'un appareil modifié. Refusé ou vide : <paramref name="fallback"/>.
        /// </summary>
        public static string Display(string pseudo, string fallback)
        {
            if (string.IsNullOrWhiteSpace(pseudo)) return fallback;
            string s = pseudo.Trim();
            if (s.Length > MaxLength + 4) s = s.Substring(0, MaxLength + 4); // jamais de nom interminable dans une ligne
            return IsAllowed(s) ? s : fallback;
        }

        public static bool IsAllowed(string pseudo)
        {
            string all = Normalize(pseudo);
            if (all.Length == 0) return false;
            foreach (var w in Contains) if (all.Contains(Normalize(w))) return false;
            // Mots entiers : on découpe sur les séparateurs et sur les changements minuscule/majuscule (« TonPere »).
            foreach (var token in Tokens(pseudo))
            {
                string t = Normalize(token);
                foreach (var w in Words) if (t == Normalize(w)) return false;
            }
            return true;
        }

        static IEnumerable<string> Tokens(string s)
        {
            var cur = new StringBuilder();
            char prev = '\0';
            foreach (var c in s)
            {
                bool sep = c == '_' || c == '-' || c == ' ' || c == '.';
                bool camel = char.IsUpper(c) && char.IsLower(prev);
                if ((sep || camel) && cur.Length > 0) { yield return cur.ToString(); cur.Clear(); }
                if (!sep) cur.Append(c);
                prev = c;
            }
            if (cur.Length > 0) yield return cur.ToString();
        }

        /// <summary>Minuscules, sans accents ni séparateurs, chiffres remplacés par les lettres qu'ils imitent, lettres répétées réduites.</summary>
        public static string Normalize(string s)
        {
            var sb = new StringBuilder();
            char prev = '\0';
            foreach (var raw in s.ToLowerInvariant())
            {
                char c = raw;
                switch (c)
                {
                    case 'à': case 'â': case 'ä': case 'á': case '4': case '@': c = 'a'; break;
                    case 'é': case 'è': case 'ê': case 'ë': case '3': c = 'e'; break;
                    case 'î': case 'ï': case 'í': case '1': case '!': c = 'i'; break;
                    case 'ô': case 'ö': case 'ó': case '0': c = 'o'; break;
                    case 'ù': case 'û': case 'ü': case 'ú': c = 'u'; break;
                    case 'ç': c = 'c'; break;
                    case '5': case '$': c = 's'; break;
                    case '7': c = 't'; break;
                }
                if (c < 'a' || c > 'z') continue;
                if (c == prev) continue;
                sb.Append(c);
                prev = c;
            }
            return sb.ToString();
        }
    }
}
