using System;

namespace Bascule.Core
{
    /// <summary>Progression des récompenses en vidéo de la boutique (sauvegardée dans Profile).</summary>
    [Serializable]
    public class AdRewardState
    {
        /// <summary>Vidéos regardées pour la tête exclusive, puis pour la planche assortie (on peut les étaler sur plusieurs jours).</summary>
        public int headViews;
        public int plankViews;
        /// <summary>Coffres ouverts avec une vidéo aujourd'hui.</summary>
        public string chestDay = "";
        public int chestsToday;
        /// <summary>Butins de coffre doublés avec une vidéo (statistique).</summary>
        public int chestsDoubled;
    }

    /// <summary>
    /// Récompenses en vidéo (toujours au choix du joueur, jamais imposées) :
    /// - la Superstar, tête exclusive, après adsForExclusiveHead vidéos (une à la fois ou d'affilée, quand on veut) ;
    /// - la planche « Scène étoilée », assortie, après adsForExclusivePlank vidéos de plus ;
    /// - un coffre gratuit par vidéo, adChestsPerDay fois par jour ;
    /// - doubler les pièces du coffre de fin de partie (une fois par coffre).
    /// Rien ne change le score ni la difficulté : ce sont des objets de style et des pièces (qui n'achètent que du style).
    /// </summary>
    public static class AdRewards
    {
        public static HeadDef ExclusiveHead
        {
            get
            {
                foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.Ads) return h;
                return null;
            }
        }

        public static PlankSkin ExclusivePlank
        {
            get
            {
                foreach (var s in PlankCatalog.All) if (s.AdExclusive) return s;
                return null;
            }
        }

        public static int HeadNeeded(AdTuning t) => Math.Max(1, t.adsForExclusiveHead);
        public static int PlankNeeded(AdTuning t) => Math.Max(1, t.adsForExclusivePlank);

        public static bool HeadOwned(Profile p)
        {
            var h = ExclusiveHead;
            return h == null || p.unlockedHeads.Contains(h.Id);
        }

        public static bool PlankOwned(Profile p)
        {
            var s = ExclusivePlank;
            return s == null || p.ownedPlanks.Contains(s.Id);
        }

        public static int HeadProgress(AdTuning t, Profile p) => HeadOwned(p) ? HeadNeeded(t) : Math.Min(HeadNeeded(t), p.adRewards.headViews);
        public static int PlankProgress(AdTuning t, Profile p) => PlankOwned(p) ? PlankNeeded(t) : Math.Min(PlankNeeded(t), p.adRewards.plankViews);

        /// <summary>Une vidéo vue jusqu'au bout pour la tête exclusive ; vrai si elle vient d'être gagnée.</summary>
        public static bool WatchedForHead(AdTuning t, Profile p)
        {
            if (HeadOwned(p)) return false;
            p.adRewards.headViews++;
            if (p.adRewards.headViews < HeadNeeded(t)) return false;
            p.unlockedHeads.Add(ExclusiveHead.Id);
            return true;
        }

        /// <summary>Une vidéo vue jusqu'au bout pour la planche exclusive ; vrai si elle vient d'être gagnée (elle est alors posée).</summary>
        public static bool WatchedForPlank(AdTuning t, Profile p)
        {
            if (PlankOwned(p)) return false;
            p.adRewards.plankViews++;
            if (p.adRewards.plankViews < PlankNeeded(t)) return false;
            p.ownedPlanks.Add(ExclusivePlank.Id);
            p.selectedPlank = ExclusivePlank.Id;
            return true;
        }

        static void Roll(AdRewardState s, string today)
        {
            if (s.chestDay == today) return;
            s.chestDay = today;
            s.chestsToday = 0;
        }

        public static int ChestsLeft(AdTuning t, Profile p, string today)
        {
            Roll(p.adRewards, today);
            return Math.Max(0, t.adChestsPerDay - p.adRewards.chestsToday);
        }

        /// <summary>Coffre gagné avec une vidéo : son contenu (déjà donné), ou null s'il n'en reste plus aujourd'hui.</summary>
        public static Chest.Loot OpenChest(AdTuning t, Profile p, string today, uint seed)
        {
            if (ChestsLeft(t, p, today) <= 0) return null;
            p.adRewards.chestsToday++;
            return Chest.Roll(p, seed ^ 0x5EEDu ^ (uint)(p.adRewards.chestsToday * 40503));
        }

        /// <summary>Double les pièces d'un coffre déjà ouvert (vidéo vue) ; renvoie les pièces ajoutées.</summary>
        public static int DoubleChest(Profile p, Chest.Loot loot)
        {
            if (loot == null || loot.Coins <= 0) return 0;
            Wallet.Add(p, loot.Coins);
            p.adRewards.chestsDoubled++;
            return loot.Coins;
        }

        public static void Sanitize(Profile p)
        {
            if (p.adRewards == null) p.adRewards = new AdRewardState();
            if (p.adRewards.chestDay == null) p.adRewards.chestDay = "";
            p.adRewards.headViews = Math.Max(0, p.adRewards.headViews);
            p.adRewards.plankViews = Math.Max(0, p.adRewards.plankViews);
            p.adRewards.chestsToday = Math.Max(0, p.adRewards.chestsToday);
        }
    }
}
