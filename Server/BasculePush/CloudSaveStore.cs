using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace BasculePush
{
    /// <summary>
    /// Cloud Save vu du module. Toujours avec le jeton du SERVICE (pas celui du joueur qui appelle) : il peut lire les
    /// données privées du destinataire et écrire les données protégées, ce qu'aucun joueur ne peut faire.
    /// </summary>
    public sealed class CloudSaveStore : IPushStore
    {
        readonly IExecutionContext _context;
        readonly IGameApiClient _api;

        public CloudSaveStore(IExecutionContext context, IGameApiClient api)
        {
            _context = context;
            _api = api;
        }

        public async Task<Dictionary<string, StoredItem>> GetPublicAsync(string playerId, params string[] keys)
        {
            try { return ToItems((await _api.CloudSaveData.GetPublicItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (Is(e, HttpStatusCode.NotFound)) { return new Dictionary<string, StoredItem>(); }
        }

        public async Task<Dictionary<string, StoredItem>> GetDefaultAsync(string playerId, params string[] keys)
        {
            try { return ToItems((await _api.CloudSaveData.GetItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (Is(e, HttpStatusCode.NotFound)) { return new Dictionary<string, StoredItem>(); }
        }

        public async Task<Dictionary<string, StoredItem>> GetProtectedAsync(string playerId, params string[] keys)
        {
            try { return ToItems((await _api.CloudSaveData.GetProtectedItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (Is(e, HttpStatusCode.NotFound)) { return new Dictionary<string, StoredItem>(); }
        }

        public async Task<bool> TrySetProtectedAsync(string playerId, string key, string value, string writeLock)
        {
            try
            {
                if (!string.IsNullOrEmpty(writeLock))
                {
                    // Avec le verrou lu : Cloud Save refuse (409) si la valeur a changé depuis la lecture.
                    await _api.CloudSaveData.SetProtectedItemAsync(_context, _context.ServiceToken, _context.ProjectId, playerId,
                        new SetItemBody(key, value ?? "", writeLock));
                    return true;
                }
                // Valeur encore absente : Cloud Save n'a pas de « créer seulement si absent ». On écrit, puis on relit : si
                // le verrou relu n'est plus celui de notre écriture, un autre appel a écrit juste après et c'est lui qui
                // compte. Ne concerne que le tout premier envoi d'un joueur (ensuite, le verrou ci-dessus est strict).
                var written = await _api.CloudSaveData.SetProtectedItemAsync(_context, _context.ServiceToken, _context.ProjectId, playerId,
                    new SetItemBody(key, value ?? "", null));
                string mine = written?.Data?.WriteLock;
                var now = await GetProtectedAsync(playerId, key);
                return !string.IsNullOrEmpty(mine) && now.TryGetValue(key, out var item) && item.WriteLock == mine;
            }
            catch (ApiException e) when (Is(e, HttpStatusCode.Conflict)) { return false; }
        }

        public Task SetProtectedAsync(string playerId, IDictionary<string, string> values) =>
            _api.CloudSaveData.SetProtectedItemBatchAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, Batch(values));

        public Task SetDefaultAsync(string playerId, IDictionary<string, string> values) =>
            _api.CloudSaveData.SetItemBatchAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, Batch(values));

        public async Task<StoredItem> GetCustomAsync(string customId, string key)
        {
            try
            {
                var items = ToItems((await _api.CloudSaveData.GetCustomItemsAsync(_context, _context.ServiceToken, _context.ProjectId, customId, new List<string> { key })).Data);
                return items.TryGetValue(key, out var item) ? item : null;
            }
            catch (ApiException e) when (Is(e, HttpStatusCode.NotFound)) { return null; }
        }

        public Task SetCustomAsync(string customId, string key, string value) =>
            _api.CloudSaveData.SetCustomItemAsync(_context, _context.ServiceToken, _context.ProjectId, customId, new SetItemBody(key, value ?? "", null));

        static SetItemBatchBody Batch(IDictionary<string, string> values) =>
            new SetItemBatchBody(values.Select(kv => new SetItemBody(kv.Key, kv.Value ?? "", null)).ToList());

        /// <summary>404 : joueur sans aucune donnée (ou compte supprimé), comme des données vides. 409 : verrou d'écriture périmé.</summary>
        static bool Is(ApiException e, HttpStatusCode code) => e.Response != null && e.Response.StatusCode == code;

        /// <summary>Valeurs en texte : le jeu n'écrit que du texte, mais un nombre ou un objet ne fait pas planter.</summary>
        internal static Dictionary<string, StoredItem> ToItems(GetItemsResponse data)
        {
            var result = new Dictionary<string, StoredItem>();
            if (data?.Results == null) return result;
            foreach (var item in data.Results)
            {
                if (item?.Key == null || item.Value == null) continue;
                DateTime? modified = item.Modified?.Date;
                result[item.Key] = new StoredItem
                {
                    Value = item.Value is string s ? s : Convert.ToString(item.Value, CultureInfo.InvariantCulture),
                    WriteLock = item.WriteLock,
                    Modified = modified.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(modified.Value.Kind == DateTimeKind.Local ? modified.Value.ToUniversalTime() : modified.Value, DateTimeKind.Utc))
                        : (DateTimeOffset?)null,
                };
            }
            return result;
        }
    }

    /// <summary>Les trois secrets d'Apple, lus dans le Secret Manager de Unity à chaque envoi (une clé changée sert tout de suite).</summary>
    public sealed class SecretManagerCredentials : IApnsCredentialsSource
    {
        public const string KeySecret = "APNS_KEY", KeyIdSecret = "APNS_KEY_ID", TeamIdSecret = "APNS_TEAM_ID";

        readonly IExecutionContext _context;
        readonly IGameApiClient _api;

        public SecretManagerCredentials(IExecutionContext context, IGameApiClient api)
        {
            _context = context;
            _api = api;
        }

        public async Task<ApnsCredentials> GetAsync()
        {
            var c = new ApnsCredentials
            {
                PrivateKey = (await _api.SecretManager.GetSecret(_context, KeySecret))?.Value,
                KeyId = (await _api.SecretManager.GetSecret(_context, KeyIdSecret))?.Value,
                TeamId = (await _api.SecretManager.GetSecret(_context, TeamIdSecret))?.Value,
            };
            return c.IsComplete ? c : null;
        }
    }
}
