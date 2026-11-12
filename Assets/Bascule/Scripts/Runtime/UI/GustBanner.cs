using Bascule.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Annonce d'une rafale, en grand et en mouvement (partie solo et duel).
    ///
    /// Annonce : une grande banderole bleue légèrement penchée entre en glissant depuis le côté d'où vient le vent,
    /// avec « RAFALE ! » en très gros, trois grandes flèches qui défilent dans le sens du vent, des traits d'air qui
    /// filent à travers, et une jauge qui se vide jusqu'au souffle.
    /// Souffle : la banderole se resserre en une pastille compacte en bas (on voit toute la pile), les flèches accélèrent.
    /// Fin : la pastille part dans le sens du vent en s'effaçant.
    ///
    /// Posée sur l'herbe, sous la planche : elle ne cache ni le poids tenu, ni la pile. Rien n'y capte le doigt.
    /// Un sous-canvas à part (Ui.Isolate) : ses animations ne redessinent pas le reste de l'interface.
    /// </summary>
    public sealed class GustBanner
    {
        const float WideW = 1260f, WideH = 230f, SmallW = 660f, SmallH = 116f, WideY = 250f, SmallY = 150f;
        const int Streaks = 9;

        readonly RectTransform _root, _ribbon, _content, _bar, _barFill;
        readonly CanvasGroup _group;
        readonly Image _ribbonImg, _gloss;
        readonly Text _title;
        readonly Image[] _arrows = new Image[3];
        readonly RectTransform[] _streaks = new RectTransform[Streaks];
        readonly Image[] _streakImgs = new Image[Streaks];
        readonly float[] _streakX = new float[Streaks], _streakY = new float[Streaks], _streakSpeed = new float[Streaks];

        WindPhase _shown;
        float _alpha, _slide, _compact, _pop, _leave;
        int _dir = 1;

        static readonly Color Blue = ArtCache.Hex("#2F7BEA");
        static readonly Color DeepBlue = ArtCache.Hex("#1B4FB8");

        public GustBanner(Transform parent, ArtCache art)
        {
            _root = Ui.Rect("Rafale", parent);
            Ui.Place(_root, 0.5f, 0, 0, WideY, 1080, WideH, 0.5f, 0.5f);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false; _group.interactable = false;

            // Ombre portée puis banderole, avec un reflet clair sur le haut : l'aspect « bonbon » des boutons du jeu.
            _ribbonImg = Ui.Image(_root, art.Pill, Blue, "Banderole", true);
            _ribbon = _ribbonImg.rectTransform;
            Ui.Place(_ribbon, 0.5f, 0.5f, 0, 0, WideW, WideH);
            var rim = Ui.Image(_ribbon, art.Pill, DeepBlue, "Bord", true);
            Ui.Stretch(rim.rectTransform, -10, -10, -10, -10);
            rim.transform.SetAsFirstSibling();
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.06f, 0.2f, 0.45f);
            shadow.effectDistance = new Vector2(0, -14);
            _gloss = Ui.Image(_ribbon, art.Pill, new Color(1, 1, 1, 0.22f), "Reflet", true);
            var g = _gloss.rectTransform;
            g.anchorMin = new Vector2(0, 0.55f); g.anchorMax = new Vector2(1, 0.9f);
            g.offsetMin = new Vector2(40, 0); g.offsetMax = new Vector2(-40, 0);

            // Traits d'air qui traversent la banderole.
            for (int i = 0; i < Streaks; i++)
            {
                var s = Ui.Image(_ribbon, art.Capsule, new Color(1, 1, 1, 0f), "Trait", true);
                s.pixelsPerUnitMultiplier = 6f; // bouts ronds à l'échelle d'un trait fin
                _streakImgs[i] = s;
                _streaks[i] = s.rectTransform;
                _streaks[i].anchorMin = _streaks[i].anchorMax = new Vector2(0.5f, 0.5f);
                Respawn(i, true);
            }

            _content = Ui.Rect("Contenu", _ribbon);
            Ui.Place(_content, 0.5f, 0.5f, 0, 0, 1000, WideH);
            _title = Ui.Title(_content, Loc.Get("gust_title"), 124);
            Ui.Place(_title.rectTransform, 0.5f, 0.5f, -150, 20, 660, 160);
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < _arrows.Length; i++)
            {
                var a = Ui.Icon(_content, art.IconSprite(Icon.Back), "Fleche");
                Ui.Place(a.rectTransform, 0.5f, 0.5f, 0, 22, 96, 96);
                _arrows[i] = a;
            }

            // Jauge jusqu'au souffle.
            var track = Ui.Image(_content, art.Pill, new Color(0.06f, 0.12f, 0.35f, 0.55f), "Jauge", true);
            _bar = track.rectTransform;
            Ui.Place(_bar, 0.5f, 0.5f, 0, -72, 520, 24);
            var fill = Ui.Image(track.transform, art.Pill, Color.white, "Remplissage", true);
            _barFill = fill.rectTransform;
            _barFill.anchorMin = new Vector2(0, 0); _barFill.anchorMax = new Vector2(0, 1); _barFill.pivot = new Vector2(0, 0.5f);
            _barFill.anchoredPosition = Vector2.zero;

            Ui.Isolate(_root.gameObject);
            _root.gameObject.SetActive(false);
        }

        void Respawn(int i, bool anywhere)
        {
            float half = WideW * 0.5f;
            _streakX[i] = anywhere ? Random.Range(-half, half) : -_dir * (half + Random.Range(40f, 260f));
            _streakY[i] = Random.Range(-WideH * 0.36f, WideH * 0.36f);
            _streakSpeed[i] = Random.Range(1300f, 2100f);
            _streaks[i].sizeDelta = new Vector2(Random.Range(90f, 220f), Random.Range(7f, 13f));
        }

        /// <summary>L'annonce est à l'écran (la bannière du 128 se pose alors plus haut).</summary>
        public bool Visible => _root.gameObject.activeSelf;

        public void Hide()
        {
            _shown = WindPhase.None;
            _alpha = 0f; _slide = 0f; _compact = 0f; _leave = 0f;
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// À chaque image. <paramref name="warning"/> : avancement de l'annonce (0..1) ; <paramref name="envelope"/> : force du souffle.
        /// Renvoie vrai quand une annonce commence (pour le conseil de la première rafale).
        /// </summary>
        public bool Tick(WindPhase phase, int dir, float warning, float envelope, bool reducedMotion, float dt)
        {
            bool started = false;
            if (phase != _shown)
            {
                if (phase == WindPhase.Warning)
                {
                    started = true;
                    _dir = dir >= 0 ? 1 : -1;
                    _slide = 0f; _compact = 0f; _leave = 0f; _pop = 1f;
                    for (int i = 0; i < Streaks; i++) Respawn(i, true);
                }
                else if (phase == WindPhase.Blowing) _pop = 0.5f;
                _shown = phase;
            }
            bool on = phase != WindPhase.None;
            if (on) _dir = dir >= 0 ? 1 : -1;
            _alpha = Mathf.MoveTowards(_alpha, on ? 1f : 0f, dt * (on ? 8f : 2.2f));
            bool visible = _alpha > 0.01f;
            if (_root.gameObject.activeSelf != visible) _root.gameObject.SetActive(visible);
            if (!visible) return started;
            _group.alpha = _alpha;

            float now = Time.unscaledTime;
            // Entrée : glisse depuis le côté d'où vient le vent, petit dépassement à l'arrivée.
            _slide = Mathf.MoveTowards(_slide, 1f, dt / 0.42f);
            float k = _slide, c1 = 1.70158f, c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            if (reducedMotion) ease = k;
            // Sortie : part dans le sens du vent.
            if (!on) _leave = Mathf.MoveTowards(_leave, 1f, dt * 2.2f);
            float x = -_dir * 1300f * (1f - ease) + _dir * 700f * _leave * _leave;

            // Souffle : la banderole se resserre en pastille, en bas.
            _compact = Mathf.MoveTowards(_compact, phase == WindPhase.Blowing || (!on && _shown != WindPhase.Warning) ? 1f : 0f, dt * 3.2f);
            float cmp = Mathf.SmoothStep(0f, 1f, _compact);
            float w = Mathf.Lerp(WideW, SmallW, cmp), h = Mathf.Lerp(WideH, SmallH, cmp);
            _ribbon.sizeDelta = new Vector2(w, h);
            _root.anchoredPosition = new Vector2(x, Mathf.Lerp(WideY, SmallY, cmp));
            float tilt = Mathf.Lerp(-3.5f * _dir, 0f, cmp);
            if (!reducedMotion && phase == WindPhase.Warning) tilt += Mathf.Sin(now * 9f) * 0.8f;
            _ribbon.localEulerAngles = new Vector3(0, 0, tilt);

            _pop = Mathf.MoveTowards(_pop, 0f, dt * 2.8f);
            float beat = phase == WindPhase.Warning && !reducedMotion ? 1f + 0.035f * Mathf.Sin(now * 12f) : 1f;
            float scale = (1f + 0.22f * _pop * _pop) * beat;
            _content.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, cmp) * scale;
            _content.sizeDelta = new Vector2(1000f, WideH);
            // La banderole s'assombrit un peu pendant le souffle : le texte reste lisible, la pile passe devant le regard.
            _ribbonImg.color = Color.Lerp(Blue, DeepBlue, cmp * 0.35f);

            // Titre à gauche, flèches du côté où pousse le vent.
            _title.rectTransform.anchoredPosition = new Vector2(-_dir * 150f, 20f);
            float speed = phase == WindPhase.Blowing ? 3.4f : 1.7f;
            for (int i = 0; i < _arrows.Length; i++)
            {
                var a = _arrows[i];
                a.rectTransform.localEulerAngles = new Vector3(0, 0, _dir > 0 ? 180f : 0f);
                a.rectTransform.anchoredPosition = new Vector2(_dir * (275f + i * 80f), 20f);
                float wave = Mathf.Repeat(now * speed - i * 0.27f, 1f);
                var col = Color.white; col.a = 0.3f + 0.7f * Mathf.Sin(wave * Mathf.PI);
                a.color = col;
                float sk = 1f + 0.12f * Mathf.Sin(wave * Mathf.PI);
                a.rectTransform.localScale = new Vector3(sk, sk, 1f);
            }

            // Jauge : visible pendant l'annonce, se vide jusqu'au souffle.
            bool barOn = phase == WindPhase.Warning;
            if (_bar.gameObject.activeSelf != barOn) _bar.gameObject.SetActive(barOn);
            if (barOn) _barFill.sizeDelta = new Vector2(Mathf.Max(26f, 520f * (1f - Mathf.Clamp01(warning))), 0f);

            // Traits d'air.
            float strength = phase == WindPhase.Blowing ? 0.5f + 0.5f * envelope : phase == WindPhase.Warning ? 0.45f : 0.2f;
            for (int i = 0; i < Streaks; i++)
            {
                bool streakOn = !reducedMotion && cmp < 0.6f;
                if (_streakImgs[i].enabled != streakOn) _streakImgs[i].enabled = streakOn;
                if (!streakOn) continue;
                _streakX[i] += _dir * _streakSpeed[i] * strength * dt;
                float half = w * 0.5f;
                if (Mathf.Abs(_streakX[i]) > half + 260f) Respawn(i, false);
                _streaks[i].anchoredPosition = new Vector2(_streakX[i], _streakY[i] * (h / WideH));
                float edge = 1f - Mathf.Clamp01((Mathf.Abs(_streakX[i]) - (half - 220f)) / 220f);
                _streakImgs[i].color = new Color(1f, 1f, 1f, 0.42f * edge * (1f - cmp));
            }
            return started;
        }
    }
}
