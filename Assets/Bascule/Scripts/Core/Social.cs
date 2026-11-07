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

    /// <summary>Codes amis : l'identifiant de joueur en ligne, présenté par groupes de 4 pour être lisible.</summary>
    public static class FriendCode
    {
        public const int MaxFriends = 50;

        public static string Format(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < playerId.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append('-');
                sb.Append(playerId[i]);
            }
            return sb.ToString();
        }

        /// <summary>Retire espaces et tirets d'un code saisi ou collé ; null s'il ne ressemble pas à un code.</summary>
        public static string Parse(string text)
        {
            if (text == null) return null;
            // Un message partagé contient « code ami : XXXX-XXXX… » : on prend le plus long mot alphanumérique.
            string best = "";
            var cur = new StringBuilder();
            foreach (var c in text + " ")
            {
                if (char.IsLetterOrDigit(c) && c < 128) cur.Append(c);
                else if (c == '-' && cur.Length > 0) { /* séparateur de groupes : on continue */ }
                else
                {
                    if (cur.Length > best.Length) best = cur.ToString();
                    cur.Clear();
                }
            }
            return best.Length >= 12 && best.Length <= 64 ? best : null;
        }

        /// <summary>Ajoute un ami ; renvoie false s'il est déjà là, si c'est soi-même ou si la liste est pleine.</summary>
        public static bool Add(Profile p, string playerId, string myId)
        {
            if (string.IsNullOrEmpty(playerId) || playerId == myId) return false;
            if (p.friends.Contains(playerId) || p.friends.Count >= MaxFriends) return false;
            p.friends.Add(playerId);
            return true;
        }
    }

    /// <summary>
    /// Modération du classement (règle 1.2 de l'App Store : contenu créé par les joueurs) :
    /// chaque joueur peut bloquer ou signaler un pseudo. Un joueur bloqué disparaît de son classement
    /// et de sa liste d'amis. Les signalements partent par e-mail vers l'éditeur (voir GameApp.ReportPlayer).
    /// </summary>
    public static class Moderation
    {
        public const int MaxBlocked = 500;

        public static bool IsBlocked(Profile p, string playerId)
            => !string.IsNullOrEmpty(playerId) && p.blocked != null && p.blocked.Contains(playerId);

        /// <summary>Bloque un joueur ; renvoie false si c'est soi-même, un identifiant vide, ou s'il l'est déjà.</summary>
        public static bool Block(Profile p, string playerId, string myId)
        {
            if (string.IsNullOrEmpty(playerId) || playerId == myId) return false;
            if (p.blocked == null) p.blocked = new System.Collections.Generic.List<string>();
            if (p.friends != null) p.friends.Remove(playerId);
            if (p.blocked.Contains(playerId)) return false;
            if (p.blocked.Count >= MaxBlocked) p.blocked.RemoveAt(0);
            p.blocked.Add(playerId);
            return true;
        }

        public static void UnblockAll(Profile p) => p.blocked?.Clear();
    }
}
