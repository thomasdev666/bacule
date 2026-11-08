using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using Bascule.UI;
using UnityEngine;

namespace Bascule
{
    /// <summary>Bilan d'une partie terminée.</summary>
    public sealed class RunResult
    {
        public GameMode Mode;
        public uint Seed;
        public string DayKey;
        public int Score;
        public float BestSave;
        public int Saves;
        public int Balloons;
        public int BestChain;
        public int MaxValue;
        public int Drops;
        public int LostWeights;
        public bool Revived;
        /// <summary>Rafales de vent traversées sans tomber.</summary>
        public int Gusts;
        public int Merges;
        /// <summary>Partie spéciale (récompenses seulement) et objectif de la partie.</summary>
        public SpecialKind Special;
        public int Goal;
        /// <summary>Durée de la partie (secondes de jeu, sans les pauses).</summary>
        public float Seconds;
        /// <summary>Danger maximal par tranche de <see cref="GameSession.TensionSlice"/> secondes (frise du partage).</summary>
        public List<float> Tension = new List<float>();

        public RunAchievements Achievements => new RunAchievements
        {
            Score = Score, BestSave = BestSave, Balloons = Balloons, BestChain = BestChain,
        };

        public RunStats Stats => new RunStats
        {
            Score = Score, Merges = Merges, MaxValue = MaxValue, Balloons = Balloons, BestChain = BestChain,
            BestSave = BestSave, Daily = Mode == GameMode.Daily, DayKey = DayKey,
            Gusts = Gusts, Seconds = Seconds, LostWeights = LostWeights, Drops = Drops,
            Special = Special, Goal = Goal, Seed = Seed,
        };
    }

    /// <summary>
    /// Une partie : la planche, la mascotte, les poids. Applique la boucle de jeu de la section 2 :
    /// viser en glissant, relâcher, atterrir, fusionner, perdre les poids qui tombent, lâcher si ça penche trop.
    /// La logique pure (inclinaison, score, suite des poids) vit dans Bascule.Core ; ici on la relie à la physique 2D.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public enum State { Intro, Arrival, Idle, Cooldown, Aiming, Losing, Over }

        // Dépendances
        Tuning _t;
        ArtCache _art;
        Font _font;
        Camera _cam;
        AudioService _audio;
        HapticsService _haptics;
        Effects _fx;

        // Monde
        MascotView _mascot;
        Rigidbody2D _plankBody;
        BoxCollider2D _plankCol;
        readonly BoxCollider2D[] _lips = new BoxCollider2D[2];
        readonly SpriteRenderer[] _lipSr = new SpriteRenderer[2];
        SpriteRenderer _plankSr, _guide;
        Backdrop _backdrop;
        Transform _weightsRoot;
        PhysicsMaterial2D _material;

        // Logique
        PlankModel _plank;
        SpawnSequence _seq;
        ScoreKeeper _score;
        NearMissTracker _nearMiss;
        readonly List<WeightBody> _weights = new List<WeightBody>();
        readonly List<KeyValuePair<WeightBody, WeightBody>> _pendingMerges = new List<KeyValuePair<WeightBody, WeightBody>>();
        readonly List<Phantom> _phantoms = new List<Phantom>();
        WeightBody _held;
        float _aimX, _cooldown, _runTime, _plankLength, _loseTimer, _torque, _load;
        bool _dragging;
        RunSnapshot _lossSnapshot;
        float _slowMo, _sweatTimer;
        bool _recordAnnounced;
        int _theme;
        // Partage : frise de tension et photo de la chute.
        public const float TensionSlice = 8f;
        readonly List<float> _tension = new List<float>();
        float _sliceTime, _slicePeak, _photoTimer;
        RenderTexture _photo;
        /// <summary>Photo de la chute (sans l'interface), prise juste après la bascule ; null avant la première chute.</summary>
        public RenderTexture Photo => _photo;
        /// <summary>Hauteur de la planche dans la photo (0 = bas, 1 = haut), pour cadrer la carte de score.</summary>
        public float PhotoFocusY { get; private set; } = 0.45f;
        public bool HasPhoto { get; private set; }

        /// <summary>Record à battre pendant la partie (pour l'annonce « nouveau record »).</summary>
        public int BestToBeat { get; set; }
        /// <summary>Objectif de la partie (0 : aucun) ; l'atteindre rapporte des pièces.</summary>
        public int Goal { get; set; }
        /// <summary>Partie spéciale annoncée sur l'accueil (ne change que les récompenses).</summary>
        public SpecialKind Special { get; set; }
        public bool GoalDone { get; private set; }
        /// <summary>Remontée du monde voulue par l'accueil (unités du monde), appliquée seulement au repos.</summary>
        public float HomeLift { get; set; }
        /// <summary>Hauteur des pieds de Basile à l'écran (pixels, depuis le bas), sans la remontée de l'accueil.</summary>
        public float FeetScreenY => _cam.WorldToScreenPoint(new Vector3(0, WorldLayout.GroundY(_cam) - _fx.Lift, 0)).y;
        public float PixelsPerUnit => _cam.pixelHeight / (2f * _cam.orthographicSize);
        bool _goalAnnounced;

        struct Phantom { public int Value; public float LocalX; public float Remaining; }

        public State Current { get; private set; } = State.Idle;
        public GameMode Mode { get; private set; }
        public uint Seed { get; private set; }
        public string DayKey { get; private set; } = "";
        public bool Revived { get; private set; }
        public bool Paused { get; private set; }
        public bool IsRunning => Current == State.Aiming || Current == State.Cooldown;
        public bool HasRun => Current != State.Idle && Current != State.Intro && Current != State.Arrival;
        public int Score => _score?.Score ?? 0;
        public int NextValue => _seq?.Next ?? 1;
        public int HeldValue => _seq?.Current ?? 1;
        public float Danger => _plank?.Danger ?? 0f;
        public float Angle => _plank?.Angle ?? 0f;
        public float Multiplier => _score?.Multiplier ?? 1f;
        public int Drops => _seq?.DropIndex ?? 0;
        /// <summary>Cœurs restants (−1 : système désactivé).</summary>
        /// <summary>Un cœur gagné ou perdu (pour l'animation du HUD).</summary>
        public MascotView Mascot => _mascot;

        public event Action ScoreChanged;
        public Backdrop Backdrop => _backdrop;
        public event Action Dropped;
        /// <summary>Une fusion vient de créer ce poids (tutoriel).</summary>
        public event Action<int> Merged;
        /// <summary>Éclair de couleur plein écran (gros combo, gros poids, 128) : intensité, couleur.</summary>
        public event Action<float, Color> ScreenFlash;
        /// <summary>Combo de N fusions enchaînées (N ≥ 2) : néon sur le contour de l'écran.</summary>
        public event Action<int> Combo;
        static readonly Color[] ComboColors =
        {
            ArtCache.Hex("#FFC93C"), ArtCache.Hex("#FF9A3C"), ArtCache.Hex("#FF5A8A"), ArtCache.Hex("#A66CFF"), ArtCache.Hex("#4A9BFF"), ArtCache.Hex("#2EC4A6"),
        };
        int _nextMilestone = 1000;
        /// <summary>Un poids est tombé (tutoriel).</summary>
        public event Action<int> WeightLost;
        public event Action<RunResult> RunEnded;
        /// <summary>Le score vient de dépasser le record pendant la partie.</summary>
        public event Action NewRecord;
        /// <summary>Le score vient d'atteindre l'objectif de la partie.</summary>
        public event Action GoalReachedEvent;
        /// <summary>Basile vient de lâcher (début de la chute, avant l'écran de fin) : le duel prévient l'adversaire tout de suite.</summary>
        public event Action LoseStarted;
        /// <summary>Temps de jeu de la partie (s), sans l'arrivée de Basile ni les pauses.</summary>
        public float RunTime => _runTime;
        /// <summary>Une rafale s'annonce (direction : +1 vers la droite).</summary>
        public event Action<int> WindWarned;
        /// <summary>La rafale commence à souffler.</summary>
        public event Action<int> WindBlowing;

        // Vent (à haut score, voir Core/Wind.cs)
        Gusts _wind;
        WindLayer _windFx;
        public WindPhase WindPhase => _wind?.Phase ?? WindPhase.None;
        public int WindDirection => _wind?.Direction ?? 1;
        public float WindEnvelope => _wind?.Envelope ?? 0f;
        public float WindWarning => _wind?.WarningProgress ?? 0f;
        /// <summary>Nombre de rafales de la partie (première rafale : le HUD explique quoi faire).</summary>
        public int WindCount => _wind?.Count ?? 0;
        /// <summary>Duel : secondes avant l'annonce de la prochaine rafale commune (-1 : aucune de prévue).</summary>
        public float NextGustIn => _wind?.NextScriptedIn ?? -1f;

        // ------------------------------------------------------------------ Mise en place

        public void Init(Tuning tuning, ArtCache art, Font font, Camera cam, AudioService audio, HapticsService haptics)
        {
            _t = tuning; _art = art; _font = font; _cam = cam; _audio = audio; _haptics = haptics;
            _material = new PhysicsMaterial2D("Bascule") { friction = _t.weights.friction, bounciness = _t.weights.bounciness };
            Physics2D.velocityIterations = 12;
            Physics2D.positionIterations = 8;
            // Correction des chevauchements plus douce : un poids qui s'enfonce un peu est ressorti en douceur,
            // au lieu d'être éjecté (ce qui faisait « exploser » une pile).
            Physics2D.maxLinearCorrection = 0.08f;
            Physics2D.maxAngularCorrection = 4f;
            // Les contacts sont réutilisés d'un pas à l'autre au lieu d'être recréés (moins de travail, pas d'à-coups).
            Physics2D.reuseCollisionCallbacks = true;

            var fxGo = new GameObject("Effets");
            fxGo.transform.SetParent(transform, false);
            _fx = fxGo.AddComponent<Effects>();
            _fx.Init(art, font, cam);
            var windGo = new GameObject("Vent");
            windGo.transform.SetParent(transform, false);
            _windFx = windGo.AddComponent<WindLayer>();
            _windFx.Init(art, cam);

            _weightsRoot = new GameObject("Poids").transform;
            _weightsRoot.SetParent(transform, false);
            _plank = new PlankModel(_t.plank);
            BuildWorld();
            SetPlankLength(_t.plank.startLength);
            PlacePlank(true);
        }

        public void ApplyTuning(Tuning tuning)
        {
            _t = tuning;
            _material.friction = _t.weights.friction;
            _material.bounciness = _t.weights.bounciness;
            _plank?.SetTuning(_t.plank);
        }

        public void SetHead(HeadDef head) => _mascot.SetHead(head);

        int _plankSkin;

        /// <summary>Apparence de la planche (boutique) : seul le dessin change, jamais la taille ni la physique.</summary>
        public void SetPlank(PlankSkin skin)
        {
            if (skin == null || _plankSr == null || skin.Id == _plankSkin) return;
            _plankSkin = skin.Id;
            var raster = SceneArtist.Plank(_t.plank.startLength, _t.plank.thickness, PlankPpu, out int border, skin);
            var size = _plankSr.size;
            _plankSr.sprite = ArtCache.ToSprite(raster, PlankPpu, new Vector4(border, 0, border, 0));
            _plankSr.size = size;
            ApplyLipArt(skin);
            SetPlankLength(_plankLength); // le rebord se replace (l'arrondi du bout dépend de la planche)
        }

        public void SetReducedMotion(bool on) { _fx.ReducedMotion = on; _mascot.ReducedMotion = on; }

        /// <summary>À rappeler si l'écran change de taille (rotation interdite, mais tablettes / pliables).</summary>
        public void Layout()
        {
            WorldLayout.Fit(_cam);
            _fx.RecenterCamera();
            _backdrop.Layout();
            // Pendant l'arrivée, la mascotte et la planche sont placées par l'animation (l'écran change souvent de
            // taille au lancement, notamment avec Unity Remote : ne pas la téléporter au milieu).
            if (Current == State.Intro) { _introStartX = -WorldLayout.HalfWidth(_cam) - 1.8f; return; }
            if (Current == State.Arrival) return; // l'arrivée place elle-même la mascotte et la planche
            float ground = WorldLayout.GroundY(_cam);
            _mascot.transform.position = new Vector3(0, ground, 0);
            PlacePlank(true);
        }

        void BuildWorld()
        {
            var bd = new GameObject("Decor");
            bd.transform.SetParent(transform, false);
            _backdrop = bd.AddComponent<Backdrop>();
            _backdrop.Init(_art, _cam);

            _mascot = MascotView.Create(transform, _art);
            _mascot.HitGround += p =>
            {
                _fx.Dust(p, 1f);
                _fx.Dust(p + new Vector2(-1.5f, 0), 0.8f);
                _fx.Shake(0.7f);
                _audio.Play(Sfx.Thud, 64, 1f);
                _haptics.Medium();
            };

            var plankGo = new GameObject("Planche");
            plankGo.transform.SetParent(transform, false);
            plankGo.AddComponent<PlankMarker>();
            _plankBody = plankGo.AddComponent<Rigidbody2D>();
            _plankBody.bodyType = RigidbodyType2D.Kinematic;
            _plankBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _plankCol = plankGo.AddComponent<BoxCollider2D>();
            _plankCol.sharedMaterial = _material;
            var woodRaster = SceneArtist.Plank(_t.plank.startLength, _t.plank.thickness, PlankPpu, out int border);
            var wood = ArtCache.ToSprite(woodRaster, PlankPpu, new Vector4(border, 0, border, 0));
            _plankSr = SpriteUtil.Make("Bois", plankGo.transform, wood, Color.white, Layers.Plank);
            _plankSr.drawMode = SpriteDrawMode.Sliced;
            // Rebords en bois aux deux bouts : de vraies collisions, qui bougent avec la planche.
            for (int i = 0; i < 2; i++)
            {
                var lip = new GameObject(i == 0 ? "RebordGauche" : "RebordDroit");
                lip.transform.SetParent(plankGo.transform, false);
                lip.AddComponent<PlankMarker>();
                _lips[i] = lip.AddComponent<BoxCollider2D>();
                _lips[i].sharedMaterial = _material;
                _lipSr[i] = SpriteUtil.Make("Rebord", lip.transform, null, Color.white, Layers.Plank + 1);
                _lipSr[i].flipX = i == 1; // le rebord droit est le miroir du gauche
            }

            ApplyLipArt(PlankCatalog.Get(0)); // planche en bois au départ (SetPlank change ensuite si besoin)
            _guide = SpriteUtil.Make("Visee", transform, _art.Pixel, new Color(1, 1, 1, 0.45f), Layers.Held - 1);
            _guide.enabled = false;
            Layout();
        }

        void SetPlankLength(float len)
        {
            _plankLength = len;
            float th = _t.plank.thickness;
            _plankCol.size = new Vector2(len, th);
            // La texture a 6 px de marge (contour) de chaque côté.
            float pad = 12f / PlankPpu;
            _plankSr.size = new Vector2(len + pad, th + pad);
            float lh = _t.plank.endLipHeight, lw = _t.plank.endLipWidth;
            for (int i = 0; i < 2; i++)
            {
                if (_lips[i] == null) continue;
                bool on = lh > 0.01f && lw > 0.01f;
                _lips[i].enabled = on;
                _lipSr[i].enabled = on;
                if (!on) continue;
                float side = i == 0 ? -1f : 1f;
                // Posé sur le dessus plat de la planche, juste après l'arrondi du bout (même place que son dessin).
                _lips[i].transform.localPosition = new Vector3(side * (len * 0.5f - _lipInset - lw * 0.5f), th * 0.5f + lh * 0.5f - 0.02f, 0);
                _lips[i].size = new Vector2(lw, lh + 0.04f);
                // Le dessin du rebord est ancré sur son coin bas-extérieur, au ras du dessus de la planche.
                _lipSr[i].transform.localScale = Vector3.one;
                _lipSr[i].transform.localPosition = new Vector3(side * lw * 0.5f, -lh * 0.5f + 0.02f, 0);
            }
        }

        const float PlankPpu = 160f, LipPpu = 400f;

        /// <summary>Dessine les rebords assortis à la planche (rondin, bambou, colonne, sucre d'orge…), une fois par planche.</summary>
        float _lipInset = 0.08f;

        void ApplyLipArt(PlankSkin skin)
        {
            _lipInset = SceneArtist.LipInset(skin, _t.plank.thickness);
            float lw = Mathf.Max(0.05f, _t.plank.endLipWidth), lh = Mathf.Max(0.05f, _t.plank.endLipHeight);
            var raster = SceneArtist.PlankLip(skin, lw, lh, LipPpu);
            var pivot = new Vector2(SceneArtist.LipPad / SceneArtist.LipImageW, SceneArtist.LipPad / SceneArtist.LipImageH);
            var sprite = ArtCache.ToSprite(raster, LipPpu, Vector4.zero, pivot);
            foreach (var sr in _lipSr)
            {
                if (sr == null) continue;
                sr.drawMode = SpriteDrawMode.Simple;
                sr.sprite = sprite;
                sr.color = Color.white;
            }
        }

        Vector2 PlankCenter(out Vector2 along)
        {
            float rad = -_plank.Angle * Mathf.Deg2Rad;
            along = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var normal = new Vector2(-along.y, along.x);
            return _mascot.PivotWorld + normal * (_t.plank.thickness * 0.5f);
        }

        void PlacePlank(bool instant)
        {
            if (_plankBody.bodyType != RigidbodyType2D.Kinematic) return;
            var center = PlankCenter(out _);
            float rot = -_plank.Angle;
            if (instant)
            {
                _plankBody.position = center;
                _plankBody.rotation = rot;
                _plankBody.transform.SetPositionAndRotation(center, Quaternion.Euler(0, 0, rot));
            }
            else
            {
                _plankBody.MovePosition(center);
                _plankBody.MoveRotation(rot);
            }
        }

        // ------------------------------------------------------------------ Cycle d'une partie

        public void StartRun(GameMode mode, uint seed, string dayKey)
        {
            ClearWorld();
            Mode = mode; Seed = seed; DayKey = dayKey ?? ""; Revived = false;
            _recordAnnounced = false;
            _goalAnnounced = false; GoalDone = false;
            _nextMilestone = 1000;
            _plank = new PlankModel(_t.plank);
            // Duel : rafales programmées, les mêmes au même instant chez les deux joueurs.
            _wind = mode == GameMode.Duel
                ? Gusts.Scripted(_t.wind, DuelRules.GustSchedule(seed, _t.duel), _t.duel.gustMaxPeakRatio)
                : new Gusts(_t.wind, seed ^ 0x5A17C3u);
            _gustsSurvived = 0; _lastWind = WindPhase.None;
            _seq = new SpawnSequence(seed, _t.spawn);
            _score = new ScoreKeeper(_t.score);
            NewNearMiss();
            _runTime = 0;
            _tension.Clear(); _sliceTime = 0; _slicePeak = 0; HasPhoto = false;
            SetPlankLength(Difficulty.PlankLength(0, _t.plank));
            // Basile arrive en marchant, la planche sur les épaules, puis la partie commence.
            _arriveT = 0f;
            _mascot.transform.position = new Vector3(-WorldLayout.HalfWidth(_cam) - 1.8f, WorldLayout.GroundY(_cam), 0);
            _mascot.Walking = true;
            _mascot.HandsOverride = true;
            _mascot.CushionVisible = true;
            Current = State.Arrival;
            ScoreChanged?.Invoke();
        }

        const float ArriveSeconds = 1.6f;
        float _arriveT;

        /// <summary>Arrivée en début de partie : la mascotte et la planche entrent ensemble depuis la gauche.</summary>
        void UpdateArrival(float dt)
        {
            _arriveT += dt;
            float k = Mathf.Clamp01(_arriveT / ArriveSeconds);
            float e = 1f - (1f - k) * (1f - k);
            float startX = -WorldLayout.HalfWidth(_cam) - 1.8f;
            float x = Mathf.Lerp(startX, 0f, e);
            _mascot.transform.position = new Vector3(x, WorldLayout.GroundY(_cam), 0);
            PlacePlank(true);
            // Les mains tiennent la planche par-dessous pendant la marche.
            var c = PlankCenter(out _);
            float reach = MascotRig.HandReach, below = _t.plank.thickness * 0.5f + 0.04f;
            _mascot.HandLeftWorld = c + new Vector2(-reach, -below);
            _mascot.HandRightWorld = c + new Vector2(reach, -below);
            if (k >= 1f)
            {
                _mascot.Walking = false;
                _mascot.HandsOverride = false;
                _mascot.Happy();
                Current = State.Cooldown;
                _cooldown = 0.35f;
            }
        }

        public void ResumeRun(RunSnapshot s)
        {
            ClearWorld();
            Mode = s.mode; Seed = s.seed; DayKey = s.dayKey ?? ""; Revived = s.revived;
            _recordAnnounced = s.score > BestToBeat;
            Goal = s.goal; Special = (SpecialKind)s.special;
            GoalDone = _goalAnnounced = Goal > 0 && s.score >= Goal;
            _nextMilestone = s.score / 1000 * 1000 + 1000;
            _plank = new PlankModel(_t.plank);
            _plank.Restore(s.plankAngle, s.plankVelocity, s.strainTime);
            _plank.Sensitivity = Difficulty.Sensitivity(s.score, _t.plank);
            // Reprise (ou seconde chance) : pas de rafale « en retard » pour les paliers déjà passés, et un temps de répit.
            _wind = new Gusts(_t.wind, s.seed ^ 0x5A17C3u ^ (uint)s.score);
            _lastWind = WindPhase.None; _gustsSurvived = s.gusts;
            _wind.Reset(s.score);
            _seq = new SpawnSequence(s.seed, _t.spawn);
            _seq.Restore(s.rngState, s.dropIndex, s.current, s.next);
            _score = new ScoreKeeper(_t.score);
            _score.Restore(s.score, s.bestChain, s.merges, s.balloons, s.maxValue, s.lostWeights);
            NewNearMiss();
            _nearMiss.Restore(s.bestSave, s.saves);
            _runTime = s.elapsed;
            SetPlankLength(Difficulty.PlankLength(s.score, _t.plank));
            PlacePlank(true);
            foreach (var w in s.weights)
            {
                var body = SpawnWeight(w.value, new Vector2(w.x, w.y), w.landed);
                body.Body.rotation = w.rotation;
                body.transform.rotation = Quaternion.Euler(0, 0, w.rotation);
                body.Body.SetVelocity(new Vector2(w.vx, w.vy));
                body.Body.angularVelocity = w.angularVelocity;
            }
            Current = State.Cooldown;
            _cooldown = 0.4f;
            ScoreChanged?.Invoke();
        }

        /// <summary>Photo de la partie pour la reprendre plus tard (le poids tenu n'est pas lâché : il revient).</summary>
        public RunSnapshot Capture()
        {
            if (_seq == null) return null;
            var s = new RunSnapshot
            {
                mode = Mode, seed = Seed, dayKey = DayKey, rngState = _seq.RngState, dropIndex = _seq.DropIndex,
                current = _seq.Current, next = _seq.Next, plankAngle = _plank.Angle, plankVelocity = _plank.AngularVelocity,
                strainTime = _plank.StrainTime, score = _score.Score, bestChain = _score.BestChain, merges = _score.Merges,
                balloons = _score.Balloons, maxValue = _score.MaxValue, lostWeights = _score.LostWeights, bestSave = _nearMiss.BestSave, saves = _nearMiss.Saves,
                gusts = _gustsSurvived, revived = Revived, elapsed = _runTime, goal = Goal, special = (int)Special,
            };
            foreach (var w in _weights)
            {
                if (w == null || w.Merging) continue;
                var p = w.Body.position;
                var v = w.Body.GetVelocity();
                s.weights.Add(new WeightState
                {
                    value = w.Value, x = p.x, y = p.y, rotation = w.Body.rotation, vx = v.x, vy = v.y,
                    angularVelocity = w.Body.angularVelocity, landed = w.Landed,
                });
            }
            return s;
        }

        public bool CanRevive => Current == State.Over && !Revived && _lossSnapshot != null && Mode != GameMode.Duel;

        /// <summary>
        /// Seconde chance : on revient à l'instant de la chute, la planche est remise à plat
        /// et on retire les poids du côté lourd jusqu'à un équilibre jouable.
        /// </summary>
        public void Revive()
        {
            if (!CanRevive) return;
            var s = _lossSnapshot;
            s.revived = true;
            // On remet la planche à plat en faisant tourner les poids avec elle autour du pivot.
            var pivot = _mascot.PivotWorld;
            var undo = Quaternion.Euler(0, 0, s.plankAngle);
            foreach (var w in s.weights)
            {
                Vector2 p = undo * new Vector3(w.x - pivot.x, w.y - pivot.y, 0);
                w.x = pivot.x + p.x; w.y = pivot.y + p.y + 0.02f;
                w.rotation += s.plankAngle;
            }
            s.plankAngle = 0; s.plankVelocity = 0; s.strainTime = 0;
            float limit = new PlankModel(_t.plank) { Sensitivity = Difficulty.Sensitivity(s.score, _t.plank) }.LosingTorque * 0.3f;
            float half = _t.plank.referenceHalfLength;
            float Torque() { float tq = 0; foreach (var w in s.weights) if (w.landed) tq += WeightCatalog.TorqueWeight(w.value, _t.plank) * (w.x - _mascot.PivotWorld.x) / half; return tq; }
            float torque = Torque();
            while (Mathf.Abs(torque) > limit && s.weights.Count > 0)
            {
                float sign = Mathf.Sign(torque);
                WeightState worst = null; float worstT = 0;
                foreach (var w in s.weights)
                {
                    float c = WeightCatalog.TorqueWeight(w.value, _t.plank) * (w.x - _mascot.PivotWorld.x) / half * sign;
                    if (c > worstT) { worstT = c; worst = w; }
                }
                if (worst == null) break;
                s.weights.Remove(worst);
                torque = Torque();
            }
            // Les poids repartent immobiles, posés.
            foreach (var w in s.weights) { w.vx = w.vy = w.angularVelocity = 0; }
            _mascot.ResetPose();
            ResumeRun(s);
            Revived = true;
            _audio.Play(Sfx.Phew);
        }

        public RunResult BuildResult() => new RunResult
        {
            Mode = Mode, Seed = Seed, DayKey = DayKey, Score = Score,
            BestSave = _nearMiss?.BestSave ?? 0, Saves = _nearMiss?.Saves ?? 0,
            Balloons = _score?.Balloons ?? 0, BestChain = _score?.BestChain ?? 0, MaxValue = _score?.MaxValue ?? 0,
            Drops = Drops, Revived = Revived,
            LostWeights = _score?.LostWeights ?? 0,
            Merges = _score?.Merges ?? 0, Seconds = _runTime, Gusts = _gustsSurvived,
            Special = Special, Goal = Goal,
            Tension = new List<float>(_tension),
        };

        /// <summary>Retour à l'accueil : planche vide, mascotte détendue.</summary>
        // ------------------------------------------------------------------ Arrivée au lancement

        float _introT, _introStartX;
        int _introSmoothFrames;
        const float WalkTime = 2.4f, CrouchTime = 0.6f, LiftTime = 0.9f;

        public bool InIntro => Current == State.Intro;

        /// <summary>
        /// Au lancement de l'appli : la planche est posée au sol, la mascotte arrive en marchant,
        /// s'accroupit, l'attrape et la soulève, prête à jouer.
        /// </summary>
        public void PlayIntro()
        {
            GoIdle();
            Current = State.Intro;
            _introT = 0f;
            _introSmoothFrames = 0;
            _introStartX = -WorldLayout.HalfWidth(_cam) - 1.8f;
            var ground = WorldLayout.GroundY(_cam);
            _mascot.transform.position = new Vector3(_introStartX, ground, 0);
            _mascot.Walking = true;
            _mascot.HandsOverride = true;
            _mascot.CushionVisible = false;
            PlaceIntroPlank(0f);
        }

        /// <summary>Le joueur est pressé : on saute l'arrivée.</summary>
        public void SkipIntro()
        {
            if (Current != State.Intro) return;
            _mascot.ResetPose();
            _mascot.transform.position = new Vector3(0, WorldLayout.GroundY(_cam), 0);
            Current = State.Idle;
            PlacePlank(true);
        }

        Vector2 GroundPlankCenter() => new Vector2(0, WorldLayout.GroundY(_cam) + _t.plank.thickness * 0.5f + 0.05f);

        void PlaceIntroPlank(float lift)
        {
            var normal = PlankCenter(out _);
            var c = Vector2.Lerp(GroundPlankCenter(), normal, lift);
            _plankBody.position = c;
            _plankBody.rotation = 0;
            _plankBody.transform.SetPositionAndRotation(c, Quaternion.identity);
        }

        void UpdateIntro(float dt)
        {
            // Les premières images après le lancement sont lentes (dessins, chargement) : la mascotte attend hors de
            // l'écran que le jeu tourne de façon fluide, sinon toute sa marche passerait en quelques images saccadées.
            if (_introSmoothFrames < 12)
            {
                _introSmoothFrames = dt < 0.05f ? _introSmoothFrames + 1 : 0;
                _mascot.transform.position = new Vector3(_introStartX, WorldLayout.GroundY(_cam), 0);
                PlaceIntroPlank(0f);
                return;
            }
            _introT += Mathf.Min(dt, 1f / 30f);
            float ground = WorldLayout.GroundY(_cam);
            var m = _mascot.transform;
            float reach = MascotRig.HandReach, below = _t.plank.thickness * 0.5f + 0.04f;
            if (_introT < WalkTime)
            {
                float k = _introT / WalkTime;
                k = 1f - (1f - k) * (1f - k);
                m.position = new Vector3(Mathf.Lerp(_introStartX, 0f, k), ground, 0);
                _mascot.HandLeftWorld = m.TransformPoint(new Vector3(-1.25f, 1.35f, 0));
                _mascot.HandRightWorld = m.TransformPoint(new Vector3(1.25f, 1.35f, 0));
                PlaceIntroPlank(0f);
                return;
            }
            m.position = new Vector3(0, ground, 0);
            _mascot.Walking = false;
            var gc = GroundPlankCenter();
            if (_introT < WalkTime + CrouchTime)
            {
                // Elle s'accroupit et pose les mains sur la planche.
                float k = (_introT - WalkTime) / CrouchTime;
                _mascot.ExtraSquash = Mathf.Lerp(0f, 0.38f, k);
                var restL = m.TransformPoint(new Vector3(-1.25f, 1.35f, 0));
                var restR = m.TransformPoint(new Vector3(1.25f, 1.35f, 0));
                _mascot.HandLeftWorld = Vector2.Lerp(restL, gc + new Vector2(-reach, below), k);
                _mascot.HandRightWorld = Vector2.Lerp(restR, gc + new Vector2(reach, below), k);
                PlaceIntroPlank(0f);
                return;
            }
            float t = Mathf.Clamp01((_introT - WalkTime - CrouchTime) / LiftTime);
            float e = t * t * (3f - 2f * t);
            _mascot.ExtraSquash = Mathf.Lerp(0.38f, 0f, e);
            PlaceIntroPlank(e);
            var c = _plankBody.position;
            _mascot.HandLeftWorld = c + new Vector2(-reach, -below);
            _mascot.HandRightWorld = c + new Vector2(reach, -below);
            _mascot.CushionVisible = e > 0.9f;
            if (t >= 1f)
            {
                _mascot.HandsOverride = false;
                _mascot.ExtraSquash = 0f;
                _mascot.CushionVisible = true;
                _mascot.Happy();
                _audio.Play(Sfx.Thud, 8, 0.6f);
                Current = State.Idle;
            }
        }

        public void GoIdle()
        {
            ClearWorld();
            _seq = null;
            _score = null;
            _plank = new PlankModel(_t.plank);
            SetPlankLength(_t.plank.startLength);
            PlacePlank(true);
            Current = State.Idle;
            ScoreChanged?.Invoke();
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            _dragging = false;
        }

        /// <summary>Le vent à l'écran : traits d'air, feuilles, nuages qui filent, légère secousse pendant le souffle.</summary>
        void UpdateWindFx()
        {
            bool playing = Current == State.Aiming || Current == State.Cooldown;
            float warn = playing && WindPhase == WindPhase.Warning ? 0.35f + 0.65f * WindWarning : 0f;
            float blow = playing ? WindEnvelope : 0f;
            _windFx.Set(warn, blow, WindDirection, _fx.ReducedMotion);
            _backdrop.Wind = WindDirection * (warn * 0.6f + blow * 2.4f);
            if (blow > 0.2f && !Paused) _fx.Shake(0.05f * blow);
        }

        void ClearWorld()
        {
            _windFx?.Clear();
            _mascot.transform.position = new Vector3(0, WorldLayout.GroundY(_cam), 0);
            foreach (var w in _weights) if (w != null) Destroy(w.gameObject);
            _weights.Clear();
            if (_held != null) Destroy(_held.gameObject);
            _held = null;
            _pendingMerges.Clear();
            _phantoms.Clear();
            _fx.Clear();
            _lossSnapshot = null;
            _dragging = false;
            if (_slowMo > 0) { _slowMo = 0; Time.timeScale = Paused ? 0f : 1f; }
            _guide.enabled = false;
            _plankBody.bodyType = RigidbodyType2D.Kinematic;
            _plankBody.SetVelocity(Vector2.zero);
            _plankBody.angularVelocity = 0;
            _mascot.ResetPose();
        }

        void NewNearMiss()
        {
            _nearMiss = new NearMissTracker();
            _nearMiss.SaveCompleted += peak =>
            {
                if (peak < 0.9f || !IsRunning) return;
                _fx.Text(_mascot.PivotWorld + new Vector2(0, 2.4f), Loc.Format("save", NearMissTracker.DisplayPercent(peak)), Palette.Accent, 1.2f, 1.6f);
                if (!_fx.ReducedMotion) _slowMo = 0.6f; // ralenti : le moment fort du jeu
                _audio.Play(Sfx.Phew);
                _haptics.Medium();
            };
        }

        // ------------------------------------------------------------------ Boucle

        void Update()
        {
            float dt = Time.deltaTime;
            _fx.LiftTarget = Current == State.Idle ? HomeLift : 0f;
            _backdrop.SetLift(_fx.Lift);
            UpdateBackground(dt);
            // La tête rougit doucement avec le score (racine : lent au début) et avec la charge portée.
            float load = Mathf.Clamp01(_load / 220f);
            bool calm = Current == State.Idle || Current == State.Intro || Current == State.Arrival;
            _mascot.Effort = calm ? 0f : Mathf.Clamp01(Mathf.Sqrt(Score / 60000f) * 0.8f + load * 0.35f);
            if (Current == State.Intro) UpdateIntro(dt);
            if (Current == State.Arrival) UpdateArrival(dt);
            _mascot.Tick(dt, _plank.Angle, calm ? 0f : _plank.Danger, load, _t.mascot);
            UpdateWindFx();
            if (Paused) return;
            UpdateSlowMotion();
            UpdateSweat(dt);

            switch (Current)
            {
                case State.Cooldown:
                    _cooldown -= dt;
                    if (_cooldown <= 0) SpawnHeld();
                    break;
                case State.Aiming:
                    HandleAim();
                    break;
                case State.Losing:
                    _loseTimer -= dt;
                    if (_photoTimer > 0 && (_photoTimer -= dt) <= 0) TakePhoto();
                    if (_loseTimer <= 0)
                    {
                        Current = State.Over;
                        RunEnded?.Invoke(BuildResult());
                    }
                    break;
            }

            if (_held != null)
            {
                float bob = Mathf.Sin(Time.time * 3f) * 0.06f;
                _held.transform.position = new Vector3(_aimX, WorldLayout.SpawnY(_cam) + bob, 0);
                float top = WorldLayout.SpawnY(_cam) - _held.Size * 0.5f;
                float bottom = _mascot.PivotWorld.y + 0.2f;
                _guide.enabled = top > bottom;
                _guide.transform.position = new Vector3(_aimX, (top + bottom) * 0.5f, 0);
                _guide.transform.localScale = new Vector3(0.05f, Mathf.Max(0.01f, top - bottom), 1);
            }
            else _guide.enabled = false;
        }

        /// <summary>Duel : le poids est là mais on ne peut pas encore le lâcher (compte à rebours).</summary>
        public bool InputLocked { get; set; }

        void HandleAim()
        {
            if (InputLocked) { _dragging = false; return; }
            if (!PointerInput.Read(out var screen, out bool pressed, out bool held, out bool released)) return;
            if (pressed && !PointerInput.OverUi(screen)) _dragging = true;
            if (!_dragging) return;
            if (held || released || pressed)
            {
                var world = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
                float hw = AimLimit(_held.Size);
                _aimX = Mathf.Clamp(world.x, -hw, hw);
            }
            if (released)
            {
                _dragging = false;
                Drop();
            }
        }

        /// <summary>On ne vise qu'au-dessus de la planche : un poids lâché tombe forcément dessus.</summary>
        float AimLimit(float size)
        {
            float lip = _t.plank.endLipHeight > 0.01f ? _t.plank.endLipWidth : 0f;
            float plankHalf = (_plankLength * 0.5f - lip) * Mathf.Cos(_plank.Angle * Mathf.Deg2Rad);
            float screen = WorldLayout.HalfWidth(_cam) - 0.1f;
            return Mathf.Max(0.1f, Mathf.Min(plankHalf, screen) - size * 0.45f);
        }

        void SpawnHeld()
        {
            _held = WeightBody.Create(_weightsRoot, _seq.Current, _art, _font, _t.weights, _material);
            _held.SetHeld(true);
            _held.PopIn(0.4f);
            float hw = AimLimit(_held.Size);
            _aimX = Mathf.Clamp(_aimX, -hw, hw);
            _held.transform.position = new Vector3(_aimX, WorldLayout.SpawnY(_cam), 0);
            Current = State.Aiming;
        }

        void Drop()
        {
            if (_held == null) return;
            var w = _held;
            _held = null;
            w.transform.position = new Vector3(_aimX, WorldLayout.SpawnY(_cam), 0);
            w.Body.position = w.transform.position;
            Hook(w);
            w.Dropped = true;
            w.SetHeld(false);
            w.Body.SetVelocity(new Vector2(0, -1f));
            _weights.Add(w);
            _seq.Advance();
            _score.OnDrop();
            float len = Difficulty.PlankLength(_score.Score, _t.plank);
            if (Mathf.Abs(len - _plankLength) > 0.02f) SetPlankLength(len);
            Current = State.Cooldown;
            _cooldown = _t.spawn.dropCooldown;
            _audio.Play(Sfx.Click, 1, 0.5f);
            Dropped?.Invoke();
        }

        WeightBody SpawnWeight(int value, Vector2 pos, bool landed)
        {
            var w = WeightBody.Create(_weightsRoot, value, _art, _font, _t.weights, _material);
            // Le poids fusionné est plus gros que les deux d'origine : on le pose dans l'angle de la planche
            // et on le remonte jusqu'à ce qu'il ne chevauche plus rien (sinon le moteur l'éjecte avec la pile).
            float rot = landed ? -_plank.Angle : 0f;
            pos = FreeSpot(pos, w.Size, w.HalfHeight, rot);
            w.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, rot));
            w.Body.position = pos;
            w.Body.rotation = rot;
            w.Landed = landed;
            w.Dropped = true;
            w.SetHeld(false);
            Hook(w);
            _weights.Add(w);
            return w;
        }

        void Hook(WeightBody w)
        {
            w.OnTouchSame = (a, b) => _pendingMerges.Add(new KeyValuePair<WeightBody, WeightBody>(a, b));
            w.OnFirstLanding = OnLanded;
        }

        void OnLanded(WeightBody w)
        {
            if (!IsRunning) return;
            _audio.Play(Sfx.Thud, w.Value, Mathf.Clamp(0.5f + w.Value * 0.03f, 0.5f, 1f));
            _mascot.Absorb(w.Value);
            float strength = Mathf.Clamp01(WeightCatalog.IndexOf(w.Value) / 6f);
            _fx.Dust(w.Body.position - new Vector2(0, w.HalfHeight), strength);
            w.Squash(0.18f + strength * 0.12f);
            if (w.Value >= 16) _fx.Shake(0.15f + strength * 0.2f);
            if (w.Value >= 8) _haptics.Light();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (Current == State.Intro || Current == State.Arrival) return;
            if (Current == State.Idle || Current == State.Over)
            {
                _torque = 0; _load = 0;
                if (Current == State.Idle) { _plank.Step(dt, 0, 0); PlacePlank(false); }
                return;
            }
            if (Current == State.Losing) { CheckLostWeights(); return; }

            _runTime += dt;
            LimitSpeeds();
            ProcessMerges();
            ComputeTorque(dt, out _torque, out _load);
            _plank.Sensitivity = CurrentSensitivity();
            _plank.Step(dt, _torque + StepWind(dt), _load);
            _nearMiss.Update(_plank.Danger, _plank.Lost);
            if (!_plank.Lost)
            {
                _slicePeak = Mathf.Max(_slicePeak, _plank.Danger);
                _sliceTime += dt;
                if (_sliceTime >= TensionSlice) { _tension.Add(_slicePeak); _sliceTime = 0; _slicePeak = 0; }
            }
            PlacePlank(false);
            CheckLostWeights();
            if (_plank.Lost) BeginLose();
        }

        /// <summary>Sensibilité de la planche : selon le score, et en duel aussi selon le temps (la pression monte toujours).</summary>
        float CurrentSensitivity()
        {
            float s = Difficulty.Sensitivity(Score, _t.plank);
            if (Mode == GameMode.Duel) s = Mathf.Max(s, DuelRules.Sensitivity(_runTime, _t.duel));
            return s;
        }

        int _gustsSurvived;
        WindPhase _lastWind;

        /// <summary>Fait avancer le vent ; renvoie le couple qu'il ajoute à la planche.</summary>
        float StepWind(float dt)
        {
            if (_wind == null) return 0f;
            // Pas de rafale pendant le souffle du 128, la stabilisation d'un 64, ni dans les toutes premières secondes.
            bool blocked = _plank.Calm > 0f || _plank.Steady > 0f || _runTime < 3f;
            float signedRatio = _plank.Angle / Mathf.Max(1f, _t.plank.maxAngle) * _t.plank.tiltDirection;
            var started = _wind.Update(dt, Score, _plank.Danger, signedRatio, blocked);
            // Une rafale traversée : son souffle s'est terminé et la planche tient toujours.
            if (_lastWind == WindPhase.Blowing && _wind.Phase == WindPhase.None && !_plank.Lost) _gustsSurvived++;
            _lastWind = _wind.Phase;
            if (started == WindPhase.Warning)
            {
                _audio.Play(Sfx.Wind, 1, 0.9f);
                _haptics.Light();
                WindWarned?.Invoke(_wind.Direction);
            }
            else if (started == WindPhase.Blowing)
            {
                _haptics.Medium();
                WindBlowing?.Invoke(_wind.Direction);
            }
            return _wind.Torque(_plank.LosingTorque);
        }

        void ComputeTorque(float dt, out float torque, out float load)
        {
            torque = 0; load = 0;
            var center = PlankCenter(out var along);
            float half = _plankLength * 0.5f;
            var normal = new Vector2(-along.y, along.x);
            foreach (var w in _weights)
            {
                if (w == null || !w.Landed || w.Merging) continue;
                var d = w.Body.position - center;
                float lx = Vector2.Dot(d, along), ly = Vector2.Dot(d, normal);
                if (Mathf.Abs(lx) > half + w.Size * 0.5f || ly < -0.3f) continue; // tombé de la planche
                torque += WeightCatalog.TorqueWeight(w.Value, _t.plank) * lx / _t.plank.referenceHalfLength;
                load += w.Value;
            }
            // Le 128 qui s'envole retire son poids progressivement (balloonLiftSeconds) : la planche penche, sans claquer.
            for (int i = _phantoms.Count - 1; i >= 0; i--)
            {
                var p = _phantoms[i];
                float k = _t.plank.balloonLiftSeconds > 0 ? Mathf.Clamp01(p.Remaining / _t.plank.balloonLiftSeconds) : 0f;
                torque += WeightCatalog.TorqueWeight(p.Value, _t.plank) * p.LocalX / _t.plank.referenceHalfLength * k;
                load += p.Value * k;
                p.Remaining -= dt;
                if (p.Remaining <= 0) _phantoms.RemoveAt(i); else _phantoms[i] = p;
            }
        }

        void ProcessMerges()
        {
            if (_pendingMerges.Count == 0) return;
            var pairs = _pendingMerges.ToArray();
            _pendingMerges.Clear();
            foreach (var pair in pairs)
            {
                var a = pair.Key; var b = pair.Value;
                if (a == null || b == null || a.Merging || b.Merging || a.Value != b.Value || !a.Dropped || !b.Dropped) continue;
                Merge(a, b);
            }
        }

        void Merge(WeightBody a, WeightBody b)
        {
            a.Merging = b.Merging = true;
            var pa = a.Body.position; var pb = b.Body.position;
            var lower = pa.y <= pb.y ? pa : pb;
            var pos = Vector2.Lerp(lower, (pa + pb) * 0.5f, 0.35f);
            var vel = (a.Body.GetVelocity() + b.Body.GetVelocity()) * 0.25f;
            bool landed = a.Landed || b.Landed;
            int value = a.Value * 2;
            RemoveWeight(a);
            RemoveWeight(b);

            int points = _score.OnMerge(value, _runTime);
            Merged?.Invoke(value);
            var color = ArtCache.ToColor(WeightCatalog.Color(value));
            float rank = WeightCatalog.IndexOf(value) / 7f; // 0 pour un 2, 1 pour un 128
            _fx.Burst(pos, color, 8 + Mathf.RoundToInt(rank * 8), Mathf.Lerp(0.8f, 1.7f, rank));
            _fx.Shockwave(pos, color, WeightCatalog.Size(value, _t.weights));
            // Les points grossissent avec le poids créé, et prennent sa couleur à partir du 16.
            var pointsColor = value >= 16 ? Color.Lerp(color, Color.white, 0.35f) : Color.white;
            _fx.Text(pos + new Vector2(0, 0.9f), "+" + Loc.FormatScore(points), pointsColor, Mathf.Lerp(0.8f, 1.25f, rank));
            if (value >= 32) { _fx.Shake(0.12f + rank * 0.15f); ScreenFlash?.Invoke(0.08f + rank * 0.08f, color); }

            // Gros poids créé d'un coup (64) : la planche se stabilise un instant (un peu plus facile, pas imperdable).
            if (value >= _t.plank.bigMergeValue && !WeightCatalog.IsMax(value)) _plank.GiveSteady(_t.plank.bigMergeSteadySeconds);

            if (WeightCatalog.IsMax(value))
            {
                // Le 128 s'envole en montgolfière : gros bonus, mais le côté qui le portait s'allège d'un coup.
                var center = PlankCenter(out var along);
                if (landed) _phantoms.Add(new Phantom { Value = value, LocalX = Vector2.Dot(pos - center, along), Remaining = _t.plank.balloonLiftSeconds });
                // Souffle : le temps de rattraper l'équilibre avec le poids suivant (on ne perd pas sur un exploit).
                _plank.GiveCalm(_t.plank.balloonLiftSeconds + _t.plank.balloonGraceSeconds);
                int bonus = _score.OnBalloon();
                _fx.Balloon(pos);
                _fx.Confetti(pos, 30);
                _fx.Text(pos + new Vector2(0, 2.2f), Loc.Format("balloon", Loc.FormatScore(bonus)), Palette.Accent, 1.2f, 1.8f);
                _fx.Shake(0.5f);
                ScreenFlash?.Invoke(0.4f, new Color(1f, 0.9f, 0.5f));
                _mascot.Cheer();
                _audio.Play(Sfx.Lift);
                _haptics.Medium();
            }
            else
            {
                var w = SpawnWeight(value, pos, landed);
                w.Body.SetVelocity(vel);
                w.PopIn();
                _mascot.Happy();
                _audio.Play(Sfx.Pop, value);
                if (value >= 16) _haptics.Light();
            }

            if (_score.Chain >= 2)
            {
                // Combo : plus la chaîne est longue, plus le texte est gros, coloré et secoue l'écran.
                int chain = _score.Chain;
                var cc = ComboColors[Mathf.Min(chain - 2, ComboColors.Length - 1)];
                _fx.Text(pos + new Vector2(0, 1.6f), Loc.Format("combo", _score.Multiplier.ToString("0.#")), cc, Mathf.Min(1.9f, 1f + 0.2f * (chain - 2)), 1.3f);
                if (chain >= 3)
                {
                    _fx.Burst(pos + new Vector2(0, 0.4f), new Color(1f, 0.85f, 0.3f), 8 + chain * 2, 1.2f + 0.15f * chain);
                    _fx.Shockwave(pos, cc, 1.2f + 0.3f * chain);
                    _fx.Shake(0.12f + 0.06f * chain);
                }
                if (chain >= 4) ScreenFlash?.Invoke(Mathf.Min(0.35f, 0.12f + 0.05f * chain), cc);
                _audio.Play(Sfx.Chime, _score.Chain);
                Combo?.Invoke(chain);
            }
            ScoreChanged?.Invoke();
        }

        void RemoveWeight(WeightBody w)
        {
            _weights.Remove(w);
            if (w == null) return;
            // Destroy n'agit qu'en fin d'image : on sort tout de suite le poids de la physique,
            // sinon il gêne encore le poids fusionné qui apparaît à sa place.
            w.Body.simulated = false;
            foreach (var c in w.GetComponents<Collider2D>()) c.enabled = false;
            Destroy(w.gameObject);
        }

        static readonly Collider2D[] Overlaps = new Collider2D[16];
        static readonly ContactFilter2D NoFilter = new ContactFilter2D().NoFilter();

        /// <summary>Remonte une position (le long de la verticale de la planche) jusqu'à une place libre.</summary>
        Vector2 FreeSpot(Vector2 pos, float width, float halfHeight, float rotDeg)
        {
            var up = (Vector2)(Quaternion.Euler(0, 0, rotDeg) * Vector3.up);
            var box = new Vector2(width * 0.82f, halfHeight * 1.8f);
            for (int k = 0; k < 14; k++)
            {
                int n = Physics2D.OverlapBox(pos, box, rotDeg, NoFilter, Overlaps);
                bool blocked = false;
                for (int i = 0; i < n; i++)
                {
                    var c = Overlaps[i];
                    if (c == null || !c.enabled || c.isTrigger) continue;
                    var wb = c.GetComponent<WeightBody>();
                    if (wb != null && wb.Merging) continue;
                    blocked = true;
                    break;
                }
                if (!blocked) return pos;
                pos += up * 0.06f;
            }
            return pos;
        }

        /// <summary>Garde-fou : aucune pièce ne part à toute vitesse (évite les « explosions » de pile).</summary>
        void LimitSpeeds()
        {
            // Bien au-dessus de ce que la gravité produit (une chute complète atteint environ 17) : ne sert qu'aux vrais bugs.
            const float maxSpeed = 30f, maxSpin = 900f;
            foreach (var w in _weights)
            {
                if (w == null || !w.Body.simulated) continue;
                var v = w.Body.GetVelocity();
                if (v.sqrMagnitude > maxSpeed * maxSpeed) w.Body.SetVelocity(v.normalized * maxSpeed);
                if (Mathf.Abs(w.Body.angularVelocity) > maxSpin) w.Body.angularVelocity = Mathf.Sign(w.Body.angularVelocity) * maxSpin;
            }
        }

        void CheckLostWeights()
        {
            float floor = _mascot.PivotWorld.y - 5.5f;
            float hw = WorldLayout.HalfWidth(_cam) + 2f;
            for (int i = _weights.Count - 1; i >= 0; i--)
            {
                var w = _weights[i];
                if (w == null) { _weights.RemoveAt(i); continue; }
                var p = w.Body.position;
                if (p.y > floor && Mathf.Abs(p.x) < hw) continue;
                _weights.RemoveAt(i);
                if (IsRunning && !w.Merging)
                {
                    // Un poids perdu coûte des points, et un cœur s'il est lourd : chaque chute compte.
                    int penalty = _score.OnWeightLost(w.Value);
                    // Sur deux lignes (« Perdu ! » puis « -64 ») et assez loin du bord : le texte reste entier à l'écran,
                    // même quand il grossit en apparaissant.
                    float edge = WorldLayout.HalfWidth(_cam) - 1.9f;
                    var at = new Vector2(Mathf.Clamp(p.x, -edge, edge), _mascot.PivotWorld.y + 1.2f);
                    _fx.Text(at, Loc.Get("lost_weight") + (penalty > 0 ? "\n-" + Loc.FormatScore(penalty) : ""), Palette.Primary, 0.9f, 1.3f);
                    _fx.Shake(0.35f);
                    _audio.Play(Sfx.Lost, 1, 0.9f);
                    _haptics.Medium();
                    _mascot.Absorb(8);
                    WeightLost?.Invoke(w.Value);
                    ScoreChanged?.Invoke();
                }
                Destroy(w.gameObject);
            }
        }

        void BeginLose()
        {
            LoseStarted?.Invoke();
            _lossSnapshot = Capture();
            Current = State.Losing;
            _loseTimer = 2.2f; // le temps de voir la mascotte tomber
            if (_held != null) { Destroy(_held.gameObject); _held = null; }
            _dragging = false;
            _mascot.LetGo(Mathf.Abs(_plank.Angle) > 1f ? Mathf.Sign(_plank.Angle) : 1f);
            _plankBody.bodyType = RigidbodyType2D.Dynamic;
            _plankBody.gravityScale = 1.2f;
            _plankBody.mass = 4f;
            _plankBody.angularVelocity = -Mathf.Sign(_plank.Angle) * 90f;
            _fx.Shake(1f);
            _audio.Play(Sfx.Fall);
            _haptics.Heavy();
            // La chute au ralenti avec les « derniers mots » de Basile : le moment que l'on filme et dont on parle.
            // La partie est déjà perdue : le ralenti ne change rien au résultat.
            if (!_fx.ReducedMotion) _slowMo = 1.4f;
            _fx.Text(_mascot.PivotWorld + new Vector2(0, 2.8f), Loc.Get("last_" + UnityEngine.Random.Range(0, LastWords)), Color.white, 1.15f, 2f);
            _photoTimer = 0.3f;
        }

        const int LastWords = 8;

        // ------------------------------------------------------------------ Duel

        /// <summary>
        /// Duel gagné : l'adversaire a lâché et le temps du dernier poids est écoulé. La partie s'arrête sans chute :
        /// Basile lève les bras sous les confettis.
        /// </summary>
        public bool FinishDuelRun()
        {
            if (!IsRunning && Current != State.Arrival) return false;
            if (Current == State.Arrival) { _mascot.Walking = false; _mascot.HandsOverride = false; }
            if (_held != null) { Destroy(_held.gameObject); _held = null; }
            _dragging = false;
            _guide.enabled = false;
            Current = State.Over;
            _mascot.Cheer();
            _fx.Confetti(_mascot.PivotWorld + new Vector2(0, 3.5f), 60);
            _audio.Play(Sfx.Lift);
            _haptics.Medium();
            RunEnded?.Invoke(BuildResult());
            return true;
        }

        /// <summary>Photo de la partie pour l'adversaire (positions par rapport au pivot : indépendantes de la taille d'écran).</summary>
        public void CaptureDuel(DuelSnapshot s)
        {
            s.TimeMs = (uint)Mathf.Max(0f, _runTime * 1000f);
            s.Score = Score;
            s.Angle = _plank != null ? _plank.Angle : 0f;
            s.Danger = _plank != null ? _plank.Danger : 0f;
            var pivot = _mascot.PivotWorld;
            s.HeldValue = _held != null ? _held.Value : 0;
            s.HeldX = _held != null ? _held.transform.position.x - pivot.x : 0f;
            s.Wind = WindDirection * Mathf.Max(WindEnvelope, WindPhase == WindPhase.Warning ? 0.15f : 0f);
            s.Weights.Clear();
            // Du plus bas au plus haut : si la pile dépasse la limite d'un paquet, ce sont les plus hauts qui manquent.
            foreach (var w in _weights)
            {
                if (w == null || w.Merging) continue;
                if (s.Weights.Count >= DuelProtocol.MaxWeights) break;
                var p = (Vector2)w.transform.position - pivot;
                s.Weights.Add(new DuelWeight { Id = w.NetId, Value = w.Value, X = p.x, Y = p.y, Rotation = w.transform.eulerAngles.z });
            }
        }

        /// <summary>Photo du monde (caméra du jeu seule, sans l'interface) pour la carte de score. Une image, une fois par chute.</summary>
        void TakePhoto()
        {
            try
            {
                // Même cadrage que l'écran (aussi en paysage, où la caméra n'occupe qu'une colonne).
                int w = 540, h = Mathf.Clamp(Mathf.RoundToInt(w / Mathf.Max(0.3f, _cam.aspect)), 720, 1240);
                if (_photo != null && (_photo.width != w || _photo.height != h)) { _photo.Release(); Destroy(_photo); _photo = null; }
                if (_photo == null) _photo = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { name = "PhotoChute" };
                var previous = _cam.targetTexture;
                var rect = _cam.rect;
                _cam.rect = new Rect(0, 0, 1, 1);
                _cam.targetTexture = _photo;
                _cam.Render();
                _cam.targetTexture = previous;
                _cam.rect = rect;
                _cam.ResetAspect();
                PhotoFocusY = Mathf.Clamp01(_cam.WorldToViewportPoint(_mascot.PivotWorld).y + 0.12f);
                HasPhoto = true;
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Photo de la chute : " + e.Message); HasPhoto = false; }
        }

        void UpdateBackground(float dt)
        {
            // L'ambiance avance dans la journée avec le score (matin, midi, doré, couchant, nuit).
            int theme = Difficulty.BackgroundIndex(Score, Theme.All.Length);
            if (theme != _theme)
            {
                _theme = theme;
                _backdrop.SetTheme(theme, Current == State.Idle);
                // Le décor change en fondu, sans message : le changement se voit, il n'a pas besoin d'être annoncé.
            }
            if (IsRunning && !_recordAnnounced && BestToBeat > 0 && Score > BestToBeat)
            {
                _recordAnnounced = true;
                _fx.Confetti(new Vector2(0, _cam.orthographicSize - 3f), 50);
                _fx.Text(new Vector2(0, _cam.orthographicSize - 5.4f), Loc.Get("new_best"), Palette.Accent, 1.3f, 1.8f);
                _audio.Play(Sfx.Lift, 1, 0.7f);
                _haptics.Medium();
                NewRecord?.Invoke();
            }
            // Paliers : 1 000, 2 000, 3 000… un grand chiffre doré et des confettis (sauf pendant le record, qui prime).
            if (IsRunning && Score >= _nextMilestone)
            {
                int m = Score / 1000 * 1000;
                _nextMilestone = m + 1000;
                if (!(_recordAnnounced && Score > BestToBeat && Score - BestToBeat < 300))
                {
                    _fx.Text(new Vector2(0, _cam.orthographicSize - 6.6f), Loc.Format("milestone", Loc.FormatScore(m)), Palette.Accent, 1.5f, 1.4f);
                    _fx.Confetti(new Vector2(0, _cam.orthographicSize - 4f), 24);
                    _audio.Play(Sfx.Chime, Mathf.Clamp(m / 1000 + 2, 3, 8), 0.7f);
                }
            }
            if (IsRunning && !_goalAnnounced && Goal > 0 && Score >= Goal)
            {
                _goalAnnounced = true; GoalDone = true;
                // Pas d'annonce plein écran si le record tombe au même moment : le record prime.
                if (!(_recordAnnounced && Score > BestToBeat && BestToBeat >= Goal))
                {
                    _fx.Text(new Vector2(0, _cam.orthographicSize - 5.4f), Loc.Get("goal_reached"), Palette.Success, 1.1f, 1.4f);
                    _audio.Play(Sfx.Chime, 1, 0.8f);
                    _haptics.Light();
                }
                GoalReachedEvent?.Invoke();
            }
        }

        void UpdateSlowMotion()
        {
            if (_slowMo <= 0f) return;
            _slowMo -= Time.unscaledDeltaTime;
            Time.timeScale = _slowMo > 0f ? Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(_slowMo / 0.3f)) : 1f;
        }

        void UpdateSweat(float dt)
        {
            if (!IsRunning) return;
            var mood = _mascot.CurrentMood;
            if (mood != Mood.Strained && mood != Mood.LastEffort) return;
            _sweatTimer -= dt;
            if (_sweatTimer > 0) return;
            _sweatTimer = mood == Mood.LastEffort ? 0.25f : 0.6f;
            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            _fx.SweatDrop(_mascot.TempleWorld(side), side);
        }
    }

    /// <summary>Vitesse d'un Rigidbody2D, compatible Unity 2022 (velocity) et Unity 6 (linearVelocity).</summary>
    public static class Rigidbody2DCompat
    {
        public static Vector2 GetVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
