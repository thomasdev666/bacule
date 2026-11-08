using System;
using System.Collections.Generic;

namespace Bascule.Core
{
    public enum WindPhase { None, Warning, Blowing }

    /// <summary>
    /// Rafales de vent (à partir de 5 000 points) : de temps en temps, le vent pousse la planche d'un côté.
    ///
    /// Déroulé d'une rafale :
    /// 1. Annonce (warningSeconds) : bandeau « Rafale », flèche et premiers traits de vent. Le joueur a le temps de réagir
    ///    (poser son poids de l'autre côté).
    /// 2. Souffle (blowSeconds) : un couple s'ajoute à celui des poids, monte en douceur, ondule un peu, puis retombe.
    ///
    /// Quand :
    /// - à chaque palier (5 000, 8 000, 10 000, puis tous les 2 500 points) ;
    /// - entre les paliers, au plus une fois toutes les 45 à 75 s, plus tôt si le joueur stagne (moins de 200 points
    ///   gagnés en 20 s : il joue la sécurité, on relance la tension) ;
    /// - jamais deux rafales à moins de 25 s, jamais pendant le souffle du 128 ou la stabilisation d'un 64,
    ///   jamais quand la planche est déjà en danger (on veut de la tension, pas une défaite injuste).
    ///
    /// Force : exprimée en part du couple de défaite. Le vent seul ne pousse jamais l'équilibre au-delà de
    /// maxPeakRatio (85 %) : il ne fait pas perdre une planche stable, mais il punit un joueur qui n'a pas réagi.
    /// </summary>
    public sealed class Gusts
    {
        readonly WindTuning _t;
        readonly GameRng _rng;

        public WindPhase Phase { get; private set; }
        /// <summary>+1 : le vent souffle vers la droite (le côté droit descend). -1 : vers la gauche.</summary>
        public int Direction { get; private set; } = 1;
        /// <summary>Force de la rafale en cours (part du couple de défaite).</summary>
        public float Strength { get; private set; }
        /// <summary>Nombre de rafales depuis le début de la partie.</summary>
        public int Count { get; private set; }
        /// <summary>Temps écoulé dans la phase en cours (s).</summary>
        public float PhaseTime { get; private set; }

        int _milestone;            // index du prochain palier
        int _nextMilestoneScore;
        bool _pending;             // un palier a été franchi, la rafale attend un moment juste
        float _sinceLast, _gap;
        float _windowTime;
        int _windowStartScore;
        bool _stagnant;
        bool _started;             // seuil de départ atteint

        // Duel : rafales programmées (mêmes instants, directions et forces pour les deux joueurs).
        List<DuelGust> _script;
        int _scriptIndex;
        float _clock, _scriptPeak;

        /// <summary>
        /// Rafales du duel : elles suivent la liste commune (DuelRules.GustSchedule), au même instant chez les deux
        /// joueurs, quel que soit le score. Seule la limite d'équité dépend de la planche de chacun.
        /// </summary>
        public static Gusts Scripted(WindTuning tuning, List<DuelGust> schedule, float maxPeakRatio)
        {
            var g = new Gusts(tuning, 1);
            g._script = schedule ?? new List<DuelGust>();
            g._scriptPeak = maxPeakRatio;
            return g;
        }

        /// <summary>Temps écoulé (duel), en secondes.</summary>
        public float Clock => _clock;

        public Gusts(WindTuning tuning, uint seed)
        {
            _t = tuning ?? new WindTuning();
            _rng = new GameRng(seed == 0 ? 0xA5A5u : seed);
            Reset(0);
        }

        /// <summary>Repart à zéro pour une partie qui commence (ou qui reprend) à ce score : les paliers déjà passés ne déclenchent rien.</summary>
        public void Reset(int score)
        {
            Phase = WindPhase.None;
            PhaseTime = 0f;
            Count = 0;
            _pending = false;
            _sinceLast = 0f;
            _gap = NextGap();
            _windowTime = 0f;
            _windowStartScore = score;
            _stagnant = false;
            _milestone = 0;
            _nextMilestoneScore = MilestoneScore(0);
            while (_nextMilestoneScore <= score) { _milestone++; _nextMilestoneScore = MilestoneScore(_milestone); }
            _started = score >= _t.startScore;
        }

        /// <summary>Après une seconde chance : le vent s'arrête et laisse le temps de souffler.</summary>
        public void Calm()
        {
            Phase = WindPhase.None;
            PhaseTime = 0f;
            _sinceLast = 0f;
            _pending = false;
        }

        /// <summary>Score du palier numéro <paramref name="index"/> (5 000, 8 000, 10 000, 12 500, 15 000…).</summary>
        public int MilestoneScore(int index)
        {
            var m = _t.milestones;
            if (m != null && index < m.Length) return m[index];
            int last = m != null && m.Length > 0 ? m[m.Length - 1] : _t.startScore;
            int extra = index - (m != null ? m.Length : 0) + 1;
            return last + Math.Max(500, _t.milestoneEvery) * extra;
        }

        float NextGap() => _t.gapMin + (_t.gapMax - _t.gapMin) * _rng.NextFloat();

        /// <summary>
        /// Avance le vent d'un pas.
        /// </summary>
        /// <param name="score">Score actuel.</param>
        /// <param name="danger">Danger de la planche (0..1, 1 = limite).</param>
        /// <param name="signedRatio">Angle / angle max, signé (+ = côté droit en bas).</param>
        /// <param name="blocked">Moment où une rafale ne doit pas commencer (souffle du 128, stabilisation, tir en cours…).</param>
        /// <returns>La phase qui vient de commencer (Warning ou Blowing), sinon None.</returns>
        public WindPhase Update(float dt, int score, float danger, float signedRatio, bool blocked)
        {
            if (dt <= 0f) return WindPhase.None;
            if (_script != null) return UpdateScripted(dt, signedRatio);
            if (!_started)
            {
                if (score < _t.startScore) return WindPhase.None;
                _started = true;
                _windowStartScore = score;
            }

            // Paliers franchis : une rafale est attendue.
            while (score >= _nextMilestoneScore)
            {
                _pending = true;
                _milestone++;
                _nextMilestoneScore = MilestoneScore(_milestone);
            }

            // Le joueur stagne-t-il ? (fenêtre glissante simple)
            _windowTime += dt;
            if (_windowTime >= _t.stagnationWindow)
            {
                _stagnant = score - _windowStartScore < _t.stagnationPoints;
                _windowTime = 0f;
                _windowStartScore = score;
            }

            switch (Phase)
            {
                case WindPhase.Warning:
                    PhaseTime += dt;
                    if (PhaseTime >= _t.warningSeconds) { Phase = WindPhase.Blowing; PhaseTime = 0f; return WindPhase.Blowing; }
                    return WindPhase.None;
                case WindPhase.Blowing:
                    PhaseTime += dt;
                    if (PhaseTime >= _t.blowSeconds) { Phase = WindPhase.None; PhaseTime = 0f; _sinceLast = 0f; }
                    return WindPhase.None;
            }

            _sinceLast += dt * (_stagnant ? _t.stagnationSpeedup : 1f);
            bool due = _pending || _sinceLast >= _gap;
            if (!due || blocked || _sinceLast < _t.minGap || danger > _t.maxDangerToStart) return WindPhase.None;

            // Nouvelle rafale.
            Direction = _rng.NextFloat() < 0.5f ? -1 : 1;
            float baseStrength = Math.Min(_t.strengthMax, _t.strengthStart + _t.strengthStep * Count);
            // Équité : le vent seul n'emmène pas l'équilibre au-delà de maxPeakRatio.
            float toward = signedRatio * Direction; // > 0 : la planche penche déjà du côté où le vent pousse
            Strength = Math.Max(_t.strengthMin, Math.Min(baseStrength, _t.maxPeakRatio - Math.Max(0f, toward)));
            Phase = WindPhase.Warning;
            PhaseTime = 0f;
            _pending = false;
            _sinceLast = 0f;
            _gap = NextGap();
            Count++;
            return WindPhase.Warning;
        }

        WindPhase UpdateScripted(float dt, float signedRatio)
        {
            _clock += dt;
            switch (Phase)
            {
                case WindPhase.Warning:
                    PhaseTime += dt;
                    if (PhaseTime >= _t.warningSeconds) { Phase = WindPhase.Blowing; PhaseTime = 0f; return WindPhase.Blowing; }
                    return WindPhase.None;
                case WindPhase.Blowing:
                    PhaseTime += dt;
                    if (PhaseTime >= _t.blowSeconds) { Phase = WindPhase.None; PhaseTime = 0f; }
                    return WindPhase.None;
            }
            if (_scriptIndex >= _script.Count || _clock < _script[_scriptIndex].At) return WindPhase.None;
            var g = _script[_scriptIndex++];
            Direction = g.Direction >= 0 ? 1 : -1;
            float toward = signedRatio * Direction;
            Strength = Math.Max(_t.strengthMin, Math.Min(g.Strength, _scriptPeak - Math.Max(0f, toward)));
            Phase = WindPhase.Warning;
            PhaseTime = 0f;
            Count++;
            return WindPhase.Warning;
        }

        /// <summary>Secondes avant la prochaine rafale du duel (annonce), ou -1 s'il n'y en a plus / hors duel.</summary>
        public float NextScriptedIn => _script != null && _scriptIndex < _script.Count ? _script[_scriptIndex].At - _clock : -1f;

        /// <summary>Intensité du souffle (0..1) : montée douce, ondulations, retombée. 0 pendant l'annonce.</summary>
        public float Envelope
        {
            get
            {
                if (Phase != WindPhase.Blowing) return 0f;
                float t = PhaseTime, d = Math.Max(0.01f, _t.blowSeconds), r = Math.Max(0.01f, Math.Min(_t.rampSeconds, d * 0.5f));
                float e = t < r ? Smooth(t / r) : t > d - r ? Smooth((d - t) / r) : 1f;
                // Ondulations : une vraie rafale n'est pas un mur, elle respire.
                float wobble = 0.86f + 0.14f * (float)Math.Sin(t * 7.3f) * (float)Math.Sin(t * 2.1f + 0.7f);
                return Clamp01(e * wobble);
            }
        }

        /// <summary>Avancement de l'annonce (0..1).</summary>
        public float WarningProgress => Phase == WindPhase.Warning ? Clamp01(PhaseTime / Math.Max(0.01f, _t.warningSeconds)) : 0f;

        /// <summary>Couple ajouté par le vent (même unité que celui des poids).</summary>
        public float Torque(float losingTorque) => Direction * Strength * losingTorque * Envelope;

        static float Smooth(float x) { x = Clamp01(x); return x * x * (3f - 2f * x); }
        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
