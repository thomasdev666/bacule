// Copies minimales des API publiques des paquets Unity Ads 4.x, LevelPlay 8.x, Unity IAP 4.x, Mobile Notifications 2.x et Unity Gaming Services (Leaderboards 2.x),
// uniquement pour vérifier hors Unity que le code qui les utilise compile :
//   dotnet build Tools/CompileCheck -p:ExtraDefines="BASCULE_STUBS;BASCULE_UNITYADS;BASCULE_IAP;BASCULE_NOTIFICATIONS;BASCULE_UGS;UNITY_ANDROID"
// Ne fait pas partie du jeu.
#if BASCULE_STUBS
using System;
using System.Collections.Generic;

namespace UnityEngine.Advertisements
{
    public enum UnityAdsInitializationError { UNKNOWN, INTERNAL_ERROR, INVALID_ARGUMENT, AD_BLOCKER_DETECTED }
    public enum UnityAdsLoadError { INITIALIZE_FAILED, INTERNAL_ERROR, INVALID_ARGUMENT, NO_FILL, TIMEOUT, UNKNOWN }
    public enum UnityAdsShowError { NOT_INITIALIZED, NOT_READY, VIDEO_PLAYER_ERROR, INVALID_ARGUMENT, NO_CONNECTION, ALREADY_SHOWING, INTERNAL_ERROR, TIMEOUT, UNKNOWN }
    public enum UnityAdsShowCompletionState { SKIPPED, COMPLETED, UNKNOWN }
    public interface IUnityAdsInitializationListener { void OnInitializationComplete(); void OnInitializationFailed(UnityAdsInitializationError error, string message); }
    public interface IUnityAdsLoadListener { void OnUnityAdsAdLoaded(string placementId); void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message); }
    public interface IUnityAdsShowListener
    {
        void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message);
        void OnUnityAdsShowStart(string placementId);
        void OnUnityAdsShowClick(string placementId);
        void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState);
    }
    public class MetaData { public MetaData(string category) { } public void Set(string key, object value) { } }
    public static class Advertisement
    {
        public static bool isInitialized => false;
        public static bool isSupported => false;
        public static void Initialize(string gameId, bool testMode, IUnityAdsInitializationListener listener) { }
        public static void Load(string placementId, IUnityAdsLoadListener listener) { }
        public static void Show(string placementId, IUnityAdsShowListener listener) { }
        public static void SetMetaData(MetaData metaData) { }
    }
}

namespace UnityEngine.Purchasing.Extension
{
    public class PurchaseFailureDescription { public string productId; public UnityEngine.Purchasing.PurchaseFailureReason reason; public string message; }
    public interface IPurchasingModule { }
}

namespace UnityEngine.Purchasing
{
    public enum ProductType { Consumable, NonConsumable, Subscription }
    public enum InitializationFailureReason { PurchasingUnavailable, NoProductsAvailable, AppNotKnown }
    public enum PurchaseFailureReason { PurchasingUnavailable, ExistingPurchasePending, ProductUnavailable, SignatureInvalid, UserCancelled, PaymentDeclined, DuplicateTransaction, Unknown }
    public enum PurchaseProcessingResult { Complete, Pending }
    public class ProductDefinition { public string id; public ProductType type; }
    public class ProductMetadata { public string localizedPriceString; }
    public class Product { public ProductDefinition definition; public ProductMetadata metadata; public bool hasReceipt; }
    public class ProductCollection { public Product WithID(string id) => null; public Product[] all => new Product[0]; }
    public interface IStoreController { ProductCollection products { get; } void InitiatePurchase(string productId); }
    public interface IStoreExtension { }
    public interface IExtensionProvider { T GetExtension<T>() where T : IStoreExtension; }
    public interface IAppleExtensions : IStoreExtension { void RestoreTransactions(Action<bool, string> callback); }
    public class PurchaseEventArgs { public Product purchasedProduct; }
    public interface IStoreListener
    {
        void OnInitialized(IStoreController controller, IExtensionProvider extensions);
        void OnInitializeFailed(InitializationFailureReason error);
        void OnInitializeFailed(InitializationFailureReason error, string message);
        PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent);
        void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason);
    }
    public interface IDetailedStoreListener : IStoreListener { void OnPurchaseFailed(Product product, Extension.PurchaseFailureDescription failureDescription); }
    public class StandardPurchasingModule : Extension.IPurchasingModule { public static StandardPurchasingModule Instance() => null; }
    public class ConfigurationBuilder
    {
        public static ConfigurationBuilder Instance(Extension.IPurchasingModule first, params Extension.IPurchasingModule[] rest) => null;
        public ConfigurationBuilder AddProduct(string id, ProductType type) => this;
    }
    public static class UnityPurchasing { public static void Initialize(IDetailedStoreListener listener, ConfigurationBuilder builder) { } }
}

namespace Unity.Notifications.Android
{
    public enum Importance { None, Low, Default, High }
    public enum PermissionStatus { NotRequested, Allowed, Denied, DeniedDontAskAgain, RequestPending, NotificationsBlockedForApp }
    public class AndroidNotificationChannel { public AndroidNotificationChannel(string id, string name, string description, Importance importance) { } }
    public struct AndroidNotification { public string Title { get; set; } public string Text { get; set; } public DateTime FireTime { get; set; } }
    public class PermissionRequest { public PermissionStatus Status => PermissionStatus.Allowed; }
    public static class AndroidNotificationCenter
    {
        public static void RegisterNotificationChannel(AndroidNotificationChannel channel) { }
        public static int SendNotification(AndroidNotification notification, string channelId) => 0;
        public static void CancelAllScheduledNotifications() { }
    }
}

namespace Unity.Notifications.iOS
{
    [Flags] public enum AuthorizationOption { None = 0, Badge = 1, Sound = 2, Alert = 4 }
    public class AuthorizationRequest : IDisposable
    {
        public AuthorizationRequest(AuthorizationOption options, bool registerForRemote) { }
        public bool IsFinished => true;
        public bool Granted => true;
        public void Dispose() { }
    }
    public interface iOSNotificationTrigger { }
    public struct iOSNotificationTimeIntervalTrigger : iOSNotificationTrigger { public TimeSpan TimeInterval { get; set; } public bool Repeats { get; set; } }
    public class iOSNotification
    {
        public string Identifier { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool ShowInForeground { get; set; }
        public iOSNotificationTrigger Trigger { get; set; }
    }
    public static class iOSNotificationCenter
    {
        public static void ScheduleNotification(iOSNotification notification) { }
        public static void RemoveAllScheduledNotifications() { }
        public static int ApplicationBadge { get; set; }
    }
}

namespace Unity.Services.Core
{
    public enum ServicesInitializationState { Uninitialized, Initializing, Initialized }
    public static class UnityServices
    {
        public static ServicesInitializationState State => ServicesInitializationState.Uninitialized;
        public static System.Threading.Tasks.Task InitializeAsync() => System.Threading.Tasks.Task.CompletedTask;
    }
}

namespace Unity.Services.Authentication
{
    public interface IAuthenticationService
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        string PlayerName { get; }
        System.Threading.Tasks.Task SignInAnonymouslyAsync(SignInOptions options = null);
        System.Threading.Tasks.Task<string> UpdatePlayerNameAsync(string playerName);
        System.Threading.Tasks.Task DeleteAccountAsync();
    }
    public class SignInOptions { }
    public static class AuthenticationService { public static IAuthenticationService Instance => null; }
}

namespace Unity.Services.Leaderboards.Models
{
    public class LeaderboardEntry { public string PlayerId; public string PlayerName; public int Rank; public double Score; public string Metadata; }
    public class LeaderboardScoresPage { public System.Collections.Generic.List<LeaderboardEntry> Results; }
    public class LeaderboardScores { public System.Collections.Generic.List<LeaderboardEntry> Results; }
}

namespace Unity.Services.Leaderboards
{
    using Unity.Services.Leaderboards.Models;
    using System.Threading.Tasks;
    public class AddPlayerScoreOptions { public object Metadata { get; set; } }
    public class GetScoresOptions { public int? Limit { get; set; } public bool IncludeMetadata { get; set; } }
    public class GetPlayerScoreOptions { public bool IncludeMetadata { get; set; } }
    public class GetScoresByPlayerIdsOptions { public bool IncludeMetadata { get; set; } }
    public interface ILeaderboardsService
    {
        Task<LeaderboardEntry> AddPlayerScoreAsync(string leaderboardId, double score, AddPlayerScoreOptions options = null);
        Task<LeaderboardScoresPage> GetScoresAsync(string leaderboardId, GetScoresOptions options = null);
        Task<LeaderboardEntry> GetPlayerScoreAsync(string leaderboardId, GetPlayerScoreOptions options = null);
        Task<LeaderboardScores> GetScoresByPlayerIdsAsync(string leaderboardId, System.Collections.Generic.List<string> playerIds, GetScoresByPlayerIdsOptions options = null);
    }
    public static class LeaderboardsService { public static ILeaderboardsService Instance => null; }
}
namespace Unity.Services.LevelPlay
{
    public class LevelPlayConfiguration { }
    public class LevelPlayInitError { }
    public class LevelPlayAdInfo { }
    public class LevelPlayAdError { }
    public class LevelPlayAdDisplayInfoError { }
    public class LevelPlayReward { }
    public enum LevelPlayAdFormat { REWARDED, INTERSTITIAL, BANNER }
    public class LevelPlay
    {
        public static event Action<LevelPlayConfiguration> OnInitSuccess { add { } remove { } }
        public static event Action<LevelPlayInitError> OnInitFailed { add { } remove { } }
        public static void Init(string appKey, string userId = null, LevelPlayAdFormat[] adFormats = null) { }
        public static void SetConsent(bool consent) { }
        public static void SetMetaData(string key, string value) { }
    }
    public class LevelPlayRewardedAd
    {
        public LevelPlayRewardedAd(string adUnitId) { }
        public event Action<LevelPlayAdInfo> OnAdLoaded { add { } remove { } }
        public event Action<LevelPlayAdError> OnAdLoadFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdDisplayed { add { } remove { } }
        public event Action<LevelPlayAdDisplayInfoError> OnAdDisplayFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo, LevelPlayReward> OnAdRewarded { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdClosed { add { } remove { } }
        public void LoadAd() { }
        public void ShowAd(string placementName = null) { }
        public bool IsAdReady() => false;
    }
    public class LevelPlayInterstitialAd
    {
        public LevelPlayInterstitialAd(string adUnitId) { }
        public event Action<LevelPlayAdInfo> OnAdLoaded { add { } remove { } }
        public event Action<LevelPlayAdError> OnAdLoadFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdDisplayed { add { } remove { } }
        public event Action<LevelPlayAdDisplayInfoError> OnAdDisplayFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdClosed { add { } remove { } }
        public void LoadAd() { }
        public void ShowAd(string placementName = null) { }
        public bool IsAdReady() => false;
    }
}
#endif
