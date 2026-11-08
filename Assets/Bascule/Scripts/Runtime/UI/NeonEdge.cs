using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Néon sur tout le contour de l'écran, à la manière de l'iPhone quand Siri s'active : une lueur douce qui suit
    /// les coins arrondis, en dégradé (bleu, violet, rose, orange) qui tourne autour de l'écran, et dont l'épaisseur
    /// ondule. Il s'allume sur les combos : plus le combo est long, plus il est intense, épais, rapide et long.
    ///
    /// Dessiné directement en maillage (aucune image) : un liseré vif au bord, puis une lueur qui s'efface vers
    /// l'intérieur. Rien n'est dessiné quand il est éteint ; dans son propre sous-canvas (Ui.Isolate) pour ne pas
    /// redessiner le reste de l'interface pendant l'animation. Il ne capte jamais le doigt.
    /// </summary>
    public sealed class NeonEdge : MaskableGraphic
    {
        static readonly Color[] Palette =
        {
            new Color(0.29f, 0.61f, 1f),    // bleu
            new Color(0.65f, 0.42f, 1f),    // violet
            new Color(1f, 0.35f, 0.65f),    // rose
            new Color(1f, 0.62f, 0.25f),    // orange
            new Color(0.3f, 0.85f, 1f),     // cyan
        };

        const int Segments = 180;
        /// <summary>
        /// Arrondi de la lueur intérieure dans les coins (unités d'interface), volontairement petit : le néon va jusqu'aux
        /// vrais coins de l'image (anneau extérieur), et c'est l'écran du téléphone qui les arrondit lui-même. Le néon
        /// épouse donc exactement la forme de chaque écran (coins arrondis d'iPhone ou coins carrés), sans forme dessinée.
        /// </summary>
        public float CornerRadius = 48f;

        float _intensity, _target, _hold, _spin, _phase, _breath, _width = 1f, _speed = 0.25f;
        public bool ReducedMotion;

        public static NeonEdge Create(Transform parent)
        {
            var rt = Ui.Stretch(Ui.Rect("Neon", parent));
            var n = rt.gameObject.AddComponent<NeonEdge>();
            n.raycastTarget = false;
            Ui.Isolate(rt.gameObject);
            rt.gameObject.SetActive(false);
            return n;
        }

        /// <summary>Combo de <paramref name="chain"/> fusions : le néon s'allume selon la force du combo.</summary>
        public void Combo(int chain)
        {
            if (chain < 2) return;
            float k = Mathf.Clamp01((chain - 2) / 3f); // ×2 → 0, ×5 et plus → 1
            // Bien visible dès le premier combo, même sur un ciel clair ; énorme sur les gros combos.
            Ignite(Mathf.Lerp(0.9f, 1f, k), Mathf.Lerp(1.1f, 2.6f, k), Mathf.Lerp(1.2f, 2.4f, k), Mathf.Lerp(0.35f, 0.9f, k));
        }

        /// <summary>Allume le néon : intensité (0..1), durée pleine (s), épaisseur relative, vitesse de rotation (tours/s).</summary>
        public void Ignite(float intensity, float seconds, float width, float speed)
        {
            if (!gameObject.activeSelf) { gameObject.SetActive(true); _intensity = 0f; }
            _target = Mathf.Max(_target, intensity);
            _hold = Mathf.Max(_hold, seconds);
            _width = Mathf.Max(_width * (_intensity > 0.05f ? 1f : 0f), width);
            _speed = Mathf.Max(_speed * (_intensity > 0.05f ? 1f : 0f), speed);
            _spin = 1f; // petit coup de vitesse au départ
        }

        public void Off()
        {
            _intensity = _target = _hold = 0f;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _hold -= dt;
            if (_hold <= 0f) _target = Mathf.MoveTowards(_target, 0f, dt * 0.9f);
            // Montée rapide, retombée douce.
            _intensity = Mathf.MoveTowards(_intensity, _target, dt * (_target > _intensity ? 6f : 1.4f));
            if (_intensity <= 0.002f && _target <= 0f) { gameObject.SetActive(false); return; }
            _spin = Mathf.MoveTowards(_spin, 0f, dt * 1.5f);
            float speed = ReducedMotion ? 0.05f : _speed * (1f + 2f * _spin);
            _phase = Mathf.Repeat(_phase + dt * speed, 1f);
            _breath += dt * (ReducedMotion ? 0.5f : 2.2f);
            SetVerticesDirty();
        }

        static Color Gradient(float t)
        {
            t = Mathf.Repeat(t, 1f) * Palette.Length;
            int i = (int)t;
            float f = t - i;
            f = f * f * (3f - 2f * f);
            return Color.Lerp(Palette[i % Palette.Length], Palette[(i + 1) % Palette.Length], f);
        }

        /// <summary>Point du contour (rectangle aux coins arrondis) et sa normale vers l'intérieur, pour t dans [0, 1).</summary>
        static void Perimeter(Rect r, float radius, float t, out Vector2 p, out Vector2 n)
        {
            float w = r.width, h = r.height;
            radius = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            float sw = w - 2 * radius, sh = h - 2 * radius, arc = Mathf.PI * 0.5f * radius;
            float total = 2 * sw + 2 * sh + 4 * arc;
            float d = Mathf.Repeat(t, 1f) * total;
            // Parcours dans le sens horaire depuis le haut gauche : haut, coin, droite, coin, bas, coin, gauche, coin.
            float[] lens = { sw, arc, sh, arc, sw, arc, sh, arc };
            int seg = 0;
            while (seg < 7 && d > lens[seg]) { d -= lens[seg]; seg++; }
            float xMin = r.xMin, xMax = r.xMax, yMin = r.yMin, yMax = r.yMax;
            switch (seg)
            {
                case 0: p = new Vector2(xMin + radius + d, yMax); n = Vector2.down; return;
                case 2: p = new Vector2(xMax, yMax - radius - d); n = Vector2.left; return;
                case 4: p = new Vector2(xMax - radius - d, yMin); n = Vector2.up; return;
                case 6: p = new Vector2(xMin, yMin + radius + d); n = Vector2.right; return;
            }
            // Coins : centre du quart de cercle et angle de départ.
            Vector2 c; float a0;
            if (seg == 1) { c = new Vector2(xMax - radius, yMax - radius); a0 = 90f; }
            else if (seg == 3) { c = new Vector2(xMax - radius, yMin + radius); a0 = 0f; }
            else if (seg == 5) { c = new Vector2(xMin + radius, yMin + radius); a0 = -90f; }
            else { c = new Vector2(xMin + radius, yMax - radius); a0 = 180f; }
            float a = (a0 - d / Mathf.Max(0.001f, arc) * 90f) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            p = c + dir * radius;
            n = -dir;
        }

        /// <summary>Point où la demi-droite partant de p dans la direction d sort du rectangle r.</summary>
        static Vector2 ToEdge(Rect r, Vector2 p, Vector2 d)
        {
            float tx = d.x > 1e-4f ? (r.xMax - p.x) / d.x : d.x < -1e-4f ? (r.xMin - p.x) / d.x : float.MaxValue;
            float ty = d.y > 1e-4f ? (r.yMax - p.y) / d.y : d.y < -1e-4f ? (r.yMin - p.y) / d.y : float.MaxValue;
            float t = Mathf.Max(0f, Mathf.Min(tx, ty));
            return p + d * t;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_intensity <= 0.002f) return;
            var r = rectTransform.rect;
            float a = _intensity;
            float glow = 95f * _width, core = 16f * Mathf.Lerp(1f, 1.5f, Mathf.Clamp01(_width - 1f));
            var v = UIVertex.simpleVert;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                Perimeter(r, CornerRadius, t, out var p, out var n);
                var col = Gradient(t - _phase);
                // L'épaisseur ondule le long du bord, comme une lumière vivante.
                float wave = ReducedMotion ? 1f : 0.7f + 0.3f * Mathf.Sin((t * 4f + _breath * 0.35f) * Mathf.PI * 2f) * Mathf.Sin((t * 2f - _breath * 0.21f) * Mathf.PI * 2f + 1.3f);
                float g = glow * Mathf.Max(0.35f, wave);
                var bright = Color.Lerp(col, Color.white, 0.15f);
                // Jusqu'au vrai bord de l'image, dans le prolongement (coins compris) : aucun coin non éclairé.
                v.position = ToEdge(r, p, -n); v.color = new Color(bright.r, bright.g, bright.b, a); vh.AddVert(v);
                // Bord : liseré vif.
                v.position = p; v.color = new Color(bright.r, bright.g, bright.b, a); vh.AddVert(v);
                // Fin du liseré.
                v.position = p + n * core; v.color = new Color(col.r, col.g, col.b, a * 0.95f); vh.AddVert(v);
                // Milieu de la lueur.
                v.position = p + n * (core + g * 0.35f); v.color = new Color(col.r, col.g, col.b, a * 0.55f); vh.AddVert(v);
                // Bout de la lueur : transparent.
                v.position = p + n * (core + g); v.color = new Color(col.r, col.g, col.b, 0f); vh.AddVert(v);
            }
            for (int i = 0; i < Segments; i++)
            {
                int b = i * 5, nb = (i + 1) * 5;
                for (int k = 0; k < 4; k++)
                {
                    vh.AddTriangle(b + k, nb + k, nb + k + 1);
                    vh.AddTriangle(b + k, nb + k + 1, b + k + 1);
                }
            }
        }
    }
}
