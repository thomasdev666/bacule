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

        public async Task<Dictionary<string, string>> GetPublicAsync(string playerId, params string[] keys)
        {
            try { return ToStrings((await _api.CloudSaveData.GetPublicItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (IsNotFound(e)) { return new Dictionary<string, string>(); }
        }

        public async Task<Dictionary<string, string>> GetDefaultAsync(string playerId, params string[] keys)
        {
            try { return ToStrings((await _api.CloudSaveData.GetItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (IsNotFound(e)) { return new Dictionary<string, string>(); }
        }

        public async Task<Dictionary<string, string>> GetProtectedAsync(string playerId, params string[] keys)
        {
            try { return ToStrings((await _api.CloudSaveData.GetProtectedItemsAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, keys.ToList())).Data); }
            catch (ApiException e) when (IsNotFound(e)) { return new Dictionary<string, string>(); }
        }

        public Task SetProtectedAsync(string playerId, IDictionary<string, string> values) =>
            _api.CloudSaveData.SetProtectedItemBatchAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, Batch(values));

        public Task SetDefaultAsync(string playerId, IDictionary<string, string> values) =>
            _api.CloudSaveData.SetItemBatchAsync(_context, _context.ServiceToken, _context.ProjectId, playerId, Batch(values));

        static SetItemBatchBody Batch(IDictionary<string, string> values) =>
            new SetItemBatchBody(values.Select(kv => new SetItemBody(kv.Key, kv.Value ?? "")).ToList());

        /// <summary>Joueur sans aucune donnée (ou compte supprimé) : comme des données vides.</summary>
        static bool IsNotFound(ApiException e) => e.Response != null && e.Response.StatusCode == HttpStatusCode.NotFound;

        /// <summary>Valeurs en texte : le jeu n'écrit que du texte, mais un nombre ou un objet ne fait pas planter.</summary>
        internal static Dictionary<string, string> ToStrings(GetItemsResponse data)
        {
            var result = new Dictionary<string, string>();
            if (data?.Results == null) return result;
            foreach (var item in data.Results)
            {
                if (item?.Key == null || item.Value == null) continue;
                result[item.Key] = item.Value is string s ? s : Convert.ToString(item.Value, CultureInfo.InvariantCulture);
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
