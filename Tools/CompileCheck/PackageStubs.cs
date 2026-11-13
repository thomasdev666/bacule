// Copies minimales des API publiques des paquets Unity Ads 4.x, LevelPlay 8.x, Unity IAP 5.x (API « Legacy »), Mobile Notifications 2.x et Unity Gaming Services (Leaderboards 2.x),
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
    public interface IPurchasingModule { }
}

namespace UnityEngine.Purchasing
{
    public enum ProductType { Consumable, NonConsumable, Subscription }
    // IAP 5 : déplacé de UnityEngine.Purchasing.Extension vers UnityEngine.Purchasing.
    public class PurchaseFailureDescription { public string productId; public PurchaseFailureReason reason; public string message; }
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
    public interface IDetailedStoreListener : IStoreListener { void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription); }
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
    public class GetScoresOptions { public int? Offset { get; set; } public int? Limit { get; set; } public bool IncludeMetadata { get; set; } }
    public class GetPlayerRangeOptions { public int? RangeLimit { get; set; } public bool IncludeMetadata { get; set; } }
    public class GetPlayerScoreOptions { public bool IncludeMetadata { get; set; } }
    public class GetScoresByPlayerIdsOptions { public bool IncludeMetadata { get; set; } }
    public interface ILeaderboardsService
    {
        Task<LeaderboardEntry> AddPlayerScoreAsync(string leaderboardId, double score, AddPlayerScoreOptions options = null);
        Task<LeaderboardScoresPage> GetScoresAsync(string leaderboardId, GetScoresOptions options = null);
        Task<LeaderboardEntry> GetPlayerScoreAsync(string leaderboardId, GetPlayerScoreOptions options = null);
        Task<LeaderboardScores> GetPlayerRangeAsync(string leaderboardId, GetPlayerRangeOptions options = null);
        Task<LeaderboardScores> GetScoresByPlayerIdsAsync(string leaderboardId, System.Collections.Generic.List<string> playerIds, GetScoresByPlayerIdsOptions options = null);
    }
    public static class LeaderboardsService { public static ILeaderboardsService Instance => null; }
}
namespace Unity.Services.LevelPlay
{
    public class LevelPlayConfiguration { }
    public class LevelPlayInitError { public int ErrorCode { get; } public string ErrorMessage { get; } }
    public class LevelPlayAdInfo { }
    public class LevelPlayAdError { public int ErrorCode { get; } public string ErrorMessage { get; } }
    public class LevelPlayReward { }
    public enum LevelPlayAdFormat { REWARDED, INTERSTITIAL, BANNER }
    public class LevelPlay
    {
        public static event Action<LevelPlayConfiguration> OnInitSuccess { add { } remove { } }
        public static event Action<LevelPlayInitError> OnInitFailed { add { } remove { } }
        public static void Init(string appKey, string userId = null, LevelPlayAdFormat[] adFormats = null) { }
        public static void SetConsent(bool consent) { }
        public static void SetMetaData(string key, string value) { }
        public static void LaunchTestSuite() { }
    }
    public class LevelPlayRewardedAd
    {
        public LevelPlayRewardedAd(string adUnitId) { }
        public event Action<LevelPlayAdInfo> OnAdLoaded { add { } remove { } }
        public event Action<LevelPlayAdError> OnAdLoadFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdDisplayed { add { } remove { } }
        public event Action<LevelPlayAdInfo, LevelPlayAdError> OnAdDisplayFailed { add { } remove { } }
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
        public event Action<LevelPlayAdInfo, LevelPlayAdError> OnAdDisplayFailed { add { } remove { } }
        public event Action<LevelPlayAdInfo> OnAdClosed { add { } remove { } }
        public void LoadAd() { }
        public void ShowAd(string placementName = null) { }
        public bool IsAdReady() => false;
    }
}
namespace Unity.Services.CloudSave.Models
{
    public class FieldFilter
    {
        public enum OpOptions { EQ = 1, NE = 2, LT = 3, LE = 4, GT = 5, GE = 6 }
        public FieldFilter(string key, object value, OpOptions op, bool asc) { }
    }
    public class Query
    {
        public Query(List<FieldFilter> fields, HashSet<string> returnKeys = default, int offset = default, int limit = default) { }
    }
    public interface IDeserializable { T GetAs<T>(); }
    public class Item { public string Key { get; } public IDeserializable Value { get; } }
    public class EntityData { public string Id { get; } public List<Item> Data { get; } }
}

namespace Unity.Services.CloudSave.Models.Data.Player
{
    public abstract class WriteAccessClassOptions { }
    public class PublicWriteAccessClassOptions : WriteAccessClassOptions { }
    public class SaveOptions { public SaveOptions() { } public SaveOptions(WriteAccessClassOptions accessClassOptions) { } }
    public class QueryOptions { }
}

namespace Unity.Services.CloudSave
{
    public interface IPlayerDataService
    {
        System.Threading.Tasks.Task<Dictionary<string, string>> SaveAsync(IDictionary<string, object> data, Models.Data.Player.SaveOptions options);
        System.Threading.Tasks.Task<List<Models.EntityData>> QueryAsync(Models.Query query, Models.Data.Player.QueryOptions options);
    }
    public interface IDataService { IPlayerDataService Player { get; } }
    public interface ICloudSaveService { IDataService Data { get; } }
    public static class CloudSaveService { public static ICloudSaveService Instance => null; }
}
namespace Unity.Collections
{
    public struct DataStreamReader { public int Length => 0; public byte ReadByte() => 0; }
    public struct DataStreamWriter { public bool WriteByte(byte value) => true; }
}

namespace Unity.Networking.Transport
{
    public struct NetworkEndpoint { public static NetworkEndpoint AnyIpv4 => default; }
    public struct NetworkSettings { }
    public struct NetworkPipeline { }
    public struct ReliableSequencedPipelineStage { }
    public struct UnreliableSequencedPipelineStage { }
    public static class NetworkEvent { public enum Type : short { Empty = 0, Data, Connect, Disconnect } }
    public struct NetworkConnection
    {
        public bool IsCreated => false;
        public static bool operator ==(NetworkConnection a, NetworkConnection b) => true;
        public static bool operator !=(NetworkConnection a, NetworkConnection b) => false;
        public override bool Equals(object o) => o is NetworkConnection;
        public override int GetHashCode() => 0;
    }
    public interface INetworkInterface { }
    public struct WebSocketNetworkInterface : INetworkInterface { }
    public struct NetworkDriver : IDisposable
    {
        public static NetworkDriver Create(NetworkSettings settings) => default;
        public static NetworkDriver Create<N>(N networkInterface, NetworkSettings settings) where N : unmanaged, INetworkInterface => default;
        public bool IsCreated => false;
        public NetworkPipeline CreatePipeline(params Type[] stages) => default;
        public int Bind(NetworkEndpoint endpoint) => 0;
        public int Listen() => 0;
        public NetworkConnection Connect(NetworkEndpoint endpoint) => default;
        public NetworkConnection Accept() => default;
        public int Disconnect(NetworkConnection c) => 0;
        public Unity.Jobs.JobHandle ScheduleUpdate(Unity.Jobs.JobHandle dependency = default) => default;
        public NetworkEvent.Type PopEventForConnection(NetworkConnection c, out Unity.Collections.DataStreamReader reader) { reader = default; return NetworkEvent.Type.Empty; }
        public int BeginSend(NetworkPipeline pipe, NetworkConnection c, out Unity.Collections.DataStreamWriter writer, int requiredPayloadSize = 0) { writer = default; return 0; }
        public int EndSend(Unity.Collections.DataStreamWriter writer) => 0;
        public void Dispose() { }
    }
}

namespace Unity.Networking.Transport.Relay
{
    public struct RelayServerData { public NetworkEndpoint Endpoint; public bool IsWebSocket; }
    /// <summary>État de la liaison avec le relais (Unity Transport 2.x).</summary>
    public enum RelayConnectionStatus { NotEstablished = 0, Established, AllocationInvalid }
    public static class RelayDriverExtensions
    {
        public static RelayConnectionStatus GetRelayConnectionStatus(this NetworkDriver driver) => RelayConnectionStatus.NotEstablished;
    }
    public static class RelayParameterExtensions
    {
        public static ref NetworkSettings WithRelayParameters(ref this NetworkSettings settings, ref RelayServerData serverData, int relayConnectionTimeMS = 3000) => ref settings;
    }
}

namespace Unity.Services.Multiplayer
{
    public enum NetworkRole { Client = 0, Server = 1, Host = 2 }
    public enum RelayProtocol : byte { Unknown = 0, Default = 1, UDP = 2, DTLS = 3, WSS = 4 }
    public class RelayNetworkOptions
    {
        public RelayNetworkOptions(RelayProtocol protocol) { }
        public RelayNetworkOptions(RelayProtocol protocol, string region) { }
    }
    public class NetworkConfiguration
    {
        public Unity.Networking.Transport.Relay.RelayServerData RelayServerData { get; }
        public NetworkRole Role { get; }
    }
    public interface INetworkHandler
    {
        System.Threading.Tasks.Task StartAsync(NetworkConfiguration configuration);
        System.Threading.Tasks.Task StopAsync();
    }
    public abstract class BaseSessionOptions { }
    public class SessionOptions : BaseSessionOptions { public bool IsPrivate { get; set; } public int MaxPlayers { get; set; } public string Name { get; set; } }
    public class JoinSessionOptions : BaseSessionOptions { }
    public static class SessionOptionsExtensions
    {
        public static T WithRelayNetwork<T>(this T options, string region = default) where T : SessionOptions => options;
        public static T WithRelayNetwork<T>(this T options, RelayNetworkOptions networkOptions) where T : SessionOptions => options;
        public static T WithNetworkHandler<T>(this T options, INetworkHandler networkHandler) where T : BaseSessionOptions => options;
    }
    public interface ISession
    {
        event Action Deleted;
        event Action<string> PlayerHasLeft;
        event Action RemovedFromSession;
        string Code { get; }
        bool IsHost { get; }
        IHostSession AsHost();
        System.Threading.Tasks.Task LeaveAsync();
    }
    public interface IHostSession : ISession { System.Threading.Tasks.Task DeleteAsync(); }
    public interface IMultiplayerService
    {
        System.Threading.Tasks.Task<IHostSession> CreateSessionAsync(SessionOptions sessionOptions);
        System.Threading.Tasks.Task<ISession> JoinSessionByCodeAsync(string sessionCode, JoinSessionOptions sessionOptions = default);
    }
    public static class MultiplayerService { public static IMultiplayerService Instance { get; set; } }
}
#endif
