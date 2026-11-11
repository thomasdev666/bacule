using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    // =====================================================================================================
    // Réglages
    // =====================================================================================================

    /// <summary>Pièces, niveaux, missions, cadeau du jour. Les valeurs viennent de bascule_tuning.json (section « economy »).</summary>
    [Serializable]
    public class EconomyTuning
    {
        /// <summary>Pièces gagnées par partie = score / scorePerCoin.</summary>
        public int scorePerCoin = 40;
        /// <summary>XP par partie = xpPerGame + score / scorePerXp.</summary>
        public int xpPerGame = 10;
        public int scorePerXp = 10;
        /// <summary>XP pour passer du niveau n au suivant = levelXpBase + levelXpStep × n.</summary>
        public int levelXpBase = 75;
        public int levelXpStep = 50;
        /// <summary>Pièces offertes au passage du niveau n = levelCoinsBase + levelCoinsStep × n.</summary>
        public int levelCoinsBase = 40;
        public int levelCoinsStep = 8;
        /// <summary>Cadeau du jour, jour 1 à 7 de suite (un jour manqué : on repart au jour 1) : pièces de chaque jour.</summary>
        public int[] dailyGift = { 50, 0, 120, 0, 200, 0, 300 };
        /// <summary>
        /// Objet du jour, en plus des pièces : "coins" (rien de plus), "chest" (un coffre), "ticket" (une ouverture
        /// gratuite de la machine à surprises), "plank" (la prochaine planche qu'on n'a pas ; sinon des pièces).
        /// </summary>
        public string[] dailyGiftItems = { "coins", "chest", "coins", "ticket", "coins", "chest", "plank" };
        /// <summary>Bonus quand les 3 missions du jour sont faites.</summary>
        public int missionsBonus = 100;
        public int missionXp = 40;
    }

    /// <summary>
    /// Règles des publicités. Principe : une pub imposée (interstitiel) ne coupe jamais une partie, n'arrive jamais
    /// avant que le joueur ait accroché, jamais juste après un record, et jamais deux fois de suite.
    /// Les pubs récompensées sont toujours au choix du joueur.
    /// </summary>
    [Serializable]
    public class AdTuning
    {
        public int firstInterstitialAfterGames = 6;
        public int interstitialEveryGames = 3;
        public float minSecondsBetweenInterstitials = 180f;
        public float minSecondsAfterRewarded = 120f;
        /// <summary>Pas de pub imposée après une partie de moins de N secondes (la frustration s'ajouterait à la pub).</summary>
        public float minRunSecondsForInterstitial = 25f;
        public int maxInterstitialsPerDay = 12;
        public int maxRewardedPerDay = 20;
        /// <summary>Pièces gratuites en vidéo dans la boutique.</summary>
        public int freeCoinsPerDay = 5;
        public int freeCoinsAmount = 60;
        /// <summary>Après N pubs imposées vues, on propose une fois de les retirer (achat).</summary>
        public int removeAdsHintAfter = 4;
        /// <summary>Offre de départ : proposée après N parties, valable offerHours heures.</summary>
        public int starterOfferAfterGames = 8;
        public float starterOfferHours = 48f;
    }

    // =====================================================================================================
    // État sauvegardé (dans Profile)
    // =====================================================================================================

    public enum MissionKind { PlayGames, ScoreInGame, TotalScore, Merges, MakeValue, Balloon, Combo, NearMiss, PlayDaily, /* plus proposée (cœurs retirés) : gardée pour les anciennes sauvegardes */ NoHeartLost,
        // 1.3 : missions d'exploit (toujours ajouter à la fin : les types sont sauvegardés)
        SurviveGusts, HoldSeconds, CleanScore,
        // Duel : jouer avec un ami (seulement si le duel est disponible dans la version)
        PlayDuels, WinDuels }

    [Serializable]
    public class Mission
    {
        public MissionKind kind;
        public int target;
        public int progress;
        public int reward;
        public bool done;
    }

    [Serializable]
    public class MissionState
    {
        public string day = "";
        public List<Mission> list = new List<Mission>();
        public bool bonusClaimed;
        public bool rerolled;
        public int generation;
    }

    [Serializable]
    public class GiftState
    {
        public string lastDay = "";
        public int streak;
    }

    [Serializable]
    public class AdState
    {
        public int gamesSinceInterstitial;
        public double lastInterstitial = -1e9;
        public double lastRewarded = -1e9;
        public string day = "";
        public int interstitialsToday;
        public int rewardedToday;
        public int freeCoinsToday;
        public int interstitialsTotal;
        public int rewardedTotal;
        public bool removeAdsHintShown;
    }

    [Serializable]
    public class OfferState
    {
        /// <summary>Fin de l'offre de départ (secondes Unix). 0 = jamais proposée.</summary>
        public double starterEnds;
        public bool starterPopupShown;
    }

    /// <summary>Ce qu'une partie a apporté à l'économie, pour l'écran de fin.</summary>
    public sealed class EconomyGain
    {
        public int Coins;
        public int Xp;
        public int LevelsGained;
        public int LevelCoins;
        public int LevelBefore, LevelAfter;
        public float XpFractionBefore, XpFractionAfter;
        public List<Mission> MissionsDone = new List<Mission>();
        public int MissionCoins;
        public bool MissionsBonus;
        public int BonusCoins;
        public List<HeadDef> LevelHeads = new List<HeadDef>();
        /// <summary>Objectif de la partie atteint (pièces en plus).</summary>
        public bool GoalReached;
        public int GoalCoins;
        /// <summary>Défi du jour réussi à cette partie (null sinon) et ses pièces.</summary>
        public DailyChallenge.Def DailyObjective;
        public int DailyCoins;
        /// <summary>Coffre ouvert à cette partie (null sinon), et progression du coffre après la partie.</summary>
        public Chest.Loot Chest;
        public int ChestProgress;
        public SpecialKind Special;
        public int Total => Coins + LevelCoins + MissionCoins + BonusCoins + GoalCoins + DailyCoins + (Chest?.Coins ?? 0);
    }

    /// <summary>Tout ce qu'une partie a accompli (pour les missions et les têtes).</summary>
    public struct RunStats
    {
        public int Score;
        public int Merges;
        public int MaxValue;
        public int Balloons;
        public int BestChain;
        public float BestSave;
        public bool Daily;
        /// <summary>Jour du défi (mode défi du jour).</summary>
        public string DayKey;
        /// <summary>Rafales de vent traversées sans tomber, durée de la partie (s), poids perdus.</summary>
        public int Gusts;
        public float Seconds;
        public int LostWeights;
        /// <summary>Poids posés.</summary>
        public int Drops;
        /// <summary>Règle spéciale de la partie (récompenses seulement), objectif de la partie, graine du coffre.</summary>
        public SpecialKind Special;
        public int Goal;
        public uint Seed;
    }

    // =====================================================================================================
    // Règles
    // =====================================================================================================

    public static class Levels
    {
        public static int XpToNext(EconomyTuning t, int level) => Math.Max(1, t.levelXpBase + t.levelXpStep * Math.Max(1, level));
        public static int GameXp(EconomyTuning t, int score) => t.xpPerGame + Math.Max(0, score) / Math.Max(1, t.scorePerXp);
        public static int LevelCoins(EconomyTuning t, int level) => t.levelCoinsBase + t.levelCoinsStep * level;
        public static float Fraction(EconomyTuning t, Profile p) => Math.Min(1f, p.xp / (float)XpToNext(t, p.level));

        /// <summary>Ajoute de l'XP ; renvoie le nombre de niveaux gagnés (et crédite leurs pièces).</summary>
        public static int AddXp(EconomyTuning t, Profile p, int xp, out int coins)
        {
            coins = 0;
            int gained = 0;
            p.xp += Math.Max(0, xp);
            while (p.xp >= XpToNext(t, p.level))
            {
                p.xp -= XpToNext(t, p.level);
                p.level++;
                gained++;
                coins += LevelCoins(t, p.level);
            }
            p.coins += coins;
            return gained;
        }
    }

    public static class Wallet
    {
        public static int CoinsForScore(EconomyTuning t, int score) => Math.Max(0, score) / Math.Max(1, t.scorePerCoin);

        public static void Add(Profile p, int coins)
        {
            if (coins <= 0) return;
            p.coins += coins;
            p.coinsEarned += coins;
        }

        public static bool Spend(Profile p, int coins)
        {
            if (coins < 0 || p.coins < coins) return false;
            p.coins -= coins;
            return true;
        }
    }

    public enum GiftItem { Coins, Chest, Ticket, Plank }

    /// <summary>Ce que donne un jour du cadeau : des pièces et, certains jours, un coffre, un ticket ou une planche.</summary>
    public sealed class GiftResult
    {
        public int Day;
        public GiftItem Item;
        public int Coins;
        public Chest.Loot Chest;
        public PlankSkin Plank;
        /// <summary>Pièces que la vidéo « ×2 » double : celles du jour et celles du coffre.</summary>
        public int DoublableCoins => Coins + (Chest?.Coins ?? 0);
    }

    public static class DailyGift
    {
        /// <summary>Pièces données au jour 7 si l'on a déjà toutes les planches.</summary>
        public const int PlankFallbackCoins = 600;

        public static bool Available(GiftState g, string today) => g.lastDay != today;

        /// <summary>Protège-séries utilisés par le dernier cadeau récupéré (pour le dire au joueur).</summary>
        public static int ShieldsUsedLastClaim;

        /// <summary>Jour du cycle (1 à 7) qui sera donné aujourd'hui.</summary>
        public static int NextDay(GiftState g, string today, int cycle, int shields = 0)
        {
            if (!Available(g, today)) return ((g.streak - 1) % cycle) + 1;
            int streak = StreakShield.Continues(g.lastDay, today, shields, out _) ? g.streak + 1 : 1;
            return ((streak - 1) % cycle) + 1;
        }

        public static int Cycle(EconomyTuning t) => Math.Max(1, t.dailyGift?.Length ?? 1);

        public static int Amount(EconomyTuning t, int day) => t.dailyGift == null || t.dailyGift.Length == 0 ? 0 : t.dailyGift[Math.Min(t.dailyGift.Length, Math.Max(1, day)) - 1];

        public static GiftItem ItemOf(EconomyTuning t, int day)
        {
            var items = t.dailyGiftItems;
            if (items == null || items.Length == 0) return GiftItem.Coins;
            switch (items[Math.Min(items.Length, Math.Max(1, day)) - 1])
            {
                case "chest": return GiftItem.Chest;
                case "ticket": return GiftItem.Ticket;
                case "plank": return GiftItem.Plank;
                default: return GiftItem.Coins;
            }
        }

        /// <summary>La planche que donnerait le jour « planche » : la moins chère qu'on n'a pas encore (ou null).</summary>
        public static PlankSkin NextPlank(Profile p)
        {
            PlankSkin best = null;
            foreach (var s in PlankCatalog.All)
                if (s.Price > 0 && !Cosmetics.OwnsPlank(p, s.Id) && (best == null || s.Price < best.Price)) best = s;
            return best;
        }

        /// <summary>Donne le cadeau du jour ; renvoie ce qui a été donné (null si déjà pris aujourd'hui).</summary>
        public static GiftResult Claim(EconomyTuning t, Profile p, string today, uint seed = 0)
        {
            var g = p.gift;
            if (!Available(g, today)) return null;
            int day = NextDay(g, today, Cycle(t), p.streakShields);
            // Jour manqué : un protège-série en réserve garde la série (il est consommé).
            bool keeps = StreakShield.Continues(g.lastDay, today, p.streakShields, out int used);
            if (keeps && used > 0) { p.streakShields -= used; ShieldsUsedLastClaim = used; } else ShieldsUsedLastClaim = 0;
            g.streak = keeps ? g.streak + 1 : 1;
            g.lastDay = today;
            var r = new GiftResult { Day = day, Item = ItemOf(t, day), Coins = Amount(t, day) };
            switch (r.Item)
            {
                case GiftItem.Chest: r.Chest = Bascule.Core.Chest.Roll(p, seed ^ GameRng.HashSeed(today)); break;
                case GiftItem.Ticket: p.surpriseTickets++; break;
                case GiftItem.Plank:
                    r.Plank = NextPlank(p);
                    if (r.Plank != null) p.ownedPlanks.Add(r.Plank.Id);
                    else r.Coins += PlankFallbackCoins;
                    break;
            }
            Wallet.Add(p, r.Coins);
            return r;
        }
    }

    public static class Missions
    {
        public const int Count = 3;

        /// <summary>Nouvelles missions si le jour a changé.</summary>
        public static bool Refresh(Profile p, string today)
        {
            var m = p.missions;
            if (m.day == today && m.list.Count == Count) return false;
            m.day = today;
            m.bonusClaimed = false;
            m.rerolled = false;
            m.generation = 0;
            m.list = Generate(GameRng.HashSeed(p.playerId + "|missions|" + today), p.level, p.bestScore, p.gamesPlayed);
            return true;
        }

        /// <summary>Le duel en temps réel est disponible dans cette version (réglé par le jeu au lancement).</summary>
        public static bool DuelsAvailable;

        /// <summary>Fait avancer les missions de duel avec un duel terminé ; renvoie celles qui viennent d'être réussies.</summary>
        public static List<Mission> ApplyDuel(MissionState s, bool won)
        {
            var done = new List<Mission>();
            foreach (var m in s.list)
            {
                if (m.done) continue;
                if (m.kind == MissionKind.PlayDuels) m.progress++;
                else if (m.kind == MissionKind.WinDuels && won) m.progress++;
                else continue;
                if (m.progress >= m.target) { m.progress = m.target; m.done = true; done.Add(m); }
            }
            return done;
        }

        /// <summary>3 missions de types différents, adaptées au niveau et au record du joueur.</summary>
        public static List<Mission> Generate(uint seed, int level, int best, int gamesPlayed, MissionKind? avoid = null, IList<Mission> keep = null)
        {
            var rng = new GameRng(seed);
            var list = new List<Mission>();
            if (keep != null) list.AddRange(keep);
            var pool = new List<MissionKind>
            {
                MissionKind.PlayGames, MissionKind.ScoreInGame, MissionKind.TotalScore, MissionKind.Merges,
                MissionKind.MakeValue, MissionKind.Combo, MissionKind.PlayDaily,
            };
            // Les missions les plus dures n'arrivent qu'une fois le jeu bien compris.
            if (level >= 6) pool.Add(MissionKind.NearMiss);
            if (level >= 10 || best >= 4000) pool.Add(MissionKind.Balloon);
            // Missions d'exploit : tenir longtemps, ne rien laisser tomber, et (dès que le vent souffle) traverser des rafales.
            if (gamesPlayed >= 5 || level >= 3) { pool.Add(MissionKind.HoldSeconds); pool.Add(MissionKind.CleanScore); }
            if (best >= 6500) pool.Add(MissionKind.SurviveGusts);
            // Duel avec un ami : pour donner envie de jouer à deux (jamais pour un tout nouveau joueur).
            if (DuelsAvailable && (gamesPlayed >= 3 || level >= 2)) pool.Add(MissionKind.PlayDuels);
            if (DuelsAvailable && level >= 4) pool.Add(MissionKind.WinDuels);
            // Toujours une mission facile pour les débutants : « jouer N parties ».
            bool beginner = gamesPlayed < 10;
            int guard = 0;
            while (list.Count < Count && guard++ < 100)
            {
                var kind = beginner && list.Count == 0 && keep == null ? MissionKind.PlayGames : pool[(int)(rng.NextFloat() * pool.Count) % pool.Count];
                if (avoid.HasValue && kind == avoid.Value) continue;
                if (list.Exists(x => x.kind == kind)) continue;
                list.Add(Make(kind, rng, level, best));
            }
            return list;
        }

        static int Round(int v, int step) => Math.Max(step, (int)Math.Round(v / (double)step) * step);

        static Mission Make(MissionKind kind, GameRng rng, int level, int best)
        {
            float r = rng.NextFloat();
            int bestRef = Math.Max(800, best);
            var m = new Mission { kind = kind };
            switch (kind)
            {
                case MissionKind.PlayGames: m.target = 3 + (int)(r * 3); m.reward = 50; break;
                case MissionKind.ScoreInGame: m.target = Round((int)(bestRef * (0.55f + 0.3f * r)), 100); m.reward = 80; break;
                case MissionKind.TotalScore: m.target = Round((int)(bestRef * (2.2f + 1.5f * r)), 500); m.reward = 70; break;
                case MissionKind.Merges: m.target = Round(30 + (int)(r * 40) + level * 2, 5); m.reward = 60; break;
                case MissionKind.MakeValue: m.target = best >= 2500 || level >= 8 ? (r < 0.5f ? 64 : 32) : (r < 0.5f ? 32 : 16); m.reward = m.target >= 64 ? 90 : 60; break;
                case MissionKind.Combo: m.target = level >= 5 && r < 0.5f ? 4 : 3; m.reward = m.target >= 4 ? 100 : 60; break;
                case MissionKind.SurviveGusts: m.target = r < 0.6f ? 2 : 3; m.reward = m.target >= 3 ? 130 : 100; break;
                case MissionKind.HoldSeconds: m.target = level >= 8 ? (r < 0.5f ? 150 : 180) : (r < 0.5f ? 90 : 120); m.reward = m.target >= 150 ? 90 : 70; break;
                case MissionKind.CleanScore: m.target = Round((int)(bestRef * (0.3f + 0.15f * r)), 100); m.reward = 90; break;
                case MissionKind.PlayDaily: m.target = 1; m.reward = 60; break;
                case MissionKind.PlayDuels: m.target = r < 0.5f ? 1 : 2; m.reward = m.target >= 2 ? 90 : 60; break;
                case MissionKind.WinDuels: m.target = r < 0.6f ? 1 : 2; m.reward = m.target >= 2 ? 140 : 90; break;
                case MissionKind.NoHeartLost: m.target = Round((int)(bestRef * (0.3f + 0.2f * r)), 100); m.reward = 80; break;
                case MissionKind.NearMiss: m.target = 90; m.reward = 90; break;
                case MissionKind.Balloon: m.target = 1; m.reward = 150; break;
            }
            return m;
        }

        /// <summary>Fait avancer les missions avec une partie terminée ; renvoie celles qui viennent d'être réussies.</summary>
        public static List<Mission> Apply(MissionState s, RunStats r)
        {
            var done = new List<Mission>();
            foreach (var m in s.list)
            {
                if (m.done) continue;
                Step(m, r);
                if (m.progress >= m.target) { m.progress = m.target; m.done = true; done.Add(m); }
            }
            return done;
        }

        /// <summary>Fait avancer une mission avec une partie terminée (sans la marquer réussie).</summary>
        public static void Step(Mission m, RunStats r)
        {
                switch (m.kind)
                {
                    case MissionKind.PlayGames: m.progress++; break;
                    case MissionKind.ScoreInGame: m.progress = Math.Max(m.progress, r.Score); break;
                    case MissionKind.TotalScore: m.progress += r.Score; break;
                    case MissionKind.Merges: m.progress += r.Merges; break;
                    case MissionKind.MakeValue: if (r.MaxValue >= m.target) m.progress = m.target; break;
                    case MissionKind.Combo: m.progress = Math.Max(m.progress, r.BestChain); break;
                    case MissionKind.PlayDaily: if (r.Daily) m.progress = 1; break;
                    case MissionKind.NoHeartLost: m.progress = Math.Max(m.progress, r.Score); break; // ancienne mission (sauvegardes)
                    case MissionKind.NearMiss: m.progress = Math.Max(m.progress, (int)Math.Round(Math.Min(r.BestSave, 1f) * 100)); break;
                    case MissionKind.Balloon: m.progress += r.Balloons; break;
                    case MissionKind.SurviveGusts: m.progress = Math.Max(m.progress, r.Gusts); break;
                    case MissionKind.HoldSeconds: m.progress = Math.Max(m.progress, (int)r.Seconds); break;
                    case MissionKind.CleanScore: if (r.LostWeights == 0) m.progress = Math.Max(m.progress, r.Score); break;
                }
        }

        public static bool AllDone(MissionState s) => s.list.Count > 0 && s.list.TrueForAll(m => m.done);

        /// <summary>Remplace une mission pas encore faite (une fois par jour, contre une vidéo).</summary>
        public static bool Reroll(Profile p, int index)
        {
            var s = p.missions;
            if (s.rerolled || index < 0 || index >= s.list.Count || s.list[index].done) return false;
            var old = s.list[index];
            var others = new List<Mission>(s.list);
            others.RemoveAt(index);
            s.generation++;
            var fresh = Generate(GameRng.HashSeed(p.playerId + "|reroll|" + s.day + "|" + s.generation), p.level, p.bestScore, 99, old.kind, others);
            var added = fresh.Find(m => !others.Contains(m));
            if (added == null) return false;
            s.list[index] = added;
            s.rerolled = true;
            return true;
        }
    }

    /// <summary>Applique une partie terminée à l'économie : pièces, XP, niveaux, missions, têtes de niveau.</summary>
    public static class EconomyRun
    {
        public static EconomyGain Apply(EconomyTuning t, Profile p, RunStats r, string localDay)
        {
            var g = new EconomyGain { LevelBefore = p.level, XpFractionBefore = Levels.Fraction(t, p) };
            Missions.Refresh(p, localDay);
            g.Special = r.Special;
            g.Coins = Wallet.CoinsForScore(t, r.Score) * SpecialRun.CoinsMultiplier(r.Special);
            Wallet.Add(p, g.Coins);
            if (r.Goal > 0 && r.Score >= r.Goal)
            {
                g.GoalReached = true;
                g.GoalCoins = RunGoal.Reward;
                Wallet.Add(p, g.GoalCoins);
            }

            // Défi du jour : son objectif, réussi une fois par jour (pièces en plus).
            if (r.Daily && !string.IsNullOrEmpty(r.DayKey) && p.daily.objectiveDate != r.DayKey)
            {
                var obj = DailyChallenge.For(r.DayKey);
                if (DailyChallenge.Met(obj, r))
                {
                    p.daily.objectiveDate = r.DayKey;
                    g.DailyObjective = obj;
                    g.DailyCoins = obj.Reward;
                    Wallet.Add(p, g.DailyCoins);
                }
            }

            g.MissionsDone = Missions.Apply(p.missions, r);
            foreach (var m in g.MissionsDone) g.MissionCoins += m.reward;
            Wallet.Add(p, g.MissionCoins);
            if (!p.missions.bonusClaimed && Missions.AllDone(p.missions))
            {
                p.missions.bonusClaimed = true;
                g.MissionsBonus = true;
                g.BonusCoins = t.missionsBonus;
                Wallet.Add(p, g.BonusCoins);
            }

            g.Xp = (Levels.GameXp(t, r.Score) + g.MissionsDone.Count * t.missionXp) * SpecialRun.XpMultiplier(r.Special);
            g.LevelsGained = Levels.AddXp(t, p, g.Xp, out g.LevelCoins);
            p.coinsEarned += g.LevelCoins;
            g.LevelAfter = p.level;
            g.XpFractionAfter = Levels.Fraction(t, p);
            g.LevelHeads = UnlockLevelHeads(p);
            g.Chest = Bascule.Core.Chest.OnGameFinished(p, r.Seed, SpecialRun.ChestSteps(r.Special));
            g.ChestProgress = p.chestGames;
            return g;
        }

        public static List<HeadDef> UnlockLevelHeads(Profile p)
        {
            var list = new List<HeadDef>();
            foreach (var h in HeadCatalog.All)
                if (h.Unlock == UnlockKind.Level && p.level >= h.UnlockValue && !p.unlockedHeads.Contains(h.Id))
                {
                    p.unlockedHeads.Add(h.Id);
                    list.Add(h);
                }
            return list;
        }

        /// <summary>Achète une tête de la boutique avec des pièces.</summary>
        public static bool BuyHead(Profile p, HeadDef h)
        {
            if (h.Unlock != UnlockKind.Shop || p.unlockedHeads.Contains(h.Id)) return false;
            if (!Wallet.Spend(p, h.UnlockValue)) return false;
            p.unlockedHeads.Add(h.Id);
            return true;
        }
    }

    public static class AdPolicy
    {
        static void Roll(AdState s, string today)
        {
            if (s.day == today) return;
            s.day = today;
            s.interstitialsToday = 0;
            s.rewardedToday = 0;
            s.freeCoinsToday = 0;
        }

        /// <summary>À appeler à chaque fin de partie (avant de décider d'un interstitiel).</summary>
        public static void OnRunEnded(AdState s) => s.gamesSinceInterstitial++;

        /// <summary>
        /// Faut-il montrer une pub imposée maintenant (au moment où le joueur quitte l'écran de fin) ?
        /// celebration = record battu ou tête gagnée : on laisse le joueur savourer.
        /// </summary>
        public static bool ShouldShowInterstitial(AdTuning t, AdState s, Profile p, bool celebration, float runSeconds, double now, string today)
        {
            Roll(s, today);
            if (p.adsRemoved) return false;
            if (p.gamesPlayed < t.firstInterstitialAfterGames) return false;
            if (s.gamesSinceInterstitial < t.interstitialEveryGames) return false;
            if (now - s.lastInterstitial < t.minSecondsBetweenInterstitials) return false;
            if (now - s.lastRewarded < t.minSecondsAfterRewarded) return false;
            if (s.interstitialsToday >= t.maxInterstitialsPerDay) return false;
            if (celebration) return false;
            if (runSeconds < t.minRunSecondsForInterstitial) return false;
            return true;
        }

        public static void OnInterstitialShown(AdState s, double now, string today)
        {
            Roll(s, today);
            s.gamesSinceInterstitial = 0;
            s.lastInterstitial = now;
            s.interstitialsToday++;
            s.interstitialsTotal++;
        }

        public static bool CanWatchRewarded(AdTuning t, AdState s, string today)
        {
            Roll(s, today);
            return s.rewardedToday < t.maxRewardedPerDay;
        }

        public static void OnRewardedWatched(AdState s, double now, string today)
        {
            Roll(s, today);
            s.lastRewarded = now;
            s.rewardedToday++;
            s.rewardedTotal++;
        }

        public static int FreeCoinsLeft(AdTuning t, AdState s, string today)
        {
            Roll(s, today);
            return Math.Max(0, t.freeCoinsPerDay - s.freeCoinsToday);
        }

        /// <summary>Faut-il suggérer (une seule fois) l'achat « sans pubs » ?</summary>
        public static bool ShouldHintRemoveAds(AdTuning t, AdState s, Profile p) =>
            !p.adsRemoved && !s.removeAdsHintShown && s.interstitialsTotal >= t.removeAdsHintAfter;
    }

    public static class Offers
    {
        /// <summary>Lance l'offre de départ si c'est le moment ; vrai si elle vient de démarrer (afficher la fenêtre).</summary>
        public static bool CheckStarter(AdTuning t, Profile p, double now)
        {
            if (p.offers.starterEnds > 0 || Owns(p, Shop.Starter)) return false;
            if (p.gamesPlayed < t.starterOfferAfterGames) return false;
            p.offers.starterEnds = now + t.starterOfferHours * 3600.0;
            return true;
        }

        public static bool StarterActive(Profile p, double now) => !Owns(p, Shop.Starter) && p.offers.starterEnds > now;

        public static double StarterSecondsLeft(Profile p, double now) => Math.Max(0, p.offers.starterEnds - now);

        public static bool Owns(Profile p, string product) => p.ownedProducts.Contains(product);
    }

    // =====================================================================================================
    // Catalogue des achats
    // =====================================================================================================

    public enum ProductKind { Consumable, NonConsumable }

    public sealed class ProductDef
    {
        public string Id;
        public ProductKind Kind;
        public string TitleKey, DescKey;
        /// <summary>Prix affiché tant que le store n'a pas répondu (le vrai prix vient du store, dans la devise du joueur).</summary>
        public string DefaultPrice;
        public int Coins;
        public bool RemovesAds;
        /// <summary>Têtes « Premium » liées à ce produit.</summary>
        public string HeadsProduct;
        /// <summary>Badge « -60 % », « +20 % »… (clé de texte).</summary>
        public string BadgeKey;
    }

    /// <summary>
    /// Achats : uniquement du confort ou du cosmétique, jamais d'avantage en partie (cahier des charges, section 9).
    /// Les pièces n'achètent que des têtes.
    /// </summary>
    public static class Shop
    {
        public const string RemoveAds = "bascule.removeads";
        public const string Starter = "bascule.starter";
        public const string CoinsSmall = "bascule.coins.small";
        public const string CoinsMedium = "bascule.coins.medium";
        public const string CoinsLarge = "bascule.coins.large";
        public const string Legends = "bascule.legends";

        public static readonly IReadOnlyList<ProductDef> Products = new List<ProductDef>
        {
            new ProductDef { Id = Starter, Kind = ProductKind.NonConsumable, TitleKey = "p_starter", DescKey = "p_starter_desc", DefaultPrice = "3,99 €", Coins = 2000, RemovesAds = true, HeadsProduct = Starter, BadgeKey = "badge_starter" },
            new ProductDef { Id = RemoveAds, Kind = ProductKind.NonConsumable, TitleKey = "p_removeads", DescKey = "p_removeads_desc", DefaultPrice = "2,99 €", RemovesAds = true },
            new ProductDef { Id = CoinsSmall, Kind = ProductKind.Consumable, TitleKey = "p_coins_s", DescKey = "p_coins_desc", DefaultPrice = "0,99 €", Coins = 600 },
            new ProductDef { Id = CoinsMedium, Kind = ProductKind.Consumable, TitleKey = "p_coins_m", DescKey = "p_coins_desc", DefaultPrice = "2,99 €", Coins = 2200, BadgeKey = "badge_plus20" },
            new ProductDef { Id = CoinsLarge, Kind = ProductKind.Consumable, TitleKey = "p_coins_l", DescKey = "p_coins_desc", DefaultPrice = "6,99 €", Coins = 6000, BadgeKey = "badge_plus40" },
            new ProductDef { Id = Legends, Kind = ProductKind.NonConsumable, TitleKey = "p_legends", DescKey = "p_legends_desc", DefaultPrice = "4,99 €", HeadsProduct = Legends },
        };

        public static ProductDef Get(string id)
        {
            foreach (var p in Products) if (p.Id == id) return p;
            return null;
        }

        /// <summary>Donne au joueur ce qu'il a acheté. Les achats définitifs ne sont donnés qu'une fois (restauration comprise).</summary>
        public static bool Grant(Profile p, string productId)
        {
            var def = Get(productId);
            if (def == null) return false;
            if (def.Kind == ProductKind.NonConsumable)
            {
                if (p.ownedProducts.Contains(def.Id)) return false;
                p.ownedProducts.Add(def.Id);
            }
            if (def.RemovesAds) p.adsRemoved = true;
            if (def.Coins > 0) { p.coins += def.Coins; p.coinsBought += def.Coins; }
            if (!string.IsNullOrEmpty(def.HeadsProduct))
                foreach (var h in HeadCatalog.All)
                    if (h.Unlock == UnlockKind.Premium && h.Product == def.HeadsProduct && !p.unlockedHeads.Contains(h.Id))
                        p.unlockedHeads.Add(h.Id);
            return true;
        }
    }

    /// <summary>Dates « jour » (aaaa-mm-jj) partagées par le défi, le cadeau et les missions.</summary>
    public static class Dates
    {
        /// <summary>Nombre de jours de <paramref name="previous"/> à <paramref name="today"/> (−1 si une date est invalide).</summary>
        public static int DaysBetween(string previous, string today)
        {
            if (!TryParse(previous, out var a) || !TryParse(today, out var b)) return -1;
            return (int)Math.Round((b - a).TotalDays);
        }

        public static bool IsDayBefore(string previous, string today)
        {
            if (!TryParse(previous, out var a) || !TryParse(today, out var b)) return false;
            return (b - a).TotalDays == 1;
        }

        public static bool TryParse(string key, out DateTime date) =>
            DateTime.TryParseExact(key, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);

        public static double UnixSeconds(DateTime utc) => (utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
