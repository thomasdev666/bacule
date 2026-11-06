using System;
using Bascule.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>Palette de l'interface.</summary>
    public static class Palette
    {
        public static readonly Color Ink = ArtCache.Hex("#2B2D42");
        public static readonly Color Outline = ArtCache.Hex("#2A1E3D");
        public static readonly Color Paper = ArtCache.Hex("#FFF6E5");
        public static readonly Color White = Color.white;
        public static readonly Color Primary = ArtCache.Hex("#FF5A5F");
        public static readonly Color Play = ArtCache.Hex("#5BD16E");
        public static readonly Color Secondary = ArtCache.Hex("#4A9BFF");
        public static readonly Color Accent = ArtCache.Hex("#FFC93C");
        public static readonly Color Success = ArtCache.Hex("#2EC4A6");
        public static readonly Color Purple = ArtCache.Hex("#A66CFF");
        public static readonly Color Muted = ArtCache.Hex("#8D99AE");
        public static readonly Color Dark = ArtCache.Hex("#3D3B5C");
        public static readonly Color Shade = new Color(0.1f, 0.08f, 0.2f, 0.62f);
        public static readonly Color Glass = new Color(0.1f, 0.08f, 0.22f, 0.45f);
    }

    /// <summary>
    /// Fabrique d'interface uGUI par le code. Référence 1080 × 1920 en portrait : toutes les tailles sont en pixels
    /// de cette référence. Style « jeu mobile » : boutons bonbon en relief, textes blancs cernés, cartes crème.
    /// </summary>
    public static class Ui
    {
        public const float RefWidth = 1080f, RefHeight = 1920f;
        /// <summary>Taille minimale de texte (lisible par une personne âgée sur un petit téléphone).</summary>
        public const int MinFont = 44;

        public static Font Font;
        public static ArtCache Art;
        public static float TextScale = 1f;
        public static Action Click;

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Place un élément : ancre (0..1), position relative à l'ancre, taille, pivot.</summary>
        public static RectTransform Place(RectTransform rt, float ax, float ay, float x, float y, float w, float h, float px = 0.5f, float py = 0.5f)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Image(Transform parent, Sprite sprite, Color color, string name = "Image", bool sliced = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sliced) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            // Pilules et verre : bouts toujours bien ronds, même plus fins que l'image (jamais de pointe).
            if (sliced && Art != null && sprite != null && (sprite == Art.Pill || sprite == Art.Glass)) CapsuleSlice.Attach(img);
            return img;
        }

        public static Image Panel(Transform parent, Color color, bool rounded = false, string name = "Panel") =>
            Image(parent, rounded ? Art.Pill : null, color, name, rounded);

        /// <summary>Verre liquide (pastilles, dock, score, onglets) : translucide, liseré et reflets, sans teinte.</summary>
        public static Image Glass(Transform parent, string name = "Verre") => Image(parent, Art.Glass ?? Art.Pill, Art.Glass != null ? Color.white : Palette.Glass, name, true);

        /// <summary>Carte crème à contour (fenêtres, cellules).</summary>
        public static Image Card(Transform parent, string name = "Carte") => Image(parent, Art.Panel, Color.white, name, true);

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(Mathf.Max(MinFont, size) * TextScale);
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            FitText.Attach(t); // jamais de texte qui dépasse de sa zone
            return t;
        }

        /// <summary>Texte blanc épais cerné de sombre, avec ombre : titres, score, libellés de boutons.</summary>
        public static Text Title(Transform parent, string text, int size, Color? color = null, string name = "Titre")
        {
            var t = Label(parent, text, size, color ?? Color.white, TextAnchor.MiddleCenter, name);
            Outlined(t, Mathf.Max(3f, size * 0.06f));
            return t;
        }

        /// <summary>
        /// Texte sans contour ni ombre (texte foncé posé sur une tuile ou une étiquette claire). Un titre a deux contours
        /// et une ombre : en couper un seul laissait des lettres foncées cernées de foncé, collées et illisibles.
        /// </summary>
        public static Text Plain(Text t)
        {
            if (t == null) return t;
            foreach (var sh in t.GetComponents<Shadow>()) sh.enabled = false; // Outline hérite de Shadow
            return t;
        }

        public static void Outlined(Text t, float thickness)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Palette.Outline;
            o.effectDistance = new Vector2(thickness, -thickness);
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = Palette.Outline;
            o2.effectDistance = new Vector2(thickness * 0.7f, thickness * 0.7f);
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0.16f, 0.1f, 0.25f, 0.8f);
            s.effectDistance = new Vector2(0, -thickness * 1.9f);
        }

        /// <summary>
        /// Gros bouton « bonbon » en relief, teinté de la couleur voulue, avec icône facultative.
        /// Zone tactile large, enfoncement au toucher, son et vibration légère.
        /// </summary>
        public static Button Button(Transform parent, string label, Color color, Action onClick, Icon? icon = null, int fontSize = 60, string name = "Bouton")
        {
            var img = Image(parent, Art.Button, color, name, true);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            btn.onClick.AddListener(() => { Click?.Invoke(); onClick?.Invoke(); });
            img.gameObject.AddComponent<PressScale>();

            // Reflet liquide (blanc, non teinté) : le bouton de couleur prend l'aspect d'un verre coloré.
            if (Art.Gloss != null)
            {
                var gloss = Image(img.transform, Art.Gloss, Color.white, "Reflet", true);
                Stretch(gloss.rectTransform);
            }
            // Contenu au-dessus du relief (14 px de profondeur dans la texture), centré icône + texte.
            var content = Stretch(Rect("Contenu", img.transform), 18, 6, 18, 20);
            var layout = content.gameObject.AddComponent<ButtonLayout>();
            if (icon.HasValue)
            {
                var ic = Image(content, Art.IconSprite(icon.Value), Color.white, "Icone");
                ic.preserveAspect = true;
                layout.Icon = ic.rectTransform;
                layout.IconSize = Mathf.Clamp(fontSize * 1.35f, 56f, 120f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var text = Title(content, label, fontSize, Color.white, "Label");
                // Une seule ligne : si la place manque, la taille baisse (FitText) au lieu de passer à la ligne et de déborder.
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                layout.Label = text;
            }
            return btn;
        }

        /// <summary>Bouton carré avec une icône seule (pause, retour, réglages…).</summary>
        public static Button IconButton(Transform parent, Icon icon, Color color, Action onClick, string name = "BoutonIcone") =>
            Button(parent, null, color, onClick, icon, 60, name);

        public static Text ButtonLabel(Button b)
        {
            var t = b.transform.Find("Contenu/Label");
            return t != null ? t.GetComponent<Text>() : null;
        }

        public static Image Icon(Transform parent, Sprite sprite, string name = "Icon")
        {
            var img = Image(parent, sprite, Color.white, name);
            img.preserveAspect = true;
            return img;
        }

        /// <summary>Colonne qui empile ses enfants (menus).</summary>
        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, int padding = 0)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static LayoutElement Size(Component c, float height, float width = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            if (width > 0) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }

        /// <summary>Zone de défilement verticale ; renvoie le conteneur où ajouter les lignes.</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var viewport = Stretch(Rect("Scroll", parent)); // remplit la zone (sinon 100 × 100 px : menus vides)
            viewport.gameObject.AddComponent<RectMask2D>();
            var bg = viewport.gameObject.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.001f);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = viewport;
            return content;
        }

        /// <summary>
        /// Met un élément animé dans son propre sous-canvas : quand il bouge, Unity ne recalcule que lui, et non toute
        /// l'interface (recommandation officielle de Unity pour l'interface sur mobile).
        /// </summary>
        public static void Isolate(GameObject go)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Web : pas de sous-canvas ajouté en cours de route. Un canvas imbriqué créé dans un écran caché peut
            // s'afficher quelques instants à la mauvaise échelle (boutons énormes à la fin de la partie).
            return;
#else
            if (go.GetComponent<Canvas>() != null) return;
            go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>(); // les boutons animés restent cliquables
#endif
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }

    /// <summary>Centre l'icône et le texte d'un bouton ensemble ; le texte rétrécit s'il est trop long.</summary>
    /// <summary>
    /// Garantit qu'un texte tient dans sa zone : s'il dépasse (en largeur sur une ligne, en hauteur s'il passe à la
    /// ligne), sa taille baisse jusqu'à ce qu'il tienne. Remplace le « Best Fit » de Unity, qui ne réduit rien quand le
    /// débordement vertical est autorisé (le réglage par défaut) : c'est ce qui faisait sortir des textes des boutons.
    /// Ne travaille que quand le texte ou la taille de sa zone change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FitText : MonoBehaviour
    {
        Text _t;
        int _base, _min, _applied;
        bool _self;

        public static void Attach(Text t)
        {
            var f = t.gameObject.AddComponent<FitText>();
            f._t = t;
            f._base = f._applied = t.fontSize;
            f._min = 0;
            t.RegisterDirtyVerticesCallback(f.Dirty);
        }

        /// <summary>Taille voulue du texte (avant réduction).</summary>
        public static int BaseSize(Text t)
        {
            var f = t.GetComponent<FitText>();
            if (f == null) return t.resizeTextForBestFit ? t.resizeTextMaxSize : t.fontSize;
            f.Absorb();
            return f._base;
        }

        /// <summary>Remet le texte à sa taille voulue (avant de mesurer sa largeur, par exemple).</summary>
        public static void Restore(Text t)
        {
            var f = t.GetComponent<FitText>();
            if (f == null) return;
            f.Absorb();
            f.Set(f._base);
            f.enabled = true;
        }

        /// <summary>Ajuste tout de suite tous les textes sous <paramref name="root"/> (carte rendue dans la même image).</summary>
        public static void FitAll(Transform root)
        {
            foreach (var f in root.GetComponentsInChildren<FitText>(true)) f.Fit();
        }

        void Dirty() { if (!_self) enabled = true; }

        /// <summary>Prend en compte un « Best Fit » ou une taille réglés par le code après la création.</summary>
        void Absorb()
        {
            if (_t.resizeTextForBestFit)
            {
                _base = _t.resizeTextMaxSize;
                _min = _t.resizeTextMinSize;
                _self = true; _t.resizeTextForBestFit = false; _self = false;
                _applied = -1;
            }
            if (_applied >= 0 && _t.fontSize != _applied) { _base = _t.fontSize; _applied = _t.fontSize; }
        }

        void Set(int size)
        {
            _applied = size;
            if (_t.fontSize == size) return;
            _self = true; _t.fontSize = size; _self = false;
        }

        void LateUpdate() { Fit(); enabled = false; }

        void Fit()
        {
            if (_t == null) return;
            Absorb();
            // Texte dont la largeur suit son contenu (rangée d'icône + texte, étiquette ajustée) : le réduire le ferait
            // rétrécir encore à chaque passage (zone vide au premier affichage → texte minuscule). On le laisse à sa taille.
            if (SizedByLayout()) { Set(_base); return; }
            var rect = _t.rectTransform.rect.size;
            string text = _t.text;
            if (string.IsNullOrEmpty(text) || rect.x < 2f || rect.y < 2f) { Set(_base); return; }
            int min = Mathf.Max(18, _min > 0 ? Mathf.Min(_min, _base) : Mathf.RoundToInt(_base * 0.5f));
            if (Fits(_base, rect, text)) { Set(_base); return; }
            int lo = min, hi = _base - 1, best = min;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (Fits(mid, rect, text)) { best = mid; lo = mid + 1; } else hi = mid - 1;
            }
            Set(best);
        }

        bool SizedByLayout()
        {
            var fitter = GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained) return true;
            var parent = transform.parent;
            if (parent == null) return false;
            var group = parent.GetComponent<HorizontalLayoutGroup>();
            return group != null && group.enabled && group.childControlWidth;
        }

        bool Fits(int size, Vector2 rect, string text)
        {
            bool wrap = _t.horizontalOverflow == HorizontalWrapMode.Wrap;
            var settings = _t.GetGenerationSettings(new Vector2(wrap ? rect.x : 100000f, 100000f));
            settings.resizeTextForBestFit = false;
            settings.fontSize = size;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var gen = _t.cachedTextGeneratorForLayout;
            float ppu = _t.pixelsPerUnit;
            // Marge pour le contour et l'ombre des titres.
            if (!wrap && gen.GetPreferredWidth(text, settings) / ppu > rect.x * 0.97f) return false;
            return gen.GetPreferredHeight(text, settings) / ppu <= rect.y * 1.02f;
        }
    }

    public sealed class ButtonLayout : MonoBehaviour
    {
        public RectTransform Icon;
        public float IconSize;
        public Text Label;
        const float Spacing = 18f;
        Vector2 _lastSize;
        string _lastText;

        void LateUpdate()
        {
            var rt = (RectTransform)transform;
            var size = rt.rect.size;
            string text = Label != null ? Label.text : null;
            if (size == _lastSize && text == _lastText) return;
            _lastSize = size; _lastText = text;
            float icon = Icon != null ? Mathf.Min(IconSize, size.y) : 0f;
            float gap = Icon != null && Label != null && !string.IsNullOrEmpty(text) ? Spacing : 0f;
            float textW = 0f;
            if (Label != null && !string.IsNullOrEmpty(text))
            {
                var settings = Label.GetGenerationSettings(new Vector2(10000f, size.y));
                settings.resizeTextForBestFit = false;
                settings.fontSize = FitText.BaseSize(Label);
                float want = Label.cachedTextGeneratorForLayout.GetPreferredWidth(text, settings) / Label.pixelsPerUnit + 8f;
                // Bouton étroit : l'icône se resserre d'abord (jusqu'aux deux tiers), puis le texte rétrécit (FitText).
                if (Icon != null && want + icon + gap > size.x) { icon = Mathf.Max(icon * 0.66f, Mathf.Min(icon, size.x - want - gap)); gap = Spacing * 0.6f; }
                textW = Mathf.Max(1f, Mathf.Min(want, size.x - icon - gap));
            }
            float total = icon + gap + textW, x = -total * 0.5f;
            if (Icon != null)
            {
                Icon.anchorMin = Icon.anchorMax = new Vector2(0.5f, 0.5f);
                Icon.sizeDelta = new Vector2(icon, icon);
                Icon.anchoredPosition = new Vector2(x + icon * 0.5f, 0);
            }
            if (Label != null)
            {
                var lr = Label.rectTransform;
                lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
                lr.sizeDelta = new Vector2(Mathf.Max(1f, textW), size.y);
                lr.anchoredPosition = new Vector2(x + icon + gap + textW * 0.5f, 0);
            }
        }
    }

    /// <summary>Le bouton s'enfonce au toucher et rebondit au relâcher.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        float _target = 1f, _scale = 1f, _vel;
        public void OnPointerDown(PointerEventData e) => _target = 0.92f;
        public void OnPointerUp(PointerEventData e) => _target = 1f;
        public void OnPointerExit(PointerEventData e) => _target = 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _vel += ((_target - _scale) * 600f - _vel * 26f) * dt;
            _scale += _vel * dt;
            transform.localScale = new Vector3(_scale, _scale, 1f);
        }
    }

    /// <summary>Ouverture douce d'un menu : fondu et léger zoom (0,22 s), au lieu d'une apparition sèche.</summary>
    public sealed class ScreenIntro : MonoBehaviour
    {
        public RectTransform Content;
        CanvasGroup _group;
        float _t = 1f;

        void Awake() { _group = GetComponent<CanvasGroup>(); if (_group == null) _group = gameObject.AddComponent<CanvasGroup>(); }
        void OnEnable() { _t = 0f; Apply(); }

        void Update()
        {
            if (_t >= 1f) return;
            _t += Time.unscaledDeltaTime / 0.22f;
            Apply();
        }

        void Apply()
        {
            float x = Mathf.Clamp01(_t), e = 1f - (1f - x) * (1f - x) * (1f - x);
            if (_group != null) _group.alpha = e;
            if (Content != null) Content.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, e);
        }
    }

    /// <summary>Interrupteur animé (réglages) : piste arrondie verte ou grise, pastille blanche qui glisse.</summary>
    public sealed class UiSwitch : MonoBehaviour
    {
        Image _track;
        RectTransform _knob;
        float _x;
        bool _on;

        public bool On
        {
            get => _on;
            set { _on = value; enabled = true; }
        }

        public static UiSwitch Create(Transform parent, ArtCache art, bool on)
        {
            var track = Ui.Image(parent, art.Pill, Color.white, "Interrupteur", true);
            var sw = track.gameObject.AddComponent<UiSwitch>();
            sw._track = track;
            var shade = Ui.Image(track.transform, art.Circle, new Color(0.1f, 0.08f, 0.2f, 0.25f), "Ombre");
            var knob = Ui.Image(track.transform, art.Circle, Color.white, "Pastille");
            sw._knob = knob.rectTransform;
            Ui.Place(sw._knob, 0.5f, 0.5f, 0, 0, 66, 66);
            Ui.Place(shade.rectTransform, 0.5f, 0.5f, 0, -5, 66, 66);
            shade.transform.SetParent(sw._knob, true);
            shade.transform.SetAsFirstSibling();
            shade.rectTransform.anchoredPosition = new Vector2(0, -5);
            sw._on = on;
            sw._x = on ? 1f : 0f;
            sw.Apply();
            return sw;
        }

        void Apply()
        {
            _knob.anchoredPosition = new Vector2(Mathf.Lerp(-33f, 33f, _x), 0);
            _track.color = Color.Lerp(new Color(0.55f, 0.6f, 0.68f, 0.55f), Palette.Play, _x);
        }

        void Update()
        {
            float target = _on ? 1f : 0f;
            _x = Mathf.MoveTowards(_x, target, Time.unscaledDeltaTime * 7f);
            Apply();
            if (Mathf.Approximately(_x, target)) enabled = false;
        }
    }

    /// <summary>Apparition élastique (écrans, fenêtres, récompenses).</summary>
    public sealed class PopIn : MonoBehaviour
    {
        public float Delay;
        float _t = -1f;

        void OnEnable() { _t = -Delay; transform.localScale = Vector3.one * 0.01f; }

        void Update()
        {
            if (_t >= 1f) return;
            _t += Time.unscaledDeltaTime / 0.38f;
            float x = Mathf.Clamp01(_t);
            // Courbe avec dépassement (ease-out-back).
            const float c1 = 1.9f, c3 = c1 + 1f;
            float e = x <= 0 ? 0.01f : 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
            transform.localScale = new Vector3(e, e, 1f);
        }
    }

    /// <summary>Pulsation douce (bouton Jouer, récompense).</summary>
    public sealed class Pulse : MonoBehaviour
    {
        public float Amount = 0.05f, Speed = 3f;
        void Awake() => Ui.Isolate(gameObject);
        void Update()
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * Speed) * Amount;
            transform.localScale = new Vector3(s, s, 1);
        }
    }

    /// <summary>Rotation continue (rayons de victoire).</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float Speed = 20f;
        void Awake() => Ui.Isolate(gameObject);
        void Update() => transform.Rotate(0, 0, Speed * Time.unscaledDeltaTime);
    }

    /// <summary>Balancement de bascule (lettres du logo).</summary>
    public sealed class Rock : MonoBehaviour
    {
        public float Angle = 6f, Speed = 1.6f, Phase;
        public float Bob;
        Vector2 _base;
        void Awake() => Ui.Isolate(gameObject);
        void Start() { _base = ((RectTransform)transform).anchoredPosition; }
        void Update()
        {
            float t = Time.unscaledTime * Speed + Phase;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * Angle);
            ((RectTransform)transform).anchoredPosition = _base + new Vector2(0, Mathf.Sin(t * 2f) * Bob);
        }
    }

    /// <summary>Compteur qui défile jusqu'à la valeur (score de fin de partie).</summary>
    public sealed class CountUp : MonoBehaviour
    {
        Text _text;
        int _target;
        float _t;
        public Action Tick;

        public void Run(int target)
        {
            _text = GetComponent<Text>();
            _target = target;
            _t = 0f;
            enabled = true;
        }

        void Update()
        {
            if (_text == null) return;
            _t += Time.unscaledDeltaTime / Mathf.Lerp(0.6f, 1.4f, Mathf.Clamp01(_target / 5000f));
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(_t), 3);
            _text.text = Loc.FormatScore(Mathf.RoundToInt(_target * e));
            if (_t >= 1f) enabled = false;
        }
    }

    /// <summary>
    /// Garde une pilule découpée (Pill, Glass) bien ronde même quand elle est plus fine que son image : les coins de
    /// l'image (46 px de rayon) sont réduits à la moitié de la hauteur. Sans cela, une barre de 34 px de haut
    /// écrase ses coins et finit en pointe.
    /// </summary>
    [ExecuteAlways]
    public sealed class CapsuleSlice : MonoBehaviour
    {
        const float Corner = 46f;
        Image _img;

        public static Image Attach(Image img)
        {
            if (img == null) return null;
            var c = img.GetComponent<CapsuleSlice>() ?? img.gameObject.AddComponent<CapsuleSlice>();
            c._img = img;
            c.Apply();
            return img;
        }

        void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            if (_img == null) _img = GetComponent<Image>();
            if (_img == null) return;
            var rt = (RectTransform)transform;
            float h = rt.rect.height > 1 ? rt.rect.height : rt.sizeDelta.y;
            if (h <= 1) return;
            _img.pixelsPerUnitMultiplier = Mathf.Max(1f, Corner / (h * 0.5f));
        }
    }

    /// <summary>Adapte un panneau à la zone sûre de l'écran (encoche, barre de navigation).</summary>
    public sealed class SafeArea : MonoBehaviour
    {
        Rect _last;
        RectTransform _rt;

        void Awake() { _rt = (RectTransform)transform; Apply(); }
        void Update() { if (Screen.safeArea != _last) Apply(); }

        void Apply()
        {
            _last = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            // Écran en paysage (éditeur, ordinateur) : le jeu est affiché dans une colonne portrait, sans encoche.
            if (Screen.width > Screen.height * GameApp.MaxAspect)
            {
                _rt.anchorMin = Vector2.zero; _rt.anchorMax = Vector2.one;
                _rt.offsetMin = _rt.offsetMax = Vector2.zero;
                return;
            }
            var area = _last;
#if UNITY_EDITOR
            // Dans l'éditeur (et avec Unity Remote), Unity ne connaît pas l'encoche du téléphone : pour un écran de
            // téléphone moderne (plus de 1,9 fois plus haut que large), on simule celle de l'iPhone 14 Pro
            // (Dynamic Island : 59 points en haut, barre d'accueil : 34 points en bas, sur 852).
            if (area.width >= Screen.width - 1 && area.height >= Screen.height - 1 && Screen.height > Screen.width * 1.9f)
            {
                float top = Screen.height * 59f / 852f, bottom = Screen.height * 34f / 852f;
                area = new Rect(0, bottom, Screen.width, Screen.height - top - bottom);
            }
#endif
            var min = area.position;
            var max = area.position + area.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = _rt.offsetMax = Vector2.zero;
        }
    }
}
