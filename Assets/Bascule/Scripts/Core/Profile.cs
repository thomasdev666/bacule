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
