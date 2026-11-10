using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bascule.Core
{
    public enum GameMode { Infinite, Daily, Duel }

    /// <summary>Sauvegarde locale du joueur (collection, record, réglages). Sérialisée en JSON.</summary>
    [Serializable]
    public class Profile
    {
        public int version = 1;
        public string playerId = "";
        public string pseudo = "";
        public int bestScore;
        public int gamesPlayed;
        public int selectedHead;
        /// <summary>Tête au hasard : à chaque partie, Basile porte une autre tête de la collection.</summary>
        public bool randomHead;
        public List<int> unlockedHeads = new List<int> { 0 };
        /// <summary>Têtes déjà vues dans la collection (les autres têtes débloquées font la pastille « nouveau »).</summary>
        public List<int> seenHeads = new List<int> { 0 };
        public bool seenHeadsInit;
        /// <summary>Planches achetées (la 0, en bois, est toujours là) et planche choisie : apparence seulement.</summary>
        public List<int> ownedPlanks = new List<int> { 0 };
        public int selectedPlank;
        /// <summary>Protège-séries en réserve (cadeau du jour) et machines à surprises ouvertes.</summary>
        public int streakShields;
        public int surprisesOpened;
        /// <summary>Tickets de la machine à surprises (cadeau du jour) : une ouverture gratuite chacun.</summary>
        public int surpriseTickets;
        public List<int> personalTop = new List<int>();
        public List<PendingScore> pendingScores = new List<PendingScore>();
        public DailyState daily = new DailyState();
        public Settings settings = new Settings();
        public bool adsRemoved;
        public List<string> ownedProducts = new List<string>();
        /// <summary>Version de l'écran d'information confidentialité accepté (0 = jamais vu).</summary>
        public int consentVersion;

        // Progression et économie (Core/Economy.cs).
        public int coins;
        public int coinsEarned;
        public int coinsBought;
        public int level = 1;
        public int xp;
        public MissionState missions = new MissionState();
        public GiftState gift = new GiftState();
        public AdState ads = new AdState();
        public OfferState offers = new OfferState();
        /// <summary>Tutoriel de la première partie terminé.</summary>
        public bool tutorialDone;
        /// <summary>L'explication du 128 (« il s'envole : gros bonus… ») a déjà été montrée.</summary>
        public bool balloonTipShown;
        /// <summary>Première rafale : on explique quoi faire (une seule fois).</summary>
        public bool windTipShown;
        /// <summary>Code ami court publié en ligne (vide tant qu'il ne l'est pas : on partage alors le code long).</summary>
        public string friendCode = "";
        /// <summary>Duels en temps réel joués et gagnés (tête « Challenger » à 15 victoires).</summary>
        public int duelsPlayed;
        public int duelWins;
        /// <summary>La demande d'autorisation des notifications a déjà été faite.</summary>
        public bool notificationsAsked;
        /// <summary>La demande de note sur le store a déjà été faite (une seule fois).</summary>
        public bool ratingAsked;
        /// <summary>Amis ajoutés par code (identifiants de joueur en ligne).</summary>
        public List<string> friends = new List<string>();
        /// <summary>Joueurs bloqués ou signalés : leurs pseudos ne s'affichent plus dans le classement.</summary>
        public List<string> blocked = new List<string>();
        /// <summary>Parties jouées depuis le dernier coffre, et coffres ouverts.</summary>
        public int chestGames;
        public int chestsOpened;
        /// <summary>Statistiques cumulées (écran « Mes stats » et analyse).</summary>
        public long totalScore;
        public int totalMerges;
        public int totalBalloons;
        public float totalSeconds;
        public string firstDay = "";
        public int daysPlayed;
        public string lastDay = "";
        /// <summary>Saison de la sauvegarde (Seasons.Current) : une ancienne saison repart à zéro au lancement.</summary>
        public int season;
        /// <summary>L'écran de bienvenue (lancement du jeu) reste à montrer à l'accueil.</summary>
        public bool welcomePending;

        public void Sanitize()
        {
            if (unlockedHeads == null) unlockedHeads = new List<int>();
            if (ownedPlanks == null) ownedPlanks = new List<int>();
            if (!ownedPlanks.Contains(0)) ownedPlanks.Add(0);
            if (!ownedPlanks.Contains(selectedPlank)) selectedPlank = 0;
            streakShields = Math.Max(0, Math.Min(StreakShield.Max, streakShields));
            surprisesOpened = Math.Max(0, surprisesOpened);
            surpriseTickets = Math.Max(0, Math.Min(9, surpriseTickets));
            if (!unlockedHeads.Contains(0)) unlockedHeads.Insert(0, 0);
            if (seenHeads == null) seenHeads = new List<int>();
            // Mise à jour d'une ancienne sauvegarde : toutes les têtes déjà gagnées comptent comme vues (pas de pastille géante).
            if (!seenHeadsInit) { seenHeads = new List<int>(unlockedHeads); seenHeadsInit = true; }
            if (personalTop == null) personalTop = new List<int>();
            if (pendingScores == null) pendingScores = new List<PendingScore>();
            if (daily == null) daily = new DailyState();
            if (settings == null) settings = new Settings();
            if (ownedProducts == null) ownedProducts = new List<string>();
            if (!unlockedHeads.Contains(selectedHead)) selectedHead = 0;
            if (string.IsNullOrEmpty(playerId)) playerId = Guid.NewGuid().ToString("N");
            settings.textScale = settings.textScale < 1f ? 1f : (settings.textScale > 1.6f ? 1.6f : settings.textScale);
            if (friends == null) friends = new List<string>();
            if (blocked == null) blocked = new List<string>();
            if (missions == null) missions = new MissionState();
            if (missions.list == null) missions.list = new List<Mission>();
            if (gift == null) gift = new GiftState();
            if (ads == null) ads = new AdState();
            if (offers == null) offers = new OfferState();
            if (level < 1) level = 1;
            if (xp < 0) xp = 0;
            if (coins < 0) coins = 0;
            // Anciennes sauvegardes : les achats définitifs déjà faits gardent leurs effets.
            if (ownedProducts.Contains(Shop.RemoveAds) || ownedProducts.Contains(Shop.Starter)) adsRemoved = true;
        }
    }

    /// <summary>
    /// Saisons : pour le lancement officiel (version « mondes et tenues », bien plus exigeante), tout le monde repart à
    /// zéro (record, têtes, pièces, niveau, planches, missions, séries, statistiques) et les classements en ligne
    /// changent de nom (Boards). On garde seulement ce qui n'est pas de la progression : identité, pseudo, amis,
    /// joueurs bloqués, réglages, consentement, et les achats définitifs (sans pubs, packs de têtes), qu'Apple impose de
    /// ne jamais faire perdre. Pour une prochaine remise à zéro : augmenter Current (et les noms des classements).
    /// </summary>
    public static class Seasons
    {
        public const int Current = 2;

        /// <summary>Vrai si la sauvegarde date d'une saison précédente (elle doit repartir à zéro).</summary>
        public static bool IsOld(Profile p) => p != null && p.season < Current;

        /// <summary>Nouvelle sauvegarde de la saison en cours, qui garde de l'ancienne ce qui n'est pas de la progression.</summary>
        public static Profile Reset(Profile old)
        {
            var fresh = new Profile();
            fresh.playerId = old.playerId;
            fresh.pseudo = old.pseudo;
            fresh.friendCode = old.friendCode;
            fresh.friends = old.friends ?? new List<string>();
            fresh.blocked = old.blocked ?? new List<string>();
            fresh.settings = old.settings ?? new Settings();
            fresh.consentVersion = old.consentVersion;
            fresh.notificationsAsked = old.notificationsAsked;
            fresh.ratingAsked = old.ratingAsked;
            // Ce qui s'apprend une fois pour toutes : pas besoin de le réexpliquer.
            fresh.tutorialDone = old.tutorialDone;
            fresh.balloonTipShown = old.balloonTipShown;
            fresh.windTipShown = old.windTipShown;
            // Achats définitifs rendus (sans pubs, têtes premium) ; les pièces, elles, repartent toutes de zéro.
            foreach (var id in old.ownedProducts ?? new List<string>()) Shop.Grant(fresh, id);
            if (old.adsRemoved) fresh.adsRemoved = true;
            fresh.coins = 0;
            fresh.coinsBought = 0;
            fresh.season = Current;
            fresh.welcomePending = true;
            fresh.Sanitize();
            fresh.seenHeads = new List<int>(fresh.unlockedHeads);
            fresh.seenHeadsInit = true;
            return fresh;
        }
    }

    [Serializable]
    public class Settings
    {
        public bool sound = true;
        public bool music = true;
        public bool vibration = true;
        /// <summary>"fr", "en" ou vide (langue du téléphone).</summary>
        public string language = "";
        /// <summary>Taille du texte : 1, 1.25 ou 1.5.</summary>
        public float textScale = 1f;
        /// <summary>Prototype : 0 = suivre le fichier de réglages, 1 = naturel, -1 = inversé.</summary>
        public int tiltDirectionOverride;
        public bool reducedMotion;
        /// <summary>Rappels (notifications locales) : activés seulement si le joueur l'a accepté.</summary>
        public bool notifications;
        /// <summary>120 images/s sur les écrans qui le permettent (plus fluide, mais le téléphone chauffe plus).</summary>
        public bool highFrameRate;
        /// <summary>Mode vidéo : filigrane « BASCULE · défi XXXX » pendant la partie, conseils masqués (pour filmer son écran).</summary>
        public bool creatorMode;
    }

    [Serializable]
    public class DailyState
    {
        public string lastPlayedDate = "";
        public int streak;
        public int bestStreak;
        public string bestDate = "";
        public int bestScoreToday;
        /// <summary>Jour où l'objectif du défi du jour a été réussi.</summary>
        public string objectiveDate = "";
    }

    /// <summary>Score joué hors ligne, envoyé au classement plus tard.</summary>
    [Serializable]
    public class PendingScore
    {
        public string board;
        public int score;
        public int head;
        public string date;
    }

    /// <summary>Ce que la partie a rapporté, pour l'écran de fin.</summary>
    public sealed class RunRewards
    {
        public bool NewBest;
        public int PreviousBest;
        public List<HeadDef> NewHeads = new List<HeadDef>();
        public HeadDef NextHead;
        public int DailyStreak;
    }

    public static class Progress
    {
        /// <summary>Note un jour d'ouverture de l'appli (jours joués, premier jour).</summary>
        public static void TouchDay(Profile p, string localDay)
        {
            if (string.IsNullOrEmpty(p.firstDay)) p.firstDay = localDay;
            if (p.lastDay == localDay) return;
            p.lastDay = localDay;
            p.daysPlayed++;
        }

        public const int PersonalTopSize = 10;

        /// <summary>Met à jour le profil après une partie et renvoie les récompenses (record, nouvelles têtes).</summary>
        public static RunRewards ApplyRun(Profile p, GameMode mode, RunAchievements run, string todayKey)
        {
            var r = new RunRewards { PreviousBest = p.bestScore };
            p.gamesPlayed++;
            if (run.Score > p.bestScore) { p.bestScore = run.Score; r.NewBest = run.Score > 0; }

            p.personalTop.Add(run.Score);
            p.personalTop.Sort((a, b) => b.CompareTo(a));
            if (p.personalTop.Count > PersonalTopSize) p.personalTop.RemoveRange(PersonalTopSize, p.personalTop.Count - PersonalTopSize);

            if (mode == GameMode.Daily)
            {
                Daily.RegisterPlay(p.daily, todayKey);
                if (p.daily.bestDate != todayKey) { p.daily.bestDate = todayKey; p.daily.bestScoreToday = 0; }
                if (run.Score > p.daily.bestScoreToday) p.daily.bestScoreToday = run.Score;
            }
            run.DailyStreak = p.daily.streak;
            r.DailyStreak = p.daily.streak;

            foreach (var h in HeadCatalog.All)
            {
                if (p.unlockedHeads.Contains(h.Id)) continue;
                if (HeadCatalog.IsEarned(h, run)) { p.unlockedHeads.Add(h.Id); r.NewHeads.Add(h); }
            }
            r.NextHead = HeadCatalog.NextScoreHead(p.unlockedHeads);
            return r;
        }
    }

    /// <summary>
    /// Têtes gagnées que le joueur n'a pas encore regardées : une pastille rouge sur « Têtes » (et sur chaque onglet)
    /// qui ne disparaît qu'une fois qu'il les a vues dans la collection. Il découvre ainsi chaque tête gagnée.
    /// </summary>
    public static class HeadNews
    {
        public static bool IsNew(Profile p, int id) => p.unlockedHeads.Contains(id) && !p.seenHeads.Contains(id);

        public static int Count(Profile p)
        {
            int n = 0;
            foreach (var id in p.unlockedHeads) if (!p.seenHeads.Contains(id)) n++;
            return n;
        }

        public static int Count(Profile p, Func<HeadDef, bool> filter)
        {
            int n = 0;
            foreach (var h in HeadCatalog.All) if (filter(h) && IsNew(p, h.Id)) n++;
            return n;
        }

        /// <summary>Marque ces têtes comme vues ; renvoie vrai si quelque chose a changé.</summary>
        public static bool MarkSeen(Profile p, IEnumerable<int> ids)
        {
            bool changed = false;
            foreach (var id in ids)
                if (p.unlockedHeads.Contains(id) && !p.seenHeads.Contains(id)) { p.seenHeads.Add(id); changed = true; }
            return changed;
        }
    }

    /// <summary>
    /// Défi du jour : un exploit bien plus dur qu'une mission, le même pour tout le monde ce jour-là, à réussir
    /// en une seule partie (souvent deux conditions à la fois). 28 défis différents qui tournent dans un ordre fixe :
    /// aucun ne revient avant 4 semaines. 150 pièces, une fois par jour. Le vent reste à partir de 5 000 points.
    /// </summary>
    public static class DailyChallenge
    {
        public const int Reward = 150;

        /// <summary>Un défi : texte (clé Loc « dc_… » et ses deux nombres) et condition de réussite.</summary>
        public sealed class Def
        {
            public string Key;
            public int A, B;
            public int Reward = DailyChallenge.Reward;
            public Func<RunStats, bool> Met;
        }

        static Def D(string key, int a, int b, Func<RunStats, bool> met) => new Def { Key = key, A = a, B = b, Met = met };

        /// <summary>
        /// Les 28 défis, dans l'ordre où ils passent (types alternés : score, endurance, précision, combos, 128…).
        /// Ne pas réordonner sans raison : les joueurs verraient deux fois le même défi en peu de temps.
        /// </summary>
        public static readonly Def[] All =
        {
            D("combo", 4, 0, r => r.BestChain >= 4),
            D("clean_score", 3000, 0, r => r.Score >= 3000 && r.LostWeights == 0),
            D("hold", 180, 0, r => r.Seconds >= 180),
            D("score", 5000, 0, r => r.Score >= 5000),
            D("balloon", 1, 0, r => r.Balloons >= 1),
            D("merges", 60, 0, r => r.Merges >= 60),
            D("save", 95, 0, r => r.BestSave >= 0.95f),
            D("score_combo", 4000, 4, r => r.Score >= 4000 && r.BestChain >= 4),
            D("clean_value", 64, 0, r => r.MaxValue >= 64 && r.LostWeights == 0),
            D("drops", 150, 0, r => r.Drops >= 150),
            D("clean_hold", 150, 0, r => r.Seconds >= 150 && r.LostWeights == 0),
            D("combo", 5, 0, r => r.BestChain >= 5),
            D("gust", 1, 0, r => r.Gusts >= 1),
            D("score_save", 3000, 90, r => r.Score >= 3000 && r.BestSave >= 0.9f),
            D("merges", 80, 0, r => r.Merges >= 80),
            D("clean_combo", 4, 0, r => r.BestChain >= 4 && r.LostWeights == 0),
            D("hold", 240, 0, r => r.Seconds >= 240),
            D("score", 6000, 0, r => r.Score >= 6000),
            D("clean_balloon", 1, 0, r => r.Balloons >= 1 && r.LostWeights == 0),
            D("clean_merges", 50, 0, r => r.Merges >= 50 && r.LostWeights == 0),
            D("hold_combo", 120, 4, r => r.Seconds >= 120 && r.BestChain >= 4),
            D("balloon", 2, 0, r => r.Balloons >= 2),
            D("clean_score", 4500, 0, r => r.Score >= 4500 && r.LostWeights == 0),
            D("drops_clean", 100, 0, r => r.Drops >= 100 && r.LostWeights == 0),
            D("score_save", 4500, 90, r => r.Score >= 4500 && r.BestSave >= 0.9f),
            D("value_combo", 64, 4, r => r.MaxValue >= 64 && r.BestChain >= 4),
            D("gust", 2, 0, r => r.Gusts >= 2),
            D("clean_hold", 200, 0, r => r.Seconds >= 200 && r.LostWeights == 0),
        };

        static readonly DateTime Origin = new DateTime(2026, 1, 1);

        /// <summary>Numéro du jour (depuis le 1er janvier 2026), d'après la clé « aaaa-mm-jj ».</summary>
        public static int DayIndex(string dayKey)
        {
            if (!DateTime.TryParseExact(dayKey ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return 0;
            return (int)Math.Floor((d - Origin).TotalDays);
        }

        /// <summary>Défi du jour (identique pour tous les joueurs ce jour-là ; le même revient tous les 28 jours).</summary>
        public static Def For(string dayKey)
        {
            int n = All.Length, i = DayIndex(dayKey) % n;
            return All[i < 0 ? i + n : i];
        }

        public static bool Met(Def d, RunStats r) => d != null && d.Met(r);

        public static bool DoneToday(Profile p, string dayKey) => p.daily.objectiveDate == dayKey;
    }

    /// <summary>Défi du jour : graine commune à tous les joueurs pour la journée (UTC), série de jours consécutifs.</summary>
    public static class Daily
    {
        public static string Key(DateTime utcNow) => utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static uint Seed(string dayKey) => GameRng.HashSeed("bascule-daily-" + dayKey);

        public static void RegisterPlay(DailyState d, string todayKey)
        {
            if (d.lastPlayedDate == todayKey) return;
            d.streak = IsDayBefore(d.lastPlayedDate, todayKey) ? d.streak + 1 : 1;
            if (d.streak > d.bestStreak) d.bestStreak = d.streak;
            d.lastPlayedDate = todayKey;
        }

        /// <summary>Série affichée sur l'accueil : retombe à 0 si un jour a été manqué.</summary>
        public static int CurrentStreak(DailyState d, string todayKey)
        {
            if (d.lastPlayedDate == todayKey || IsDayBefore(d.lastPlayedDate, todayKey)) return d.streak;
            return 0;
        }

        public static bool PlayedToday(DailyState d, string todayKey) => d.lastPlayedDate == todayKey;

        static bool IsDayBefore(string previous, string today) => Dates.IsDayBefore(previous, today);
    }

    /// <summary>Photo d'une partie en cours, pour reprendre au même endroit après avoir quitté l'application.</summary>
    [Serializable]
    public class RunSnapshot
    {
        public int version = 1;
        public GameMode mode;
        public uint seed;
        public string dayKey = "";
        public uint rngState;
        public int dropIndex;
        public int current;
        public int next;
        public float plankAngle;
        public float plankVelocity;
        public float strainTime;
        public int score;
        public int bestChain;
        public int merges;
        public int balloons;
        public int maxValue;
        public float bestSave;
        public int saves;
        public bool revived;
        public float elapsed;
        public int lostWeights;
        /// <summary>Rafales déjà traversées dans cette partie.</summary>
        public int gusts;
        public int goal;
        public int special;
        public List<WeightState> weights = new List<WeightState>();
    }

    [Serializable]
    public class WeightState
    {
        public int value;
        public float x, y, rotation, vx, vy, angularVelocity;
        public bool landed;
    }
}
