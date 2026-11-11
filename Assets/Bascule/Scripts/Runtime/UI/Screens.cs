using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Bascule.UI
{
    /// <summary>Base d'un écran : racine plein écran, voile optionnel, contenu dans la zone sûre.</summary>
    public abstract class UiScreen
    {
        protected GameApp App;
        protected ArtCache Art => App.Art;
        public RectTransform Root { get; private set; }
        protected RectTransform Safe;
        public bool Visible => Root != null && Root.gameObject.activeSelf;

        public void Create(GameApp app, Transform canvas, Color? background)
        {
            App = app;
            Root = Ui.Stretch(Ui.Rect(GetType().Name, canvas));
            if (background.HasValue)
            {
                var bg = Root.gameObject.AddComponent<Image>();
                bg.color = background.Value;
                bg.raycastTarget = true; // bloque les touchers vers le jeu
            }
            Safe = Ui.Stretch(Ui.Rect("Zone", Root));
            Safe.gameObject.AddComponent<SafeArea>();
            // Les menus (avec un fond) s'ouvrent en fondu ; l'accueil, la partie et les messages apparaissent tout de suite.
            if (background.HasValue) Root.gameObject.AddComponent<ScreenIntro>().Content = Safe;
            Build();
            Root.gameObject.SetActive(false);
        }

        protected abstract void Build();
        public virtual void Show() { Root.gameObject.SetActive(true); Root.SetAsLastSibling(); Refresh(); }
        public virtual void Hide() { if (Root != null) Root.gameObject.SetActive(false); }
        public virtual void Refresh() { }
        public virtual void Tick(float dt) { }

        protected Button BackButton(Action onBack)
        {
            var b = Ui.IconButton(Safe, Icon.Back, Palette.Dark, onBack, "Retour");
            Ui.Place((RectTransform)b.transform, 0, 1, 36, -36, 150, 156, 0, 1);
            return b;
        }

        protected Text Header(string text)
        {
            var t = Ui.Title(Safe, text, 84);
            Ui.Place(t.rectTransform, 0.5f, 1, 70, -112, 700, 130);
            return t;
        }

        /// <summary>Petite étiquette sombre translucide avec icône et texte.</summary>
        protected Text Tag(Transform parent, Icon? icon, string text, float w, float h, out RectTransform rt, int size = 44)
        {
            var pill = Ui.Glass(parent, "Etiquette");
            rt = pill.rectTransform;
            rt.sizeDelta = new Vector2(w, h);
            var row = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter; row.spacing = 12; row.padding = new RectOffset(24, 24, 4, 4);
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            if (icon.HasValue)
            {
                var ic = Ui.Icon(pill.transform, Art.IconSprite(icon.Value));
                Ui.Size(ic, h * 0.72f, h * 0.72f);
            }
            var t = Ui.Title(pill.transform, text, size);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }
    }

    // ===================================================================== Accueil

    public sealed class HomeScreen : UiScreen
    {
        Text _best, _level, _coins, _missionBadge, _shopBadge, _headBadge, _special;
        GameObject _specialGo;
        Button _resume, _daily;
        RectTransform _xpFill;
        GameObject _bestIcon;
        // Au-dessus de la barre d'accueil de l'iPhone (indicateur d'environ 34 pt, soit 70 px de référence).
        const float DockBottom = 72f, DockHeight = 168f;
        RectTransform _dock;
        readonly Vector3[] _corners = new Vector3[4];
        GameObject _missionBadgeGo, _shopBadgeGo, _headBadgeGo;
        int _shownCoins = -1;

        static readonly Color[] LogoColors = { Palette.Primary, Palette.Accent, Palette.Play, Palette.Secondary, Palette.Purple, Palette.Primary, Palette.Accent };

        protected override void Build()
        {
            // Barre du haut : réglages, niveau (avec sa barre d'XP), pièces (touche = boutique).
            var gear = Ui.IconButton(Safe, Icon.Gear, Palette.Dark, App.ShowSettings, "Reglages");
            Ui.Place((RectTransform)gear.transform, 0, 1, 28, -28, 124, 130, 0, 1);
            var lvl = Ui.Glass(Safe, "Niveau");
            // Niveau : étoile à gauche, puis « Niv. 7 » en haut et la barre d'XP en dessous, bien séparés.
            Ui.Place(lvl.rectTransform, 0, 1, 168, -32, 330, 120, 0, 1);
            var star = Ui.Icon(lvl.transform, Art.IconSprite(Icon.Star), "Etoile");
            star.color = Palette.Accent;
            Ui.Place(star.rectTransform, 0, 0.5f, 14, 2, 88, 88, 0, 0.5f);
            _level = Ui.Title(lvl.transform, "", 44);
            Ui.Place(_level.rectTransform, 0, 1, 114, -12, 196, 54, 0, 1);
            _level.alignment = TextAnchor.MiddleLeft;
            _level.horizontalOverflow = HorizontalWrapMode.Overflow;
            _xpFill = MetaUi.Bar(lvl.transform, Art, Palette.Play, "XP");
            var xpTrack = (RectTransform)_xpFill.parent;
            Ui.Place(xpTrack, 0, 0, 116, 22, 186, 18, 0, 0);
            var trackImg = xpTrack.GetComponent<Image>();
            trackImg.color = new Color(0.08f, 0.08f, 0.16f, 0.4f);
            // Barre fine : bouts arrondis à son échelle (sinon les coins du sprite se déforment).
            trackImg.pixelsPerUnitMultiplier = 4f;
            _xpFill.GetComponent<Image>().pixelsPerUnitMultiplier = 4f;
            _coins = MetaUi.CoinPill(Safe, Art, out var coinsRt, 50, () => App.ShowShop("accueil"));
            Ui.Place(coinsRt, 1, 1, -28, -36, 330, 112, 1, 1); // même hauteur visuelle que le niveau

            // Logo : les lettres de BASCULE se balancent comme une planche.
            var logo = Ui.Rect("Logo", Safe);
            Ui.Place(logo, 0.5f, 1, 0, -262, 980, 200);
            var rock = logo.gameObject.AddComponent<Rock>();
            rock.Angle = 3.5f; rock.Speed = 1.2f;
            string word = Loc.Get("title");
            float step = 118f, x0 = -(word.Length - 1) * step * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                var letter = Ui.Title(logo, word[i].ToString(), 156, LogoColors[i % LogoColors.Length], "Lettre");
                Ui.Place(letter.rectTransform, 0.5f, 0.5f, x0 + i * step, 0, 150, 200);
                letter.horizontalOverflow = HorizontalWrapMode.Overflow;
                var r = letter.gameObject.AddComponent<Rock>();
                r.Angle = 5f; r.Speed = 2.2f; r.Phase = i * 0.6f; r.Bob = 10f;
            }

            // Record : bien détaché du logo, étiquette ajustée à son texte.
            _best = Tag(Safe, Icon.Trophy, "", 520, 80, out var bestRt);
            Ui.Place(bestRt, 0.5f, 1, 0, -452, 560, 82);
            var fit = bestRt.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _bestIcon = bestRt.GetChild(0).gameObject;

            _resume = Ui.Button(Safe, Loc.Get("resume"), Palette.Success, App.ResumeSavedRun, Icon.Replay, 50, "Reprendre");
            Ui.Place((RectTransform)_resume.transform, 0.5f, 1, 0, -586, 640, 110);

            var play = Ui.Button(Safe, Loc.Get("play"), Palette.Play, () => App.Play(GameMode.Infinite), Icon.Play, 100, "Jouer");
            Ui.Place((RectTransform)play.transform, 0.5f, 1, 0, -748, 760, 200);
            var pulse = play.gameObject.AddComponent<Pulse>();
            pulse.Amount = 0.035f; pulse.Speed = 3.2f;
            UnityEngine.Object.Destroy(play.GetComponent<PressScale>());
            // Ruban « partie spéciale » sur le bouton Jouer, quand la prochaine partie en est une.
            var ribbon = Ui.Image(play.transform, Art.Pill, Palette.Purple, "Speciale", true);
            _specialGo = ribbon.gameObject;
            Ui.Place(ribbon.rectTransform, 1, 1, 40, -10, 340, 84, 1, 1);
            ribbon.rectTransform.localRotation = Quaternion.Euler(0, 0, -6f);
            ribbon.raycastTarget = false;
            _special = Ui.Title(ribbon.transform, "...", 44);
            Ui.Stretch(_special.rectTransform, 16, 4, 16, 4);
            _special.resizeTextForBestFit = true; _special.resizeTextMinSize = 32; _special.resizeTextMaxSize = 44;
            _special.raycastTarget = false;

            _daily = Ui.Button(Safe, Loc.Get("daily"), Palette.Secondary, App.ShowDailyIntro, Icon.Calendar, 54, "Defi");
            Ui.Place((RectTransform)_daily.transform, 0.5f, 1, 0, -920, 700, 130);

            // Barre du bas, sous les pieds de Basile : les cinq accès secondaires, comme un dock d'application.
            var dock = Ui.Glass(Safe, "Dock");
            var dockRt = dock.rectTransform;
            _dock = dockRt;
            Ui.Place(dockRt, 0.5f, 0, 0, DockBottom, 1040, DockHeight, 0.5f, 0);

            // Le duel n'apparaît que s'il fonctionne vraiment dans cette version (jamais de bouton « bientôt » :
            // une fonction annoncée mais absente peut faire refuser l'application).
            var items = new List<(Icon icon, string label, Color color, Action action)>
            {
                (Icon.Face, Loc.Get("heads"), Palette.Purple, App.ShowCollection),
                (Icon.Target, Loc.Get("missions"), Palette.Success, App.ShowMissions),
                (Icon.Bag, Loc.Get("shop"), Palette.Primary, () => App.ShowShop("accueil")),
                (Icon.Trophy, Loc.Get("leaderboard"), Palette.Accent, App.ShowLeaderboard),
            };
            if (App.DuelAvailable) items.Add((Icon.Swords, Loc.Get("duel"), Palette.Secondary, App.OpenDuel));
            var row = items.ToArray();
            float spacing = row.Length >= 5 ? 204f : 250f;
            for (int i = 0; i < row.Length; i++)
            {
                float x = (i - (row.Length - 1) * 0.5f) * spacing;
                var b = Ui.IconButton(dockRt, row[i].icon, row[i].color, row[i].action, row[i].label);
                Ui.Place((RectTransform)b.transform, 0.5f, 0, x, 50, 116, 122, 0.5f, 0);
                var lbl = Ui.Title(dockRt, row[i].label, 34, Color.white, "Legende");
                Ui.Place(lbl.rectTransform, 0.5f, 0, x, 8, 200, 44, 0.5f, 0);
                lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 26; lbl.resizeTextMaxSize = lbl.fontSize;
                if (i == 4)
                {
                    // Duel : petite pastille « EN DIRECT » qui attire l'œil.
                    var ring = Ui.Image(b.transform, Art.Pill, Color.white, "Direct", true);
                    Ui.Place(ring.rectTransform, 0.5f, 1, 0, 26, 160, 54, 0.5f, 1);
                    var live = Ui.Image(ring.transform, Art.Pill, Palette.Primary, "Fond", true);
                    Ui.Stretch(live.rectTransform, 4, 4, 4, 4);
                    var lt = Ui.Title(live.transform, Loc.Get("live_tag"), 30, Color.white);
                    Ui.Stretch(lt.rectTransform, 8, 0, 8, 3);
                }
                if (i == 0) _headBadge = MetaUi.Badge(b.transform, Art, out _headBadgeGo);
                if (i == 1) _missionBadge = MetaUi.Badge(b.transform, Art, out _missionBadgeGo);
                if (i == 2) _shopBadge = MetaUi.Badge(b.transform, Art, out _shopBadgeGo);
                if (i <= 2)
                {
                    // Pastille à l'échelle des boutons du dock.
                    var go = i == 0 ? _headBadgeGo : i == 1 ? _missionBadgeGo : _shopBadgeGo;
                    var t = i == 0 ? _headBadge : i == 1 ? _missionBadge : _shopBadge;
                    // Petite pastille posée sur le coin, presque entièrement hors du bouton : l'icône reste visible.
                    Ui.Place((RectTransform)go.transform, 1, 1, 26, 26, 50, 50, 1, 1);
                    var disc = go.GetComponent<Image>();
                    disc.sprite = Art.Circle; disc.type = Image.Type.Simple;
                    var ring = go.AddComponent<Outline>(); // liseré blanc : la pastille rouge se détache aussi sur la Boutique rouge
                    ring.effectColor = Color.white; ring.effectDistance = new Vector2(3, -3);
                    t.fontSize = Mathf.RoundToInt(32 * Ui.TextScale);
                    Ui.Stretch(t.rectTransform, -4, -2, -4, 3);
                }
            }
        }

        public override void Refresh()
        {
            var p = App.Profile;
            _best.text = p.bestScore > 0 ? Loc.Format("best", Loc.FormatScore(p.bestScore)) : Loc.Get("hint_short");
            _bestIcon.SetActive(p.bestScore > 0);
            _resume.gameObject.SetActive(App.HasSavedRun);
            string today = App.TodayKey;
            int streak = Daily.CurrentStreak(p.daily, today);
            var label = Ui.ButtonLabel(_daily);
            if (label != null) label.text = Loc.Get("daily");

            _level.text = Loc.Format("level", p.level);
            MetaUi.SetFill(_xpFill, Levels.Fraction(App.Tuning.economy, p));
            Missions.Refresh(p, App.LocalDay);
            int left = p.missions.list.FindAll(m => !m.done).Count;
            _missionBadgeGo.SetActive(left > 0);
            _missionBadge.text = left.ToString();
            // Têtes gagnées pas encore regardées.
            int fresh = HeadNews.Count(p);
            _headBadgeGo.SetActive(fresh > 0);
            _headBadge.text = fresh > 9 ? "9+" : fresh.ToString();
            bool starter = Offers.StarterActive(p, App.Now);
            _shopBadgeGo.SetActive(starter);
            _shopBadge.text = "!";
            var next = SpecialRun.Next(p);
            _specialGo.SetActive(next != SpecialKind.None);
            if (next != SpecialKind.None) _special.text = Loc.Get("special_short_" + next);
            _shownCoins = -1;
            Tick(0);
        }

        public override void Tick(float dt)
        {
            // Basile se tient au-dessus de la barre du bas : on remonte le monde juste ce qu'il faut (rien sur les écrans où il y a la place).
            var s = App.Session;
            if (s != null && _dock != null)
            {
                _dock.GetWorldCorners(_corners); // canvas en superposition : coordonnées = pixels de l'écran
                float need = _corners[1].y + 24f - s.FeetScreenY;
                s.HomeLift = Mathf.Clamp(need / Mathf.Max(1f, s.PixelsPerUnit), 0f, 1.2f);
            }
            int c = App.Profile.coins;
            if (c == _shownCoins) return;
            _shownCoins = c;
            _coins.text = Loc.FormatScore(c);
        }
    }

    // ===================================================================== Pendant la partie

    public sealed class HudScreen : UiScreen
    {
        Text _score, _best, _next, _hint, _mode, _goal, _mark;
        CanvasGroup _hintGroup;
        int _shownGoal = -2;
        float _goalPunch;
        // Barre d'avancement du défi du jour (« 32 / 60 »).
        RectTransform _dailyBar, _dailyFill;
        Image _dailyFillImg;
        Text _dailyText;
        int _dailyShown = int.MinValue;
        Image _hand;
        // Tutoriel de la première partie : viser, fusionner, équilibrer ; puis les cœurs.
        enum Tuto { Off, Aim, Merge, Good, Watch }
        Tuto _tuto;
        bool _balanceShown;
        static bool _lostShown;
        Image _nextWeight, _vignette, _progressFill, _nextHead;
        RectTransform _bubble, _progressFillRt;
        GameObject _progress;
        int _shownScore = -1, _shownNext = -1;
        float _punch, _hintTimer, _bubbleX, _flashA, _glow;
        Image _flash;
        RectTransform _hudBottom;
        // Disposition du duel : l'en-tête TOI / VS / adversaire remplace le score ; niveau et « Suivant » se resserrent.
        RectTransform _pauseRt, _scorePillRt, _tubeRt, _nextRt;
        Text _tubeLabel;
        // Annonce de rafale : grande banderole animée (UI/GustBanner.cs).
        GustBanner _gust;
        readonly Vector3[] _corners = new Vector3[4];

        NeonEdge _neon;

        /// <summary>Combo : le contour de l'écran s'illumine (plus fort et plus longtemps avec la longueur du combo).</summary>
        public void OnCombo(int chain)
        {
            _neon.ReducedMotion = App.Profile.settings.reducedMotion;
            _neon.Combo(chain);
        }

        /// <summary>Éclair de couleur plein écran (appelé par la partie sur un gros combo ou un 128).</summary>
        public void Flash(float strength, Color color)
        {
            if (App.Profile.settings.reducedMotion) return;
            _flash.color = new Color(color.r, color.g, color.b, 0);
            _flashA = Mathf.Max(_flashA, strength);
            _flash.gameObject.SetActive(true);
            _glow = 1f;
        }
        const float TubeTravel = 255f, ProgressWidth = 400f;

        protected override void Build()
        {
            // Voile rouge de danger, sous tout le reste.
            _vignette = Ui.Image(Root, Art.Vignette, new Color(1, 1, 1, 0), "Danger");
            // Éclair plein écran (gros combo, 128) : très bref, coupé en mode animations réduites.
            _flash = Ui.Image(Root, null, new Color(1, 1, 1, 0), "Eclair");
            Ui.Stretch(_flash.rectTransform);
            _flash.gameObject.SetActive(false);
            Ui.Stretch(_vignette.rectTransform);
            _vignette.transform.SetAsFirstSibling();
            // Néon des combos sur tout le contour de l'écran (sous les textes du HUD, au-dessus du jeu).
            _neon = NeonEdge.Create(Root);
            _neon.transform.SetSiblingIndex(1);

            var pause = Ui.IconButton(Safe, Icon.Pause, Palette.Dark, App.PauseGame, "Pause");
            _pauseRt = (RectTransform)pause.transform;
            Ui.Place(_pauseRt, 0, 1, 30, -30, 140, 148, 0, 1);

            var scorePill = Ui.Glass(Safe, "FondScore");
            Ui.Place(scorePill.rectTransform, 0.5f, 1, 0, -40, 440, 150, 0.5f, 1);
            _scorePillRt = scorePill.rectTransform;
            _score = Ui.Title(scorePill.transform, "0", 108);
            Ui.Stretch(_score.rectTransform, 0, -6, 0, 6);
            _score.horizontalOverflow = HorizontalWrapMode.Overflow;
            _best = Ui.Title(Safe, "", 40, ArtCache.Hex("#FFE7A0"));
            Ui.Place(_best.rectTransform, 0.5f, 1, 0, -222, 600, 56);
            _mode = Ui.Title(Safe, "", 40, ArtCache.Hex("#BFE0FF"));
            Ui.Place(_mode.rectTransform, 0.5f, 1, 0, -470, 600, 56);

            // Niveau à bulle : la bulle file vers le côté haut, les bouts rouges = danger.
            var tube = Ui.Image(Safe, Art.LevelTube, Color.white, "Niveau");
            Ui.Place(tube.rectTransform, 0.5f, 1, 0, -300, 600, 90);
            _tubeRt = tube.rectTransform;
            _tubeLabel = Ui.Title(tube.transform, Loc.Get("duel_your_plank"), 50, ArtCache.Hex("#BFE0FF"), "Legende");
            Ui.Place(_tubeLabel.rectTransform, 0.5f, 0, 0, -6, 600, 70, 0.5f, 1);
            var bubble = Ui.Image(tube.transform, Art.Bubble, Color.white, "Bulle");
            _bubble = bubble.rectTransform;
            Ui.Place(_bubble, 0.5f, 0.5f, 0, 0, 74, 74);
            // La bulle bouge à chaque image : dans son propre sous-canvas, le reste du HUD n'est pas recalculé.
            Ui.Isolate(tube.gameObject);

            // Progression vers la prochaine tête.
            // Une vraie jauge : rail sombre cerné, remplissage doré avec reflet, bouts bien ronds (CapsuleSlice),
            // et la prochaine tête dans un médaillon rond au bout du rail.
            var rim = Ui.Image(Safe, Art.Pill, Palette.Outline, "Progression", true);
            _progress = rim.gameObject;
            Ui.Place(rim.rectTransform, 0.5f, 1, -40, -400, ProgressWidth + 12, 46);
            CapsuleSlice.Attach(rim);
            var track = Ui.Image(rim.transform, Art.Pill, ArtCache.Hex("#4A4E78"), "Rail", true);
            Ui.Stretch(track.rectTransform, 6, 6, 6, 6);
            CapsuleSlice.Attach(track);
            _progressFill = Ui.Image(track.transform, Art.Pill, Palette.Accent, "Remplissage", true);
            CapsuleSlice.Attach(_progressFill);
            _progressFillRt = _progressFill.rectTransform;
            _progressFillRt.anchorMin = new Vector2(0, 0); _progressFillRt.anchorMax = new Vector2(0, 1);
            _progressFillRt.pivot = new Vector2(0, 0.5f);
            _progressFillRt.anchoredPosition = Vector2.zero;
            MetaUi.Shine(_progressFill);
            var medal = Ui.Image(rim.transform, Art.Circle, Palette.Outline, "Medaillon");
            Ui.Place(medal.rectTransform, 1, 0.5f, 40, 0, 104, 104);
            var medalIn = Ui.Image(medal.transform, Art.Circle, Palette.Paper, "Fond");
            Ui.Place(medalIn.rectTransform, 0.5f, 0.5f, 0, 0, 90, 90);
            _nextHead = Ui.Icon(medal.transform, null, "ProchaineTete");
            Ui.Place(_nextHead.rectTransform, 0.5f, 0.5f, 0, 2, 84, 84);

            // Poids suivant.
            var box = Ui.Card(Safe, "Suivant");
            Ui.Place(box.rectTransform, 1, 1, -26, -26, 210, 250, 1, 1);
            _nextRt = box.rectTransform;
            var lbl = Ui.Label(box.transform, Loc.Get("next"), 40, Palette.Ink);
            Ui.Place(lbl.rectTransform, 0.5f, 1, 0, -44, 200, 56);
            _nextWeight = Ui.Icon(box.transform, null, "Poids");
            Ui.Place(_nextWeight.rectTransform, 0.5f, 0, 0, 108, 150, 150);
            _next = Ui.Label(_nextWeight.transform, "1", 44, Palette.Ink);
            _next.resizeTextForBestFit = true; _next.resizeTextMinSize = 20; _next.resizeTextMaxSize = 60;

            // Conseil : un texte cerné qui flotte au-dessus de la planche, sans panneau (plus léger, ne cache rien).
            var hintRt = Ui.Rect("Conseil", Safe);
            Ui.Place(hintRt, 0.5f, 0.5f, 0, 330, 900, 150);
            _hintRt = hintRt;
            _hintGroup = hintRt.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.blocksRaycasts = false; _hintGroup.interactable = false;
            _hint = Ui.Title(hintRt, Loc.Get("hint"), 48);
            Ui.Stretch(_hint.rectTransform);
            _hint.resizeTextForBestFit = true; _hint.resizeTextMinSize = Ui.MinFont; _hint.resizeTextMaxSize = _hint.fontSize;
            _hint.raycastTarget = false;

            // Objectif de la partie (mode infini) : petite ligne sous la progression, verte une fois atteint.
            _goal = Ui.Title(Safe, "...", 40, ArtCache.Hex("#BFE0FF"), "Objectif");
            Ui.Place(_goal.rectTransform, 0.5f, 1, 0, -470, 700, 56);
            _goal.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Défi du jour : barre d'avancement sous l'objectif.
            var barBg = Ui.Image(Safe, Art.Pill, new Color(0.1f, 0.08f, 0.22f, 0.55f), "AvancementDefi", true);
            _dailyBar = barBg.rectTransform;
            Ui.Place(_dailyBar, 0.5f, 1, 0, -522, 560, 40);
            var fill = Ui.Image(_dailyBar, Art.Pill, Palette.Accent, "Remplissage", true);
            _dailyFillImg = fill;
            _dailyFill = fill.rectTransform;
            _dailyFill.anchorMin = new Vector2(0, 0); _dailyFill.anchorMax = new Vector2(0, 1);
            _dailyFill.pivot = new Vector2(0, 0.5f);
            _dailyFill.offsetMin = new Vector2(4, 4); _dailyFill.offsetMax = new Vector2(40, -4);
            _dailyText = Ui.Title(_dailyBar, "", 30, Color.white);
            Ui.Stretch(_dailyText.rectTransform, 10, 0, 10, 2);
            Ui.Isolate(_dailyBar.gameObject);
            _dailyBar.gameObject.SetActive(false);
            // Repère invisible au bas de l'interface du haut (bas de la ligne « Objectif ») : le poids tenu se place dessous.
            _hudBottom = Ui.Rect("BasInterface", Safe);
            Ui.Place(_hudBottom, 0.5f, 1, 0, -498, 10, 2, 0.5f, 0.5f);
            // Mode vidéo : le nom du jeu et le code défi restent lisibles sur les écrans filmés (TikTok, Reels, Shorts).
            _mark = Ui.Title(Safe, "", 40, new Color(1, 1, 1, 0.8f), "Filigrane");
            Ui.Place(_mark.rectTransform, 0.5f, 0, 0, 34, 1000, 56, 0.5f, 0);
            _mark.horizontalOverflow = HorizontalWrapMode.Overflow;
            _gust = new GustBanner(Safe, Art);
            _hand = Ui.Icon(Safe, Art.IconSprite(Icon.Hand), "Main");
            Ui.Place(_hand.rectTransform, 0.5f, 0.5f, 0, 120, 150, 150);
        }

        void UpdateWind(GameSession s, float dt)
        {
            bool started = _gust.Tick(s.WindPhase, s.WindDirection, s.WindWarning, s.WindEnvelope, App.Profile.settings.reducedMotion, dt);
            if (started)
            {
                var p = App.Profile;
                if (!p.windTipShown) { p.windTipShown = true; Say("tip_wind", 4.2f, 2); }
            }
        }

        GameObject HintRoot => _hintGroup.gameObject;
        RectTransform _hintRt;

        int _hintPriority;

        /// <summary>
        /// Affiche un conseil au-dessus de la planche. Un conseil important (l'explication du 128) ne peut pas être
        /// remplacé par un conseil moins important tant qu'il n'a pas eu le temps d'être lu.
        /// </summary>
        void Say(string key, float seconds, int priority = 0)
        {
            if (_hintTimer > 0.3f && priority < _hintPriority) return;
            _hintPriority = priority;
            _hint.text = Loc.Get(key);
            _hintTimer = seconds;
            _hintRt.localScale = Vector3.one * 1.08f;
        }

        public void OnDropped()
        {
            if (_tuto == Tuto.Aim) { _tuto = Tuto.Merge; Say("tuto_merge", 7f); }
            // Un poids lâché ferme le conseil affiché, sauf un conseil important (le 128) qui reste le temps d'être lu.
            else if (_tuto == Tuto.Off && _hintPriority < 2) _hintTimer = Mathf.Min(_hintTimer, 0.3f);
        }

        public void OnMerged(int value)
        {
            if (_tuto == Tuto.Merge || _tuto == Tuto.Aim) { _tuto = Tuto.Good; Say("tuto_good", 2.5f); return; }
            // Le 128 : expliqué une seule fois, dès le premier 64 (on sait alors ce qui arrive en en fusionnant deux),
            // ou au premier 128 si le joueur n'a jamais eu de 64 à l'écran.
            var p = App.Profile;
            if (!p.balloonTipShown && value >= 64)
            {
                p.balloonTipShown = true;
                Say("tip_128", 6f, 2);
            }
        }

        public void OnWeightLost(int value)
        {
            // Expliqué la première fois qu'un poids tombe, pendant les premières parties.
            if (_lostShown || App.Profile.gamesPlayed > 5) return;
            _lostShown = true;
            Say("tuto_lost", 4.5f);
        }

        public void HideHint() => OnDropped();

        public override void Show()
        {
            base.Show();
            var s0 = App.Session;
            _shownScore = -1;
            _shownNext = -1;
            _balanceShown = false;
            _tuto = App.Profile.tutorialDone || s0.Mode != GameMode.Infinite || s0.Drops > 0 ? Tuto.Off : Tuto.Aim;
            if (_tuto == Tuto.Aim) Say("tuto_aim", 999f);
            else { _hint.text = Loc.Get("hint"); _hintTimer = App.Profile.gamesPlayed < 3 && !App.Profile.settings.creatorMode ? 6f : 0f; }
            var s = App.Session;
            bool creator = App.Profile.settings.creatorMode;
            _mark.gameObject.SetActive(creator);
            if (creator)
                _mark.text = s.Mode == GameMode.Daily ? Loc.Format("creator_mark_daily", Brag.DayNumber(s.DayKey))
                    : Loc.Get("creator_mark");
            _mode.text = s.Mode == GameMode.Duel ? Loc.Get("duel") : ""; // défi du jour : la ligne de l'objectif le dit déjà
            _shownGoal = -2;
            UpdateGoal(s);
            _gust.Hide();
            _neon.Off();
            _flashA = 0f; _flash.gameObject.SetActive(false); // pas d'éclair resté de la partie précédente
            // La barre du défi du jour repart cachée : elle se replace (avec le repère du poids tenu) à la première image.
            _dailyBar.gameObject.SetActive(false);
            _dailyShown = int.MinValue;
            SetDuelLayout(App.Duel != null && App.Duel.InMatch);
        }

        /// <summary>
        /// Duel : plus de pause (le temps ne s'arrête pas chez l'autre), plus de score central ni de record ;
        /// le niveau se loge entre la vue en direct et « Suivant », et le poids tenu descend sous la vue en direct.
        /// </summary>
        void SetDuelLayout(bool duel)
        {
            _pauseRt.gameObject.SetActive(!duel);
            _scorePillRt.gameObject.SetActive(!duel);
            _best.gameObject.SetActive(!duel);
            _tubeLabel.gameObject.SetActive(duel);
            if (duel)
            {
                _mode.gameObject.SetActive(false);
                _goal.gameObject.SetActive(false);
                _progress.SetActive(false);
                Ui.Place(_tubeRt, 0.5f, 1, 78, -258, 600, 90);
                _tubeRt.localScale = Vector3.one * 0.7f;
                Ui.Place(_nextRt, 1, 1, -26, -196, 210, 250, 1, 1);
                _nextRt.localScale = Vector3.one * 0.9f;
                Ui.Place(_hudBottom, 0.5f, 1, 0, DuelHudScreen.CardTop - DuelHudScreen.CardHeight - 62, 10, 2, 0.5f, 0.5f);
            }
            else
            {
                _mode.gameObject.SetActive(true);
                Ui.Place(_tubeRt, 0.5f, 1, 0, -300, 600, 90);
                _tubeRt.localScale = Vector3.one;
                Ui.Place(_nextRt, 1, 1, -26, -26, 210, 250, 1, 1);
                _nextRt.localScale = Vector3.one;
                Ui.Place(_hudBottom, 0.5f, 1, 0, -498, 10, 2, 0.5f, 0.5f);
            }
        }

        /// <summary>Barre « 32 / 60 » du défi du jour : avance en direct, verte une fois réussi, rouge si c'est raté.</summary>
        void UpdateDailyBar(GameSession s)
        {
            bool on = s.Mode == GameMode.Daily && !(App.Duel != null && App.Duel.InMatch) && !DailyChallenge.DoneToday(App.Profile, s.DayKey);
            if (_dailyBar.gameObject.activeSelf != on)
            {
                _dailyBar.gameObject.SetActive(on);
                // Le poids tenu se place sous la barre.
                Ui.Place(_hudBottom, 0.5f, 1, 0, on ? -548 : -498, 10, 2, 0.5f, 0.5f);
                _dailyShown = int.MinValue;
            }
            if (!on) return;
            var p = DailyChallenge.Track(DailyChallenge.For(s.DayKey), s.LiveStats);
            int key = p.Done ? -1 : p.Failed ? -2 : p.Current;
            if (key == _dailyShown) return; // texte et barre seulement quand ça change (pas d'allocation à chaque image)
            if (_dailyShown >= 0 && key == -1) _goalPunch = 1f;
            _dailyShown = key;
            float w = _dailyBar.rect.width > 1 ? _dailyBar.rect.width : 560f;
            _dailyFill.sizeDelta = new Vector2(Mathf.Max(32f, (w - 8f) * (p.Failed ? 1f : p.Fraction)), _dailyFill.sizeDelta.y);
            _dailyFillImg.color = p.Done ? Palette.Play : p.Failed ? Palette.Primary : Palette.Accent;
            _dailyText.text = p.Done ? Loc.Get("daily_progress_done") : p.Failed ? Loc.Get("daily_progress_failed")
                : p.Time ? Loc.Format("daily_progress", Loc.FormatDuration(p.Current), Loc.FormatDuration(p.Target))
                : Loc.Format("daily_progress", Loc.FormatScore(p.Current), Loc.FormatScore(p.Target));
        }

        void UpdateGoal(GameSession s)
        {
            // Défi du jour : son objectif reste affiché sous le niveau pendant la partie.
            if (s.Mode == GameMode.Daily && !(App.Duel != null && App.Duel.InMatch))
            {
                if (_shownGoal == 2) return;
                _shownGoal = 2;
                _goal.gameObject.SetActive(true);
                bool done = DailyChallenge.DoneToday(App.Profile, s.DayKey);
                _goal.text = done ? Loc.Get("daily_objective_done") : Loc.Format("daily_objective", Loc.DailyText(DailyChallenge.For(s.DayKey)));
                _goal.color = done ? ArtCache.Hex("#9CF5B0") : ArtCache.Hex("#FFE7A0");
                return;
            }
            int state = s.Goal <= 0 ? -1 : s.GoalDone ? 1 : 0;
            if (state == _shownGoal) return;
            if (_shownGoal == 0 && state == 1) _goalPunch = 1f;
            _shownGoal = state;
            _goal.gameObject.SetActive(state >= 0);
            if (state < 0) return;
            _goal.text = state == 1 ? Loc.Get("goal_reached") : Loc.Format("goal_label", Loc.FormatScore(s.Goal));
            _goal.color = state == 1 ? ArtCache.Hex("#9CF5B0") : ArtCache.Hex("#BFE0FF");
        }


        public override void Tick(float dt)
        {
            var s = App.Session;
            UpdateWind(s, dt);
            // Canvas en superposition : les coins sont directement en pixels d'écran.
            _hudBottom.GetWorldCorners(_corners);
            WorldLayout.HudBottomScreenY = _corners[0].y;
            if (s.Score != _shownScore)
            {
                // Le score bondit d'autant plus que le gain est gros, et brille en doré sur les gros gains.
                if (_shownScore >= 0 && s.Score > _shownScore)
                {
                    int gain = s.Score - _shownScore;
                    _punch = Mathf.Min(1.8f, 0.8f + gain / 150f);
                    if (gain >= 100) _glow = Mathf.Max(_glow, Mathf.Min(1f, gain / 300f));
                }
                _shownScore = s.Score;
                _score.text = Loc.FormatScore(s.Score);
                int best = Mathf.Max(App.Profile.bestScore, s.Score);
                _best.text = Loc.Format("best", Loc.FormatScore(best));
                UpdateProgress(s.Score);
            }
            _punch = Mathf.MoveTowards(_punch, 0, dt * 4f);
            float k = 1f + _punch * 0.22f;
            _score.rectTransform.localScale = new Vector3(k, k, 1);
            _glow = Mathf.MoveTowards(_glow, 0, dt * 2.2f);
            _score.color = Color.Lerp(Color.white, Palette.Accent, _glow);
            if (_flashA > 0f)
            {
                _flashA = Mathf.MoveTowards(_flashA, 0f, dt * 2.4f);
                var fc = _flash.color; fc.a = _flashA; _flash.color = fc;
                if (_flashA <= 0f) _flash.gameObject.SetActive(false);
            }

            int next = s.NextValue;
            if (next != _shownNext)
            {
                _shownNext = next;
                _nextWeight.sprite = Art.Weight(next);
                _next.text = next.ToString();
                float plate = (WeightArtist.PlateCenterY - 0.5f) * 150f;
                float h = WeightArtist.DigitHeight(next.ToString().Length) * 150f * 1.45f;
                Ui.Place(_next.rectTransform, 0.5f, 0.5f, 0, plate, 100, h);
                _next.fontSize = Mathf.RoundToInt(h);
                _nextWeight.GetComponent<RectTransform>().localScale = Vector3.one * 1.25f;
            }
            var nw = _nextWeight.rectTransform;
            nw.localScale = Vector3.Lerp(nw.localScale, Vector3.one, dt * 8f);

            // Bulle : vers le côté haut de la planche (comme un vrai niveau).
            float max = App.Tuning.plank.maxAngle;
            float target = Mathf.Clamp(-s.Angle / max, -1.12f, 1.12f) * TubeTravel / 1.12f;
            _bubbleX = Mathf.Lerp(_bubbleX, target, 1f - Mathf.Exp(-dt * 14f));
            _bubble.anchoredPosition = new Vector2(_bubbleX, 0);

            float danger = Mathf.Clamp01((s.Danger - 0.62f) / 0.38f);
            float pulse = danger > 0.7f ? 0.75f + Mathf.Sin(Time.unscaledTime * 10f) * 0.25f : 1f;
            _vignette.color = new Color(1, 1, 1, danger * 0.85f * pulse);
            // Un voile plein écran transparent est quand même dessiné : on le coupe quand il ne se voit pas.
            bool vis = danger > 0.01f;
            if (_vignette.enabled != vis) _vignette.enabled = vis;

            if (_tuto != Tuto.Off && !_balanceShown && s.Danger > 0.55f && _tuto != Tuto.Aim)
            {
                _balanceShown = true;
                Say("tuto_balance", 3.5f);
            }
            if (_tuto == Tuto.Good && _hintTimer <= 0) _tuto = Tuto.Watch;
            _hintTimer -= dt;
            // Apparition et disparition en fondu (pas de panneau qui surgit).
            float a = Mathf.Clamp01(_hintTimer / 0.35f);
            _hintGroup.alpha = Mathf.MoveTowards(_hintGroup.alpha, a, dt * 5f);
            bool hintOn = _hintTimer > 0 || _hintGroup.alpha > 0.01f;
            if (HintRoot.activeSelf != hintOn) HintRoot.SetActive(hintOn);
            UpdateGoal(s);
            UpdateDailyBar(s);
            _goalPunch = Mathf.MoveTowards(_goalPunch, 0, dt * 2.5f);
            _goal.rectTransform.localScale = Vector3.one * (1f + _goalPunch * 0.25f);
            var hr = _hintRt;
            hr.localScale = Vector3.Lerp(hr.localScale, Vector3.one, dt * 8f);
            // Main qui montre le geste : glisser de gauche à droite, puis lâcher.
            bool hand = _tuto == Tuto.Aim;
            _hand.gameObject.SetActive(hand);
            if (hand)
            {
                float t = Time.unscaledTime * 1.4f;
                _hand.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t) * 260f, 160f + Mathf.Abs(Mathf.Cos(t)) * 20f);
                _hand.color = new Color(1, 1, 1, 0.75f + Mathf.Sin(t * 2f) * 0.25f);
            }

        }

        void UpdateProgress(int score)
        {
            var nextHead = HeadCatalog.NextScoreHead(App.Profile.unlockedHeads);
            _progress.SetActive(nextHead != null && !(App.Duel != null && App.Duel.InMatch));
            if (nextHead == null) return;
            int prev = 0;
            foreach (var h in HeadCatalog.All)
                if (h.Unlock == UnlockKind.Score && h.UnlockValue < nextHead.UnlockValue && h.UnlockValue > prev) prev = h.UnlockValue;
            float t = Mathf.Clamp01((score - prev) / (float)Mathf.Max(1, nextHead.UnlockValue - prev));
            _progressFillRt.sizeDelta = new Vector2(Mathf.Max(34f, ProgressWidth * t), 0); // 34 = hauteur du rail : un rond au départ
            _nextHead.sprite = score >= nextHead.UnlockValue ? Art.Portrait(nextHead) : Art.Locked(nextHead);
        }
    }

    public sealed class PauseScreen : UiScreen
    {
        Text _score;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.5f, 0, 0, 820, 960);
            card.gameObject.AddComponent<PopIn>();
            var title = Ui.Title(card.transform, Loc.Get("pause"), 100, Palette.Accent);
            Ui.Place(title.rectTransform, 0.5f, 1, 0, -110, 700, 150);
            // Où en est la partie : le score et le record à battre.
            _score = Ui.Label(card.transform, "", 46, Palette.Muted);
            Ui.Place(_score.rectTransform, 0.5f, 1, 0, -215, 720, 60);
            var col = Ui.Rect("Menu", card.transform);
            Ui.Place(col, 0.5f, 0.5f, 0, -100, 660, 560);
            Ui.Column(col, 30);
            Ui.Size(Ui.Button(col, Loc.Get("continue"), Palette.Play, App.ResumeGame, Icon.Play, 62), 170);
            Ui.Size(Ui.Button(col, Loc.Get("restart"), Palette.Accent, App.RestartRun, Icon.Replay, 56), 150);
            Ui.Size(Ui.Button(col, Loc.Get("home"), Palette.Dark, App.GoHome, Icon.Home, 56), 150);
        }

        public override void Refresh()
        {
            var s = App.Session;
            _score.text = Loc.Format("pause_score", Loc.FormatScore(s.Score), Loc.FormatScore(Mathf.Max(App.Profile.bestScore, s.Score)));
        }
    }

    // ===================================================================== Fin de partie

    public sealed class GameOverScreen : UiScreen
    {
        Text _title, _subtitle, _score, _best, _details, _nextText, _level, _xp, _coins;
        Button _revive, _double;
        RectTransform _heads, _rays, _card, _xpFill;
        GameObject _nextBlock;
        Image _nextPortrait, _nextFill;
        GameObject _nextTrack;
        Button _nextBuy;
        EconomyGain _gain;
        Pulse _sharePulse;
        // Barre d'XP animée niveau par niveau (1.2) : elle se remplit, s'arrête à chaque niveau gagné, montre sa
        // récompense (les pièces rejoignent le compteur), puis repart. Plus de bandeau « Niveau X » par-dessus l'écran.
        float _xpV, _xpTo, _xpWait, _levelPunch, _coinsPunch;
        int _levelCoinsShown;
        bool _chestOpened;
        Image _xpFillImage;
        RectTransform _coinsRt;
        RectTransform _levelPop;
        Text _levelPopText;
        CanvasGroup _levelPopGroup;
        float _levelPopT = -1f;
        // Récompenses de la partie (objectif, missions, bonus) : montrées dans la carte après la barre d'XP,
        // une par une, comme les niveaux. Jamais de bandeau par-dessus l'écran de fin.
        readonly List<KeyValuePair<string, int>> _runRewards = new List<KeyValuePair<string, int>>();
        int _runRewardIndex, _runRewardCoinsShown, _runRewardCoins;
        readonly Image[] _chestPips = new Image[Chest.GamesPerChest];
        RectTransform _chestIcon, _chestBtnIcon;
        Button _chestBtn;
        Text _chestText;
        float _chestPop;

        protected override void Build()
        {
            var rays = Ui.Image(Safe, Art.Rays, new Color(1, 0.92f, 0.6f, 0.35f), "Rayons");
            _rays = rays.rectTransform;
            Ui.Place(_rays, 0.5f, 1, 0, -560, 1500, 1500);
            _rays.gameObject.AddComponent<Spin>().Speed = 12f;

            // En haut : « Basile a lâché ! » puis le titre de la partie, celui qu'on a envie de montrer (ou d'avouer).
            _subtitle = Ui.Title(Safe, "", 44, ArtCache.Hex("#BFE0FF"));
            Ui.Place(_subtitle.rectTransform, 0.5f, 1, 0, -70, 1000, 60);
            _title = Ui.Title(Safe, "", 66, Color.white);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -150, 1000, 100);
            _title.resizeTextForBestFit = true; _title.resizeTextMinSize = 44; _title.resizeTextMaxSize = _title.fontSize;

            var card = Ui.Card(Safe, "Bilan");
            _card = card.rectTransform;
            Ui.Place(_card, 0.5f, 1, 0, -220, 940, 860, 0.5f, 1);
            card.gameObject.AddComponent<PopIn>();
            var lbl = Ui.Label(card.transform, Loc.Get("score_label"), 46, Palette.Muted);
            Ui.Place(lbl.rectTransform, 0.5f, 1, 0, -58, 600, 56);
            _score = Ui.Title(card.transform, "0", 170, Palette.Accent);
            // Contour plus fin que celui d'un titre de cette taille (10 px, ombre de 19 px) : il empiétait sur « Record ».
            foreach (var sh in _score.GetComponents<Shadow>()) UnityEngine.Object.DestroyImmediate(sh);
            Ui.Outlined(_score, 6f);
            Ui.Place(_score.rectTransform, 0.5f, 1, 0, -166, 900, 184);
            _score.horizontalOverflow = HorizontalWrapMode.Overflow;
            _score.gameObject.AddComponent<CountUp>().enabled = false;
            _best = Ui.Title(card.transform, "", 50, Palette.Primary);
            Ui.Place(_best.rectTransform, 0.5f, 1, 0, -290, 900, 60);
            _details = Ui.Label(card.transform, "", 44, Palette.Ink);
            Ui.Place(_details.rectTransform, 0.5f, 1, 0, -346, 860, 60);

            // Niveau : la barre d'XP se remplit à chaque partie, même ratée.
            _level = Ui.Title(card.transform, "", 46, Palette.Secondary);
            Ui.Place(_level.rectTransform, 0, 1, 40, -418, 190, 60, 0, 0.5f);
            _level.alignment = TextAnchor.MiddleLeft;
            _xpFill = MetaUi.Bar(card.transform, Art, Palette.Play, "XP");
            Ui.Place((RectTransform)_xpFill.parent, 0, 1, 230, -418, 470, 38, 0, 0.5f);
            _xpFillImage = _xpFill.GetComponent<Image>();
            _xp = Ui.Label(card.transform, "", 44, Palette.Muted, TextAnchor.MiddleLeft);
            Ui.Place(_xp.rectTransform, 0, 1, 714, -418, 220, 60, 0, 0.5f);
            _xp.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Récompense d'un niveau : « Niv. 7  +150 » (pièce dorée) qui jaillit du bout de la barre et monte en s'effaçant.
            _levelPop = Ui.Rect("RecompenseNiveau", card.transform);
            Ui.Place(_levelPop, 0, 1, 470, -418, 660, 80);
            _levelPopGroup = _levelPop.gameObject.AddComponent<CanvasGroup>();
            _levelPopGroup.blocksRaycasts = false; _levelPopGroup.interactable = false;
            var popBg = Ui.Image(_levelPop, Art.Pill, Palette.Accent, "Fond", true);
            Ui.Stretch(popBg.rectTransform);
            CapsuleSlice.Attach(popBg);
            var popCoin = Ui.Icon(_levelPop, Art.IconSprite(Icon.Coin), "Piece");
            Ui.Place(popCoin.rectTransform, 1, 0.5f, -50, 0, 56, 56);
            _levelPopText = Ui.Title(_levelPop, "...", 44);
            Ui.Place(_levelPopText.rectTransform, 0.5f, 0.5f, -30, 2, 560, 70);
            Ui.Isolate(_levelPop.gameObject);
            _levelPop.gameObject.SetActive(false);

            // Coffre : se remplit d'une case par partie, s'ouvre toutes les 3 parties. Quand il est gagné, toute la
            // ligne devient un bouton doré « Ouvrir le coffre ! » (le coffre s'ouvre devant le joueur).
            // Coffre en attente : un vrai bouton « bonbon » doré, avec un petit coffre qui gigote dedans.
            _chestBtn = Ui.Button(card.transform, Loc.Get("chest_open"), Palette.Accent, () => App.OpenRunChest(), null, 50, "OuvrirCoffre");
            Ui.Place((RectTransform)_chestBtn.transform, 0.5f, 1, 0, -522, 800, 130);
            var btnLabel = Ui.ButtonLabel(_chestBtn);
            if (btnLabel != null) btnLabel.transform.parent.GetComponent<RectTransform>().offsetMin = new Vector2(130, 0);
            _chestBtnIcon = MetaUi.MiniChest(_chestBtn.transform, Art, 92);
            Ui.Place(_chestBtnIcon, 0, 0.5f, 40, 8, 92, 92 * 0.9f, 0, 0.5f);
            // Sinon : petit coffre, texte lisible (encre sur la carte) et trois cases.
            _chestIcon = MetaUi.MiniChest(card.transform, Art, 84);
            Ui.Place(_chestIcon, 0, 1, 34, -508, 84, 84 * 0.9f, 0, 0.5f);
            _chestText = Ui.Label(card.transform, "...", 42, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(_chestText.rectTransform, 0, 1, 132, -508, 560, 60, 0, 0.5f);
            for (int i = 0; i < _chestPips.Length; i++)
            {
                // Trois étoiles dorées (icône du jeu) : pleines une à une, grises tant qu'elles ne sont pas gagnées.
                _chestPips[i] = Ui.Icon(card.transform, Art.IconSprite(Icon.Star), "Case");
                Ui.Place(_chestPips[i].rectTransform, 1, 1, -60 - (_chestPips.Length - 1 - i) * 72, -508, 64, 64, 0.5f, 0.5f);
            }

            _heads = Ui.Rect("NouvellesTetes", card.transform);
            Ui.Place(_heads, 0.5f, 0, 0, 140, 880, 230);
            var h = _heads.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;

            // Prochain objectif (tête de score, tête de la boutique ou de niveau) : la carotte qui donne envie de rejouer.
            var nb = Ui.Rect("ProchaineTete", card.transform);
            _nextBlock = nb.gameObject;
            Ui.Place(nb, 0.5f, 0, 0, 128, 860, 220);
            _nextPortrait = Ui.Icon(nb, null, "Portrait");
            Ui.Place(_nextPortrait.rectTransform, 0, 0.5f, 30, 0, 200, 200, 0, 0.5f);
            _nextText = Ui.Label(nb, "", 46, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(_nextText.rectTransform, 0, 0.5f, 260, 58, 580, 84, 0, 0.5f);
            _nextText.resizeTextForBestFit = true; _nextText.resizeTextMinSize = 38; _nextText.resizeTextMaxSize = _nextText.fontSize;
            var track = Ui.Image(nb, Art.Pill, new Color(0.17f, 0.18f, 0.26f, 0.2f), "Barre", true);
            Ui.Place(track.rectTransform, 0, 0.5f, 260, -50, 560, 44, 0, 0.5f);
            CapsuleSlice.Attach(track);
            _nextTrack = track.gameObject;
            _nextBuy = Ui.Button(nb, Loc.Get("see_head"), Palette.Play, () => App.OpenShopHeads(), Icon.Bag, 44, "Voir");
            Ui.Place((RectTransform)_nextBuy.transform, 0, 0.5f, 260, -42, 340, 132, 0, 0.5f); // 132 px : hauteur minimale d'un bouton « bonbon » (bords non écrasés)
            _nextFill = Ui.Image(track.transform, Art.Pill, Palette.Accent, "Remplissage", true);
            CapsuleSlice.Attach(_nextFill);
            MetaUi.Shine(_nextFill);
            var fr = _nextFill.rectTransform;
            fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(0, 1); fr.pivot = new Vector2(0, 0.5f);

            // Pièces gagnées, et x2 en vidéo (au choix).
            _coins = MetaUi.CoinPill(Safe, Art, out var coinsRt, 56);
            Ui.Place(coinsRt, 0.5f, 1, -220, -1155, 340, 116);
            _coinsRt = coinsRt;
            _double = Ui.Button(Safe, Loc.Get("coins_double"), Palette.Purple, App.DoubleRunCoins, Icon.Film, 54, "Doubler");
            Ui.Place((RectTransform)_double.transform, 0.5f, 1, 220, -1155, 330, 124);
            _double.gameObject.AddComponent<Pulse>().Amount = 0.04f;

            _revive = Ui.Button(Safe, Loc.Get("revive"), Palette.Play, App.ReviveWithAd, Icon.Film, 60, "Revivre");
            Ui.Place((RectTransform)_revive.transform, 0.5f, 0, 0, 610, 780, 160);
            _revive.gameObject.AddComponent<Pulse>().Amount = 0.03f;
            var replay = Ui.Button(Safe, Loc.Get("replay"), Palette.Primary, App.Replay, Icon.Replay, 64, "Rejouer");
            Ui.Place((RectTransform)replay.transform, 0.5f, 0, 0, 430, 780, 170);
            var share = Ui.Button(Safe, Loc.Get("share"), Palette.Secondary, App.ShareScore, Icon.Share, 50, "Partager");
            Ui.Place((RectTransform)share.transform, 0.5f, 0, -200, 260, 380, 140);
            _sharePulse = share.gameObject.AddComponent<Pulse>();
            _sharePulse.Amount = 0.05f;
            var home = Ui.Button(Safe, Loc.Get("home"), Palette.Dark, App.GoHome, Icon.Home, 50, "Accueil");
            Ui.Place((RectTransform)home.transform, 0.5f, 0, 200, 260, 380, 140);
        }

        public void Present(RunResult r, RunRewards rewards, EconomyGain gain, bool canRevive, bool reviveFree, Texture card)
        {
            Show();
            _gain = gain;
            _xpV = gain.XpFractionBefore;
            _xpTo = gain.LevelsGained + gain.XpFractionAfter;
            _xpWait = 0.7f; // petite attente : la carte apparaît, puis la barre se remplit
            _levelCoinsShown = 0;
            _chestOpened = false;
            _levelPunch = _coinsPunch = 0f;
            _levelPopT = -1f;
            _levelPop.gameObject.SetActive(false);
            _level.text = Loc.Format("level", gain.LevelBefore);
            _level.color = Palette.Secondary;
            MetaUi.SetFill(_xpFill, _xpV);
            var brag = App.BragOf(r, rewards);
            _subtitle.text = Loc.Format("game_over", App.Tuning.mascot.mascotName);
            _title.text = Loc.Format("title_label", Brag.Title(brag));
            _title.color = rewards.NewBest ? Palette.Accent : Color.white;
            _score.GetComponent<CountUp>().Run(r.Score);
            _best.text = rewards.NewBest ? Loc.Get("new_best") : Loc.Format("best", Loc.FormatScore(Mathf.Max(rewards.PreviousBest, r.Score)));
            _best.color = rewards.NewBest ? Palette.Primary : Palette.Muted;
            var newHeads = new List<HeadDef>(rewards.NewHeads);
            newHeads.AddRange(gain.LevelHeads);
            _rays.gameObject.SetActive(rewards.NewBest || newHeads.Count > 0);
            int? gap = Brag.PointsFromBest(brag);
            _details.text = r.BestSave >= 0.85f ? Loc.Format("near_miss", NearMissTracker.DisplayPercent(r.BestSave))
                : gap.HasValue ? Loc.Format("close_to_best", Loc.FormatScore(gap.Value))
                : (r.BestChain >= 2 ? Loc.Format("best_combo", r.BestChain) : "");
            _xp.text = Loc.Format("xp_gain", gain.Xp);
            // Les pièces du coffre s'ajoutent quand on l'ouvre ; celles des niveaux, quand la barre passe chaque niveau.
            RefreshCoins();
            _double.gameObject.SetActive(gain.Coins >= 10);
            // Le bouton Partager s'anime aux moments dont on a envie de parler : record, sauvetage fou, record frôlé.
            bool worthSharing = rewards.NewBest || r.BestSave >= 0.95f || gap.HasValue;
            _sharePulse.enabled = worthSharing;
            if (!worthSharing) _sharePulse.transform.localScale = Vector3.one;
            _double.interactable = true;

            Ui.Clear(_heads);
            bool any = newHeads.Count > 0;
            _heads.gameObject.SetActive(any);
            for (int i = 0; i < newHeads.Count && i < 3; i++)
            {
                var cell = Ui.Rect("Tete", _heads);
                cell.sizeDelta = new Vector2(250, 230);
                // Halo doré derrière la tête gagnée.
                var halo = Ui.Image(cell, Art.SoftDisc, new Color(1f, 0.82f, 0.3f, 0.75f), "Halo");
                Ui.Place(halo.rectTransform, 0.5f, 1, 0, 30, 250, 250, 0.5f, 1);
                halo.raycastTarget = false;
                var icon = Ui.Icon(cell, Art.Portrait(newHeads[i]));
                Ui.Place(icon.rectTransform, 0.5f, 1, 0, 0, 176, 176, 0.5f, 1);
                // Étiquette « NOUVEAU ! » : on voit tout de suite que la tête vient d'être débloquée.
                var ntag = Ui.Image(cell, Art.Pill, Palette.Primary, "Nouveau", true);
                Ui.Place(ntag.rectTransform, 1, 1, 6, 10, 168, 50, 1, 1);
                ntag.transform.localRotation = Quaternion.Euler(0, 0, -10);
                ntag.gameObject.AddComponent<Pulse>().Amount = 0.08f;
                var nt = Ui.Title(ntag.transform, Loc.Get("new_head_tag"), 30);
                Ui.Stretch(nt.rectTransform, 8, 0, 8, 3);
                // Le nom posé sous la tête, dans la case (pivot en bas) : il ne déborde plus de la carte.
                var name = Ui.Title(cell, newHeads[i].Name(Loc.Language), 36);
                Ui.Place(name.rectTransform, 0.5f, 0, 0, 2, 250, 50, 0.5f, 0);
                cell.gameObject.AddComponent<PopIn>().Delay = 0.4f + i * 0.15f;
            }

            _nextBlock.SetActive(!any);
            if (!any) ShowNextGoal(r);

            bool free = reviveFree;
            Ui.ButtonLabel(_revive).text = Loc.Get(free ? "revive_free" : "revive");
            _revive.gameObject.SetActive(canRevive);

            // Coffre : cases remplies (le coffre ouvert montre les 3 cases pleines).
            int filled = gain.Chest != null ? Chest.GamesPerChest : gain.ChestProgress;
            for (int i = 0; i < _chestPips.Length; i++)
            {
                _chestPips[i].color = i < filled ? Palette.Accent : new Color(0.45f, 0.5f, 0.6f, 0.45f);
                _chestPips[i].rectTransform.localScale = Vector3.one;
            }
            SetChestRow(gain.Chest != null, false);
            _chestPop = gain.Chest != null ? 1.2f : 0f;

            // Récompenses de la partie : avec un coffre, elles sont listées dans l'écran du coffre. Sans coffre, elles
            // jaillissent une par une dans la carte, après la barre d'XP (aucun bandeau par-dessus l'écran).
            _runRewards.Clear();
            _runRewardIndex = 0;
            _runRewardCoinsShown = 0;
            if (gain.Chest == null)
            {
                if (gain.GoalReached) _runRewards.Add(new KeyValuePair<string, int>(Loc.Get("goal_reached"), gain.GoalCoins));
                if (gain.DailyObjective != null) _runRewards.Add(new KeyValuePair<string, int>(Loc.Get("daily_objective_done"), gain.DailyCoins));
                if (gain.MissionsDone.Count > 0)
                {
                    int mc = 0; foreach (var m in gain.MissionsDone) mc += m.reward;
                    _runRewards.Add(new KeyValuePair<string, int>(gain.MissionsDone.Count > 1 ? Loc.Format("missions_done_n", gain.MissionsDone.Count) : Loc.Get("mission_title"), mc));
                }
                if (gain.MissionsBonus) _runRewards.Add(new KeyValuePair<string, int>(Loc.Get("missions_bonus_done"), gain.BonusCoins));
            }
            _runRewardCoins = 0;
            foreach (var rw in _runRewards) _runRewardCoins += rw.Value;
            RefreshCoins();
        }

        void SetChestRow(bool waiting, bool opened)
        {
            bool button = waiting && !opened;
            _chestBtn.gameObject.SetActive(button);
            _chestIcon.gameObject.SetActive(!button);
            _chestText.gameObject.SetActive(!button);
            foreach (var pip in _chestPips) pip.gameObject.SetActive(!button);
            int left = Chest.GamesPerChest - (_gain?.ChestProgress ?? 0);
            _chestText.text = opened ? Loc.Format("chest_opened", Loc.FormatScore(_gain?.Chest?.Coins ?? 0))
                : left <= 1 ? Loc.Get("chest_next_1") : Loc.Format("chest_next_n", left);
        }

        /// <summary>Le coffre vient d'être ouvert : ses pièces rejoignent le total.</summary>
        public void OnChestOpened()
        {
            if (_gain == null) return;
            _chestOpened = true;
            SetChestRow(true, true);
            RefreshCoins();
        }

        /// <summary>
        /// Pièces gagnées affichées : la partie, les missions et l'objectif tout de suite ; les niveaux au moment où la barre
        /// les passe ; le coffre quand on l'ouvre ; le double quand la vidéo est vue.
        /// </summary>
        void RefreshCoins()
        {
            if (_gain == null) return;
            int total = _gain.Total - _gain.LevelCoins + _levelCoinsShown - (_runRewardCoins - _runRewardCoinsShown)
                - (_chestOpened ? 0 : (_gain.Chest?.Coins ?? 0))
                + (App.CoinsDoubled ? _gain.Coins : 0);
            _coins.text = "+" + Loc.FormatScore(Mathf.Max(0, total));
        }

        /// <summary>La barre vient de passer un niveau : récompense visible, son, vibration, pièces au compteur.</summary>
        void OnLevelReached(int level)
        {
            var eco = App.Tuning.economy;
            int coins = Levels.LevelCoins(eco, level);
            _levelCoinsShown = Mathf.Min(_gain.LevelCoins, _levelCoinsShown + coins);
            RefreshCoins();
            _level.text = Loc.Format("level", level);
            _level.color = Palette.Accent;
            _levelPunch = 1f;
            _coinsPunch = 1f;
            ShowPop(Loc.Format("level", level) + "  +" + Loc.FormatScore(coins));
            _xpFillImage.color = Palette.Accent;
            App.PlaySfx(Sfx.Chime, Mathf.Clamp(2 + level - _gain.LevelBefore, 1, 8));
            App.HapticMedium();
        }

        /// <summary>Pastille dorée qui jaillit du bout de la barre et monte en s'effaçant (niveau, objectif, missions).</summary>
        void ShowPop(string text)
        {
            _levelPopText.text = text;
            _levelPopT = 0f;
            _levelPop.gameObject.SetActive(true);
        }

        void TickXp(float dt)
        {
            if (_xpWait > 0f) { _xpWait -= dt; return; }
            // Barre finie : les autres récompenses de la partie, une par une.
            if (_xpV >= _xpTo - 1e-4f && _runRewardIndex < _runRewards.Count)
            {
                var r = _runRewards[_runRewardIndex++];
                _runRewardCoinsShown += r.Value;
                RefreshCoins();
                ShowPop(r.Value > 0 ? r.Key + "  +" + Loc.FormatScore(r.Value) : r.Key);
                _coinsPunch = 1f;
                App.PlaySfx(Sfx.Chime, 4 + _runRewardIndex);
                _xpWait = 1.1f;
                return;
            }
            if (_xpV < _xpTo - 1e-4f)
            {
                // Prochain arrêt : le niveau suivant s'il est atteint pendant cette partie, sinon la fin de la barre.
                float next = Mathf.Floor(_xpV + 1e-4f) + 1f;
                bool toLevel = next <= _xpTo + 1e-4f;
                float stop = toLevel ? next : _xpTo;
                // Un niveau entier se remplit en ~0,8 s (plus vite s'il y en a beaucoup), et ralentit à l'approche de l'arrêt.
                float speed = (App.Profile.settings.reducedMotion ? 2.5f : 1.25f) * (_gain.LevelsGained > 3 ? 1.6f : 1f);
                float left = stop - _xpV;
                _xpV = Mathf.MoveTowards(_xpV, stop, dt * speed * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(left * 4f)));
                if (toLevel && _xpV >= stop - 1e-4f)
                {
                    _xpV = stop;
                    MetaUi.SetFill(_xpFill, 1f);
                    OnLevelReached(_gain.LevelBefore + (int)Mathf.Round(stop));
                    _xpWait = 0.55f; // le temps de voir la récompense
                    return;
                }
            }
            int whole = Mathf.Min(_gain.LevelsGained, Mathf.FloorToInt(_xpV + 1e-4f));
            float frac = _xpV - whole;
            MetaUi.SetFill(_xpFill, frac);
            if (_levelPunch <= 0f) _level.text = Loc.Format("level", _gain.LevelBefore + whole);
        }

        void TickXpFx(float dt)
        {
            _levelPunch = Mathf.MoveTowards(_levelPunch, 0f, dt * 2.2f);
            _coinsPunch = Mathf.MoveTowards(_coinsPunch, 0f, dt * 2.5f);
            _level.rectTransform.localScale = Vector3.one * (1f + 0.35f * _levelPunch * _levelPunch);
            if (_levelPunch <= 0f && _level.color != Palette.Secondary) _level.color = Palette.Secondary;
            _coinsRt.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(_coinsPunch * Mathf.PI));
            _xpFillImage.color = Color.Lerp(Palette.Play, Palette.Accent, _levelPunch);
            if (_levelPopT >= 0f)
            {
                _levelPopT += dt;
                float t = _levelPopT / 1.3f;
                float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) * 6f));
                _levelPop.localScale = Vector3.one * pop;
                _levelPop.anchoredPosition = new Vector2(470f, -418f + 70f + 50f * Mathf.Clamp01(t));
                _levelPopGroup.alpha = t < 0.7f ? 1f : Mathf.Clamp01(1f - (t - 0.7f) / 0.3f);
                if (t >= 1f) { _levelPopT = -1f; _levelPop.gameObject.SetActive(false); }
            }
        }

        /// <summary>Choisit l'objectif le plus proche : la prochaine tête de score, de niveau ou de la boutique.</summary>
        void ShowNextGoal(RunResult r)
        {
            var p = App.Profile;
            int best = Mathf.Max(p.bestScore, r.Score);
            int coins = p.coins + (_gain?.Total ?? 0);
            float bestT = -1f;
            HeadDef goal = null;
            string text = "";

            var next = HeadCatalog.NextScoreHead(p.unlockedHeads);
            if (next != null)
            {
                int prev = 0;
                foreach (var h in HeadCatalog.All)
                    if (h.Unlock == UnlockKind.Score && h.UnlockValue < next.UnlockValue && h.UnlockValue > prev) prev = h.UnlockValue;
                float t = Mathf.Clamp01((best - prev) / (float)Mathf.Max(1, next.UnlockValue - prev));
                bestT = t; goal = next;
                text = Loc.Format("next_head_left", Loc.FormatScore(next.UnlockValue), Loc.FormatScore(best)); // une tête de score se gagne en une seule partie
            }
            var shop = HeadCatalog.SuggestedShopHead(p.unlockedHeads, coins, p.gamesPlayed);
            if (shop != null)
            {
                float t = Mathf.Clamp01(coins / (float)shop.UnlockValue);
                if (t > bestT) { bestT = t; goal = shop; text = coins >= shop.UnlockValue ? Loc.Format("can_buy_head", shop.Name(Loc.Language)) : Loc.Format("next_shop_head", Loc.FormatScore(shop.UnlockValue - coins), shop.Name(Loc.Language)); }
            }
            var lvl = HeadCatalog.NextLevelHead(p.unlockedHeads);
            if (lvl != null && _gain != null)
            {
                float t = Mathf.Clamp01((_gain.LevelAfter - 1 + _gain.XpFractionAfter) / Mathf.Max(1f, lvl.UnlockValue - 1));
                if (t > bestT) { bestT = t; goal = lvl; text = Loc.Format("next_level_head", lvl.UnlockValue); }
            }
            _nextBlock.SetActive(goal != null);
            if (goal == null) return;
            // Tête de la boutique déjà payable : on la montre en couleur, avec un bouton pour aller l'acheter
            // (au lieu d'une barre pleine qui ne disait rien).
            bool affordable = goal.Unlock == UnlockKind.Shop && coins >= goal.UnlockValue;
            _nextPortrait.sprite = affordable ? Art.Portrait(goal) : Art.Locked(goal);
            _nextTrack.SetActive(!affordable);
            _nextBuy.gameObject.SetActive(affordable);
            _nextText.text = text;
            _nextFill.color = goal.Unlock == UnlockKind.Shop ? Palette.Accent : goal.Unlock == UnlockKind.Level ? Palette.Play : Palette.Primary;
            _nextFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(44f, 560f * bestT), 0);
        }

        public override void Tick(float dt)
        {
            if (_gain == null) return;
            TickXp(dt);
            TickXpFx(dt);
            if (_chestBtn != null && _chestBtn.gameObject.activeSelf && !App.Profile.settings.reducedMotion)
            {
                float now = Time.unscaledTime, shake = Mathf.Repeat(now, 1.3f) < 0.45f ? 1f : 0f;
                _chestBtnIcon.localEulerAngles = new Vector3(0, 0, Mathf.Sin(now * 30f) * 7f * shake);
                _chestBtnIcon.localScale = Vector3.one * (1f + 0.06f * shake * Mathf.Abs(Mathf.Sin(now * 15f)));
            }
            if (_chestPop > 0f)
            {
                // Le coffre sautille quand il s'ouvre.
                _chestPop -= dt;
                float b = Mathf.Abs(Mathf.Sin(_chestPop * 9f)) * Mathf.Clamp01(_chestPop) * 0.3f;
                _chestIcon.localScale = Vector3.one * (1f + b);
                if (_chestPop <= 0f) _chestIcon.localScale = Vector3.one;
            }
        }

        public void OnDoubled()
        {
            _double.interactable = false;
            _double.gameObject.SetActive(false);
            RefreshCoins();
            if (_gain != null)
            {
                ShowPop(Loc.Get("coins_doubled") + "  +" + Loc.FormatScore(_gain.Coins));
                _coinsPunch = 1f;
                App.PlaySfx(Sfx.Chime, 6);
            }
        }

        public void HideRevive() => _revive.gameObject.SetActive(false);
    }

    // ===================================================================== Collection

    public sealed class CollectionScreen : UiScreen
    {
        // Organisation : en haut la tête portée (grande, avec sa rareté) et la progression ; puis des onglets par
        // famille de têtes ; dans chaque onglet, une grille triée par difficulté, la prochaine à gagner signalée.
        RectTransform _col, _area;
        Text _count, _coins, _wornName, _wornRarity;
        Image _wornPortrait, _wornHalo, _wornChip;
        UiSwitch _random;
        RectTransform _progressFill;
        string _tab = "sec_score";
        readonly Dictionary<string, Button> _tabs = new Dictionary<string, Button>();
        readonly Dictionary<string, Text> _tabCounts = new Dictionary<string, Text>();
        readonly Dictionary<string, (GameObject go, Text text)> _tabNews = new Dictionary<string, (GameObject, Text)>();
        // Portraits pas encore en mémoire : ils apparaissent au fur et à mesure, sans bloquer l'ouverture du menu.
        readonly List<(Image image, HeadDef head, bool locked)> _pending = new List<(Image, HeadDef, bool)>();

        static readonly string[] Tabs = { "sec_score", "sec_level", "sec_shop", "sec_special", "sec_premium" };

        public static Color RarityColor(Rarity r) =>
            r == Rarity.Common ? ArtCache.Hex("#8D99AE") : r == Rarity.Rare ? ArtCache.Hex("#4A9BFF") : r == Rarity.Epic ? ArtCache.Hex("#A66CFF") : ArtCache.Hex("#FFB627");

        static bool InTab(string tab, HeadDef h)
        {
            switch (tab)
            {
                case "sec_score": return h.Unlock == UnlockKind.Score || h.Unlock == UnlockKind.Default;
                case "sec_level": return h.Unlock == UnlockKind.Level;
                case "sec_shop": return h.Unlock == UnlockKind.Shop;
                case "sec_special": return h.IsSpecial;
                default: return h.Unlock == UnlockKind.Premium;
            }
        }

        protected override void Build()
        {
            BackButton(App.GoHome);
            var header = Header(Loc.Get("heads"));
            header.rectTransform.anchoredPosition = new Vector2(-70, header.rectTransform.anchoredPosition.y);
            header.rectTransform.sizeDelta = new Vector2(440, header.rectTransform.sizeDelta.y);
            _coins = MetaUi.CoinPill(Safe, Art, out var crt, 46, () => App.ShowShop("collection"));
            Ui.Place(crt, 1, 1, -36, -60, 270, 104, 1, 1);

            // Tête portée.
            var hero = Ui.Card(Safe, "TetePortee");
            Ui.Place(hero.rectTransform, 0.5f, 1, 0, -236, 1008, 280, 0.5f, 1);
            _wornHalo = Ui.Image(hero.transform, Art.SoftDisc, Color.white, "Halo");
            Ui.Place(_wornHalo.rectTransform, 0, 0.5f, 10, 0, 280, 280, 0, 0.5f);
            _wornPortrait = Ui.Icon(hero.transform, null, "Portrait");
            Ui.Place(_wornPortrait.rectTransform, 0, 0.5f, 40, 0, 220, 220, 0, 0.5f);
            var label = Ui.Label(hero.transform, Loc.Get("worn_head"), 38, Palette.Muted, TextAnchor.MiddleLeft);
            Ui.Place(label.rectTransform, 0, 1, 300, -48, 420, 50, 0, 0.5f);
            _wornName = Ui.Title(hero.transform, "", 62, Color.white);
            Ui.Place(_wornName.rectTransform, 0, 1, 300, -104, 440, 76, 0, 0.5f);
            _wornName.alignment = TextAnchor.MiddleLeft;
            _wornChip = Ui.Image(hero.transform, Art.Pill, Color.white, "Rarete", true);
            Ui.Place(_wornChip.rectTransform, 0, 1, 300, -164, 230, 56, 0, 0.5f);
            _wornRarity = Ui.Title(_wornChip.transform, "", 36);
            Ui.Stretch(_wornRarity.rectTransform, 12, 0, 12, 4);
            // Tête au hasard : un interrupteur en haut à droite de la carte.
            var rnd = Ui.Label(hero.transform, Loc.Get("random_head"), 36, Palette.Muted, TextAnchor.MiddleRight);
            Ui.Place(rnd.rectTransform, 1, 1, -36, -46, 230, 48, 1, 0.5f);
            _random = UiSwitch.Create(hero.transform, Art, App.Profile.randomHead);
            Ui.Place((RectTransform)_random.transform, 1, 1, -36, -112, 140, 78, 1, 0.5f);
            var swImg = _random.GetComponent<Image>();
            swImg.raycastTarget = true;
            _random.gameObject.AddComponent<Button>().onClick.AddListener(() => { Ui.Click?.Invoke(); App.SetRandomHead(!App.Profile.randomHead); });
            // Progression de la collection.
            _count = Ui.Title(hero.transform, "", 44, Palette.Accent);
            Ui.Place(_count.rectTransform, 1, 0, -36, 92, 280, 60, 1, 0.5f);
            _count.alignment = TextAnchor.MiddleRight;
            _progressFill = MetaUi.Bar(hero.transform, Art, Palette.Accent, "Progression");
            Ui.Place((RectTransform)_progressFill.parent, 1, 0, -36, 44, 400, 30, 1, 0.5f);

            // Onglets : une famille de têtes à la fois (moins long à parcourir, et on voit ce qui reste dans chacune).
            var bar = Ui.Glass(Safe, "Onglets");
            Ui.Place(bar.rectTransform, 0.5f, 1, 0, -536, 1008, 128, 0.5f, 1);
            for (int i = 0; i < Tabs.Length; i++)
            {
                string tab = Tabs[i];
                var b = Ui.Button(bar.transform, Loc.Get(tab), Palette.Dark, () => { _tab = tab; Refresh(); }, null, 36, "Onglet");
                Ui.Place((RectTransform)b.transform, 0, 0.5f, 10 + i * 198, 4, 196, 112, 0, 0.5f);
                _tabs[tab] = b;
                var cnt = Ui.Label(b.transform, "", 30, ArtCache.Hex("#D8E9FF"));
                Ui.Place(cnt.rectTransform, 0.5f, 0, 0, 22, 180, 34, 0.5f, 0);
                _tabCounts[tab] = cnt;
                // Pastille rouge : têtes de cet onglet gagnées mais pas encore vues.
                var nb = MetaUi.Badge(b.transform, Art, out var nbGo);
                Ui.Place((RectTransform)nbGo.transform, 1, 1, 14, 14, 50, 50, 1, 1);
                var nd = nbGo.GetComponent<Image>(); nd.sprite = Art.Circle; nd.type = Image.Type.Simple;
                var nr = nbGo.AddComponent<Outline>(); nr.effectColor = Color.white; nr.effectDistance = new Vector2(3, -3);
                nb.fontSize = Mathf.RoundToInt(30 * Ui.TextScale);
                Ui.Stretch(nb.rectTransform, -4, -2, -4, 3);
                nbGo.SetActive(false);
                _tabNews[tab] = (nbGo, nb);
                // Le libellé de l'onglet remonte pour laisser la place au compte.
                var lbl = Ui.ButtonLabel(b);
                if (lbl != null) lbl.transform.parent.GetComponent<RectTransform>().offsetMin = new Vector2(18, 46);
            }

            _area = Ui.Rect("Zone", Safe);
            Ui.Stretch(_area, 24, 680, 24, 20);
            _col = Ui.Scroll(_area, out _);
            Ui.Column(_col, 10);
            _col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, 40);
        }

        /// <summary>Ouvre la collection sur un onglet (« sec_shop » : les têtes de la boutique).</summary>
        public void SetTab(string tab) { if (System.Array.IndexOf(Tabs, tab) >= 0) _tab = tab; }

        /// <summary>À l'ouverture : directement sur l'onglet qui contient des têtes nouvelles, s'il y en a.</summary>
        public void OpenOnNews()
        {
            var p = App.Profile;
            if (HeadNews.Count(p, h => InTab(_tab, h)) > 0) return;
            foreach (var tab in Tabs)
                if (HeadNews.Count(p, h => InTab(tab, h)) > 0) { _tab = tab; return; }
        }

        // Têtes nouvelles montrées pendant cette visite (étiquette « NOUVEAU ! » jusqu'à la fermeture du menu).
        readonly HashSet<int> _shownNew = new HashSet<int>();

        public override void Hide() { base.Hide(); _shownNew.Clear(); }

        public override void Refresh()
        {
            var p = App.Profile;
            Ui.Clear(_col);
            _pending.Clear();
            _coins.text = Loc.FormatScore(p.coins);

            var worn = HeadCatalog.Get(p.selectedHead);
            var rarity = HeadCatalog.RarityOf(worn);
            _wornPortrait.sprite = Art.Portrait(worn);
            _wornName.text = worn.Name(Loc.Language);
            var rc = RarityColor(rarity);
            _wornHalo.color = new Color(rc.r, rc.g, rc.b, 0.45f);
            _wornChip.color = rc;
            _wornRarity.text = Loc.Get("rarity_" + rarity);
            if (_random != null && _random.On != p.randomHead) _random.On = p.randomHead;
            _count.text = Loc.Format("collection_short", p.unlockedHeads.Count, HeadCatalog.All.Count);
            MetaUi.SetFill(_progressFill, p.unlockedHeads.Count / (float)Mathf.Max(1, HeadCatalog.All.Count));

            foreach (var tab in Tabs)
            {
                int total = 0, owned = 0;
                foreach (var h in HeadCatalog.All) if (InTab(tab, h)) { total++; if (p.unlockedHeads.Contains(h.Id)) owned++; }
                _tabs[tab].GetComponent<Image>().color = tab == _tab ? Palette.Primary : Palette.Dark;
                _tabCounts[tab].text = owned + "/" + total;
                int fresh = HeadNews.Count(p, h => InTab(tab, h));
                var (newGo, newText) = _tabNews[tab];
                newGo.SetActive(fresh > 0 && tab != _tab);
                newText.text = fresh > 9 ? "9+" : fresh.ToString();
            }

            var heads = new List<HeadDef>();
            foreach (var h in HeadCatalog.All) if (InTab(_tab, h)) heads.Add(h);
            // Tri par difficulté (les têtes premium gardent l'ordre du catalogue).
            if (_tab != "sec_premium" && _tab != "sec_special") heads.Sort((a, b) => a.UnlockValue.CompareTo(b.UnlockValue));
            HeadDef next = null;
            if (_tab == "sec_score" || _tab == "sec_level" || _tab == "sec_shop")
                foreach (var h in heads) if (!p.unlockedHeads.Contains(h.Id)) { next = h; break; }

            var gridRt = Ui.Rect("Grille", _col);
            int rows = (heads.Count + 2) / 3;
            Ui.Size(gridRt, rows * 438 + 10);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(326, 420);
            grid.spacing = new Vector2(14, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            // Les têtes nouvelles de l'onglet affiché sont vues maintenant (elles gardent « NOUVEAU ! » pendant cette visite).
            foreach (var h in heads) if (HeadNews.IsNew(p, h.Id)) _shownNew.Add(h.Id);
            foreach (var h in heads) Cell(gridRt, h, h == next);
            var seen = new List<int>();
            foreach (var h in heads) seen.Add(h.Id);
            if (HeadNews.MarkSeen(p, seen)) App.SaveProfileNow();

            // Les portraits qui manquent : dessinés tout de suite en arrière-plan, en priorité.
            var urgent = new List<KeyValuePair<string, Func<Raster>>>();
            foreach (var (_, head, locked) in _pending) if (!Art.PortraitReady(head, locked)) urgent.Add(ArtCache.PortraitJob(head, locked));
            if (urgent.Count > 0) Art.PrepareInBackground(urgent, true);
        }

        public override void Tick(float dt)
        {
            // Au plus 2 portraits chargés par image (et 1 seul s'il faut le dessiner) : le menu reste fluide.
            int budget = 2;
            for (int i = 0; i < _pending.Count && budget > 0;)
            {
                var (image, head, locked) = _pending[i];
                if (image == null) { _pending.RemoveAt(i); continue; }
                bool ready = Art.PortraitReady(head, locked);
                if (!ready && Art.Preparing) { i++; continue; } // encore en préparation en arrière-plan : on attend
                image.sprite = locked ? Art.Locked(head) : Art.Portrait(head);
                image.color = Color.white;
                _pending.RemoveAt(i);
                budget -= ready ? 1 : 2;
            }
        }

        void Cell(Transform parent, HeadDef h, bool isNext)
        {
            var p = App.Profile;
            bool owned = p.unlockedHeads.Contains(h.Id);
            bool worn = p.selectedHead == h.Id;
            bool buyable = !owned && h.Unlock == UnlockKind.Shop;
            bool affordable = buyable && p.coins >= h.UnlockValue;
            var rarity = HeadCatalog.RarityOf(h);
            var rc = RarityColor(rarity);
            var cell = Ui.Card(parent, "Tete" + h.Id);
            cell.color = worn ? ArtCache.Hex("#FFE9A8") : owned ? Color.white : affordable ? ArtCache.Hex("#FFF4D6") : new Color(0.8f, 0.81f, 0.87f, 1f);
            cell.raycastTarget = true;
            var b = cell.gameObject.AddComponent<Button>();
            b.onClick.AddListener(() => { Ui.Click?.Invoke(); if (owned) App.SelectHead(h.Id); else App.TapLockedHead(h); });
            cell.gameObject.AddComponent<PressScale>();

            // Bandeau et halo à la couleur de la rareté.
            var band = Ui.Image(cell.transform, Art.Pill, rc, "Rarete", true);
            Ui.Place(band.rectTransform, 0.5f, 1, 0, -16, 240, 12, 0.5f, 1);
            var halo = Ui.Image(cell.transform, Art.SoftDisc, new Color(rc.r, rc.g, rc.b, owned ? 0.42f : 0.18f), "Halo");
            Ui.Place(halo.rectTransform, 0.5f, 1, 0, -26, 270, 270, 0.5f, 1);
            if (rarity == Rarity.Legendary && owned) halo.gameObject.AddComponent<Pulse>().Amount = 0.05f;

            // Les têtes à acheter se montrent en couleur : c'est ce qui donne envie.
            bool locked = !(owned || buyable || h.Unlock == UnlockKind.Premium);
            var icon = Ui.Icon(cell.transform, Art.PortraitIfLoaded(h, locked));
            if (icon.sprite == null)
            {
                icon.sprite = Art.SoftDisc;
                icon.color = new Color(0.17f, 0.18f, 0.26f, 0.12f);
                _pending.Add((icon, h, locked));
            }
            Ui.Place(icon.rectTransform, 0.5f, 1, 0, -36, 236, 236, 0.5f, 1);
            if (locked)
            {
                var lockIcon = Ui.Icon(cell.transform, Art.IconSprite(Icon.Lock));
                Ui.Place(lockIcon.rectTransform, 0.5f, 1, 0, -140, 84, 84);
            }
            if (affordable || isNext) icon.gameObject.AddComponent<Pulse>().Amount = 0.04f;
            if (_shownNew.Contains(h.Id) && !worn)
            {
                // Tête gagnée que le joueur découvre : étiquette dorée qui pulse.
                var ntag = Ui.Image(cell.transform, Art.Pill, Palette.Accent, "Nouveau", true);
                Ui.Place(ntag.rectTransform, 0, 1, 6, -6, 190, 58, 0, 1);
                ntag.transform.localRotation = Quaternion.Euler(0, 0, 8);
                ntag.gameObject.AddComponent<Pulse>().Amount = 0.08f;
                var nt = Ui.Title(ntag.transform, Loc.Get("new_head_tag"), 34);
                Ui.Stretch(nt.rectTransform, 10, 0, 10, 4);
            }
            if (worn || isNext)
            {
                var tag = Ui.Image(cell.transform, Art.Pill, worn ? Palette.Primary : Palette.Play, "Etiquette", true);
                Ui.Place(tag.rectTransform, 1, 1, -6, -6, 176, 58, 1, 1);
                tag.transform.localRotation = Quaternion.Euler(0, 0, -8);
                var tt = Ui.Title(tag.transform, Loc.Get(worn ? "equipped" : "next_head"), 34);
                Ui.Stretch(tt.rectTransform, 10, 0, 10, 4);
            }

            bool showName = owned || buyable || h.Unlock == UnlockKind.Premium || isNext;
            var name = Ui.Label(cell.transform, showName ? h.Name(Loc.Language) : "???", 42, Palette.Ink);
            Ui.Place(name.rectTransform, 0.5f, 0, 0, 112, 300, 54);
            var rl = Ui.Label(cell.transform, Loc.Get("rarity_" + rarity), 30, rc);
            Ui.Place(rl.rectTransform, 0.5f, 0, 0, 78, 300, 36);
            if (buyable)
            {
                var price = Ui.Rect("Prix", cell.transform);
                Ui.Place(price, 0.5f, 0, 0, 36, 300, 56);
                var hl = price.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 6; hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = false;
                var coin = Ui.Icon(price, Art.IconSprite(Icon.Coin));
                coin.color = Palette.Accent;
                Ui.Size(coin, 48, 48);
                var pt = Ui.Label(price, Loc.FormatScore(h.UnlockValue), 42, affordable ? Palette.Success : Palette.Dark);
                pt.horizontalOverflow = HorizontalWrapMode.Overflow;
                return;
            }
            string sub = owned ? "" : Loc.UnlockText(h);
            var cond = Ui.Label(cell.transform, sub, 36, Palette.Dark);
            Ui.Place(cond.rectTransform, 0.5f, 0, 0, 36, 300, 48);
        }
    }

    // ===================================================================== Classement

    public sealed class LeaderboardScreen : UiScreen
    {
        string _board = Boards.World;
        RectTransform _list, _area;
        Text _status;
        Button _pseudo;
        GameObject _friendButtons;
        Button _myCode;
        readonly Dictionary<string, Button> _tabs = new Dictionary<string, Button>();
        readonly List<(Image image, int head)> _pending = new List<(Image, int)>();
        int _request;

        const string Mine = "mine", Friends = "friends";

        protected override void Build()
        {
            BackButton(App.GoHome);
            Header(Loc.Get("leaderboard"));
            var tabs = new[] { (Boards.World, "lb_world"), (Boards.Daily, "lb_daily"), (Friends, "lb_friends"), (Mine, "lb_mine") };
            for (int i = 0; i < tabs.Length; i++)
            {
                string board = tabs[i].Item1;
                var tab = Ui.Button(Safe, Loc.Get(tabs[i].Item2), Palette.Dark, () => { _board = board; Refresh(); }, null, 42, "Onglet");
                Ui.Place((RectTransform)tab.transform, 0.5f, 1, -378 + i * 252, -270, 244, 116);
                _tabs[board] = tab;
            }
            _status = Ui.Title(Safe, "", 44);
            Ui.Place(_status.rectTransform, 0.5f, 1, 0, -440, 920, 200); // assez haut pour un message sur trois lignes
            _status.resizeTextForBestFit = true; _status.resizeTextMinSize = 36; _status.resizeTextMaxSize = 44;

            _area = Ui.Rect("Liste", Safe);
            Ui.Stretch(_area, 36, 430, 36, 200);
            _list = Ui.Scroll(_area, out _);
            Ui.Column(_list, 14);

            // Amis : partager son code, ajouter celui d'un ami.
            var fb = Ui.Rect("BoutonsAmis", Safe);
            _friendButtons = fb.gameObject;
            Ui.Place(fb, 0.5f, 0, 0, 265, 1000, 140);
            var share = Ui.Button(fb, Loc.Get("friend_code_btn"), Palette.Purple, App.ShareFriendCode, Icon.Share, 44, "MonCode");
            _myCode = share;
            Ui.Place((RectTransform)share.transform, 0.5f, 0.5f, -250, 0, 480, 130);
            var add = Ui.Button(fb, Loc.Get("friend_add"), Palette.Play, App.AskFriendCode, Icon.Plus, 44, "Ajouter");
            Ui.Place((RectTransform)add.transform, 0.5f, 0.5f, 250, 0, 480, 130);

            _pseudo = Ui.Button(Safe, "...", Palette.Secondary, App.AskPseudo, Icon.Face, 48, "Pseudo");
            Ui.Place((RectTransform)_pseudo.transform, 0.5f, 0, 0, 100, 760, 140);
        }

        public override void Tick(float dt)
        {
            // Portraits chargés au fur et à mesure (jamais 50 dessins d'un coup à l'ouverture).
            int budget = 2;
            for (int i = 0; i < _pending.Count && budget > 0;)
            {
                var (image, headId) = _pending[i];
                if (image == null) { _pending.RemoveAt(i); continue; }
                var head = HeadCatalog.Get(headId);
                bool ready = Art.PortraitReady(head, false);
                if (!ready && Art.Preparing) { i++; continue; }
                image.sprite = Art.Portrait(head);
                image.color = Color.white;
                _pending.RemoveAt(i);
                budget -= ready ? 1 : 2;
            }
        }

        public override async void Refresh()
        {
            foreach (var kv in _tabs)
                kv.Value.GetComponent<Image>().color = kv.Key == _board ? Palette.Primary : Palette.Dark;
            var pl = Ui.ButtonLabel(_pseudo);
            if (pl != null) pl.text = string.IsNullOrEmpty(App.Profile.pseudo) ? Loc.Get("pseudo_title") : App.Profile.pseudo;
            bool friends = _board == Friends;
            _friendButtons.SetActive(friends);
            // Le code court s'affiche sur le bouton : on peut le lire à voix haute à un ami.
            var codeLabel = Ui.ButtonLabel(_myCode);
            if (codeLabel != null)
                codeLabel.text = string.IsNullOrEmpty(App.Profile.friendCode) ? Loc.Get("friend_code_btn")
                    : Loc.Format("friend_code_short", FriendCode.FormatShort(App.Profile.friendCode));
            Ui.Stretch(_area, 36, 430, 36, friends ? 345 : 200);
            Ui.Clear(_list);
            _pending.Clear();
            int request = ++_request;

            if (_board == Mine)
            {
                _status.text = "";
                var top = App.Profile.personalTop;
                if (top.Count == 0) _status.text = Loc.Get("lb_empty");
                for (int i = 0; i < top.Count; i++) Row(i + 1, App.Profile.pseudo, top[i], App.Profile.selectedHead, true);
                return;
            }

            var service = App.Leaderboard.Service;
            if (!service.IsOnline)
            {
                _status.text = Loc.Get(friends ? "friend_offline" : "lb_offline");
                int mine = _board == Boards.Daily ? (App.Profile.daily.bestDate == App.TodayKey ? App.Profile.daily.bestScoreToday : 0) : App.Profile.bestScore;
                if (mine > 0 && !friends) Row(0, App.Profile.pseudo, mine, App.Profile.selectedHead, true);
                return;
            }

            _status.text = Loc.Get("lb_loading");
            // Les scores pas encore envoyés partent d'abord : le classement affiché est à jour.
            await App.Leaderboard.FlushAsync(App.TodayKey);
            if (request != _request || !Visible) return;
            LeaderboardPage page;
            if (friends)
            {
                if (App.Profile.friends.Count == 0) { _status.text = Loc.Get("friends_empty"); return; }
                page = await service.GetFriendsAsync(Boards.World, App.Profile.friends);
            }
            else page = await service.GetTopAsync(_board, 50);
            if (request != _request || !Visible) return;
            // Classement du jour vide : on explique qu'il se remplit avec les parties du défi du jour (pas les parties normales).
            _status.text = page.Entries.Count == 0 ? Loc.Get(friends ? "friends_empty" : _board == Boards.Daily ? "lb_daily_empty" : "lb_empty") : "";
            foreach (var e in page.Entries)
            {
                // Joueurs bloqués ou signalés : masqués (le rang des autres ne change pas).
                if (!e.IsMe && Moderation.IsBlocked(App.Profile, e.PlayerId)) continue;
                // Pseudo des autres joueurs : filtré aussi à l'affichage (ancienne version, appareil modifié).
                Row(e.Rank, e.IsMe ? e.Name : PseudoFilter.Display(e.Name, Loc.Get("lb_player")), e.Score, e.Head, e.IsMe, e.PlayerId);
            }
            if (!friends && page.Me != null && !page.Entries.Exists(e => e.IsMe)) Row(page.Me.Rank, page.Me.Name, page.Me.Score, App.Profile.selectedHead, true);
        }

        void Row(int rank, string name, int score, int head, bool me, string playerId = null)
        {
            bool report = !me && !string.IsNullOrEmpty(playerId);
            var row = Ui.Card(_list, "Ligne");
            row.color = me ? ArtCache.Hex("#FFE9A8") : Color.white;
            Ui.Size(row, 150);
            var medal = rank >= 1 && rank <= 3;
            if (medal)
            {
                // Podium : médaille or, argent ou bronze, numéro blanc cerné.
                var disc = Ui.Image(row.transform, Art.Circle, new[] { ArtCache.Hex("#FFC93C"), ArtCache.Hex("#B8C2D6"), ArtCache.Hex("#E08A4F") }[rank - 1], "Medaille");
                Ui.Place(disc.rectTransform, 0, 0.5f, 34, 0, 96, 96, 0, 0.5f);
                var shine = Ui.Image(disc.transform, Art.Circle, new Color(1, 1, 1, 0.3f), "Reflet");
                Ui.Place(shine.rectTransform, 0.5f, 0.5f, -16, 18, 38, 26);
                var num = Ui.Title(disc.transform, rank.ToString(), 52);
                Ui.Stretch(num.rectTransform, 0, 0, 0, 4);
            }
            else
            {
                var r = Ui.Title(row.transform, rank > 0 ? rank.ToString() : "-", 52, me ? Palette.Primary : ArtCache.Hex("#AEB6C8"));
                Ui.Place(r.rectTransform, 0, 0.5f, 26, 0, 110, 110, 0, 0.5f);
            }
            var icon = Ui.Icon(row.transform, Art.PortraitIfLoaded(HeadCatalog.Get(head), false));
            if (icon.sprite == null) { icon.sprite = Art.SoftDisc; icon.color = new Color(0.17f, 0.18f, 0.26f, 0.12f); _pending.Add((icon, head)); }
            Ui.Place(icon.rectTransform, 0, 0.5f, 140, 0, 130, 130, 0, 0.5f);
            var n = Ui.Label(row.transform, string.IsNullOrEmpty(name) ? "-" : name, 48, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Place(n.rectTransform, 0, 0.5f, 290, 0, report ? 340 : 400, 100, 0, 0.5f);
            var s = Ui.Label(row.transform, Loc.FormatScore(score), 54, Palette.Ink, TextAnchor.MiddleRight);
            Ui.Place(s.rectTransform, 1, 0.5f, -36, 0, 240, 100, 1, 0.5f);
            if (report)
            {
                // Pseudo choquant : petit drapeau discret pour le signaler ou bloquer le joueur (App Store, règle 1.2).
                // Le dessin est petit, la zone touchable reste grande (88 × 100).
                string id = playerId, who = name;
                var hit = Ui.Rect("Signaler", row.transform);
                Ui.Place(hit, 1, 0.5f, -284, 0, 88, 100, 1, 0.5f);
                var hitImg = hit.gameObject.AddComponent<Image>();
                hitImg.color = new Color(0, 0, 0, 0);
                hit.gameObject.AddComponent<Button>().onClick.AddListener(() => App.ShowPlayerMenu(id, who));
                var flag = Ui.Icon(hit, Art.IconSprite(Icon.Flag), "Drapeau");
                Ui.Place(flag.rectTransform, 0.5f, 0.5f, 0, 0, 54, 54);
                flag.color = new Color(1f, 1f, 1f, 0.55f);
                flag.raycastTarget = false;
            }
        }
    }

    // ===================================================================== Réglages

    public sealed class SettingsScreen : UiScreen
    {
        // Réglages rangés en sections (cartes crème) : une pastille d'icône colorée, un libellé, et à droite un
        // interrupteur, un choix de taille ou une flèche. Les outils de prototype ne sont visibles que dans l'éditeur
        // et les versions de développement.
        RectTransform _col;
        const float RowH = 128f, Pad = 18f;

        protected override void Build()
        {
            BackButton(App.GoHome);
            Header(Loc.Get("settings"));
            var area = Ui.Rect("Zone", Safe);
            Ui.Stretch(area, 44, 240, 44, 20);
            _col = Ui.Scroll(area, out _);
            Ui.Column(_col, 14);
            _col.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 4, 40);
        }

        public override void Refresh()
        {
            Ui.Clear(_col);
            var s = App.Profile.settings;

            var sound = Section(Loc.Get("sec_sound"), 3);
            SwitchRow(sound, 0, Icon.Sound, Palette.Secondary, Loc.Get("sound"), s.sound, v => { s.sound = v; App.ApplySettings(false); });
            SwitchRow(sound, 1, Icon.Star, Palette.Purple, Loc.Get("music"), s.music, v => { s.music = v; App.ApplySettings(false); });
            SwitchRow(sound, 2, Icon.Vibrate, Palette.Success, Loc.Get("vibration"), s.vibration, v => { s.vibration = v; App.ApplySettings(false); });

            bool hfps = App.HighFrameRateAvailable;
            var display = Section(Loc.Get("sec_display"), hfps ? 4 : 3);
            TextSizeRow(display, 0, s);
            SwitchRow(display, 1, Icon.Play, Palette.Accent, Loc.Get("reduced_motion"), s.reducedMotion, v => { s.reducedMotion = v; App.ApplySettings(false); });
            SwitchRow(display, 2, Icon.Film, Palette.Primary, Loc.Get("creator_mode"), s.creatorMode, v =>
            {
                s.creatorMode = v;
                App.ApplySettings(false);
                if (v) App.Reward(Icon.Film, Palette.Purple, Loc.Get("creator_mode"), Loc.Get("creator_hint"), 0);
            });
            if (hfps) SwitchRow(display, 3, Icon.Flame, Palette.Secondary, Loc.Get("high_fps"), s.highFrameRate, v => { s.highFrameRate = v; App.ApplySettings(false); });

            if (App.NotificationsSupported)
            {
                var notif = Section(Loc.Get("sec_notif"), 1);
                SwitchRow(notif, 0, Icon.Bell, Palette.Accent, Loc.Get("notifications"), s.notifications, v => App.SetNotifications(v));
            }

            bool removeAds = !App.Profile.adsRemoved;
            var account = Section(Loc.Get("sec_account"), removeAds ? 4 : 3);
            int r = 0;
            LinkRow(account, r++, Icon.Star, Palette.Success, Loc.Get("stats"), App.ShowStats);
            LinkRow(account, r++, Icon.Bag, Palette.Primary, Loc.Get("shop"), () => App.ShowShop("reglages"));
            if (removeAds) LinkRow(account, r++, Icon.Film, Palette.Purple, Loc.Get("remove_ads"), App.BuyRemoveAds);
            LinkRow(account, r++, Icon.Replay, Palette.Dark, Loc.Get("restore"), App.RestorePurchases);

            bool online = App.Leaderboard.Service.IsOnline;
            bool blocked = App.Profile.blocked.Count > 0;
            var help = Section(Loc.Get("sec_help"), (online ? 3 : 2) + (blocked ? 1 : 0));
            LinkRow(help, 0, Icon.Bell, Palette.Secondary, Loc.Get("support"), App.OpenSupport);
            LinkRow(help, 1, Icon.Lock, Palette.Dark, Loc.Get("privacy"), App.OpenPrivacy);
            if (blocked) LinkRow(help, 2, Icon.Face, Palette.Secondary, Loc.Get("mod_unblock_all"), App.UnblockAll);
            if (online) LinkRow(help, blocked ? 3 : 2, Icon.Close, Palette.Primary, Loc.Get("delete_data"), App.AskDeleteOnlineData, true);

            // Outils de test : jamais dans la version publiée.
            if (Application.isEditor || Debug.isDebugBuild)
            {
                var proto = Section(Loc.Get("proto"), 4);
                string dir = s.tiltDirectionOverride == 0 ? Loc.Get("tilt_file") : s.tiltDirectionOverride > 0 ? Loc.Get("tilt_natural") : Loc.Get("tilt_inverse");
                LinkRow(proto, 0, Icon.Swords, Palette.Primary, $"{Loc.Get("tilt_dir")} : {dir}", () =>
                {
                    s.tiltDirectionOverride = s.tiltDirectionOverride == 0 ? 1 : s.tiltDirectionOverride > 0 ? -1 : 0;
                    App.ApplySettings(false);
                    Refresh();
                });
                LinkRow(proto, 1, Icon.Gear, Palette.Dark, Loc.Get("reload_tuning"), App.ReloadTuning);
                LinkRow(proto, 2, Icon.Coin, Palette.Accent, Loc.Get("debug_coins"), App.DebugAddCoins);
                LinkRow(proto, 3, Icon.Calendar, Palette.Accent, Loc.Get("debug_newday"), App.DebugNextDay);
            }

            var foot = Ui.Label(_col, Loc.Format("version", Application.version) + "  ·  " + Loc.Get("publisher"), 40, ArtCache.Hex("#BFE0FF"));
            foot.raycastTarget = true;
            foot.gameObject.AddComponent<Button>().onClick.AddListener(App.OnVersionTapped);
            Ui.Size(foot, 90);
        }

        /// <summary>Titre de section puis carte crème de <paramref name="rows"/> lignes.</summary>
        RectTransform Section(string title, int rows)
        {
            var t = Ui.Title(_col, title, 44, Palette.Accent);
            t.alignment = TextAnchor.LowerLeft;
            Ui.Size(t, 78);
            var card = Ui.Card(_col, "Section");
            Ui.Size(card, rows * RowH + Pad * 2);
            return card.rectTransform;
        }

        RectTransform Row(RectTransform card, int index, Icon icon, Color color, string label, Action onTap, bool danger = false)
        {
            var row = Ui.Rect("Ligne", card);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(-Pad * 2 - 24, RowH);
            row.anchoredPosition = new Vector2(0, -Pad - index * RowH);
            if (index > 0)
            {
                var line = Ui.Image(row, null, new Color(0.17f, 0.18f, 0.26f, 0.08f), "Separation");
                line.rectTransform.anchorMin = new Vector2(0, 1); line.rectTransform.anchorMax = new Vector2(1, 1);
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                line.rectTransform.sizeDelta = new Vector2(-126, 3);
                line.rectTransform.anchoredPosition = new Vector2(63, 0);
            }
            var disc = Ui.Image(row, Art.Circle, color, "Pastille");
            Ui.Place(disc.rectTransform, 0, 0.5f, 6, 0, 92, 92, 0, 0.5f);
            var ic = Ui.Icon(disc.transform, Art.IconSprite(icon), "Icone");
            Ui.Place(ic.rectTransform, 0.5f, 0.5f, 0, 0, 58, 58);
            var text = Ui.Label(row, label, 44, danger ? Palette.Primary : Palette.Ink, TextAnchor.MiddleLeft, "Libelle");
            text.rectTransform.anchorMin = new Vector2(0, 0); text.rectTransform.anchorMax = new Vector2(1, 1);
            text.rectTransform.offsetMin = new Vector2(124, 0); text.rectTransform.offsetMax = new Vector2(-190, 0);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 32; text.resizeTextMaxSize = text.fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            if (onTap != null)
            {
                var hit = row.gameObject.AddComponent<Image>();
                hit.color = new Color(1, 1, 1, 0);
                var b = row.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { Ui.Click?.Invoke(); onTap(); });
            }
            return row;
        }

        void SwitchRow(RectTransform card, int index, Icon icon, Color color, string label, bool value, Action<bool> set)
        {
            UiSwitch sw = null;
            var row = Row(card, index, icon, color, label, () => { sw.On = !sw.On; set(sw.On); });
            sw = UiSwitch.Create(row, Art, value);
            Ui.Place((RectTransform)sw.transform, 1, 0.5f, 0, 0, 150, 84, 1, 0.5f);
        }

        void LinkRow(RectTransform card, int index, Icon icon, Color color, string label, Action onTap, bool danger = false)
        {
            var row = Row(card, index, icon, color, label, onTap, danger);
            // Flèche à l'intérieur de la ligne, à 48 px du bord de la carte (jamais sur sa bordure).
            var arrow = Ui.Icon(row, Art.IconSprite(Icon.Back), "Fleche");
            arrow.color = new Color(0.55f, 0.6f, 0.68f, 0.9f);
            Ui.Place(arrow.rectTransform, 1, 0.5f, -46, 0, 44, 44, 1, 0.5f);
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, 180f);
        }

        /// <summary>Taille du texte : trois « A » de plus en plus grands, celui choisi en bleu.</summary>
        void TextSizeRow(RectTransform card, int index, Settings s)
        {
            var row = Row(card, index, Icon.Star, Palette.Secondary, Loc.Get("text_size"), null);
            var label = row.Find("Libelle") as RectTransform;
            label.offsetMax = new Vector2(-330, 0);
            float[] scales = { 1f, 1.25f, 1.5f };
            int[] sizes = { 34, 42, 50 };
            for (int i = 0; i < 3; i++)
            {
                float sc = scales[i];
                bool on = Mathf.Abs(s.textScale - sc) < 0.05f;
                var b = Ui.Image(row, Art.Pill, on ? Palette.Secondary : new Color(0.17f, 0.18f, 0.26f, 0.1f), "Taille", true);
                Ui.Place(b.rectTransform, 1, 0.5f, -(2 - i) * 104, 0, 96, 84, 1, 0.5f);
                b.raycastTarget = true;
                var t = Ui.Label(b.transform, "A", 44, on ? Color.white : Palette.Ink);
                t.fontSize = Mathf.RoundToInt(sizes[i] * Ui.TextScale);
                Ui.Stretch(t.rectTransform, 0, 0, 0, 4);
                if (on) Ui.Outlined(t, 3f);
                var btn = b.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    Ui.Click?.Invoke();
                    if (Mathf.Abs(s.textScale - sc) < 0.05f) return;
                    s.textScale = sc;
                    App.ApplySettings(true);
                });
                b.gameObject.AddComponent<PressScale>();
            }
        }
    }

    // ===================================================================== Fenêtres

    /// <summary>Demande un texte (pseudo, code de duel).</summary>
    public sealed class PromptScreen : UiScreen
    {
        Text _title, _note;
        Image _card;
        InputField _input;
        Action<string> _ok;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            _card = card;
            Ui.Place(card.rectTransform, 0.5f, 0.66f, 0, 0, 940, 720);
            card.gameObject.AddComponent<PopIn>();
            _title = Ui.Label(card.transform, "", 56, Palette.Ink);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -110, 860, 140);
            // Règles (pseudo) : sous le titre ; la fenêtre s'agrandit seulement quand il y en a.
            _note = Ui.Label(card.transform, "", 44, Palette.Muted);
            Ui.Place(_note.rectTransform, 0.5f, 1, 0, -255, 860, 170);

            var field = Ui.Image(card.transform, Art.Pill, Color.white, "Champ", true);
            field.raycastTarget = true;
            Ui.Place(field.rectTransform, 0.5f, 0.5f, 0, 30, 800, 150);
            var text = Ui.Label(field.transform, "", 60, Palette.Ink, TextAnchor.MiddleCenter);
            Ui.Stretch(text.rectTransform, 50, 10, 50, 10);
            var placeholder = Ui.Label(field.transform, "...", 60, Palette.Muted, TextAnchor.MiddleCenter);
            Ui.Stretch(placeholder.rectTransform, 50, 10, 50, 10);
            _input = field.gameObject.AddComponent<InputField>();
            _input.textComponent = text;
            _input.placeholder = placeholder;
            _input.characterLimit = 16;
            _input.lineType = InputField.LineType.SingleLine;
            // iPhone : pas de barre de saisie noire au-dessus du clavier ; on écrit directement dans le champ du jeu.
            _input.shouldHideMobileInput = true;
            // Curseur bien visible et sélection colorée (sinon on ne voit pas où l'on écrit).
            _input.customCaretColor = true;
            _input.caretColor = Palette.Ink;
            _input.caretWidth = 4;
            _input.caretBlinkRate = 1.2f;
            _input.selectionColor = new Color(0.29f, 0.61f, 1f, 0.35f);

            var ok = Ui.Button(card.transform, Loc.Get("pseudo_ok"), Palette.Play, () => { var v = _input.text; Hide(); _ok?.Invoke(v); }, Icon.Check, 54);
            Ui.Place((RectTransform)ok.transform, 0.5f, 0, 205, 120, 390, 150);
            var skip = Ui.Button(card.transform, Loc.Get("pseudo_skip"), Palette.Muted, Hide, Icon.Close, 50);
            Ui.Place((RectTransform)skip.transform, 0.5f, 0, -205, 120, 390, 150);
        }

        public void Ask(string title, string initial, InputField.ContentType type, Action<string> ok, int maxLength = 16, string note = null)
        {
            Show();
            _title.text = title;
            bool hasNote = !string.IsNullOrEmpty(note);
            _note.gameObject.SetActive(hasNote);
            _note.text = note ?? "";
            _card.rectTransform.sizeDelta = new Vector2(940, hasNote ? 940 : 720);
            _input.characterLimit = maxLength;
            _input.contentType = type;
            _input.text = initial ?? "";
            _ok = ok;
            _input.ActivateInputField();
        }
    }

    /// <summary>Signaler un pseudo ou bloquer un joueur du classement (règle 1.2 de l'App Store).</summary>
    public sealed class PlayerMenuScreen : UiScreen
    {
        Text _title;
        string _id, _name;
        GameObject _unfriend;

        protected override void Build()
        {
            var card = Ui.Card(Safe, "Fenetre");
            Ui.Place(card.rectTransform, 0.5f, 0.55f, 0, 0, 940, 1130);
            card.gameObject.AddComponent<PopIn>();
            _title = Ui.Title(card.transform, "", 64, Palette.Ink);
            Ui.Plain(_title);
            Ui.Place(_title.rectTransform, 0.5f, 1, 0, -100, 860, 120);
            var note = Ui.Label(card.transform, Loc.Get("mod_note"), 44, Palette.Muted);
            Ui.Place(note.rectTransform, 0.5f, 1, 0, -280, 860, 220);
            var report = Ui.Button(card.transform, Loc.Get("mod_report"), Palette.Primary, () => { Hide(); App.ReportPlayer(_id, _name); }, null, 52, "Signaler");
            Ui.Place((RectTransform)report.transform, 0.5f, 0, 0, 470, 780, 140);
            var block = Ui.Button(card.transform, Loc.Get("mod_block"), Palette.Dark, () => { Hide(); App.BlockPlayer(_id); }, null, 52, "Bloquer");
            Ui.Place((RectTransform)block.transform, 0.5f, 0, 0, 300, 780, 140);
            var cancel = Ui.Button(card.transform, Loc.Get("cancel"), Palette.Muted, Hide, Icon.Close, 50, "Annuler");
            Ui.Place((RectTransform)cancel.transform, 0.5f, 0, 0, 130, 780, 140);
            var unfriend = Ui.Button(card.transform, Loc.Get("friend_remove"), Palette.Secondary, () => { Hide(); App.RemoveFriend(_id); }, null, 52, "RetirerAmi");
            Ui.Place((RectTransform)unfriend.transform, 0.5f, 0, 0, 640, 780, 140);
            _unfriend = unfriend.gameObject;
        }

        public void Open(string playerId, string name)
        {
            Show();
            _id = playerId;
            _name = string.IsNullOrEmpty(name) ? "-" : name;
            _title.text = _name;
            // « Retirer des amis » seulement pour un ami.
            _unfriend.SetActive(App.Profile.friends.Contains(playerId));
        }
    }

    /// <summary>Écran de la publicité simulée (en attendant la vraie régie).</summary>
    public sealed class AdOverlay : UiScreen
    {
        Text _text;

        protected override void Build()
        {
            var icon = Ui.Icon(Safe, Art.IconSprite(Icon.Film));
            Ui.Place(icon.rectTransform, 0.5f, 0.5f, 0, 260, 220, 220);
            icon.gameObject.AddComponent<Pulse>().Amount = 0.08f;
            _text = Ui.Title(Safe, "", 60);
            Ui.Place(_text.rectTransform, 0.5f, 0.5f, 0, -60, 1000, 400);
        }

        public void Set(bool visible, float remaining, bool rewarded = true)
        {
            if (!visible) { Hide(); return; }
            if (!Visible) Show();
            _text.text = Loc.Get("ad_playing") + "\n" + Mathf.CeilToInt(remaining) + "\n\n" + Loc.Get("ad_simulated");
        }
    }

    /// <summary>Message court en bas de l'écran.</summary>
    public sealed class Toast : UiScreen
    {
        // Deux présentations, toujours en haut de l'écran (comme une notification) :
        // un message court (étiquette crème compacte, petite pastille d'icône) et une bannière de récompense
        // (carte crème, pastille colorée avec icône, titre, détail, pièces gagnées).
        RectTransform _pill, _card;
        Text _text, _title, _detail, _amount;
        Image _disc, _icon, _coin, _pillDisc, _pillIcon;
        float _timer;
        const float PillHeight = 116f, PillMaxWidth = 900f;
        const float BannerWidth = 960f, BannerPad = 36f, TextLeft = 176f;

        struct Item { public bool Reward; public string Text, Detail; public Icon Icon; public Color Color; public int Coins; public float Seconds; public float? Y; }
        readonly Queue<Item> _queue = new Queue<Item>();

        protected override void Build()
        {
            var pill = Ui.Image(Safe, Art.Pill, Palette.Paper, "Message", true);
            _pill = pill.rectTransform;
            pill.gameObject.AddComponent<PopIn>();
            _pillDisc = Ui.Image(pill.transform, Art.Circle, Palette.Secondary, "Pastille");
            Ui.Place(_pillDisc.rectTransform, 0, 0.5f, 14, 0, 88, 88, 0, 0.5f);
            _pillIcon = Ui.Icon(_pillDisc.transform, null, "Icone");
            Ui.Place(_pillIcon.rectTransform, 0.5f, 0.5f, 0, 0, 60, 60);
            _text = Ui.Label(pill.transform, "", 46, Palette.Ink, TextAnchor.MiddleLeft);
            Ui.Stretch(_text.rectTransform, 120, 8, 36, 8);
            _text.resizeTextForBestFit = true; _text.resizeTextMinSize = Ui.MinFont; _text.resizeTextMaxSize = _text.fontSize;

            var card = Ui.Card(Safe, "Recompense");
            _card = card.rectTransform;
            card.gameObject.AddComponent<PopIn>();
            // Bannière : pastille à gauche, bloc de texte au milieu (titre + détail sur 1 ou 2 lignes),
            // pièces à droite. La hauteur suit le texte : rien ne déborde ni ne touche le bord.
            _disc = Ui.Image(card.transform, Art.Circle, Palette.Success, "Pastille");
            Ui.Place(_disc.rectTransform, 0, 0.5f, BannerPad, 0, 116, 116, 0, 0.5f);
            _icon = Ui.Icon(_disc.transform, null, "Icone");
            Ui.Place(_icon.rectTransform, 0.5f, 0.5f, 0, 0, 78, 78);
            _title = Ui.Title(card.transform, "", 48, Palette.Accent);
            _title.alignment = TextAnchor.MiddleLeft;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _title.resizeTextForBestFit = true; _title.resizeTextMinSize = 36; _title.resizeTextMaxSize = 48;
            _detail = Ui.Label(card.transform, "", 40, Palette.Ink, TextAnchor.UpperLeft);
            _detail.lineSpacing = 0.95f;
            _coin = Ui.Icon(card.transform, Art.IconSprite(Icon.Coin), "Piece");
            _coin.color = Palette.Accent;
            Ui.Place(_coin.rectTransform, 1, 0.5f, -178, 0, 64, 64, 0.5f, 0.5f);
            _amount = Ui.Title(card.transform, "", 52, Palette.Accent);
            Ui.Place(_amount.rectTransform, 1, 0.5f, -BannerPad, 0, 124, 70, 1, 0.5f);
            _amount.alignment = TextAnchor.MiddleRight;
            _amount.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        /// <summary>Message court ; s'il y en a déjà un, il attend son tour.</summary>
        public void Say(string message, float seconds = 2.5f) => Say(message, Icon.Bell, Palette.Secondary, seconds);

        public void Say(string message, Icon icon, Color color, float seconds = 2.5f) =>
            Enqueue(new Item { Text = message, Icon = icon, Color = color, Seconds = seconds });

        /// <summary>Bannière de récompense (mission réussie, niveau gagné…).</summary>
        public void Reward(Icon icon, Color color, string title, string detail, int coins, float seconds = 2.8f) =>
            Enqueue(new Item { Reward = true, Icon = icon, Color = color, Text = title, Detail = detail, Coins = coins, Seconds = seconds });

        /// <summary>Bannière de récompense à une hauteur donnée (bilan de fin de partie), à la place de la file.</summary>
        public void RewardAt(float y, Icon icon, Color color, string title, string detail, int coins, float seconds = 3.2f)
        {
            _queue.Clear();
            Display(new Item { Reward = true, Icon = icon, Color = color, Text = title, Detail = detail, Coins = coins, Seconds = seconds, Y = y });
        }

        /// <summary>Efface le message affiché et ceux qui attendent (on quitte l'écran où ils avaient un sens).</summary>
        public void Clear() { _queue.Clear(); _timer = 0; Hide(); }

        void Enqueue(Item item)
        {
            if (Visible && _timer > 0.2f) { if (_queue.Count < 6) _queue.Enqueue(item); return; }
            Display(item);
        }

        void Display(Item it)
        {
            Show();
            _pill.gameObject.SetActive(!it.Reward);
            _card.gameObject.SetActive(it.Reward);
            var rt = it.Reward ? _card : _pill;
            // En haut, comme une notification : ne cache ni les boutons ni la zone de jeu.
            // Pendant la partie, juste sous le score et le niveau à bulle.
            float y = it.Y ?? (App.HudVisible ? -540f : -36f);
            if (it.Reward)
            {
                _disc.color = it.Color;
                _icon.sprite = Art.IconSprite(it.Icon);
                _title.text = it.Text;
                _detail.text = it.Detail ?? "";
                bool coins = it.Coins > 0;
                _coin.gameObject.SetActive(coins);
                _amount.text = coins ? "+" + Loc.FormatScore(it.Coins) : "";
                // Largeur du texte : de la pastille jusqu'aux pièces (ou jusqu'au bord, avec la même marge).
                float textW = BannerWidth - TextLeft - (coins ? 230f : BannerPad + 8f);
                const float top = 30f, titleH = 60f, gap = 2f, bottom = 30f;
                Ui.Place(_title.rectTransform, 0, 1, TextLeft, -top, textW, titleH, 0, 1);
                bool hasDetail = !string.IsNullOrEmpty(_detail.text);
                _detail.gameObject.SetActive(hasDetail);
                float detailH = 0f;
                if (hasDetail)
                {
                    Ui.Place(_detail.rectTransform, 0, 1, TextLeft, -(top + titleH + gap), textW, 100, 0, 1);
                    detailH = Mathf.Min(_detail.preferredHeight, _detail.fontSize * 2.4f);
                    _detail.rectTransform.sizeDelta = new Vector2(textW, detailH);
                }
                float h = Mathf.Max(176f, top + titleH + (hasDetail ? gap + detailH : 0f) + bottom);
                Ui.Place(rt, 0.5f, 1, 0, y, BannerWidth, h, 0.5f, 1);
                // Sans détail, le titre se centre verticalement.
                if (!hasDetail) _title.rectTransform.anchoredPosition = new Vector2(TextLeft, -(h - titleH) * 0.5f);
            }
            else
            {
                _text.text = it.Text;
                _pillDisc.color = it.Color;
                _pillIcon.sprite = Art.IconSprite(it.Icon);
                // Largeur ajustée au texte : une petite étiquette, pas une barre pleine largeur.
                FitText.Restore(_text);
                float w = Mathf.Clamp(_text.preferredWidth + 160f, 420f, PillMaxWidth);
                Ui.Place(rt, 0.5f, 1, 0, y, w, PillHeight, 0.5f, 1);
            }
            _timer = it.Seconds;
            var pop = rt.GetComponent<PopIn>();
            if (pop != null) { pop.enabled = false; pop.enabled = true; }
        }

        public override void Tick(float dt)
        {
            _timer -= dt;
            if (_timer > 0) return;
            if (_queue.Count > 0) Display(_queue.Dequeue());
            else Hide();
        }
    }
}
