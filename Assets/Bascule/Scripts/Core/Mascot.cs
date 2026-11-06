namespace Bascule.Core
{
    /// <summary>États de la mascotte (section 5).</summary>
    public enum Mood
    {
        Relaxed,    // planche stable : posture détendue, sourire
        Tense,      // inclinaison moyenne : bras crispés, front qui perle
        Strained,   // inclinaison forte : grimace, tremblement, pieds qui glissent
        LastEffort, // près de la défaite : dernier effort, yeux fermés
        Happy,      // fusion
        Cheering,   // le 128 s'envole : petit saut
        Defeated,   // la mascotte lâche
    }

    /// <summary>Expressions du visage, utilisées par les états et par les têtes de la collection.</summary>
    public enum Expression
    {
        Calm, Grin, Wink, Surprised, Tense, Grimace, Effort, Happy, Sleepy, Tongue, Smirk, Sad,
    }

    public static class MascotRules
    {
        /// <summary>État selon le danger (ratio d'inclinaison). Les événements (fusion, 128, défaite) passent par-dessus.</summary>
        public static Mood MoodFor(float danger, MascotTuning t)
        {
            if (danger >= t.lastEffortRatio) return Mood.LastEffort;
            if (danger >= t.strainedRatio) return Mood.Strained;
            if (danger >= t.tenseRatio) return Mood.Tense;
            return Mood.Relaxed;
        }

        public static bool IsPositive(Expression e) =>
            e == Expression.Calm || e == Expression.Grin || e == Expression.Wink || e == Expression.Happy ||
            e == Expression.Tongue || e == Expression.Smirk || e == Expression.Sleepy;

        /// <summary>Expression affichée : quand tout va bien, chaque tête garde la sienne.</summary>
        public static Expression ExpressionFor(Mood mood, Expression headExpression)
        {
            switch (mood)
            {
                case Mood.Relaxed: return IsPositive(headExpression) ? headExpression : Expression.Calm;
                case Mood.Tense: return Expression.Tense;
                case Mood.Strained: return Expression.Grimace;
                case Mood.LastEffort: return Expression.Effort;
                case Mood.Happy: return Expression.Happy;
                case Mood.Cheering: return Expression.Grin;
                case Mood.Defeated: return Expression.Sad;
                default: return Expression.Calm;
            }
        }

        /// <summary>Intensité du tremblement (0..1).</summary>
        public static float Tremble(Mood mood) =>
            mood == Mood.LastEffort ? 1f : mood == Mood.Strained ? 0.55f : mood == Mood.Tense ? 0.15f : 0f;

        public static bool Sweats(Mood mood) => mood == Mood.Tense || mood == Mood.Strained || mood == Mood.LastEffort;
    }
}
