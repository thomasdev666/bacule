using System;
using System.Collections.Generic;
using Bascule.Core;
using Bascule.Duel;
using Bascule.Services;
using Bascule.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bascule
{
    /// <summary>
    /// Point d'entrée : construit tout le jeu par le code au lancement (caméra, monde, interface, services),
    /// dans n'importe quelle scène. Rien à câbler dans l'éditeur.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            new GameObject("Bascule").AddComponent<GameApp>();
        }

        /// <summary>Lien vers la politique de confidentialité (obligatoire sur les stores). À remplacer.</summary>
        public const string PrivacyUrl = "https://orpheex.fr/bascule/confidentialite";
        /// <summary>Page d'aide et de contact (demandée par les stores).</summary>
        public const string SupportUrl = "https://orpheex.fr/bascule";
        /// <summary>Adresse qui reçoit les signalements de pseudos (à lire chaque jour : Apple demande une réponse rapide).</summary>
        public const string ContactEmail = "contact@orpheex.fr";
        /// <summary>Version des conditions (aucun écran au lancement ; la politique de confidentialité est dans les réglages).</summary>
        const int ConsentVersion = 2;
        const float AutosaveSeconds = 10f;

        /// <summary>Identifiants Unity Ads (tableau de bord Unity Ads > Monetization). À remplacer avant la publication.</summary>
        static class AdIds
        {
#if UNITY_IOS
            public const string Game = "800428080", Rewarded = "BP_Rewarded_iOS", Interstitial = "BP_Interstitial_iOS";
            /// <summary>LevelPlay (tableau de bord Unity → Monetization) : clé de l'app iOS et identifiants des blocs.</summary>
            public const string LevelPlayAppKey = "2959b19e5", LevelPlayRewarded = "xvinq3655r2dltpx", LevelPlayInterstitial = "7lj35rsgj2famh1w";
#else
            public const string Game = "0000000", Rewarded = "Rewarded_Android", Interstitial = "Interstitial_Android";
            public const string LevelPlayAppKey = "A_REMPLIR", LevelPlayRewarded = "A_REMPLIR", LevelPlayInterstitial = "A_REMPLIR";
#endif
            /// <summary>
            /// Pubs de test dans les builds de développement seulement : la version envoyée à Apple (build normal)
            /// affiche les vraies pubs. Pour voir des pubs de test sur ton iPhone avec la vraie version, ajoute-le
            /// comme « appareil de test » dans le tableau de bord Unity Ads (ne jamais cliquer sur ses propres pubs).
            /// </summary>
            public static readonly bool TestMode = Debug.isDebugBuild;
        }

        public Tuning Tuning { get; private set; }
        public Profile Profile { get; private set; }
        public ArtCache Art { get; private set; }
        public GameSession Session { get; private set; }
        public LeaderboardSync Leaderboard { get; private set; }
        public string TodayKey => Daily.Key(DateTime.UtcNow);
        /// <summary>Jour local (cadeau, missions, publicités) : change à minuit chez le joueur.</summary>
        public string LocalDay => DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        public double Now => Dates.UnixSeconds(DateTime.UtcNow);
        public bool HasSavedRun => _save.HasRun;
        public Analytics Stats { get; private set; }
        public EconomyGain LastGain => _pendingGain;
        public bool LastGainDoubled => _gainDoubled;
        public IPurchaseService Purchases => _purchases;
        public bool NotificationsSupported => _notifications.IsSupported;

        SaveService _save;
        AudioService _audio;
        MusicPlayer _music;
        HapticsService _haptics;
        IAdService _ads;
        bool _adsStarted;
        IPurchaseService _purchases;
        IShareService _share;
        INotificationService _notifications;
        ScoreCard _card;
        Camera _cam;
        Font _font;
        int _tiltFromFile = 1;

        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _frame;
        Camera _borders;
        HomeScreen _home;
        HudScreen _hud;
        PauseScreen _pause;
        GameOverScreen _gameOver;
        CollectionScreen _collection;
        ChestScreen _chest;
        SurpriseScreen _surprise;
        bool _chestOpened;
        LeaderboardScreen _leaderboard;
        SettingsScreen _settings;
        PromptScreen _prompt;
        PlayerMenuScreen _playerMenu;
        GiftScreen _gift;
        MissionsScreen _missions;
        ShopScreen _shop;
        ConfirmScreen _confirm;
        HeadRevealScreen _headReveal;
        DailyScreen _daily;
        WelcomeScreen _welcome;
        OnboardScreen _onboard;
        WheelScreen _wheelScreen;
        QuestScreen _questScreen;
        EventScreen _eventScreen;
        DuelLobbyScreen _duelLobby;
        DuelHudScreen _duelHud;
        DuelResultScreen _duelResult;
        /// <summary>Duel en temps réel (code, vue en direct, résultat).</summary>
        public DuelController Duel { get; private set; }
        AdOverlay _adOverlay;
        Toast _toast;
        UiScreen _current;

        RunResult _pendingResult;
        RunRewards _pendingRewards;
        EconomyGain _pendingGain;
        bool _gainDoubled, _celebration, _leavingGameOver, _starterPopupPending, _ratePending;
        float _lastRunSeconds;
        // Parties finies depuis le lancement du jeu : pas de pub imposée pendant les premières (AdTuning.sessionGraceGames).
        // Un retour dans le jeu après plus de 30 min en arrière-plan compte comme un nouveau lancement.
        int _gamesThisSession;
        DateTime _backgroundSince = DateTime.MinValue;
        bool _pendingFinalized = true;
        float _autosave;
        Vector2Int _screenSize;

        // ------------------------------------------------------------------ Lancement

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AudioService.UseSafeBuffer();

            // Écrans 120 Hz (iPhone Pro, ProMotion, beaucoup d'Android) : le jeu suit l'écran, jusqu'à 120 images/s.
#if UNITY_2022_2_OR_NEWER
            int hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
#else
            int hz = Screen.currentResolution.refreshRate;
#endif
            _displayHz = Mathf.Clamp(hz, 60, 120);
            ApplyFrameRate(false);
            QualitySettings.vSyncCount = 0;
            Time.fixedDeltaTime = 1f / 60f;
#if !(BASCULE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER)
            Input.multiTouchEnabled = false; // une seule main, un seul doigt
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _save = new SaveService();
            Profile = _save.LoadProfile();
            if (_save.SeasonReset) _save.ClearRun(); // la partie en cours datait de l'ancienne saison
            Stats = new Analytics();
            Tuning = TuningLoader.Load();
            _tiltFromFile = Tuning.plank.tiltDirection;
            Loc.Language = Loc.Resolve(Profile.settings.language, Application.systemLanguage.ToString());

            _font = Resources.Load<Font>("Fonts/Fredoka-Bold");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Art = new ArtCache();
            Art.Build();
            Ui.Font = _font;
            Ui.Art = Art;
            BootCurtain.Create(_font); // même ciel et même logo que l'écran de chargement : pas de flash au démarrage
            Ui.Click = () => { _audio.Play(Sfx.Click, 1, 0.6f); _haptics.Light(); };

            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.cullingMask &= ~(1 << 5); // l'interface (couche UI) n'est pas filmée par la caméra du jeu
            DontDestroyOnLoad(_cam.gameObject);
            // Un seul écouteur, sur la caméra du jeu, avec le limiteur qui évite la saturation du mélange.
#if UNITY_2023_1_OR_NEWER
            var listener = FindFirstObjectByType<AudioListener>();
#else
            var listener = FindObjectOfType<AudioListener>();
#endif
            if (listener == null) listener = _cam.gameObject.AddComponent<AudioListener>();
#if !UNITY_WEBGL || UNITY_EDITOR
            // Pas sur le web : Unity n'y prend pas en charge OnAudioFilterRead (le navigateur mixe lui-même le son).
            if (listener.GetComponent<SoftLimiter>() == null) listener.gameObject.AddComponent<SoftLimiter>();
#endif

            _audio = new AudioService(gameObject);
            _music = new MusicPlayer(gameObject);
            _audio.Duck = a => _music.Duck(a);
            _haptics = new HapticsService();

            var sessionGo = new GameObject("Partie");
            sessionGo.transform.SetParent(transform, false);
            Session = sessionGo.AddComponent<GameSession>();
            Session.Init(Tuning, Art, _font, _cam, _audio, _haptics);
            Session.RunEnded += OnRunEnded;
            Session.Dropped += () => _hud.OnDropped();
            Session.Merged += v => _hud.OnMerged(v);
            Session.Ballooned += pts => _hud.OnBalloon(pts);
            Session.ScreenFlash += (a, c) => _hud.Flash(a, c);
            Session.WeightLost += v => _hud.OnWeightLost(v);
            Session.Combo += c => _hud.OnCombo(c);
            Session.LoseStarted += () => Duel?.OnLocalLoseStarted();
            InitDuel();
            Missions.DuelsAvailable = DuelAvailable;

            // La régie démarre une fois l'âge et le choix des pubs connus (premier lancement : après les questions).
            _ads = new NoAdService();
            if (!PrivacyRules.NeedsAsking(Profile, CurrentYear)) StartAds();
#if BASCULE_IAP && !UNITY_EDITOR
            _purchases = new UnityIapService();
#else
            _purchases = new StubPurchaseService();
#endif
            _purchases.Purchased += OnPurchased;
#if BASCULE_NOTIFICATIONS && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            _notifications = new MobileNotificationService(this);
#else
            _notifications = new NoNotificationService();
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            _share = new WebShareService();
#else
            _share = new NativeShareService(msg => _toast.Say(msg));
#endif
            _card = new ScoreCard(Art);

#if BASCULE_UGS
            ILeaderboardService lb = new UgsLeaderboardService();
#else
            ILeaderboardService lb = new OfflineLeaderboardService();
#endif
            Leaderboard = new LeaderboardSync(lb, Profile, SaveProfile);
            Leaderboard.RankKnown += OnRankKnown;
            InitLeaderboard();

            BuildCanvas();
            ApplySettings(false);
            _screenSize = new Vector2Int(Screen.width, Screen.height);
            Session.Layout();
            // Au lancement, la mascotte arrive et ramasse sa planche (sauf si une partie attend d'être reprise).
            if (!HasSavedRun) Session.PlayIntro();

            NewDayCheck();
            StartCoroutine(WarmUp());
            Stats.Event("app_open", ("day", Profile.daysPlayed), ("games", Profile.gamesPlayed), ("level", Profile.level), ("coins", Profile.coins));
            // Pas d'écran d'explication au lancement : on arrive directement sur le jeu.
            if (Profile.consentVersion < ConsentVersion) { Profile.consentVersion = ConsentVersion; SaveProfile(); }
            ShowHome();
        }

        /// <summary>Année en cours (âge du joueur, Core/Privacy.cs).</summary>
        public static int CurrentYear => DateTime.UtcNow.Year;

        /// <summary>Ce que la régie a le droit de faire pour ce joueur.</summary>
        public AdPrivacy AdPrivacyNow => new AdPrivacy
        {
            Personalized = PrivacyRules.Personalized(Profile, CurrentYear),
            Child = PrivacyRules.ChildDirected(Profile, CurrentYear),
        };

        /// <summary>
        /// Démarre la régie (une seule fois), avec le consentement du joueur. Avant les questions du premier lancement,
        /// aucune pub n'est chargée : rien n'est envoyé aux régies sans connaître l'âge et le choix du joueur.
        /// </summary>
        void StartAds()
        {
            if (_adsStarted) return;
            _adsStarted = true;
#if BASCULE_LEVELPLAY && !UNITY_EDITOR
            _ads = new LevelPlayAdService(AdIds.LevelPlayAppKey, AdIds.LevelPlayRewarded, AdIds.LevelPlayInterstitial, AdPrivacyNow);
#elif BASCULE_UNITYADS && !UNITY_EDITOR
            _ads = new UnityAdsService(AdIds.Game, AdIds.Rewarded, AdIds.Interstitial, AdIds.TestMode, AdPrivacyNow);
#else
            // Pubs simulées seulement dans l'éditeur et les versions de test ; une version publiée sans régie n'en montre aucune.
            if (Application.isEditor || Debug.isDebugBuild)
                _ads = new SimulatedAdService(this, (visible, remaining, rewarded) => _adOverlay.Set(visible, remaining, rewarded));
            else _ads = new NoAdService();
#endif
        }

        /// <summary>Le joueur a changé son choix (réglages) : la régie le sait pour les pubs suivantes.</summary>
        void ApplyAdPrivacy()
        {
            if (!_adsStarted) { StartAds(); return; }
#if BASCULE_LEVELPLAY && !UNITY_EDITOR
            LevelPlayAdService.ApplyPrivacy(AdPrivacyNow);
#elif BASCULE_UNITYADS && !UNITY_EDITOR
            UnityAdsService.ApplyPrivacy(AdPrivacyNow);
#endif
        }

        /// <summary>
        /// Fluidité : toutes les images sont dessinées en arrière-plan dès le lancement (sur un autre cœur), puis chargées
        /// en mémoire une par image affichée, de préférence pendant les menus. Pendant une partie, plus rien n'est
        /// calculé au dernier moment (nouveau poids, expression, ambiance qui change à 2 500 points…).
        /// </summary>
        System.Collections.IEnumerator WarmUp()
        {
            Art.PrepareInBackground(Art.GameplayJobs());
            Art.PrepareInBackground(Backdrop.AllSceneJobs());
            Art.PrepareInBackground(Art.HeadJobs(false));
            yield return null;

            // 1. Ce qui sert en partie : poids, expressions, rougeur.
            foreach (var v in WeightCatalog.Values) { yield return WaitFile("weight" + v); Art.Weight(v); yield return null; }
            foreach (Expression e in Enum.GetValues(typeof(Expression))) { yield return WaitFile("face_" + e); Art.Face(e); yield return null; }
            yield return WaitFile("flush");
            _ = Art.Flush;

            // Portraits affichés pendant ou juste après une partie (prochaine tête), et ceux de la boutique.
            var soon = new List<HeadDef>();
            var nextScore = HeadCatalog.NextScoreHead(Profile.unlockedHeads);
            foreach (var h in HeadCatalog.All)
                if ((h.Unlock == UnlockKind.Score && nextScore != null && h.UnlockValue >= nextScore.UnlockValue && soon.Count < 3) || h.Unlock == UnlockKind.Premium)
                    soon.Add(h);
            var shopNext = HeadCatalog.NextShopHead(Profile.unlockedHeads);
            if (shopNext != null) soon.Add(shopNext);
            var urgent = new List<KeyValuePair<string, Func<Raster>>>();
            foreach (var h in soon) { urgent.Add(ArtCache.PortraitJob(h, false)); urgent.Add(ArtCache.PortraitJob(h, true)); }
            Art.PrepareInBackground(urgent, true);
            foreach (var h in soon)
            {
                yield return WaitFile("portrait" + h.Id); Art.Portrait(h); yield return null;
                yield return WaitFile("locked" + h.Id); Art.Locked(h); yield return null;
            }

            // 2. Les ambiances du décor (grosses images) : chargées hors partie, sauf la suivante si on joue déjà.
            // Seulement le deuxième monde : les suivants se chargent un par un pendant la partie (GameSession), pour ne
            // pas garder 14 décors en mémoire.
            yield return WaitFile("scene_t1ground");
            yield return Session.Backdrop.Preload(1);

            // 3. Les portraits (collection, boutique).
            foreach (var h in HeadCatalog.All)
            {
                while (Session.IsRunning && !Session.Paused) yield return null;
                yield return WaitFile("portrait" + h.Id);
                Art.Portrait(h);
                yield return null;
            }
        }

        /// <summary>Attend (au plus quelques secondes) qu'une image préparée en arrière-plan soit prête.</summary>
        System.Collections.IEnumerator WaitFile(string key)
        {
            float limit = Time.realtimeSinceStartup + 20f;
            while (!ArtCache.IsReady(key) && Art.Preparing && Time.realtimeSinceStartup < limit) yield return null;
        }

        /// <summary>Nouveau jour : missions, jours joués. Appelé au lancement et au retour dans l'appli.</summary>
        void NewDayCheck()
        {
            Progress.TouchDay(Profile, LocalDay);
            if (Missions.Refresh(Profile, LocalDay)) SaveProfile();
        }

        async void InitLeaderboard()
        {
            if (!await Leaderboard.Service.InitializeAsync()) return;
            if (!string.IsNullOrEmpty(Profile.pseudo)) await Leaderboard.Service.SetPseudoAsync(Profile.pseudo);
            Leaderboard.Flush(TodayKey);
            // Amis réciproques : les derniers amis ajoutés sont republiés à chaque lancement (un ajout fait hors ligne,
            // ou avec une ancienne version, finit ainsi par arriver chez l'ami).
            if (Profile.friends.Count > 0) _ = Leaderboard.Service.PublishFriendLinksAsync(Profile.friends);
            _socialTimer = 2f;
            _inviteTimer = 1f;
            // Code ami court (Cloud Save) : publié une fois, puis gardé dans le profil.
            string code = await Leaderboard.Service.PublishFriendCodeAsync();
            if (!string.IsNullOrEmpty(code) && code != Profile.friendCode)
            {
                Profile.friendCode = code;
                SaveProfile();
                if (_leaderboard != null && _leaderboard.Visible) _leaderboard.Refresh();
            }
        }

        /// <summary>Code à donner à un ami : le court s'il est publié (« 4K7-Q2M »), sinon le long.</summary>
        public string MyFriendCode
        {
            get
            {
                if (!string.IsNullOrEmpty(Profile.friendCode)) return FriendCode.FormatShort(Profile.friendCode);
                var id = Leaderboard.Service.PlayerId;
                return string.IsNullOrEmpty(id) ? "" : FriendCode.Format(id);
            }
        }

        void BuildCanvas()
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if BASCULE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
                DontDestroyOnLoad(es);
            }

            if (_canvas == null)
            {
                var go = new GameObject("Interface", typeof(RectTransform));
                go.layer = 5;
                DontDestroyOnLoad(go);
                _canvas = go.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 10;
                _scaler = go.AddComponent<CanvasScaler>();
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _scaler.referenceResolution = new Vector2(Ui.RefWidth, Ui.RefHeight);
                _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                go.AddComponent<GraphicRaycaster>();
            }
            FitCanvas();

            Ui.Clear(_canvas.transform);
            Ui.TextScale = Profile.settings.textScale;
            // Cadre portrait : sur un écran paysage (éditeur, PC), toute l'interface tient dans une colonne 9:16.
            _frame = Ui.Rect("Cadre", _canvas.transform);
            FitFrame();
            var t = _frame;
            _home = Make<HomeScreen>(t, null);
            _hud = Make<HudScreen>(t, null);
            _duelHud = Make<DuelHudScreen>(t, null);
            _gameOver = Make<GameOverScreen>(t, Palette.Shade);
            _collection = Make<CollectionScreen>(t, Palette.Shade);
            _leaderboard = Make<LeaderboardScreen>(t, Palette.Shade);
            _settings = Make<SettingsScreen>(t, Palette.Shade);
            _pause = Make<PauseScreen>(t, Palette.Shade);
            _prompt = Make<PromptScreen>(t, Palette.Shade);
            _playerMenu = Make<PlayerMenuScreen>(t, Palette.Shade);
            _missions = Make<MissionsScreen>(t, Palette.Shade);
            _shop = Make<ShopScreen>(t, Palette.Shade);
            _gift = Make<GiftScreen>(t, Palette.Shade);
            _duelLobby = Make<DuelLobbyScreen>(t, Palette.Shade);
            _duelResult = Make<DuelResultScreen>(t, new Color(0.08f, 0.05f, 0.18f, 0.93f));
            _daily = Make<DailyScreen>(t, Palette.Shade);
            _friendPicker = Make<FriendPickerScreen>(t, Palette.Shade);
            _duelInvite = Make<DuelInviteScreen>(t, new Color(0.08f, 0.05f, 0.18f, 0.9f));
            _welcome = Make<WelcomeScreen>(t, Palette.Shade);
            _onboard = Make<OnboardScreen>(t, Palette.Shade);
            _wheelScreen = Make<WheelScreen>(t, new Color(0.08f, 0.05f, 0.18f, 0.95f));
            _questScreen = Make<QuestScreen>(t, Palette.Shade);
            _eventScreen = Make<EventScreen>(t, Palette.Shade);
            _confirm = Make<ConfirmScreen>(t, Palette.Shade);
            _surprise = Make<SurpriseScreen>(t, new Color(0.08f, 0.05f, 0.18f, 0.985f));
            _chest = Make<ChestScreen>(t, new Color(0.08f, 0.05f, 0.18f, 0.97f)); // presque opaque : le bilan derrière ne brouille pas le coffre
            _headReveal = Make<HeadRevealScreen>(t, new Color(0.08f, 0.05f, 0.18f, 1f)); // nouvelle tête en grand (code promo)
            _adOverlay = Make<AdOverlay>(t, new Color(0, 0, 0, 0.92f));
            _toast = Make<Toast>(t, null);
        }

        T Make<T>(Transform canvas, Color? bg) where T : UiScreen, new()
        {
            var s = new T();
            s.Create(this, canvas, bg);
            return s;
        }

        /// <summary>
        /// Le jeu est en portrait. Téléphones (même très allongés, iPhone 14 Pro : 0,46) et tablettes (iPad : 0,69 à 0,75)
        /// remplissent tout l'écran ; seul un écran plus large (ordinateur, tablette tournée) affiche une colonne portrait.
        /// </summary>
        public const float MaxAspect = 0.8f;

        static bool Landscape => Screen.height > 0 && Screen.width / (float)Screen.height > MaxAspect;

        void FitCanvas()
        {
            // Téléphones fins : on cale la largeur. Tablettes et écrans paysage : la hauteur.
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 0.5f;
            _scaler.matchWidthOrHeight = aspect > 9f / 16f ? 1f : 0f;
            FitFrame();

            // Caméra du jeu limitée à la colonne portrait, bandes noires autour.
            if (Landscape)
            {
                float w = Screen.height * (9f / 16f) / Screen.width;
                _cam.rect = new Rect(0.5f - w * 0.5f, 0f, w, 1f);
                if (_borders == null)
                {
                    var go = new GameObject("Bordures");
                    DontDestroyOnLoad(go);
                    _borders = go.AddComponent<Camera>();
                    _borders.depth = _cam.depth - 10;
                    _borders.clearFlags = CameraClearFlags.SolidColor;
                    _borders.backgroundColor = new Color(0.06f, 0.05f, 0.1f);
                    _borders.cullingMask = 0;
                }
                _borders.enabled = true;
            }
            else
            {
                _cam.rect = new Rect(0, 0, 1, 1);
                if (_borders != null) _borders.enabled = false;
            }
        }

        void FitFrame()
        {
            if (_frame == null) return;
            if (Landscape)
            {
                _frame.anchorMin = new Vector2(0.5f, 0f);
                _frame.anchorMax = new Vector2(0.5f, 1f);
                _frame.pivot = new Vector2(0.5f, 0.5f);
                _frame.sizeDelta = new Vector2(Ui.RefHeight * 9f / 16f, 0f);
                _frame.anchoredPosition = Vector2.zero;
            }
            else Ui.Stretch(_frame);
        }

        // ------------------------------------------------------------------ Navigation

        void Show(UiScreen screen)
        {
            foreach (var s in new UiScreen[] { _home, _hud, _gameOver, _collection, _leaderboard, _settings, _pause, _missions, _shop, _gift, _confirm, _daily, _welcome, _onboard, _friendPicker, _duelInvite, _wheelScreen, _questScreen, _eventScreen, _duelLobby, _duelResult })
                if (s != screen) s.Hide();
            // Le HUD du duel accompagne le HUD de jeu, juste au-dessus de lui.
            if (screen == _hud && Duel != null && Duel.InMatch)
            {
                _duelHud.Show();
                _duelHud.Root.SetSiblingIndex(_hud.Root.GetSiblingIndex() + 1);
            }
            else _duelHud.Hide();
            _current = screen;
            screen?.Show();
        }

        public void GoHome()
        {
            if (_current == _gameOver && !_leavingGameOver) { if (ChestWaiting) { OpenRunChest(GoHome); return; } LeaveGameOver(GoHome); return; }
            FinalizeRun();
            if (Session.IsRunning) SaveRunSnapshot();
            Session.SetPaused(false);
            Session.GoIdle();
            ShowHome();
        }

        void ShowHome()
        {
            Show(_home);
            _socialTimer = Mathf.Min(_socialTimer, 1.5f); // amis et invitations vérifiés peu après le retour à l'accueil
            _inviteTimer = Mathf.Min(_inviteTimer, 0.5f);
            if (!string.IsNullOrEmpty(_pendingDuelCode)) { ConsumePendingDuelLink(); return; }
            HomePopups();
        }

        /// <summary>
        /// Au plus une fenêtre à chaque retour à l'accueil, par ordre d'importance :
        /// cadeau du jour, offre de départ (une fois), suggestion « sans pubs » (une fois), rappels (une fois).
        /// </summary>
        void HomePopups()
        {
            // Lancement : écran de bienvenue, une seule fois, avant tout le reste.
            if (Profile.welcomePending)
            {
                Profile.welcomePending = false;
                SaveProfile();
                _welcome.Show();
                return;
            }
            // Puis pseudo, âge et pubs (seulement ce qui manque ; un joueur déjà installé ne voit que l'âge et les pubs).
            if (NeedsOnboarding) { _onboard.Open(AfterOnboarding, NeedsPseudo, NeedsPrivacyNow); return; }
            // Coffres gagnés en route (quête du débutant, roue) : ouverts maintenant, devant le joueur.
            if (_bonusChests.Count > 0)
            {
                var (loot, title) = _bonusChests.Dequeue();
                _chest.PresentLoot(loot.Coins, loot.Head, title, () => { _home.Refresh(); HomePopups(); });
                return;
            }
            // Un tout nouveau joueur joue d'abord : le cadeau arrive après sa première partie.
            if (Profile.gamesPlayed >= 1 && DailyGift.Available(Profile.gift, LocalDay)) { _gift.Show(); return; }
            if (_starterPopupPending && Offers.StarterActive(Profile, Now))
            {
                _starterPopupPending = false;
                Profile.offers.starterPopupShown = true;
                SaveProfile();
                Stats.Event("offer_shown", ("offer", "starter"));
                ShowShop("offre_depart");
                return;
            }
            if (AdPolicy.ShouldHintRemoveAds(Tuning.ads, Profile.ads, Profile))
            {
                Profile.ads.removeAdsHintShown = true;
                SaveProfile();
                Stats.Event("offer_shown", ("offer", "removeads"));
                _confirm.Ask(Loc.Get("removeads_hint_title"), Loc.Get("removeads_hint_body"), Art.IconSprite(Icon.Film),
                    Loc.Get("p_removeads") + "  " + _purchases.Price(Shop.RemoveAds), () => BuyProduct(Shop.RemoveAds), Loc.Get("later"), null);
                return;
            }
            // Note sur le store, juste après un record (le joueur est content) : c'est ce qui fait le plus monter le jeu
            // dans la recherche. iPhone : la fenêtre de notation d'Apple directement (pas de question avant : Apple
            // décide de l'afficher, au plus 3 fois par an), au plus 3 fois, à 4 jours d'écart au moins.
            // Android : une seule fois, avec une question avant (la page du jeu s'ouvre dans Google Play).
            if (_ratePending && RatingDue())
            {
                _ratePending = false;
                Profile.ratingAsked = true;
                Profile.ratingAsks++;
                Profile.ratingLastDay = LocalDay;
                SaveProfile();
                Stats.Event("rate_prompt", ("n", Profile.ratingAsks));
#if UNITY_IOS && !UNITY_EDITOR
                OpenStoreReview();
#else
                _confirm.Ask(Loc.Get("rate_title"), Loc.Get("rate_body"), Art.IconSprite(Icon.Star), Loc.Get("rate_yes"), OpenStoreReview, Loc.Get("rate_no"), null);
#endif
                return;
            }
            if (!Profile.notificationsAsked && _notifications.IsSupported && Profile.chestsOpened >= 1)
            {
                Profile.notificationsAsked = true;
                SaveProfile();
                _confirm.Ask(Loc.Get("notif_ask_title"), Loc.Get("notif_ask_body"), Art.IconSprite(Icon.Bell),
                    Loc.Get("notif_yes"), () => SetNotifications(true), Loc.Get("later"), null);
            }
        }

        public void ShowCollection() { _collection.OpenOnNews(); Show(_collection); }
        /// <summary>Sauvegarde immédiate du profil (têtes vues, réglages).</summary>
        public void SaveProfileNow() => SaveProfile();

        /// <summary>Depuis le bilan : aller acheter la tête de la boutique qu'on peut s'offrir (la partie est enregistrée).</summary>
        public void OpenShopHeads()
        {
            if (_current == _gameOver && !_leavingGameOver) { if (ChestWaiting) { OpenRunChest(OpenShopHeads); return; } LeaveGameOver(OpenShopHeads); return; }
            FinalizeRun();
            Session.SetPaused(false);
            Session.GoIdle();
            _collection.SetTab("sec_shop");
            Show(_collection);
        }
        public void ShowSettings() => Show(_settings);
        public void ShowMissions() { Missions.Refresh(Profile, LocalDay); Show(_missions); }

        public void ShowShop(string from)
        {
            Stats.Event("shop_open", ("from", from));
            Show(_shop);
        }

        public void ShowLeaderboard()
        {
            Show(_leaderboard);
            if (string.IsNullOrEmpty(Profile.pseudo) && Leaderboard.Service.IsOnline) AskPseudo();
        }

        // ------------------------------------------------------------------ Partie

        public void Play(GameMode mode)
        {
            FinalizeRun();
            _save.ClearRun();
            uint seed = mode == GameMode.Daily ? Daily.Seed(TodayKey) : GameRng.RandomSeed();
            StartRun(mode, seed);
        }

        void StartRun(GameMode mode, uint seed)
        {
            Session.SetPaused(false);
            if (Profile.randomHead && mode != GameMode.Duel) RollRandomHead(); // tête au hasard : une autre tête à chaque partie (pas en duel : l'adversaire a déjà reçu la nôtre)
            // Mode infini seulement : objectif de la partie et partie spéciale (récompenses, jamais le score).
            Session.Goal = mode == GameMode.Infinite ? RunGoal.For(Profile) : 0;
            Session.Special = mode == GameMode.Infinite ? SpecialRun.Next(Profile) : SpecialKind.None;
            Session.StartRun(mode, seed, mode == GameMode.Daily ? TodayKey : "");
            Show(_hud);
            if (Session.Special != SpecialKind.None)
                _toast.Reward(Icon.Star, Palette.Purple, Loc.Get("special_now"), Loc.Get("special_" + Session.Special), 0, 3.2f);
            Stats.Event("run_start", ("mode", mode.ToString()), ("n", Profile.gamesPlayed + 1));
        }

        /// <summary>Défi du jour : on explique la règle et le record avant de lancer, au lieu de démarrer une partie sans prévenir.</summary>
        public void ShowDailyIntro()
        {
            _daily.Show();
            _daily.Root.SetAsLastSibling();
        }

        // ------------------------------------------------------------------ Duel en temps réel

        string _pendingDuelCode;

        /// <summary>Le duel en temps réel est disponible dans cette version (paquet réseau installé).</summary>
        public bool DuelAvailable => Duel != null && Duel.Available;

        void InitDuel()
        {
#if BASCULE_MULTIPLAYER
            IDuelNetwork net = new UgsDuelNetwork();
#else
            IDuelNetwork net = new OfflineDuelNetwork();
#endif
            Duel = new DuelController(net, () => Tuning);
            if (net.Available) Duel.AttachView(OpponentView.Create(transform, Art, _font, Tuning));
            Duel.StartRun = seed =>
            {
                FinalizeRun();
                _save.ClearRun();
                StartRun(GameMode.Duel, seed);
            };
            Duel.FinishRun = () => Session.FinishDuelRun();
            Duel.Capture = snap => Session.CaptureDuel(snap);
            Duel.RunTime = () => Session.RunTime;
            Duel.Score = () => Session.Score;
            Duel.LockInput = on => Session.InputLocked = on;
            Duel.Changed += OnDuelChanged;
            Duel.Notice += key => _toast?.Say(Loc.Get(key));
            Duel.ResultReady += OnDuelResult;

            // Invitation : bascule://duel/CODE ouvre le jeu et rejoint le duel (au lancement ou application déjà ouverte).
            Application.deepLinkActivated += OnDeepLink;
            if (!string.IsNullOrEmpty(Application.absoluteURL)) _pendingDuelCode = DuelRules.CodeFromLink(Application.absoluteURL);
        }

        void OnDeepLink(string url)
        {
            var code = DuelRules.CodeFromLink(url);
            if (code == null) return;
            Stats.Event("duel_link");
            if (Duel.InMatch) return; // déjà en duel : on ne coupe pas la partie en cours
            // Coffre, machine à surprises ou pub à l'écran : l'invitation attend le retour à l'accueil.
            if (_chest.Visible || _surprise.Visible || AdBusy) { _pendingDuelCode = code; return; }
            if (Duel.InDuel) Duel.Leave();
            JoinDuel(code);
        }

        /// <summary>Un lien d'invitation a ouvert le jeu : on rejoint le duel dès que l'accueil est prêt.</summary>
        void ConsumePendingDuelLink()
        {
            if (string.IsNullOrEmpty(_pendingDuelCode)) return;
            var code = _pendingDuelCode;
            _pendingDuelCode = null;
            JoinDuel(code);
        }

        DuelHello MyHello() => new DuelHello
        {
            Name = string.IsNullOrEmpty(Profile.pseudo) ? "" : Profile.pseudo,
            Head = Profile.selectedHead,
            Plank = Profile.selectedPlank,
        };

        public void OpenDuel()
        {
            if (!DuelAvailable) { _toast.Say(Loc.Get("duel_unavailable")); return; }
            FinalizeRun();
            Stats.Event("duel_open", ("wins", Profile.duelWins));
            Show(_duelLobby);
        }

        public void DuelHost()
        {
            Stats.Event("duel_host");
            Duel.Host(MyHello());
        }

        public void DuelJoinPrompt()
        {
            _prompt.Ask(Loc.Get("duel_join_prompt"), "", InputField.ContentType.Alphanumeric, JoinDuel, 12);
        }

        void JoinDuel(string code)
        {
            if (!DuelAvailable) { _toast.Say(Loc.Get("duel_unavailable")); return; }
            if (Duel.InDuel) return;
            code = DuelRules.NormalizeCode(code);
            if (string.IsNullOrEmpty(code)) { _toast.Say(Loc.Get("duel_invalid")); return; }
            FinalizeRun();
            if (Session.IsRunning) { SaveRunSnapshot(); Session.SetPaused(false); Session.GoIdle(); }
            Show(_duelLobby);
            Stats.Event("duel_join");
            Duel.Join(MyHello(), code);
        }

        // ------------------------------------------------------------------ Amis : ajout réciproque et invitations au duel

        FriendPickerScreen _friendPicker;
        DuelInviteScreen _duelInvite;
        float _socialTimer;
        bool _socialBusy, _inviteSent;
        readonly HashSet<string> _seenInvites = new HashSet<string>();
        /// <summary>Une invitation de plus de 5 minutes ne s'affiche plus (le duel a été abandonné).</summary>
        const long InviteMaxAge = 300;

        public void OpenFriendPicker() => _friendPicker.Show();

        public async System.Threading.Tasks.Task<bool> InviteFriendToDuel(string friendId, string name)
        {
            if (string.IsNullOrEmpty(Duel.Code)) return false;
            bool ok = await Leaderboard.Service.SendDuelInviteAsync(friendId, Duel.Code, Profile.pseudo, Profile.selectedHead);
            if (ok)
            {
                _inviteSent = true;
                InvitedName = name;
                InvitedHead = FriendHeads.TryGetValue(friendId, out var h) ? h : 0;
                Stats.Event("duel_invite_friend");
                if (_duelLobby.Visible) _duelLobby.Refresh();
            }
            else _toast.Say(Loc.Get("duel_invite_failed"), 2.5f);
            return ok;
        }

        /// <summary>Ami invité au duel en cours (affiché dans la salle d'attente), vide sinon.</summary>
        public string InvitedName { get; private set; } = "";
        public int InvitedHead { get; private set; }
        /// <summary>Têtes des amis (vues dans le classement des amis), pour leurs portraits.</summary>
        public readonly Dictionary<string, int> FriendHeads = new Dictionary<string, int>();
        (string id, string name) _challengeAfterHost;

        /// <summary>Défier un ami en un geste : le duel est créé, puis l'invitation part dès que le code existe.</summary>
        public void ChallengeFriend(string friendId, string name)
        {
            if (!DuelAvailable) { _toast.Say(Loc.Get("duel_unavailable")); return; }
            if (Duel.InDuel)
            {
                if (Duel.Current == DuelController.Phase.Hosting) _ = InviteFriendToDuel(friendId, name);
                return;
            }
            FinalizeRun();
            Show(_duelLobby);
            _challengeAfterHost = (friendId, name);
            Stats.Event("duel_challenge_friend");
            DuelHost();
        }

        void ClearDuelInvite()
        {
            if (!_inviteSent) return;
            _inviteSent = false;
            _ = Leaderboard.Service.ClearDuelInviteAsync();
        }

        float _inviteTimer;
        bool _inviteBusy;

        /// <summary>
        /// Amis réciproques (toutes les 30 s hors partie) : ajoute les joueurs qui nous ont ajouté en ami. Une seule
        /// requête par case, et seulement hors partie : le jeu ne se met jamais à attendre le réseau.
        /// </summary>
        async void SyncFriends()
        {
            if (_socialBusy || !Leaderboard.Service.IsOnline) return;
            _socialBusy = true;
            try
            {
                var me = Leaderboard.Service.PlayerId;
                int added = 0;
                bool mutual = false;
                foreach (var id in await Leaderboard.Service.FindPlayersWhoAddedMeAsync())
                {
                    if (FriendCode.AddedBy(Profile, id, me)) added++;
                    if (Profile.friends.Contains(id)) mutual = true;
                }
                // Parrainage « Duo » : la tête se gagne dès qu'un ami et toi êtes amis tous les deux.
                if (mutual && DuoHead.Grant(Profile))
                {
                    SaveProfile();
                    Stats.Event("duo_head");
                    _toast.Reward(Icon.Heart, Palette.Secondary, Loc.Format("new_head", DuoHead.Head.Name(Loc.Language)), Loc.Get("duo_detail"), 0);
                    if (_home.Visible) _home.Refresh();
                }
                if (added > 0)
                {
                    SaveProfile();
                    Stats.Event("friend_mutual", ("added", added));
                    _toast.Say(Loc.Get("friend_added_you_generic"), 3f);
                    if (_leaderboard.Visible) _leaderboard.Refresh();
                    if (_duelLobby.Visible) _duelLobby.Refresh();
                }
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Amis réciproques : " + e.Message); }
            finally { _socialBusy = false; }
        }

        /// <summary>
        /// Invitations au duel (toutes les 4 s sur les menus) : la plus récente d'un ami s'affiche en grand
        /// (DuelInviteScreen). Une invitation de plus de 5 minutes, ou déjà vue, ne revient pas.
        /// </summary>
        async void CheckInvites()
        {
            if (_inviteBusy || !Leaderboard.Service.IsOnline || !DuelAvailable || Duel.InDuel || Profile.friends.Count == 0) return;
            _inviteBusy = true;
            try
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                DuelInvite best = null;
                foreach (var inv in await Leaderboard.Service.FindDuelInvitesAsync())
                {
                    if (!Profile.friends.Contains(inv.FromId) || Moderation.IsBlocked(Profile, inv.FromId)) continue;
                    if (Math.Abs(now - inv.At) > InviteMaxAge || _seenInvites.Contains(inv.FromId + inv.Code)) continue;
                    if (best == null || inv.At > best.At) best = inv;
                }
                if (best == null || !CanShowInvite) return;
                _seenInvites.Add(best.FromId + best.Code);
                string name = PseudoFilter.Display(best.FromName, Loc.Get("lb_player"));
                Stats.Event("duel_invite_received");
                _haptics.Medium();
                PlaySfx(Sfx.Chime, 8, 0.9f);
                string code = best.Code;
                _duelInvite.Open(name, best.Head, () => { Stats.Event("duel_invite_accept"); JoinDuel(code); });
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Invitations au duel : " + e.Message); }
            finally { _inviteBusy = false; }
        }

        /// <summary>Une invitation peut s'afficher : pas de partie, de pub, de coffre ni d'autre fenêtre en cours.</summary>
        bool CanShowInvite => !Duel.InDuel && !AdBusy && !Session.IsRunning && !_confirm.Visible && !_prompt.Visible
            && !_chest.Visible && !_headReveal.Visible && !_surprise.Visible && !_welcome.Visible && !_onboard.Visible && !_duelInvite.Visible && !_gameOver.Visible;

        /// <summary>Écrans de menu sur lesquels on vérifie les amis et les invitations.</summary>
        bool SocialScreen => _home.Visible || _wheelScreen.Visible || _leaderboard.Visible || _shop.Visible || _collection.Visible || _missions.Visible
            || _settings.Visible || (_duelLobby.Visible && !Duel.InDuel);

        public void DuelCancel()
        {
            ClearDuelInvite();
            Duel.Leave();
            ShowHome();
        }

        public void DuelShareInvite()
        {
            if (string.IsNullOrEmpty(Duel.Code)) return;
            Stats.Event("duel_invite");
            _share.ShareText(Loc.Format("duel_invite_text", DuelRules.InviteLink(Duel.Code), Duel.Code));
        }

        public void DuelCopyCode()
        {
            if (string.IsNullOrEmpty(Duel.Code)) return;
            GUIUtility.systemCopyBuffer = Duel.Code;
            _toast.Say(Loc.Get("duel_copied"), Icon.Check, Palette.Play);
        }

        public void DuelAskQuit()
        {
            if (!Duel.InMatch) { DuelHome(); return; }
            _confirm.Ask(Loc.Get("duel_quit_title"), Loc.Format("duel_quit_body", Duel.TheirName), Art.IconSprite(Icon.Flag),
                Loc.Get("duel_quit_ok"), () => Duel.Forfeit(), Loc.Get("duel_quit_no"), null);
        }

        /// <summary>Après une victoire : inviter un autre ami à venir jouer (code ami et lien du jeu).</summary>
        public void ShareDuelBrag()
        {
            Stats.Event("duel_brag");
            string code = MyFriendCode;
            _share.ShareText(Loc.Format("duel_brag_text", string.IsNullOrEmpty(code) ? "" : code, StoreLink));
        }

        /// <summary>Lien du jeu sur le store de ce téléphone.</summary>
        string StoreLink =>
#if UNITY_ANDROID
            "https://play.google.com/store/apps/details?id=" + Application.identifier;
#else
            "https://apps.apple.com/app/id" + AppStoreId;
#endif

        public void DuelRematch()
        {
            Stats.Event("duel_rematch");
            Duel.AskRematch();
            if (_duelResult.Visible) _duelResult.Refresh();
        }

        public void DuelHome()
        {
            ClearDuelInvite();
            Duel.Leave();
            Session.InputLocked = false;
            Session.SetPaused(false);
            Session.GoIdle();
            ShowHome();
        }

        void OnDuelChanged()
        {
            switch (Duel.Current)
            {
                case DuelController.Phase.None:
                    _challengeAfterHost = (null, null);
                    InvitedName = "";
                    if (_duelLobby.Visible) _duelLobby.Refresh();
                    break;
                case DuelController.Phase.Connecting:
                case DuelController.Phase.Hosting:
                case DuelController.Phase.Greeting:
                    if (Duel.Current == DuelController.Phase.Hosting && !string.IsNullOrEmpty(_challengeAfterHost.id))
                    {
                        var (fid, fname) = _challengeAfterHost;
                        _challengeAfterHost = (null, null);
                        _ = InviteFriendToDuel(fid, fname);
                    }
                    if (!_duelLobby.Visible) Show(_duelLobby); else _duelLobby.Refresh();
                    break;
                case DuelController.Phase.Countdown:
                    // Départ (ou revanche) : la partie est lancée à la prochaine image par le contrôleur.
                    _confirm.Hide();
                    _prompt.Hide();
                    break;
                case DuelController.Phase.Results:
                    if (_duelResult.Visible) _duelResult.Refresh();
                    break;
            }
        }

        void OnDuelResult()
        {
            var o = Duel.Outcome;
            int coins = DuelRules.Record(Profile, o, Tuning.duel);
            Missions.Refresh(Profile, LocalDay);
            coins += DuelRules.ApplyMissions(Profile, o, Tuning.economy, out var duelMissions);
            foreach (var m in duelMissions) _toast.Reward(MetaUi.MissionIcon(m.kind), MetaUi.MissionColor(m.kind), Loc.Get("mission_title"), Loc.MissionText(m), m.reward, 2.6f);
            var heads = DuelRules.UnlockHeads(Profile);
            Duel.SetRewards(coins, heads);
            SaveProfile();
            Stats.Event("duel_end", ("outcome", o.ToString()), ("seconds", Duel.MySeconds), ("forfeit", Duel.ByForfeit),
                ("wins", Profile.duelWins), ("played", Profile.duelsPlayed));
            if (heads.Count > 0) Stats.Event("head_unlocked", ("id", heads[0].Id));
            if (o == DuelOutcome.Win) _haptics.Medium();
            // Abandon de ma part : ma partie s'arrête là.
            if (Session.IsRunning && Session.Mode == GameMode.Duel) { Session.InputLocked = false; Session.GoIdle(); }
            // Un instant pour voir la fin (bras levés, chute de l'adversaire), puis le résultat.
            StartCoroutine(ShowDuelResult(Duel.ByForfeit ? 0.4f : 1.4f));
        }

        System.Collections.IEnumerator ShowDuelResult(float delay)
        {
            float t = 0f;
            while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }
            if (Duel.Current != DuelController.Phase.Results) yield break; // revanche déjà lancée, ou duel quitté
            Session.InputLocked = false;
            _confirm.Hide();
            Show(_duelResult);
            _audio.Play(Duel.Outcome == DuelOutcome.Lose ? Sfx.Click : Sfx.Lift);
        }

        public void ResumeSavedRun()
        {
            var snapshot = _save.LoadRun();
            if (snapshot == null) { _home.Refresh(); return; }
            Session.SetPaused(false);
            Session.ResumeRun(snapshot);
            Show(_hud);
        }

        public void PauseGame()
        {
            if (!Session.IsRunning || Session.Mode == GameMode.Duel) return; // le temps ne s'arrête pas chez l'adversaire
            Session.SetPaused(true);
            SaveRunSnapshot();
            _pause.Show();
        }

        public void ResumeGame()
        {
            _pause.Hide();
            Session.SetPaused(false);
        }

        public void RestartRun()
        {
            _pause.Hide();
            var mode = Session.Mode;
            uint seed = mode == GameMode.Infinite ? GameRng.RandomSeed() : Session.Seed;
            _save.ClearRun();
            StartRun(mode, seed);
        }

        public void Replay()
        {
            if (_current == _gameOver && !_leavingGameOver) { if (ChestWaiting) { OpenRunChest(Replay); return; } LeaveGameOver(Replay); return; }
            FinalizeRun();
            var mode = Session.Mode;
            uint seed = mode == GameMode.Infinite ? GameRng.RandomSeed() : mode == GameMode.Daily ? Daily.Seed(TodayKey) : Session.Seed;
            StartRun(mode, seed);
        }

        /// <summary>
        /// Scores en ligne d'une partie solo : le classement mondial (parties normales) et le classement du jour, qui
        /// garde le meilleur score de la journée, quelle que soit la partie (normale ou défi du jour).
        /// </summary>
        void EnqueueScores(RunResult result)
        {
            if (result.Mode == GameMode.Duel) return;
            if (result.Mode == GameMode.Infinite) Leaderboard.Enqueue(Boards.World, result.Score, Profile.selectedHead, "");
            string day = result.Mode == GameMode.Daily && !string.IsNullOrEmpty(result.DayKey) ? result.DayKey : TodayKey;
            Leaderboard.Enqueue(Boards.Daily, result.Score, Profile.selectedHead, day);
        }

        void OnRunEnded(RunResult result)
        {
            _save.ClearRun();
            if (result.Mode == GameMode.Duel)
            {
                // Duel : pas de bilan solo (record, classement, coffre) ; le résultat arrive quand les deux ont fini.
                Duel.OnLocalRunEnded(result);
                return;
            }
            _pendingResult = result;
            _pendingFinalized = false;
            _gainDoubled = false;
            _leavingGameOver = false;
            _chestOpened = false;
            // Le joueur a quitté l'écran de jeu pendant la chute (lien d'invitation à un duel) : la partie est
            // enregistrée sans bilan, pour ne pas afficher l'écran de fin par-dessus un autre écran.
            if (_current != _hud)
            {
                _pendingGain = null;
                EnqueueScores(result);
                FinalizeRun();
                Session.GoIdle();
                return;
            }

            // Aperçu des récompenses sans rien enregistrer : le joueur peut encore revivre.
            var preview = JsonUtility.FromJson<Profile>(JsonUtility.ToJson(Profile));
            preview.Sanitize();
            var rewards = Progress.ApplyRun(preview, result.Mode, result.Achievements, TodayKey);
            _pendingRewards = rewards;
            _pendingGain = EconomyRun.Apply(Tuning.economy, preview, result.Stats, LocalDay);
            _celebration = rewards.NewBest || rewards.NewHeads.Count > 0 || _pendingGain.LevelHeads.Count > 0;
            _lastRunSeconds = result.Seconds;
            // Classements : le score part tout de suite (avant, il partait en quittant l'écran de fin).
            // Une seconde chance ne peut que l'améliorer : le meilleur des deux est gardé.
            EnqueueScores(result);
            Leaderboard.Flush(TodayKey);

            // La carte de score n'est fabriquée qu'au moment de partager (sinon : à-coup à chaque défaite).
            Texture card = null;

            _hud.Hide();
            // Seconde chance : toujours contre une vidéo choisie par le joueur (même avec « Sans publicité », qui retire
            // seulement les pubs imposées entre les parties).
            bool canRevive = Session.CanRevive && _ads.CanShowRewarded && AdPolicy.CanWatchRewarded(Tuning.ads, Profile.ads, LocalDay);
            _gameOver.Present(result, rewards, _pendingGain, canRevive, false, card);
            _current = _gameOver;
        }

        /// <summary>Enregistre la partie terminée (record, têtes, pièces, missions, classement). Sans effet si déjà fait.</summary>
        void FinalizeRun()
        {
            if (_pendingFinalized || _pendingResult == null) return;
            _pendingFinalized = true;
            var r = _pendingResult;
            int levelBefore = Profile.level;
            bool tutorial = !Profile.tutorialDone;
            ApplyRunTo(Profile, r, out var rewards, out var gain, out bool starter);
            if (starter) _starterPopupPending = true;
            if (rewards.NewBest) _ratePending = true;
            if (tutorial) Stats.Event("tutorial_done", ("score", r.Score));
            SaveProfile();
            ShowLiveRewards();
            Leaderboard.Flush(TodayKey);
            if (rewards.NewHeads.Count > 0 || gain.LevelHeads.Count > 0) Session.SetHead(HeadCatalog.Get(Profile.selectedHead));

            Stats.Event("run_end", ("mode", r.Mode.ToString()), ("n", Profile.gamesPlayed), ("score", r.Score), ("seconds", r.Seconds), ("drops", r.Drops),
("lost", r.LostWeights), ("revived", r.Revived), ("best", rewards.NewBest),
                ("heads", rewards.NewHeads.Count + gain.LevelHeads.Count), ("coins", gain.Total), ("level", Profile.level));
            if (Profile.level > levelBefore) Stats.Event("level_up", ("level", Profile.level));
            foreach (var m in gain.MissionsDone) Stats.Event("mission_done", ("kind", m.kind.ToString()), ("target", m.target));
            if (gain.MissionsBonus) Stats.Event("missions_all");
        }

        /// <summary>Tout ce qu'une partie terminée change dans une sauvegarde (la vraie, ou une copie).</summary>
        void ApplyRunTo(Profile p, RunResult r, out RunRewards rewards, out EconomyGain gain, out bool starterStarted)
        {
            rewards = Progress.ApplyRun(p, r.Mode, r.Achievements, TodayKey);
            gain = EconomyRun.Apply(Tuning.economy, p, r.Stats, LocalDay);
            p.totalScore += r.Score;
            p.totalMerges += r.Merges;
            p.totalBalloons += r.Balloons;
            p.totalSeconds += r.Seconds;
            AdPolicy.OnRunEnded(p.ads);
            _gamesThisSession++; // aucune pub imposée pendant les premières parties de chaque lancement
            starterStarted = Offers.CheckStarter(Tuning.ads, p, Now);
            p.tutorialDone = true;
            // Quête du débutant et événement de la semaine (récompenses données ici ; affichées par FinalizeRun).
            var quest = BeginnerQuest.OnRun(p, r.Stats.Seed);
            var week = WeeklyEvent.OnRun(p, CurrentEvent, r.Stats);
            if (p == Profile) { _lastQuest = quest; _lastWeek = week; }
        }

        bool _privacyAskedHere;
        List<BeginnerQuest.Result> _lastQuest;
        List<WeeklyEvent.Reward> _lastWeek;
        /// <summary>Coffres gagnés hors fin de partie (quête, roue…), ouverts devant le joueur au retour à l'accueil.</summary>
        readonly Queue<(Chest.Loot loot, string title)> _bonusChests = new Queue<(Chest.Loot, string)>();

        /// <summary>Événement de la semaine en cours (lundi 00:00 UTC).</summary>
        public WeeklyEvent.Def CurrentEvent => WeeklyEvent.For(WeeklyEvent.WeekKey(DateTime.UtcNow));

        /// <summary>Récompenses de la quête et de l'événement : un bandeau chacune, les coffres attendent l'accueil.</summary>
        void ShowLiveRewards()
        {
            if (_lastQuest != null)
                foreach (var q in _lastQuest)
                {
                    string title = Loc.Format("quest_done", q.Step.Index + 1, BeginnerQuest.Steps.Length);
                    Stats.Event("quest_step", ("step", q.Step.Index + 1));
                    if (q.Chest != null) { _bonusChests.Enqueue((q.Chest, Loc.Get("quest_chest_title"))); _toast.Reward(Icon.Gift, Palette.Accent, title, Loc.Get("quest_chest_detail"), 0); }
                    else if (q.Plank != null) _toast.Reward(Icon.Star, Palette.Play, title, Loc.Format("quest_plank_detail", q.Plank.Name(Loc.Language)), 0);
                    else _toast.Reward(Icon.Coin, Palette.Accent, title, Loc.Get(q.Step.Key), q.Coins);
                    if (q.Plank != null) Session.SetPlank(q.Plank);
                }
            if (_lastWeek != null)
                foreach (var w in _lastWeek)
                {
                    string title = Loc.Format("event_tier_done", w.Tier + 1, WeeklyEvent.TierFractions.Length);
                    Stats.Event("event_tier", ("tier", w.Tier + 1));
                    if (w.Head != null) _toast.Reward(Icon.Crown, Palette.Purple, title, Loc.Format("event_head_won", w.Head.Name(Loc.Language)), 0);
                    else if (w.Ticket) _toast.Reward(Icon.Gift, Palette.Primary, title, Loc.Get("event_ticket_won"), 0);
                    else _toast.Reward(Icon.Coin, Palette.Accent, title, Loc.Get("event_coins_won"), w.Coins);
                }
            _lastQuest = null;
            _lastWeek = null;
        }

        /// <summary>
        /// L'application passe en arrière-plan sur l'écran de fin, alors que la partie n'est pas encore enregistrée
        /// (le joueur peut encore revivre) : on écrit sur le disque une copie de la sauvegarde avec la partie comptée.
        /// Si iOS ferme l'application, rien n'est perdu ; si le joueur revient, il peut toujours revivre, et la partie
        /// n'est comptée qu'une fois (avant : une pub « revivre » faisait compter la partie deux fois).
        /// </summary>
        void SaveProfileWithPendingRun()
        {
            if (_pendingFinalized || _pendingResult == null) { SaveProfile(); return; }
            try
            {
                var copy = JsonUtility.FromJson<Profile>(JsonUtility.ToJson(Profile));
                copy.Sanitize();
                ApplyRunTo(copy, _pendingResult, out _, out _, out _);
                _save.SaveProfile(copy);
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Sauvegarde de la partie en attente : " + e.Message); SaveProfile(); }
        }

        /// <summary>Un coffre gagné à cette partie attend d'être ouvert.</summary>
        public bool ChestWaiting => _pendingGain?.Chest != null && !_chestOpened && _pendingResult != null;

        /// <summary>
        /// Ouvre le coffre de la partie devant le joueur (écran du coffre). La partie est alors enregistrée : on ne peut
        /// plus revivre. <paramref name="after"/> : ce qui suit la fermeture (rejouer, accueil), ou rien.
        /// </summary>
        public void OpenRunChest(Action after = null)
        {
            if (!ChestWaiting) { after?.Invoke(); return; }
            _chestOpened = true;
            int goal = _pendingResult.Goal;
            FinalizeRun();
            _gameOver.HideRevive();
            var loot = _pendingGain.Chest;
            // Coffre de fin de partie : ses pièces peuvent être doublées avec une vidéo (une fois, au choix du joueur).
            Action<Action<int>> doubler = null;
            if (loot != null && loot.Coins > 0)
                doubler = done => WatchRewarded(Placements.ChestDouble, () =>
                {
                    int added = AdRewards.DoubleChest(Profile, loot);
                    Stats.Event("chest_doubled", ("coins", added));
                    done?.Invoke(added);
                });
            _chest.Present(_pendingGain, goal, () => { _gameOver.OnChestOpened(); after?.Invoke(); }, doubler);
        }

        public void PlaySfx(Sfx s, int step = 0, float volume = 1f) => _audio.Play(s, step, volume);
        public void HapticMedium() => _haptics.Medium();
        public void HapticLight() => _haptics.Light();

        void LeaveGameOver(Action next)
        {
            // Rapport 1.3 : l'âge et le choix des pubs se demandent à la fin de la première partie (le joueur a déjà
            // joué), avant la toute première pub.
            if (!_onboard.Visible && !_privacyAskedHere && PrivacyRules.NeedsAsking(Profile, CurrentYear))
            {
                _privacyAskedHere = true; // une fois par lancement : jamais de boucle
                FinalizeRun();
                _onboard.Open(() => { AfterOnboarding(); LeaveGameOver(next); }, false, true);
                return;
            }
            _leavingGameOver = true;
            _toast.Clear(); // le bilan de la partie ne suit pas le joueur sur l'écran suivant
            FinalizeRun();
            if (_ads.CanShowInterstitial && AdPolicy.ShouldShowInterstitial(Tuning.ads, Profile.ads, Profile, _celebration, _lastRunSeconds, Now, LocalDay, _gamesThisSession))
            {
                Stats.Event("ad_interstitial", ("games", Profile.gamesPlayed), ("today", Profile.ads.interstitialsToday + 1));
                _adStarted = Time.realtimeSinceStartup;
                _ads.ShowInterstitial(() =>
                {
                    _adStarted = -1f;
                    AdPolicy.OnInterstitialShown(Profile.ads, Now, LocalDay);
                    SaveProfile();
                    next();
                    _leavingGameOver = false;
                });
                return;
            }
            next();
            _leavingGameOver = false;
        }

        /// <summary>Une pub est à l'écran (ou va l'être). Garde-fou de 5 minutes si la régie ne répond jamais.</summary>
        bool AdBusy => _adStarted >= 0f && Time.realtimeSinceStartup - _adStarted < 300f;
        float _adStarted = -1f;

        /// <summary>Pub récompensée choisie par le joueur, avec plafond quotidien.</summary>
        public void WatchRewarded(string placement, Action onReward)
        {
            if (!AdPolicy.CanWatchRewarded(Tuning.ads, Profile.ads, LocalDay)) { _toast.Say(Loc.Get("free_coins_none")); return; }
            if (!_ads.CanShowRewarded) { _toast.Say(Loc.Get("ad_unavailable")); Stats.Event("ad_unavailable", ("placement", placement)); return; }
            if (AdBusy) return; // double toucher : une seule pub à la fois
            _adStarted = Time.realtimeSinceStartup;
            _ads.ShowRewarded(placement, ok =>
            {
                _adStarted = -1f;
                if (!ok) { _toast.Say(Loc.Get("ad_unavailable")); return; }
                AdPolicy.OnRewardedWatched(Profile.ads, Now, LocalDay);
                Stats.Event("ad_rewarded", ("placement", placement), ("today", Profile.ads.rewardedToday));
                onReward?.Invoke();
                SaveProfile();
            });
        }

        public void ReviveWithAd()
        {
            // Partie déjà enregistrée (coffre ouvert, écran de fin quitté) : on ne peut plus la reprendre.
            if (!Session.CanRevive || _pendingFinalized || _pendingResult == null || _current != _gameOver) { _gameOver.HideRevive(); return; }
            void Revive()
            {
                // Le bouton ne disparaît qu'ici : si la pub n'est pas disponible, la seconde chance reste proposée.
                _gameOver.HideRevive();
                _pendingFinalized = true; // la partie continue : on enregistrera à la vraie fin
                _pendingResult = null;
                Session.Revive();
                Show(_hud);
            }
            WatchRewarded(Placements.Revive, () => { Stats.Event("revive", ("free", false)); Revive(); });
        }

        /// <summary>Doubler les pièces de la partie (vidéo), une fois par partie.</summary>
        public void DoubleRunCoins()
        {
            if (_gainDoubled || _pendingGain == null || _pendingGain.Coins <= 0) return;
            WatchRewarded(Placements.DoubleCoins, () =>
            {
                _gainDoubled = true;
                Wallet.Add(Profile, _pendingGain.Coins);
                // Pas de bandeau : la carte de fin montre le gain elle-même.
                _gameOver.OnDoubled();
            });
        }

        /// <summary>Ce que raconte la partie qu'on vient de finir (titre, frise, record frôlé…).</summary>
        public BragInfo BragOf(RunResult r, RunRewards rewards)
        {
            return new BragInfo
            {
                Mode = r.Mode, DayKey = r.DayKey, Seed = r.Seed, Score = r.Score,
                NewBest = rewards != null ? rewards.NewBest : r.Score > Profile.bestScore,
                PreviousBest = rewards != null ? rewards.PreviousBest : Profile.bestScore,
                BestSave = r.BestSave, Saves = r.Saves, BestChain = r.BestChain, Balloons = r.Balloons,
                MaxValue = r.MaxValue, LostWeights = r.LostWeights, Seconds = r.Seconds, Tension = r.Tension,
            };
        }

        /// <summary>
        /// Partage : la carte 9:16 (photo de la chute, titre, score, code défi) et le résumé « à la Wordle ».
        /// Le résumé est aussi copié : TikTok et Instagram ignorent souvent le texte joint, on le colle en légende.
        /// </summary>
        public void ShareScore()
        {
            var r = _pendingResult ?? Session.BuildResult();
            var b = BragOf(r, _pendingResult != null ? _pendingRewards : null);
            var head = HeadCatalog.Get(Profile.selectedHead);
            try
            {
                string text = Core.Brag.Summary(b);
                _card.Render(b, head, Tuning.mascot.mascotName, Session.HasPhoto && Session.Photo.IsCreated() ? Session.Photo : null, Session.PhotoFocusY);
                GUIUtility.systemCopyBuffer = text;
                if (_card.Path != null) _share.ShareImage(_card.Path, text);
                else _share.ShareText(text);
                if (!_share.ShowsOwnPanel) _toast.Say(Loc.Get("caption_copied"), Icon.Share, Palette.Secondary);
                Stats.Event("share_score", ("mode", r.Mode.ToString()), ("score", r.Score), ("title", Core.Brag.TitleKey(b)), ("best", b.NewBest));
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Partage : " + e.Message); }
        }

        void OnRankKnown(string board, int rank)
        {
            // Places gagnées depuis la dernière fois (mondial, ou du jour pour le jour en cours), affichées en fin de partie.
            bool world = board == Boards.World;
            int previous = world ? Profile.worldRank : (Profile.dailyRankDay == TodayKey ? Profile.dailyRank : 0);
            if (world) Profile.worldRank = rank;
            else if (board == Boards.Daily) { Profile.dailyRank = rank; Profile.dailyRankDay = TodayKey; }
            SaveProfile();
            if (_gameOver.Visible && (world || board == Boards.Daily)) _gameOver.ShowRank(world, rank, previous);
            if (!world) return;
            foreach (var h in HeadCatalog.All)
            {
                if (h.Unlock != UnlockKind.WorldTop || Profile.unlockedHeads.Contains(h.Id) || rank > h.UnlockValue) continue;
                Profile.unlockedHeads.Add(h.Id);
                SaveProfile();
                _toast.Say(Loc.Format("new_head", h.Name(Loc.Language)), Icon.Face, Palette.Purple);
            }
        }

        // ------------------------------------------------------------------ Collection, réglages, achats

        /// <summary>Tête au hasard : activée ou coupée depuis la collection. Activée, elle change tout de suite de tête.</summary>
        public void SetRandomHead(bool on)
        {
            Profile.randomHead = on;
            if (on) RollRandomHead();
            SaveProfile();
            _collection.Refresh();
            if (on) _toast.Say(Loc.Get("random_head_on"), Icon.Face, Palette.Purple);
        }

        void RollRandomHead()
        {
            int id = HeadCatalog.PickRandom(Profile.unlockedHeads, Profile.selectedHead, () => UnityEngine.Random.value * 0.99999);
            if (id == Profile.selectedHead) return;
            Profile.selectedHead = id;
            SaveProfile();
            Session.SetHead(HeadCatalog.Get(id));
        }

        /// <summary>Réglages → Code promo : le code d'un partenaire débloque sa tête, une seule fois par joueur.</summary>
        public void AskPromoCode() =>
            _prompt.Ask(Loc.Get("promo_title"), "", InputField.ContentType.Standard, RedeemPromoCode, 24, Loc.Get("promo_note"));

        void RedeemPromoCode(string text)
        {
            var result = PromoCodes.Redeem(Profile, text, out var promo);
            switch (result)
            {
                case PromoResult.Empty:
                    return;
                case PromoResult.Unknown:
                    Stats.Event("promo_unknown");
                    _toast.Say(Loc.Get("promo_unknown"), Icon.Close, Palette.Primary, 3f);
                    return;
                case PromoResult.AlreadyUsed:
                    _toast.Say(Loc.Get("promo_used"), Icon.Check, Palette.Secondary, 3f);
                    return;
            }
            SaveProfile();
            Stats.Event("promo_ok", ("code", promo.Key));
            var head = HeadCatalog.Get(promo.HeadId);
            // Le personnage complet apparaît en grand (il tombe du ciel avec sa tenue et sa planche), puis « La porter ».
            _headReveal.Show(head, Loc.Get("promo_ok_title"), Loc.Format("promo_gift", promo.Partner), () => SelectHead(head.Id));
            if (_collection.Visible) _collection.Refresh();
        }

        public void SelectHead(int id)
        {
            if (!Profile.unlockedHeads.Contains(id)) return;
            Profile.randomHead = false; // choisir une tête précise coupe le hasard
            Profile.selectedHead = id;
            SaveProfile();
            Session.SetHead(HeadCatalog.Get(id));
            _collection.Refresh();
        }

        /// <summary>Tête verrouillée touchée dans la collection : l'acheter (pièces) ou aller à la boutique (premium).</summary>
        public void TapLockedHead(HeadDef h)
        {
            if (h.Unlock == UnlockKind.Premium) { ShowShop("collection"); return; }
            // Tête de partenaire : on propose tout de suite d'entrer le code.
            if (h.Unlock == UnlockKind.Code) { AskPromoCode(); return; }
            if (h.Unlock != UnlockKind.Shop) { _toast.Say(Loc.UnlockText(h)); return; }
            if (Profile.coins < h.UnlockValue)
            {
                _confirm.Ask(Loc.Format("missing_coins", Loc.FormatScore(h.UnlockValue - Profile.coins)), Loc.Get("p_coins_desc"), Art.Portrait(h),
                    Loc.Get("shop"), () => ShowShop("tete"), Loc.Get("cancel"), null);
                return;
            }
            _confirm.Ask(Loc.Format("buy_head", h.Name(Loc.Language)), Loc.FormatScore(h.UnlockValue), Art.Portrait(h),
                Loc.Get("buy"), () =>
                {
                    if (!EconomyRun.BuyHead(Profile, h)) return;
                    Stats.Event("head_buy", ("id", h.Id), ("price", h.UnlockValue));
                    _toast.Say(Loc.Format("bought_head", h.Name(Loc.Language)), Icon.Face, Palette.Purple);
                    SelectHead(h.Id);
                    ShowCollection();
                }, Loc.Get("cancel"), null);
        }

        // ------------------------------------------------------------------ Dépenses en pièces (rien ne change le score)

        /// <summary>Pas assez de pièces : on dit combien il manque et on propose la boutique.</summary>
        void MissingCoins(int price, Sprite image)
        {
            _confirm.Ask(Loc.Format("missing_coins", Loc.FormatScore(price - Profile.coins)), Loc.Get("p_coins_desc"), image,
                Loc.Get("see_coins"), () => { ShowShop("manque"); _shop.ScrollToCoins(); }, Loc.Get("cancel"), null);
        }

        /// <summary>Planche touchée dans la boutique : la choisir, ou l'acheter.</summary>
        public void TapPlank(PlankSkin s)
        {
            if (s.AdExclusive && !Cosmetics.OwnsPlank(Profile, s.Id)) { _toast.Say(Loc.Get("plank_exclusive_hint"), 3f); return; }
            if (Cosmetics.OwnsPlank(Profile, s.Id))
            {
                Cosmetics.SelectPlank(Profile, s.Id);
                SaveProfile();
                Session.SetPlank(s);
                _shop.Refresh();
                return;
            }
            var preview = _shop.PlankSprite(s);
            if (Profile.coins < s.Price) { MissingCoins(s.Price, preview); return; }
            _confirm.Ask(Loc.Format("buy_plank", s.Name(Loc.Language)), Loc.Format("price_coins", Loc.FormatScore(s.Price)) + "\n\n" + Loc.Get("cosmetic_note"), preview,
                Loc.Get("buy"), () =>
                {
                    if (!Cosmetics.BuyPlank(Profile, s)) return;
                    Stats.Event("plank_buy", ("id", s.Id), ("price", s.Price));
                    SaveProfile();
                    Session.SetPlank(s);
                    _toast.Reward(Icon.Star, Palette.Secondary, Loc.Format("bought_plank", s.Name(Loc.Language)), Loc.Get("plank_equipped_detail"), 0);
                    _shop.Refresh();
                }, Loc.Get("cancel"), null);
        }

        /// <summary>Machine à surprises : chances affichées avant d'ouvrir, jamais de doublon.</summary>
        public void OpenSurprise()
        {
            if (!SurpriseBox.Available(Profile)) { _toast.Say(Loc.Get("surprise_none"), Icon.Gift, Palette.Purple); return; }
            if (Profile.surpriseTickets <= 0 && Profile.coins < SurpriseBox.Price) { MissingCoins(SurpriseBox.Price, Art.IconSprite(Icon.Gift)); return; }
            _surprise.Present();
        }

        /// <summary>Bouton Ouvrir de la machine : la tête est tirée et payée, puis le carrousel la montre.</summary>
        public void ConfirmSurprise()
        {
            if (_surprise.Busy) return;
            var pool = SurpriseBox.Odds(Profile);
            bool free = Profile.surpriseTickets > 0;
            var won = SurpriseBox.Open(Profile, GameRng.HashSeed(Profile.playerId + "|surprise"), free);
            if (won == null) { _surprise.Hide(); MissingCoins(SurpriseBox.Price, Art.IconSprite(Icon.Gift)); return; }
            Stats.Event("surprise_open", ("kind", won.Kind.ToString()), ("id", won.Kind == PrizeKind.Head ? won.Head.Id : won.Kind == PrizeKind.Plank ? won.Plank.Id : won.Coins), ("free", free), ("n", Profile.surprisesOpened));
            SaveProfile();
            _haptics.Medium();
            _surprise.Spin(won, pool);
        }

        /// <summary>Lot de la machine à « porter » : la tête ou la planche gagnée.</summary>
        public void WearPrize(Prize p)
        {
            if (p == null) return;
            if (p.Kind == PrizeKind.Head) SelectHead(p.Head.Id);
            else if (p.Kind == PrizeKind.Plank) TapPlank(p.Plank);
        }

        public void OnSurpriseClosed() { if (_shop.Visible) _shop.Refresh(); if (_collection.Visible) _collection.Refresh(); }

        public void BuyShield()
        {
            if (Profile.streakShields >= StreakShield.Max) { _toast.Say(Loc.Get("shield_full"), Icon.Calendar, Palette.Accent); return; }
            if (Profile.coins < StreakShield.Price) { MissingCoins(StreakShield.Price, Art.IconSprite(Icon.Calendar)); return; }
            _confirm.Ask(Loc.Get("shield_title"), Loc.Get("shield_desc") + "\n\n" + Loc.Format("price_coins", Loc.FormatScore(StreakShield.Price)), Art.IconSprite(Icon.Calendar),
                Loc.Get("buy"), () =>
                {
                    if (!StreakShield.Buy(Profile)) return;
                    Stats.Event("shield_buy", ("stock", Profile.streakShields));
                    SaveProfile();
                    _toast.Say(Loc.Format("shield_stock", Profile.streakShields, StreakShield.Max), Icon.Calendar, Palette.Accent);
                    _shop.Refresh();
                }, Loc.Get("cancel"), null);
        }

        // ------------------------------------------------------------------ Cadeau, missions

        public void ClaimGift(bool doubled)
        {
            if (!DailyGift.Available(Profile.gift, LocalDay)) { _gift.Hide(); return; }
            void Claim(bool twice)
            {
                var r = DailyGift.Claim(Tuning.economy, Profile, LocalDay, GameRng.HashSeed(Profile.playerId + "|gift"));
                if (r == null) { _gift.Hide(); return; }
                if (twice) Wallet.Add(Profile, r.DoublableCoins);
                int coins = r.Coins * (twice ? 2 : 1);
                Stats.Event("gift_claim", ("day", r.Day), ("item", r.Item.ToString()), ("doubled", twice));
                SaveProfile();
                _gift.Hide();
                _home.Refresh();
                if (DailyGift.ShieldsUsedLastClaim > 0) _toast.Reward(Icon.Calendar, Palette.Accent, Loc.Get("shield_used"), Loc.Format("shield_stock", Profile.streakShields, StreakShield.Max), 0);
                switch (r.Item)
                {
                    case GiftItem.Chest:
                        // Le coffre du jour s'ouvre devant le joueur, comme celui des parties (on le tapote).
                        _chest.PresentLoot(r.Chest.Coins * (twice ? 2 : 1), r.Chest.Head, Loc.Get("gift_chest_title"), () => _home.Refresh());
                        break;
                    case GiftItem.Ticket:
                        _toast.Reward(Icon.Gift, Palette.Purple, Loc.Get("gift_ticket_title"), null, coins);
                        if (SurpriseBox.Available(Profile)) _surprise.Present();
                        break;
                    case GiftItem.Plank:
                        if (r.Plank != null)
                        {
                            var plank = r.Plank;
                            _confirm.Ask(Loc.Format("gift_plank_title", plank.Name(Loc.Language)), coins > 0 ? "+" + Loc.FormatScore(coins) + " " + Loc.Get("coins_word") : "", MetaUi.PlankPreview(Art, plank),
                                Loc.Get("wear"), () => TapPlank(plank), Loc.Get("later"), null);
                        }
                        else _toast.Reward(Icon.Gift, Palette.Primary, Loc.Get("gift_title"), Loc.Format("gift_day", r.Day), coins);
                        break;
                    default:
                        _toast.Reward(Icon.Gift, Palette.Primary, Loc.Get("gift_title"), Loc.Format("gift_day", r.Day), coins);
                        break;
                }
            }
            if (doubled) WatchRewarded(Placements.GiftDouble, () => Claim(true));
            else Claim(false);
        }

        /// <summary>Ce que le cadeau de tel jour fera gagner, en quelques mots (rappel « demain »).</summary>
        string GiftWords(int day)
        {
            var t = Tuning.economy;
            switch (DailyGift.ItemOf(t, day))
            {
                case GiftItem.Chest: return Loc.Get("gift_notif_chest");
                case GiftItem.Ticket: return Loc.Get("gift_notif_ticket");
                case GiftItem.Plank: return DailyGift.NextPlank(Profile) != null ? Loc.Get("gift_notif_plank") : Loc.Format("gift_notif_coins", Loc.FormatScore(DailyGift.Amount(t, day) + DailyGift.PlankFallbackCoins));
                default: return Loc.Format("gift_notif_coins", Loc.FormatScore(DailyGift.Amount(t, day)));
            }
        }

        public void RerollMission(int index)
        {
            WatchRewarded(Placements.Reroll, () =>
            {
                if (Missions.Reroll(Profile, index)) Stats.Event("mission_reroll", ("index", index));
                _missions.Refresh();
            });
        }

        // ------------------------------------------------------------------ Boutique

        public void BuyProduct(string id)
        {
            if (!_purchases.IsAvailable) { _toast.Say(Loc.Get("store_unavailable")); return; }
            Stats.Event("iap_start", ("product", id));
            _purchases.Buy(id, ok =>
            {
                if (!ok) { _toast.Say(Loc.Get("purchase_failed")); Stats.Event("iap_fail", ("product", id)); }
            });
        }

        /// <summary>Le store confirme un achat (ou le rend après une réinstallation) : on donne le produit.</summary>
        void OnPurchased(string id)
        {
            if (!Shop.Grant(Profile, id)) return;
            Stats.Event("iap_done", ("product", id));
            SaveProfile();
            _toast.Say(Loc.Get("thanks"), Icon.Heart, Palette.Primary);
            if (_shop.Visible) _shop.Refresh();
            if (_settings.Visible) _settings.Refresh();
            if (_home.Visible) _home.Refresh();
            if (_confirm.Visible) _confirm.Hide();
        }

        public void WatchFreeCoins()
        {
            if (AdPolicy.FreeCoinsLeft(Tuning.ads, Profile.ads, LocalDay) <= 0) { _toast.Say(Loc.Get("free_coins_none")); return; }
            WatchRewarded(Placements.FreeCoins, () =>
            {
                Profile.ads.freeCoinsToday++;
                Wallet.Add(Profile, Tuning.ads.freeCoinsAmount);
                _toast.Reward(Icon.Coin, Palette.Accent, Loc.Get("free_coins_title"), Loc.Format("free_coins_left", AdPolicy.FreeCoinsLeft(Tuning.ads, Profile.ads, LocalDay)), Tuning.ads.freeCoinsAmount);
                if (_shop.Visible) _shop.Refresh();
            });
        }

        // ------------------------------------------------------------------ Roue, quête, événement (Core/LiveOps.cs)

        public void ShowWheel()
        {
            FinalizeRun();
            Stats.Event("wheel_open", ("free", Wheel.FreeReady(Profile.wheel, Now) ? 1 : 0));
            Show(_wheelScreen);
        }

        public void ShowQuest() => _questScreen.Show();
        public void ShowEvent() => _eventScreen.Show();

        /// <summary>Tour de roue (gratuit, ou après une vidéo) : le lot est tiré et donné ; l'écran l'annonce à l'arrêt.</summary>
        public Wheel.Result SpinWheel(bool viaAd)
        {
            var r = Wheel.Spin(Profile, viaAd, Now, LocalDay, (uint)Environment.TickCount ^ (uint)Profile.wheel.spins * 7919u);
            if (r == null) return null;
            SaveProfile();
            Stats.Event("wheel_spin", ("ad", viaAd ? 1 : 0), ("prize", r.Kind.ToString()), ("coins", r.Coins));
            ScheduleNotifications();
            return r;
        }

        /// <summary>Le joueur a vu son lot : coffre ouvert devant lui, tête portée, accueil à jour.</summary>
        public void AfterWheelPrize(Wheel.Result r, bool viaAd)
        {
            if (r == null) return;
            if (r.Head != null) { SelectHead(r.Head.Id); _toast.Reward(Icon.Crown, Palette.Purple, Loc.Format("new_head", r.Head.Name(Loc.Language)), Loc.Get("wheel_detail_Jackpot"), 0); }
            if (r.Chest != null) _chest.PresentLoot(r.Chest.Coins, r.Chest.Head, Loc.Get("wheel_chest_title"), () => { if (_wheelScreen.Visible) _wheelScreen.Refresh(); });
        }

        // ------------------------------------------------------------------ Vidéos de la boutique (Core/AdRewards.cs)

        /// <summary>Une vidéo pour la Superstar (tête exclusive) : chaque vidéo vue compte, même des jours après.</summary>
        public void WatchForExclusiveHead()
        {
            var head = AdRewards.ExclusiveHead;
            if (head == null || AdRewards.HeadOwned(Profile)) return;
            WatchRewarded(Placements.ExclusiveHead, () =>
            {
                bool won = AdRewards.WatchedForHead(Tuning.ads, Profile);
                string name = head.Name(Loc.Language);
                if (won)
                {
                    Stats.Event("video_head_won", ("views", Profile.adRewards.headViews));
                    SelectHead(head.Id);
                    _toast.Reward(Icon.Crown, Palette.Purple, Loc.Format("video_won_head", name), Loc.Get("video_won_head_detail"), 0);
                    _haptics.Medium();
                }
                else VideoStepToast(AdRewards.HeadNeeded(Tuning.ads) - Profile.adRewards.headViews, name);
                if (_shop.Visible) _shop.Refresh();
            });
        }

        /// <summary>Une vidéo pour la planche « Scène étoilée » (assortie, exclusive).</summary>
        public void WatchForExclusivePlank()
        {
            var plank = AdRewards.ExclusivePlank;
            if (plank == null || AdRewards.PlankOwned(Profile)) return;
            WatchRewarded(Placements.ExclusivePlank, () =>
            {
                bool won = AdRewards.WatchedForPlank(Tuning.ads, Profile);
                string name = plank.Name(Loc.Language);
                if (won)
                {
                    Stats.Event("video_plank_won", ("views", Profile.adRewards.plankViews));
                    Session.SetPlank(plank);
                    _toast.Reward(Icon.Star, Palette.Purple, Loc.Format("video_won_plank", name), Loc.Get("plank_equipped_detail"), 0);
                    _haptics.Medium();
                }
                else VideoStepToast(AdRewards.PlankNeeded(Tuning.ads) - Profile.adRewards.plankViews, name);
                if (_shop.Visible) _shop.Refresh();
            });
        }

        void VideoStepToast(int left, string name)
        {
            _toast.Say(left <= 1 ? Loc.Format("video_last", name) : Loc.Format("video_left", left, name), Icon.Film, Palette.Purple);
        }

        /// <summary>Un coffre gratuit contre une vidéo (quelques fois par jour).</summary>
        public void WatchForChest()
        {
            if (AdRewards.ChestsLeft(Tuning.ads, Profile, LocalDay) <= 0)
            {
                _toast.Say(Loc.Format("video_chest_none", Tuning.ads.adChestsPerDay));
                return;
            }
            WatchRewarded(Placements.VideoChest, () =>
            {
                var loot = AdRewards.OpenChest(Tuning.ads, Profile, LocalDay, (uint)Environment.TickCount);
                if (loot == null) return;
                SaveProfile();
                Stats.Event("video_chest", ("coins", loot.Coins), ("head", loot.Head != null ? 1 : 0));
                _chest.PresentLoot(loot.Coins, loot.Head, Loc.Get("video_chest_won"), () => { if (_shop.Visible) _shop.Refresh(); });
            });
        }

        // ------------------------------------------------------------------ Rappels

        public void SetNotifications(bool on)
        {
            if (!on)
            {
                Profile.settings.notifications = false;
                _notifications.CancelAll();
                SaveProfile();
                if (_settings.Visible) _settings.Refresh();
                return;
            }
            _notifications.RequestPermission(granted =>
            {
                Profile.settings.notifications = granted;
                Stats.Event("notif_permission", ("granted", granted));
                SaveProfile();
                if (_settings.Visible) _settings.Refresh();
            });
        }

        /// <summary>
        /// Rappels programmés en quittant l'appli : au plus un par jour, en fin d'après-midi.
        /// Demain : cadeau et missions. Ce soir : série de défi en danger. Dans 3 jours : retour.
        /// </summary>
        void ScheduleNotifications()
        {
            if (!_notifications.IsSupported || !Profile.settings.notifications) return;
            _notifications.CancelAll();
            var today = DateTime.Now.Date;
            var now = DateTime.Now;
            // Au plus un rappel par jour (promis dans la politique de confidentialité) : le plus utile du jour gagne.
            var plan = new SortedDictionary<DateTime, (string id, string title, string body, DateTime at)>();
            void Add(string id, string title, string body, DateTime at)
            {
                if (at <= now.AddMinutes(5) || plan.ContainsKey(at.Date)) return;
                plan[at.Date] = (id, title, body, at);
            }
            int streak = Daily.CurrentStreak(Profile.daily, TodayKey);
            if (streak >= 2 && !Daily.PlayedToday(Profile.daily, TodayKey))
                Add("serie", Loc.Format("notif_streak_title", streak), Loc.Get("notif_streak_body"), today.AddHours(19));
            // Roue de la chance : prête 24 h après le dernier tour gratuit (jamais en pleine nuit).
            double wheelIn = Wheel.FreeIn(Profile.wheel, Now);
            if (Profile.wheel.spins > 0 && wheelIn > 0)
            {
                var at = now.AddSeconds(wheelIn);
                if (at.Hour < 9) at = at.Date.AddHours(9);
                else if (at.Hour >= 21) at = at.Date.AddDays(1).AddHours(9);
                Add("roue", Loc.Get("notif_wheel_title"), Loc.Get("notif_wheel_body"), at);
            }
            int nextDay = DailyGift.NextDay(Profile.gift, today.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), DailyGift.Cycle(Tuning.economy));
            Add("cadeau", Loc.Get("notif_gift_title"), Loc.Format("notif_gift_body", GiftWords(nextDay)), today.AddDays(1).AddHours(18).AddMinutes(30));
            // Superstar : plus que quelques vidéos (seulement si le joueur a commencé).
            int views = Profile.adRewards.headViews, need = AdRewards.HeadNeeded(Tuning.ads);
            if (views > 0 && !AdRewards.HeadOwned(Profile))
                Add("superstar", Loc.Get("notif_superstar_title"), Loc.Format("notif_superstar_body", need - views), today.AddDays(2).AddHours(18).AddMinutes(15));
            // Événement : les derniers jours de la semaine, s'il reste des paliers.
            if (Profile.week.week == WeeklyEvent.WeekKey(DateTime.UtcNow) && Profile.week.tiers < WeeklyEvent.TierFractions.Length)
            {
                var left = WeeklyEvent.Left(DateTime.UtcNow);
                if (left.TotalHours > 30) Add("semaine", Loc.Get("notif_event_title"), Loc.Get("notif_event_body"), now.Add(left).AddHours(-30).Date.AddHours(18));
            }
            Add("retour", Loc.Get("notif_comeback_title"), Loc.Format("notif_comeback_body", Loc.FormatScore(Profile.bestScore)), today.AddDays(3).AddHours(18).AddMinutes(30));
            foreach (var n in plan.Values) _notifications.Schedule(n.id, n.title, n.body, n.at);
        }

        public void AskPseudo()
        {
            _prompt.Ask(Loc.Get("pseudo_title"), Profile.pseudo, InputField.ContentType.Standard, name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                // Pseudo visible par tout le monde : longueur, caractères et grossièretés vérifiés.
                name = PseudoFilter.Clean(name);
                if (name == null) { _toast.Say(Loc.Get("pseudo_refused"), 3.5f); return; }
                SetPseudo(name);
            }, 16, Loc.Get("pseudo_rules"));
        }

        /// <summary>Pseudo déjà vérifié (PseudoFilter) : enregistré et envoyé au classement.</summary>
        public async void SetPseudo(string name)
        {
            Profile.pseudo = name;
            SaveProfile();
            Stats.Event("pseudo_set");
            await Leaderboard.Service.SetPseudoAsync(name);
            if (_leaderboard.Visible) _leaderboard.Refresh();
        }

        // ------------------------------------------------------------------ Premier lancement : pseudo, âge, pubs

        bool NeedsPseudo => string.IsNullOrEmpty(Profile.pseudo) && !Profile.pseudoAsked;
        /// <summary>Âge et pubs : demandés une fois que le joueur a fait sa première partie.</summary>
        bool NeedsPrivacyNow => PrivacyRules.NeedsAsking(Profile, CurrentYear) && Profile.gamesPlayed >= 1;
        bool NeedsOnboarding => NeedsPseudo || NeedsPrivacyNow;

        /// <summary>« C'est parti ! » sur l'écran de bienvenue : on enchaîne sur le pseudo (l'âge et les pubs viendront après la 1re partie).</summary>
        public void AfterWelcome()
        {
            if (NeedsOnboarding) _onboard.Open(AfterOnboarding, NeedsPseudo, NeedsPrivacyNow);
            else AfterOnboarding();
        }

        void AfterOnboarding()
        {
            SaveProfile();
            // La régie démarre une fois l'âge et le choix connus (sans effet si elle tourne déjà).
            if (!PrivacyRules.NeedsAsking(Profile, CurrentYear)) StartAds();
        }

        /// <summary>
        /// Choix des pubs (premier lancement ou réglages). Accord d'un joueur de 15 ans ou plus sur iPhone : la demande
        /// d'iOS suit (une seule fois dans la vie de l'app). <paramref name="then"/> : quand tout est fini.
        /// </summary>
        public void SetAdChoice(AdChoice choice, Action then)
        {
            if (choice == AdChoice.Personalized && PrivacyRules.IsMinor(Profile, CurrentYear))
            {
                _toast.Say(Loc.Get("ads_personal_minor_toast"), 3f);
                then?.Invoke();
                return;
            }
            PrivacyRules.SetChoice(Profile, choice, CurrentYear);
            SaveProfile();
            Stats.Event("ads_choice", ("personal", choice == AdChoice.Personalized ? 1 : 0));
            if (choice == AdChoice.Personalized && TrackingPermission.CanAsk)
            {
                TrackingPermission.Request(this, granted =>
                {
                    Stats.Event("att", ("granted", granted ? 1 : 0));
                    ApplyAdPrivacy();
                    then?.Invoke();
                });
                return;
            }
            ApplyAdPrivacy();
            then?.Invoke();
        }

        // ------------------------------------------------------------------ Diagnostic des pubs (caché)

        int _versionTaps;
        float _versionTapTime;

        /// <summary>
        /// Réglages : toucher 7 fois la ligne de version affiche l'état de la régie (codes d'erreur LevelPlay) ;
        /// 7 fois de plus demande le Test Suite LevelPlay au prochain lancement. Rien ne change pour le joueur.
        /// </summary>
        public void OnVersionTapped()
        {
            if (Time.unscaledTime - _versionTapTime > 1.5f) _versionTaps = 0;
            _versionTapTime = Time.unscaledTime;
            _versionTaps++;
            if (_versionTaps == 7) _toast.Say(AdDiagnostic, 9f);
            if (_versionTaps == 14)
            {
#if BASCULE_LEVELPLAY && !UNITY_EDITOR
                PlayerPrefs.SetInt(LevelPlayAdService.TestSuitePref, 1);
                PlayerPrefs.Save();
#endif
                _toast.Say(Loc.Get("diag_testsuite"), 5f);
                _versionTaps = 0;
            }
        }

        string AdDiagnostic
        {
            get
            {
#if BASCULE_LEVELPLAY && !UNITY_EDITOR
                if (_ads is LevelPlayAdService lp) return lp.Diagnostic;
#endif
                return _ads.IsSimulated ? Loc.Get("diag_simulated") : "Unity Ads";
            }
        }

        // ------------------------------------------------------------------ Modération du classement

        public void ShowPlayerMenu(string playerId, string name)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            _playerMenu.Open(playerId, name);
        }

        public void RemoveFriend(string playerId)
        {
            if (!FriendCode.Remove(Profile, playerId)) return;
            SaveProfile();
            _ = Leaderboard.Service.PublishFriendLinksAsync(Profile.friends);
            Stats.Event("friend_remove");
            _toast.Say(Loc.Get("friend_removed"), 2.5f);
            if (_leaderboard.Visible) _leaderboard.Refresh();
        }

        public void BlockPlayer(string playerId)
        {
            Moderation.Block(Profile, playerId, Leaderboard.Service.PlayerId);
            SaveProfile();
            Stats.Event("player_block");
            _toast.Say(Loc.Get("mod_blocked"), 3f);
            if (_leaderboard.Visible) _leaderboard.Refresh();
        }

        /// <summary>
        /// Signalement : un e-mail prérempli (pseudo + identifiants) part vers l'éditeur. Le joueur reste visible ;
        /// pour ne plus le voir, « Bloquer ». Pour supprimer le pseudo : tableau de bord Unity Cloud →
        /// Leaderboards → chercher l'identifiant du joueur.
        /// </summary>
        public void ReportPlayer(string playerId, string name)
        {
            // Signaler n'efface plus le joueur (1.2) : seul « Bloquer » le masque. L'e-mail part vers l'éditeur.
            Stats.Event("player_report");
            string subject = Uri.EscapeDataString(Loc.Get("mod_mail_subject"));
            string body = Uri.EscapeDataString(Loc.Format("mod_mail_body", name, playerId, Leaderboard.Service.PlayerId ?? ""));
            Application.OpenURL("mailto:" + ContactEmail + "?subject=" + subject + "&body=" + body);
            _toast.Say(Loc.Get("mod_reported"), 3.5f);
        }

        public void UnblockAll()
        {
            Moderation.UnblockAll(Profile);
            SaveProfile();
            _toast.Say(Loc.Get("mod_unblocked"));
            if (_settings.Visible) _settings.Refresh();
        }

        // ------------------------------------------------------------------ Amis

        public void ShareFriendCode()
        {
            string code = MyFriendCode;
            if (string.IsNullOrEmpty(code)) { _toast.Say(Loc.Get("friend_offline")); return; }
            Stats.Event("friend_share", ("short", string.IsNullOrEmpty(Profile.friendCode) ? 0 : 1));
            _share.ShareText(Loc.Format("friend_share_text", code));
        }

        public void AskFriendCode()
        {
            if (!Leaderboard.Service.IsOnline) { _toast.Say(Loc.Get("friend_offline")); return; }
            _prompt.Ask(Loc.Get("friend_prompt"), "", InputField.ContentType.Standard, async text =>
            {
                // Code court (« 4K7-Q2M ») : le serveur retrouve le joueur. Sinon, ancien code long collé tel quel.
                string id = null;
                var shortCode = FriendCode.ParseShort(text);
                if (shortCode != null) id = await Leaderboard.Service.FindPlayerByCodeAsync(shortCode);
                if (id == null) id = FriendCode.Parse(text);
                if (id == null) { _toast.Say(Loc.Get("friend_invalid")); return; }
                if (!FriendCode.Add(Profile, id, Leaderboard.Service.PlayerId)) { _toast.Say(Loc.Get("friend_already")); return; }
                SaveProfile();
                // Amitié réciproque : l'ami nous retrouvera et nous ajoutera à son tour (dès qu'il ouvre le jeu).
                _ = Leaderboard.Service.PublishFriendLinksAsync(Profile.friends);
                Stats.Event("friend_add", ("friends", Profile.friends.Count));
                _toast.Say(Loc.Get("friend_added"), Icon.Check, Palette.Play);
                if (_leaderboard.Visible) _leaderboard.Refresh();
            }, 120);
        }

        /// <summary>Droit à l'effacement : supprime le compte en ligne (pseudo, scores, amis). La progression locale reste.</summary>
        public void AskDeleteOnlineData()
        {
            _confirm.Ask(Loc.Get("delete_title"), Loc.Get("delete_body"), Art.IconSprite(Icon.Close), Loc.Get("delete_ok"), async () =>
            {
                if (!Leaderboard.Service.IsOnline) { _toast.Say(Loc.Get("delete_offline")); return; }
                bool ok = await Leaderboard.Service.DeleteAccountAsync();
                if (!ok) { _toast.Say(Loc.Get("delete_offline")); return; }
                Profile.pseudo = "";
                Profile.friendCode = ""; // le code ami publié en ligne est effacé avec le compte
                Profile.friends.Clear();
                Profile.pendingScores.Clear();
                SaveProfile();
                Stats.Event("online_data_deleted");
                _toast.Say(Loc.Get("delete_done"));
            }, Loc.Get("cancel"), null);
        }

        public void OpenSupport() => Application.OpenURL(SupportUrl);

        int _displayHz = 60;

        /// <summary>
        /// 60 images/s par défaut, comme la plupart des jeux mobiles : à 120, le téléphone chauffe et finit par
        /// ralentir de lui-même (et la batterie fond). 120 en option, seulement si l'écran le permet.
        /// </summary>
        void ApplyFrameRate(bool high)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1; // sur le web, le navigateur cadence l'affichage
#else
            Application.targetFrameRate = high ? _displayHz : 60;
#endif
        }

        public bool HighFrameRateAvailable => _displayHz > 60;

        /// <param name="rebuildUi">Vrai si la langue ou la taille du texte a changé (l'interface est reconstruite).</param>
        public void ApplySettings(bool rebuildUi)
        {
            var s = Profile.settings;
            _audio.Enabled = s.sound;
            _music.Enabled = s.music;
            _haptics.Enabled = s.vibration;
            ApplyFrameRate(s.highFrameRate);
            Loc.Language = Loc.Resolve(s.language, Application.systemLanguage.ToString());
            Tuning.plank.tiltDirection = s.tiltDirectionOverride != 0 ? s.tiltDirectionOverride : _tiltFromFile;
            Session.ApplyTuning(Tuning);
            Session.SetReducedMotion(s.reducedMotion);
            Session.SetHead(HeadCatalog.Get(Profile.selectedHead));
            Session.SetPlank(PlankCatalog.Get(Profile.selectedPlank));
            SaveProfile();
            if (rebuildUi)
            {
                var wasSettings = _current == _settings;
                BuildCanvas();
                Show(wasSettings ? (UiScreen)_settings : _home);
            }
        }

        public void ReloadTuning()
        {
            Tuning = TuningLoader.Load();
            _tiltFromFile = Tuning.plank.tiltDirection;
            ApplySettings(false);
            _toast.Say(Loc.Get("reload_tuning") + " : OK");
        }

        public void BuyRemoveAds() => BuyProduct(Shop.RemoveAds);

        public void RestorePurchases()
        {
            if (!_purchases.IsAvailable) { _toast.Say(Loc.Get("store_unavailable")); return; }
            _purchases.Restore(ok =>
            {
                // Toujours une réponse visible : réussi, ou boutique injoignable.
                _toast.Say(Loc.Get(ok ? "restored" : "store_unavailable"));
                if (_settings.Visible) _settings.Refresh();
                if (_shop.Visible) _shop.Refresh();
            });
        }

        public void ShowStats()
        {
            var p = Profile;
            string body = string.Join("\n", new[]
            {
                Loc.Format("stat_games", Loc.FormatScore(p.gamesPlayed)),
                Loc.Format("stat_time", Loc.Duration(p.totalSeconds)),
                Loc.Format("stat_merges", Loc.FormatScore(p.totalMerges)),
                Loc.Format("stat_balloons", Loc.FormatScore(p.totalBalloons)),
                Loc.Format("stat_days", p.daysPlayed),
                Loc.Format("best", Loc.FormatScore(p.bestScore)),
            });
            _confirm.Ask(Loc.Get("stats"), body, Art.Portrait(HeadCatalog.Get(p.selectedHead)), Loc.Get("pseudo_ok"), null, null, null);
        }

        public void OpenPrivacy() => Application.OpenURL(PrivacyUrl);

        // Outils de test (Réglages → Prototype, éditeur et versions de développement seulement).
        public void DebugAddCoins()
        {
            Wallet.Add(Profile, 1000);
            SaveProfile();
            _toast.Reward(Icon.Coin, Palette.Accent, "Test", null, 1000);
        }

        /// <summary>Fait comme si on était le lendemain : cadeau, missions et pubs du jour repartent.</summary>
        public void DebugNextDay()
        {
            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            if (Profile.gift.lastDay == LocalDay) Profile.gift.lastDay = yesterday;
            Profile.missions.day = "";
            Profile.ads.day = "";
            NewDayCheck();
            SaveProfile();
            _toast.Say(Loc.Get("debug_newday") + " : OK");
        }

        /// <summary>iOS : fenêtre de note native d'Apple. Android : page du jeu sur Google Play.</summary>
        bool RatingDue()
        {
            if (Profile.gamesPlayed < 5) return false;
#if UNITY_IOS && !UNITY_EDITOR
            if (Profile.ratingAsks >= 3) return false;
            return string.IsNullOrEmpty(Profile.ratingLastDay) || Dates.DaysBetween(Profile.ratingLastDay, LocalDay) >= 4;
#else
            return !Profile.ratingAsked && Profile.daysPlayed >= 2;
#endif
        }

        void OpenStoreReview()
        {
            Stats.Event("rate_yes");
#if UNITY_IOS && !UNITY_EDITOR
            if (!UnityEngine.iOS.Device.RequestStoreReview()) OpenWriteReview();
#else
            OpenWriteReview();
#endif
        }

        /// <summary>
        /// Réglages → « Noter Bascule » : la page « Écrire un avis » de l'App Store (étoiles + avis), toujours disponible.
        /// La fenêtre d'Apple, elle, peut ne pas s'afficher (Apple la limite à 3 fois par an et le joueur peut la couper).
        /// </summary>
        public void RateFromSettings()
        {
            Stats.Event("rate_settings");
            Profile.ratingAsked = true;
            SaveProfile();
            OpenWriteReview();
        }

        void OpenWriteReview()
        {
#if UNITY_IOS
            Application.OpenURL("itms-apps://apps.apple.com/app/id" + AppStoreId + "?action=write-review");
#else
            Application.OpenURL("market://details?id=" + Application.identifier);
#endif
        }

        /// <summary>Identifiant numérique du jeu sur l'App Store (App Store Connect → Informations sur l'app → Apple ID).</summary>
        const string AppStoreId = "6831568074";

        public void Toast(string message, float seconds = 2.5f) => _toast.Say(message, seconds);
        public void Reward(Icon icon, Color color, string title, string detail, int coins) => _toast.Reward(icon, color, title, detail, coins);
        public void RewardAt(float y, Icon icon, Color color, string title, string detail, int coins) => _toast.RewardAt(y, icon, color, title, detail, coins);
        public bool CoinsDoubled => _gainDoubled;
        public bool GameOverVisible => _gameOver != null && _gameOver.Visible;
        public bool HudVisible => _hud != null && _hud.Visible;

        // ------------------------------------------------------------------ Sauvegarde, cycle de vie

        void SaveProfile() => _save.SaveProfile(Profile);

        void SaveRunSnapshot()
        {
            if (!Session.IsRunning || Session.Mode == GameMode.Duel) return; // un duel ne se reprend pas plus tard
            var s = Session.Capture();
            if (s != null) _save.SaveRun(s);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_screenSize.x != Screen.width || _screenSize.y != Screen.height)
            {
                _screenSize = new Vector2Int(Screen.width, Screen.height);
                FitCanvas();
                Session.Layout();
            }

            Duel.Tick(Time.deltaTime);
            if (_hud.Visible) _hud.Tick(dt);
            if (_daily.Visible) _daily.Tick(dt);
            if (_duelHud.Visible) _duelHud.Tick(dt);
            if (_duelLobby.Visible) _duelLobby.Tick(dt);
            if (_duelResult.Visible) _duelResult.Tick(dt);
            if (_home.Visible) _home.Tick(dt);
            // Hors partie : invitations au duel toutes les 4 s, amis réciproques toutes les 30 s.
            if (SocialScreen)
            {
                _inviteTimer -= dt;
                if (_inviteTimer <= 0f) { _inviteTimer = 4f; CheckInvites(); }
                _socialTimer -= dt;
                if (_socialTimer <= 0f) { _socialTimer = 30f; SyncFriends(); }
            }
            // Le duel a commencé : l'invitation envoyée à l'ami n'a plus lieu d'être.
            if (_inviteSent && Duel.InMatch) ClearDuelInvite();
            if (_shop.Visible) _shop.Tick(dt);
            if (_wheelScreen.Visible) _wheelScreen.Tick(dt);
            if (_eventScreen.Visible) _eventScreen.Tick(dt);
            if (_missions.Visible) _missions.Tick(dt);
            if (_collection.Visible) _collection.Tick(dt);
            if (_leaderboard.Visible) _leaderboard.Tick(dt);
            if (_gameOver.Visible) _gameOver.Tick(dt);
            if (_chest.Visible) _chest.Tick(dt);
            if (_headReveal.Visible) _headReveal.Tick(dt);
            if (_surprise.Visible) _surprise.Tick(dt);
            UpdateMusic(dt);
            if (_toast.Visible) _toast.Tick(dt);

            if (Session.IsRunning && !Session.Paused)
            {
                _autosave -= dt;
                if (_autosave <= 0) { _autosave = AutosaveSeconds; SaveRunSnapshot(); }
            }

            // Bouton « retour » Android.
            if (BackPressed())
            {
                if (_chest.Visible) _chest.Back();
                else if (_headReveal.Visible) _headReveal.Back();
                else if (_surprise.Visible) _surprise.Back();
                else if (_prompt.Visible) _prompt.Hide();
                else if (_playerMenu.Visible) _playerMenu.Hide();
                else if (_confirm.Visible) _confirm.Hide();
                else if (_gift.Visible) _gift.Hide();
                else if (_welcome.Visible) { _welcome.Hide(); AfterWelcome(); }
                else if (_onboard.Visible) { /* questions du premier lancement : on y répond */ }
                else if (_duelInvite.Visible) _duelInvite.Hide();
                else if (_wheelScreen.Visible) _wheelScreen.Back();
                else if (_questScreen.Visible) _questScreen.Hide();
                else if (_eventScreen.Visible) _eventScreen.Hide();
                else if (_daily.Visible) _daily.Hide();
                else if (_current == _hud && Session.Mode == GameMode.Duel && Duel.InMatch) DuelAskQuit();
                else if (_current == _duelLobby) DuelCancel();
                else if (_current == _duelResult) DuelHome();
                else if (_current == _hud && Session.IsRunning) { if (_pause.Visible) ResumeGame(); else PauseGame(); }
                else if (_current == _collection || _current == _leaderboard || _current == _settings || _current == _missions || _current == _shop) GoHome();
            }
        }

        void UpdateMusic(float dt)
        {
            var s = Session;
            // Duel : musique compétitive, du compte à rebours jusqu'à l'écran de résultat.
            _music.SetDuel(Duel != null && (Duel.InMatch || Duel.Current == DuelController.Phase.Results));
            if (s.IsRunning && !s.Paused)
                _music.Set(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, s.Danger)), 1f);
            else if (s.Paused) _music.Set(0f, 0.25f);
            else if (s.HasRun) _music.Set(0f, 0.35f); // défaite, fin de partie
            else _music.Set(0f, 0.6f); // menus
            _music.Tick(dt);
        }

        static bool BackPressed()
        {
#if BASCULE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>Le joueur quitte l'application : on sauvegarde la partie pour la reprendre au même endroit.</summary>
        void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                if (_backgroundSince != DateTime.MinValue && (DateTime.UtcNow - _backgroundSince).TotalMinutes > 30) _gamesThisSession = 0;
                _backgroundSince = DateTime.MinValue;
                _music?.OnResume();
                // Retour dans l'appli : peut-être un nouveau jour (cadeau, missions).
                NewDayCheck();
                _notifications.CancelAll();
                if (_home.Visible) { _home.Refresh(); HomePopups(); }
                return;
            }
            _backgroundSince = DateTime.UtcNow;
            if (Duel.InMatch) Duel.OnAppPaused();
            else if (Session.IsRunning)
            {
                SaveRunSnapshot();
                if (!Session.Paused) PauseGame();
            }
            // Écran de fin : la partie reste « en attente » en mémoire (seconde chance encore possible, et une pub
            // en plein écran met aussi le jeu en pause), mais elle est déjà comptée dans la sauvegarde sur le disque.
            SaveProfileWithPendingRun();
            ScheduleNotifications();
            Stats.Event("app_pause", ("games", Profile.gamesPlayed));
            SaveService.Flush();
        }

        void OnApplicationQuit()
        {
            SaveRunSnapshot();
            FinalizeRun();
            SaveProfile();
            ScheduleNotifications();
            SaveService.Flush();
        }
    }
}
