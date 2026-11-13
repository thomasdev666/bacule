using System;

namespace Bascule.Core
{
    /// <summary>
    /// Mise à jour conseillée (1.4.1) : quand l'App Store propose une version plus récente que celle du joueur, l'accueil
    /// montre « Nouvelle version » avec « Mettre à jour » et « Plus tard ». Jamais obligatoire, jamais pendant une partie,
    /// jamais au tout premier lancement ; au plus une fois par jour et <see cref="MaxAsksPerVersion"/> fois par version.
    /// </summary>
    public static class AppUpdate
    {
        public const int MaxAsksPerVersion = 3;

        /// <summary>Compare deux versions « 1.4 », « 1.4.1 », « 1.10 » chiffre par chiffre (−1, 0 ou 1). Texte illisible = 0.</summary>
        public static int Compare(string a, string b)
        {
            var pa = (a ?? "").Trim().Split('.');
            var pb = (b ?? "").Trim().Split('.');
            int n = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < n; i++)
            {
                int x = i < pa.Length ? Num(pa[i]) : 0;
                int y = i < pb.Length ? Num(pb[i]) : 0;
                if (x < 0 || y < 0) return 0;
                if (x != y) return x < y ? -1 : 1;
            }
            return 0;
        }

        static int Num(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return 0;
            return int.TryParse(s, out int v) && v >= 0 ? v : -1;
        }

        /// <summary>Vrai s'il faut proposer la mise à jour vers <paramref name="storeVersion"/> aujourd'hui.</summary>
        public static bool ShouldSuggest(Profile p, string currentVersion, string storeVersion, string today)
        {
            if (p == null || string.IsNullOrEmpty(storeVersion) || string.IsNullOrEmpty(currentVersion)) return false;
            if (p.gamesPlayed < 1) return false; // un nouveau joueur vient de télécharger la dernière version
            if (Compare(storeVersion, currentVersion) <= 0) return false;
            if (p.updateAskDay == today) return false;
            int asks = p.updateAskVersion == storeVersion ? p.updateAsks : 0;
            return asks < MaxAsksPerVersion;
        }

        /// <summary>À appeler quand la fenêtre est montrée.</summary>
        public static void MarkSuggested(Profile p, string storeVersion, string today)
        {
            if (p.updateAskVersion != storeVersion) { p.updateAskVersion = storeVersion; p.updateAsks = 0; }
            p.updateAsks++;
            p.updateAskDay = today;
        }
    }
}
