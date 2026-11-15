using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BasculePush;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Api;
using Unity.Services.CloudSave.Model;

namespace BasculePush.Tests
{
    /// <summary>
    /// Cloud Save en mémoire : (accès, joueur, clé) → valeur, avec un verrou d'écriture qui change à chaque écriture (comme
    /// le vrai). Interleave : chaque lecture rend la main, pour que des appels simultanés se croisent vraiment.
    /// </summary>
    sealed class MemoryStore : IPushStore
    {
        readonly object _lock = new object();
        public readonly Dictionary<(string access, string player, string key), StoredItem> Data = new Dictionary<(string, string, string), StoredItem>();
        int _version;
        public int Writes;
        public bool Fail, Interleave;

        public void Put(string access, string player, string key, string value, long? modifiedUnix = null)
        {
            lock (_lock)
                Data[(access, player, key)] = new StoredItem
                {
                    Value = value,
                    WriteLock = "v" + (++_version),
                    Modified = modifiedUnix.HasValue ? DateTimeOffset.FromUnixTimeSeconds(modifiedUnix.Value) : DateTimeOffset.UtcNow,
                };
        }

        public string Value(string access, string player, string key)
        {
            lock (_lock) return Data.TryGetValue((access, player, key), out var v) ? v.Value : null;
        }

        async Task<Dictionary<string, StoredItem>> Read(string access, string player, string[] keys)
        {
            if (Interleave) await Task.Yield();
            if (Fail) throw new InvalidOperationException("Cloud Save indisponible");
            var r = new Dictionary<string, StoredItem>();
            lock (_lock)
                foreach (var k in keys)
                    if (Data.TryGetValue((access, player, k), out var v))
                        r[k] = new StoredItem { Value = v.Value, WriteLock = v.WriteLock, Modified = v.Modified };
            return r;
        }

        Task Write(string access, string player, IDictionary<string, string> values)
        {
            if (Fail) throw new InvalidOperationException("Cloud Save indisponible");
            lock (_lock)
            {
                Writes++;
                foreach (var kv in values) Put(access, player, kv.Key, kv.Value);
            }
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, StoredItem>> GetPublicAsync(string playerId, params string[] keys) => Read("public", playerId, keys);
        public Task<Dictionary<string, StoredItem>> GetDefaultAsync(string playerId, params string[] keys) => Read("default", playerId, keys);
        public Task<Dictionary<string, StoredItem>> GetProtectedAsync(string playerId, params string[] keys) => Read("protected", playerId, keys);
        public Task SetProtectedAsync(string playerId, IDictionary<string, string> values) => Write("protected", playerId, values);
        public Task SetDefaultAsync(string playerId, IDictionary<string, string> values) => Write("default", playerId, values);

        /// <summary>Appelé juste après une écriture sans verrou (pour simuler un autre appel qui écrit à ce moment-là).</summary>
        public Action<string, string> AfterUnlockedWrite;

        public async Task<bool> TrySetProtectedAsync(string playerId, string key, string value, string writeLock)
        {
            if (Interleave) await Task.Yield();
            if (Fail) throw new InvalidOperationException("Cloud Save indisponible");
            string mine;
            lock (_lock)
            {
                bool exists = Data.TryGetValue(("protected", playerId, key), out var current);
                // Comme Cloud Save : avec un verrou, il doit être le verrou actuel (sinon 409).
                if (writeLock != null && (!exists || current.WriteLock != writeLock)) return false;
                Writes++;
                Put("protected", playerId, key, value);
                if (writeLock != null) return true;
                mine = Data[("protected", playerId, key)].WriteLock;
            }
            // Sans verrou : écrite puis relue, comme CloudSaveStore.
            AfterUnlockedWrite?.Invoke(playerId, key);
            if (Interleave) await Task.Yield();
            lock (_lock) return Data.TryGetValue(("protected", playerId, key), out var now) && now.WriteLock == mine;
        }

        public async Task<StoredItem> GetCustomAsync(string customId, string key)
        {
            var r = await Read("custom", customId, new[] { key });
            return r.TryGetValue(key, out var v) ? v : null;
        }

        public Task SetCustomAsync(string customId, string key, string value) =>
            Write("custom", customId, new Dictionary<string, string> { [key] = value });
    }

    sealed class FixedCredentials : IApnsCredentialsSource
    {
        public ApnsCredentials Value = new ApnsCredentials { PrivateKey = "clé", KeyId = "ABC123DEFG", TeamId = "TEAM123456" };
        public Task<ApnsCredentials> GetAsync() => Task.FromResult(Value);
    }

    sealed class RecordingSender : IApnsSender
    {
        public readonly System.Collections.Concurrent.ConcurrentQueue<ApnsMessage> Queue = new System.Collections.Concurrent.ConcurrentQueue<ApnsMessage>();
        public List<ApnsMessage> Sent => Queue.ToList();
        public Func<ApnsMessage, ApnsResult> Reply = _ => new ApnsResult { StatusCode = 200 };
        public Exception Throw;

        public Task<ApnsResult> SendAsync(ApnsCredentials credentials, ApnsMessage message, CancellationToken cancellationToken)
        {
            if (Throw != null) throw Throw;
            Queue.Enqueue(message);
            return Task.FromResult(Reply(message));
        }
    }

    /// <summary>Faux réseau pour ApnsSender : garde chaque requête (avec son corps) et répond ce qu'on veut.</summary>
    sealed class FakeHandler : HttpMessageHandler
    {
        public readonly List<(HttpRequestMessage request, string body)> Requests = new List<(HttpRequestMessage, string)>();
        public Func<HttpRequestMessage, int, HttpResponseMessage> Reply = (_, __) => new HttpResponseMessage(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));
            return Reply(request, Requests.Count);
        }
    }

    sealed class FakeContext : IExecutionContext
    {
        public string ProjectId { get; set; } = "projet-test";
        public string PlayerId { get; set; } = "Inviter1";
        public string EnvironmentId { get; set; } = "env";
        public string EnvironmentName { get; set; } = "production";
        public string AccessToken { get; set; } = "jeton-joueur";
        public string UserId { get; set; }
        public string Issuer { get; set; }
        public string ServiceToken { get; set; } = "jeton-service";
        public string AnalyticsUserId { get; set; }
        public string UnityInstallationId { get; set; }
        public string CorrelationId { get; set; }
        public string ScopeId { get; set; }
        public int CallDepth { get; set; }
        public ISession Session { get; set; }
    }

    /// <summary>
    /// Faux ICloudSaveDataApi (et IGameApiClient) construit avec DispatchProxy : chaque appel est noté (nom de la méthode
    /// et arguments) et les lectures renvoient <see cref="Items"/>.
    /// </summary>
    public class CloudSaveProxy : DispatchProxy
    {
        public List<(string method, object[] args)> Calls;
        public List<Item> Items;
        public Exception Throw;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            Calls.Add((targetMethod.Name, args));
            var returnType = targetMethod.ReturnType;
            if (Throw != null)
            {
                // Comme le vrai client : l'exception sort de la tâche.
                var failed = typeof(Task).GetMethod(nameof(Task.FromException), 1, new[] { typeof(Exception) })
                    .MakeGenericMethod(returnType.GetGenericArguments()[0]);
                return failed.Invoke(null, new object[] { Throw });
            }
            var dataType = returnType.GetGenericArguments()[0].GetGenericArguments()[0];
            object data = dataType == typeof(GetItemsResponse) ? new GetItemsResponse(Items ?? new List<Item>(), new GetItemsResponseLinks(""))
                : dataType == typeof(SetItemBatchResponse) ? new SetItemBatchResponse(new List<SetItemBatchResponseResultsInner>())
                : dataType == typeof(SetItemResponse) ? new SetItemResponse("nouveau-verrou")
                : null;
            var response = Activator.CreateInstance(typeof(ApiResponse<>).MakeGenericType(dataType));
            Helpers.SetPrivate(response, "Data", data);
            Helpers.SetPrivate(response, "StatusCode", HttpStatusCode.OK);
            return typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(response.GetType()).Invoke(null, new[] { response });
        }

        public static ICloudSaveDataApi Create(List<(string, object[])> calls, List<Item> items, Exception error = null)
        {
            var proxy = Create<ICloudSaveDataApi, CloudSaveProxy>();
            var p = (CloudSaveProxy)(object)proxy;
            p.Calls = calls;
            p.Items = items;
            p.Throw = error;
            return proxy;
        }
    }

    public class GameApiProxy : DispatchProxy
    {
        public ICloudSaveDataApi CloudSave;
        public ISecretClient Secrets;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == "get_CloudSaveData") return CloudSave;
            if (targetMethod.Name == "get_SecretManager") return Secrets;
            throw new NotSupportedException(targetMethod.Name);
        }

        public static IGameApiClient Create(ICloudSaveDataApi cloudSave, ISecretClient secrets = null)
        {
            var proxy = Create<IGameApiClient, GameApiProxy>();
            var p = (GameApiProxy)(object)proxy;
            p.CloudSave = cloudSave;
            p.Secrets = secrets;
            return proxy;
        }
    }

    sealed class FakeSecrets : ISecretClient
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public Task<Secret> GetSecret(IExecutionContext executionContext, string secretKey) =>
            Task.FromResult(Values.TryGetValue(secretKey, out var v) ? new Secret(v) : null);
    }

    static class Helpers
    {
        /// <summary>Valeur rangée dans Cloud Save, comme le serveur la renvoie.</summary>
        public static Item Item(string key, object value, DateTime? modified = null, string writeLock = null) =>
            new Item(key, value, writeLock ?? "verrou-" + key, new Unity.Services.CloudSave.Model.ModifiedMetadata(modified ?? DateTime.UtcNow), new Unity.Services.CloudSave.Model.ModifiedMetadata(modified ?? DateTime.UtcNow));

        /// <summary>Les réponses du client Unity n'ont que des accesseurs privés : on les remplit par réflexion.</summary>
        public static void SetPrivate(object target, string property, object value)
        {
            var type = target.GetType();
            var prop = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = prop?.GetSetMethod(true);
            if (setter != null) { setter.Invoke(target, new[] { value }); return; }
            var field = type.GetField("<" + property + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(f => f.Name.IndexOf(property, StringComparison.OrdinalIgnoreCase) >= 0);
            if (field == null) throw new MissingMemberException(type.Name, property);
            field.SetValue(target, value);
        }

        public static ApiException NotFound() => Http(HttpStatusCode.NotFound);
        public static ApiException Conflict() => Http(HttpStatusCode.Conflict);

        static ApiException Http(HttpStatusCode code)
        {
            var response = new ApiResponse<object>();
            SetPrivate(response, "StatusCode", code);
            return new ApiException(ApiExceptionType.Http, code.ToString(), response);
        }
    }
}
