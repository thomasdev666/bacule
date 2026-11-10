using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule
{
    /// <summary>
    /// Décor : ciel en dégradé, soleil (ou lune), nuages qui dérivent, collines, prairie.
    /// L'ambiance avance dans la journée avec le score (matin → nuit) avec un fondu enchaîné.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        ArtCache _art;
        Camera _cam;
        Layer _current, _previous;
        /// <summary>Étape du décor (Difficulty.BackgroundIndex) : au-delà de la Lune, un monde déjà vu dans une autre teinte.</summary>
        int _theme = -1;
        float _fade, _lift;

        /// <summary>La caméra descend de <paramref name="lift"/> unités (accueil) : la prairie s'agrandit pour couvrir le bas.</summary>
        public void SetLift(float lift)
        {
            if (Mathf.Abs(lift - _lift) < 0.001f) return;
            _lift = lift;
            Layout();
        }

        sealed class Layer
        {
            public Transform Root;
            public readonly List<SpriteRenderer> Renderers = new List<SpriteRenderer>();
            public readonly List<SpriteRenderer> Clouds = new List<SpriteRenderer>();
            public SpriteRenderer Stars;
            public int Art;
            public Color Tint = Color.white;

            public void SetAlpha(float a)
            {
                var c = Tint; c.a = a;
                foreach (var r in Renderers) r.color = c;
            }
        }

        static readonly List<Backdrop> Live = new List<Backdrop>();
        void Awake() => Live.Add(this);
        void OnDestroy() => Live.Remove(this);

        public void Init(ArtCache art, Camera cam)
        {
            _art = art;
            _cam = cam;
            SetTheme(0, true);
        }

        public void SetTheme(int stage, bool instant = false)
        {
            stage = Mathf.Max(0, stage);
            if (stage == _theme) return;
            _theme = stage;
            if (_previous != null) Destroy(_previous.Root.gameObject);
            _previous = _current;
            if (_previous != null) foreach (var r in _previous.Renderers) r.sortingOrder -= 10; // l'ancien passe dessous
            int art = Theme.ArtOf(stage);
            _current = Build(Theme.All[art], art);
            var tint = Theme.TintOf(stage);
            _current.Tint = new Color(tint.r / 255f, tint.g / 255f, tint.b / 255f, 1f);
            _fade = instant ? 1f : 0f;
            _current.SetAlpha(_fade);
            if (instant && _previous != null) { Destroy(_previous.Root.gameObject); _previous = null; }
            Layout();
            ReleaseUnused();
        }

        /// <summary>
        /// Libère les images des mondes qui ne sont ni affichés ni le prochain : avec 14 mondes, tout garder en mémoire
        /// coûterait cher sur un petit téléphone.
        /// </summary>
        void ReleaseUnused()
        {
            if (_art == null) return;
            // Plusieurs décors partagent les images (le sien et celui de l'adversaire en duel) : on garde tout ce que
            // l'un d'eux affiche ou va afficher.
            var keep = KeepBuffer;
            keep.Clear();
            // Le premier monde (prairie du matin) reste toujours chargé : c'est celui de l'accueil et du début de chaque
            // partie. Sans cela, il était relu sur le disque d'un seul coup à chaque retour d'un monde lointain (à-coup).
            keep.Add(0);
            foreach (var b in Live)
            {
                if (b._current != null) keep.Add(b._current.Art);
                if (b._previous != null) keep.Add(b._previous.Art);
                if (b._theme >= 0) keep.Add(Theme.ArtOf(b._theme + 1));
            }
            for (int i = 0; i < Theme.All.Length; i++)
            {
                if (keep.Contains(i)) continue;
                foreach (var it in SceneItems(Theme.All[i], i))
                    if (it.Key != "stars") _art.ReleaseScene(it.Key);
            }
        }

        static readonly HashSet<int> KeepBuffer = new HashSet<int>();

        struct SceneItem { public string Name, Key; public System.Func<Raster> Draw; public float Ppu; public Vector2? Pivot; public int Order; }

        /// <summary>Les images d'une ambiance. Dessinées en haute résolution (2 à 3 fois la référence) : nettes sur les grands écrans.</summary>
        static List<SceneItem> SceneItems(Theme t, int index)
        {
            string k = "t" + index;
            var list = new List<SceneItem>
            {
                new SceneItem { Name = "Ciel", Key = k + "sky", Draw = () => SceneArtist.Sky(t, 1024), Ppu = 1024 / 5.12f, Pivot = new Vector2(0.5f, 0.5f), Order = 0 },
                new SceneItem { Name = "Soleil", Key = k + "sun", Draw = () => SceneArtist.Sun(t, 512), Ppu = 512 / 3.4f, Order = 2 },
            };
            if (t.Night) list.Add(new SceneItem { Name = "Etoiles", Key = "stars", Draw = () => SceneArtist.Stars(), Ppu = 100, Order = 1 });
            for (int i = 0; i < 3 && !t.Space; i++)
            {
                int ci = i;
                list.Add(new SceneItem { Name = "Nuage", Key = k + "cloud" + i, Draw = () => SceneArtist.Cloud(ci, t, 512, 256), Ppu = 512 / (3.4f - ci * 0.5f), Order = 3 });
            }
            list.Add(new SceneItem { Name = "CollinesLoin", Key = k + "far", Draw = () => SceneArtist.Hills(t, true, 1536, 480), Ppu = 200, Pivot = new Vector2(0.5f, 0f), Order = 4 });
            list.Add(new SceneItem { Name = "CollinesPres", Key = k + "near", Draw = () => SceneArtist.Hills(t, false, 1536, 480), Ppu = 200, Pivot = new Vector2(0.5f, 0f), Order = 5 });
            list.Add(new SceneItem { Name = "Prairie", Key = k + "ground", Draw = () => SceneArtist.Ground(t, 1536, 540), Ppu = 300, Pivot = new Vector2(0.5f, 0.82f), Order = 6 });
            return list;
        }

        /// <summary>Toutes les ambiances, pour les dessiner en arrière-plan au lancement.</summary>
        public static IEnumerable<KeyValuePair<string, System.Func<Raster>>> AllSceneJobs()
        {
            for (int i = 0; i < Theme.All.Length; i++)
                foreach (var it in SceneItems(Theme.All[i], i))
                    yield return new KeyValuePair<string, System.Func<Raster>>("scene_" + it.Key, it.Draw);
        }

        /// <summary>Charge en mémoire les images d'une étape du décor (sans l'afficher), pour qu'elle apparaisse sans à-coup.</summary>
        public System.Collections.IEnumerator Preload(int stage)
        {
            int index = Theme.ArtOf(Mathf.Max(0, stage));
            var t = Theme.All[index];
            foreach (var it in SceneItems(t, index))
            {
                _art.Scene(it.Key, it.Draw, it.Ppu, it.Pivot);
                yield return null;
            }
        }

        Layer Build(Theme t, int index)
        {
            var layer = new Layer { Root = new GameObject("Decor " + t.Name).transform, Art = index };
            layer.Root.SetParent(transform, false);
            // Le nouveau décor se dessine par-dessus l'ancien pendant le fondu.
            int o = Layers.Background + 10;

            SpriteRenderer Add(string name, Sprite sprite, int order)
            {
                var sr = SpriteUtil.Make(name, layer.Root, sprite, Color.white, order);
                layer.Renderers.Add(sr);
                return sr;
            }

            foreach (var it in SceneItems(t, index))
            {
                var sr = Add(it.Name, _art.Scene(it.Key, it.Draw, it.Ppu, it.Pivot), o + it.Order);
                if (it.Name == "Etoiles") layer.Stars = sr;
                if (it.Name == "Nuage") layer.Clouds.Add(sr);
            }
            return layer;
        }

        /// <summary>Adapte le décor à la taille de l'écran.</summary>
        public void Layout()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                var layer = pass == 0 ? _previous : _current;
                if (layer == null) continue;
                float hw = WorldLayout.HalfWidth(_cam), size = _cam.orthographicSize, ground = WorldLayout.GroundY(_cam);
                foreach (var r in layer.Renderers)
                {
                    var sp = r.sprite;
                    float sw = sp.bounds.size.x, sh = sp.bounds.size.y;
                    switch (r.name)
                    {
                        case "Ciel":
                            r.transform.localPosition = Vector3.zero;
                            r.transform.localScale = new Vector3((hw * 2f + 1f) / sw, (size * 2f + 1f) / sh, 1);
                            break;
                        case "Etoiles":
                            r.transform.localPosition = new Vector3(0, size * 0.45f, 0);
                            r.transform.localScale = Vector3.one * (hw * 2f / sw);
                            break;
                        case "Soleil":
                            r.transform.localPosition = new Vector3(hw * 0.45f, size * 0.68f, 0);
                            break;
                        case "CollinesLoin":
                            r.transform.localPosition = new Vector3(0, ground + 1.0f, 0);
                            r.transform.localScale = Vector3.one * (hw * 2f / sw);
                            break;
                        case "CollinesPres":
                            r.transform.localPosition = new Vector3(0, ground - 0.6f, 0);
                            r.transform.localScale = Vector3.one * (hw * 2.2f / sw);
                            break;
                        case "Prairie":
                            r.transform.localPosition = new Vector3(0, ground + 0.35f, 0);
                            r.transform.localScale = Vector3.one * ((hw * 2f + 0.5f) / sw) * (1f + 0.4f * Mathf.Max(0f, _lift));
                            break;
                    }
                }
                for (int i = 0; i < layer.Clouds.Count; i++)
                    layer.Clouds[i].transform.localPosition = new Vector3((i == 1 ? 0.3f : -0.35f) * hw * 2f * (i == 2 ? 0.6f : 1f), size * (0.8f - i * 0.4f), 0);
            }
        }

        /// <summary>Vent (unités/s, signé) : les nuages filent plus vite dans son sens pendant une rafale.</summary>
        public float Wind;
        float _wind;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _wind = Mathf.Lerp(_wind, Wind, 1f - Mathf.Exp(-dt * 2.5f));
            if (_fade < 1f)
            {
                _fade = Mathf.MoveTowards(_fade, 1f, dt / 1.8f);
                _current.SetAlpha(_fade);
                if (_fade >= 1f && _previous != null) { Destroy(_previous.Root.gameObject); _previous = null; ReleaseUnused(); }
            }
            float hw = WorldLayout.HalfWidth(_cam) + 2.5f;
            // Pas de tableau créé à chaque image (deux décors tournent pendant un duel).
            for (int pass = 0; pass < 2; pass++)
            {
                var layer = pass == 0 ? _previous : _current;
                if (layer == null) continue;
                for (int i = 0; i < layer.Clouds.Count; i++)
                {
                    var p = layer.Clouds[i].transform.localPosition;
                    p.x += dt * (0.12f + i * 0.06f + _wind * (1f + i * 0.35f));
                    if (p.x > hw) p.x = -hw;
                    else if (p.x < -hw) p.x = hw;
                    layer.Clouds[i].transform.localPosition = p;
                }
                if (layer.Stars != null)
                    layer.Stars.transform.localScale = Vector3.one * (WorldLayout.HalfWidth(_cam) * 2f / layer.Stars.sprite.bounds.size.x) * (1f + Mathf.Sin(Time.unscaledTime * 1.3f) * 0.01f);
            }
        }
    }
}
