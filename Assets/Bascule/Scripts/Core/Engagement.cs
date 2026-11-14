using System;

namespace Bascule.Core
{
    /// <summary>
    /// Récompenses qui répondent aux trois grandes causes de départ du modèle de 1000 joueurs (docs/RAPPORT.md) :
    /// « plus rien à gagner » → coffre toutes les 3 parties ; « toujours la même chose » → parties spéciales ;
    /// « trop de parties ratées » → objectif de partie accessible.
    /// </summary>
    public static class Chest
    {
        public const int GamesPerChest = 3;

        public sealed class Loot
        {
            public int Coins;
            public HeadDef Head; // tête de la boutique offerte (coffre légendaire)
            /// <summary>Rareté du coffre (1.5) : l'écran la dévoile tape après tape avant de l'ouvrir.</summary>
            public Rarity Tier;
        }

        /// <summary>
        /// Chances de chaque rareté, en % (Commun, Rare, Épique, Légendaire), affichées sous le coffre.
        /// Légendaire = une tête de la boutique : environ 1 coffre sur 17 (avant la 1.5 : 1 sur 12), pour que les
        /// têtes restent à découvrir longtemps.
        /// </summary>
        public static readonly int[] TierOdds = { 60, 25, 9, 6 };

        /// <summary>Pièces de chaque rareté : de min à max, par pas (Légendaire : en plus de la tête).</summary>
        static readonly int[,] TierCoins = { { 60, 110, 10 }, { 150, 230, 10 }, { 280, 400, 20 }, { 100, 150, 10 } };

        /// <summary>Une partie terminée fait avancer le coffre ; renvoie le contenu s'il s'ouvre.</summary>
        /// <summary>Parties pour remplir le coffre : le tout premier s'ouvre dès la première partie (rapport 1.3).</summary>
        public static int GamesNeeded(Profile p) => p.chestsOpened == 0 ? 1 : GamesPerChest;

        public static Loot OnGameFinished(Profile p, uint seed, int steps = 1)
        {
            p.chestGames += Math.Max(1, steps);
            if (p.chestGames < GamesNeeded(p)) return null;
            p.chestGames = 0;
            return Roll(p, seed);
        }

        /// <summary>Rareté pour un tirage entre 0 et 1.</summary>
        public static Rarity TierFor(float roll)
        {
            float acc = 0f;
            for (int i = 0; i < TierOdds.Length; i++)
            {
                acc += TierOdds[i] / 100f;
                if (roll < acc) return (Rarity)i;
            }
            return Rarity.Common;
        }

        /// <summary>
        /// Contenu d'un coffre (fin de partie, cadeau du jour, roue, vidéo, événement). La rareté est tirée d'abord et
        /// décide du contenu. Le tout premier coffre est au moins rare (on découvre que la rareté peut monter).
        /// Légendaire sans tête restante dans la boutique : épique.
        /// </summary>
        public static Loot Roll(Profile p, uint seed)
        {
            p.chestsOpened++;
            var rng = new GameRng(seed ^ (uint)(p.chestsOpened * 2654435761u));
            var tier = TierFor(rng.NextFloat());
            if (p.chestsOpened == 1 && tier == Rarity.Common) tier = Rarity.Rare;
            var loot = new Loot { Tier = tier };
            if (tier == Rarity.Legendary)
            {
                var h = HeadCatalog.NextShopHead(p.unlockedHeads);
                if (h != null) { p.unlockedHeads.Add(h.Id); loot.Head = h; }
                else loot.Tier = Rarity.Epic;
            }
            int t = (int)loot.Tier;
            int steps = (TierCoins[t, 1] - TierCoins[t, 0]) / TierCoins[t, 2] + 1;
            loot.Coins = TierCoins[t, 0] + TierCoins[t, 2] * Math.Min(steps - 1, (int)(rng.NextFloat() * steps));
            Wallet.Add(p, loot.Coins);
            return loot;
        }

        /// <summary>Pièces possibles pour une rareté (tests, aperçus).</summary>
        public static (int min, int max) CoinsRange(Rarity tier) => (TierCoins[(int)tier, 0], TierCoins[(int)tier, 1]);
    }

    /// <summary>Objectif de la partie : environ 60 % du record, arrondi ; l'atteindre rapporte des pièces.</summary>
    public static class RunGoal
    {
        public const int Reward = 25;

        public static int For(Profile p)
        {
            int best = Math.Max(0, p.bestScore);
            int goal = (int)Math.Round(Math.Max(300, best * 0.6) / 50.0) * 50;
            return goal;
        }
    }

    public enum SpecialKind { None, DoubleCoins, DoubleXp, ChestBoost }

    /// <summary>
    /// Une partie sur 4 (après les 4 premières) est « spéciale », annoncée à l'avance sur l'accueil.
    /// Elle change seulement les récompenses, jamais le score : le classement mondial reste équitable.
    /// </summary>
    public static class SpecialRun
    {
        public const int Every = 4;

        public static SpecialKind Next(Profile p)
        {
            int n = p.gamesPlayed + 1;
            if (n <= Every || n % Every != 0) return SpecialKind.None;
            int i = (n / Every) % 3;
            return i == 0 ? SpecialKind.DoubleCoins : i == 1 ? SpecialKind.DoubleXp : SpecialKind.ChestBoost;
        }

        /// <summary>Nombre de parties avant la prochaine partie spéciale (0 = c'est la prochaine).</summary>
        public static int GamesUntil(Profile p)
        {
            for (int k = 0; k < 2 * Every; k++)
            {
                int n = p.gamesPlayed + 1 + k;
                if (n > Every && n % Every == 0) return k;
            }
            return Every;
        }

        public static int CoinsMultiplier(SpecialKind k) => k == SpecialKind.DoubleCoins ? 2 : 1;
        public static int XpMultiplier(SpecialKind k) => k == SpecialKind.DoubleXp ? 2 : 1;
        public static int ChestSteps(SpecialKind k) => k == SpecialKind.ChestBoost ? 2 : 1;
    }
}
