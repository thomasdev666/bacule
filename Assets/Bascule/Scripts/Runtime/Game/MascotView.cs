using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule
{
    /// <summary>
    /// La mascotte articulée qui porte la planche. Coudes et genoux plient par cinématique inverse :
    /// sous la charge elle s'accroupit, les bras suivent la planche, les pieds glissent quand ça force,
    /// elle saute quand le 128 s'envole et lâche à la défaite. Origine du transform = entre les pieds, au sol.
    /// </summary>
    public sealed class MascotView : MonoBehaviour
    {
        public const float PivotHeight = MascotRig.PivotHeight;

        ArtCache _art;
        SpriteRenderer _shadow, _thighL, _thighR, _shinL, _shinR, _shoeL, _shoeR, _shorts, _torso;
        SpriteRenderer _flush;
        SpriteRenderer _headBack, _face, _headFront, _upperL, _upperR, _foreL, _foreR, _handL, _handR, _cushion;
        HeadDef _head;
        string _skin;

        float _time, _squash, _squashVel, _jump, _happyTimer, _cheerTimer, _defeatBlend;
        Mood _mood = Mood.Relaxed;
        Expression _shownExpression = (Expression)(-1);
        bool _defeated;
        public bool ReducedMotion;

        /// <summary>Effort de la partie (0..1) : la tête rougit doucement à mesure que le score et la charge montent.</summary>
        public float Effort;

        // Chute à la défaite.
        float _fallAngle, _fallVel;
        int _fallDir = 1;
        bool _fallen;
        /// <summary>La mascotte vient de toucher le sol (poussière, secousse).</summary>
        public event System.Action<Vector2> HitGround;

        // Arrivée au lancement : marche, mains posées où on veut (sur la planche au sol), accroupissement supplémentaire.
        public bool Walking;
        public float ExtraSquash;
        public bool HandsOverride;
        public Vector2 HandLeftWorld, HandRightWorld;
        public bool CushionVisible = true;

        public Mood CurrentMood => _mood;

        public static MascotView Create(Transform parent, ArtCache art)
        {
            var go = new GameObject("Mascotte");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<MascotView>();
            m._art = art;
            m.Build();
            return m;
        }

        SpriteRenderer R(string name, int order) => SpriteUtil.Make(name, transform, null, Color.white, order);

        void Build()
        {
            _shadow = R("Ombre", Layers.Shadow);
            _shadow.sprite = _art.SoftDisc;
            _shadow.color = new Color(0.12f, 0.08f, 0.25f, 0.35f);
            _thighL = R("CuisseG", 1); _thighR = R("CuisseD", 1);
            _shinL = R("JambeG", 2); _shinR = R("JambeD", 2);
            _shoeL = R("BasketG", 3); _shoeR = R("BasketD", 3);
            _shorts = R("Short", 4);
            _torso = R("Torse", 5);
            _headBack = R("TeteArriere", 6);
            _flush = R("Rougeur", 7);
            _flush.sprite = _art.Flush;
            _flush.color = new Color(1, 1, 1, 0);
            _face = R("Visage", 8);
            _headFront = R("TeteAvant", 9);
            _upperL = R("BrasG", 10); _upperR = R("BrasD", 10);
            _foreL = R("AvantBrasG", 11); _foreR = R("AvantBrasD", 11);
            _cushion = R("Serviette", Layers.Plank - 1);
            _handL = R("MainG", Layers.Hands); _handR = R("MainD", Layers.Hands);
            _upperL.flipY = true;
            _shoeL.flipX = true;
            _handL.flipX = true;
        }

        public void SetHead(HeadDef head)
        {
            _head = head;
            _headBack.sprite = _art.HeadBack(head);
            _headFront.sprite = _art.HeadFront(head);
            if (_skin != head.BodyKey)
            {
                var p = _art.UseMascot(head.BodyKey, _skin); // peau + tenue de cette tête
                _skin = head.BodyKey;
                _thighL.sprite = _thighR.sprite = p["thigh"];
                _shinL.sprite = _shinR.sprite = p["shin"];
                _shoeL.sprite = _shoeR.sprite = p["shoe"];
                _shorts.sprite = p["shorts"];
                _torso.sprite = p["torso"];
                _upperL.sprite = _upperR.sprite = p["upperArm"];
                _foreL.sprite = _foreR.sprite = p["forearm"];
                _handL.sprite = _handR.sprite = p["hand"];
                _cushion.sprite = p["cushion"];
            }
            _shownExpression = (Expression)(-1);
            ApplyFace();
        }

        /// <summary>Position monde du point d'appui de la planche (dessous de la planche).</summary>
        public Vector2 PivotWorld => transform.TransformPoint(new Vector3(0, PivotHeight - _squash + _jump, 0));

        public void ResetPose()
        {
            _defeated = false;
            _fallen = false;
            _fallAngle = _fallVel = 0;
            transform.localRotation = Quaternion.identity;
            Walking = false;
            HandsOverride = false;
            ExtraSquash = 0;
            CushionVisible = true;
            _defeatBlend = 0;
            _happyTimer = _cheerTimer = 0;
            _squashVel = 0;
            _mood = Mood.Relaxed;
            ApplyFace();
        }

        /// <summary>Un poids vient d'atterrir : la mascotte encaisse (les genoux plient).</summary>
        public void Absorb(int value)
        {
            if (ReducedMotion) return;
            _squashVel -= Mathf.Clamp(0.3f + value * 0.03f, 0.3f, 1.3f);
        }

        public void Happy() { _happyTimer = 0.7f; }
        public void Cheer() { _cheerTimer = 1.1f; }
        /// <summary>Défaite : elle lâche la planche et tombe du côté où ça penchait (direction = signe de l'angle).</summary>
        public void LetGo(float direction = 1f)
        {
            _defeated = true;
            _fallDir = direction >= 0 ? 1 : -1;
            _fallVel = 0;
        }

        /// <summary>Points d'émission de sueur (tempes), en monde.</summary>
        public Vector2 TempleWorld(int side) => transform.TransformPoint(new Vector3(side * 0.62f, MascotRig.HeadY + 0.25f - _squash + _jump, 0));

        /// <param name="plankAngle">Angle de la planche (degrés, positif = droite en bas).</param>
        /// <param name="danger">Danger 0..1+ (PlankModel.Danger).</param>
        /// <param name="loadRatio">Charge portée 0..1.</param>
        public void Tick(float dt, float plankAngle, float danger, float loadRatio, MascotTuning tuning)
        {
            if (_head == null) return;
            _time += dt;
            _happyTimer -= dt;
            _cheerTimer -= dt;

            if (_defeated) _mood = Mood.Defeated;
            else if (_cheerTimer > 0) _mood = Mood.Cheering;
            else if (_happyTimer > 0 && danger < tuning.strainedRatio) _mood = Mood.Happy;
            else _mood = MascotRules.MoodFor(danger, tuning);
            ApplyFace();

            float motion = ReducedMotion ? 0.3f : 1f;

            // Accroupissement : charge + respiration + ressort des impacts. Plus c'est lourd, plus c'est lent.
            float breathe = Mathf.Sin(_time * Mathf.Lerp(2.2f, 1.2f, loadRatio)) * 0.03f * motion;
            float target = 0.06f + ExtraSquash + 0.24f * loadRatio + breathe + (_mood == Mood.LastEffort ? 0.08f : 0f) + (_mood == Mood.Strained ? 0.04f : 0f);
            float stiffness = Mathf.Lerp(90f, 45f, loadRatio);
            _squashVel += ((target - _squash) * stiffness - _squashVel * 9f) * dt;
            _squash = Mathf.Clamp(_squash + _squashVel * dt, -0.12f, 0.42f);

            // Petit saut quand le 128 s'envole.
            _jump = _cheerTimer > 0.5f ? Mathf.Sin((1.1f - _cheerTimer) / 0.6f * Mathf.PI) * 0.25f * motion : 0f;

            float tremble = MascotRules.Tremble(_mood) * motion;
            float shakeX = tremble * 0.05f * (Mathf.PerlinNoise(_time * 18f, 0.3f) - 0.5f) * 2f;
            float shakeY = tremble * 0.03f * (Mathf.PerlinNoise(0.7f, _time * 21f) - 0.5f) * 2f;
            float lift = -_squash + _jump;

            // Jambes : hanches → genoux (vers l'extérieur) → chevilles. Les pieds glissent quand ça force.
            float slip = (_mood == Mood.Strained || _mood == Mood.LastEffort ? 1f : 0f) * Mathf.Sign(plankAngle) * motion;
            float walkBob = Walking ? Mathf.Abs(Mathf.Sin(_time * 9f)) * 0.08f : 0f;
            lift += walkBob;
            for (int side = -1; side <= 1; side += 2)
            {
                float slide = slip * (0.06f + 0.05f * Mathf.Sin(_time * 9f + (side > 0 ? 1.7f : 0f)));
                var ankle = new Vector2(side * MascotRig.AnkleX + slide, MascotRig.AnkleY + _jump);
                if (Walking)
                {
                    // Marche : chaque pied avance puis se lève à tour de rôle.
                    float ph = _time * 9f + (side > 0 ? Mathf.PI : 0f);
                    ankle += new Vector2(Mathf.Cos(ph) * 0.22f, Mathf.Max(0f, Mathf.Sin(ph)) * 0.28f);
                }
                var hip = new Vector2(side * MascotRig.HipX + shakeX, MascotRig.HipY + lift);
                MascotRig.TwoBone(hip.x, hip.y, ankle.x, ankle.y, MascotRig.Thigh, MascotRig.Shin, side, out float kx, out float ky);
                var knee = new Vector2(kx, ky);
                Bone(side < 0 ? _thighL : _thighR, hip, knee);
                Bone(side < 0 ? _shinL : _shinR, knee, ankle);
                var shoe = side < 0 ? _shoeL : _shoeR;
                shoe.transform.localPosition = ankle;
            }

            var bodyOffset = new Vector2(shakeX, lift + shakeY);
            _shorts.transform.localPosition = new Vector2(0, MascotRig.ShortsY) + bodyOffset * 0.8f;
            _torso.transform.localPosition = new Vector2(0, MascotRig.TorsoY) + bodyOffset;

            // Tête : s'incline un peu à l'opposé de la planche (elle résiste).
            float headTilt = Mathf.Clamp(plankAngle * 0.15f, -6f, 6f) * motion;
            if (_mood == Mood.Defeated) headTilt = Mathf.Sin(_time * 2f) * 4f;
            var headPos = new Vector2(0, MascotRig.HeadY) + bodyOffset;
            var rot = Quaternion.Euler(0, 0, headTilt);
            // Pas de tableau créé à chaque image.
            _headBack.transform.localPosition = headPos; _headBack.transform.localRotation = rot;
            _face.transform.localPosition = headPos; _face.transform.localRotation = rot;
            _headFront.transform.localPosition = headPos; _headFront.transform.localRotation = rot;

            // Serviette et bras : les mains tiennent la planche (ou retombent à la défaite).
            _defeatBlend = Mathf.MoveTowards(_defeatBlend, _defeated ? 1f : 0f, dt * 3f);
            float rad = -plankAngle * Mathf.Deg2Rad;
            var along = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var normal = new Vector2(-along.y, along.x);
            var pivot = new Vector2(0, PivotHeight + lift);
            _cushion.transform.localPosition = pivot - normal * 0.07f;
            _cushion.transform.localRotation = Quaternion.Euler(0, 0, -plankAngle);
            _cushion.enabled = _defeatBlend < 0.5f && CushionVisible;

            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector2(side * MascotRig.ShoulderX, MascotRig.ShoulderY) + bodyOffset;
                var onPlank = pivot + along * (side * MascotRig.HandReach) - normal * MascotRig.HandBelow;
                var rest = shoulder + new Vector2(side * 0.45f, -1.9f);
                if (Walking) rest += new Vector2(0, Mathf.Sin(_time * 9f + (side > 0 ? 0f : Mathf.PI)) * 0.25f);
                var handTarget = HandsOverride ? (Vector2)transform.InverseTransformPoint(side < 0 ? HandLeftWorld : HandRightWorld) : onPlank;
                var hand = Vector2.Lerp(handTarget, rest, _defeatBlend);
                MascotRig.TwoBone(shoulder.x, shoulder.y, hand.x, hand.y, MascotRig.UpperArm, MascotRig.Forearm, -side, out float ex, out float ey);
                var elbow = new Vector2(ex, ey);
                Bone(side < 0 ? _upperL : _upperR, shoulder, elbow);
                var fore = side < 0 ? _foreL : _foreR;
                Bone(fore, elbow, hand);
                // Planche très penchée : bras tendu à fond, l'avant-bras s'étire un peu pour garder la main sur la planche.
                float stretch = Mathf.Max(1f, Vector2.Distance(elbow, hand) / MascotRig.Forearm);
                fore.transform.localScale = new Vector3(Mathf.Min(stretch, 1.25f), 1f, 1f);
                var h = side < 0 ? _handL : _handR;
                h.transform.localPosition = hand;
                h.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-plankAngle, side * -70f, _defeatBlend));
            }

            // Ombre au sol, plus large quand elle s'accroupit.
            _shadow.transform.localPosition = new Vector3(0, 0.05f, 0);
            _shadow.transform.localScale = new Vector3(2.5f + _squash * 0.8f, 0.45f, 1);
            _shadow.enabled = !_fallen;

            // Rougeur d'effort (douce : la courbe est fixée par GameSession).
            float flush = Mathf.Clamp01(Effort) * 0.6f + (_mood == Mood.LastEffort ? 0.12f : 0f);
            _flush.color = new Color(1, 1, 1, Mathf.MoveTowards(_flush.color.a, flush, dt * 0.8f));
            _flush.transform.localPosition = headPos;
            _flush.transform.localRotation = rot;

            // Chute : elle bascule autour de ses pieds, rebondit un peu et reste au sol.
            if (_defeated && !ReducedMotion)
            {
                const float ground = 84f;
                if (!_fallen)
                {
                    _fallVel += dt * 520f;
                    _fallAngle += _fallVel * dt;
                    if (_fallAngle >= ground)
                    {
                        _fallAngle = ground;
                        _fallVel = -_fallVel * 0.25f;
                        if (Mathf.Abs(_fallVel) < 40f) _fallen = true;
                        HitGround?.Invoke(transform.TransformPoint(new Vector3(_fallDir * 3.2f, 0.3f, 0)));
                    }
                }
                transform.localRotation = Quaternion.Euler(0, 0, -_fallDir * _fallAngle);
            }
        }

        static void Bone(SpriteRenderer sr, Vector2 from, Vector2 to)
        {
            sr.transform.localPosition = from;
            var d = to - from;
            sr.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void ApplyFace()
        {
            if (_head == null) return;
            var e = MascotRules.ExpressionFor(_mood, _head.Expression);
            if (e == _shownExpression) return;
            _shownExpression = e;
            _face.sprite = _art.Face(e);
        }
    }
}
