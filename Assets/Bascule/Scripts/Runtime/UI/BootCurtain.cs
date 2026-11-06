using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Écran de démarrage de l'appli : Basile porte sa planche sous le ciel du jeu. Les lettres de BASCULE tombent
    /// une à une sur la planche, d'un bout à l'autre (B, E, A, L, S, U, C). Chaque lettre fait pencher la planche,
    /// qui se rééquilibre comme en partie, pendant que la bulle du niveau avance. Quand le mot est complet, les
    /// lettres sautent en vague et l'écran s'efface sur l'accueil, préparé pendant ce temps derrière lui.
    /// Durée : environ 2 secondes. Un toucher passe directement à la fin.
    /// Animations réduites : tout est déjà en place, fondu rapide.
    /// Version web : la page de chargement (WebGLTemplates/Bascule/index.html) a déjà joué la même animation. Cet
    /// écran montre donc sa dernière image (mot complet) et s'efface tout de suite, sans saut entre les deux.
    /// </summary>
    public sealed class BootCurtain : MonoBehaviour
    {
        static readonly string Word = "BASCULE";
        static readonly string[] LetterHex = { "#FF5A5F", "#FFC93C", "#5BD16E", "#4A9BFF", "#A66CFF", "#FF5A5F", "#FFC93C" };
        /// <summary>Ordre d'arrivée : d'un bout à l'autre, pour que la planche penche à gauche, à droite… puis s'équilibre.</summary>
        static readonly int[] Order = { 0, 6, 1, 5, 2, 4, 3 };
        const int TipCount = 5;

        // Rythme (secondes).
        const float FirstDrop = 0.1f, DropGap = 0.13f, WaveGap = 0.06f, FadeTime = 0.4f;
        const float PlankPpu = ArtCatalog.BootPlankPpu;

        sealed class Letter
        {
            public RectTransform Holder;
            public int Slot;
            public int State; // 0 attend, 1 tombe, 2 posée
            public float Y, Vy, Squash;
        }

        sealed class Spark
        {
            public RectTransform Rt;
            public Image Img;
            public Vector2 Start, Vel;
            public float Age = -1f;
        }

        CanvasGroup _group;
        RectTransform _rig, _head, _bubble, _sun;
        RectTransform[] _clouds = new RectTransform[0];
        float[] _cloudSpeed = new float[0];
        Text _pct;
        readonly List<Letter> _letters = new List<Letter>();
        readonly List<Spark> _sparks = new List<Spark>();
        float _u = 100f, _ch = 1920f, _cw = 1080f, _surface, _tubeW;
        float _t, _angle, _vel, _waveAt = -1f, _fadeAt = -1f, _fade = 1f;
        int _started, _landed, _frames;
        bool _quick, _still;

        public static void Create(Font font)
        {
            var go = new GameObject("RideauDemarrage", typeof(RectTransform));
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var c = go.AddComponent<BootCurtain>();
            c._group = go.AddComponent<CanvasGroup>();
            c._group.blocksRaycasts = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            c._quick = true;
#endif
            var app = GameApp.Instance;
            c._still = c._quick || (app != null && app.Profile != null && app.Profile.settings.reducedMotion);
            c.Build((RectTransform)go.transform, font);
        }

        /// <summary>Taille du canevas en pixels de référence (même calcul que le CanvasScaler, « match » 0,5).</summary>
        static Vector2 CanvasSize()
        {
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            float log = Mathf.Lerp(Mathf.Log(w / 1080f, 2), Mathf.Log(h / 1920f, 2), 0.5f);
            float scale = Mathf.Pow(2, log);
            return new Vector2(w / scale, h / scale);
        }

        void Build(RectTransform root, Font font)
        {
            var size = CanvasSize();
            _cw = size.x; _ch = size.y;
            var art = Ui.Art;
            var theme = Theme.All[0];

            // Ciel, soleil, nuages et collines : les vraies images du décor d'accueil (déjà dans l'ArtPack).
            var sky = Ui.Image(root, art?.Scene("t0sky", () => SceneArtist.Sky(theme, 1024), 1024 / 5.12f, new Vector2(0.5f, 0.5f)), Color.white, "Ciel");
            Ui.Stretch(sky.rectTransform);
            if (sky.sprite == null) sky.color = ArtCache.Hex("#7CC6F2");

            if (art != null)
            {
                var sun = Ui.Image(root, art.Scene("t0sun", () => SceneArtist.Sun(theme, 512), 512 / 3.4f), Color.white, "Soleil");
                _sun = sun.rectTransform;
                Ui.Place(_sun, 1, 1, -_cw * 0.2f, -_ch * 0.12f, 420, 420);
                _clouds = new RectTransform[3];
                _cloudSpeed = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    int ci = i;
                    var cl = Ui.Image(root, art.Scene("t0cloud" + ci, () => SceneArtist.Cloud(ci, theme, 512, 256), 512 / (3.4f - ci * 0.5f)), Color.white, "Nuage");
                    float w = 420 - ci * 70;
                    _clouds[i] = cl.rectTransform;
                    Ui.Place(_clouds[i], 0.5f, 1, (-0.3f + 0.35f * ci) * _cw, -_ch * (0.08f + 0.09f * ci), w, w * 0.5f);
                    _cloudSpeed[i] = 18f + 9f * ci;
                }
                float hw = _cw * 1.5f;
                var far = Ui.Image(root, art.Scene("t0far", () => SceneArtist.Hills(theme, true, 1536, 480), 200, new Vector2(0.5f, 0f)), Color.white, "CollinesLoin");
                Ui.Place(far.rectTransform, 0.5f, 0, 0, _ch * 0.08f, hw, hw * 480 / 1536f, 0.5f, 0);
                var near = Ui.Image(root, art.Scene("t0near", () => SceneArtist.Hills(theme, false, 1536, 480), 200, new Vector2(0.5f, 0f)), Color.white, "CollinesPres");
                Ui.Place(near.rectTransform, 0.5f, 0, 0, -_ch * 0.02f, hw, hw * 480 / 1536f, 0.5f, 0);
            }

            // Mise en page reprise de la page web : une « unité » du jeu = un 6,6e de la largeur de la planche.
            var plankSprite = art?.Scene(ArtCatalog.BootPlankKey, ArtCatalog.BootPlank, PlankPpu);
            float plankW = Mathf.Min(_cw * 0.86f, _ch * 0.5f);
            Vector2 px = plankSprite != null ? plankSprite.rect.size : new Vector2(6.6f * PlankPpu + 32, 100);
            float s = plankW / px.x; // pixels de référence par pixel de l'image
            _u = PlankPpu * s;
            float plankH = (0.3f * PlankPpu + 12) * s;   // la planche seule, marges comprises (bas de l'image)
            _surface = (0.3f * PlankPpu + 6) * s;        // dessus plat de la planche
            float headS = _u * 2.7f;
            float stageH = headS + plankH;
            float stageCenter = _ch * 0.03f;             // centre de la scène à 47 % de la hauteur, comme sur le web
            float rigY = stageCenter + stageH * 0.5f - plankH;

            // Basile, sous la planche (le haut de ses cheveux passe derrière elle).
            if (art != null)
            {
                var shadow = Ui.Image(root, art.SoftDisc, new Color(0.1f, 0.12f, 0.2f, 0.28f), "Ombre");
                float headTop = rigY + plankH * 0.65f + headS * 0.07f;
                Ui.Place(shadow.rectTransform, 0.5f, 0.5f, 0, headTop - headS * 0.93f, headS * 0.9f, _u * 0.3f);
                var head = Ui.Image(root, art.Portrait(HeadCatalog.Get(0)), Color.white, "Basile");
                head.preserveAspect = true;
                _head = head.rectTransform;
                Ui.Place(_head, 0.5f, 0.5f, 0, headTop, headS, headS, 0.5f, 1f);
            }

            // La planche et ses rebords pivotent autour du dessus de la tête.
            _rig = new GameObject("Planche", typeof(RectTransform)).GetComponent<RectTransform>();
            _rig.SetParent(root, false);
            Ui.Place(_rig, 0.5f, 0.5f, 0, rigY, plankW, px.y * s, 0.5f, 0f);
            var plank = Ui.Image(_rig, plankSprite, plankSprite != null ? Color.white : ArtCache.Hex("#C98B4F"), "Bois");
            Ui.Stretch(plank.rectTransform);

            if (font != null)
            {
                int fs = Mathf.RoundToInt(_u * 0.82f);
                for (int i = 0; i < Word.Length; i++)
                {
                    var holder = new GameObject("Lettre", typeof(RectTransform)).GetComponent<RectTransform>();
                    holder.SetParent(_rig, false);
                    Ui.Place(holder, 0.5f, 0, (i - 3) * _u * 0.76f, _surface, _u * 0.9f, fs * 1.4f, 0.5f, 0f);
                    var t = Ui.Title(holder, Word[i].ToString(), fs, ArtCache.Hex(LetterHex[i]), "Glyphe");
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var fit = t.GetComponent<FitText>();
                    if (fit != null) DestroyImmediate(fit); // une lettre seule, à taille fixe
                    // Une ligne centrée a sa ligne de base environ 0,37 fois la taille sous son centre.
                    Ui.Place(t.rectTransform, 0.5f, 0, 0, fs * 0.37f, _u * 1.1f, fs * 1.4f);
                    var l = new Letter { Holder = holder, Slot = i - 3 };
                    holder.gameObject.SetActive(false);
                    _letters.Add(l);
                }

                // Niveau à bulle : la progression du démarrage.
                if (art != null)
                {
                    _tubeW = Mathf.Min(_cw * 0.62f, _ch * 0.34f);
                    float tubeH = _tubeW * 96 / 640f;
                    float stageBottom = stageCenter - stageH * 0.5f;
                    float tubeY = stageBottom - _ch * 0.05f - tubeH * 0.5f;
                    var tube = Ui.Image(root, art.LevelTube, Color.white, "Niveau");
                    Ui.Place(tube.rectTransform, 0.5f, 0.5f, 0, tubeY, _tubeW, tubeH);
                    var bubble = Ui.Image(tube.transform, art.Bubble, Color.white, "Bulle");
                    _bubble = bubble.rectTransform;
                    Ui.Place(_bubble, 0.5f, 0.5f, -_tubeW * 0.41f, 0, tubeH * 0.78f, tubeH * 0.78f);

                    _pct = Ui.Title(root, "0 %", 46, Color.white, "Pourcentage");
                    Ui.Place(_pct.rectTransform, 0.5f, 0.5f, 0, tubeY - tubeH * 0.5f - 40, 300, 60);
                    var tip = Ui.Title(root, Loc.Get("boot_tip_" + Random.Range(0, TipCount)), 40, ArtCache.Hex("#FFF4D6"), "Conseil");
                    Ui.Place(tip.rectTransform, 0.5f, 0.5f, 0, tubeY - tubeH * 0.5f - 120, Mathf.Min(_cw - 80, 900), 110);
                }

                var by = Ui.Title(root, Loc.Get("publisher"), 40, Color.white, "Editeur");
                Ui.Place(by.rectTransform, 0.5f, 0, 0, 70, 600, 56, 0.5f, 0);
            }

            // Toucher n'importe où : passer l'animation.
            var catcher = Ui.Image(root, null, new Color(0, 0, 0, 0), "Passer");
            Ui.Stretch(catcher.rectTransform);
            catcher.raycastTarget = true;
            catcher.gameObject.AddComponent<Button>().onClick.AddListener(Skip);

            if (_still) ShowFinal();
        }

        /// <summary>Le mot complet sur la planche droite et le niveau plein : dernière image de l'animation.</summary>
        void ShowFinal()
        {
            foreach (var l in _letters)
            {
                if (l.State != 2) { l.State = 2; _landed++; }
                l.Y = 0; l.Vy = 0;
                l.Holder.gameObject.SetActive(true);
                l.Holder.anchoredPosition = new Vector2(l.Slot * _u * 0.76f, _surface);
            }
            _started = _letters.Count;
            _angle = _vel = 0;
            SetProgress(1f);
            if (_fadeAt < 0) _fadeAt = _t + (_quick ? 0f : 0.35f);
        }

        void Skip()
        {
            if (_fadeAt >= 0 && _fadeAt <= _t) return;
            _still = true;
            ShowFinal();
            _fadeAt = _t;
        }

        void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (_bubble != null) _bubble.anchoredPosition = new Vector2((-0.41f + 0.82f * p) * _tubeW, 0);
            if (_pct != null) _pct.text = Mathf.RoundToInt(p * 100) + " %";
        }

        void Update()
        {
            // Les toutes premières images (création de l'accueil) sont souvent longues : pas à plus de 0,05 s.
            float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            _frames++;
            if (_quick && _frames < 4) return;
            _t += dt;

            if (!_still) Animate(dt);
            else if (_rig != null) _rig.localEulerAngles = Vector3.zero;

            for (int i = 0; i < _clouds.Length; i++)
            {
                if (_clouds[i] == null) continue;
                var p = _clouds[i].anchoredPosition;
                p.x += _cloudSpeed[i] * dt;
                if (p.x > _cw * 0.5f + 300) p.x = -_cw * 0.5f - 300;
                _clouds[i].anchoredPosition = p;
            }
            if (_sun != null && !_still) _sun.localEulerAngles = new Vector3(0, 0, -_t * 8f);
            UpdateSparks(dt);

            if (_fadeAt >= 0 && _t >= _fadeAt)
            {
                _fade -= dt / FadeTime;
                _group.alpha = Mathf.Clamp01(_fade);
                if (_fade <= 0.35f) _group.blocksRaycasts = false;
                if (_fade <= 0f) Destroy(gameObject);
            }
        }

        void Animate(float dt)
        {
            // Une lettre de plus part toutes les 0,13 s ; la bulle avance avec le temps.
            float lastDrop = FirstDrop + DropGap * (Word.Length - 1);
            if (_started < _letters.Count && _t >= FirstDrop + DropGap * _started)
            {
                var l = _letters[Order[_started++]];
                l.State = 1;
                l.Y = _ch * 0.55f;
                l.Vy = 0;
                l.Holder.gameObject.SetActive(true);
            }
            SetProgress(_t / (lastDrop + 0.45f));

            float torque = 0;
            foreach (var l in _letters)
            {
                if (l.State == 1)
                {
                    l.Vy += _ch * 4.5f * dt;
                    l.Y -= l.Vy * dt;
                    if (l.Y <= 0) Land(l);
                }
                if (l.State == 2) torque += l.Slot;
                if (l.Squash > 0) l.Squash = Mathf.Max(0, l.Squash - dt * 5);
                l.Holder.anchoredPosition = new Vector2(l.Slot * _u * 0.76f, _surface + l.Y);
                l.Holder.localScale = new Vector3(1 + l.Squash * 0.25f, 1 - l.Squash * 0.22f, 1);
            }

            // Ressort amorti : la planche penche du côté le plus chargé, oscille puis se pose (comme sur le web).
            float goal = -torque * 3.2f;
            float acc = (goal - _angle) * 38f - _vel * 6.5f;
            _vel += acc * dt;
            _angle += _vel * dt;
            if (_rig != null) _rig.localEulerAngles = new Vector3(0, 0, _angle);
            if (_head != null) _head.localEulerAngles = new Vector3(0, 0, _angle * -0.15f);

            if (_landed == _letters.Count && _waveAt < 0)
            {
                // Mot complet : les lettres sautent en vague, puis fondu vers l'accueil.
                _waveAt = _t + 0.08f;
                _fadeAt = _waveAt + WaveGap * _letters.Count + 0.3f;
            }
            if (_waveAt >= 0)
            {
                for (int i = 0; i < _letters.Count; i++)
                {
                    float at = _waveAt + i * WaveGap;
                    if (_t >= at && _t - dt < at) { _letters[i].Squash = 1; Burst(_letters[i]); }
                }
            }
        }

        void Land(Letter l)
        {
            l.State = 2;
            l.Y = 0;
            l.Squash = 1;
            _landed++;
            _vel += -l.Slot * 26f; // la planche penche du côté de la lettre
            Burst(l);
        }

        /// <summary>Petites étincelles de la couleur de la lettre, qui jaillissent du point d'arrivée.</summary>
        void Burst(Letter l)
        {
            if (Ui.Art == null || _rig == null) return;
            var color = ArtCache.Hex(LetterHex[l.Slot + 3]);
            for (int k = 0; k < 6; k++)
            {
                Spark sp = null;
                foreach (var x in _sparks) if (x.Age < 0) { sp = x; break; }
                if (sp == null)
                {
                    var img = Ui.Image(_rig, Ui.Art.Circle, Color.white, "Etincelle");
                    sp = new Spark { Rt = img.rectTransform, Img = img };
                    Ui.Place(sp.Rt, 0.5f, 0, 0, 0, _u * 0.16f, _u * 0.16f);
                    _sparks.Add(sp);
                }
                float a = Mathf.PI * (0.15f + 0.7f * Random.value);
                sp.Age = 0;
                sp.Img.color = color;
                sp.Start = new Vector2(l.Slot * _u * 0.76f, _surface);
                sp.Vel = new Vector2(Mathf.Cos(a) * _u * 2.2f, Mathf.Sin(a) * _u * 2.6f);
                sp.Rt.gameObject.SetActive(true);
            }
        }

        void UpdateSparks(float dt)
        {
            foreach (var sp in _sparks)
            {
                if (sp.Age < 0) continue;
                sp.Age += dt;
                if (sp.Age > 0.6f) { sp.Age = -1; sp.Rt.gameObject.SetActive(false); continue; }
                float s = sp.Age;
                sp.Rt.anchoredPosition = sp.Start + new Vector2(sp.Vel.x * s, sp.Vel.y * s - 9 * _u * s * s);
                float k = 1 - s / 0.6f;
                sp.Rt.localScale = new Vector3(k, k, 1);
            }
        }
    }
}
