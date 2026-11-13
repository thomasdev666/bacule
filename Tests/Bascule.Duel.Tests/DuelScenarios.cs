using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bascule.Core;
using Bascule.Duel;
using NUnit.Framework;
using Phase = Bascule.Duel.DuelController.Phase;

namespace Bascule.DuelTests
{
    /// <summary>
    /// Toutes les façons de faire un duel, jouées par le vrai DuelController sur un faux réseau, avant chaque envoi à
    /// Apple (Tools/verif_publication.py). Chaque scénario vérifie que le duel démarre, se termine, et que les deux
    /// téléphones donnent le même résultat (jamais deux gagnants, jamais deux perdants).
    /// </summary>
    public class DuelScenarios
    {
        SynchronizationContext _previous;
        const string HostFriend = "4K7Q2M", GuestFriend = "9PXR3T";

        [SetUp]
        public void SetUp()
        {
            _previous = SynchronizationContext.Current;
            UnityEngine.Debug.Warnings.Clear();
        }

        [TearDown]
        public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previous);

        static World NewWorld(NetConditions net = null, int seed = 1)
        {
            var w = new World(net, seed);
            SynchronizationContext.SetSynchronizationContext(w.Sim.Context);
            return w;
        }

        static (World w, Device host, Device guest) Pair(NetConditions net = null, float hostFall = float.PositiveInfinity,
            float guestFall = float.PositiveInfinity, int seed = 1)
        {
            var w = NewWorld(net, seed);
            var host = w.AddDevice("Hote", HostFriend, hostFall);
            var guest = w.AddDevice("Ami", GuestFriend, guestFall);
            return (w, host, guest);
        }

        static void HostReady(World w, Device host)
        {
            host.Host();
            Assert.True(w.RunUntil(() => host.C.Current == Phase.Hosting, 30), "l'hôte n'a pas de code : " + host);
            Assert.False(string.IsNullOrEmpty(host.C.Code));
        }

        static void StartMatch(World w, Device host, Device guest, bool byFriendCode, float within)
        {
            float t0 = w.Sim.Now;
            guest.Join(byFriendCode ? host.FriendCode : host.C.Code);
            Assert.True(w.RunUntil(() => host.InMatch && guest.InMatch, within),
                $"le duel n'a pas démarré en {within} s ({w.Net}) : {host} / {guest}");
            TestContext.Progress.WriteLine($"  départ en {w.Sim.Now - t0:0.0} s ({w.Net})");
        }

        static void Finish(World w, Device a, Device b, float within = 260f)
        {
            Assert.True(w.RunUntil(() => a.Done && b.Done, within), $"pas de résultat des deux côtés : {a} / {b}");
            AssertConsistent(a, b);
        }

        static void AssertConsistent(Device a, Device b)
        {
            var x = a.C.Outcome;
            var y = b.C.Outcome;
            string why = $"{a.Name} {x} ({a.C.MyScore}) / {b.Name} {y} ({b.C.MyScore})";
            if (x == DuelOutcome.Draw) Assert.AreEqual(DuelOutcome.Draw, y, "égalité d'un seul côté : " + why);
            else Assert.AreEqual(x == DuelOutcome.Win ? DuelOutcome.Lose : DuelOutcome.Win, y, "résultats contradictoires : " + why);
            Assert.AreEqual(1, a.Results, a.Name + " : résultat donné " + a.Results + " fois");
            Assert.AreEqual(1, b.Results, b.Name + " : résultat donné " + b.Results + " fois");
        }

        // ------------------------------------------------------------------ Démarrage

        [Test]
        public void SideBySideSameWifiStartsFastAndEnds()
        {
            var (w, host, guest) = Pair(guestFall: 20f);
            HostReady(w, host);
            StartMatch(w, host, guest, byFriendCode: false, within: 5f);
            Finish(w, host, guest);
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome);
            Assert.AreEqual(DuelOutcome.Lose, guest.C.Outcome);
            Assert.IsEmpty(host.Notices.Where(n => n.Contains("lost") || n.Contains("left")), "message d'erreur côté hôte");
            Assert.IsEmpty(guest.Notices, "message d'erreur côté ami");
        }

        [Test]
        public void JoinByFriendCodeFollowsTheOpenDuel()
        {
            var (w, host, guest) = Pair(hostFall: 25f);
            HostReady(w, host);
            w.RunFor(1f); // code ami publié
            StartMatch(w, host, guest, byFriendCode: true, within: 6f);
            Finish(w, host, guest);
            Assert.AreEqual(DuelOutcome.Win, guest.C.Outcome);
            Assert.False(w.OpenDuels.ContainsKey(HostFriend), "le duel reste ouvert sous le code ami après le départ");
        }

        [Test]
        public void LinkArrivingBeforeTheJoinReturnsStillStarts()
        {
            var net = NetConditions.Wifi();
            net.LinkDelay = 0.05f;
            net.JoinReturn = 3f;
            var (w, host, guest) = Pair(net, guestFall: 15f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 6f);
            Finish(w, host, guest);
        }

        [Test]
        public void SlowMobileLinkWaitsInsteadOfFailing()
        {
            var net = NetConditions.SlowMobile();
            net.LinkDelay = 18f; // relais très lent : avant la 1.4.1, « connexion perdue » au bout de 8 s
            var (w, host, guest) = Pair(net, hostFall: 30f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 30f);
            Finish(w, host, guest);
            Assert.AreEqual(DuelOutcome.Win, guest.C.Outcome);
        }

        [Test]
        public void ColdStartFromInviteLinkWaitsForServices()
        {
            // Jeu lancé par le lien : les services (classement, codes amis) se connectent encore pendant 6 s.
            var net = NetConditions.Wifi();
            var (w, host, guest) = Pair(net, guestFall: 12f);
            HostReady(w, host);
            w.RunFor(1f);
            net.OnlineAt = w.Sim.Now + 6f;
            StartMatch(w, host, guest, byFriendCode: true, within: 12f);
            Finish(w, host, guest);
            Assert.AreEqual(1, guest.Net.Joins, "l'ami a d'abord essayé le code ami comme code de partie");
        }

        [Test]
        public void NeverLinkingGivesUpInBoundedTimeAndHostKeepsWaiting()
        {
            var net = NetConditions.Wifi();
            net.NeverLink = true;
            var (w, host, guest) = Pair(net);
            HostReady(w, host);
            float t0 = w.Sim.Now;
            guest.Join(host.C.Code);
            Assert.True(w.RunUntil(() => guest.C.Current == Phase.None, 90f), "l'ami reste bloqué sur « Connexion… » : " + guest);
            TestContext.Progress.WriteLine($"  abandon de l'ami au bout de {w.Sim.Now - t0:0} s");
            Assert.LessOrEqual(w.Sim.Now - t0, 75f, "trop long avant d'abandonner");
            Assert.IsNotEmpty(guest.Notices, "l'ami doit voir un message");
            w.RunFor(2f);
            Assert.AreEqual(Phase.Hosting, host.C.Current, "l'hôte doit continuer d'attendre : " + host);

            // Le réseau revient : un autre essai démarre normalement, avec le même hôte.
            net.NeverLink = false;
            StartMatch(w, host, guest, false, 6f);
            guest.Game.FallAt = 10f;
            Finish(w, host, guest);
        }

        [Test]
        public void CancelThenJoinAgainImmediatelyIsNotBrokenByTheOldAttempt()
        {
            var (w, host, guest) = Pair(guestFall: 10f);
            HostReady(w, host);
            guest.Join(host.C.Code);
            w.RunFor(0.1f);
            guest.C.Leave();             // l'ami touche une autre invitation…
            guest.Join(host.C.Code);     // …et rejoint tout de suite
            Assert.True(w.RunUntil(() => host.InMatch && guest.InMatch, 8f), $"{host} / {guest}");
            Finish(w, host, guest);
        }

        [Test]
        public void WrongCodeThenRightCodeIsNotDisturbedByTheOldAnswer()
        {
            // L'ami tape un mauvais code, annule tout de suite et tape le bon : la réponse « code invalide » du premier
            // essai arrive pendant le second et ne doit ni le relancer ni l'arrêter.
            var (w, host, guest) = Pair(guestFall: 10f);
            HostReady(w, host);
            guest.Join("ZZZZZZ");
            w.RunFor(0.2f);
            guest.C.Leave();
            guest.Join(host.C.Code);
            Assert.True(w.RunUntil(() => host.InMatch && guest.InMatch, 6f), $"{host} / {guest}");
            Assert.AreEqual(2, guest.Net.Joins, "nouvel essai déclenché par la vieille réponse");
            Assert.IsEmpty(guest.Notices);
            Finish(w, host, guest);
        }

        [Test]
        public void HostCancelsWhileCreatingThenHostsAgain()
        {
            var (w, host, guest) = Pair(hostFall: 10f);
            host.Host();
            w.RunFor(0.3f);
            host.C.Leave();
            host.Host();
            Assert.True(w.RunUntil(() => host.C.Current == Phase.Hosting, 10f), host.ToString());
            w.RunFor(3f); // l'ancienne création ne doit rien défaire
            Assert.AreEqual(Phase.Hosting, host.C.Current);
            Assert.True(w.Sessions.ContainsKey(host.C.Code), "le code affiché ne mène à aucune partie");
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest);
        }

        [Test]
        public void CreateFailureShowsAMessageAndCanBeRetried()
        {
            var net = NetConditions.Wifi();
            net.CreateFailures = 1;
            var (w, host, guest) = Pair(net, guestFall: 8f);
            host.Host();
            Assert.True(w.RunUntil(() => host.C.Current == Phase.None, 10f));
            Assert.Contains("duel_create_failed", host.Notices);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest);
        }

        // ------------------------------------------------------------------ Passage par Snap / Messages (arrière-plan)

        [Test]
        public void HostShortlyAwayKeepsTheSameGame()
        {
            var (w, host, guest) = Pair(guestFall: 10f);
            HostReady(w, host);
            string code = host.C.Code;
            host.Pause();
            w.RunFor(4f);
            host.Resume();
            w.RunFor(3f);
            Assert.AreEqual(1, host.Net.Created, "partie recréée pour rien");
            Assert.AreEqual(code, host.C.Code);
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest);
        }

        [TestCase(12f)]
        [TestCase(20f)]
        [TestCase(40f)]
        public void HostSendsTheCodeBySnapAndComesBack(float away)
        {
            // L'hôte part envoyer le code ; l'ami touche le lien (code ami) pendant ce temps. Le relais oublie l'hôte au
            // bout de 10 s : au retour, l'hôte recrée sa partie et le code ami y mène.
            var (w, host, guest) = Pair(guestFall: 15f);
            HostReady(w, host);
            w.RunFor(1f);
            host.Pause();
            w.RunFor(Math.Min(5f, away - 1f));
            guest.Join(host.FriendCode);
            w.RunFor(away - Math.Min(5f, away - 1f));
            host.Resume();
            bool started = w.RunUntil(() => host.InMatch && guest.InMatch, 30f);
            if (!started && away > 30f)
            {
                // Absence très longue : l'ami a abandonné avec un message ; il rejoint en touchant le lien à nouveau.
                Assert.AreEqual(Phase.None, guest.C.Current, guest.ToString());
                Assert.IsNotEmpty(guest.Notices);
                Assert.True(w.RunUntil(() => host.C.Current == Phase.Hosting, 10f), host.ToString());
                w.RunFor(1f);
                StartMatch(w, host, guest, true, 8f);
            }
            else Assert.True(started, $"pas de départ après {away} s d'absence de l'hôte : {host} / {guest}");
            Assert.AreEqual(2, host.Net.Created, "l'hôte devait recréer sa partie une fois");
            Finish(w, host, guest);
        }

        [Test]
        public void GuestAwayBeforeTheLinkRetriesOnReturn()
        {
            var net = NetConditions.Wifi();
            net.LinkDelay = 3f;
            var (w, host, guest) = Pair(net, hostFall: 12f);
            HostReady(w, host);
            guest.Join(host.C.Code);
            w.RunFor(1.5f); // partie rejointe, liaison pas encore faite
            guest.Pause();
            w.RunFor(14f);   // le relais oublie l'ami
            guest.Resume();
            Assert.True(w.RunUntil(() => host.InMatch && guest.InMatch, 20f), $"{host} / {guest}");
            Finish(w, host, guest);
        }

        // ------------------------------------------------------------------ Pendant la partie

        [TestCase(1.5f)]
        [TestCase(4f)]
        [TestCase(6.5f)]
        public void ShortExitDuringTheMatchIsNotAForfeit(float away)
        {
            // Centre de contrôle, notification : avant la 1.4.1, c'était une défaite immédiate.
            var (w, host, guest) = Pair(guestFall: 40f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            w.RunFor(12f);
            guest.Pause();
            w.RunFor(away);
            guest.Resume();
            Finish(w, host, guest);
            Assert.False(host.C.ByForfeit || guest.C.ByForfeit, $"abandon après {away} s dehors");
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome);
        }

        [TestCase(7.5f)]
        [TestCase(9f)]
        [TestCase(25f)]
        public void LongExitDuringTheMatchLosesOnBothScreens(float away)
        {
            var (w, host, guest) = Pair(guestFall: 100f, hostFall: 90f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            w.RunFor(15f);
            guest.Pause();
            w.RunFor(away);
            guest.Resume();
            Finish(w, host, guest, 30f);
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome, "celui qui est resté gagne");
            Assert.AreEqual(DuelOutcome.Lose, guest.C.Outcome, "celui qui est parti perd (avant : il pouvait gagner aussi)");
        }

        [Test]
        public void QuitByTheFlagIsAForfeit()
        {
            var (w, host, guest) = Pair();
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            w.RunFor(15f);
            guest.C.Forfeit();
            Finish(w, host, guest, 20f);
            Assert.AreEqual(DuelOutcome.Lose, guest.C.Outcome);
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome);
            Assert.True(host.C.ByForfeit);
        }

        [Test]
        public void PhoneDiesDuringTheMatchOtherWinsAfterTheTimeout()
        {
            var (w, host, guest) = Pair();
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            w.RunFor(20f);
            w.Devices.Remove(guest); // batterie vide : plus rien, pas même un « au revoir »
            float t0 = w.Sim.Now;
            Assert.True(w.RunUntil(() => host.Done, 30f), host.ToString());
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome);
            Assert.LessOrEqual(w.Sim.Now - t0, w.Tuning.duel.timeoutSeconds + 3f);
        }

        [Test]
        public void QuittingWhileWaitingKeepsTheRealResult()
        {
            // L'hôte lâche ; l'ami pose son dernier poids et attend la fin de la chute de l'hôte, puis quitte.
            // Avant la 1.4.1 : les deux perdaient.
            var (w, host, guest) = Pair(hostFall: 30f);
            host.Game.FallAnimation = 12f;
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            Assert.True(w.RunUntil(() => guest.C.Current == Phase.Waiting, 60f), guest.ToString());
            guest.C.Forfeit(); // drapeau pendant l'attente (GameApp.DuelAskQuit)
            Finish(w, host, guest, 30f);
            Assert.AreEqual(DuelOutcome.Win, guest.C.Outcome);
            Assert.AreEqual(DuelOutcome.Lose, host.C.Outcome);
        }

        [Test]
        public void TimeUpBestScoreWins()
        {
            var (w, host, guest) = Pair();
            host.Game.ScoreRate = 25f;
            guest.Game.ScoreRate = 20f;
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest, w.Tuning.duel.timeLimitSeconds + 30f);
            Assert.True(host.C.TimeUp && guest.C.TimeUp);
            Assert.AreEqual(DuelOutcome.Win, host.C.Outcome);
        }

        [Test]
        public void SimultaneousFallsAgreeOnBothPhones()
        {
            var (w, host, guest) = Pair(NetConditions.WifiAndMobile(), hostFall: 30f, guestFall: 30.1f);
            host.Game.ScoreRate = 21f;
            HostReady(w, host);
            StartMatch(w, host, guest, false, 10f);
            Finish(w, host, guest);
        }

        [Test]
        public void RematchStartsASecondDuel()
        {
            var (w, host, guest) = Pair(guestFall: 10f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest);
            host.Game.FallAt = 8f;
            guest.Game.FallAt = float.PositiveInfinity;
            host.Results = guest.Results = 0;
            w.RunFor(1.5f);
            guest.C.AskRematch();
            w.RunFor(0.5f);
            host.C.AskRematch();
            Assert.True(w.RunUntil(() => host.InMatch && guest.InMatch, 5f), $"{host} / {guest}");
            Finish(w, host, guest);
            Assert.AreEqual(DuelOutcome.Win, guest.C.Outcome);
            Assert.AreEqual(2, host.Game.Starts);
            Assert.AreEqual(2, guest.Game.Starts);
        }

        [Test]
        public void LeavingTheResultScreenTellsTheOther()
        {
            var (w, host, guest) = Pair(guestFall: 10f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 5f);
            Finish(w, host, guest);
            host.C.AskRematch();
            w.RunFor(0.5f);
            guest.C.Leave();
            w.RunFor(2f);
            Assert.False(host.C.TheyWantRematch);
            Assert.AreEqual(Phase.None, guest.C.Current);
            Assert.AreEqual(Phase.Results, host.C.Current);
        }

        [Test]
        public void HeavyLossOnLiveViewStillEndsTheSame()
        {
            var net = NetConditions.SlowMobile();
            net.Loss = 0.35;
            var (w, host, guest) = Pair(net, hostFall: 45f);
            HostReady(w, host);
            StartMatch(w, host, guest, false, 20f);
            Finish(w, host, guest);
            Assert.AreEqual(DuelOutcome.Win, guest.C.Outcome);
            Assert.Greater(host.View.Applies, 100, "la vue en direct ne bouge pas");
        }

        // ------------------------------------------------------------------ Toutes les combinaisons

        static IEnumerable<TestCaseData> Matrix()
        {
            var nets = new Func<NetConditions>[]
            {
                NetConditions.Wifi, NetConditions.WifiAndMobile, NetConditions.SlowMobile,
                () => { var n = NetConditions.Wifi(); n.Name = "liaison-avant-jointure"; n.LinkDelay = 0.02f; n.JoinReturn = 2.5f; return n; },
                () => { var n = NetConditions.SlowMobile(); n.Name = "relais-12s"; n.LinkDelay = 12f; return n; },
            };
            var endings = new (string name, float hostFall, float guestFall)[]
            {
                ("ami-lache", float.PositiveInfinity, 25f),
                ("hote-lache", 18f, float.PositiveInfinity),
                ("presque-ensemble", 40f, 40.2f),
            };
            foreach (var n in nets)
            foreach (var e in endings)
            foreach (bool friend in new[] { false, true })
                yield return new TestCaseData(n, e.hostFall, e.guestFall, friend)
                    .SetName($"Duel_{n().Name}_{e.name}_{(friend ? "codeami" : "codepartie")}");
        }

        [TestCaseSource(nameof(Matrix))]
        public void EveryNetworkEveryEnding(Func<NetConditions> net, float hostFall, float guestFall, bool byFriend)
        {
            var (w, host, guest) = Pair(net(), hostFall, guestFall);
            HostReady(w, host);
            w.RunFor(1f);
            StartMatch(w, host, guest, byFriend, w.Net.JoinReturn + w.Net.LinkDelay + 10f);
            Finish(w, host, guest);
            if (Math.Abs(hostFall - guestFall) > 1f)
                Assert.AreEqual(hostFall < guestFall ? DuelOutcome.Lose : DuelOutcome.Win, host.C.Outcome, "le premier qui lâche perd");
        }

        [Test]
        public void RandomDuelsAlwaysAgree([Range(1, 120)] int seed)
        {
            // Réseau, moment de la chute, sorties de l'appli : tirés au hasard (graine fixe : rejouable).
            var r = new Random(seed * 7919);
            var net = new NetConditions
            {
                Name = "hasard-" + seed,
                SignIn = (float)(0.1 + r.NextDouble() * 1.5),
                Create = (float)(0.3 + r.NextDouble() * 3),
                JoinCheck = (float)(0.2 + r.NextDouble() * 1.5),
                Latency = (float)(0.01 + r.NextDouble() * 0.3),
                Jitter = (float)(r.NextDouble() * 0.12),
                Loss = r.NextDouble() * 0.25,
                LinkDelay = (float)(r.NextDouble() < 0.3 ? r.NextDouble() * 0.2 : r.NextDouble() * 15),
            };
            net.JoinReturn = net.JoinCheck + (float)(r.NextDouble() * 2.5);
            float hostFall = r.NextDouble() < 0.1 ? float.PositiveInfinity : (float)(5 + r.NextDouble() * 60);
            float guestFall = r.NextDouble() < 0.1 ? float.PositiveInfinity : (float)(5 + r.NextDouble() * 60);
            var (w, host, guest) = Pair(net, hostFall, guestFall, seed);
            host.Game.ScoreRate = 15 + r.Next(10);
            guest.Game.ScoreRate = 15 + r.Next(10);
            bool friend = r.NextDouble() < 0.5;

            HostReady(w, host);
            w.RunFor(1f);
            // Parfois l'hôte part envoyer le code avant que l'ami rejoigne.
            if (r.NextDouble() < 0.3)
            {
                float away = (float)(1 + r.NextDouble() * 14);
                host.Pause();
                w.RunFor(away);
                host.Resume();
                Assert.True(w.RunUntil(() => host.C.Current == Phase.Hosting, 20f), host.ToString());
                w.RunFor(1f);
            }
            StartMatch(w, host, guest, friend, net.JoinReturn + net.LinkDelay + 30f);
            // Parfois un passage éclair dans le Centre de contrôle pendant la partie.
            if (r.NextDouble() < 0.3)
            {
                w.RunFor((float)(r.NextDouble() * 8));
                var who = r.NextDouble() < 0.5 ? host : guest;
                if (who.InMatch)
                {
                    who.Pause();
                    w.RunFor((float)(0.3 + r.NextDouble() * 4));
                    who.Resume();
                }
            }
            Finish(w, host, guest);
        }
    }
}
