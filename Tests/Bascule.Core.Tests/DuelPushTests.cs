using System;
using System.Collections.Generic;
using System.Linq;
using Bascule.Core;
using NUnit.Framework;

namespace Bascule.Core.Tests
{
    /// <summary>Règles de la notification de duel (Core/DuelPush.cs), partagées avec le module serveur Server/BasculePush.</summary>
    public class DuelPushTests
    {
        static readonly long Now = new DateTimeOffset(2026, 11, 17, 15, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        [Test]
        public void FriendListSizeMatchesTheGame()
        {
            Assert.That(DuelPush.MaxFriends, Is.EqualTo(FriendCode.MaxFriends));
        }

        [Test]
        public void FriendsCsvKeepsValidUniqueIdsInOrder()
        {
            Assert.That(DuelPush.FriendsCsv(new[] { "AbC1", "", null, "AbC1", "x,y", "Zed9", "é" }), Is.EqualTo("AbC1,Zed9"));
            Assert.That(DuelPush.FriendsCsv(null), Is.EqualTo(""));
            var many = Enumerable.Range(0, 80).Select(i => "P" + i).ToList();
            var csv = DuelPush.FriendsCsv(many).Split(',');
            Assert.That(csv, Has.Length.EqualTo(DuelPush.MaxFriends));
            Assert.That(csv[0], Is.EqualTo("P0"));
        }

        [Test]
        public void ConfirmedFriendsOnlyAndBlockedPlayersLeave()
        {
            var p = new Profile();
            p.friends.AddRange(new[] { "Ami1", "Demande2", "Ami3" });
            Friendship.MarkConfirmed(p, "Ami1");
            Friendship.MarkConfirmed(p, "Ami3");
            Assert.That(DuelPush.FriendsCsv(Friendship.ConfirmedFriends(p)), Is.EqualTo("Ami1,Ami3"), "une demande pas encore acceptée ne peut pas notifier");
            Moderation.Block(p, "Ami3", "Moi");
            Assert.That(DuelPush.FriendsCsv(Friendship.ConfirmedFriends(p)), Is.EqualTo("Ami1"), "un joueur bloqué ne peut plus notifier");
        }

        [Test]
        public void IsFriendMatchesWholeIdsOnly()
        {
            Assert.That(DuelPush.IsFriend("Abc1,Def2", "Def2"));
            Assert.That(DuelPush.IsFriend("Abc1,Def2", "Def"), Is.False);
            Assert.That(DuelPush.IsFriend("Abc1,Def2", ""), Is.False);
            Assert.That(DuelPush.IsFriend("", "Abc1"), Is.False);
            Assert.That(DuelPush.IsFriend(null, "Abc1"), Is.False);
        }

        [Test]
        public void InviteMustBeFreshAndAddressedToTheTarget()
        {
            long at = Now - 10;
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", at, "T1", Now));
            Assert.That(DuelPush.IsFreshInvite("T2", "CODE", at, "T1", Now), Is.False);
            Assert.That(DuelPush.IsFreshInvite("T1", "-", at, "T1", Now), Is.False);
            Assert.That(DuelPush.IsFreshInvite("T1", "", at, "T1", Now), Is.False);
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", -1, "T1", Now), Is.False, "heure inconnue");
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", Now - 120, "T1", Now));
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", Now - 121, "T1", Now), Is.False);
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", Now + 30, "T1", Now));
            Assert.That(DuelPush.IsFreshInvite("T1", "CODE", Now + 31, "T1", Now), Is.False);
            Assert.That(DuelPush.IsFreshInvite(null, "CODE", at, null, Now), Is.False);
            Assert.That(DuelPush.ParseUnix("1800000000"), Is.EqualTo(1800000000));
            Assert.That(DuelPush.ParseUnix("abc"), Is.EqualTo(-1));
            Assert.That(DuelPush.ParseUnix(null), Is.EqualTo(-1));
            Assert.That(DuelPush.ParseUnix("-5"), Is.EqualTo(-1));
        }

        [TestCase(60, false)]     // 16 h à Paris
        [TestCase(360, true)]     // 21 h
        [TestCase(359, false)]    // 20 h 59
        [TestCase(-420, false)]   // 8 h
        [TestCase(-421, true)]    // 7 h 59
        [TestCase(600, true)]     // 1 h du matin
        public void QuietHoursFollowTheTargetsClock(int tz, bool quiet)
        {
            Assert.That(DuelPush.IsQuietHours(Now, tz), Is.EqualTo(quiet));
        }

        [Test]
        public void TimeZoneIsReadSafely()
        {
            Assert.That(DuelPush.ParseTz("120"), Is.EqualTo(120));
            Assert.That(DuelPush.ParseTz("-300"), Is.EqualTo(-300));
            Assert.That(DuelPush.ParseTz(null), Is.EqualTo(DuelPush.DefaultTzMinutes));
            Assert.That(DuelPush.ParseTz("abc"), Is.EqualTo(DuelPush.DefaultTzMinutes));
            Assert.That(DuelPush.ParseTz("99999"), Is.EqualTo(DuelPush.DefaultTzMinutes));
            Assert.That(DuelPush.FormatTz(TimeSpan.FromHours(1)), Is.EqualTo("60"));
            Assert.That(DuelPush.FormatTz(TimeSpan.FromMinutes(-210)), Is.EqualTo("-210"));
            Assert.That(DuelPush.ParseTz(DuelPush.FormatTz(TimeSpan.FromMinutes(345))), Is.EqualTo(345));
        }

        [Test]
        public void SenderPaceIsLimited()
        {
            Assert.That(DuelPush.AllowSend("", "A", Now, out var log));
            Assert.That(log, Is.EqualTo(Now + ":A"));
            // 10 s entre deux envois, même à deux amis différents.
            Assert.That(DuelPush.AllowSend(log, "B", Now + 9, out _), Is.False);
            Assert.That(DuelPush.AllowSend(log, "B", Now + 10, out log));
            // 2 minutes avant de relancer le même ami.
            Assert.That(DuelPush.AllowSend(log, "A", Now + 119, out _), Is.False);
            Assert.That(DuelPush.AllowSend(log, "A", Now + 120, out log));
            Assert.That(log.Split(','), Has.Length.EqualTo(3));
        }

        [Test]
        public void SenderDailyCaps()
        {
            string log = "";
            long t = Now;
            int sentToA = 0;
            for (int i = 0; i < 200; i++, t += 200)
                if (DuelPush.AllowSend(log, "A", t, out var next)) { log = next; sentToA++; }
            Assert.That(sentToA, Is.EqualTo(DuelPush.MaxSameTargetPerDay), "5 par jour au même ami");
            log = "";
            int total = 0;
            for (int i = 0; i < 100; i++)
                if (DuelPush.AllowSend(log, "F" + i, Now + i * 20, out var next)) { log = next; total++; }
            Assert.That(total, Is.EqualTo(DuelPush.MaxSentPerDay), "20 par jour en tout");
            // Les envois de plus de 24 h sont oubliés.
            Assert.That(DuelPush.AllowSend(log, "Z", Now + 86400 + 2000, out var fresh));
            Assert.That(fresh.Split(','), Has.Length.LessThan(DuelPush.MaxSentPerDay));
        }

        [Test]
        public void DamagedLogIsIgnoredNotBlocking()
        {
            Assert.That(DuelPush.AllowSend("abc,:,12:,x:y," + (Now + 99999) + ":A", "A", Now, out var log));
            Assert.That(log, Is.EqualTo(Now + ":A"));
            Assert.That(DuelPush.AllowReceive("abc,,-5," + (Now + 99999), Now, out var r));
            Assert.That(r, Is.EqualTo(Now.ToString()));
        }

        [Test]
        public void ReceiverGetsTenAtMostPerDay()
        {
            string log = "";
            int received = 0;
            for (int i = 0; i < 30; i++)
                if (DuelPush.AllowReceive(log, Now + i * 60, out var next)) { log = next; received++; }
            Assert.That(received, Is.EqualTo(DuelPush.MaxReceivedPerDay));
            Assert.That(DuelPush.AllowReceive(log, Now + 86400 + 1, out _), "le lendemain, c'est reparti");
        }

        [Test]
        public void TokensAndIdsAreChecked()
        {
            Assert.That(DuelPush.IsDeviceToken(new string('a', 64)));
            Assert.That(DuelPush.IsDeviceToken(new string('F', 160)));
            Assert.That(DuelPush.IsDeviceToken(new string('a', 63)), Is.False);
            Assert.That(DuelPush.IsDeviceToken(new string('g', 64)), Is.False);
            Assert.That(DuelPush.IsDeviceToken(""), Is.False);
            Assert.That(DuelPush.IsDeviceToken(null), Is.False);
            Assert.That(DuelPush.IsPlayerId("Ab12cd"));
            Assert.That(DuelPush.IsPlayerId("ab/../c"), Is.False);
            Assert.That(DuelPush.IsPlayerId("\"x\""), Is.False);
            Assert.That(DuelPush.IsPlayerId(new string('a', 65)), Is.False);
        }

        [TestCase("Léo", "Léo")]
        [TestCase("Léo#1234", "Léo")]
        [TestCase("merde", "Un ami")]
        [TestCase("   ", "Un ami")]
        [TestCase("UnPseudoBeaucoupTropLongPourUneNotification", "UnPseudoBeaucoup")]
        [TestCase("bit.ly/x", "bitlyx")]
        [TestCase("Zoé\u202Eabc", "Zoéabc")]
        [TestCase("ab", "Un ami")]
        public void InviterNameUsesTheLeaderboardFilter(string raw, string shown)
        {
            Assert.That(DuelPush.DisplayName(raw, "Un ami"), Is.EqualTo(shown));
        }

        [Test]
        public void TextsOfTheSettingAndTheQuestion()
        {
            // Le joueur doit savoir que le même interrupteur coupe aussi les notifications de duel.
            string before = Loc.Language;
            try
            {
                Loc.Language = "fr";
                StringAssert.Contains("duel", Loc.Get("notifications_duel").ToLowerInvariant());
                StringAssert.Contains("duel", Loc.Get("notif_ask_body_duel"));
                // Sans notification de duel (Android, ou interrupteur éteint) : on ne promet pas les duels.
                StringAssert.DoesNotContain("duel", Loc.Get("notifications").ToLowerInvariant());
                StringAssert.DoesNotContain("duel", Loc.Get("notif_ask_body"));
            }
            finally { Loc.Language = before; }
        }
    }
}
