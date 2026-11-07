using System;

namespace Bascule.Core
{
    /// <summary>
    /// Physique de l'inclinaison (section 4 du cahier des charges).
    /// La planche est un ressort amorti : elle vise un angle d'équilibre proportionnel au couple
    /// (différence de poids × distance au pivot), y va progressivement, oscille un peu, se stabilise.
    /// Convention : angle positif = côté droit vers le bas ; couple positif = côté droit plus lourd.
    /// </summary>
    public sealed class PlankModel
    {
        PlankTuning _t;

        public float Angle { get; private set; }
        public float AngularVelocity { get; private set; }
        /// <summary>Temps passé au-delà de l'angle max (se recharge quand on revient).</summary>
        public float StrainTime { get; private set; }
        public bool Lost { get; private set; }
        public float TargetAngle { get; private set; }
        /// <summary>Souffle restant (s) : planche plus amortie et défaite impossible (après l'envol d'un 128).</summary>
        public float Calm { get; private set; }

        /// <summary>Donne un souffle à la planche pendant <paramref name="seconds"/> secondes.</summary>
        public void GiveCalm(float seconds) { if (seconds > Calm) Calm = seconds; }

        /// <summary>
        /// Stabilisation après un gros poids créé d'un coup (64) : la planche est mieux amortie et la tension monte
        /// moins vite pendant quelques secondes. Contrairement au souffle du 128, on peut toujours perdre.
        /// </summary>
        public float Steady { get; private set; }
        public void GiveSteady(float seconds) { if (seconds > Steady) Steady = seconds; }

        public PlankModel(PlankTuning tuning) { _t = tuning; }

        public void SetTuning(PlankTuning tuning) { _t = tuning; }

        /// <summary>Multiplie l'inclinaison obtenue pour un même couple (1 = normal ; plus à haut score, voir Difficulty.Sensitivity).</summary>
        public float Sensitivity { get; set; } = 1f;

        /// <summary>|angle| / angle max. 1 = limite de défaite.</summary>
        public float Ratio => Math.Abs(Angle) / _t.maxAngle;

        /// <summary>Danger global (0..1) en tenant compte du temps passé au-delà de la limite.</summary>
        public float Danger
        {
            get
            {
                float r = Ratio;
                if (r < 1f) return r;
                return _t.loseGraceSeconds <= 0 ? 1f : 1f + StrainTime / _t.loseGraceSeconds;
            }
        }

        public float EquilibriumAngle(float torque) => _t.tiltDirection * _t.degreesPerTorque * Sensitivity * torque;

        /// <summary>Couple au-delà duquel l'équilibre dépasse l'angle max.</summary>
        public float LosingTorque => _t.maxAngle / (_t.degreesPerTorque * Sensitivity);

        /// <param name="dt">Pas de temps (s).</param>
        /// <param name="torque">Somme des valeurs × position normalisée sur la planche.</param>
        /// <param name="load">Somme des valeurs portées (ralentit les mouvements).</param>
        public void Step(float dt, float torque, float load)
        {
            if (Lost || dt <= 0f) return;

            float limit = _t.maxAngle + _t.overshootAngle;
            TargetAngle = Clamp(EquilibriumAngle(torque), -limit * 1.5f, limit * 1.5f);

            float freq = _t.frequency / (float)Math.Sqrt(1f + _t.loadSlowdown * Math.Max(0f, load));
            float w0 = 2f * (float)Math.PI * freq;

            // Sous-pas pour rester stable même si dt est grand (téléphone qui rame).
            int steps = Math.Max(1, (int)Math.Ceiling(dt / (1f / 120f)));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float damping = _t.dampingRatio * (Calm > 0f ? _t.balloonGraceDamping : Steady > 0f ? _t.bigMergeDamping : 1f);
                float acc = w0 * w0 * (TargetAngle - Angle) - 2f * damping * w0 * AngularVelocity;
                AngularVelocity += acc * h;
                Angle += AngularVelocity * h;
                if (Angle > limit) { Angle = limit; if (AngularVelocity > 0) AngularVelocity = 0; }
                if (Angle < -limit) { Angle = -limit; if (AngularVelocity < 0) AngularVelocity = 0; }
            }

            if (Calm > 0f)
            {
                // Souffle : on ne peut pas perdre, et la tension accumulée redescend.
                Calm = Math.Max(0f, Calm - dt);
                StrainTime = Math.Max(0f, StrainTime - dt * 2f);
            }
            else if (Math.Abs(Angle) > _t.maxAngle) StrainTime += dt * (Steady > 0f ? _t.bigMergeStrainRate : 1f);
            else StrainTime = Math.Max(0f, StrainTime - dt * 2f);

            if (Steady > 0f) Steady = Math.Max(0f, Steady - dt);
            if (StrainTime > _t.loseGraceSeconds) Lost = true;
        }

        /// <summary>Seconde chance : la mascotte se rééquilibre.</summary>
        public void Revive()
        {
            Lost = false;
            StrainTime = 0f;
            Calm = 0f;
            Steady = 0f;
            Angle = Clamp(Angle, -_t.maxAngle * 0.3f, _t.maxAngle * 0.3f);
            AngularVelocity = 0f;
        }

        public void Restore(float angle, float angularVelocity, float strainTime)
        {
            Lost = false;
            Calm = 0f;
            Steady = 0f;
            Angle = angle;
            AngularVelocity = angularVelocity;
            StrainTime = strainTime;
        }

        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
