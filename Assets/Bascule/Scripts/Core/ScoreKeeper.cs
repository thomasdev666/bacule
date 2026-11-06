using System;

namespace Bascule.Core
{
    /// <summary>
    /// Score (section 6) : points à chaque fusion, multipliés par la taille du poids créé ;
    /// combo quand plusieurs fusions s'enchaînent après un même lâcher ; bonus montgolfière (128).
    /// </summary>
    public sealed class ScoreKeeper
    {
        readonly ScoreTuning _t;

        public int Score { get; private set; }
        /// <summary>Longueur de la chaîne de fusions en cours (1 = fusion simple).</summary>
        public int Chain { get; private set; }
        public int BestChain { get; private set; }
        public int Merges { get; private set; }
        public int Balloons { get; private set; }
        public int MaxValue { get; private set; }
        public int LostWeights { get; private set; }
        float _lastMergeTime = float.NegativeInfinity;

        public ScoreKeeper(ScoreTuning tuning) { _t = tuning; }

        /// <summary>Un poids est tombé de la planche : pénalité de points et combo cassé. Renvoie les points perdus.</summary>
        public int OnWeightLost(int value)
        {
            LostWeights++;
            Chain = 0;
            int penalty = Math.Min(Score, (int)Math.Round(value * _t.lostWeightPenalty));
            Score -= penalty;
            return penalty;
        }

        public float Multiplier => Math.Min(_t.maxComboMultiplier, 1f + _t.comboStep * Math.Max(0, Chain - 1));

        /// <summary>Un nouveau poids est lâché : la chaîne repart de zéro.</summary>
        public void OnDrop() { Chain = 0; }

        /// <summary>Une fusion vient de créer un poids de valeur <paramref name="createdValue"/>. Renvoie les points gagnés.</summary>
        public int OnMerge(int createdValue, float time)
        {
            if (Chain > 0 && time - _lastMergeTime <= _t.comboWindow) Chain++;
            else Chain = 1;
            _lastMergeTime = time;
            if (Chain > BestChain) BestChain = Chain;
            Merges++;
            if (createdValue > MaxValue) MaxValue = createdValue;
            int points = (int)Math.Round(createdValue * _t.pointsPerValue * Multiplier);
            Score += points;
            return points;
        }

        /// <summary>Le 128 s'envole. Renvoie le bonus.</summary>
        public int OnBalloon()
        {
            Balloons++;
            int bonus = (int)Math.Round(_t.balloonBonus * Multiplier);
            Score += bonus;
            return bonus;
        }

        public void Restore(int score, int bestChain, int merges, int balloons, int maxValue, int lostWeights = 0)
        {
            Score = score; BestChain = bestChain; Merges = merges; Balloons = balloons; MaxValue = maxValue;
            LostWeights = lostWeights;
            Chain = 0; _lastMergeTime = float.NegativeInfinity;
        }
    }

    /// <summary>
    /// Repère le « sauvetage de justesse » : la planche est passée très près de la défaite (ou au-delà,
    /// pendant le délai de grâce) puis le joueur l'a ramenée. On garde le plus serré, pour la carte de score.
    /// </summary>
    public sealed class NearMissTracker
    {
        readonly float _armRatio;
        readonly float _safeRatio;
        float _peak;
        bool _armed;

        /// <summary>Plus haut danger dont le joueur s'est sorti (1 = pile à la limite, &gt;1 = au-delà).</summary>
        public float BestSave { get; private set; }
        public int Saves { get; private set; }

        /// <summary>Déclenché quand un sauvetage est validé (valeur = danger atteint).</summary>
        public event Action<float> SaveCompleted;

        public NearMissTracker(float armRatio = 0.85f, float safeRatio = 0.5f)
        {
            _armRatio = armRatio;
            _safeRatio = safeRatio;
        }

        public void Update(float danger, bool lost)
        {
            if (lost) { _armed = false; _peak = 0; return; }
            if (danger >= _armRatio)
            {
                _armed = true;
                if (danger > _peak) _peak = danger;
            }
            else if (_armed && danger <= _safeRatio)
            {
                _armed = false;
                Saves++;
                if (_peak > BestSave) BestSave = _peak;
                SaveCompleted?.Invoke(_peak);
                _peak = 0;
            }
        }

        public void Restore(float bestSave, int saves) { BestSave = bestSave; Saves = saves; _armed = false; _peak = 0; }

        /// <summary>Pourcentage affiché : « sauvé à 97 % de la chute ». Plafonné à 99 %.</summary>
        public static int DisplayPercent(float danger) => (int)Math.Min(99, Math.Round(Math.Min(danger, 1f) * 100));
    }

    public static class Difficulty
    {
        /// <summary>La planche raccourcit petit à petit avec le score.</summary>
        public static float PlankLength(int score, PlankTuning t)
        {
            float len = t.startLength - t.shrinkPer1000Points * (score / 1000f);
            return len < t.minLength ? t.minLength : len;
        }

        /// <summary>Paliers de décor : matin, midi (300), doré (800), couchant (2 000), nuit (4 000). Des étapes plus visibles.</summary>
        public static readonly int[] ThemeScores = { 0, 300, 800, 2000, 4000 };
        /// <summary>Après la nuit, une nouvelle journée recommence tous les 3 000 points.</summary>
        public const int ThemeCycle = 3000;

        /// <summary>
        /// Décor selon le score : il change dès 300 points, puis s'espace.
        /// Après la nuit, on repart du matin toutes les <see cref="ThemeCycle"/> points.
        /// </summary>
        public static int BackgroundIndex(int score, int themeCount)
        {
            if (themeCount <= 0) return 0;
            int last = ThemeScores[ThemeScores.Length - 1];
            if (score < last + ThemeCycle)
            {
                int i = 0;
                for (int k = 0; k < ThemeScores.Length; k++) if (score >= ThemeScores[k]) i = k;
                return i % themeCount;
            }
            int day = (score - last) / ThemeCycle; // 1 = premier matin après la nuit
            return (day - 1) % themeCount;
        }
    }
}
