using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BasculePush.Tests
{
    /// <summary>Jeton JWT d'Apple et requête HTTP/2 envoyée à APNs (faux réseau).</summary>
    public class ApnsTests
    {
        static ECDsa _key;
        static string _pem;
        static readonly string Token = new string('b', 64);

        [OneTimeSetUp]
        public void MakeKey()
        {
            // Même format que le fichier .p8 d'Apple : clé P-256 en PKCS#8, encadrée par BEGIN/END PRIVATE KEY.
            _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            _pem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(_key.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks).Replace("\r", "") + "\n-----END PRIVATE KEY-----\n";
        }

        static ApnsCredentials Credentials() => new ApnsCredentials { PrivateKey = _pem, KeyId = "KEY1234567", TeamId = "TEAM765432" };

        static byte[] FromBase64Url(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s + new string('=', (4 - s.Length % 4) % 4));
        }

        [Test]
        public void JwtHasAppleHeaderClaimsAndAValidSignature()
        {
            string jwt = ApnsJwt.Create(_pem, "KEY1234567", "TEAM765432", 1_800_000_000);
            var parts = jwt.Split('.');
            Assert.That(parts, Has.Length.EqualTo(3));
            Assert.That(jwt, Does.Not.Contain("="), "base64url sans remplissage");
            var header = JObject.Parse(Encoding.UTF8.GetString(FromBase64Url(parts[0])));
            Assert.That((string)header["alg"], Is.EqualTo("ES256"));
            Assert.That((string)header["kid"], Is.EqualTo("KEY1234567"));
            var claims = JObject.Parse(Encoding.UTF8.GetString(FromBase64Url(parts[1])));
            Assert.That((string)claims["iss"], Is.EqualTo("TEAM765432"));
            Assert.That((long)claims["iat"], Is.EqualTo(1_800_000_000));
            byte[] signature = FromBase64Url(parts[2]);
            Assert.That(signature, Has.Length.EqualTo(64), "r || s, pas du DER");
            Assert.That(_key.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Test]
        public void KeyCanBePastedInManyWays()
        {
            string body = string.Concat(_pem.Split('\n').Where(l => !l.StartsWith("-----")));
            foreach (var variant in new[]
            {
                _pem,
                _pem.Replace("\n", "\r\n"),
                _pem.Replace("\n", "\\n"),
                _pem.Replace("\n", " "),
                body,
                "  " + body + "  ",
            })
            {
                using (var k = ApnsJwt.ImportKey(variant))
                    Assert.That(k.ExportSubjectPublicKeyInfo(), Is.EqualTo(_key.ExportSubjectPublicKeyInfo()));
            }
        }

        [TestCase("")]
        [TestCase("pas une clé")]
        [TestCase("-----BEGIN PRIVATE KEY-----\nAAAA\n-----END PRIVATE KEY-----")]
        public void UnreadableKeyGivesAClearError(string key)
        {
            var e = Assert.Throws<ApnsKeyException>(() => ApnsJwt.ImportKey(key));
            StringAssert.Contains("APNS_KEY", e.Message);
        }

        [Test]
        public void RsaKeyIsRefused()
        {
            using (var rsa = RSA.Create(2048))
            {
                string pem = "-----BEGIN PRIVATE KEY-----\n" + Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()) + "\n-----END PRIVATE KEY-----";
                Assert.Throws<ApnsKeyException>(() => ApnsJwt.ImportKey(pem));
            }
        }

        static ApnsMessage Message() => new ApnsMessage
        {
            DeviceToken = Token,
            Topic = "fr.orpheex.bascule",
            PayloadJson = "{\"aps\":{\"alert\":{\"title\":\"Duel !\",\"body\":\"Léo te défie en duel. Viens vite !\"}}}",
            CollapseId = "duel-Inviter1",
            ExpirationUnix = 1_800_000_300,
        };

        [Test]
        public async Task RequestFollowsApplesFormat()
        {
            var handler = new FakeHandler();
            var sender = new ApnsSender(handler, null, () => DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
            var result = await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            var (request, body) = handler.Requests.Single();
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri.ToString(), Is.EqualTo("https://api.push.apple.com/3/device/" + Token));
            Assert.That(request.Version, Is.EqualTo(HttpVersion.Version20));
            Assert.That(request.VersionPolicy, Is.EqualTo(HttpVersionPolicy.RequestVersionExact));
            string Header(string name) => request.Headers.GetValues(name).Single();
            StringAssert.StartsWith("bearer ", Header("authorization"));
            Assert.That(Header("apns-topic"), Is.EqualTo("fr.orpheex.bascule"));
            Assert.That(Header("apns-push-type"), Is.EqualTo("alert"));
            Assert.That(Header("apns-priority"), Is.EqualTo("10"));
            Assert.That(Header("apns-expiration"), Is.EqualTo("1800000300"));
            Assert.That(Header("apns-collapse-id"), Is.EqualTo("duel-Inviter1"));
            Assert.That(request.Content.Headers.ContentType.MediaType, Is.EqualTo("application/json"));
            Assert.That(body, Is.EqualTo(Message().PayloadJson));
            // Le jeton porte bien la clé et l'équipe données.
            string jwt = Header("authorization").Substring("bearer ".Length);
            var claims = JObject.Parse(Encoding.UTF8.GetString(FromBase64Url(jwt.Split('.')[1])));
            Assert.That((string)claims["iss"], Is.EqualTo("TEAM765432"));
        }

        [Test]
        public async Task RefusalReasonIsRead()
        {
            var handler = new FakeHandler
            {
                Reply = (_, __) => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"reason\":\"BadDeviceToken\"}") },
            };
            var result = await new ApnsSender(handler, null, null).SendAsync(Credentials(), Message(), CancellationToken.None);
            Assert.That(result.StatusCode, Is.EqualTo(400));
            Assert.That(result.Reason, Is.EqualTo("BadDeviceToken"));
            Assert.That(result.IsDeadToken);
        }

        [Test]
        public async Task GarbageBodyDoesNotCrash()
        {
            var handler = new FakeHandler { Reply = (_, __) => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>") } };
            var result = await new ApnsSender(handler, null, null).SendAsync(Credentials(), Message(), CancellationToken.None);
            Assert.That(result.StatusCode, Is.EqualTo(502));
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public async Task SameJwtIsReusedFor40MinutesThenRenewed()
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var handler = new FakeHandler();
            var sender = new ApnsSender(handler, null, () => now);
            await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            now = now.AddMinutes(39);
            await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            now = now.AddMinutes(2);
            await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            var auth = handler.Requests.Select(r => r.request.Headers.GetValues("authorization").Single()).ToList();
            Assert.That(auth[1], Is.EqualTo(auth[0]));
            Assert.That(auth[2], Is.Not.EqualTo(auth[0]));
        }

        [Test]
        public async Task NewKeyGivesANewJwtAtOnce()
        {
            var handler = new FakeHandler();
            var sender = new ApnsSender(handler, null, () => DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
            await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            var other = Credentials();
            other.KeyId = "NEWKEY0001";
            await sender.SendAsync(other, Message(), CancellationToken.None);
            var auth = handler.Requests.Select(r => r.request.Headers.GetValues("authorization").Single()).ToList();
            Assert.That(auth[1], Is.Not.EqualTo(auth[0]));
        }

        [Test]
        public async Task ExpiredProviderTokenIsRetriedOnceWithAFreshJwt()
        {
            var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
            var handler = new FakeHandler
            {
                Reply = (_, n) => n == 1
                    ? new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("{\"reason\":\"ExpiredProviderToken\"}") }
                    : new HttpResponseMessage(HttpStatusCode.OK),
            };
            var sender = new ApnsSender(handler, null, () => { now = now.AddSeconds(1); return now; });
            var result = await sender.SendAsync(Credentials(), Message(), CancellationToken.None);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
            Assert.That(handler.Requests[1].request.Headers.GetValues("authorization").Single(),
                Is.Not.EqualTo(handler.Requests[0].request.Headers.GetValues("authorization").Single()));
        }

        [Test]
        public async Task ProviderTokenRefusedTwiceStopsThere()
        {
            var handler = new FakeHandler
            {
                Reply = (_, __) => new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("{\"reason\":\"InvalidProviderToken\"}") },
            };
            var result = await new ApnsSender(handler, null, null).SendAsync(Credentials(), Message(), CancellationToken.None);
            Assert.That(result.StatusCode, Is.EqualTo(403));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
        }

        [Test]
        public void UnreadableKeyThrowsBeforeAnyRequest()
        {
            var handler = new FakeHandler();
            var bad = Credentials();
            bad.PrivateKey = "abc";
            Assert.ThrowsAsync<ApnsKeyException>(() => new ApnsSender(handler, null, null).SendAsync(bad, Message(), CancellationToken.None));
            Assert.That(handler.Requests, Is.Empty);
        }
    }
}
