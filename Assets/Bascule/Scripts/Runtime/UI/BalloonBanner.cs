using Bascule.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Grande bannière dorée quand un 128 s'envole : « MONTGOLFIÈRE ! » et les points gagnés d'un coup.
    /// Le joueur voit tout de suite que c'est le plus gros coup du jeu, et a envie de recommencer.
    ///
    /// Posée sur l'herbe sous la planche (comme l'annonce de rafale, plus haut si une rafale est affichée) :
    /// elle ne cache ni le poids tenu ni la pile. Entrée qui rebondit, montgolfière qui flotte, étoiles qui scintillent,
    /// puis tout s'envole vers le haut en s'effaçant. Rien n'y capte le doigt.
    /// Un sous-canvas à part (Ui.Isolate) : ses animations ne redessinent pas le reste de l'interface.
    /// </summary>
    public sealed class BalloonBanner
    {
        const float W = 1000f, H = 230f, LowY = 300f, HighY = 560f, Hold = 2.4f, Leave = 0.55f;
        const int Stars = 5;

        readonly RectTransform _root, _ribbon, _balloon;
        readonly CanvasGroup _group;
        readonly Text _title, _points;
        readonly RectTransform[] _stars = new RectTransform[Stars];
        static readonly Vector2[] StarSpots =
        {
            new Vector2(-430, 96), new Vector2(392, 104), new Vector2(456, -64), new Vector2(-300, -100), new Vector2(150, 118),
        };

        float _t = -1f, _baseY;
        bool _reduced;

        static readonly Color Gold = ArtCache.Hex("#FFB703");
        static readonly Color DeepGold = ArtCache.Hex("#D9480F");

        public BalloonBanner(Transform parent, ArtCache art)
        {
            _root = Ui.Rect("Montgolfiere", parent);
            Ui.Place(_root, 0.5f, 0, 0, LowY, 1080, H + 120, 0.5f, 0.5f);
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false; _group.interactable = false;

            // Banderole dorée « bonbon » : bord orangé avec ombre, reflet clair sur le haut.
            var ribbonImg = Ui.Image(_root, art.Pill, Gold, "Banderole", true);
            _ribbon = ribbonImg.rectTransform;
            Ui.Place(_ribbon, 0.5f, 0.5f, 0, 0, W, H);
            var rim = Ui.Image(_ribbon, art.Pill, DeepGold, "Bord", true);
            Ui.Stretch(rim.rectTransform, -10, -10, -10, -10);
            rim.transform.SetAsFirstSibling();
            var shadow = rim.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.25f, 0.08f, 0.02f, 0.45f);
            shadow.effectDistance = new Vector2(0, -14);
            var gloss = Ui.Image(_ribbon, art.Pill, new Color(1, 1, 1, 0.28f), "Reflet", true);
            var g = gloss.rectTransform;
            g.anchorMin = new Vector2(0, 0.56f); g.anchorMax = new Vector2(1, 0.9f);
            g.offsetMin = new Vector2(44, 0); g.offsetMax = new Vector2(-44, 0);

            // La montgolfière du 128 dépasse à gauche de la banderole.
            var balloon = Ui.Icon(_ribbon, art.Balloon, "Ballon");
            _balloon = balloon.rectTransform;
            Ui.Place(_balloon, 0, 0.5f, 120, 30, 230, 230);
            var tag = Ui.Title(_balloon, "128", 46);
            Ui.Place(tag.rectTransform, 0.5f, 0.5f, 0, 34, 160, 60);

            _title = Ui.Title(_ribbon, Loc.Get("balloon_title"), 92);
            Ui.Place(_title.rectTransform, 0.5f, 0.5f, 90, 36, 700, 110);
            _points = Ui.Title(_ribbon, "", 62, ArtCache.Hex("#FFF4C2"));
            Ui.Place(_points.rectTransform, 0.5f, 0.5f, 90, -54, 700, 80);

            for (int i = 0; i < Stars; i++)
            {
                var st = Ui.Icon(_root, art.IconSprite(Icon.Star), "Etoile");
                _stars[i] = st.rectTransform;
                Ui.Place(_stars[i], 0.5f, 0.5f, StarSpots[i].x, StarSpots[i].y, 64, 64);
            }

            Ui.Isolate(_root.gameObject);
            _root.gameObject.SetActive(false);
        }

        /// <summary>Lance la bannière avec les points gagnés d'un coup. <paramref name="high"/> : une rafale occupe déjà le bas.</summary>
        public void Show(int points, bool high, bool reducedMotion)
        {
            _points.text = Loc.Format("balloon_points", Loc.FormatScore(points));
            _baseY = high ? HighY : LowY;
            _reduced = reducedMotion;
            _t = 0f;
            _root.gameObject.SetActive(true);
            Tick(0f);
        }

        public void Hide()
        {
            _t = -1f;
            _root.gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (_t < 0f) return;
            _t += dt;
            if (_t >= Hold + Leave) { Hide(); return; }

            // Entrée : grossit avec un rebond (easeOutBack), sortie : monte et s'efface.
            float k = Mathf.Clamp01(_t / 0.38f), c1 = 2.2f, c3 = c1 + 1f;
            float pop = _reduced ? 1f : 1f + c3 * Mathf.Pow(k - 1f, 3) + c1 * Mathf.Pow(k - 1f, 2);
            float leave = Mathf.Clamp01((_t - Hold) / Leave);
            float alpha = (_reduced ? Mathf.Clamp01(_t / 0.2f) : Mathf.Clamp01(_t / 0.12f)) * (1f - leave);
            _group.alpha = alpha;
            float rise = _reduced ? 0f : leave * leave * 260f;
            _root.anchoredPosition = new Vector2(0f, _baseY + rise);
            _ribbon.localScale = Vector3.one * Mathf.Max(0.01f, pop);
            _ribbon.localEulerAngles = new Vector3(0, 0, _reduced ? 0f : -2.5f + Mathf.Sin(_t * 5f) * 1.2f * (1f - k * 0.5f));

            // La montgolfière flotte et tangue doucement ; elle part la première vers le ciel.
            if (!_reduced)
            {
                _balloon.anchoredPosition = new Vector2(120f, 30f + Mathf.Sin(_t * 3.4f) * 12f + leave * 220f);
                _balloon.localEulerAngles = new Vector3(0, 0, Mathf.Sin(_t * 2.6f) * 6f);
            }
            // Les points battent comme un cœur pendant la première seconde.
            float beat = _reduced ? 1f : 1f + 0.08f * Mathf.Max(0f, Mathf.Sin(_t * 14f)) * Mathf.Clamp01(1.4f - _t);
            _points.rectTransform.localScale = Vector3.one * beat;

            for (int i = 0; i < Stars; i++)
            {
                float ph = _t * 3.2f + i * 1.3f;
                float s = _reduced ? 0.8f : Mathf.Clamp01(k * 1.4f - i * 0.08f) * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(ph)));
                _stars[i].localScale = Vector3.one * s;
                _stars[i].localEulerAngles = new Vector3(0, 0, _reduced ? 0f : _t * 90f + i * 40f);
            }
        }
    }
}
