using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Le personnage complet dans l'interface, avec les mêmes images et le même squelette que dans la partie
    /// (Core/MascotRig) : jambes, tenue, tête en trois calques, bras levés qui tiennent la planche et ses rebords.
    /// Pose() plie les genoux (atterrissage, respiration) et penche un peu la planche.
    /// </summary>
    public sealed class MascotFigure
    {
        public readonly RectTransform Root;
        readonly float _u;
        readonly float _thickness;
        readonly Image _shadow, _shorts, _torso, _back, _face, _front, _cushion, _plank;
        readonly Image[] _thigh = new Image[2], _shin = new Image[2], _shoe = new Image[2], _upper = new Image[2], _fore = new Image[2], _hand = new Image[2];

        /// <param name="unitPixels">Pixels d'interface par unité monde (le personnage et sa planche font environ 5,4 unités de haut).</param>
        public MascotFigure(Transform parent, ArtCache art, HeadDef head, Expression expression, float unitPixels, float plankThickness)
        {
            Root = Ui.Rect("Personnage", parent);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = Vector2.zero; // origine : entre les pieds, au sol
            _u = unitPixels;
            _thickness = plankThickness;
            var parts = art.MascotParts(head.BodyKey);
            Sprite P(string k) => parts.TryGetValue(k, out var s) ? s : null;

            // Même ordre de dessin que dans la partie (MascotView) : du fond vers l'avant.
            _shadow = Ui.Image(Root, art.SoftDisc, new Color(0.12f, 0.08f, 0.25f, 0.35f), "Ombre");
            for (int i = 0; i < 2; i++) _thigh[i] = Part(P("thigh"), "Cuisse");
            for (int i = 0; i < 2; i++) _shin[i] = Part(P("shin"), "Jambe");
            for (int i = 0; i < 2; i++) _shoe[i] = Part(P("shoe"), "Chaussure");
            _shorts = Part(P("shorts"), "Bas");
            _torso = Part(P("torso"), "Torse");
            _back = Part(art.HeadBack(head), "TeteArriere");
            _face = Part(art.Face(expression), "Visage");
            _front = Part(art.HeadFront(head), "TeteAvant");
            for (int i = 0; i < 2; i++) _upper[i] = Part(P("upperArm"), "Bras");
            for (int i = 0; i < 2; i++) _fore[i] = Part(P("forearm"), "AvantBras");
            _cushion = Part(P("cushion"), "Serviette");
            // La planche de l'écran de démarrage : bois et rebords dessinés ensemble, déjà dans l'ArtPack.
            _plank = Part(art.Scene(ArtCatalog.BootPlankKey, ArtCatalog.BootPlank, ArtCatalog.BootPlankPpu), "Planche");
            if (_plank.sprite != null)
            {
                // Pivot au centre du bois (la planche est en bas de l'image, les rebords au-dessus).
                var r = _plank.sprite.rect;
                _plank.rectTransform.pivot = new Vector2(0.5f, (_thickness * ArtCatalog.BootPlankPpu + 12f) * 0.5f / r.height);
            }
            for (int i = 0; i < 2; i++) _hand[i] = Part(P("hand"), "Main");
            // Côté gauche : images en miroir, comme les SpriteRenderer du jeu (flipY du bras, flipX du pied et de la main).
            _upper[0].rectTransform.localScale = new Vector3(1f, -1f, 1f);
            _shoe[0].rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            _hand[0].rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            Pose(0.06f, 0f);
        }

        Image Part(Sprite s, string name)
        {
            var img = Ui.Image(Root, s, Color.white, name);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            if (s == null) { img.enabled = false; return img; }
            rt.pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
            rt.sizeDelta = s.rect.size / s.pixelsPerUnit * _u;
            return img;
        }

        void At(Image img, Vector2 world, float angle)
        {
            var rt = img.rectTransform;
            rt.anchoredPosition = world * _u;
            rt.localEulerAngles = new Vector3(0, 0, angle);
        }

        void Bone(Image img, Vector2 from, Vector2 to)
        {
            var d = to - from;
            At(img, from, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        /// <param name="squash">Genoux pliés (unités ; 0,06 au repos).</param>
        /// <param name="plankAngle">Angle de la planche en degrés (positif = droite en bas).</param>
        public void Pose(float squash, float plankAngle)
        {
            float lift = -squash;
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                var hip = new Vector2(side * MascotRig.HipX, MascotRig.HipY + lift);
                var ankle = new Vector2(side * MascotRig.AnkleX, MascotRig.AnkleY);
                MascotRig.TwoBone(hip.x, hip.y, ankle.x, ankle.y, MascotRig.Thigh, MascotRig.Shin, side, out float kx, out float ky);
                var knee = new Vector2(kx, ky);
                Bone(_thigh[i], hip, knee);
                Bone(_shin[i], knee, ankle);
                At(_shoe[i], ankle, 0f);
            }
            At(_shorts, new Vector2(0, MascotRig.ShortsY + lift * 0.8f), 0f);
            At(_torso, new Vector2(0, MascotRig.TorsoY + lift), 0f);
            float tilt = Mathf.Clamp(plankAngle * 0.15f, -6f, 6f);
            var headPos = new Vector2(0, MascotRig.HeadY + lift);
            At(_back, headPos, tilt); At(_face, headPos, tilt); At(_front, headPos, tilt);

            float rad = -plankAngle * Mathf.Deg2Rad;
            var along = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            var normal = new Vector2(-along.y, along.x);
            var pivot = new Vector2(0, MascotRig.PivotHeight + lift);
            At(_cushion, pivot - normal * 0.07f, -plankAngle);
            At(_plank, pivot + normal * (_thickness * 0.5f), -plankAngle);
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                var shoulder = new Vector2(side * MascotRig.ShoulderX, MascotRig.ShoulderY + lift);
                var hand = pivot + along * (side * MascotRig.HandReach) - normal * MascotRig.HandBelow;
                MascotRig.TwoBone(shoulder.x, shoulder.y, hand.x, hand.y, MascotRig.UpperArm, MascotRig.Forearm, -side, out float ex, out float ey);
                var elbow = new Vector2(ex, ey);
                Bone(_upper[i], shoulder, elbow);
                Bone(_fore[i], elbow, hand);
                At(_hand[i], hand, -plankAngle);
            }
            var sh = _shadow.rectTransform;
            sh.anchoredPosition = new Vector2(0, 0.05f * _u);
            sh.sizeDelta = new Vector2((2.5f + squash * 0.8f) * _u, 0.45f * _u);
        }
    }

    /// <summary>
    /// Une nouvelle tête apparaît en grand (code promo) : le personnage complet, avec sa tenue et sa planche, tombe du
    /// ciel sur un fond de rayons qui tournent, atterrit en pliant les genoux dans une pluie de confettis aux couleurs
    /// du partenaire, puis son nom s'affiche. « La porter » l'équipe tout de suite ; « Plus tard » ferme.
    /// </summary>
    public sealed class HeadRevealScreen : UiScreen
    {
        const float Unit = 150f, FallTime = 0.42f, FeetY = -560f;
        RectTransform _stage, _kicker, _title, _sub, _buttons;
        Text _kickerText, _titleText, _subText;
        Image _flash, _glow;
        MascotFigure _figure;
        Action _onWear;
        float _t = -1f, _squash, _squashVel, _clock;
        bool _landed;
        readonly List<(RectTransform rt, Image img, Vector2 v, float spin, float age)> _confetti = new List<(RectTransform, Image, Vector2, float, float)>();

        static readonly Color[] Party =
        {
            ArtCache.Hex("#00A651"), ArtCache.Hex("#FFD60A"), ArtCache.Hex("#2F6FE0"), Color.white, ArtCache.Hex("#FF8FB8"),
        };

        protected override void Build()
        {
            // Fond chaud, comme la carte de présentation : jaune pâle, orangé vers le bas, rayons blancs qui tournent.
            var bg = Ui.Image(Root, null, ArtCache.Hex("#FFE7A8"), "Fond");
            Ui.Stretch(bg.rectTransform);
            bg.transform.SetAsFirstSibling();
            var low = Ui.Image(Root, Art.SoftDisc, new Color(1f, 0.6f, 0.18f, 0.6f), "Couchant");
            Ui.Place(low.rectTransform, 0.5f, 0, 0, -200, 2400, 1700);
            low.transform.SetSiblingIndex(1);
            var rays = Ui.Image(Root, Art.Rays, new Color(1f, 1f, 1f, 0.42f), "Rayons");
            Ui.Place(rays.rectTransform, 0.5f, 0.5f, 0, -60, 2800, 2800);
            rays.transform.SetSiblingIndex(2);
            rays.gameObject.AddComponent<Spin>().Speed = 9f;
            _glow = Ui.Image(Root, Art.SoftDisc, new Color(1f, 1f, 1f, 0.75f), "Halo");
            Ui.Place(_glow.rectTransform, 0.5f, 0.5f, 0, -120, 1150, 1150);
            _glow.transform.SetSiblingIndex(3);

            _stage = Ui.Rect("Scene", Safe);
            Ui.Place(_stage, 0.5f, 0.5f, 0, FeetY, 10, 10);

            // Bandeau « Code accepté ! », nom de la tête en grand, ligne du partenaire.
            var kick = Ui.Image(Safe, Art.Pill, ArtCache.Hex("#00A651"), "Bandeau", true);
            _kicker = kick.rectTransform;
            Ui.Place(_kicker, 0.5f, 1, 0, -130, 560, 96);
            _kickerText = Ui.Title(kick.transform, "", 46);
            Ui.Stretch(_kickerText.rectTransform, 24, 2, 24, 6);
            _titleText = Ui.Title(Safe, "", 128, ArtCache.Hex("#00A651"), "Nom");
            _title = _titleText.rectTransform;
            Ui.Place(_title, 0.5f, 1, 0, -290, 1000, 170);
            _subText = Ui.Title(Safe, "", 48, Color.white, "Partenaire");
            _sub = _subText.rectTransform;
            Ui.Place(_sub, 0.5f, 1, 0, -405, 980, 70);

            var buttons = Ui.Rect("Boutons", Safe);
            _buttons = buttons;
            Ui.Place(_buttons, 0.5f, 0, 0, 0, 1080, 380, 0.5f, 0);
            var wear = Ui.Button(buttons, Loc.Get("promo_wear"), Palette.Play, () => { var a = _onWear; Close(); a?.Invoke(); }, Icon.Face, 60, "LaPorter");
            Ui.Place((RectTransform)wear.transform, 0.5f, 0, 0, 272, 760, 166);
            wear.gameObject.AddComponent<Pulse>().Amount = 0.03f;
            var later = Ui.Button(buttons, Loc.Get("later"), Palette.Muted, Close, null, 46, "PlusTard");
            Ui.Place((RectTransform)later.transform, 0.5f, 0, 0, 88, 520, 116); // 43 px d'écart avec « La porter »

            _flash = Ui.Image(Root, null, new Color(1, 1, 1, 0), "Eclair");
            Ui.Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;
        }

        /// <param name="kicker">Petit bandeau du haut (« Code accepté ! »).</param>
        /// <param name="subtitle">Ligne sous le nom (« Offerte par le BDE Samb'bach »).</param>
        public void Show(HeadDef head, string kicker, string subtitle, Action onWear)
        {
            Show();
            // Le personnage est reconstruit à chaque fois : ses images peuvent avoir été libérées entre-temps.
            if (_figure != null) UnityEngine.Object.Destroy(_figure.Root.gameObject);
            _figure = new MascotFigure(_stage, Art, head, Expression.Happy, Unit, App.Tuning.plank.thickness);
            Ui.Isolate(_figure.Root.gameObject);
            _kickerText.text = kicker;
            _titleText.text = head.Name(Loc.Language);
            _subText.text = subtitle ?? "";
            _onWear = onWear;
            _t = 0f;
            _landed = false;
            _squash = 0.06f;
            _squashVel = 0f;
            foreach (var c in _confetti) if (c.rt != null) UnityEngine.Object.Destroy(c.rt.gameObject);
            _confetti.Clear();
            _flash.transform.SetAsLastSibling();
            App.PlaySfx(Sfx.Lift, 0, 0.8f);
            Tick(0f);
        }

        void Close()
        {
            _t = -1f;
            Hide();
        }

        public void Back() => Close();

        public override void Tick(float dt)
        {
            if (_t < 0f || _figure == null) return;
            _t += dt;
            _clock += dt;
            bool reduced = App.Profile.settings.reducedMotion;

            // Éclair blanc du début.
            _flash.color = new Color(1, 1, 1, reduced ? 0f : Mathf.Clamp01(0.8f - _t * 3f));

            // Chute (elle accélère), puis atterrissage : genoux pliés, rebond, confettis, son.
            float drop = 0f;
            if (!_landed)
            {
                float k = Mathf.Clamp01(_t / FallTime);
                drop = reduced ? 0f : (1f - k * k) * 1300f;
                if (k >= 1f)
                {
                    _landed = true;
                    _squashVel = reduced ? 0f : -3.4f;
                    Burst(reduced ? 0 : 70);
                    App.PlaySfx(Sfx.Thud, 0, 1f);
                    App.PlaySfx(Sfx.Chime, 10, 0.9f);
                    App.HapticMedium();
                }
            }
            float target = 0.06f + (reduced ? 0f : Mathf.Sin(_clock * 2.2f) * 0.025f);
            _squashVel += ((target - _squash) * 70f - _squashVel * 8f) * dt;
            _squash = Mathf.Clamp(_squash + _squashVel * dt, -0.15f, 0.45f);
            _figure.Root.anchoredPosition = new Vector2(0, drop);
            _figure.Pose(_squash, reduced ? 0f : Mathf.Sin(_clock * 1.6f) * 1.5f);
            _glow.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(_clock * 3f));

            // Puis le bandeau, le nom, le partenaire et les boutons, l'un après l'autre, avec un petit rebond.
            Pop(_kicker, _t - 0.2f, reduced);
            Pop(_title, _t - 0.5f, reduced);
            Pop(_sub, _t - 0.7f, reduced);
            Pop(_buttons, _t - 0.95f, reduced);
            TickConfetti(dt);
        }

        static void Pop(RectTransform rt, float t, bool reduced)
        {
            bool on = t > 0f;
            if (rt.gameObject.activeSelf != on) rt.gameObject.SetActive(on);
            if (!on) return;
            float k = Mathf.Clamp01(t / 0.35f);
            const float c1 = 1.9f, c3 = c1 + 1f;
            float e = reduced ? 1f : 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            rt.localScale = Vector3.one * Mathf.Max(0.01f, e);
        }

        /// <summary>Confettis aux couleurs du Brésil qui jaillissent des pieds du personnage.</summary>
        void Burst(int n)
        {
            for (int i = 0; i < n; i++)
            {
                var col = Party[UnityEngine.Random.Range(0, Party.Length)];
                var img = Ui.Image(Safe, Art.Pixel, col, "Confetti");
                var rt = img.rectTransform;
                Ui.Place(rt, 0.5f, 0.5f, UnityEngine.Random.Range(-120f, 120f), FeetY + 220f, UnityEngine.Random.Range(14f, 26f), UnityEngine.Random.Range(8f, 14f));
                float a = UnityEngine.Random.Range(0.08f, 0.92f) * Mathf.PI;
                float sp = UnityEngine.Random.Range(0.5f, 1f) * 1900f;
                _confetti.Add((rt, img, new Vector2(Mathf.Cos(a) * sp, Mathf.Sin(a) * sp), UnityEngine.Random.Range(-720f, 720f), 0f));
            }
        }

        void TickConfetti(float dt)
        {
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.age += dt;
                if (c.rt == null) { _confetti.RemoveAt(i); continue; }
                c.v.y -= 2200f * dt;
                c.v *= 1f - 0.8f * dt;
                c.rt.anchoredPosition += c.v * dt;
                c.rt.localEulerAngles = new Vector3(0, 0, c.rt.localEulerAngles.z + c.spin * dt);
                c.rt.localScale = new Vector3(1f, Mathf.Abs(Mathf.Cos(c.age * 9f + i)), 1f);
                if (c.age > 2f) { var col = c.img.color; col.a = Mathf.Clamp01(1f - (c.age - 2f) / 0.4f); c.img.color = col; }
                if (c.age > 2.4f) { UnityEngine.Object.Destroy(c.rt.gameObject); _confetti.RemoveAt(i); continue; }
                _confetti[i] = c;
            }
        }
    }
}
