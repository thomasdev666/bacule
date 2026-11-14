using Bascule.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bascule
{
    /// <summary>Ordre d'affichage des éléments du monde.</summary>
    public static class Layers
    {
        public const int Background = -100, Shadow = -20, MascotBack = -10, Mascot = 0, Plank = 20, Hands = 22;
        /// <summary>Les poids sont triés entre WeightsMin et WeightsMax selon leur hauteur (le plus haut devant).</summary>
        public const int WeightsMin = 100, WeightsMax = 900, Held = 1000, Effects = 2000;
        public const int Weights = WeightsMin;
    }

    /// <summary>Doigt ou souris, avec l'ancien et le nouveau système d'entrée de Unity.</summary>
    public static class PointerInput
    {
        public static bool Read(out Vector2 position, out bool pressed, out bool held, out bool released)
        {
#if BASCULE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var p = UnityEngine.InputSystem.Pointer.current;
            if (p == null) { position = default; pressed = held = released = false; return false; }
            position = p.position.ReadValue();
            pressed = p.press.wasPressedThisFrame;
            held = p.press.isPressed;
            released = p.press.wasReleasedThisFrame;
            return true;
#else
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                position = t.position;
                pressed = t.phase == TouchPhase.Began;
                released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !released;
                return true;
            }
            position = Input.mousePosition;
            pressed = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            released = Input.GetMouseButtonUp(0);
            return true;
#endif
        }

        static readonly System.Collections.Generic.List<RaycastResult> Hits = new System.Collections.Generic.List<RaycastResult>();

        /// <summary>Vrai si le doigt est sur un bouton de l'interface (pour ne pas lâcher un poids en appuyant sur Pause).</summary>
        public static bool OverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            Hits.Clear();
            es.RaycastAll(data, Hits);
            return Hits.Count > 0;
        }
    }

    /// <summary>Cadre la caméra : au moins 8,6 unités de large (planche + marges) et 19 de haut, en portrait.</summary>
    public static class WorldLayout
    {
        public const float MinWidth = 8.6f;
        public const float MinHalfHeight = 9.6f;

        public static float Fit(Camera cam)
        {
            float aspect = Mathf.Max(0.3f, cam.aspect);
            float size = Mathf.Max(MinHalfHeight, MinWidth * 0.5f / aspect);
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(0, 0, -10);
            return size;
        }

        public static float HalfWidth(Camera cam) => cam.orthographicSize * cam.aspect;
        /// <summary>Hauteur du sol au-dessus du bas de l'écran (unités).</summary>
        public const float GroundAboveBottom = 2.1f;
        public static float GroundY(Camera cam) => -cam.orthographicSize + GroundAboveBottom;
        /// <summary>
        /// Bas de l'interface du haut (score, niveau, jauge, objectif), en pixels d'écran depuis le bas.
        /// Donné à chaque image par le HUD ; négatif tant qu'il n'est pas connu.
        /// </summary>
        public static float HudBottomScreenY = -1f;

        /// <summary>Espace entre le bas de l'interface et le centre du poids tenu (demi-poids, balancement, marge).</summary>
        const float HudClearance = 0.9f;

        /// <summary>
        /// Hauteur du poids tenu, juste sous l'interface du haut. Sur iPhone, l'encoche et l'écran plus étroit
        /// font descendre l'interface : une hauteur fixe cachait le poids derrière le texte « Objectif ».
        /// </summary>
        public static float SpawnY(Camera cam)
        {
            float size = cam.orthographicSize;
            float y = size - 5.1f;
            if (HudBottomScreenY > 0f && cam.pixelHeight > 0)
            {
                float ppu = cam.pixelHeight / (2f * size);
                float fromTop = (cam.pixelRect.yMax - HudBottomScreenY) / ppu;
                y = Mathf.Min(y, size - fromTop - HudClearance);
            }
            // Garde-fou : jamais trop près de la planche.
            return Mathf.Max(y, size - 7f);
        }
    }

    /// <summary>Aide pour les sprites « 9-slice » : étire sans déformer les bouts arrondis.</summary>
    public static class SpriteUtil
    {
        public static SpriteRenderer Make(string name, Transform parent, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Dimensionne un sprite tranché à w × h unités en gardant des arrondis proportionnés à la plus petite dimension.</summary>
        public static void SetSliced(SpriteRenderer sr, float w, float h)
        {
            float k = Mathf.Max(0.001f, Mathf.Min(w, h));
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(w / k, h / k);
            sr.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>Place un segment épais (bras, jambe) entre deux points locaux.</summary>
        public static void Segment(SpriteRenderer sr, Vector2 a, Vector2 b, float thickness)
        {
            var d = b - a;
            float len = Mathf.Max(thickness, d.magnitude + thickness);
            sr.transform.localPosition = (a + b) * 0.5f;
            sr.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // La capsule mesure 4 × 1 unités : bouts de rayon 0,5.
            float k = thickness;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(len / k, 1f);
            sr.transform.localScale = new Vector3(k, k, 1f);
        }
    }

    /// <summary>Marque la planche pour les collisions.</summary>
    public sealed class PlankMarker : MonoBehaviour { }

    /// <summary>
    /// Un poids : bloc de fonte émaillé dessiné par WeightArtist, chiffre gravé sur la plaque (TextMesh),
    /// collider trapézoïdal ajusté au dessin, tri d'affichage selon la hauteur.
    /// </summary>
    public sealed class WeightBody : MonoBehaviour
    {
        /// <summary>Hauteur d'une ligne de TextMesh ≈ characterSize × fontSize × TextLineFactor (unités monde).</summary>
        public const float TextLineFactor = 0.1f;
        const int FontSize = 96;

        public int Value { get; private set; }
        /// <summary>Identifiant stable (duel : la vue de l'adversaire suit chaque poids d'une photo à l'autre).</summary>
        public ushort NetId { get; private set; }
        static ushort _nextNetId;
        public bool Landed { get; set; }
        public bool Merging { get; set; }
        public bool Dropped { get; set; }
        /// <summary>Largeur de la base (unités monde).</summary>
        public float Size { get; private set; }
        /// <summary>Demi-hauteur du corps (unités monde).</summary>
        public float HalfHeight { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public System.Action<WeightBody, WeightBody> OnTouchSame;
        public System.Action<WeightBody> OnFirstLanding;

        SpriteRenderer _block;
        TextMesh _text, _engrave;
        Transform _visual;
        UnityEngine.Rendering.SortingGroup _group;
        float _pop, _squash;
        bool _held;

        public static WeightBody Create(Transform parent, int value, ArtCache art, Font font, WeightTuning t, PhysicsMaterial2D material)
        {
            var go = new GameObject("Poids " + value);
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<WeightBody>();
            w.NetId = ++_nextNetId == 0 ? ++_nextNetId : _nextNetId;
            w.Value = WeightCatalog.Normalize(value);
            w.Size = WeightCatalog.Size(w.Value, t);
            float tex = w.Size / (2f * WeightArtist.BottomHalfWidth); // largeur monde de la texture
            w.HalfHeight = (WeightArtist.BodyTop - WeightArtist.BodyBottom) * 0.5f * tex;
            w._group = go.AddComponent<UnityEngine.Rendering.SortingGroup>();
            w._group.sortingOrder = Layers.WeightsMin;

            w._visual = new GameObject("Visuel").transform;
            w._visual.SetParent(go.transform, false);
            var scaled = new GameObject("Echelle").transform;
            scaled.SetParent(w._visual, false);
            scaled.localScale = Vector3.one * tex;
            w._block = SpriteUtil.Make("Fonte", scaled, art.Weight(w.Value), Color.white, 0);

            string label = w.Value.ToString();
            float digitHeight = WeightArtist.DigitHeight(label.Length);
            float plateY = WeightArtist.PlateCenterY - WeightArtist.BodyCenterY;
            w._engrave = MakeText(scaled, font, label, new Color(1, 1, 1, 0.75f), digitHeight, new Vector3(0, plateY - 0.008f, 0), 1);
            w._text = MakeText(scaled, font, label, ArtCache.Hex("#2B2D42"), digitHeight, new Vector3(0, plateY + 0.004f, 0), 2);

            w.Body = go.AddComponent<Rigidbody2D>();
            w.Body.mass = WeightCatalog.Mass(w.Value, t);
            w.Body.gravityScale = t.gravityScale;
            w.Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            w.Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            w.Body.sharedMaterial = material;
            var col = go.AddComponent<PolygonCollider2D>();
            var p = WeightArtist.ColliderPoints();
            var pts = new Vector2[p.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(p[2 * i], p[2 * i + 1]) * tex;
            col.SetPath(0, pts);
            col.sharedMaterial = material;
            return w;
        }

        static TextMesh MakeText(Transform parent, Font font, string label, Color color, float capHeight, Vector3 pos, int order)
        {
            var go = new GameObject("Chiffre");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = label;
            tm.fontSize = FontSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            // Les chiffres font environ 72 % de la hauteur de ligne.
            tm.characterSize = capHeight / 0.72f / (FontSize * TextLineFactor);
            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        /// <summary>Tenu en l'air par le joueur : pas encore soumis à la physique, affiché devant tout.</summary>
        public void SetHeld(bool held)
        {
            _held = held;
            Body.simulated = !held;
            _group.sortingOrder = held ? Layers.Held : Layers.WeightsMin;
        }

        public void PopIn(float amount = 0.35f) { _pop = amount; }

        /// <summary>Écrasement à l'atterrissage (rebond élastique).</summary>
        public void Squash(float amount) { _squash = Mathf.Max(_squash, amount); }

        void Update()
        {
            if (!_held)
            {
                // Le poids le plus haut passe devant (son anse est cachée par celui qui est posé dessus).
                int order = Layers.WeightsMin + Mathf.Clamp(Mathf.RoundToInt((transform.position.y + 20f) * 20f), 0, Layers.WeightsMax - Layers.WeightsMin);
                if (_group.sortingOrder != order) _group.sortingOrder = order;
            }
            float sx = 1f, sy = 1f;
            if (_pop > 0.001f)
            {
                _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime * 2.2f);
                float s = 1f + Mathf.Sin(_pop * Mathf.PI * 3f) * _pop;
                sx *= s; sy *= s;
            }
            if (_squash > 0.001f)
            {
                _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 1.6f);
                float k = Mathf.Sin(_squash * Mathf.PI * 4f) * _squash;
                sx *= 1f + k * 0.6f; sy *= 1f - k;
            }
            _visual.localScale = new Vector3(sx, sy, 1);
            // Écrasement vers le bas : le pied du poids reste au sol.
            _visual.localPosition = new Vector3(0, -(1f - sy) * HalfHeight, 0);
        }

        void OnCollisionEnter2D(Collision2D c) => Touch(c);
        void OnCollisionStay2D(Collision2D c) => Touch(c);

        void Touch(Collision2D c)
        {
            if (Merging) return;
            // Appelé à chaque pas de physique pour chaque contact : on sort vite quand il n'y a rien à faire.
            c.collider.TryGetComponent(out WeightBody other);
            if (Landed && (other == null || other.Value != Value)) return;
            bool supported = other != null ? other.Landed : c.collider.TryGetComponent(out PlankMarker _);
            if (supported && !Landed)
            {
                Landed = true;
                OnFirstLanding?.Invoke(this);
            }
            if (other != null && !other.Merging && other.Value == Value)
                OnTouchSame?.Invoke(this, other);
        }
    }
}
