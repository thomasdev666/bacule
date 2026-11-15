using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BasculePush
{
    /// <summary>
    /// Clé d'envoi de notifications créée chez Apple (fichier .p8), son identifiant (10 caractères) et l'identifiant de
    /// l'équipe (Team ID, 10 caractères). Rangés dans le Secret Manager de Unity, jamais dans le code.
    /// </summary>
    public sealed class ApnsCredentials
    {
        public string PrivateKey;
        public string KeyId;
        public string TeamId;

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(PrivateKey) && !string.IsNullOrWhiteSpace(KeyId) && !string.IsNullOrWhiteSpace(TeamId);
    }

    public sealed class ApnsMessage
    {
        public string DeviceToken;
        public string Topic;
        public string PayloadJson;
        public string CollapseId;
        public long ExpirationUnix;
    }

    /// <summary>Réponse d'Apple : 200 si c'est accepté, sinon le code et la raison (« BadDeviceToken »…).</summary>
    public sealed class ApnsResult
    {
        public int StatusCode;
        public string Reason;

        /// <summary>Le jeton ne vaut plus rien (app supprimée, notifications coupées, jeton d'une autre app) : à effacer.</summary>
        public bool IsDeadToken =>
            StatusCode == 410 || (StatusCode == 400 && (Reason == "BadDeviceToken" || Reason == "DeviceTokenNotForTopic"));

        /// <summary>Le jeton d'authentification (JWT) est refusé : il faut en refaire un.</summary>
        public bool IsBadProviderToken =>
            StatusCode == 403 && (Reason == "ExpiredProviderToken" || Reason == "InvalidProviderToken" || Reason == "MissingProviderToken");
    }

    /// <summary>La clé .p8 rangée dans le Secret Manager est illisible.</summary>
    public sealed class ApnsKeyException : Exception
    {
        public ApnsKeyException(string message, Exception inner) : base(message, inner) { }
    }

    public interface IApnsSender
    {
        Task<ApnsResult> SendAsync(ApnsCredentials credentials, ApnsMessage message, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Envoi chez Apple (APNs) : HTTP/2 obligatoire, authentification par jeton JWT signé ES256 avec la clé .p8.
    /// Doc Apple : « Sending notification requests to APNs » et « Establishing a token-based connection to APNs ».
    /// </summary>
    public sealed class ApnsSender : IApnsSender
    {
        public const string ProductionHost = "api.push.apple.com";

        /// <summary>
        /// Apple refuse un jeton JWT de plus d'une heure, et ne veut pas qu'on en refasse plus d'un toutes les 20 minutes :
        /// le même sert 40 minutes.
        /// </summary>
        public static readonly TimeSpan JwtLifetime = TimeSpan.FromMinutes(40);

        readonly HttpClient _http;
        readonly string _host;
        readonly Func<DateTimeOffset> _clock;
        readonly object _lock = new object();
        string _jwt, _jwtFor;
        DateTimeOffset _jwtAt;

        public ApnsSender() : this(null, ProductionHost, null) { }

        /// <summary>Pour les tests : faux réseau, autre hôte, autre horloge.</summary>
        public ApnsSender(HttpMessageHandler handler, string host, Func<DateTimeOffset> clock)
        {
            _http = new HttpClient(handler ?? new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                EnableMultipleHttp2Connections = true,
            })
            {
                Timeout = TimeSpan.FromSeconds(10),
            };
            _host = host ?? ProductionHost;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<ApnsResult> SendAsync(ApnsCredentials credentials, ApnsMessage message, CancellationToken cancellationToken)
        {
            string jwt = Jwt(credentials);
            var result = await PostAsync(jwt, message, cancellationToken);
            if (result.StatusCode == 403 && result.Reason == "ExpiredProviderToken")
            {
                // Jeton trop vieux pour Apple (horloge) : un neuf, une seule nouvelle tentative. Pas pour une clé fausse
                // (InvalidProviderToken) : en refaire un à chaque appel ferait répondre Apple « trop de jetons » (429) et
                // cacherait la vraie cause. Une clé changée dans le Secret Manager donne de toute façon un jeton neuf.
                lock (_lock) { if (_jwt == jwt) _jwt = null; }
                result = await PostAsync(Jwt(credentials), message, cancellationToken);
            }
            return result;
        }

        async Task<ApnsResult> PostAsync(string jwt, ApnsMessage message, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "https://" + _host + "/3/device/" + message.DeviceToken))
            {
                request.Version = HttpVersion.Version20;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                request.Headers.TryAddWithoutValidation("authorization", "bearer " + jwt);
                request.Headers.TryAddWithoutValidation("apns-topic", message.Topic);
                request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
                request.Headers.TryAddWithoutValidation("apns-priority", "10");
                request.Headers.TryAddWithoutValidation("apns-expiration", message.ExpirationUnix.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(message.CollapseId))
                    request.Headers.TryAddWithoutValidation("apns-collapse-id", message.CollapseId.Length > 64 ? message.CollapseId.Substring(0, 64) : message.CollapseId);
                request.Content = new StringContent(message.PayloadJson, Encoding.UTF8, "application/json");
                using (var response = await _http.SendAsync(request, cancellationToken))
                {
                    string body = response.Content == null ? "" : await response.Content.ReadAsStringAsync(cancellationToken);
                    return new ApnsResult { StatusCode = (int)response.StatusCode, Reason = ReadReason(body) };
                }
            }
        }

        static string ReadReason(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try { return JObject.Parse(body).Value<string>("reason"); }
            catch (JsonException) { return null; }
        }

        string Jwt(ApnsCredentials c)
        {
            string id = c.KeyId.Trim() + "|" + c.TeamId.Trim() + "|" + c.PrivateKey;
            var now = _clock();
            lock (_lock)
            {
                if (_jwt != null && _jwtFor == id && now - _jwtAt < JwtLifetime && now >= _jwtAt) return _jwt;
                _jwt = ApnsJwt.Create(c.PrivateKey, c.KeyId.Trim(), c.TeamId.Trim(), now.ToUnixTimeSeconds());
                _jwtFor = id;
                _jwtAt = now;
                return _jwt;
            }
        }
    }

    /// <summary>Jeton d'authentification d'Apple : en-tête {alg ES256, kid}, contenu {iss = Team ID, iat}, signé avec la clé .p8.</summary>
    public static class ApnsJwt
    {
        public static string Create(string p8, string keyId, string teamId, long issuedAtUnix)
        {
            string header = Base64Url(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { alg = "ES256", kid = keyId })));
            string claims = Base64Url(Encoding.UTF8.GetBytes("{\"iss\":" + JsonConvert.ToString(teamId) + ",\"iat\":" + issuedAtUnix.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}"));
            string signingInput = header + "." + claims;
            using (var key = ImportKey(p8))
            {
                // Signature au format attendu par les JWT : r || s, 64 octets (pas le format DER).
                byte[] signature = key.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
                return signingInput + "." + Base64Url(signature);
            }
        }

        /// <summary>
        /// Contenu du fichier .p8, collé tel quel dans le Secret Manager : avec ou sans les lignes « -----BEGIN PRIVATE
        /// KEY----- », avec de vrais retours à la ligne, des espaces, ou des « \n » écrits en toutes lettres.
        /// </summary>
        public static ECDsa ImportKey(string p8)
        {
            try
            {
                string s = (p8 ?? "").Replace("\\n", "\n").Replace("\\r", "\r");
                s = s.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "");
                var sb = new StringBuilder();
                foreach (char ch in s)
                    if (!char.IsWhiteSpace(ch)) sb.Append(ch);
                byte[] der = Convert.FromBase64String(sb.ToString());
                var key = ECDsa.Create();
                try
                {
                    key.ImportPkcs8PrivateKey(der, out _);
                    if (key.KeySize != 256) throw new CryptographicException("La clé n'est pas une clé P-256.");
                    return key;
                }
                catch
                {
                    key.Dispose();
                    throw;
                }
            }
            catch (Exception e) when (e is FormatException || e is CryptographicException || e is ArgumentException)
            {
                throw new ApnsKeyException("Clé APNS_KEY illisible : coller tout le contenu du fichier .p8 téléchargé chez Apple.", e);
            }
        }

        public static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
