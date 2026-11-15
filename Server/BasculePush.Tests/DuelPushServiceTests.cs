using System;
using System.Linq;
using System.Threading.Tasks;
using Bascule.Core;
using NUnit.Framework;

namespace BasculePush.Tests
{
    /// <summary>Tous les chemins de la fonction DuelInvite, avec un Cloud Save en mémoire et un faux Apple.</summary>
    public class DuelPushServiceTests
    {
        const string Inviter = "Inviter1", Target = "Target2";
        static readonly string Token = new string('a', 32) + new string('7', 32);
        // Mardi 17 novembre 2026, 15:00 UTC (16 h à Paris).
        static readonly long Now = new DateTimeOffset(2026, 11, 17, 15, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        MemoryStore _store;
        FixedCredentials _credentials;
        RecordingSender _apns;
        DuelPushService _service;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _credentials = new FixedCredentials();
            _apns = new RecordingSender();
            _service = new DuelPushService(_store, _credentials, _apns);
            Invite(Now - 3);
            _store.Put("default", Target, DuelPush.TokenKey, Token);
            _store.Put("default", Target, DuelPush.FriendsKey, "Other9," + Inviter);
            _store.Put("default", Target, DuelPush.TzKey, "60");
        }

        void Invite(long at, string to = Target, string code = "ABCD12", string name = "Léo")
        {
            _store.Put("public", Inviter, DuelPush.DuelToKey, to);
            _store.Put("public", Inviter, DuelPush.DuelCodeKey, code);
            _store.Put("public", Inviter, DuelPush.DuelAtKey, at.ToString());
            _store.Put("public", Inviter, DuelPush.DuelNameKey, name);
        }

        Task<string> Send(long now) => _service.SendAsync(Inviter, Target, now);

        [Test]
        public async Task FreshInviteToAFriendIsSent()
        {
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
            Assert.That(_apns.Sent, Has.Count.EqualTo(1));
            var m = _apns.Sent[0];
            Assert.That(m.DeviceToken, Is.EqualTo(Token));
            Assert.That(m.Topic, Is.EqualTo("fr.orpheex.bascule"));
            Assert.That(m.CollapseId, Is.EqualTo("duel-" + Inviter));
            Assert.That(m.ExpirationUnix, Is.EqualTo(Now + 300));
            StringAssert.Contains("Léo te défie en duel", m.PayloadJson);
            StringAssert.Contains("\"sound\":\"default\"", m.PayloadJson);
            // Journaux écrits (envoi de l'invitant, réception du destinataire) et diagnostic.
            Assert.That(_store.Value("protected", Inviter, DuelPush.SentLogKey), Is.EqualTo(Now + ":" + Target));
            Assert.That(_store.Value("protected", Target, DuelPush.ReceivedLogKey), Is.EqualTo(Now.ToString()));
            StringAssert.EndsWith("Z sent", _store.Value("protected", Inviter, DuelPush.StatusKey));
        }

        [Test]
        public async Task PayloadIsValidJsonForApple()
        {
            await Send(Now);
            var json = Newtonsoft.Json.Linq.JObject.Parse(_apns.Sent[0].PayloadJson);
            Assert.That((string)json["aps"]["alert"]["title"], Is.EqualTo("Duel !"));
            Assert.That((string)json["aps"]["alert"]["body"], Is.EqualTo("Léo te défie en duel. Viens vite !"));
            Assert.That(System.Text.Encoding.UTF8.GetByteCount(_apns.Sent[0].PayloadJson), Is.LessThan(4096));
        }

        [TestCase("")]
        [TestCase("ab")]
        [TestCase("Target2/../x")]
        [TestCase("Inviter1")]
        public async Task BadTargetIsRefused(string target)
        {
            Assert.That(await _service.SendAsync(Inviter, target, Now), Is.EqualTo(PushStatus.Invalid).Or.EqualTo(PushStatus.Stale));
            Assert.That(_apns.Sent, Is.Empty);
        }

        [Test]
        public async Task InvitingYourselfIsInvalid()
        {
            Assert.That(await _service.SendAsync(Inviter, Inviter, Now), Is.EqualTo(PushStatus.Invalid));
        }

        [Test]
        public async Task OldInviteIsStale()
        {
            Invite(Now - 121);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
            Assert.That(_apns.Sent, Is.Empty);
        }

        [Test]
        public async Task InviteToSomeoneElseIsStale()
        {
            Invite(Now, to: "Other9");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
        }

        [Test]
        public async Task ClearedInviteIsStale()
        {
            Invite(Now, to: "-");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
            Invite(Now, code: "-");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
        }

        [Test]
        public async Task InviteFromTheFutureIsStale()
        {
            Invite(Now + 31);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
            Invite(Now + 30);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
        }

        [Test]
        public async Task TargetWithoutTokenIsOffAndNothingIsWritten()
        {
            _store.Put("default", Target, DuelPush.TokenKey, "");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Off));
            Assert.That(_store.Writes, Is.Zero);
        }

        [Test]
        public async Task TargetWhoDidNotAcceptTheInviterIsNotBothered()
        {
            _store.Put("default", Target, DuelPush.FriendsKey, "Other9");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NotFriend));
            _store.Put("default", Target, DuelPush.FriendsKey, "Inviter12");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NotFriend));
            Assert.That(_apns.Sent, Is.Empty);
        }

        [Test]
        public async Task NothingAtNightForTheTarget()
        {
            // 15:00 UTC = 21:00 à UTC+6.
            _store.Put("default", Target, DuelPush.TzKey, "360");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Quiet));
            // 15:00 UTC = 07:59 à UTC-7:01.
            _store.Put("default", Target, DuelPush.TzKey, "-421");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Quiet));
            _store.Put("default", Target, DuelPush.TzKey, "-420");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
        }

        [Test]
        public async Task SameFriendCannotBeSpammed()
        {
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
            Invite(Now + 60);
            Assert.That(await Send(Now + 60), Is.EqualTo(PushStatus.Limit));
            Invite(Now + 125);
            Assert.That(await Send(Now + 125), Is.EqualTo(PushStatus.Sent));
            Assert.That(_apns.Sent, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task TargetReceivesTenAtMostPerDay()
        {
            _store.Put("protected", Target, DuelPush.ReceivedLogKey, string.Join(",", System.Linq.Enumerable.Range(0, 10).Select(i => (Now - 3600 - i).ToString())));
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Limit));
            Assert.That(_apns.Sent, Is.Empty);
            // Le lendemain, c'est reparti.
            Invite(Now + 86400 - 3500);
            Assert.That(await Send(Now + 86400 - 3500), Is.EqualTo(PushStatus.Sent));
        }

        [Test]
        public async Task MissingAppleKeyIsReported()
        {
            _credentials.Value = null;
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NoKey));
            StringAssert.EndsWith("Z nokey", _store.Value("protected", Inviter, DuelPush.StatusKey));
            _credentials.Value = new ApnsCredentials { PrivateKey = "x", KeyId = " ", TeamId = "T" };
            Invite(Now + 200);
            Assert.That(await Send(Now + 200), Is.EqualTo(PushStatus.NoKey));
        }

        [Test]
        public async Task UnreadableAppleKeyIsReported()
        {
            _apns.Throw = new ApnsKeyException("illisible", null);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NoKey));
        }

        [Test]
        public async Task DeadTokenIsErased()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 410, Reason = "Unregistered" };
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Gone));
            Assert.That(_store.Value("default", Target, DuelPush.TokenKey), Is.EqualTo(""));
            Invite(Now + 200);
            Assert.That(await Send(Now + 200), Is.EqualTo(PushStatus.Off));
        }

        [Test]
        public async Task BadDeviceTokenIsErasedToo()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 400, Reason = "BadDeviceToken" };
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Gone));
            Assert.That(_store.Value("default", Target, DuelPush.TokenKey), Is.EqualTo(""));
        }

        [Test]
        public async Task AppleBusyOrOtherRefusal()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 429, Reason = "TooManyRequests" };
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Busy));
            _apns.Reply = _ => new ApnsResult { StatusCode = 403, Reason = "InvalidProviderToken" };
            Invite(Now + 200);
            Assert.That(await Send(Now + 200), Is.EqualTo("apns:403:InvalidProviderToken"));
            Assert.That(_store.Value("default", Target, DuelPush.TokenKey), Is.EqualTo(Token), "le jeton n'est pas effacé pour un problème de clé");
        }

        [Test]
        public async Task NetworkFailureIsAnError()
        {
            _apns.Throw = new System.Net.Http.HttpRequestException("pas de réseau");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Error));
        }

        [Test]
        public async Task CloudSaveFailureIsAnErrorNotACrash()
        {
            _store.Fail = true;
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Error));
        }

        [TestCase("Léo#1234", "Léo")]
        [TestCase("  Zoé  ", "Zoé")]
        [TestCase("", "Un ami")]
        [TestCase("connard", "Un ami")]
        [TestCase("P0rn0Star", "Un ami")]
        [TestCase("Jean\u0007Paul", "JeanPaul")]
        public async Task InviterNameIsFiltered(string raw, string shown)
        {
            Invite(Now, name: raw);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
            StringAssert.Contains("\"body\":\"" + shown + " te défie en duel", _apns.Sent[0].PayloadJson);
        }

        [Test]
        public async Task NameCannotBreakTheJson()
        {
            Invite(Now, name: "a\"},\"x\":{\"");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
            var json = Newtonsoft.Json.Linq.JObject.Parse(_apns.Sent[0].PayloadJson);
            Assert.That(json.Count, Is.EqualTo(1), "seule la clé aps");
        }
    }
}
