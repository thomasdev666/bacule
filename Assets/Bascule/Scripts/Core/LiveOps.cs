using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bascule.Core
{
    // =====================================================================================================
    // Rendez-vous qui font revenir (rapport docs/RAPPORT_1_3.md) : quête du débutant, événement de la semaine,
    // roue de la chance. Règle d'or inchangée : rien de tout cela ne change le score ni la difficulté.
    // =====================================================================================================

    [Serializable]
    public class QuestState
    {
        /// <summary>Étapes de la quête du débutant réussies (0 à 3).</summary>
        public int done;
    }

    /// <summary>
    /// Quête du débutant : trois étapes faciles, affichées dès l'accueil, chacune récompensée tout de suite.
    /// 1. Jouer une partie : 150 pièces. 2. Faire 500 points en une partie : un coffre. 3. Jouer 5 parties : la planche
    /// Bambou (ou 300 pièces si on l'a déjà). Le joueur sait quoi faire, et sa première session dure plus longtemps.
    /// </summary>
    public static class BeginnerQuest
    {
        public enum Kind { Coins, Chest, Plank }

        public sealed class Step
        {
            public int Index;
            public string Key;
            public int Target;
            public Kind Reward;
            public int Coins;
        }

        public static readonly Step[] Steps =
        {
            new Step { Index = 0, Key = "quest_s1", Target = 1, Reward = Kind.Coins, Coins = 150 },
            new Step { Index = 1, Key = "quest_s2", Target = 500, Reward = Kind.Chest },
            new Step { Index = 2, Key = "quest_s3", Target = 5, Reward = Kind.Plank, Coins = 300 },
        };

        public const int BambooPlank = 1;

        public static bool Active(Profile p) => p.quest.done < Steps.Length;

        /// <summary>Avancement de l'étape (valeur, cible) : parties jouées, ou meilleur score.</summary>
        public static int Value(Profile p, Step s) => s.Index == 1 ? p.bestScore : p.gamesPlayed;

        public static bool Met(Profile p, Step s) => Value(p, s) >= s.Target;

        public sealed class Result
        {
            public Step Step;
            public int Coins;
            public PlankSkin Plank;
            public Chest.Loot Chest;
        }

        /// <summary>
        /// Après une partie (profil déjà à jour : parties jouées, record) : les étapes qui viennent d'être réussies, dans
        /// l'ordre, avec leur récompense déjà donnée. Le coffre (étape 2) est ouvert par l'appli devant le joueur.
        /// </summary>
        public static List<Result> OnRun(Profile p, uint seed)
        {
            var list = new List<Result>();
            while (Active(p) && Met(p, Steps[p.quest.done]))
            {
                var s = Steps[p.quest.done];
                var r = new Result { Step = s };
                switch (s.Reward)
                {
                    case Kind.Coins:
                        r.Coins = s.Coins;
                        Wallet.Add(p, s.Coins);
                        break;
                    case Kind.Chest:
                        r.Chest = Chest.Roll(p, seed ^ 0x9E5Bu);
                        break;
                    case Kind.Plank:
                        if (!Cosmetics.OwnsPlank(p, BambooPlank))
                        {
                            p.ownedPlanks.Add(BambooPlank);
                            p.selectedPlank = BambooPlank;
                            r.Plank = PlankCatalog.Get(BambooPlank);
                        }
                        else
                        {
                            r.Coins = s.Coins;
                            Wallet.Add(p, s.Coins);
                        }
                        break;
                }
                p.quest.done++;
                list.Add(r);
            }
            return list;
        }
    }

    // ----------------------------------------------------------------------------------------------- Semaine

    [Serializable]
    public class EventState
    {
        /// <summary>Lundi de la semaine en cours (aaaa-mm-jj) ; une autre semaine remet l'avancement à zéro.</summary>
        public string week = "";
        public int progress;
        /// <summary>Paliers déjà gagnés (0 à 3).</summary>
        public int tiers;
    }

    /// <summary>
    /// Événement de la semaine : chaque lundi (00:00 UTC), un monde à l'honneur et un objectif commun à remplir en
    /// plusieurs parties (cumuler des points, des fusions, des parties, des poids posés, du temps de tenue).
    /// Trois paliers : 150 pièces, une surprise gratuite, puis la « tête de la semaine » (une tête de la boutique qui
    /// change chaque semaine ; 800 pièces si on l'a déjà). Donne une raison de revenir plusieurs jours de suite.
    /// </summary>
    public static class WeeklyEvent
    {
        public enum Kind { TotalScore, Merges, Games, Drops, HoldSeconds }

        public sealed class Def
        {
            public string Week;
            public int Number;
            public Kind Kind;
            public int Target;
            /// <summary>Monde à l'honneur (index dans Theme.All) : couleur et nom de l'événement.</summary>
            public int Theme;
            /// <summary>Tête de la semaine (dernier palier).</summary>
            public HeadDef Head;
        }

        static readonly (Kind kind, int target)[] Goals =
        {
            (Kind.TotalScore, 15000), (Kind.Merges, 120), (Kind.Games, 15), (Kind.Drops, 350), (Kind.HoldSeconds, 900),
        };

        public static readonly float[] TierFractions = { 1f / 3f, 2f / 3f, 1f };
        public const int TierCoins = 150, OwnedHeadCoins = 800;

        static readonly DateTime Epoch = new DateTime(2024, 1, 1); // un lundi

        /// <summary>Lundi (UTC) de la semaine de <paramref name="utc"/>.</summary>
        public static string WeekKey(DateTime utc)
        {
            int days = (int)Math.Floor((utc.Date - Epoch).TotalDays);
            int monday = days - ((days % 7) + 7) % 7;
            return Epoch.AddDays(monday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static int WeekNumber(string week)
        {
            if (!DateTime.TryParseExact(week, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return 0;
            return Math.Max(0, (int)Math.Floor((d - Epoch).TotalDays) / 7);
        }

        public static Def For(string week)
        {
            int n = WeekNumber(week);
            var g = Goals[n % Goals.Length];
            var shop = new List<HeadDef>();
            foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.Shop) shop.Add(h);
            return new Def
            {
                Week = week,
                Number = n,
                Kind = g.kind,
                Target = g.target,
                // Les mondes au-delà de la prairie, chacun à son tour (la plage, la forêt, le désert…).
                Theme = 5 + (n * 3) % 9,
                Head = shop.Count > 0 ? shop[(n * 7) % shop.Count] : null,
            };
        }

        /// <summary>Temps restant avant le lundi suivant (UTC).</summary>
        public static TimeSpan Left(DateTime utc)
        {
            var monday = DateTime.ParseExact(WeekKey(utc), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return monday.AddDays(7) - utc;
        }

        public static void Roll(EventState s, string week)
        {
            if (s.week == week) return;
            s.week = week;
            s.progress = 0;
            s.tiers = 0;
        }

        public static int Gain(Def d, RunStats r)
        {
            switch (d.Kind)
            {
                case Kind.TotalScore: return Math.Max(0, r.Score);
                case Kind.Merges: return Math.Max(0, r.Merges);
                case Kind.Games: return 1;
                case Kind.Drops: return Math.Max(0, r.Drops);
                case Kind.HoldSeconds: return Math.Max(0, (int)r.Seconds);
                default: return 0;
            }
        }

        public static int TierTarget(Def d, int tier) => (int)Math.Ceiling(d.Target * TierFractions[tier]);

        public sealed class Reward
        {
            public int Tier;
            public int Coins;
            public bool Ticket;
            public HeadDef Head;
        }

        /// <summary>Une partie compte pour l'événement ; renvoie les paliers qui viennent d'être gagnés (récompenses données).</summary>
        public static List<Reward> OnRun(Profile p, Def d, RunStats r)
        {
            Roll(p.week, d.Week);
            var list = new List<Reward>();
            if (p.week.tiers >= TierFractions.Length) return list;
            p.week.progress = Math.Min(d.Target, p.week.progress + Gain(d, r));
            while (p.week.tiers < TierFractions.Length && p.week.progress >= TierTarget(d, p.week.tiers))
            {
                var w = new Reward { Tier = p.week.tiers };
                switch (p.week.tiers)
                {
                    case 0:
                        w.Coins = TierCoins;
                        Wallet.Add(p, TierCoins);
                        break;
                    case 1:
                        w.Ticket = true;
                        p.surpriseTickets = Math.Min(9, p.surpriseTickets + 1);
                        break;
                    default:
                        if (d.Head != null && !p.unlockedHeads.Contains(d.Head.Id))
                        {
                            p.unlockedHeads.Add(d.Head.Id);
                            w.Head = d.Head;
                        }
                        else
                        {
                            w.Coins = OwnedHeadCoins;
                            Wallet.Add(p, OwnedHeadCoins);
                        }
                        break;
                }
                p.week.tiers++;
                list.Add(w);
            }
            return list;
        }
    }

    // ----------------------------------------------------------------------------------------------- Roue

    public enum WheelPrize { Coins, Chest, Ticket, Shield, Boost, Jackpot }

    public sealed class WheelSlot
    {
        public WheelPrize Kind;
        public int Coins;
        /// <summary>Poids du tirage (chance = poids / total), affichée au joueur.</summary>
        public float Weight;
        public string Color;
    }

    [Serializable]
    public class WheelState
    {
        /// <summary>Dernier tour gratuit (secondes Unix).</summary>
        public double lastFree = -1e12;
        /// <summary>Tours en plus avec une vidéo : jour et nombre.</summary>
        public string adDay = "";
        public int adSpins;
        public int spins;
        public int jackpots;
    }

    /// <summary>
    /// Roue de la chance : un tour gratuit toutes les 24 heures, et jusqu'à 2 tours en plus par jour avec une vidéo.
    /// Huit lots (chances affichées) : pièces, coffre, surprise gratuite, protège-série, pièces ×2 pendant 3 parties,
    /// et le gros lot : la tête « Chanceux », introuvable ailleurs (1 500 pièces si on l'a déjà).
    /// </summary>
    public static class Wheel
    {
        public const double Cooldown = 24 * 3600;
        public const int AdSpinsPerDay = 2;
        public const int BoostGames = 3, BoostMax = 9, JackpotCoins = 1500, ShieldCoins = 150;

        /// <summary>Les parts, dans l'ordre des aiguilles d'une montre à partir du haut.</summary>
        public static readonly WheelSlot[] Slots =
        {
            new WheelSlot { Kind = WheelPrize.Coins, Coins = 50, Weight = 24, Color = "#FFB703" },
            new WheelSlot { Kind = WheelPrize.Chest, Weight = 13, Color = "#A66CFF" },
            new WheelSlot { Kind = WheelPrize.Coins, Coins = 100, Weight = 20, Color = "#4A9BFF" },
            new WheelSlot { Kind = WheelPrize.Ticket, Weight = 7, Color = "#FF5A5F" },
            new WheelSlot { Kind = WheelPrize.Coins, Coins = 250, Weight = 12, Color = "#5BD16E" },
            new WheelSlot { Kind = WheelPrize.Shield, Weight = 8, Color = "#2EC4A6" },
            new WheelSlot { Kind = WheelPrize.Boost, Weight = 13, Color = "#FF8A3D" },
            new WheelSlot { Kind = WheelPrize.Jackpot, Weight = 3, Color = "#3D3B5C" },
        };

        public static float Chance(int i)
        {
            float total = 0f;
            foreach (var s in Slots) total += s.Weight;
            return Slots[i].Weight / total;
        }

        public static bool FreeReady(WheelState s, double now) => now - s.lastFree >= Cooldown;

        public static double FreeIn(WheelState s, double now) => Math.Max(0, s.lastFree + Cooldown - now);

        public static int AdSpinsLeft(WheelState s, string today) => s.adDay == today ? Math.Max(0, AdSpinsPerDay - s.adSpins) : AdSpinsPerDay;

        /// <summary>Part tirée pour un nombre <paramref name="r"/> dans [0, 1).</summary>
        public static int Pick(double r)
        {
            float total = 0f;
            foreach (var s in Slots) total += s.Weight;
            double x = r * total, acc = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                acc += Slots[i].Weight;
                if (x < acc) return i;
            }
            return Slots.Length - 1;
        }

        public sealed class Result
        {
            public int Slot;
            public WheelPrize Kind;
            public int Coins;
            public HeadDef Head;
            public Chest.Loot Chest;
            public int BoostGames;
        }

        /// <summary>
        /// Tourne la roue (tour gratuit, ou tour en plus après une vidéo) : tire un lot et le donne. Null si aucun tour
        /// n'est disponible. Le coffre est ouvert par l'appli devant le joueur (déjà donné ici).
        /// </summary>
        public static Result Spin(Profile p, bool viaAd, double now, string today, uint seed)
        {
            var s = p.wheel;
            if (viaAd)
            {
                if (AdSpinsLeft(s, today) <= 0) return null;
                if (s.adDay != today) { s.adDay = today; s.adSpins = 0; }
                s.adSpins++;
            }
            else
            {
                if (!FreeReady(s, now)) return null;
                s.lastFree = now;
            }
            s.spins++;
            var rng = new GameRng(GameRng.Mix(seed ^ (uint)(s.spins * 2654435761u)));
            int i = Pick(rng.NextFloat());
            var slot = Slots[i];
            var res = new Result { Slot = i, Kind = slot.Kind };
            switch (slot.Kind)
            {
                case WheelPrize.Coins:
                    res.Coins = slot.Coins;
                    Wallet.Add(p, slot.Coins);
                    break;
                case WheelPrize.Chest:
                    res.Chest = Chest.Roll(p, seed ^ 0xC4E57u);
                    break;
                case WheelPrize.Ticket:
                    p.surpriseTickets = Math.Min(9, p.surpriseTickets + 1);
                    break;
                case WheelPrize.Shield:
                    if (p.streakShields < StreakShield.Max) p.streakShields++;
                    else { res.Coins = ShieldCoins; Wallet.Add(p, ShieldCoins); }
                    break;
                case WheelPrize.Boost:
                    p.coinBoostGames = Math.Min(BoostMax, p.coinBoostGames + BoostGames);
                    res.BoostGames = BoostGames;
                    break;
                case WheelPrize.Jackpot:
                    s.jackpots++;
                    var head = LuckyHead;
                    if (head != null && !p.unlockedHeads.Contains(head.Id)) { p.unlockedHeads.Add(head.Id); res.Head = head; }
                    else { res.Coins = JackpotCoins; Wallet.Add(p, JackpotCoins); }
                    break;
            }
            return res;
        }

        public static HeadDef LuckyHead
        {
            get
            {
                foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.Wheel) return h;
                return null;
            }
        }

        /// <summary>Pièces de la partie doublées par le lot « pièces ×2 » : consomme une partie du bonus.</summary>
        public static bool UseBoost(Profile p)
        {
            if (p.coinBoostGames <= 0) return false;
            p.coinBoostGames--;
            return true;
        }
    }

    /// <summary>Parrainage « Duo » : la tête se gagne dès qu'on a un ami réciproque.</summary>
    public static class DuoHead
    {
        public static HeadDef Head
        {
            get
            {
                foreach (var h in HeadCatalog.All) if (h.Unlock == UnlockKind.Friends) return h;
                return null;
            }
        }

        /// <summary>Donne la tête Duo si elle manque ; vrai si elle vient d'être gagnée.</summary>
        public static bool Grant(Profile p)
        {
            var h = Head;
            if (h == null || p.unlockedHeads.Contains(h.Id)) return false;
            p.unlockedHeads.Add(h.Id);
            return true;
        }
    }
}
