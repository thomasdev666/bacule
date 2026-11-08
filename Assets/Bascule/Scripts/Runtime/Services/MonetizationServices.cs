using System;
using System.Collections;
using System.Collections.Generic;
using Bascule.Core;
using UnityEngine;

namespace Bascule.Services
{
    // =====================================================================================================
    // Publicités
    // =====================================================================================================

    /// <summary>
    /// Régie publicitaire. Deux formats seulement :
    /// - récompensée : toujours choisie par le joueur (revivre, doubler ses pièces, cadeau x2, pièces gratuites, changer une mission) ;
    /// - interstitiel : entre deux parties, selon les règles de Core.AdPolicy (jamais pendant une partie).
    /// Pas de bannière : elle gênerait la planche et rapporte peu.
    /// </summary>
    public interface IAdService
    {
        bool IsSimulated { get; }
        bool CanShowRewarded { get; }
        bool CanShowInterstitial { get; }
        /// <param name="onDone">true si la pub a été vue jusqu'au bout (récompense accordée).</param>
        void ShowRewarded(string placement, Action<bool> onDone);
        void ShowInterstitial(Action onDone);
    }

    public static class Placements
    {
        public const string Revive = "revive", DoubleCoins = "double_coins", GiftDouble = "gift_double", FreeCoins = "free_coins", Reroll = "mission_reroll";
    }

    /// <summary>Pub simulée (éditeur, ou tant que la régie n'est pas branchée) : un écran noir avec un compte à rebours.</summary>
    public sealed class SimulatedAdService : IAdService
    {
        readonly MonoBehaviour _host;
        readonly Action<bool, float, bool> _overlay;
        public float RewardedSeconds = 2.5f, InterstitialSeconds = 1.6f;

        /// <param name="overlay">Affiche/masque l'écran « Publicité… » (visible, secondes restantes, récompensée ?).</param>
        public SimulatedAdService(MonoBehaviour host, Action<bool, float, bool> overlay)
        {
            _host = host;
            _overlay = overlay;
        }

        public bool IsSimulated => true;
        public bool CanShowRewarded => true;
        public bool CanShowInterstitial => true;

        public void ShowRewarded(string placement, Action<bool> onDone) => _host.StartCoroutine(Run(RewardedSeconds, true, () => onDone?.Invoke(true)));
        public void ShowInterstitial(Action onDone) => _host.StartCoroutine(Run(InterstitialSeconds, false, onDone));

        IEnumerator Run(float seconds, bool rewarded, Action done)
        {
            float t = seconds;
            while (t > 0)
            {
                _overlay?.Invoke(true, t, rewarded);
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            _overlay?.Invoke(false, 0, rewarded);
            done?.Invoke();
        }
    }

    /// <summary>
    /// Version publiée sans régie publicitaire : aucune pub (jamais de fausse pub dans une version envoyée aux stores).
    /// Les boutons « regarder une pub » disent simplement qu'aucune pub n'est disponible.
    /// </summary>
    public sealed class NoAdService : IAdService
    {
        public bool IsSimulated => false;
        public bool CanShowRewarded => false;
        public bool CanShowInterstitial => false;
        public void ShowRewarded(string placement, Action<bool> onDone) => onDone?.Invoke(false);
        public void ShowInterstitial(Action onDone) => onDone?.Invoke();
    }

#if BASCULE_UNITYADS
    /// <summary>
    /// Unity Ads (paquet com.unity.ads, gratuit). Pubs non personnalisées : on déclare au SDK que le joueur
    /// n'a pas consenti au ciblage (RGPD) et qu'il faut des pubs « non comportementales ».
    /// Identifiants à remplir dans GameApp (AdIds) depuis le tableau de bord Unity Ads.
    /// </summary>
    public sealed class UnityAdsService : IAdService, UnityEngine.Advertisements.IUnityAdsInitializationListener,
        UnityEngine.Advertisements.IUnityAdsLoadListener, UnityEngine.Advertisements.IUnityAdsShowListener
    {
        readonly string _rewardedId, _interstitialId;
        bool _rewardedReady, _interstitialReady;
        Action<bool> _rewardedDone;
        Action _interstitialDone;
        int _retries;

        public UnityAdsService(string gameId, string rewardedId, string interstitialId, bool testMode)
        {
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            var gdpr = new UnityEngine.Advertisements.MetaData("gdpr");
            gdpr.Set("consent", "false");
            UnityEngine.Advertisements.Advertisement.SetMetaData(gdpr);
            var privacy = new UnityEngine.Advertisements.MetaData("privacy");
            privacy.Set("consent", "false");
            privacy.Set("mode", "mixed"); // public mixte : enfants possibles, donc jamais de ciblage
            UnityEngine.Advertisements.Advertisement.SetMetaData(privacy);
            var user = new UnityEngine.Advertisements.MetaData("user");
            user.Set("nonbehavioral", "true");
            UnityEngine.Advertisements.Advertisement.SetMetaData(user);
            if (!UnityEngine.Advertisements.Advertisement.isInitialized && UnityEngine.Advertisements.Advertisement.isSupported)
                UnityEngine.Advertisements.Advertisement.Initialize(gameId, testMode, this);
        }

        public bool IsSimulated => false;
        public bool CanShowRewarded => _rewardedReady;
        public bool CanShowInterstitial => _interstitialReady;

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (!_rewardedReady) { onDone?.Invoke(false); Load(_rewardedId); return; }
            _rewardedReady = false;
            _rewardedDone = onDone;
            UnityEngine.Advertisements.Advertisement.Show(_rewardedId, this);
        }

        public void ShowInterstitial(Action onDone)
        {
            if (!_interstitialReady) { onDone?.Invoke(); Load(_interstitialId); return; }
            _interstitialReady = false;
            _interstitialDone = onDone;
            UnityEngine.Advertisements.Advertisement.Show(_interstitialId, this);
        }

        void Load(string id) => UnityEngine.Advertisements.Advertisement.Load(id, this);

        public void OnInitializationComplete() { Load(_rewardedId); Load(_interstitialId); }
        public void OnInitializationFailed(UnityEngine.Advertisements.UnityAdsInitializationError error, string message) =>
            Debug.LogWarning("[Bascule] Unity Ads : initialisation impossible (" + error + ") " + message);

        public void OnUnityAdsAdLoaded(string placementId)
        {
            _retries = 0;
            if (placementId == _rewardedId) _rewardedReady = true;
            else if (placementId == _interstitialId) _interstitialReady = true;
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityEngine.Advertisements.UnityAdsLoadError error, string message)
        {
            Debug.LogWarning("[Bascule] Unity Ads : pas de pub (" + placementId + ", " + error + ")");
            if (_retries++ < 3 && GameApp.Instance != null) GameApp.Instance.StartCoroutine(RetryLater(placementId));
        }

        IEnumerator RetryLater(string id)
        {
            yield return new WaitForSecondsRealtime(10f * _retries);
            Load(id);
        }

        public void OnUnityAdsShowFailure(string placementId, UnityEngine.Advertisements.UnityAdsShowError error, string message) => Finish(placementId, false);
        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }
        public void OnUnityAdsShowComplete(string placementId, UnityEngine.Advertisements.UnityAdsShowCompletionState state) =>
            Finish(placementId, state == UnityEngine.Advertisements.UnityAdsShowCompletionState.COMPLETED);

        void Finish(string placementId, bool completed)
        {
            if (placementId == _rewardedId)
            {
                var cb = _rewardedDone; _rewardedDone = null;
                cb?.Invoke(completed);
            }
            else
            {
                var cb = _interstitialDone; _interstitialDone = null;
                cb?.Invoke();
            }
            Load(placementId);
        }
    }
#endif

#if BASCULE_LEVELPLAY
    /// <summary>
    /// Unity LevelPlay (paquet com.unity.services.levelplay, « Ads Mediation ») : la régie de Unity depuis 2026
    /// (le paquet Advertisement Legacy ne sert plus à gagner de l'argent). Pubs non personnalisées : pas de
    /// consentement au ciblage (RGPD) et « ne pas vendre » (CCPA), déclarés avant le démarrage.
    /// Clé d'app et identifiants des blocs à remplir dans GameApp (AdIds), depuis le tableau de bord Unity.
    /// Les rappels du SDK sont repassés sur le fil principal avant de toucher au jeu.
    /// </summary>
    public sealed class LevelPlayAdService : IAdService
    {
        readonly string _rewardedId, _interstitialId;
        global::Unity.Services.LevelPlay.LevelPlayRewardedAd _rewarded;
        global::Unity.Services.LevelPlay.LevelPlayInterstitialAd _interstitial;
        Action<bool> _rewardedDone;
        Action _interstitialDone;
        bool _earned, _ready;
        int _rewardedRetries, _interstitialRetries;
        readonly Queue<Action> _main = new Queue<Action>();

        /// <summary>Clé PlayerPrefs : ouvrir le Test Suite LevelPlay au prochain lancement (diagnostic caché, Réglages).</summary>
        public const string TestSuitePref = "bascule_lp_testsuite";
        string _init = "…", _rewardedState = "…", _interstitialState = "…";
        bool _testSuite;

        /// <summary>État lisible de la régie (Réglages : toucher 7 fois la ligne de version).</summary>
        public string Diagnostic => "LevelPlay " + _init + " | Récompensée : " + (_rewarded != null && _rewarded.IsAdReady() ? "prête" : _rewardedState)
            + " | Entre parties : " + (_interstitial != null && _interstitial.IsAdReady() ? "prête" : _interstitialState);

        public LevelPlayAdService(string appKey, string rewardedId, string interstitialId)
        {
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            if (GameApp.Instance != null) GameApp.Instance.StartCoroutine(Pump());
            // Avant le démarrage : aucune donnée utilisée pour cibler le joueur.
            global::Unity.Services.LevelPlay.LevelPlay.SetConsent(false);
            global::Unity.Services.LevelPlay.LevelPlay.SetMetaData("do_not_sell", "true");
            // Public mixte (des collégiens jouent) : l'identifiant publicitaire de l'appareil n'est pas collecté.
            global::Unity.Services.LevelPlay.LevelPlay.SetMetaData("is_deviceid_optout", "true");
            _testSuite = PlayerPrefs.GetInt(TestSuitePref, 0) == 1;
            if (_testSuite)
            {
                PlayerPrefs.SetInt(TestSuitePref, 0);
                PlayerPrefs.Save();
                global::Unity.Services.LevelPlay.LevelPlay.SetMetaData("is_test_suite", "enable");
            }
            global::Unity.Services.LevelPlay.LevelPlay.OnInitSuccess += config => Post(OnReady);
            global::Unity.Services.LevelPlay.LevelPlay.OnInitFailed += error => Post(() =>
            {
                _init = "échec " + (error != null ? error.ErrorCode + " " + error.ErrorMessage : "");
                Debug.LogWarning("[Bascule] LevelPlay : démarrage impossible (" + _init + ")");
            });
            global::Unity.Services.LevelPlay.LevelPlay.Init(appKey);
        }

        static string Err(global::Unity.Services.LevelPlay.LevelPlayAdError e) => e == null ? "erreur" : "erreur " + e.ErrorCode + " " + e.ErrorMessage;

        void Post(Action a) { lock (_main) _main.Enqueue(a); }

        IEnumerator Pump()
        {
            while (true)
            {
                while (true)
                {
                    Action a;
                    lock (_main) { if (_main.Count == 0) break; a = _main.Dequeue(); }
                    try { a(); } catch (Exception e) { Debug.LogException(e); }
                }
                yield return null;
            }
        }

        void OnReady()
        {
            _ready = true;
            _init = "OK";
            if (_testSuite) global::Unity.Services.LevelPlay.LevelPlay.LaunchTestSuite();
            _rewarded = new global::Unity.Services.LevelPlay.LevelPlayRewardedAd(_rewardedId);
            _rewarded.OnAdLoaded += info => Post(() => { _rewardedRetries = 0; _rewardedState = "prête"; });
            _rewarded.OnAdLoadFailed += error => Post(() => { _rewardedState = Err(error); RetryLater(true); });
            _rewarded.OnAdRewarded += (info, reward) => Post(() => _earned = true);
            _rewarded.OnAdDisplayFailed += (info, error) => Post(() => FinishRewarded(false));
            _rewarded.OnAdClosed += info => Post(() => GameApp.Instance.StartCoroutine(CloseRewarded()));
            _rewarded.LoadAd();

            _interstitial = new global::Unity.Services.LevelPlay.LevelPlayInterstitialAd(_interstitialId);
            _interstitial.OnAdLoaded += info => Post(() => { _interstitialRetries = 0; _interstitialState = "prête"; });
            _interstitial.OnAdLoadFailed += error => Post(() => { _interstitialState = Err(error); RetryLater(false); });
            _interstitial.OnAdDisplayFailed += (info, error) => Post(FinishInterstitial);
            _interstitial.OnAdClosed += info => Post(FinishInterstitial);
            _interstitial.LoadAd();
        }

        public bool IsSimulated => false;
        public bool CanShowRewarded => _ready && _rewarded != null && _rewarded.IsAdReady();
        public bool CanShowInterstitial => _ready && _interstitial != null && _interstitial.IsAdReady();

        public void ShowRewarded(string placement, Action<bool> onDone)
        {
            if (!CanShowRewarded) { onDone?.Invoke(false); if (_rewarded != null) _rewarded.LoadAd(); return; }
            _earned = false;
            _rewardedDone = onDone;
            _rewarded.ShowAd();
        }

        public void ShowInterstitial(Action onDone)
        {
            if (!CanShowInterstitial) { onDone?.Invoke(); if (_interstitial != null) _interstitial.LoadAd(); return; }
            _interstitialDone = onDone;
            _interstitial.ShowAd();
        }

        // Selon la régie, « récompense » arrive juste avant ou juste après « fermée » : on attend un instant.
        IEnumerator CloseRewarded()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            FinishRewarded(_earned);
        }

        void FinishRewarded(bool earned)
        {
            var cb = _rewardedDone; _rewardedDone = null;
            cb?.Invoke(earned);
            _earned = false;
            _rewarded?.LoadAd();
        }

        void FinishInterstitial()
        {
            var cb = _interstitialDone; _interstitialDone = null;
            cb?.Invoke();
            _interstitial?.LoadAd();
        }

        void RetryLater(bool rewarded)
        {
            int n = rewarded ? ++_rewardedRetries : ++_interstitialRetries;
            if (GameApp.Instance == null) return;
            GameApp.Instance.StartCoroutine(Reload(rewarded, Mathf.Min(60f, 10f * n)));
        }

        IEnumerator Reload(bool rewarded, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (rewarded) _rewarded?.LoadAd(); else _interstitial?.LoadAd();
        }
    }
#endif

    // =====================================================================================================
    // Achats
    // =====================================================================================================

    /// <summary>
    /// Achats in-app : uniquement du confort ou du cosmétique (retirer les pubs, pièces pour des têtes, têtes premium),
    /// jamais d'avantage de jeu (cahier des charges, section 9). Le catalogue est dans Core.Shop.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsAvailable { get; }
        /// <summary>Prix affiché, dans la devise du joueur quand le store a répondu.</summary>
        string Price(string productId);
        void Buy(string productId, Action<bool> onDone);
        void Restore(Action<bool> onDone);
        /// <summary>Un achat est confirmé (achat, restauration, ou achat terminé après un redémarrage) : donner le produit.</summary>
        event Action<string> Purchased;
    }

    /// <summary>En attendant Unity IAP : l'achat est simulé dans l'éditeur, indisponible sur téléphone.</summary>
    public sealed class StubPurchaseService : IPurchaseService
    {
        public bool IsAvailable => Application.isEditor;
        public event Action<string> Purchased;

        public string Price(string productId) => Shop.Get(productId)?.DefaultPrice ?? "";

        public void Buy(string productId, Action<bool> onDone)
        {
            Debug.Log("[Bascule] Achat simulé : " + productId);
            bool ok = Application.isEditor;
            if (ok) Purchased?.Invoke(productId);
            onDone?.Invoke(ok);
        }

        public void Restore(Action<bool> onDone) => onDone?.Invoke(false);
    }

#if BASCULE_IAP
    /// <summary>
    /// Unity IAP (paquet com.unity.purchasing, version 5.x, API « Legacy » héritée de la 4.x, toujours fournie mais déconseillée : avertissements normaux). Produits à créer avec les mêmes identifiants
    /// dans Google Play Console et App Store Connect (voir docs/INTEGRATIONS.md).
    /// La vérification des reçus côté serveur est conseillée avant un gros lancement.
    /// </summary>
    public sealed class UnityIapService : IPurchaseService, UnityEngine.Purchasing.IDetailedStoreListener
    {
        UnityEngine.Purchasing.IStoreController _controller;
        UnityEngine.Purchasing.IExtensionProvider _extensions;
        readonly Dictionary<string, Action<bool>> _pending = new Dictionary<string, Action<bool>>();
        public event Action<string> Purchased;

        public UnityIapService() { Init(); }

        async void Init()
        {
            // Unity IAP 4.x veut que les services Unity soient démarrés avant lui (sinon avertissement, et certains
            // magasins refusent de s'initialiser). Sans réseau, on continue quand même : l'App Store répondra plus tard.
            try
            {
                if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
                    await Unity.Services.Core.UnityServices.InitializeAsync();
            }
            catch (Exception e) { Debug.LogWarning("[Bascule] Services Unity non démarrés : " + e.Message); }
            var builder = UnityEngine.Purchasing.ConfigurationBuilder.Instance(UnityEngine.Purchasing.StandardPurchasingModule.Instance());
            foreach (var p in Shop.Products)
                builder.AddProduct(p.Id, p.Kind == ProductKind.Consumable ? UnityEngine.Purchasing.ProductType.Consumable : UnityEngine.Purchasing.ProductType.NonConsumable);
            UnityEngine.Purchasing.UnityPurchasing.Initialize(this, builder);
        }

        public bool IsAvailable => _controller != null;

        public string Price(string productId)
        {
            var p = _controller?.products.WithID(productId);
            return p != null && p.metadata != null && !string.IsNullOrEmpty(p.metadata.localizedPriceString)
                ? p.metadata.localizedPriceString
                : Shop.Get(productId)?.DefaultPrice ?? "";
        }

        public void Buy(string productId, Action<bool> onDone)
        {
            if (_controller == null) { onDone?.Invoke(false); return; }
            _pending[productId] = onDone;
            _controller.InitiatePurchase(productId);
        }

        public void Restore(Action<bool> onDone)
        {
            if (_extensions == null) { onDone?.Invoke(false); return; }
#if UNITY_IOS
            _extensions.GetExtension<UnityEngine.Purchasing.IAppleExtensions>().RestoreTransactions((ok, _) => onDone?.Invoke(ok));
#else
            // Android : Google Play rend les achats définitifs tout seul au lancement (ProcessPurchase).
            onDone?.Invoke(true);
#endif
        }

        public void OnInitialized(UnityEngine.Purchasing.IStoreController controller, UnityEngine.Purchasing.IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            // Achats définitifs déjà faits (réinstallation, autre appareil Android) : on les redonne.
            foreach (var p in controller.products.all)
                if (p.hasReceipt && p.definition.type == UnityEngine.Purchasing.ProductType.NonConsumable) Purchased?.Invoke(p.definition.id);
        }

        public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error) => Debug.LogWarning("[Bascule] IAP indisponible : " + error);
        public void OnInitializeFailed(UnityEngine.Purchasing.InitializationFailureReason error, string message) => Debug.LogWarning("[Bascule] IAP indisponible : " + error + " " + message);

        public UnityEngine.Purchasing.PurchaseProcessingResult ProcessPurchase(UnityEngine.Purchasing.PurchaseEventArgs e)
        {
            string id = e.purchasedProduct.definition.id;
            Purchased?.Invoke(id);
            Done(id, true);
            return UnityEngine.Purchasing.PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.PurchaseFailureReason reason) => Done(product.definition.id, false);
        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.PurchaseFailureDescription description) => Done(product.definition.id, false);

        void Done(string id, bool ok)
        {
            if (!_pending.TryGetValue(id, out var cb)) return;
            _pending.Remove(id);
            cb?.Invoke(ok);
        }
    }
#endif

    // =====================================================================================================
    // Partage
    // =====================================================================================================

    /// <summary>Partage natif de la carte de score.</summary>
    public interface IShareService
    {
        void ShareImage(string imagePath, string text);
        /// <summary>Vrai si le service affiche son propre panneau (web) : pas besoin du message « légende copiée ».</summary>
        bool ShowsOwnPanel { get; }
        /// <summary>Partage un simple texte (code ami). Le texte est aussi copié dans le presse-papiers.</summary>
        void ShareText(string text);
    }

    /// <summary>
    /// Avec le paquet NativeShare (yasirkula) installé, ouvre la feuille de partage du téléphone
    /// (WhatsApp, Snapchat, Instagram, TikTok…). Sans lui, copie le texte et indique où est l'image.
    /// </summary>
    public sealed class NativeShareService : IShareService
    {
        readonly Action<string> _toast;

        public NativeShareService(Action<string> toast) { _toast = toast; }

        public bool ShowsOwnPanel => false;

        public void ShareImage(string imagePath, string text)
        {
#if BASCULE_NATIVESHARE
            new NativeShare().AddFile(imagePath).SetText(text).Share();
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("[Bascule] Carte de score enregistrée : " + imagePath + " (installer NativeShare pour le partage natif, voir docs/INTEGRATIONS.md)");
            _toast?.Invoke(Application.isEditor ? "Image : " + imagePath : "Texte copié !");
#endif
        }

        public void ShareText(string text)
        {
            GUIUtility.systemCopyBuffer = text;
#if BASCULE_NATIVESHARE
            new NativeShare().SetText(text).Share();
#else
            _toast?.Invoke(Loc.Get("friend_copied"));
#endif
        }
    }
}
