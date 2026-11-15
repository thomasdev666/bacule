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

        /// <summary>Invitation écrite à <paramref name="at"/> (heure du serveur) ; <paramref name="phoneAt"/> : heure du téléphone.</summary>
        void Invite(long at, string to = Target, string code = "ABCD12", string name = "Léo", long? phoneAt = null)
        {
            _store.Put("public", Inviter, DuelPush.DuelToKey, to, at);
            _store.Put("public", Inviter, DuelPush.DuelCodeKey, code, at);
            _store.Put("public", Inviter, DuelPush.DuelAtKey, (phoneAt ?? at).ToString(), at);
            _store.Put("public", Inviter, DuelPush.DuelNameKey, name, at);
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
        public async Task InviteWrittenInTheFutureIsStale()
        {
            Invite(Now + 31);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Stale));
            Invite(Now + 30);
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Sent));
        }

        [Test]
        public async Task PhoneClockOfTheInviterDoesNotMatter()
        {
            // Téléphone de l'invitant en avance d'une heure : c'est l'heure d'écriture du serveur qui compte.
            Invite(Now - 3, phoneAt: Now + 3600);
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
        public async Task StrangerLearnsNothingAboutTheTarget()
        {
            // Un inconnu reçoit la même réponse, que la cible ait les notifications ou non.
            _store.Put("default", Target, DuelPush.FriendsKey, "Other9");
            string withToken = await Send(Now);
            _store.Put("default", Target, DuelPush.TokenKey, "");
            string withoutToken = await Send(Now);
            Assert.That(withToken, Is.EqualTo(PushStatus.NotFriend));
            Assert.That(withoutToken, Is.EqualTo(PushStatus.NotFriend));
            Assert.That(_store.Value("protected", Target, DuelPush.ReceivedLogKey), Is.Null, "rien d'écrit chez la cible");
        }

        [Test]
        public async Task TwentySimultaneousCallsSendOnlyOnce()
        {
            // Client modifié qui lance 20 appels en même temps : les journaux sont réécrits avec leur verrou, un seul passe.
            _store.Put("protected", Inviter, DuelPush.SentLogKey, (Now - 7200) + ":Other9");
            _store.Put("protected", Target, DuelPush.ReceivedLogKey, (Now - 7200).ToString());
            _store.Interleave = true;
            var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => Send(Now))));
            Assert.That(results.Count(r => r == PushStatus.Sent), Is.EqualTo(1));
            Assert.That(results.Where(r => r != PushStatus.Sent), Is.All.EqualTo(PushStatus.Limit));
            Assert.That(_apns.Sent, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task FirstEverLogLosesIfAnotherCallWritesRightAfter()
        {
            // Tout premier envoi (journal absent, donc sans verrou) : un autre appel écrit juste après le nôtre, c'est lui
            // qui compte et celui-ci s'arrête.
            _store.AfterUnlockedWrite = (player, key) =>
            {
                _store.AfterUnlockedWrite = null;
                _store.Put("protected", player, key, Now + ":Target2");
            };
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Limit));
            Assert.That(_apns.Sent, Is.Empty);
        }

        [Test]
        public async Task SimultaneousCallsFromManyFriendsRespectTheTargetCap()
        {
            // 10 déjà reçus moins un : 12 amis différents en même temps, un seul de plus arrive.
            var friends = Enumerable.Range(0, 12).Select(i => "Friend" + i).ToList();
            _store.Put("default", Target, DuelPush.FriendsKey, string.Join(",", friends));
            _store.Put("protected", Target, DuelPush.ReceivedLogKey, string.Join(",", Enumerable.Range(0, 9).Select(i => (Now - 3600 - i).ToString())));
            foreach (var f in friends)
            {
                _store.Put("public", f, DuelPush.DuelToKey, Target, Now);
                _store.Put("public", f, DuelPush.DuelCodeKey, "CODE", Now);
                _store.Put("public", f, DuelPush.DuelNameKey, f, Now);
            }
            _store.Interleave = true;
            var results = await Task.WhenAll(friends.Select(f => Task.Run(() => _service.SendAsync(f, Target, Now))));
            Assert.That(results.Count(r => r == PushStatus.Sent), Is.EqualTo(1));
            Assert.That(_store.Value("protected", Target, DuelPush.ReceivedLogKey).Split(','), Has.Length.EqualTo(10));
        }

        [Test]
        public async Task TargetWhoDidNotAcceptTheInviterIsNotBothered()
        {
            _store.Put("default", Target, DuelPush.FriendsKey, "Other9");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NotFriend));
            _store.Put("default", Target, DuelPush.FriendsKey, "Inviter12");
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.NotFriend));
            _store.Put("default", Target, DuelPush.FriendsKey, "");
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
        public async Task DeadTokenIsErasedWithTheFriendsAndTimeZone()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 410, Reason = "Unregistered" };
            Assert.That(await Send(Now), Is.EqualTo(PushStatus.Gone));
            Assert.That(_store.Value("default", Target, DuelPush.TokenKey), Is.EqualTo(""));
            Assert.That(_store.Value("default", Target, DuelPush.FriendsKey), Is.EqualTo(""));
            Assert.That(_store.Value("default", Target, DuelPush.TzKey), Is.EqualTo(""));
            Invite(Now + 200);
            Assert.That(await Send(Now + 200), Is.EqualTo(PushStatus.NotFriend));
            Assert.That(_apns.Sent, Has.Count.EqualTo(1));
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
        public async Task AppleUnreachableIsNamed()
        {
            _apns.Throw = new System.Net.Http.HttpRequestException("pas de réseau");
            Assert.That(await Send(Now), Is.EqualTo("apnsnet:HttpRequestException"));
            StringAssert.EndsWith("Z apnsnet:HttpRequestException", _store.Value("protected", Inviter, DuelPush.StatusKey));
        }

        // ------------------------------------------------------------------ Auto-test (déploiement, vérification)

        [Test]
        public async Task SelfTestIsOkWhenAppleRefusesOnlyTheFakeToken()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 400, Reason = "BadDeviceToken" };
            Assert.That(await _service.SelfTestAsync(Now), Is.EqualTo(PushStatus.Ok));
            var m = _apns.Sent.Single();
            Assert.That(m.DeviceToken, Is.EqualTo(new string('0', 64)));
            Assert.That(m.Topic, Is.EqualTo("fr.orpheex.bascule"));
            Assert.That(_store.Writes, Is.EqualTo(1), "seulement l'heure de l'auto-test : aucun jeton effacé");
        }

        [Test]
        public async Task SelfTestRunsOnceEveryFiveMinutesForTheWholeGame()
        {
            _apns.Reply = _ => new ApnsResult { StatusCode = 400, Reason = "BadDeviceToken" };
            Assert.That(await _service.SelfTestAsync(Now), Is.EqualTo(PushStatus.Ok));
            Assert.That(await _service.SelfTestAsync(Now + 299), Is.EqualTo(PushStatus.Wait));
            Assert.That(await _service.SelfTestAsync(Now + 300), Is.EqualTo(PushStatus.Ok));
            Assert.That(_apns.Sent, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task SelfTestNamesTheProblem()
        {
            _credentials.Value = null;
            Assert.That(await _service.SelfTestAsync(Now), Is.EqualTo(PushStatus.NoKey));
            _credentials.Value = new FixedCredentials().Value;
            _apns.Reply = _ => new ApnsResult { StatusCode = 403, Reason = "InvalidProviderToken" };
            Assert.That(await _service.SelfTestAsync(Now + 400), Is.EqualTo("apns:403:InvalidProviderToken"));
            _apns.Throw = new TaskCanceledException();
            Assert.That(await _service.SelfTestAsync(Now + 800), Is.EqualTo("apnsnet:TaskCanceledException"));
            _store.Fail = true;
            Assert.That(await _service.SelfTestAsync(Now + 1200), Is.EqualTo(PushStatus.Error));
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
        [TestCase("bit.ly/x", "bitlyx")]
        [TestCase("Léo\u202Ex", "Léox")]
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
