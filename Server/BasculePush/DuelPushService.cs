using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bascule.Core;
using Newtonsoft.Json;

namespace BasculePush
{
    /// <summary>Ce que renvoie la fonction (et ce qui est gardé dans push_status).</summary>
    public static class PushStatus
    {
        public const string Sent = "sent";            // Apple a accepté la notification
        public const string Invalid = "invalid";      // identifiant de joueur abîmé, ou s'inviter soi-même
        public const string Stale = "stale";          // pas d'invitation récente vers ce joueur dans les données publiques
        public const string Off = "off";              // le destinataire n'a pas de jeton (rappels coupés, Android, ancienne version)
        public const string NotFriend = "notfriend";  // le destinataire n'a pas (ou plus) l'invitant dans ses amis confirmés
        public const string Quiet = "quiet";          // nuit chez le destinataire
        public const string Limit = "limit";          // trop d'envois de l'invitant, ou trop de réceptions du destinataire
        public const string NoKey = "nokey";          // clé Apple absente ou abîmée dans le Secret Manager
        public const string Gone = "gone";            // jeton périmé (app supprimée, rappels coupés) : effacé
        public const string Busy = "busy";            // Apple demande d'attendre (429, 5xx)
        public const string Error = "error";          // Cloud Save ou réseau indisponible
        public const string Apns = "apns";            // autre refus d'Apple : « apns:403:InvalidProviderToken »…
    }

    /// <summary>Données Cloud Save lues et écrites par le module (implémentée par CloudSaveStore, et en mémoire dans les tests).</summary>
    public interface IPushStore
    {
        /// <summary>Données publiques d'un joueur (valeurs en texte ; clé absente = pas dans le résultat).</summary>
        Task<Dictionary<string, string>> GetPublicAsync(string playerId, params string[] keys);
        /// <summary>Données par défaut (privées : le joueur et le serveur seulement).</summary>
        Task<Dictionary<string, string>> GetDefaultAsync(string playerId, params string[] keys);
        /// <summary>Données protégées (écrites par le serveur seulement).</summary>
        Task<Dictionary<string, string>> GetProtectedAsync(string playerId, params string[] keys);
        Task SetProtectedAsync(string playerId, IDictionary<string, string> values);
        Task SetDefaultAsync(string playerId, IDictionary<string, string> values);
    }

    /// <summary>Clé Apple (Secret Manager de Unity).</summary>
    public interface IApnsCredentialsSource
    {
        /// <summary>null si un des trois secrets manque.</summary>
        Task<ApnsCredentials> GetAsync();
    }

    /// <summary>Toutes les vérifications, puis l'envoi. Sans dépendance à Unity : testé dans BasculePush.Tests.</summary>
    public sealed class DuelPushService
    {
        /// <summary>Identifiant de l'app chez Apple (« topic » de la notification).</summary>
        public const string BundleId = "fr.orpheex.bascule";
        public const string FallbackName = "Un ami";

        readonly IPushStore _store;
        readonly IApnsCredentialsSource _credentials;
        readonly IApnsSender _apns;

        public DuelPushService(IPushStore store, IApnsCredentialsSource credentials, IApnsSender apns)
        {
            _store = store;
            _credentials = credentials;
            _apns = apns;
        }

        public async Task<string> SendAsync(string inviterId, string targetId, long nowUnix)
        {
            if (!DuelPush.IsPlayerId(inviterId) || !DuelPush.IsPlayerId(targetId) || inviterId == targetId) return PushStatus.Invalid;
            string status;
            try { status = await CheckAndSendAsync(inviterId, targetId, nowUnix); }
            catch (Exception) { status = PushStatus.Error; }
            // Diagnostic : dernier résultat, lisible dans le tableau de bord (Joueurs → Cloud Save → données protégées).
            // Rien n'est écrit pour le cas courant sans intérêt (destinataire sans notifications).
            if (status != PushStatus.Off)
            {
                try
                {
                    await _store.SetProtectedAsync(inviterId, new Dictionary<string, string>
                    {
                        [DuelPush.StatusKey] = DateTimeOffset.FromUnixTimeSeconds(nowUnix).ToString("yyyy-MM-dd HH:mm:ss") + "Z " + status,
                    });
                }
                catch (Exception) { /* le diagnostic ne doit jamais faire échouer l'appel */ }
            }
            return status;
        }

        async Task<string> CheckAndSendAsync(string inviterId, string targetId, long now)
        {
            // 1. Une vraie invitation, récente, vers ce joueur (écrite par le jeu de l'invitant juste avant l'appel).
            var invite = await _store.GetPublicAsync(inviterId, DuelPush.DuelToKey, DuelPush.DuelCodeKey, DuelPush.DuelAtKey, DuelPush.DuelNameKey);
            if (!DuelPush.IsFreshInvite(Get(invite, DuelPush.DuelToKey), Get(invite, DuelPush.DuelCodeKey), Get(invite, DuelPush.DuelAtKey), targetId, now))
                return PushStatus.Stale;

            // 2. Le destinataire veut des notifications, et a accepté l'invitant comme ami.
            var target = await _store.GetDefaultAsync(targetId, DuelPush.TokenKey, DuelPush.FriendsKey, DuelPush.TzKey);
            string token = Get(target, DuelPush.TokenKey);
            if (!DuelPush.IsDeviceToken(token)) return PushStatus.Off;
            if (!DuelPush.IsFriend(Get(target, DuelPush.FriendsKey), inviterId)) return PushStatus.NotFriend;

            // 3. Jamais la nuit chez lui.
            if (DuelPush.IsQuietHours(now, DuelPush.ParseTz(Get(target, DuelPush.TzKey)))) return PushStatus.Quiet;

            // 4. Cadence : l'invitant n'insiste pas, le destinataire n'est pas inondé. Les journaux sont écrits avant
            // l'envoi : un envoi raté compte aussi.
            var sent = await _store.GetProtectedAsync(inviterId, DuelPush.SentLogKey);
            if (!DuelPush.AllowSend(Get(sent, DuelPush.SentLogKey), targetId, now, out string newSent)) return PushStatus.Limit;
            var received = await _store.GetProtectedAsync(targetId, DuelPush.ReceivedLogKey);
            if (!DuelPush.AllowReceive(Get(received, DuelPush.ReceivedLogKey), now, out string newReceived)) return PushStatus.Limit;
            await _store.SetProtectedAsync(inviterId, new Dictionary<string, string> { [DuelPush.SentLogKey] = newSent });
            await _store.SetProtectedAsync(targetId, new Dictionary<string, string> { [DuelPush.ReceivedLogKey] = newReceived });

            // 5. Envoi chez Apple.
            ApnsCredentials credentials;
            try { credentials = await _credentials.GetAsync(); }
            catch (Exception) { return PushStatus.NoKey; }
            if (credentials == null || !credentials.IsComplete) return PushStatus.NoKey;
            string name = DuelPush.DisplayName(Get(invite, DuelPush.DuelNameKey), FallbackName);
            var message = new ApnsMessage
            {
                DeviceToken = token,
                Topic = BundleId,
                PayloadJson = Payload(name),
                CollapseId = "duel-" + inviterId,
                ExpirationUnix = now + DuelPush.ExpirationSeconds,
            };
            ApnsResult result;
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    result = await _apns.SendAsync(credentials, message, cts.Token);
            }
            catch (ApnsKeyException) { return PushStatus.NoKey; }
            catch (Exception) { return PushStatus.Error; }

            if (result.StatusCode == 200) return PushStatus.Sent;
            if (result.IsDeadToken)
            {
                // Jeton périmé : effacé pour ne plus jamais l'essayer (le jeu en republie un neuf s'il revient).
                try { await _store.SetDefaultAsync(targetId, new Dictionary<string, string> { [DuelPush.TokenKey] = "" }); }
                catch (Exception) { /* sera retenté au prochain envoi */ }
                return PushStatus.Gone;
            }
            if (result.StatusCode == 429 || result.StatusCode >= 500) return PushStatus.Busy;
            return PushStatus.Apns + ":" + result.StatusCode + ":" + (result.Reason ?? "");
        }

        /// <summary>Contenu de la notification (format d'Apple), en français comme le jeu (Loc.FrenchOnly).</summary>
        public static string Payload(string inviterName)
        {
            var body = new Dictionary<string, object>
            {
                ["aps"] = new Dictionary<string, object>
                {
                    ["alert"] = new Dictionary<string, object>
                    {
                        ["title"] = "Duel !",
                        ["body"] = inviterName + " te défie en duel. Viens vite !",
                    },
                    ["sound"] = "default",
                    ["thread-id"] = "duel",
                },
            };
            return JsonConvert.SerializeObject(body);
        }

        static string Get(Dictionary<string, string> data, string key) =>
            data != null && data.TryGetValue(key, out var v) ? v : null;
    }
}
