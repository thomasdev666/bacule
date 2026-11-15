using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bascule.Core;
using Newtonsoft.Json;

namespace BasculePush
{
    /// <summary>Ce que renvoient les fonctions (et ce qui est gardé dans push_status).</summary>
    public static class PushStatus
    {
        public const string Sent = "sent";            // Apple a accepté la notification
        public const string Invalid = "invalid";      // identifiant de joueur abîmé, ou s'inviter soi-même
        public const string Stale = "stale";          // pas d'invitation récente vers ce joueur dans les données publiques
        public const string NotFriend = "notfriend";  // le destinataire n'a pas (ou plus) l'invitant dans ses amis
        public const string Off = "off";              // le destinataire n'a pas de jeton (rappels coupés, Android, ancienne version)
        public const string Quiet = "quiet";          // nuit chez le destinataire
        public const string Limit = "limit";          // trop d'envois de l'invitant, ou trop de réceptions du destinataire
        public const string NoKey = "nokey";          // clé Apple absente ou abîmée dans le Secret Manager
        public const string Gone = "gone";            // jeton périmé (app supprimée, rappels coupés) : effacé
        public const string Busy = "busy";            // Apple demande d'attendre (429, 5xx)
        public const string Error = "error";          // Cloud Save indisponible
        public const string ApnsNet = "apnsnet";      // Apple injoignable : « apnsnet:HttpRequestException »…
        public const string Apns = "apns";            // autre refus d'Apple : « apns:403:InvalidProviderToken »…
        public const string Ok = "ok";                // auto-test : clé, équipe, app et réseau bons
        public const string Wait = "wait";            // auto-test déjà fait il y a moins de 5 minutes
    }

    /// <summary>Une valeur Cloud Save, avec son verrou d'écriture et l'heure (du serveur) de sa dernière écriture.</summary>
    public sealed class StoredItem
    {
        public string Value;
        public string WriteLock;
        public DateTimeOffset? Modified;
    }

    /// <summary>Données Cloud Save lues et écrites par le module (implémentée par CloudSaveStore, et en mémoire dans les tests).</summary>
    public interface IPushStore
    {
        /// <summary>Données publiques d'un joueur (clé absente = pas dans le résultat).</summary>
        Task<Dictionary<string, StoredItem>> GetPublicAsync(string playerId, params string[] keys);
        /// <summary>Données par défaut (privées : le joueur et le serveur seulement).</summary>
        Task<Dictionary<string, StoredItem>> GetDefaultAsync(string playerId, params string[] keys);
        /// <summary>Données protégées (écrites par le serveur seulement).</summary>
        Task<Dictionary<string, StoredItem>> GetProtectedAsync(string playerId, params string[] keys);
        /// <summary>
        /// Écrit une valeur protégée seulement si elle n'a pas changé depuis la lecture (<paramref name="writeLock"/> lu avec
        /// elle). Renvoie false si quelqu'un l'a modifiée entre-temps. Valeur encore absente (null) : écrite puis relue, false
        /// si une autre écriture est passée juste après.
        /// </summary>
        Task<bool> TrySetProtectedAsync(string playerId, string key, string value, string writeLock);
        Task SetProtectedAsync(string playerId, IDictionary<string, string> values);
        Task SetDefaultAsync(string playerId, IDictionary<string, string> values);
        /// <summary>Données du jeu qui n'appartiennent à aucun joueur (auto-test).</summary>
        Task<StoredItem> GetCustomAsync(string customId, string key);
        Task SetCustomAsync(string customId, string key, string value);
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
        /// <summary>Données du jeu (pas d'un joueur) où l'auto-test note sa dernière heure.</summary>
        public const string GameDataId = "bascule_push", SelfTestKey = "selftest_at";
        /// <summary>Jeton factice de l'auto-test : Apple le refuse (BadDeviceToken) seulement si tout le reste est bon.</summary>
        public static readonly string FakeToken = new string('0', 64);

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
            // 1. Une vraie invitation, récente, vers ce joueur (écrite par le jeu de l'invitant juste avant l'appel). L'heure
            // est celle du serveur (dernière écriture de duel_to) : l'horloge du téléphone de l'invitant peut être fausse.
            var invite = await _store.GetPublicAsync(inviterId, DuelPush.DuelToKey, DuelPush.DuelCodeKey, DuelPush.DuelAtKey, DuelPush.DuelNameKey);
            invite.TryGetValue(DuelPush.DuelToKey, out var to);
            long invitedAt = to?.Modified?.ToUnixTimeSeconds() ?? DuelPush.ParseUnix(Value(invite, DuelPush.DuelAtKey));
            if (!DuelPush.IsFreshInvite(to?.Value, Value(invite, DuelPush.DuelCodeKey), invitedAt, targetId, now))
                return PushStatus.Stale;

            // 2. Le destinataire a accepté l'invitant comme ami (vérifié AVANT le jeton : un inconnu n'apprend même pas si ce
            // joueur a les notifications), puis il veut des notifications.
            var target = await _store.GetDefaultAsync(targetId, DuelPush.TokenKey, DuelPush.FriendsKey, DuelPush.TzKey);
            if (!DuelPush.IsFriend(Value(target, DuelPush.FriendsKey), inviterId)) return PushStatus.NotFriend;
            string token = Value(target, DuelPush.TokenKey);
            if (!DuelPush.IsDeviceToken(token)) return PushStatus.Off;

            // 3. Jamais la nuit chez lui.
            if (DuelPush.IsQuietHours(now, DuelPush.ParseTz(Value(target, DuelPush.TzKey)))) return PushStatus.Quiet;

            // 4. Cadence : l'invitant n'insiste pas, le destinataire n'est pas inondé. Chaque journal est réécrit avec le
            // verrou lu : si deux appels arrivent en même temps, un seul passe (sinon on dépasserait les plafonds). Écrits
            // avant l'envoi : un envoi raté compte aussi.
            var sent = await _store.GetProtectedAsync(inviterId, DuelPush.SentLogKey);
            sent.TryGetValue(DuelPush.SentLogKey, out var sentItem);
            if (!DuelPush.AllowSend(sentItem?.Value, targetId, now, out string newSent)) return PushStatus.Limit;
            var received = await _store.GetProtectedAsync(targetId, DuelPush.ReceivedLogKey);
            received.TryGetValue(DuelPush.ReceivedLogKey, out var receivedItem);
            if (!DuelPush.AllowReceive(receivedItem?.Value, now, out string newReceived)) return PushStatus.Limit;
            if (!await _store.TrySetProtectedAsync(inviterId, DuelPush.SentLogKey, newSent, sentItem?.WriteLock)) return PushStatus.Limit;
            if (!await _store.TrySetProtectedAsync(targetId, DuelPush.ReceivedLogKey, newReceived, receivedItem?.WriteLock)) return PushStatus.Limit;

            // 5. Envoi chez Apple.
            string name = DuelPush.DisplayName(Value(invite, DuelPush.DuelNameKey), FallbackName);
            var (status, deadToken) = await SendToAppleAsync(new ApnsMessage
            {
                DeviceToken = token,
                Topic = BundleId,
                PayloadJson = Payload(name),
                CollapseId = "duel-" + inviterId,
                ExpirationUnix = now + DuelPush.ExpirationSeconds,
            });
            if (deadToken)
            {
                // Jeton périmé : tout est effacé, comme quand le joueur coupe les rappels (son jeu republie s'il revient).
                try
                {
                    await _store.SetDefaultAsync(targetId, new Dictionary<string, string>
                    {
                        [DuelPush.TokenKey] = "", [DuelPush.FriendsKey] = "", [DuelPush.TzKey] = "",
                    });
                }
                catch (Exception) { /* sera retenté au prochain envoi */ }
            }
            return status;
        }

        /// <summary>
        /// Auto-test : envoi à un faux jeton. Apple répond « BadDeviceToken » seulement si la clé, l'équipe et l'app sont
        /// bonnes et si Cloud Code peut joindre Apple : c'est le « ok ». Au plus une fois toutes les 5 minutes pour tout le
        /// jeu (n'importe quel joueur peut l'appeler).
        /// </summary>
        public async Task<string> SelfTestAsync(long nowUnix)
        {
            try
            {
                var last = await _store.GetCustomAsync(GameDataId, SelfTestKey);
                long at = DuelPush.ParseUnix(last?.Value);
                if (at >= 0 && nowUnix - at < DuelPush.SelfTestGapSeconds && at - nowUnix < DuelPush.SelfTestGapSeconds) return PushStatus.Wait;
                await _store.SetCustomAsync(GameDataId, SelfTestKey, nowUnix.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception) { return PushStatus.Error; }
            var (status, _) = await SendToAppleAsync(new ApnsMessage
            {
                DeviceToken = FakeToken,
                Topic = BundleId,
                PayloadJson = Payload("Test"),
                ExpirationUnix = 0,
            });
            return status == PushStatus.Apns + ":400:BadDeviceToken" ? PushStatus.Ok : status;
        }

        /// <summary>Envoi chez Apple et traduction de sa réponse ; dead = jeton du destinataire à effacer.</summary>
        async Task<(string status, bool dead)> SendToAppleAsync(ApnsMessage message)
        {
            ApnsCredentials credentials;
            try { credentials = await _credentials.GetAsync(); }
            catch (Exception) { return (PushStatus.NoKey, false); }
            if (credentials == null || !credentials.IsComplete) return (PushStatus.NoKey, false);
            ApnsResult result;
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    result = await _apns.SendAsync(credentials, message, cts.Token);
            }
            catch (ApnsKeyException) { return (PushStatus.NoKey, false); }
            catch (Exception e) { return (PushStatus.ApnsNet + ":" + e.GetType().Name, false); }
            if (result.StatusCode == 200) return (PushStatus.Sent, false);
            if (result.IsDeadToken && message.DeviceToken != FakeToken) return (PushStatus.Gone, true);
            if (result.StatusCode == 429 || result.StatusCode >= 500) return (PushStatus.Busy, false);
            return (PushStatus.Apns + ":" + result.StatusCode + ":" + (result.Reason ?? ""), false);
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

        static string Value(Dictionary<string, StoredItem> data, string key) =>
            data != null && data.TryGetValue(key, out var v) ? v?.Value : null;
    }
}
