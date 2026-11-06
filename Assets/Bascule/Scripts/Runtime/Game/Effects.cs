using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule
{
    /// <summary>
    /// Retours visuels (le « jus » du jeu) : textes flottants cernés, étincelles, onde de choc, poussière,
    /// confettis, gouttes de sueur, montgolfière, secousse de caméra.
    /// </summary>
    public sealed class Effects : MonoBehaviour
    {
        static readonly Color[] ConfettiColors =
        {
            ArtCache.Hex("#FF4F5E"), ArtCache.Hex("#FFC93C"), ArtCache.Hex("#3A86FF"), ArtCache.Hex("#12B5A6"), ArtCache.Hex("#F25CA2"), ArtCache.Hex("#9B5DE5"),
        };

        ArtCache _art;
        Font _font;
        Camera _cam;
        Vector3 _camBase;
        float _shake;
        public bool ReducedMotion;
#if UNITY_WEBGL && !UNITY_EDITOR
        // Navigateur de téléphone : moitié moins de particules (le jeu reste fluide pendant les combos).
        const bool Lite = true;
#else
        const bool Lite = false;
#endif

        // Particules recyclées : un combo n'instancie plus des dizaines d'objets (à-coups, ramasse-miettes).
        readonly Stack<Particle> _pool = new Stack<Particle>();
        int _live;
        const int MaxLive = 140;

        static int Lighter(int count) => Lite ? Mathf.Max(2, count / 2) : count;

        Particle Spawn(string name, Sprite sprite, Color color, int order, Vector3 pos, Quaternion rot, float scale)
        {
            if (_live >= MaxLive) return null; // déjà assez à l'écran : on n'en ajoute pas
            Particle p = null;
            while (_pool.Count > 0 && p == null) p = _pool.Pop();
            if (p == null)
            {
                var sr = SpriteUtil.Make(name, transform, sprite, color, order);
                p = sr.gameObject.AddComponent<Particle>();
                p.Owner = this;
            }
            else
            {
                p.gameObject.SetActive(true);
                var sr = p.Renderer;
                sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            }
            p.transform.SetPositionAndRotation(pos, rot);
            p.transform.localScale = Vector3.one * scale;
            _live++;
            return p;
        }

        void Recycle(Particle p)
        {
            _live = Mathf.Max(0, _live - 1);
            p.gameObject.SetActive(false);
            _pool.Push(p);
        }
        /// <summary>Décalage de la caméra vers le bas (le monde remonte) : sur l'accueil, pour libérer la barre du bas.</summary>
        public float LiftTarget;
        public float Lift { get; private set; }

        public void Init(ArtCache art, Font font, Camera cam)
        {
            _art = art;
            _font = font;
            _cam = cam;
            _camBase = cam.transform.position;
        }

        public void RecenterCamera() { _camBase = _cam.transform.position; }

        /// <summary>Texte qui jaillit puis s'efface, cerné de sombre pour rester lisible sur tous les fonds.</summary>
        // Textes recyclés : une fusion n'alloue plus dix objets (moins de travail pour le ramasse-miettes, pas d'à-coups).
        readonly Stack<Floater> _textPool = new Stack<Floater>();
        static readonly Color Outline = new Color(0.17f, 0.12f, 0.26f, 1f);

        public void Text(Vector2 pos, string text, Color color, float size = 1f, float duration = 1.1f)
        {
            Floater f = _textPool.Count > 0 ? _textPool.Pop() : null;
            if (f == null)
            {
                var go = new GameObject("Texte");
                go.transform.SetParent(transform, false);
                for (int i = 0; i < 9; i++) MakeText(go.transform, "", Outline, 1f, Vector2.zero, Layers.Effects + 10);
                MakeText(go.transform, "", Color.white, 1f, Vector2.zero, Layers.Effects + 11);
                f = go.AddComponent<Floater>();
                f.Pool = _textPool;
            }
            f.transform.position = pos;
            f.transform.localScale = Vector3.one;
            f.gameObject.SetActive(true);
            float o = 0.045f * size;
            var texts = f.Texts;
            for (int i = 0; i < texts.Length; i++)
            {
                var tm = texts[i];
                bool main = i == texts.Length - 1;
                tm.text = text;
                tm.characterSize = 0.055f * size;
                tm.color = main ? color : Outline;
                tm.transform.localPosition = main ? Vector2.zero : Offsets[i] * o;
            }
            f.Init(duration, ReducedMotion ? 0.4f : 1.4f);
        }

        static readonly Vector2[] Offsets =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 1),
            new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(0, -2.2f),
        };

        void MakeText(Transform parent, string text, Color color, float size, Vector2 offset, int order)
        {
            var go = new GameObject("t");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var tm = go.AddComponent<TextMesh>();
            tm.font = _font;
            tm.text = text;
            tm.fontSize = 96;
            tm.characterSize = 0.055f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (_font != null) mr.sharedMaterial = _font.material;
            mr.sortingOrder = order;
        }

        public void Burst(Vector2 pos, Color color, int count = 8, float radius = 1f)
        {
            if (ReducedMotion) count = Mathf.Max(3, count / 3);
            count = Lighter(count);
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var p = Spawn("Eclat", _art.Spark, Color.Lerp(color, Color.white, 0.4f), Layers.Effects + 5, pos, Quaternion.identity, Random.Range(0.35f, 0.6f) * radius);
                if (p == null) break;
                p.Init(dir * Random.Range(3f, 5.5f) * radius, 0.5f, 0f, 400f, true);
            }
        }

        /// <summary>Onde de choc colorée (fusion).</summary>
        public void Shockwave(Vector2 pos, Color color, float size)
        {
            var sr = SpriteUtil.Make("Onde", transform, _art.Ring, color, Layers.Effects + 4);
            sr.transform.position = pos;
            sr.gameObject.AddComponent<Grower>().Init(size * 0.4f, size * 2.2f, 0.4f);
        }

        /// <summary>Nuage de poussière à l'atterrissage d'un poids.</summary>
        public void Dust(Vector2 pos, float strength)
        {
            int count = ReducedMotion ? 2 : Lighter(Mathf.RoundToInt(Mathf.Lerp(3, 7, strength)));
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var at = pos + new Vector2(side * Random.Range(0.05f, 0.3f), Random.Range(-0.05f, 0.1f));
                var p = Spawn("Poussiere", _art.Puff, new Color(1f, 0.97f, 0.9f, 0.9f), Layers.Effects, at, Quaternion.identity, Random.Range(0.25f, 0.45f) * (0.7f + strength));
                if (p == null) break;
                p.Init(new Vector2(side * Random.Range(1f, 2.4f), Random.Range(0.3f, 1.2f)), 0.55f, -1.5f, 0f, false, 1.6f);
            }
        }

        /// <summary>Goutte de sueur qui s'envole de la tempe (mascotte en difficulté).</summary>
        public void SweatDrop(Vector2 pos, int side)
        {
            if (ReducedMotion) return;
            var p = Spawn("Sueur", _art.Sweat, Color.white, Layers.Effects, pos, Quaternion.identity, 1f);
            p?.Init(new Vector2(side * Random.Range(1.2f, 2.2f), Random.Range(1.5f, 2.5f)), 0.7f, -9f, side * -200f, false);
        }

        /// <summary>Pluie de confettis (record, 128, nouvelle tête).</summary>
        public void Confetti(Vector2 from, int count = 40)
        {
            if (ReducedMotion) count /= 4;
            count = Lighter(count);
            for (int i = 0; i < count; i++)
            {
                var at = from + new Vector2(Random.Range(-0.5f, 0.5f), 0);
                var p = Spawn("Confetti", _art.Confetti, ConfettiColors[i % ConfettiColors.Length], Layers.Effects + 3, at, Quaternion.Euler(0, 0, Random.Range(0, 360f)), 1f);
                if (p == null) break;
                var v = new Vector2(Random.Range(-5f, 5f), Random.Range(5f, 11f));
                p.Init(v, Random.Range(1.6f, 2.4f), -9f, Random.Range(-600f, 600f), false, 0.7f);
            }
        }

        /// <summary>Le 128 devient une montgolfière qui s'envole.</summary>
        public void Balloon(Vector2 pos)
        {
            var root = new GameObject("Montgolfiere");
            root.transform.SetParent(transform, false);
            root.transform.position = pos;
            SpriteUtil.Make("Ballon", root.transform, _art.Balloon, Color.white, Layers.Effects + 1).transform.localPosition = new Vector3(0, 0.5f, 0);
            var label = new GameObject("128");
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0, 0.85f, 0);
            var tm = label.AddComponent<TextMesh>();
            tm.font = _font; tm.text = "128"; tm.fontSize = 96; tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter; tm.color = Color.white;
            var mr = label.GetComponent<MeshRenderer>();
            if (_font != null) mr.sharedMaterial = _font.material;
            mr.sortingOrder = Layers.Effects + 2;
            root.AddComponent<BalloonFlight>();
        }

        public void Shake(float amount)
        {
            if (ReducedMotion) amount *= 0.25f;
            _shake = Mathf.Max(_shake, amount);
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _pool.Clear();
            _live = 0;
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            float d = LiftTarget - Lift;
            Lift = Mathf.Abs(d) < 0.002f ? LiftTarget : Lift + d * Mathf.Min(1f, Time.unscaledDeltaTime * 6f);
            var basePos = _camBase - new Vector3(0, Lift, 0);
            if (_shake > 0.001f)
            {
                _shake = Mathf.MoveTowards(_shake, 0, Time.unscaledDeltaTime * 1.5f);
                var o = Random.insideUnitCircle * _shake * 0.35f;
                _cam.transform.position = basePos + new Vector3(o.x, o.y, 0);
            }
            else _cam.transform.position = basePos;
        }

        sealed class Floater : MonoBehaviour
        {
            float _life, _total, _rise;
            TextMesh[] _texts;
            Color[] _colors;

            public void Init(float duration, float rise)
            {
                _life = _total = duration; _rise = rise;
                _texts = Texts;
                if (_colors == null || _colors.Length != _texts.Length) _colors = new Color[_texts.Length];
                for (int i = 0; i < _texts.Length; i++) _colors[i] = _texts[i].color;
            }

            public Stack<Floater> Pool;
            TextMesh[] _cache;
            public TextMesh[] Texts => _cache ?? (_cache = GetComponentsInChildren<TextMesh>(true));

            void Update()
            {
                _life -= Time.unscaledDeltaTime;
                float t = 1f - _life / _total;
                transform.position += Vector3.up * _rise * Time.unscaledDeltaTime * (1f - t);
                float s = t < 0.12f ? Mathf.Lerp(0.3f, 1.25f, t / 0.12f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.12f) * 5f));
                transform.localScale = new Vector3(s, s, 1);
                float a = t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f;
                for (int i = 0; i < _texts.Length; i++) { var c = _colors[i]; c.a *= a; _texts[i].color = c; }
                if (_life <= 0)
                {
                    if (Pool != null && Pool.Count < 24) { gameObject.SetActive(false); Pool.Push(this); }
                    else Destroy(gameObject);
                }
            }
        }

        sealed class Particle : MonoBehaviour
        {
            public Effects Owner;
            public SpriteRenderer Renderer => _sr != null ? _sr : (_sr = GetComponent<SpriteRenderer>());
            Vector2 _v;
            float _life, _total, _gravity, _spin, _grow;
            bool _drag;
            SpriteRenderer _sr;
            Vector3 _scale;
            float _alpha;

            public void Init(Vector2 velocity, float life, float gravity, float spin, bool drag, float grow = 1f)
            {
                _v = velocity; _life = _total = life; _gravity = gravity; _spin = spin; _drag = drag; _grow = grow;
                _sr = GetComponent<SpriteRenderer>();
                _scale = transform.localScale;
                _alpha = _sr.color.a;
            }

            void Update()
            {
                float dt = Time.deltaTime;
                _life -= dt;
                _v.y += _gravity * dt;
                if (_drag) _v *= 1f - 4f * dt;
                transform.position += (Vector3)(_v * dt);
                transform.Rotate(0, 0, _spin * dt);
                float t = 1f - Mathf.Clamp01(_life / _total);
                transform.localScale = _scale * Mathf.Lerp(1f, _grow, t);
                var c = _sr.color; c.a = _alpha * Mathf.Clamp01(_life / (_total * 0.5f)); _sr.color = c;
                if (_life <= 0)
                {
                    if (Owner != null) Owner.Recycle(this); else Destroy(gameObject);
                }
            }
        }

        sealed class Grower : MonoBehaviour
        {
            float _from, _to, _life, _total;
            SpriteRenderer _sr;

            public void Init(float from, float to, float life) { _from = from; _to = to; _life = _total = life; _sr = GetComponent<SpriteRenderer>(); }

            void Update()
            {
                _life -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_life / _total);
                float e = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.one * Mathf.Lerp(_from, _to, e);
                var c = _sr.color; c.a = 1f - t; _sr.color = c;
                if (_life <= 0) Destroy(gameObject);
            }
        }

        sealed class BalloonFlight : MonoBehaviour
        {
            float _t;
            Vector3 _start;

            void Start() { _start = transform.position; }

            void Update()
            {
                _t += Time.deltaTime;
                float y = _start.y + _t * _t * 1.8f + _t * 1.2f;
                float x = _start.x + Mathf.Sin(_t * 2.4f) * 0.4f;
                transform.position = new Vector3(x, y, 0);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 2.4f + 0.6f) * 8f);
                float s = Mathf.Min(1f, _t * 4f);
                transform.localScale = new Vector3(s, s, 1);
                if (_t > 4f) Destroy(gameObject);
            }
        }
    }
}
