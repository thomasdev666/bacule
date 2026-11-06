using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bascule.Core
{
    /// <summary>Ce qu'on raconte d'une partie quand on la partage (voir docs/PARTAGE.md).</summary>
    public sealed class BragInfo
    {
        public GameMode Mode;
        public string DayKey = "";
        public uint Seed;
        public int Score;
        public bool NewBest;
        public int PreviousBest;
        public float BestSave;
        public int Saves;
        public int BestChain;
        public int Balloons;
        public int MaxValue;
        public int LostWeights;
        public float Seconds;
        /// <summary>Danger maximal atteint par tranche de partie (0 = calme, 1 = limite de la chute).</summary>
        public IList<float> Tension;
    }

    /// <summary>
    /// Partage « à la Wordle » : un résumé court, lisible sans avoir joué, qui raconte la partie sans la dévoiler
    /// (la frise de tension 🟩🟨🟧🟥💥), un titre qui fait réagir et un code pour relever le défi.
    /// Tout est calculé ici, en C# pur, pour être testé.
    /// </summary>
    public static class Brag
    {
        public const string Site = "orpheex.fr/bascule";
        public const string Hashtag = "#DéfiBascule";
        public const int MaxSquares = 10;
        /// <summary>Premier jour du Défi du jour (n°1). Les numéros servent à comparer « le n°12 » entre amis.</summary>
        public static readonly DateTime FirstDay = new DateTime(2026, 11, 1);

        public static int DayNumber(string dayKey)
        {
            if (!DateTime.TryParseExact(dayKey ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return 1;
            return Math.Max(1, (int)(d - FirstDay).TotalDays + 1);
        }

        /// <summary>
        /// Titre de la partie : le détail le plus marquant d'abord (un exploit ou un échec drôle), sinon le niveau.
        /// Un titre absurde (« Mains de beurre ») fait autant parler qu'un exploit.
        /// </summary>
        public static string TitleKey(BragInfo b)
        {
            if (b.Seconds > 0 && b.Seconds < 20f && b.Score < 300) return "title_express";
            if (b.BestSave >= 0.97f) return "title_tightrope";
            if (b.BestChain >= 5) return "title_combo";
            if (b.Balloons >= 2) return "title_balloon";
            if (b.LostWeights >= 5) return "title_butter";
            if (b.Saves >= 3) return "title_survivor";
            if (b.MaxValue >= 64) return "title_strong";
            if (b.Score < 1000) return "title_apprentice";
            if (b.Score < 3000) return "title_balancer";
            if (b.Score < 8000) return "title_master";
            return "title_legend";
        }

        public static string Title(BragInfo b) => Loc.Get(TitleKey(b));

        /// <summary>Regroupe les tranches pour ne pas dépasser <paramref name="max"/> cases (on garde le pic de chaque groupe).</summary>
        public static List<float> Compress(IList<float> peaks, int max = MaxSquares)
        {
            var result = new List<float>();
            if (peaks == null || peaks.Count == 0 || max <= 0) return result;
            if (peaks.Count <= max) { result.AddRange(peaks); return result; }
            for (int i = 0; i < max; i++)
            {
                int from = i * peaks.Count / max, to = (i + 1) * peaks.Count / max;
                float m = 0;
                for (int k = from; k < to; k++) m = Math.Max(m, peaks[k]);
                result.Add(m);
            }
            return result;
        }

        public static string Square(float danger) =>
            danger < 0.5f ? "🟩" : danger < 0.75f ? "🟨" : danger < 0.9f ? "🟧" : "🟥";

        /// <summary>Frise de tension : une case par tranche, et 💥 pour la chute finale.</summary>
        public static string Tension(IList<float> peaks, bool fell = true)
        {
            var sb = new StringBuilder();
            foreach (var p in Compress(peaks)) sb.Append(Square(p));
            if (fell) sb.Append("💥");
            return sb.ToString();
        }

        /// <summary>À combien de points du record (null si record battu ou trop loin pour en parler).</summary>
        public static int? PointsFromBest(BragInfo b)
        {
            if (b.NewBest || b.PreviousBest <= 0 || b.Score <= 0) return null;
            int gap = b.PreviousBest - b.Score;
            if (gap <= 0 || b.Score < b.PreviousBest * 0.85f) return null;
            return gap;
        }

        /// <summary>
        /// Message envoyé avec l'image (aussi copié dans le presse-papiers) : quelques phrases qui se lisent comme un
        /// message entre amis. D'abord le score (et le record), puis ce qui s'est passé, puis le défi et le lien.
        /// Pas de frise de carrés de couleur : elle est sur l'image, elle ne disait rien dans un message.
        /// </summary>
        public static string Summary(BragInfo b)
        {
            var sb = new StringBuilder();
            bool daily = b.Mode == GameMode.Daily;
            string score = Loc.FormatScore(b.Score);
            if (daily) sb.AppendLine(Loc.Format(b.NewBest ? "brag_hook_daily_record" : "brag_hook_daily", DayNumber(b.DayKey), score));
            else if (b.NewBest) sb.AppendLine(Loc.Format("brag_hook_record", score));
            else if (PointsFromBest(b) is int gap) sb.AppendLine(Loc.Format("brag_hook_close", score, Loc.FormatScore(gap)));
            else sb.AppendLine(Loc.Format("brag_hook", score));
            // Ce qui s'est passé : le titre de la partie, puis le moment fort (sauvetage, combo, montgolfière).
            string moment = b.BestSave >= 0.85f ? Loc.Format("brag_moment_save", NearMissTracker.DisplayPercent(b.BestSave))
                : b.Balloons > 0 ? Loc.Format("brag_moment_balloon", b.Balloons)
                : b.BestChain >= 3 ? Loc.Format("brag_moment_combo", b.BestChain)
                : null;
            sb.AppendLine(moment == null ? Loc.Format("brag_story", Title(b)) : Loc.Format("brag_story_moment", Title(b), moment));
            sb.AppendLine();
            sb.AppendLine(daily ? Loc.Get("brag_daily_call") : Loc.Format("brag_challenge", ChallengeCode.Encode(b.Seed)));
            sb.Append("👉 ").Append(Site);
            return sb.ToString();
        }
    }
}
