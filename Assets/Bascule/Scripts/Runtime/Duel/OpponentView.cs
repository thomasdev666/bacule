using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Duel
{
    /// <summary>
    /// La partie de l'adversaire, en direct. Ce n'est pas une vidéo : c'est une vraie petite scène du jeu (son Basile
    /// avec sa tête, sa planche avec son apparence, ses poids, le décor de son score), rejouée à partir des photos
    /// reçues 15 fois par seconde et filmée par une seconde caméra dans une texture. Résultat : une image nette, à la
    /// même qualité que la sienne, avec très peu de données réseau.
    /// La scène est posée loin du monde du joueur (x = 2000) : aucune des deux caméras ne voit l'autre monde.
    /// </summary>
    public sealed class OpponentView : MonoBehaviour
    {
        const float Offset = 2000f;
        const float OrthoSize = 6.2f;
        const int TexWidth = 640, TexHeight = 640; // carré : le cadre de la vue en direct

        ArtCache _art;
        Font _font;
        Tuning _t;
        Camera _cam;
        Backdrop _backdrop;
        MascotView _mascot;
        SpriteRenderer _plank;
        WeightBody _held;
        int _heldValue;
        readonly Dictionary<ushort, WeightBody> _weights = new Dictionary<ushort, WeightBody>();
        readonly HashSet<ushort> _seen = new HashSet<ushort>();
        readonly List<ushort> _gone = new List<ushort>();
        int _theme = -1, _plankSkin = -1;
        float _plankLength;
        bool _defeated;
        float _renderTimer;

        public RenderTexture Texture { get; private set; }

        public static OpponentView Create(Transform parent, ArtCache art, Font font, Tuning tuning)
        {
            var go = new GameObject("Adversaire");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(Offset, 0, 0);
            var v = go.AddComponent<OpponentView>();
            v.Build(art, font, tuning);
            return v;
        }

        void Build(ArtCache art, Font font, Tuning tuning)
        {
            _art = art; _font = font; _t = tuning;
            Texture = new RenderTexture(TexWidth, TexHeight, 16, RenderTextureFormat.ARGB32) { name = "VueAdversaire", antiAliasing = 2 };
            Texture.Create();

            var camGo = new GameObject("CameraAdversaire");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0, 0, -10);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = OrthoSize;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.12f, 0.12f, 0.24f, 1f);
            _cam.targetTexture = Texture;
            _cam.nearClipPlane = 0.1f; _cam.farClipPlane = 50f;
            // On la fait tourner nous-mêmes (30 images/s suffisent pour une petite vue, et ça ménage la batterie).
            _cam.enabled = false;

            var bd = new GameObject("Decor");
            bd.transform.SetParent(transform, false);
            _backdrop = bd.AddComponent<Backdrop>();
            _backdrop.Init(art, _cam);

            _mascot = MascotView.Create(transform, art);
            _mascot.transform.position = new Vector3(Offset, WorldLayout.GroundY(_cam), 0);

            var plankGo = new GameObject("Planche");
            plankGo.transform.SetParent(transform, false);
            _plank = SpriteUtil.Make("Bois", plankGo.transform, null, Color.white, Layers.Plank);
            _plank.drawMode = SpriteDrawMode.Sliced;
            SetPlankSkin(0);
            gameObject.SetActive(false);
        }

        /// <summary>Prépare la vue pour un nouvel adversaire.</summary>
        public void Begin(int head, int plankSkin)
        {
            gameObject.SetActive(true);
            Clear();
            _defeated = false;
            _mascot.ResetPose();
            _mascot.SetHead(HeadCatalog.Get(head));
            SetPlankSkin(plankSkin);
            _theme = -1;
            SetTheme(0);
            _cam.Render();
        }

        public void End()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>L'adversaire a lâché : son Basile tombe, comme chez lui.</summary>
        public void Defeat(float direction)
        {
            if (_defeated) return;
            _defeated = true;
            _mascot.LetGo(direction);
            if (_held != null) { Destroy(_held.gameObject); _held = null; _heldValue = 0; }
        }

        /// <summary>L'adversaire a gagné : son Basile lève les bras.</summary>
        public void Cheer() { if (!_defeated) _mascot.Cheer(); }

        void Clear()
        {
            foreach (var w in _weights.Values) if (w != null) Destroy(w.gameObject);
            _weights.Clear();
            if (_held != null) Destroy(_held.gameObject);
            _held = null; _heldValue = 0;
        }

        void SetPlankSkin(int id)
        {
            if (id == _plankSkin) return;
            _plankSkin = id;
            const float ppu = 160f;
            var raster = SceneArtist.Plank(_t.plank.startLength, _t.plank.thickness, ppu, out int border, PlankCatalog.Get(id));
            _plank.sprite = ArtCache.ToSprite(raster, ppu, new Vector4(border, 0, border, 0));
            _plankLength = -1f;
        }

        void SetTheme(int index)
        {
            if (index == _theme) return;
            bool first = _theme < 0;
            _theme = index;
            _backdrop.SetTheme(index, first);
        }

        static WeightBody MakeWeight(Transform parent, int value, ArtCache art, Font font, Tuning t)
        {
            var w = WeightBody.Create(parent, value, art, font, t.weights, null);
            // Simple image : pas de physique dans la vue de l'adversaire.
            w.Body.simulated = false;
            w.SetHeld(false);
            return w;
        }

        /// <summary>Met la scène à jour avec la photo interpolée de l'adversaire.</summary>
        public void Apply(DuelSnapshot s, float dt)
        {
            if (s == null || !gameObject.activeSelf) return;
            SetTheme(Difficulty.BackgroundIndex(s.Score, Theme.All.Length));
            float len = Difficulty.PlankLength(s.Score, _t.plank);
            if (Mathf.Abs(len - _plankLength) > 0.01f)
            {
                _plankLength = len;
                const float pad = 12f / 160f;
                _plank.size = new Vector2(len + pad, _t.plank.thickness + pad);
            }

            _mascot.Tick(dt, s.Angle, _defeated ? 0f : s.Danger, Mathf.Clamp01(s.Weights.Count / 30f), _t.mascot);
            var pivot = _mascot.PivotWorld;
            if (!_defeated)
            {
                float rad = -s.Angle * Mathf.Deg2Rad;
                var along = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                var normal = new Vector2(-along.y, along.x);
                _plank.transform.parent.SetPositionAndRotation(pivot + normal * (_t.plank.thickness * 0.5f), Quaternion.Euler(0, 0, -s.Angle));
            }
            else
            {
                // La planche glisse et tombe avec Basile.
                var tr = _plank.transform.parent;
                tr.position += Vector3.down * dt * 4f;
                tr.rotation = Quaternion.RotateTowards(tr.rotation, Quaternion.Euler(0, 0, Mathf.Sign(s.Angle) * -70f), 140f * dt);
            }

            // Poids : on crée ceux qui apparaissent, on déplace les autres, on retire ceux qui ont disparu (fusion, chute).
            _seen.Clear();
            foreach (var d in s.Weights)
            {
                _seen.Add(d.Id);
                if (!_weights.TryGetValue(d.Id, out var w) || w == null || w.Value != d.Value)
                {
                    if (w != null) Destroy(w.gameObject);
                    w = MakeWeight(transform, d.Value, _art, _font, _t);
                    w.PopIn(0.25f);
                    _weights[d.Id] = w;
                }
                var p = _defeated ? (Vector2)w.transform.position + Vector2.down * dt * 5f : pivot + new Vector2(d.X, d.Y);
                w.transform.SetPositionAndRotation(p, Quaternion.Euler(0, 0, d.Rotation));
            }
            _gone.Clear();
            foreach (var id in _weights.Keys) if (!_seen.Contains(id)) _gone.Add(id);
            foreach (var id in _gone) { if (_weights[id] != null) Destroy(_weights[id].gameObject); _weights.Remove(id); }

            // Poids tenu, en haut, à l'endroit où il vise.
            if (!_defeated && s.HeldValue != _heldValue)
            {
                if (_held != null) Destroy(_held.gameObject);
                _held = s.HeldValue > 0 ? MakeWeight(transform, s.HeldValue, _art, _font, _t) : null;
                if (_held != null) { _held.SetHeld(true); _held.PopIn(0.3f); }
                _heldValue = s.HeldValue;
            }
            if (_held != null)
                _held.transform.position = new Vector3(pivot.x + s.HeldX, _cam.transform.position.y + OrthoSize - 1.15f + Mathf.Sin(Time.time * 3f) * 0.05f, 0);
        }

        void LateUpdate()
        {
            if (!gameObject.activeSelf) return;
            _renderTimer -= Time.unscaledDeltaTime;
            if (_renderTimer > 0f) return;
            _renderTimer = 1f / 30f;
            _cam.Render();
        }

        void OnDestroy()
        {
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
        }
    }
}
