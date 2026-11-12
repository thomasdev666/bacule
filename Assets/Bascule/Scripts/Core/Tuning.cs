using System;

namespace Bascule.Core
{
    /// <summary>
    /// Tous les réglages de gameplay. Les valeurs par défaut ci-dessous sont écrasées par
    /// Assets/Bascule/Resources/bascule_tuning.json : on règle le jeu dans ce fichier, sans recoder.
    /// Les noms de champs doivent rester identiques à ceux du JSON (JsonUtility).
    /// </summary>
    [Serializable]
    public class Tuning
    {
        public PlankTuning plank = new PlankTuning();
        public WeightTuning weights = new WeightTuning();
        public SpawnTuning spawn = new SpawnTuning();
        public ScoreTuning score = new ScoreTuning();
        public MascotTuning mascot = new MascotTuning();
        public EconomyTuning economy = new EconomyTuning();
        public AdTuning ads = new AdTuning();
        public WindTuning wind = new WindTuning();
        public DuelTuning duel = new DuelTuning();

        /// <summary>Corrige les valeurs absurdes (fichier mal édité) pour que le jeu reste jouable.</summary>
        public void Sanitize()
        {
            if (plank == null) plank = new PlankTuning();
            if (weights == null) weights = new WeightTuning();
            if (spawn == null) spawn = new SpawnTuning();
            if (score == null) score = new ScoreTuning();
            if (mascot == null) mascot = new MascotTuning();
            if (economy == null) economy = new EconomyTuning();
            if (ads == null) ads = new AdTuning();
            if (wind == null) wind = new WindTuning();
            if (duel == null) duel = new DuelTuning();
            duel.countdownSeconds = Clamp(duel.countdownSeconds, 0f, 10f);
            duel.sensitivityMax = Clamp(duel.sensitivityMax, 1f, 3f);
            duel.gustGapMin = Clamp(duel.gustGapMin, 5f, 300f);
            duel.gustGapMax = Clamp(duel.gustGapMax, duel.gustGapMin, 300f);
            duel.finalWindowSeconds = Clamp(duel.finalWindowSeconds, 0f, 10f);
            duel.snapshotsPerSecond = Math.Max(5, Math.Min(30, duel.snapshotsPerSecond));
            duel.timeoutSeconds = Clamp(duel.timeoutSeconds, 3f, 60f);
            duel.challengerWins = Math.Max(1, duel.challengerWins);
            wind.warningSeconds = Clamp(wind.warningSeconds, 0.5f, 5f);
            wind.blowSeconds = Clamp(wind.blowSeconds, 0.5f, 8f);
            wind.rampSeconds = Clamp(wind.rampSeconds, 0.05f, 3f);
            wind.minGap = Clamp(wind.minGap, 5f, 600f);
            wind.gapMin = Clamp(wind.gapMin, wind.minGap, 600f);
            wind.gapMax = Clamp(wind.gapMax, wind.gapMin, 900f);
            wind.stagnationWindow = Clamp(wind.stagnationWindow, 2f, 120f);
            wind.stagnationSpeedup = Clamp(wind.stagnationSpeedup, 1f, 5f);
            wind.strengthMin = Clamp(wind.strengthMin, 0f, 1f);
            wind.strengthStart = Clamp(wind.strengthStart, wind.strengthMin, 1f);
            wind.strengthMax = Clamp(wind.strengthMax, wind.strengthStart, 1f);
            wind.maxPeakRatio = Clamp(wind.maxPeakRatio, 0.2f, 1.2f);
            wind.maxDangerToStart = Clamp(wind.maxDangerToStart, 0f, 1f);
            plank.sensitivityPer1000 = Clamp(plank.sensitivityPer1000, 0f, 1f);
            plank.sensitivityMax = Clamp(plank.sensitivityMax, 1f, 3f);
            if (economy.dailyGift == null || economy.dailyGift.Length == 0) economy.dailyGift = new EconomyTuning().dailyGift;
            if (economy.dailyGiftItems == null || economy.dailyGiftItems.Length == 0) economy.dailyGiftItems = new EconomyTuning().dailyGiftItems;
            economy.scorePerCoin = Math.Max(1, economy.scorePerCoin);
            economy.scorePerXp = Math.Max(1, economy.scorePerXp);
            ads.interstitialEveryGames = Math.Max(1, ads.interstitialEveryGames);

            plank.tiltDirection = plank.tiltDirection < 0 ? -1 : 1;
            plank.maxAngle = Clamp(plank.maxAngle, 5f, 80f);
            plank.overshootAngle = Clamp(plank.overshootAngle, 0f, 30f);
            plank.frequency = Clamp(plank.frequency, 0.05f, 5f);
            plank.dampingRatio = Clamp(plank.dampingRatio, 0f, 3f);
            plank.loadSlowdown = Clamp(plank.loadSlowdown, 0f, 1f);
            plank.loseGraceSeconds = Clamp(plank.loseGraceSeconds, 0f, 5f);
            plank.minLength = Clamp(plank.minLength, 1f, 20f);
            plank.startLength = Clamp(plank.startLength, plank.minLength, 20f);
            plank.referenceHalfLength = Clamp(plank.referenceHalfLength, 0.5f, 20f);
            plank.balloonLiftSeconds = Clamp(plank.balloonLiftSeconds, 0f, 3f);
            plank.balloonGraceSeconds = Clamp(plank.balloonGraceSeconds, 0f, 6f);
            plank.balloonGraceDamping = Clamp(plank.balloonGraceDamping, 1f, 5f);
            plank.bigMergeSteadySeconds = Clamp(plank.bigMergeSteadySeconds, 0f, 4f);
            plank.bigMergeDamping = Clamp(plank.bigMergeDamping, 1f, 4f);
            plank.bigMergeStrainRate = Clamp(plank.bigMergeStrainRate, 0.2f, 1f);
            plank.torqueExponent = Clamp(plank.torqueExponent, 0.3f, 1.5f);

            weights.baseSize = Clamp(weights.baseSize, 0.2f, 3f);
            weights.maxSize = Clamp(weights.maxSize, weights.baseSize, 5f);

            if (spawn.tiers == null || spawn.tiers.Length == 0) spawn.tiers = SpawnTuning.DefaultTiers();
            foreach (var tier in spawn.tiers)
            {
                if (tier.values == null || tier.values.Length == 0) { tier.values = new[] { 1 }; tier.chances = new[] { 1f }; }
                if (tier.chances == null || tier.chances.Length != tier.values.Length)
                {
                    tier.chances = new float[tier.values.Length];
                    for (int i = 0; i < tier.chances.Length; i++) tier.chances[i] = 1f;
                }
                for (int i = 0; i < tier.values.Length; i++)
                    tier.values[i] = WeightCatalog.Normalize(tier.values[i]);
            }
            Array.Sort(spawn.tiers, (a, b) => a.fromDrop.CompareTo(b.fromDrop));
            score.comboWindow = Clamp(score.comboWindow, 0.05f, 10f);
            score.maxComboMultiplier = Clamp(score.maxComboMultiplier, 1f, 50f);
        }

        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }

    /// <summary>Rafales de vent à haut score (voir Core/Wind.cs).</summary>
    [Serializable]
    public class WindTuning
    {

        /// <summary>Score à partir duquel le vent peut souffler.</summary>
        public int startScore = 5000;
        /// <summary>Paliers qui déclenchent une rafale ; ensuite, un palier tous les milestoneEvery points.</summary>
        public int[] milestones = { 5000, 8000, 10000 };
        public int milestoneEvery = 2500;
        /// <summary>Jamais deux rafales à moins de minGap secondes.</summary>
        public float minGap = 25f;
        /// <summary>Entre deux paliers : une rafale au hasard toutes les gapMin à gapMax secondes.</summary>
        public float gapMin = 45f;
        public float gapMax = 75f;
        /// <summary>Stagnation : moins de stagnationPoints gagnés en stagnationWindow secondes, la prochaine rafale arrive stagnationSpeedup fois plus vite.</summary>
        public float stagnationWindow = 20f;
        public int stagnationPoints = 200;
        public float stagnationSpeedup = 1.8f;
        /// <summary>Annonce (s), souffle (s) et montée/retombée du souffle (s).</summary>
        public float warningSeconds = 1.8f;
        public float blowSeconds = 3f;
        public float rampSeconds = 0.7f;
        /// <summary>Force (part du couple de défaite) : première rafale, gain par rafale, maximum, minimum.</summary>
        public float strengthStart = 0.22f;
        public float strengthStep = 0.05f;
        public float strengthMax = 0.52f;
        public float strengthMin = 0.12f;
        /// <summary>Le vent seul n'emmène jamais l'équilibre au-delà de cette part de l'angle max.</summary>
        public float maxPeakRatio = 0.85f;
        /// <summary>
        /// Premières rafales « douces » (gentleGusts) : elles servent à découvrir le vent, jamais à faire perdre. Le vent
        /// seul ne dépasse pas gentlePeakRatio de l'angle max, et il s'efface quand la planche penche : plein de
        /// gentleFadeFrom de danger, plus rien à gentleFadeTo.
        /// </summary>
        public int gentleGusts = 3;
        public float gentlePeakRatio = 0.55f;
        public float gentleFadeFrom = 0.5f;
        public float gentleFadeTo = 0.8f;
        /// <summary>Pas de nouvelle rafale si la planche est déjà au-delà de ce danger.</summary>
        public float maxDangerToStart = 0.5f;
    }

    [Serializable]
    public class PlankTuning
    {
        /// <summary>1 : la planche penche vers le côté le plus lourd (naturel). -1 : vers le plus léger.</summary>
        public int tiltDirection = 1;
        /// <summary>Inclinaison d'équilibre (degrés) par unité de couple. Couple = valeur × position (−1 bout gauche, +1 bout droit).</summary>
        public float degreesPerTorque = 2.6f;
        /// <summary>Petits rebords en bois aux bouts de la planche (hauteur, largeur ; unités monde). 0 = pas de rebord.</summary>
        public float endLipHeight = 0.22f;
        public float endLipWidth = 0.12f;
        /// <summary>
        /// Poids effectif d'un poids pour le couple = valeur^torqueExponent. 1 = réaliste (un 64 pèse 64 fois un 1) ;
        /// moins de 1 = les gros poids écrasent moins le reste, et fusionner allège un peu la planche.
        /// 0,85 a été retenu après simulation (Tools/balance_sim.py) : avec 1, un seul 64 mal placé fait perdre à coup sûr.
        /// </summary>
        public float torqueExponent = 0.85f;
        /// <summary>Fréquence d'oscillation à vide (Hz). Plus haut = planche plus nerveuse.</summary>
        public float frequency = 0.55f;
        /// <summary>Ralentissement par unité de poids portée : la mascotte bouge lentement quand elle porte lourd.</summary>
        public float loadSlowdown = 0.012f;
        /// <summary>Amortissement : 0 = oscille sans fin, 1 = aucun rebond. 0,3–0,5 donne quelques oscillations lisibles.</summary>
        public float dampingRatio = 0.42f;
        /// <summary>Inclinaison maximale (degrés). Au-delà, la mascotte force puis lâche.</summary>
        public float maxAngle = 30f;
        /// <summary>Temps toléré au-delà de maxAngle avant la défaite (permet les sauvetages de justesse).</summary>
        public float loseGraceSeconds = 0.55f;
        /// <summary>Dépassement possible au-delà de maxAngle (degrés), pour l'effet visuel.</summary>
        public float overshootAngle = 6f;
        public float startLength = 6.6f;
        public float minLength = 4.2f;
        /// <summary>Raccourcissement de la planche (unités monde) par tranche de 1000 points.</summary>
        public float shrinkPer1000Points = 0.14f;
        /// <summary>
        /// Le raccourcissement s'arrête à ce score (1.2). Le bras de levier est fixe (referenceHalfLength) : une planche
        /// plus courte donne moins de couple, donc à haut score elle rendait le jeu PLUS facile. Avant ce score, rien ne change.
        /// </summary>
        public int shrinkUntilScore = 4000;
        /// <summary>
        /// Sensibilité à haut score : à partir de sensitivityFromScore, la planche penche un peu plus pour le même couple
        /// (+sensitivityPer1000 par tranche de 1000 points, jusqu'à sensitivityMax). La tension monte avec le score.
        /// </summary>
        public int sensitivityFromScore = 4000;
        public float sensitivityPer1000 = 0.05f;
        public float sensitivityMax = 1.35f;
        public float thickness = 0.3f;
        /// <summary>Demi-longueur qui sert d'unité de bras de levier. Fixe : une planche plus courte donne moins de levier mais moins de place.</summary>
        public float referenceHalfLength = 3.3f;
        /// <summary>Durée pendant laquelle le poids du 128 s'efface en s'envolant (0 = instantané). Assez longue pour qu'on puisse réagir.</summary>
        public float balloonLiftSeconds = 1.6f;
        /// <summary>
        /// Souffle après l'envol d'un 128 : pendant ce temps, la planche est plus amortie et la défaite impossible.
        /// Le 128 est le plus bel exploit du jeu : il ne doit pas faire perdre la partie dans la seconde.
        /// </summary>
        public float balloonGraceSeconds = 2.5f;
        /// <summary>Amortissement multiplié par ce facteur pendant le souffle (la planche ralentit au lieu de claquer).</summary>
        public float balloonGraceDamping = 2.2f;
        /// <summary>
        /// Gros poids créé d'un coup (valeur au moins bigMergeValue, 64 par défaut) : pendant bigMergeSteadySeconds,
        /// amortissement multiplié par bigMergeDamping et tension qui monte bigMergeStrainRate fois moins vite.
        /// Un peu plus facile, mais on peut toujours perdre.
        /// </summary>
        public int bigMergeValue = 64;
        public float bigMergeSteadySeconds = 1.5f;
        public float bigMergeDamping = 1.6f;
        public float bigMergeStrainRate = 0.6f;
    }

    [Serializable]
    public class WeightTuning
    {
        /// <summary>Côté (unités monde) du poids 1.</summary>
        public float baseSize = 0.58f;
        /// <summary>Taille = baseSize × valeur^exposant, plafonnée à maxSize.</summary>
        public float sizeExponent = 0.27f;
        public float maxSize = 1.75f;
        public float friction = 0.45f;
        public float bounciness = 0.04f;
        public float gravityScale = 1.7f;
        /// <summary>Masse physique = valeur^exposant (le couple, lui, utilise la vraie valeur).</summary>
        public float massExponent = 0.5f;
    }

    [Serializable]
    public class SpawnTuning
    {
        /// <summary>Délai entre le lâcher et l'apparition du poids suivant (s).</summary>
        public float dropCooldown = 0.45f;
        public SpawnTier[] tiers = DefaultTiers();

        public static SpawnTier[] DefaultTiers() => new[]
        {
            new SpawnTier { fromDrop = 0,  values = new[] { 1, 2, 4 },        chances = new[] { 50f, 35f, 15f } },
            new SpawnTier { fromDrop = 15, values = new[] { 1, 2, 4, 8 },     chances = new[] { 35f, 35f, 22f, 8f } },
            new SpawnTier { fromDrop = 40, values = new[] { 1, 2, 4, 8, 16 }, chances = new[] { 25f, 30f, 28f, 14f, 3f } },
            new SpawnTier { fromDrop = 90, values = new[] { 1, 2, 4, 8, 16 }, chances = new[] { 18f, 26f, 30f, 19f, 7f } },
        };
    }

    /// <summary>Palier de difficulté : à partir du n-ième poids lâché, les poids sont tirés avec ces chances.</summary>
    [Serializable]
    public class SpawnTier
    {
        public int fromDrop;
        public int[] values;
        public float[] chances;
    }

    [Serializable]
    public class ScoreTuning
    {
        /// <summary>Points d'une fusion = valeur créée × pointsPerValue × multiplicateur de combo.</summary>
        public int pointsPerValue = 3;
        /// <summary>Deux fusions séparées de moins de comboWindow secondes (après un même lâcher) forment une chaîne.</summary>
        public float comboWindow = 1.2f;
        /// <summary>Le multiplicateur gagne comboStep par fusion en chaîne.</summary>
        public float comboStep = 0.5f;
        public float maxComboMultiplier = 4f;
        /// <summary>Bonus quand le 128 s'envole (multiplié par le combo en cours).</summary>
        public int balloonBonus = 600;
        /// <summary>Points perdus par unité de valeur quand un poids tombe de la planche (un 8 qui tombe : 8 × 4 = −32).</summary>
        public float lostWeightPenalty = 4f;
    }

    [Serializable]
    public class MascotTuning
    {
        /// <summary>Seuils (part de l'inclinaison max) des états de la mascotte.</summary>
        public float tenseRatio = 0.3f;
        public float strainedRatio = 0.55f;
        public float lastEffortRatio = 0.82f;
        public string mascotName = "Basile";
    }
}
