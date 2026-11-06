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
            public HeadDef Head; // tête de la boutique offerte (parfois)
        }

        /// <summary>Une partie terminée fait avancer le coffre ; renvoie le contenu s'il s'ouvre.</summary>
        public static Loot OnGameFinished(Profile p, uint seed, int steps = 1)
        {
            p.chestGames += Math.Max(1, steps);
            if (p.chestGames < GamesPerChest) return null;
            p.chestGames = 0;
            return Roll(p, seed);
        }

        /// <summary>Contenu d'un coffre (fin de partie ou cadeau du jour) : 60 à 200 pièces, parfois une tête.</summary>
        public static Loot Roll(Profile p, uint seed)
        {
            p.chestsOpened++;
            var rng = new GameRng(seed ^ (uint)(p.chestsOpened * 2654435761u));
            var loot = new Loot { Coins = 60 + 10 * (int)(rng.NextFloat() * 15) };
            // Une tête de la boutique, c'est rare : environ un coffre sur 12 (V18 : les têtes doivent se mériter).
            bool head = rng.NextFloat() < 0.08f;
            if (head)
            {
                var h = HeadCatalog.NextShopHead(p.unlockedHeads);
                if (h != null) { p.unlockedHeads.Add(h.Id); loot.Head = h; }
            }
            Wallet.Add(p, loot.Coins);
            return loot;
        }
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
