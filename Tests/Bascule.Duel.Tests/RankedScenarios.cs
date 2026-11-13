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
    /// Duel classé (1.5) : recherche d'un adversaire sans code, fantômes, enregistrement. Le vrai DuelController sur le
    /// faux réseau, comme DuelScenarios. Personne ne doit jamais rester bloqué sur « Recherche… ».
    /// </summary>
    public class RankedScenarios
    {
        SynchronizationContext _previous;

        [SetUp]
        public void SetUp() { _previous = SynchronizationContext.Current; UnityEngine.Debug.Warnings.Clear(); }

        [TearDown]
        public void TearDown() => SynchronizationContext.SetSynchronizationContext(_previous);

        static World NewWorld(NetConditions net = null, int seed = 1)
        {
            var w = new World(net, seed);
            SynchronizationContext.SetSynchronizationContext(w.Sim.Context);
            return w;
        }

        static void AssertPair(Device a, Device b)
        {
            Assert.True(a.Done && b.Done, $"pas de résultat des deux côtés : {a} / {b}");
            var x = a.C.Outcome; var y = b.C.Outcome;
            if (x == DuelOutcome.Draw) Assert.AreEqual(DuelOutcome.Draw, y);
            else Assert.AreEqual(x == DuelOutcome.Win ? DuelOutcome.Lose : DuelOutcome.Win, y, $"{a.Name} {x} / {b.Name} {y}");
            Assert.AreEqual(1, a.Results); Assert.AreEqual(1, b.Results);
        }

        /// <summary>Un duel classé entre deux vrais joueurs ; renvoie l'enregistrement complet de celui qui a lâché.</summary>
        static GhostRecording RecordedDuel(World w, float loserFall, string loserName = "Lea")
        {
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice(loserName, "BBBBBB", loserFall);
            a.Search();
            w.RunFor(4f);
            b.Search();
            Assert.True(w.RunUntil(() => a.InMatch && b.InMatch, 15f), $"{a} / {b}");
            Assert.True(w.RunUntil(() => a.Done && b.Done, 260f));
            AssertPair(a, b);
            Assert.AreEqual(1, b.Recordings.Count, "celui qui a lâché a un enregistrement");
            w.Devices.Remove(a); w.Devices.Remove(b);
            return b.Recordings[0];
        }

        // ------------------------------------------------------------------ Recherche

        [Test]
        public void TwoSearchersAreMatchedWithoutAnyCode()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB", 20f);
            a.Search();
            w.RunFor(5f);
            Assert.AreEqual(Phase.Searching, a.C.Current);
            float t0 = w.Sim.Now;
            b.Search();
            Assert.True(w.RunUntil(() => a.InMatch && b.InMatch, 8f), $"{a} / {b}");
            TestContext.Progress.WriteLine($"  adversaire trouvé en {w.Sim.Now - t0:0.0} s");
            Assert.True(a.C.Ranked && b.C.Ranked);
            Assert.False(a.C.VsGhost || b.C.VsGhost);
            Assert.AreEqual("Lea", a.C.TheirName);
            Assert.AreEqual("Max", b.C.TheirName);
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            AssertPair(a, b);
            Assert.AreEqual(DuelOutcome.Win, a.C.Outcome);
            // Pas de revanche en classé.
            a.C.AskRematch(); b.C.AskRematch();
            w.RunFor(3f);
            Assert.AreEqual(Phase.Results, a.C.Current);
            Assert.IsEmpty(w.RankedOpen, "une partie classée reste ouverte après le duel");
        }

        [Test]
        public void SearchingAtTheSameInstantStillEndsWell([Range(1, 40)] int seed)
        {
            // Les deux ne trouvent personne et ouvrent chacun sa partie : l'un referme la sienne et rejoint l'autre.
            var w = NewWorld(null, seed);
            var a = w.AddDevice("Max", "AAAAAA", 30f);
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search(); b.Search();
            bool matched = w.RunUntil(() => a.InMatch && b.InMatch, 26f);
            if (!matched)
            {
                // Très rare : chacun voit « personne » (et se voit proposer un fantôme). Jamais bloqué.
                Assert.True(w.RunUntil(() => a.C.Current == Phase.None && b.C.Current == Phase.None, 30f), $"bloqués : {a} / {b}");
                Assert.AreEqual(1, a.NoOpponent); Assert.AreEqual(1, b.NoOpponent);
                Assert.Inconclusive("pas trouvés cette fois (graine " + seed + ")");
            }
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            AssertPair(a, b);
        }

        [Test]
        public void SimultaneousSearchUsuallyMatches()
        {
            int ok = 0, total = 60;
            for (int seed = 100; seed < 100 + total; seed++)
            {
                var w = NewWorld(null, seed);
                var a = w.AddDevice("Max", "AAAAAA");
                var b = w.AddDevice("Lea", "BBBBBB");
                a.Search(); b.Search();
                if (w.RunUntil(() => a.InMatch && b.InMatch, 26f)) ok++;
            }
            TestContext.Progress.WriteLine($"  arrivés en même temps : {ok}/{total} duels trouvés");
            Assert.AreEqual(total, ok, "deux joueurs arrivés en même temps se ratent trop souvent");
        }

        [Test]
        public void AloneGetsNoOpponentAfterTheSearchTime()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA");
            a.Search();
            Assert.True(w.RunUntil(() => a.NoOpponent == 1, 30f), a.ToString());
            Assert.That(w.Sim.Now, Is.InRange(w.Tuning.league.searchSeconds, w.Tuning.league.searchSeconds + 1.5f));
            Assert.AreEqual(Phase.None, a.C.Current);
            w.RunFor(3f);
            Assert.IsEmpty(w.RankedOpen, "ma partie reste ouverte alors que j'ai arrêté de chercher");
            Assert.AreEqual(1, a.NoOpponent);
        }

        [Test]
        public void CancelledSearchLeavesNothingBehind()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search();
            w.RunFor(6f);
            a.C.Leave();
            w.RunFor(2f);
            Assert.IsEmpty(w.RankedOpen);
            b.Search();
            Assert.True(w.RunUntil(() => b.NoOpponent == 1, 30f), "Lea a rejoint la partie abandonnée de Max : " + b);
            Assert.AreEqual(Phase.None, a.C.Current);
        }

        [Test]
        public void SearchThenCancelThenSearchAgainIsClean()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA", 15f);
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search();
            w.RunFor(0.3f);
            a.C.Leave();
            a.Search();
            w.RunFor(5f);
            b.Search();
            Assert.True(w.RunUntil(() => a.InMatch && b.InMatch, 22f), $"{a} / {b}");
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            AssertPair(a, b);
        }

        [Test]
        public void OpponentFoundButNeverLinkedIsBounded()
        {
            var net = NetConditions.Wifi();
            net.NeverLink = true;
            var w = NewWorld(net);
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search();
            w.RunFor(5f);
            b.Search();
            Assert.True(w.RunUntil(() => a.C.Current == Phase.None && b.C.Current == Phase.None, 60f), $"bloqués : {a} / {b}");
            Assert.AreEqual(1, a.NoOpponent);
            Assert.AreEqual(1, b.NoOpponent);
            w.RunFor(3f);
            Assert.IsEmpty(w.RankedOpen);
        }

        [Test]
        public void ServerErrorDuringSearchRetriesQuietly()
        {
            var net = NetConditions.Wifi();
            net.SearchFailures = 2;
            var w = NewWorld(net);
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB", 12f);
            a.Search();
            w.RunFor(6f);
            b.Search();
            Assert.True(w.RunUntil(() => a.InMatch && b.InMatch, 22f), $"{a} / {b}");
            Assert.IsEmpty(a.Notices);
        }

        [Test]
        public void LeavingTheAppWhileSearchingStartsAFreshSearch()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA", 14f);
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search();
            w.RunFor(5f);
            a.Pause();
            w.RunFor(15f); // le relais oublie Max : sa partie ouverte est morte
            a.Resume();
            w.RunFor(4f);
            b.Search();
            Assert.True(w.RunUntil(() => a.InMatch && b.InMatch, 14f), $"{a} / {b}");
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            AssertPair(a, b);
        }

        [Test]
        public void ThreeSearchersNobodyIsStuck([Range(1, 30)] int seed)
        {
            var r = new Random(seed * 31);
            var w = NewWorld(r.NextDouble() < 0.5 ? NetConditions.Wifi() : NetConditions.WifiAndMobile(), seed);
            var ds = new[] { w.AddDevice("Max", "AAAAAA", 20f), w.AddDevice("Lea", "BBBBBB", 30f), w.AddDevice("Tom", "CCCCCC", 40f) };
            var starts = ds.Select(_ => (float)(r.NextDouble() * 12)).ToArray();
            var started = new bool[3];
            float end = 200f;
            while (w.Sim.Now < end)
            {
                for (int i = 0; i < 3; i++) if (!started[i] && w.Sim.Now >= starts[i]) { started[i] = true; ds[i].Search(); }
                w.Step();
            }
            foreach (var d in ds)
                Assert.True(d.C.Current == Phase.None || d.C.Current == Phase.Results, "bloqué : " + d);
            // Ceux qui ont joué : un adversaire qui a joué aussi, avec le résultat inverse.
            foreach (var d in ds.Where(x => x.Done))
            {
                var other = ds.FirstOrDefault(x => x != d && x.Done && x.C.TheirName == d.Name && d.C.TheirName == x.Name);
                Assert.NotNull(other, "adversaire introuvable pour " + d);
                AssertPair(d, other);
            }
            Assert.LessOrEqual(ds.Count(x => x.Done), 2);
            Assert.IsEmpty(w.RankedOpen);
        }

        // ------------------------------------------------------------------ Enregistrement et fantômes

        [Test]
        public void TheOneWhoFellGetsACompleteRecordingTheWinnerDoesNot()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB", 25f);
            a.Search(); w.RunFor(4f); b.Search();
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            var lost = b.Recordings.Single();
            Assert.True(lost.Usable(w.Tuning.duel, w.Tuning.league), "la partie de celle qui a lâché doit faire un fantôme");
            Assert.True(lost.Fell);
            Assert.AreEqual("Lea", lost.Who.Name);
            Assert.That(lost.SurvivedMs / 1000f, Is.EqualTo(25f).Within(0.3f));
            // Le gagnant s'est arrêté quand elle a lâché : partie interrompue, pas de fantôme.
            Assert.True(a.Recordings.Count == 0 || !a.Recordings[0].Usable(w.Tuning.duel, w.Tuning.league));
            // Environ 5 photos par seconde, pas 20.
            float seconds = lost.EndMs / 1000f;
            Assert.That(lost.Snapshots.Count, Is.InRange((int)(seconds * 3.5f), (int)(seconds * 6f) + 3));

            // Aller-retour par le format publié.
            var back = GhostRecording.FromBase64(lost.ToBase64());
            Assert.NotNull(back);
            Assert.AreEqual(lost.Seed, back.Seed);
            Assert.AreEqual(lost.FellMs, back.FellMs);
            Assert.AreEqual(lost.FinalScore, back.FinalScore);
            Assert.AreEqual(lost.Snapshots.Count, back.Snapshots.Count);
            Assert.AreEqual("Lea", back.Who.Name);
            Assert.True(back.Usable(w.Tuning.duel, w.Tuning.league));
            Assert.IsNull(GhostRecording.FromBase64("pas un fantôme"));
            Assert.IsNull(GhostRecording.Decode(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }

        [Test]
        public void FriendDuelsAndForfeitsAreNeverRecorded()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB", 12f);
            a.Host();
            Assert.True(w.RunUntil(() => a.C.Current == Phase.Hosting, 10f));
            b.Join(a.C.Code);
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            Assert.IsEmpty(a.Recordings); Assert.IsEmpty(b.Recordings);
            Assert.False(a.C.Ranked || b.C.Ranked);

            var w2 = NewWorld();
            var c = w2.AddDevice("Max", "AAAAAA");
            var d = w2.AddDevice("Lea", "BBBBBB");
            c.Search(); w2.RunFor(4f); d.Search();
            Assert.True(w2.RunUntil(() => c.InMatch && d.InMatch, 15f));
            w2.RunFor(20f);
            d.C.Forfeit();
            Assert.True(w2.RunUntil(() => c.Done && d.Done, 30f));
            Assert.True(d.C.IForfeited);
            Assert.IsEmpty(d.Recordings, "un abandon ne fait pas un fantôme");
            Assert.True(c.Recordings.Count == 0 || !c.Recordings[0].Usable(w2.Tuning.duel, w2.Tuning.league));
        }

        [Test]
        public void GhostFallsAtItsRecordedTimeAndThePlayerWins()
        {
            var w = NewWorld();
            var ghost = GhostRecording.FromBase64(RecordedDuel(w, 25f).ToBase64());
            var me = w.AddDevice("Tom", "CCCCCC");
            Assert.True(me.C.StartGhost(me.Hello, ghost));
            Assert.True(w.RunUntil(() => me.InMatch, 3f), me.ToString());
            Assert.True(me.C.Ranked && me.C.VsGhost);
            Assert.AreEqual("Lea", me.C.TheirName);
            Assert.AreEqual(ghost.Seed, me.C.Seed, "même graine : mêmes poids et mêmes rafales");
            float? sawFallAt = null;
            me.C.TheyFellEvent += () => sawFallAt = me.Game.RunTime;
            Assert.True(w.RunUntil(() => me.Done, 120f), me.ToString());
            Assert.AreEqual(DuelOutcome.Win, me.C.Outcome);
            Assert.False(me.C.ByForfeit);
            Assert.NotNull(sawFallAt);
            // Synchronisation : le fantôme lâche au même instant de MA partie que dans la sienne (à une image près).
            Assert.That(sawFallAt.Value, Is.EqualTo(ghost.FellMs / 1000f).Within(0.12f), "le fantôme n'est pas calé sur ma partie");
            Assert.AreEqual(ghost.FellScore, me.C.TheirScore);
            Assert.Greater(me.View.Applies, 100, "le fantôme ne bouge pas à l'écran");
            Assert.AreEqual(1, me.Results);
        }

        [Test]
        public void GustsOfTheGhostDuelAreTheGhostsOwn()
        {
            // Les rafales viennent de la graine : rejouer le fantôme donne exactement la même liste, aux mêmes temps.
            var w = NewWorld();
            var ghost = RecordedDuel(w, 40f);
            var me = w.AddDevice("Tom", "CCCCCC");
            me.C.StartGhost(me.Hello, ghost);
            Assert.True(w.RunUntil(() => me.InMatch, 3f));
            var mine = DuelRules.GustSchedule(me.C.Seed, w.Tuning.duel);
            var theirs = DuelRules.GustSchedule(ghost.Seed, w.Tuning.duel);
            Assert.AreEqual(theirs.Count, mine.Count);
            for (int i = 0; i < mine.Count; i++)
            {
                Assert.AreEqual(theirs[i].At, mine[i].At);
                Assert.AreEqual(theirs[i].Direction, mine[i].Direction);
                Assert.AreEqual(theirs[i].Strength, mine[i].Strength);
            }
            // Et le compte à rebours est celui de sa partie : « Partez ! » tombe au même temps de partie.
            Assert.AreEqual(ghost.Countdown, w.Tuning.duel.countdownSeconds, 0.01f);
        }

        [Test]
        public void PlayerFallingFirstLosesToTheGhost()
        {
            var w = NewWorld();
            var ghost = RecordedDuel(w, 40f);
            var me = w.AddDevice("Tom", "CCCCCC", 12f);
            me.C.StartGhost(me.Hello, ghost);
            float t0 = w.Sim.Now;
            Assert.True(w.RunUntil(() => me.Done, 60f), me.ToString());
            Assert.AreEqual(DuelOutcome.Lose, me.C.Outcome);
            // Résultat peu après ma chute (fenêtre du dernier poids du fantôme), pas à la fin de son enregistrement.
            Assert.LessOrEqual(w.Sim.Now - t0, 3f + 12f + 1.5f + w.Tuning.duel.finalWindowSeconds + 2f);
            // Ma partie perdue est complète : elle devient à son tour un fantôme.
            Assert.True(me.Recordings.Single().Usable(w.Tuning.duel, w.Tuning.league));
        }

        [Test]
        public void GhostAndPlayerFallingTogetherIsDecidedByScore()
        {
            var w = NewWorld();
            var ghost = RecordedDuel(w, 30f);
            var me = w.AddDevice("Tom", "CCCCCC", 30.1f, scoreRate: 40f);
            me.C.StartGhost(me.Hello, ghost);
            Assert.True(w.RunUntil(() => me.Done, 80f), me.ToString());
            Assert.AreEqual(DuelOutcome.Win, me.C.Outcome, "chutes simultanées : le meilleur score gagne");
        }

        [Test]
        public void TimeUpGhostEndsOnScore()
        {
            var w = NewWorld();
            var a = w.AddDevice("Max", "AAAAAA", scoreRate: 20f);
            var b = w.AddDevice("Lea", "BBBBBB", scoreRate: 22f);
            a.Search(); w.RunFor(4f); b.Search();
            Assert.True(w.RunUntil(() => a.Done && b.Done, 260f));
            Assert.True(a.C.TimeUp && b.C.TimeUp);
            var ghost = b.Recordings.Single();
            Assert.True(ghost.Usable(w.Tuning.duel, w.Tuning.league), "une partie tenue jusqu'au bout fait un fantôme");
            Assert.False(ghost.Fell);
            w.Devices.Clear();
            var me = w.AddDevice("Tom", "CCCCCC", scoreRate: 25f);
            me.C.StartGhost(me.Hello, GhostRecording.FromBase64(ghost.ToBase64()));
            Assert.True(w.RunUntil(() => me.Done, 260f), me.ToString());
            Assert.True(me.C.TimeUp);
            Assert.AreEqual(DuelOutcome.Win, me.C.Outcome);
            Assert.AreEqual(ghost.FinalScore, me.C.TheirScore);
        }

        [Test]
        public void QuittingAGhostDuelIsAForfeitAndLeavesCleanly()
        {
            var w = NewWorld();
            var ghost = RecordedDuel(w, 40f);
            var me = w.AddDevice("Tom", "CCCCCC");
            me.C.StartGhost(me.Hello, ghost);
            Assert.True(w.RunUntil(() => me.InMatch, 3f));
            w.RunFor(10f);
            me.C.Forfeit();
            w.RunFor(1f);
            Assert.AreEqual(DuelOutcome.Lose, me.C.Outcome);
            Assert.True(me.C.IForfeited);
            Assert.False(me.Link.Ghost, "le fantôme tourne encore");
            me.C.Leave();
            // Ensuite, un vrai duel classé marche normalement.
            var other = w.AddDevice("Zoe", "DDDDDD", 9f);
            me.Results = 0;
            me.Search(); w.RunFor(4f); other.Search();
            Assert.True(w.RunUntil(() => me.Done && other.Done, 120f), $"{me} / {other}");
            Assert.False(me.C.VsGhost);
            AssertPair(me, other);
        }

        [Test]
        public void ShortExitDuringAGhostDuelIsNotAForfeitLongOneIs()
        {
            var w = NewWorld();
            var ghost = RecordedDuel(w, 40f);
            var me = w.AddDevice("Tom", "CCCCCC");
            me.C.StartGhost(me.Hello, ghost);
            Assert.True(w.RunUntil(() => me.InMatch, 3f));
            w.RunFor(8f);
            me.Pause(); w.RunFor(3f); me.Resume();
            w.RunFor(2f);
            Assert.True(me.InMatch, "courte sortie : la partie continue : " + me);
            me.Pause(); w.RunFor(12f); me.Resume();
            w.RunFor(1f);
            Assert.True(me.Done);
            Assert.AreEqual(DuelOutcome.Lose, me.C.Outcome);
            Assert.True(me.C.IForfeited);
        }

        [Test]
        public void DamagedGhostIsRefused()
        {
            var w = NewWorld();
            var me = w.AddDevice("Tom", "CCCCCC");
            Assert.False(me.C.StartGhost(me.Hello, null));
            Assert.AreEqual(Phase.None, me.C.Current);
            var empty = new GhostRecording { Seed = 5, Countdown = 3 };
            Assert.False(empty.Usable(w.Tuning.duel, w.Tuning.league));
        }

        // ------------------------------------------------------------------ Points

        [Test]
        public void PointsFollowTheResult()
        {
            var w = NewWorld();
            var t = w.Tuning.league;
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB", 25f);
            a.Search(); w.RunFor(4f); b.Search();
            Assert.True(w.RunUntil(() => a.Done && b.Done, 120f));
            int pa = League.Points(a.C.Outcome, a.C.VsGhost, a.C.IForfeited, a.C.ByForfeit && a.C.Outcome == DuelOutcome.Win, a.C.MySeconds, t);
            int pb = League.Points(b.C.Outcome, b.C.VsGhost, b.C.IForfeited, b.C.ByForfeit && b.C.Outcome == DuelOutcome.Win, b.C.MySeconds, t);
            Assert.AreEqual(t.winPoints, pa);
            Assert.AreEqual(t.losePoints, pb);
        }

        [Test]
        public void QuickForfeitCannotBeFarmed()
        {
            // Deux complices : l'un abandonne dès le départ. Le « gagnant » n'a que les points d'une défaite, l'autre rien.
            var w = NewWorld();
            var t = w.Tuning.league;
            var a = w.AddDevice("Max", "AAAAAA");
            var b = w.AddDevice("Lea", "BBBBBB");
            a.Search(); w.RunFor(4f); b.Search();
            Assert.True(w.RunUntil(() => a.C.Current == Phase.Playing && b.C.Current == Phase.Playing, 20f));
            w.RunFor(2f);
            b.C.Forfeit();
            Assert.True(w.RunUntil(() => a.Done && b.Done, 20f));
            int pa = League.Points(a.C.Outcome, false, a.C.IForfeited, a.C.ByForfeit && a.C.Outcome == DuelOutcome.Win, a.C.MySeconds, t);
            int pb = League.Points(b.C.Outcome, false, b.C.IForfeited, false, b.C.MySeconds, t);
            Assert.AreEqual(t.losePoints, pa);
            Assert.AreEqual(0, pb);
        }
    }
}
