using System.IO;
using Bascule.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>
    /// Carte de score à partager, au format vertical 9:16 des Stories et de TikTok (docs/PARTAGE.md) :
    /// la photo de la chute dans un polaroïd, le titre de la partie, le score géant, la frise de tension
    /// (les mêmes cases que le résumé texte) et le code défi pour rejouer exactement la même partie.
    /// Rendue dans une texture puis enregistrée en PNG, seulement quand le joueur partage.
    /// </summary>
    public sealed class ScoreCard
    {
        public const int Width = 1080, Height = 1920;
        const float FarAway = 5000f;
        const float PhotoW = 760f, PhotoH = 860f;
        static readonly Color[] LogoColors = { Palette.Primary, Palette.Accent, Palette.Play, Palette.Secondary, Palette.Purple, Palette.Primary, Palette.Accent };
        static readonly Color[] TensionColors = { ArtCache.Hex("#5BD16B"), ArtCache.Hex("#FFD447"), ArtCache.Hex("#FF9A3C"), ArtCache.Hex("#F2545B") };

        readonly ArtCache _art;
        Camera _cam;
        Canvas _canvas;
        RenderTexture _rt;
        Image _sky, _hills, _ground, _head, _badge, _rays;
        RawImage _photo;
        GameObject _polaroid;
        Text _score, _caption, _line, _extra, _code, _site;
        readonly Image[] _squares = new Image[Brag.MaxSquares + 1];

        public Texture2D Texture { get; private set; }
        public string Path { get; private set; }

        public ScoreCard(ArtCache art) { _art = art; }

        void Build()
        {
            float scale = Ui.TextScale;
            Ui.TextScale = 1f; // la carte garde toujours la même mise en page

            _rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { name = "CarteDeScore" };
            var camGo = new GameObject("CameraCarte");
            Object.DontDestroyOnLoad(camGo);
            camGo.transform.position = new Vector3(FarAway, 0, -10);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.black;
            _cam.cullingMask = 1 << 5;
            _cam.targetTexture = _rt;
            _cam.enabled = false;

            var canvasGo = new GameObject("CanvasCarte", typeof(RectTransform));
            Object.DontDestroyOnLoad(canvasGo);
            canvasGo.layer = 5;
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _cam;
            _canvas.planeDistance = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            _sky = Ui.Image(root, null, Color.white, "Ciel");
            Ui.Stretch(_sky.rectTransform);
            _hills = Ui.Image(root, null, Color.white, "Collines");
            Ui.Place(_hills.rectTransform, 0.5f, 0, 0, 170, Width * 1.1f, 400, 0.5f, 0);
            _ground = Ui.Image(root, null, Color.white, "Prairie");
            Ui.Place(_ground.rectTransform, 0.5f, 0, 0, -40, Width + 20, 480, 0.5f, 0);
            // Voile sombre : le polaroïd (même ciel) ressort du fond.
            var dim = Ui.Image(root, null, new Color(0.1f, 0.08f, 0.2f, 0.42f), "Voile");
            Ui.Stretch(dim.rectTransform);

            // Logo coloré : le nom du jeu doit se lire même en vignette de 200 pixels.
            string word = "BASCULE";
            for (int i = 0; i < word.Length; i++)
            {
                var l = Ui.Title(root, word[i].ToString(), 130, LogoColors[i]);
                Ui.Place(l.rectTransform, 0.5f, 1, (i - 3) * 112f, -120 + Mathf.Sin(i * 0.9f) * 10f, 140, 170);
                l.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(i * 1.3f) * 5f);
            }

            _rays = Ui.Image(root, _art.Rays, new Color(1, 1, 1, 0.5f), "Rayons");
            Ui.Place(_rays.rectTransform, 0.5f, 1, 0, -720, 1300, 1300);

            // Sans photo (partage avant toute chute) : la tête de la mascotte en grand, comme avant.
            _head = Ui.Icon(root, null, "Tete");
            Ui.Place(_head.rectTransform, 0.5f, 1, 0, -680, 640, 640);

            // Polaroïd : la vraie photo de la chute, légèrement de travers.
            var frame = Ui.Card(root, "Polaroid");
            _polaroid = frame.gameObject;
            Ui.Place(frame.rectTransform, 0.5f, 1, 0, -230, PhotoW + 60, PhotoH + 150, 0.5f, 1);
            frame.rectTransform.localRotation = Quaternion.Euler(0, 0, -2.5f);
            var photoGo = new GameObject("Photo", typeof(RectTransform));
            photoGo.transform.SetParent(frame.transform, false);
            _photo = photoGo.AddComponent<RawImage>();
            _photo.raycastTarget = false;
            Ui.Place(_photo.rectTransform, 0.5f, 1, 0, -30, PhotoW, PhotoH, 0.5f, 1);
            _caption = Ui.Label(frame.transform, "", 52, Palette.Ink);
            Ui.Place(_caption.rectTransform, 0.5f, 0, -70, 14, PhotoW - 200, 96, 0.5f, 0);
            _caption.resizeTextForBestFit = true; _caption.resizeTextMinSize = 34; _caption.resizeTextMaxSize = 52;
            _caption.fontStyle = FontStyle.Bold;

            // Badge de la tête portée, épinglé sur le coin du polaroïd.
            _badge = Ui.Icon(frame.transform, null, "Badge");
            Ui.Place(_badge.rectTransform, 1, 0, -10, 70, 280, 280, 0.5f, 0.5f);
            _badge.rectTransform.localRotation = Quaternion.Euler(0, 0, 10f);

            _score = Ui.Title(root, "", 200, Palette.Accent);
            Ui.Place(_score.rectTransform, 0.5f, 1, 0, -1360, 1000, 220);
            _score.horizontalOverflow = HorizontalWrapMode.Overflow;
            _line = Ui.Title(root, "", 54, Color.white);
            Ui.Place(_line.rectTransform, 0.5f, 1, 0, -1490, 1000, 70);
            _extra = Ui.Title(root, "", 46, ArtCache.Hex("#BFFFE0"));
            Ui.Place(_extra.rectTransform, 0.5f, 1, 0, -1560, 1000, 64);

            // Frise de tension : vert = calme, rouge = à deux doigts de tomber, la dernière case = la chute.
            for (int i = 0; i < _squares.Length; i++)
            {
                _squares[i] = Ui.Image(root, _art.Button, Color.white, "Case", true);
                _squares[i].pixelsPerUnitMultiplier = 3f; // coins arrondis à l'échelle d'une petite case
                Ui.Place(_squares[i].rectTransform, 0.5f, 1, 0, -1640, 62, 62);
            }

            var banner = Ui.Image(root, _art.Button, Palette.Primary, "Bandeau", true);
            Ui.Place(banner.rectTransform, 0.5f, 0, 0, 36, 1000, 190, 0.5f, 0);
            _code = Ui.Title(banner.transform, "", 50);
            Ui.Stretch(_code.rectTransform, 30, 14, 30, 70);
            _code.resizeTextForBestFit = true; _code.resizeTextMinSize = 30; _code.resizeTextMaxSize = 50;
            _site = Ui.Title(banner.transform, Brag.Site, 40, ArtCache.Hex("#FFE8B0"));
            Ui.Place(_site.rectTransform, 0.5f, 0, 0, 22, 900, 52, 0.5f, 0);

            canvasGo.SetActive(false);
            Ui.TextScale = scale;
        }

        /// <summary>Fabrique la carte et l'enregistre (persistentDataPath/bascule_score.png).</summary>
        public Texture2D Render(BragInfo b, HeadDef head, string mascotName, Texture photo, float focusY)
        {
            if (_cam == null) Build();
            int ti = Difficulty.BackgroundIndex(b.Score, Theme.All.Length);
            var t = Theme.All[ti];
            string k = "t" + ti;
            _sky.sprite = _art.Scene(k + "sky", () => SceneArtist.Sky(t), 100, new Vector2(0.5f, 0.5f));
            _hills.sprite = _art.Scene(k + "near", () => SceneArtist.Hills(t, false, 768, 240), 100, new Vector2(0.5f, 0f));
            _ground.sprite = _art.Scene(k + "ground", () => SceneArtist.Ground(t, 512, 180), 100, new Vector2(0.5f, 0.82f));

            bool hasPhoto = photo != null;
            _polaroid.SetActive(hasPhoto);
            _head.gameObject.SetActive(!hasPhoto);
            _rays.rectTransform.anchoredPosition = new Vector2(0, hasPhoto ? -720 : -680);
            var portrait = _art.Portrait(head);
            _head.sprite = portrait;
            _badge.sprite = portrait;
            if (hasPhoto)
            {
                // On recadre la photo (format de l'écran) autour de la planche et de la tour.
                _photo.texture = photo;
                float uvH = Mathf.Min(1f, (photo.width / (PhotoW / PhotoH)) / photo.height);
                float y0 = Mathf.Clamp(focusY - uvH * 0.5f, 0f, 1f - uvH);
                _photo.uvRect = new Rect(0, y0, 1, uvH);
            }
            _caption.text = Loc.Format("title_label", Brag.Title(b));

            _score.text = Loc.FormatScore(b.Score);
            _line.text = b.NewBest ? Loc.Get("new_best") : mascotName + " · " + head.Name(Loc.Language);
            int? gap = Brag.PointsFromBest(b);
            _extra.text = b.BestSave >= 0.85f ? Loc.Format("near_miss", NearMissTracker.DisplayPercent(b.BestSave))
                : gap.HasValue ? Loc.Format("close_to_best", Loc.FormatScore(gap.Value)) : "";

            var peaks = Brag.Compress(b.Tension);
            int n = peaks.Count + 1;
            float step = 74f;
            for (int i = 0; i < _squares.Length; i++)
            {
                bool on = i < n;
                _squares[i].gameObject.SetActive(on);
                if (!on) continue;
                _squares[i].rectTransform.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * step, -1640);
                if (i < peaks.Count)
                {
                    float d = peaks[i];
                    _squares[i].color = TensionColors[d < 0.5f ? 0 : d < 0.75f ? 1 : d < 0.9f ? 2 : 3];
                    _squares[i].rectTransform.localRotation = Quaternion.identity;
                }
                else
                {
                    _squares[i].color = Palette.Dark; // la chute
                    _squares[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);
                }
            }

            _code.text = b.Mode == GameMode.Daily
                ? Loc.Format("card_daily", Brag.DayNumber(b.DayKey))
                : Loc.Get("card_challenge") + "  " + ChallengeCode.Encode(b.Seed);

            _canvas.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            FitText.FitAll(_canvas.transform); // la carte est rendue tout de suite : on ajuste les textes avant
            Canvas.ForceUpdateCanvases();
            _cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = _rt;
            if (Texture == null) Texture = new Texture2D(Width, Height, TextureFormat.RGB24, false) { name = "Carte" };
            Texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            Texture.Apply();
            RenderTexture.active = previous;
            _canvas.gameObject.SetActive(false);

            Path = System.IO.Path.Combine(Application.persistentDataPath, "bascule_score.png");
            try { File.WriteAllBytes(Path, Texture.EncodeToPNG()); }
            catch (System.Exception e) { Debug.LogWarning("[Bascule] Carte non enregistrée : " + e.Message); Path = null; }
            return Texture;
        }
    }
}
