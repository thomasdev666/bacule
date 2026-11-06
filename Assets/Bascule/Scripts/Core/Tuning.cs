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
