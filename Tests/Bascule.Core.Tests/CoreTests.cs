using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bascule.Core;
using NUnit.Framework;

namespace Bascule.Core.Tests
{
    public class PlankModelTests
    {
        static PlankTuning T() => new PlankTuning();

        static PlankModel Settle(PlankTuning t, float torque, float load, float seconds = 20f)
        {
            var m = new PlankModel(t);
            for (float s = 0; s < seconds; s += 1 / 60f) m.Step(1 / 60f, torque, load);
            return m;
        }

        [Test]
        public void SceneryChangesEarlyThenNewWorldsUntilTheMoon()
        {
            Assert.AreEqual(0, Difficulty.BackgroundIndex(0));
            Assert.AreEqual(0, Difficulty.BackgroundIndex(299));
            Assert.AreEqual(1, Difficulty.BackgroundIndex(300), "premier changement dès 300");
            Assert.AreEqual(Biome.Beach, Theme.All[Difficulty.BackgroundIndex(2500)].Biome, "la plage dès 2 500");
            // Avant 8 000 (là où s'arrêtent la plupart des parties), on voit déjà la plupart des mondes.
            var early = new HashSet<Biome>();
            for (int sc = 0; sc < 8000; sc += 100) early.Add(Theme.All[Theme.ArtOf(Difficulty.BackgroundIndex(sc))].Biome);
            Assert.That(early.Count, Is.GreaterThanOrEqualTo(6), "prairie, plage, forêt, désert, montagne, ville avant 8 000");
            Assert.AreEqual(Biome.City, Theme.All[Difficulty.BackgroundIndex(7999)].Biome);
            Assert.AreEqual(Biome.Moon, Theme.All[Difficulty.BackgroundIndex(Difficulty.LoopScore - 1)].Biome, "la Lune avant la boucle");
            Assert.That(Difficulty.LoopScore, Is.LessThanOrEqualTo(20000), "les 20 000 sont rares : tous les mondes avant");
            Assert.AreEqual(Difficulty.ThemeScores.Length, Theme.All.Length, "un palier par monde");
            // Chaque palier change de monde ou de teinte ; au-delà de 30 000, des mondes déjà vus, recolorés.
            var seen = new HashSet<string>();
            for (int sc = 0; sc < 120000; sc += 250)
            {
                int st = Difficulty.BackgroundIndex(sc);
                Assert.That(Theme.ArtOf(st), Is.InRange(0, Theme.All.Length - 1));
                seen.Add(Theme.ArtOf(st) + "/" + Theme.TintOf(st).r + "," + Theme.TintOf(st).g);
            }
            Assert.That(seen.Count, Is.GreaterThan(Theme.All.Length + 20));
            int after = Difficulty.BackgroundIndex(Difficulty.LoopScore);
            Assert.AreNotEqual(Biome.Meadow, Theme.All[Theme.ArtOf(after)].Biome);
            var tn = Theme.TintOf(after);
            Assert.False(tn.r == 255 && tn.g == 255 && tn.b == 255, "variante colorée");
        }

        [Test]
        public void BalloonCalmPreventsAnInstantLoss()
        {
            // Après l'envol d'un 128, il reste un gros poids d'un seul côté : sans souffle, la partie est perdue.
            var t = T();
            float heavy = t.maxAngle / t.degreesPerTorque * 1.6f;
            var without = new PlankModel(t);
            for (float s = 0; s < 2.5f; s += 1 / 60f) without.Step(1 / 60f, heavy, 64);
            Assert.True(without.Lost, "sans souffle, ce couple fait perdre");

            var with = new PlankModel(t);
            with.GiveCalm(t.balloonLiftSeconds + t.balloonGraceSeconds);
            for (float s = 0; s < t.balloonLiftSeconds + t.balloonGraceSeconds - 0.1f; s += 1 / 60f) with.Step(1 / 60f, heavy, 64);
            Assert.False(with.Lost, "pendant le souffle, on ne perd pas");
            // Le souffle ne dure pas : si rien n'est rattrapé, la défaite revient ensuite.
            for (float s = 0; s < 3f; s += 1 / 60f) with.Step(1 / 60f, heavy, 64);
            Assert.True(with.Lost);
        }

        [Test]
        public void BigMergeSteadiesThePlankButCanStillLose()
        {
            // Un 64 créé d'un coup : la planche est un peu plus stable (perte plus tardive), mais pas imperdable.
            var t = T();
            float heavy = t.maxAngle / t.degreesPerTorque * 1.6f;
            float LossTime(bool steady)
            {
                var m = new PlankModel(t);
                if (steady) m.GiveSteady(t.bigMergeSteadySeconds);
                for (float s = 0; s < 10f; s += 1 / 60f) { m.Step(1 / 60f, heavy, 64); if (m.Lost) return s; }
                return 99f;
            }
            float plain = LossTime(false), steadied = LossTime(true);
            Assert.Less(plain, 99f);
            Assert.Greater(steadied, plain, "la stabilisation laisse un peu plus de temps");
            Assert.Less(steadied, 99f, "on peut quand même perdre");
        }

        [Test]
        public void EmptyPlankStaysLevel()
        {
            var m = Settle(T(), 0, 0);
            Assert.That(m.Angle, Is.EqualTo(0).Within(1e-4));
            Assert.False(m.Lost);
        }

        [Test]
        public void HeavierRightSideTiltsRightDownTowardEquilibrium()
        {
            var t = T();
            var m = Settle(t, 8f, 8f);
            Assert.That(m.Angle, Is.EqualTo(8f * t.degreesPerTorque).Within(0.05));
            Assert.False(m.Lost);
        }

        [Test]
        public void InverseDirectionTiltsTheOtherWay()
        {
            var t = T(); t.tiltDirection = -1;
            var m = Settle(t, 8f, 8f);
            Assert.That(m.Angle, Is.EqualTo(-8f * t.degreesPerTorque).Within(0.05));
        }

        [Test]
        public void TiltIsProgressiveNotInstant()
        {
            var t = T();
            var m = new PlankModel(t);
            m.Step(1 / 60f, 10f, 10f);
            Assert.That(Math.Abs(m.Angle), Is.LessThan(1f), "une seule frame ne doit presque pas bouger la planche");
        }

        [Test]
        public void TiltOvershootsThenSettles()
        {
            var t = T();
            var m = new PlankModel(t);
            float target = 10f * t.degreesPerTorque, peak = 0;
            for (int i = 0; i < 60 * 15; i++) { m.Step(1 / 60f, 10f, 10f); peak = Math.Max(peak, m.Angle); }
            Assert.That(peak, Is.GreaterThan(target * 1.05f), "oscille un peu (amorti, pas critique)");
            Assert.That(m.Angle, Is.EqualTo(target).Within(0.1f), "puis se stabilise");
        }

        [Test]
        public void LongImbalanceBeyondLimitLoses()
        {
            var t = T();
            var m = Settle(t, m_LosingTorque(t) * 1.5f, 40f, 10f);
            Assert.True(m.Lost);
        }

        [Test]
        public void BriefOvershootIsForgiven()
        {
            // Couple juste sous la limite : l'oscillation dépasse un instant mais pas assez longtemps.
            var t = T();
            var m = Settle(t, m_LosingTorque(t) * 0.86f, 30f, 15f);
            Assert.False(m.Lost);
        }

        [Test]
        public void HeavyLoadMovesSlower()
        {
            var t = T();
            var light = new PlankModel(t);
            var heavy = new PlankModel(t);
            for (int i = 0; i < 20; i++) { light.Step(1 / 60f, 5f, 5f); heavy.Step(1 / 60f, 5f, 200f); }
            Assert.That(Math.Abs(heavy.Angle), Is.LessThan(Math.Abs(light.Angle)));
        }

        [Test]
        public void LargeTimeStepStaysStable()
        {
            var m = new PlankModel(T());
            for (int i = 0; i < 100; i++) m.Step(0.25f, 3f, 3f);
            Assert.False(float.IsNaN(m.Angle));
            Assert.That(Math.Abs(m.Angle), Is.LessThanOrEqualTo(T().maxAngle + T().overshootAngle));
        }

        [Test]
        public void ReviveRestoresControl()
        {
            var t = T();
            var m = Settle(t, 100f, 100f, 5f);
            Assert.True(m.Lost);
            m.Revive();
            Assert.False(m.Lost);
            Assert.That(Math.Abs(m.Angle), Is.LessThanOrEqualTo(t.maxAngle * 0.3f + 1e-3));
        }

        static float m_LosingTorque(PlankTuning t) => new PlankModel(t).LosingTorque;
    }

    public class SpawnSequenceTests
    {
        [Test]
        public void SameSeedSameSequence()
        {
            var a = new SpawnSequence(1234, new SpawnTuning());
            var b = new SpawnSequence(1234, new SpawnTuning());
            for (int i = 0; i < 300; i++) Assert.AreEqual(a.Advance(), b.Advance());
        }

        [Test]
        public void DifferentSeedsDiffer()
        {
            var a = new SpawnSequence(1, new SpawnTuning());
            var b = new SpawnSequence(2, new SpawnTuning());
            int diff = 0;
            for (int i = 0; i < 100; i++) if (a.Advance() != b.Advance()) diff++;
            Assert.That(diff, Is.GreaterThan(20));
        }

        [Test]
        public void NextBecomesCurrent()
        {
            var s = new SpawnSequence(99, new SpawnTuning());
            for (int i = 0; i < 50; i++)
            {
                int next = s.Next;
                s.Advance();
                Assert.AreEqual(next, s.Current);
            }
        }

        [Test]
        public void EarlyWeightsAreSmallLaterHeavier()
        {
            var s = new SpawnSequence(5, new SpawnTuning());
            var early = new List<int>();
            for (int i = 0; i < 15; i++) early.Add(s.Advance());
            Assert.That(early.Max(), Is.LessThanOrEqualTo(4));
            var late = new List<int>();
            for (int i = 0; i < 400; i++) late.Add(s.Advance());
            Assert.That(late.Average(), Is.GreaterThan(early.Average()));
            Assert.That(late.Max(), Is.LessThan(WeightCatalog.MaxValue), "le 128 ne tombe jamais du ciel, il se fabrique");
        }

        [Test]
        public void RestoreContinuesIdentically()
        {
            var a = new SpawnSequence(42, new SpawnTuning());
            for (int i = 0; i < 37; i++) a.Advance();
            var b = new SpawnSequence(7, new SpawnTuning());
            b.Restore(a.RngState, a.DropIndex, a.Current, a.Next);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.Advance(), b.Advance());
        }

        [Test]
        public void DailySeedIsStablePerDay()
        {
            Assert.AreEqual(Daily.Seed("2026-11-02"), Daily.Seed("2026-11-02"));
            Assert.AreNotEqual(Daily.Seed("2026-11-02"), Daily.Seed("2026-11-03"));
            // Valeur figée : si elle change, tous les joueurs n'ont plus la même suite (tests de non-régression).
            Assert.AreEqual(GameRng.HashSeed("abc"), 440920331u);
        }
    }

    public class WeightTests
    {
        [Test]
        public void ValuesArePowersOfTwo()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 4, 8, 16, 32, 64, 128 }, WeightCatalog.Values);
            foreach (var v in WeightCatalog.Values) Assert.True(WeightCatalog.IsValid(v));
            Assert.False(WeightCatalog.IsValid(3));
            Assert.AreEqual(4, WeightCatalog.Normalize(5));
        }

        [Test]
        public void SizeGrowsButIsCapped()
        {
            var t = new WeightTuning();
            float prev = 0;
            foreach (var v in WeightCatalog.Values)
            {
                float s = WeightCatalog.Size(v, t);
                Assert.That(s, Is.GreaterThanOrEqualTo(prev));
                Assert.That(s, Is.LessThanOrEqualTo(t.maxSize));
                prev = s;
            }
            Assert.That(WeightCatalog.Size(1, t) / WeightCatalog.Size(128, t), Is.GreaterThan(0.3f), "le petit reste visible");
        }

        [Test]
        public void TorqueWeightFollowsExponent()
        {
            var t = new PlankTuning { torqueExponent = 1f };
            Assert.AreEqual(64f, WeightCatalog.TorqueWeight(64, t), 1e-4);
            t.torqueExponent = 0.85f;
            Assert.That(WeightCatalog.TorqueWeight(64, t), Is.LessThan(64f));
            Assert.That(WeightCatalog.TorqueWeight(8, t) * 2, Is.GreaterThan(WeightCatalog.TorqueWeight(16, t)), "fusionner allège");
            Assert.AreEqual(1f, WeightCatalog.TorqueWeight(1, t), 1e-4);
        }

        [Test]
        public void EveryValueHasADistinctColor()
        {
            var colors = WeightCatalog.Values.Select(WeightCatalog.ColorHex).ToList();
            Assert.AreEqual(colors.Count, colors.Distinct().Count());
        }

        [Test]
        public void NumberBadgeIsReadableOnEveryColor()
        {
            // Le chiffre (encre) est posé sur une pastille blanche : contraste maximal quelle que soit la couleur.
            Assert.That(Rgba.Contrast(HeadArtist.Ink, Rgba.White), Is.GreaterThan(7f));
        }
    }

    public class ScoreTests
    {
        [Test]
        public void MergePointsScaleWithValue()
        {
            var t = new ScoreTuning();
            var s = new ScoreKeeper(t);
            s.OnDrop();
            int p2 = s.OnMerge(2, 0f);
            s.OnDrop();
            int p16 = s.OnMerge(16, 10f);
            Assert.AreEqual(2 * t.pointsPerValue, p2);
            Assert.AreEqual(16 * t.pointsPerValue, p16);
            Assert.AreEqual(18 * t.pointsPerValue, s.Score);
        }

        [Test]
        public void ChainedMergesRaiseMultiplier()
        {
            var t = new ScoreTuning();
            var s = new ScoreKeeper(t);
            s.OnDrop();
            s.OnMerge(2, 0.0f);
            s.OnMerge(4, 0.3f);
            int third = s.OnMerge(8, 0.6f);
            Assert.AreEqual(3, s.Chain);
            Assert.AreEqual((int)Math.Round(8 * t.pointsPerValue * (1 + 2 * t.comboStep)), third);
            Assert.AreEqual(3, s.BestChain);
        }

        [Test]
        public void ChainBreaksAfterWindowOrNewDrop()
        {
            var s = new ScoreKeeper(new ScoreTuning());
            s.OnDrop(); s.OnMerge(2, 0f); s.OnMerge(4, 5f);
            Assert.AreEqual(1, s.Chain);
            s.OnMerge(8, 5.2f);
            Assert.AreEqual(2, s.Chain);
            s.OnDrop();
            s.OnMerge(2, 5.3f);
            Assert.AreEqual(1, s.Chain);
        }

        [Test]
        public void MultiplierIsCapped()
        {
            var t = new ScoreTuning();
            var s = new ScoreKeeper(t);
            s.OnDrop();
            for (int i = 0; i < 30; i++) s.OnMerge(2, i * 0.1f);
            Assert.AreEqual(t.maxComboMultiplier, s.Multiplier);
        }

        [Test]
        public void BalloonGivesBigBonus()
        {
            var t = new ScoreTuning();
            var s = new ScoreKeeper(t);
            int bonus = s.OnBalloon();
            Assert.AreEqual(t.balloonBonus, bonus);
            Assert.AreEqual(1, s.Balloons);
        }

        [Test]
        public void PlankShrinksWithScoreDownToMinimum()
        {
            var t = new PlankTuning { shrinkUntilScore = 0 };
            Assert.AreEqual(t.startLength, Difficulty.PlankLength(0, t));
            Assert.That(Difficulty.PlankLength(4000, t), Is.LessThan(t.startLength));
            Assert.AreEqual(t.minLength, Difficulty.PlankLength(1_000_000, t));
        }

        [Test]
        public void PlankStopsShrinkingAtHighScoreSinceShrinkingMadeItEasier()
        {
            // Le bras de levier est fixe : une planche plus courte donnait moins de couple, donc moins de danger.
            var t = new PlankTuning();
            Assert.That(Difficulty.PlankLength(2000, t), Is.LessThan(Difficulty.PlankLength(1000, t)));
            Assert.AreEqual(Difficulty.PlankLength(t.shrinkUntilScore, t), Difficulty.PlankLength(12000, t));
        }

        [Test]
        public void PlankGetsMoreSensitiveAtHighScore()
        {
            var t = new PlankTuning();
            Assert.AreEqual(1f, Difficulty.Sensitivity(0, t));
            Assert.AreEqual(1f, Difficulty.Sensitivity(t.sensitivityFromScore, t));
            Assert.That(Difficulty.Sensitivity(8000, t), Is.GreaterThan(1.1f));
            Assert.AreEqual(t.sensitivityMax, Difficulty.Sensitivity(1_000_000, t), 1e-5);
            var m = new PlankModel(t) { Sensitivity = 1.2f };
            Assert.AreEqual(t.maxAngle / (t.degreesPerTorque * 1.2f), m.LosingTorque, 1e-4);
        }
    }

    public class DuelTests
    {
        [Test]
        public void InviteLinksGiveTheCode()
        {
            Assert.AreEqual("K7QM2X", DuelRules.CodeFromLink("bascule://duel/K7QM2X"));
            Assert.AreEqual("K7QM2X", DuelRules.CodeFromLink("bascule://duel/k7qm2x/"));
            Assert.AreEqual("K7QM2X", DuelRules.CodeFromLink("https://orpheex.fr/bascule/duel.html?c=K7QM2X"));
            Assert.AreEqual("K7QM2X", DuelRules.CodeFromLink("https://orpheex.fr/bascule/duel.html?x=1&c=k7qm2x#top"));
            Assert.AreEqual("K7QM2X", DuelRules.CodeFromLink(DuelRules.InviteLink("K7QM2X")));
            Assert.IsNull(DuelRules.CodeFromLink("bascule://duel"));
            Assert.IsNull(DuelRules.CodeFromLink("https://orpheex.fr/bascule/duel.html"));
            // 1.4.1 : le code ami de l'hôte (f) passe avant le code de la partie (c), qui reste lu par la 1.4.
            string link = DuelRules.InviteLink("ZX81QP", "4K7Q2M");
            Assert.AreEqual("4K7Q2M", DuelRules.CodeFromLink(link));
            Assert.AreEqual("ZX81QP", DuelRules.SessionFromLink(link));
            Assert.AreEqual("4K7Q2M", DuelRules.CodeFromLink("bascule://duel/ZX81QP?f=4k7q2m"));
            Assert.AreEqual("ZX81QP", DuelRules.SessionFromLink("bascule://duel/ZX81QP?f=4k7q2m"));
            Assert.AreEqual("ZX81QP", DuelRules.CodeFromLink(DuelRules.InviteLink("ZX81QP", "")));
            Assert.IsNull(DuelRules.CodeFromLink("https://orpheex.fr/bascule"));
            Assert.IsNull(DuelRules.CodeFromLink(null));
        }

        [Test]
        public void ChallengerAfterFifteenWins()
        {
            var p = new Profile();
            var t = new DuelTuning();
            for (int i = 0; i < 14; i++) DuelRules.Record(p, DuelOutcome.Win, t);
            Assert.AreEqual(0, DuelRules.UnlockHeads(p).Count);
            DuelRules.Record(p, DuelOutcome.Lose, t);
            Assert.AreEqual(0, DuelRules.UnlockHeads(p).Count);
            DuelRules.Record(p, DuelOutcome.Win, t);
            var heads = DuelRules.UnlockHeads(p);
            Assert.AreEqual(1, heads.Count);
            Assert.AreEqual("Challenger", heads[0].NameFr);
            Assert.That(p.unlockedHeads, Does.Contain(heads[0].Id));
            Assert.AreEqual(0, DuelRules.UnlockHeads(p).Count, "une seule fois");
            Assert.AreEqual(16, p.duelsPlayed);
            Assert.AreEqual(15 * t.coinsWin + t.coinsLose, p.coins - new Profile().coins);
        }

        [Test]
        public void OpponentNamesAreFiltered()
        {
            Assert.AreEqual("Léa", DuelRules.CleanName("Léa", "Ami"));
            Assert.AreEqual("Ami", DuelRules.CleanName("", "Ami"));
            Assert.AreEqual("Ami", DuelRules.CleanName("connard", "Ami"));
            Assert.AreEqual("Super Lea", DuelRules.CleanName("Super Lea", "Ami"));
        }

        [Test]
        public void PressureRisesWithTimeNotScore()
        {
            var t = new DuelTuning();
            Assert.AreEqual(1f, DuelRules.Sensitivity(10f, t));
            Assert.That(DuelRules.Sensitivity(80f, t), Is.GreaterThan(1.2f));
            Assert.That(DuelRules.Sensitivity(140f, t), Is.GreaterThan(DuelRules.Sensitivity(80f, t)));
            Assert.AreEqual(t.sensitivityMax, DuelRules.Sensitivity(10000f, t), 1e-5);
        }

        [Test]
        public void BothPlayersGetTheSameGusts()
        {
            var t = new DuelTuning();
            var a = DuelRules.GustSchedule(1234, t);
            var b = DuelRules.GustSchedule(1234, t);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) { Assert.AreEqual(a[i].At, b[i].At); Assert.AreEqual(a[i].Direction, b[i].Direction); Assert.AreEqual(a[i].Strength, b[i].Strength); }
            Assert.AreEqual(t.firstGustSeconds, a[0].At);
            for (int i = 1; i < a.Count; i++)
            {
                float gap = a[i].At - a[i - 1].At;
                Assert.That(gap, Is.InRange(t.gustGapMin, t.gustGapMax));
                Assert.That(a[i].Strength, Is.GreaterThanOrEqualTo(a[i - 1].Strength));
            }
            Assert.AreNotEqual(DuelRules.GustSchedule(99, t)[1].At, a[1].At, "une autre graine donne d'autres rafales");
        }

        [Test]
        public void ScriptedGustsFollowTheScheduleAndStayFair()
        {
            var wt = new WindTuning();
            var schedule = new List<DuelGust> { new DuelGust { At = 2f, Direction = 1, Strength = 0.9f } };
            var g = Gusts.Scripted(wt, schedule, 0.92f);
            WindPhase started = WindPhase.None;
            for (int i = 0; i < 150 && started == WindPhase.None; i++) started = g.Update(0.02f, 0, 1.5f, 0.5f, true);
            Assert.AreEqual(WindPhase.Warning, started, "même en danger : le duel ne décale pas les rafales");
            Assert.That(g.Clock, Is.InRange(1.99f, 2.05f));
            Assert.That(0.5f + g.Strength, Is.LessThanOrEqualTo(0.92f + 1e-4f), "le vent seul ne fait pas perdre");
        }

        [Test]
        public void FirstToFallLosesWithTieBreakOnScore()
        {
            var t = new DuelTuning();
            Assert.AreEqual(DuelOutcome.Win, DuelRules.Decide(null, 40f, 100, 900, t));
            Assert.AreEqual(DuelOutcome.Lose, DuelRules.Decide(40f, null, 900, 100, t));
            Assert.AreEqual(DuelOutcome.Win, DuelRules.Decide(41f, 40f, 100, 900, t), "il est tombé avant moi");
            Assert.AreEqual(DuelOutcome.Win, DuelRules.Decide(40.1f, 40f, 900, 100, t), "chutes simultanées : meilleur score");
            Assert.AreEqual(DuelOutcome.Lose, DuelRules.Decide(40f, 40.2f, 100, 900, t));
            Assert.AreEqual(DuelOutcome.Draw, DuelRules.Decide(40f, 40.1f, 500, 500, t));
        }

        [Test]
        public void DuelEndsAfterThreeMinutesAndBestScoreWins()
        {
            var t = new DuelTuning();
            Assert.AreEqual(180f, t.timeLimitSeconds, "un duel dure au plus 3 minutes");
            Assert.AreEqual(180f, DuelRules.TimeLeft(0f, t));
            Assert.AreEqual(60f, DuelRules.TimeLeft(120f, t), 1e-4);
            Assert.AreEqual(0f, DuelRules.TimeLeft(200f, t));
            Assert.IsFalse(DuelRules.TimeUp(179.9f, t));
            Assert.IsTrue(DuelRules.TimeUp(180f, t));
            // Temps écoulé, personne n'a lâché : le meilleur score gagne, égalité si les scores sont égaux.
            Assert.AreEqual(DuelOutcome.Win, DuelRules.Decide(null, null, 1200, 900, t));
            Assert.AreEqual(DuelOutcome.Lose, DuelRules.Decide(null, null, 900, 1200, t));
            Assert.AreEqual(DuelOutcome.Draw, DuelRules.Decide(null, null, 1000, 1000, t));
            // Lâcher juste avant la fin fait perdre, même avec un meilleur score.
            Assert.AreEqual(DuelOutcome.Lose, DuelRules.Decide(179.5f, null, 5000, 900, t));
            // Sans limite (0) : le duel ne finit jamais au temps.
            var free = new DuelTuning { timeLimitSeconds = 0f };
            Assert.IsFalse(DuelRules.TimeUp(10000f, free));
            Assert.IsTrue(float.IsPositiveInfinity(DuelRules.TimeLeft(10f, free)));
        }

        [Test]
        public void RecordingADuelGivesCoinsAndCountsWins()
        {
            var t = new DuelTuning();
            var p = new Profile(); p.Sanitize();
            int before = p.coins;
            Assert.AreEqual(t.coinsWin, DuelRules.Record(p, DuelOutcome.Win, t));
            Assert.AreEqual(t.coinsLose, DuelRules.Record(p, DuelOutcome.Lose, t));
            Assert.AreEqual(before + t.coinsWin + t.coinsLose, p.coins);
            Assert.AreEqual(2, p.duelsPlayed);
            Assert.AreEqual(1, p.duelWins);
            var challenger = HeadCatalog.All.First(h => h.Unlock == UnlockKind.DuelWins);
            Assert.IsFalse(HeadCatalog.IsEarned(challenger, new RunAchievements { DuelWins = t.challengerWins - 1 }));
            Assert.IsTrue(HeadCatalog.IsEarned(challenger, new RunAchievements { DuelWins = t.challengerWins }));
        }

        [Test]
        public void MessagesRoundTrip()
        {
            var h = DuelProtocol.ReadHello(DuelProtocol.Hello(new DuelHello { Name = "Léa", Head = 22, Plank = 3 }));
            Assert.AreEqual("Léa", h.Name); Assert.AreEqual(22, h.Head); Assert.AreEqual(3, h.Plank);

            DuelProtocol.ReadStart(DuelProtocol.Start(0xDEADBEEF, 3f), out uint seed, out float cd);
            Assert.AreEqual(0xDEADBEEF, seed); Assert.AreEqual(3f, cd, 1e-3);

            var snap = new DuelSnapshot { TimeMs = 71234, Score = 5210, Angle = -12.34f, Danger = 0.88f, HeldValue = 16, HeldX = 1.4f, Wind = -0.5f };
            for (int i = 0; i < 60; i++) snap.Weights.Add(new DuelWeight { Id = (ushort)(1000 + i), Value = 1 << (i % 7), X = -3f + i * 0.1f, Y = 0.5f + i * 0.05f, Rotation = i * 3.3f - 50f });
            var bytes = DuelProtocol.Snapshot(snap);
            Assert.That(bytes.Length, Is.LessThan(1100), "une photo tient dans un paquet réseau");
            Assert.AreEqual(DuelMsg.Snapshot, DuelProtocol.Kind(bytes));
            var back = DuelProtocol.ReadSnapshot(bytes);
            Assert.AreEqual(snap.TimeMs, back.TimeMs); Assert.AreEqual(snap.Score, back.Score);
            Assert.AreEqual(snap.Angle, back.Angle, 0.01f); Assert.AreEqual(snap.Danger, back.Danger, 0.01f);
            Assert.AreEqual(16, back.HeldValue); Assert.AreEqual(1.4f, back.HeldX, 0.01f); Assert.AreEqual(-0.5f, back.Wind, 0.01f);
            Assert.AreEqual(60, back.Weights.Count);
            for (int i = 0; i < 60; i++)
            {
                Assert.AreEqual(snap.Weights[i].Id, back.Weights[i].Id);
                Assert.AreEqual(snap.Weights[i].Value, back.Weights[i].Value);
                Assert.AreEqual(snap.Weights[i].X, back.Weights[i].X, 0.01f);
                Assert.AreEqual(snap.Weights[i].Rotation, back.Weights[i].Rotation, 0.1f);
            }
            DuelProtocol.ReadFell(DuelProtocol.Fell(83000, 4100), out uint ft, out int fs);
            Assert.AreEqual(83000u, ft); Assert.AreEqual(4100, fs);
            Assert.AreEqual((DuelMsg)0, DuelProtocol.Kind(new byte[] { 3, 99 }), "autre version : ignoré");
            Assert.DoesNotThrow(() => DuelProtocol.ReadSnapshot(new byte[] { 3, 1, 5 }), "message tronqué : pas de plantage");
        }

        [Test]
        public void OpponentViewIsSmoothlyInterpolated()
        {
            var buf = new SnapshotBuffer { DelaySeconds = 0.1f };
            // Photos toutes les 66 ms, arrivées avec un peu d'irrégularité.
            for (int i = 0; i <= 10; i++)
            {
                var s = new DuelSnapshot { TimeMs = (uint)(i * 66), Score = i * 10, Angle = i * 2f };
                s.Weights.Add(new DuelWeight { Id = 7, Value = 8, X = i * 0.1f, Y = 1f, Rotation = 350f + i * 4f });
                if (i == 10) s.Weights.Add(new DuelWeight { Id = 9, Value = 2, X = 2f, Y = 3f });
                buf.Add(s, 5f + i * 0.066f + (i % 3) * 0.01f);
            }
            // 5 s d'écart d'horloge : la vue doit le retrouver. Un peu avant la dernière photo + retard.
            var v = buf.Sample(5f + 0.6f + 0.1f);
            Assert.NotNull(v);
            Assert.That(v.Angle, Is.InRange(17f, 19.5f), "angle interpolé entre deux photos");
            var w7 = v.Weights.Find(w => w.Id == 7);
            Assert.That(w7.X, Is.InRange(0.85f, 0.97f));
            // 350° + 4°·i traverse 360° : l'interpolation prend le chemin court.
            Assert.That(w7.Rotation, Is.InRange(370f, 390f).Or.InRange(10f, 30f));
            // Une photo en retard (plus ancienne) est ignorée.
            int before = buf.Count;
            buf.Add(new DuelSnapshot { TimeMs = 10 }, 6f);
            Assert.AreEqual(before, buf.Count);
            // Bien après la dernière photo : on garde la dernière (pas d'extrapolation folle).
            var late = buf.Sample(20f);
            Assert.AreEqual(20f, late.Angle, 1e-3);
            Assert.AreEqual(2, late.Weights.Count, "le poids apparu en dernier est bien là");
        }

        [Test]
        public void DuelCodeIsReadFromTextOrLink()
        {
            Assert.AreEqual("K7Q3M2", DuelRules.NormalizeCode("k7q-3m2"));
            Assert.AreEqual("K7Q3M2", DuelRules.NormalizeCode("Viens me défier sur Bascule ! Code : K7Q3M2 https://orpheex.fr/duel?c=K7Q3M2"));
            Assert.IsNull(DuelRules.NormalizeCode(""));
        }
    }

    public class WindTests
    {
        /// <summary>Fait tourner le vent ; avec <paramref name="stop"/>, s'arrête dès qu'une phase commence.</summary>
        static WindPhase Run(Gusts g, float seconds, int score, float danger = 0f, float ratio = 0f, bool blocked = false, bool stop = false)
        {
            var started = WindPhase.None;
            for (float t = 0; t < seconds; t += 0.02f)
            {
                var p = g.Update(0.02f, score, danger, ratio, blocked);
                if (p != WindPhase.None && started == WindPhase.None) { started = p; if (stop) break; }
            }
            return started;
        }

        [Test]
        public void NoWindBeforeStartScore()
        {
            var g = new Gusts(new WindTuning(), 1);
            Assert.AreEqual(WindPhase.None, Run(g, 300f, 4999));
            Assert.AreEqual(0, g.Count);
        }

        [Test]
        public void MilestoneTriggersAGustWithWarningThenBlow()
        {
            var t = new WindTuning();
            var g = new Gusts(t, 1);
            Run(g, 30f, 4000);
            Assert.AreEqual(WindPhase.None, g.Phase);
            // Palier 5 000 franchi : la rafale attend l'écart minimal, puis s'annonce.
            Assert.AreEqual(WindPhase.Warning, Run(g, t.minGap + 1f, 5000, stop: true));
            Assert.AreEqual(1, g.Count);
            Assert.AreEqual(0f, g.Envelope, "pas de poussée pendant l'annonce");
            Run(g, t.warningSeconds + t.rampSeconds + 0.1f, 5000);
            Assert.AreEqual(WindPhase.Blowing, g.Phase);
            Assert.That(g.Envelope, Is.GreaterThan(0.6f));
            Run(g, t.blowSeconds, 5000);
            Assert.AreEqual(WindPhase.None, g.Phase);
        }

        [Test]
        public void GustsAreRareNeverCloserThanMinGap()
        {
            var t = new WindTuning();
            var g = new Gusts(t, 7);
            g.Reset(6000);
            // 10 minutes à score figé (stagnation : rafales plus fréquentes, mais jamais plus que minGap).
            Run(g, 600f, 6000);
            float slowest = t.minGap + t.warningSeconds + t.blowSeconds;
            Assert.That(g.Count, Is.LessThanOrEqualTo((int)(600f / slowest) + 1));
            Assert.That(g.Count, Is.GreaterThanOrEqualTo(600 / (int)(t.gapMax + t.warningSeconds + t.blowSeconds)));
        }

        [Test]
        public void NoGustWhenPlankAlreadyInDangerOrBlocked()
        {
            var t = new WindTuning();
            var g = new Gusts(t, 3);
            g.Reset(4900);
            Assert.AreEqual(WindPhase.None, Run(g, 200f, 5100, danger: 0.8f));
            Assert.AreEqual(WindPhase.None, Run(g, 200f, 5100, blocked: true));
            Assert.AreEqual(WindPhase.Warning, Run(g, 2f, 5100));
        }

        [Test]
        public void WindAloneNeverPushesPastTheFairLimit()
        {
            var t = new WindTuning { strengthStart = 0.6f, strengthMax = 0.6f };
            for (uint seed = 1; seed < 40; seed++)
            {
                var g = new Gusts(t, seed);
                g.Reset(4900);
                // La planche penche déjà de 45 % : si le vent pousse du même côté, il est bridé.
                Run(g, 60f, 5100, danger: 0.45f, ratio: 0.45f, stop: true);
                Assert.AreEqual(1, g.Count);
                float peak = 0.45f * g.Direction + g.Strength;
                Assert.That(peak, Is.LessThanOrEqualTo(t.maxPeakRatio + 1e-4), "seed " + seed);
            }
        }

        /// <summary>Fait souffler la rafale en cours jusqu'au plein du souffle.</summary>
        static void ToPeak(Gusts g, WindTuning t) => Run(g, t.warningSeconds + t.rampSeconds + 0.2f, g.Count > 0 ? 5100 : 5100);

        [Test]
        public void FirstGustsAreGentleAndCannotTopple()
        {
            var t = new WindTuning();
            var g = new Gusts(t, 11);
            g.Reset(4900);
            Assert.AreEqual(WindPhase.Warning, Run(g, 60f, 5100, stop: true));
            Assert.True(g.Gentle, "première rafale : douce");
            Assert.That(g.Strength, Is.LessThanOrEqualTo(t.gentlePeakRatio + 1e-4));
            Assert.That(g.Strength, Is.LessThan(0.3f), "bien plus douce qu'avant (0,42)");
            ToPeak(g, t);
            Assert.AreEqual(WindPhase.Blowing, g.Phase);
            Assert.That(Math.Abs(g.Torque(100f, 0.2f)), Is.GreaterThan(5f), "planche droite : le vent se sent");
            Assert.AreEqual(0f, g.Torque(100f, t.gentleFadeTo), 1e-4, "planche qui penche : le vent s'efface");
            Assert.That(Math.Abs(g.Torque(100f, 0.65f)), Is.LessThan(Math.Abs(g.Torque(100f, 0.2f))));
        }

        [Test]
        public void LaterGustsGrowButStaySofterThanBefore()
        {
            var t = new WindTuning();
            var g = new Gusts(t, 4);
            g.Reset(4900);
            float last = 0f;
            for (int i = 1; i <= 8; i++)
            {
                Assert.AreEqual(WindPhase.Warning, Run(g, 200f, 5100, stop: true), "rafale " + i);
                Assert.That(g.Strength, Is.GreaterThanOrEqualTo(last - 1e-4), "de plus en plus fortes");
                Assert.That(g.Strength, Is.LessThanOrEqualTo(t.strengthMax + 1e-4));
                last = g.Strength;
                Assert.AreEqual(i <= t.gentleGusts, g.Gentle, "rafale " + i);
                if (i == t.gentleGusts + 1)
                {
                    ToPeak(g, t);
                    Assert.That(Math.Abs(g.Torque(100f, t.gentleFadeTo)), Is.GreaterThan(1f), "après la découverte, le vent ne s'efface plus");
                }
                Run(g, t.warningSeconds + t.blowSeconds + 0.5f, 5100);
            }
            Assert.That(t.strengthMax, Is.LessThan(0.6f), "un peu moins violentes qu'avant (0,6)");
        }

        [Test]
        public void FairLimitWinsOverMinimumStrength()
        {
            var t = new WindTuning { gentleGusts = 0 };
            for (uint seed = 1; seed < 30; seed++)
            {
                var g = new Gusts(t, seed);
                g.Reset(4900);
                // Planche déjà penchée à 80 % : une rafale du même côté attend ; jamais au-delà de la limite.
                Run(g, 120f, 5100, danger: 0.4f, ratio: 0.8f, stop: true);
                if (g.Count == 0) continue;
                float toward = 0.8f * g.Direction;
                Assert.That(toward + g.Strength, Is.LessThanOrEqualTo(t.maxPeakRatio + 1e-4), "seed " + seed);
            }
        }

        [Test]
        public void ResetSkipsMilestonesAlreadyPassed()
        {
            var g = new Gusts(new WindTuning(), 5);
            g.Reset(9000);
            Assert.AreEqual(10000, g.MilestoneScore(2));
            Assert.AreEqual(12500, g.MilestoneScore(3));
            // Reprise à 9 000 : pas de rafale « en retard » pour 5 000 et 8 000.
            Assert.AreEqual(WindPhase.None, Run(g, 20f, 9000));
        }
    }

    public class NearMissTests
    {
        [Test]
        public void SaveIsRecordedWhenComingBackFromTheEdge()
        {
            var n = new NearMissTracker();
            float got = 0;
            n.SaveCompleted += v => got = v;
            foreach (var d in new[] { 0.2f, 0.7f, 0.9f, 0.97f, 0.8f, 0.6f, 0.4f }) n.Update(d, false);
            Assert.AreEqual(1, n.Saves);
            Assert.AreEqual(0.97f, n.BestSave, 1e-5);
            Assert.AreEqual(0.97f, got, 1e-5);
            Assert.AreEqual(97, NearMissTracker.DisplayPercent(n.BestSave));
        }

        [Test]
        public void NoSaveIfTheGameIsLost()
        {
            var n = new NearMissTracker();
            n.Update(0.95f, false);
            n.Update(1.4f, true);
            n.Update(0.1f, false);
            Assert.AreEqual(0, n.Saves);
        }

        [Test]
        public void DisplayNeverClaimsHundredPercent()
        {
            Assert.AreEqual(99, NearMissTracker.DisplayPercent(1.6f));
        }
    }

    public class MascotTests
    {
        [Test]
        public void MoodFollowsTilt()
        {
            var t = new MascotTuning();
            Assert.AreEqual(Mood.Relaxed, MascotRules.MoodFor(0.1f, t));
            Assert.AreEqual(Mood.Tense, MascotRules.MoodFor(0.4f, t));
            Assert.AreEqual(Mood.Strained, MascotRules.MoodFor(0.7f, t));
            Assert.AreEqual(Mood.LastEffort, MascotRules.MoodFor(0.95f, t));
        }

        [Test]
        public void RelaxedKeepsHeadsOwnPositiveExpression()
        {
            Assert.AreEqual(Expression.Wink, MascotRules.ExpressionFor(Mood.Relaxed, Expression.Wink));
            Assert.AreEqual(Expression.Calm, MascotRules.ExpressionFor(Mood.Relaxed, Expression.Grimace));
            Assert.AreEqual(Expression.Effort, MascotRules.ExpressionFor(Mood.LastEffort, Expression.Wink));
        }
    }

    public class PrivacyTests
    {
        [Test]
        public void AgeIsCountedCarefullyWithOnlyTheYear()
        {
            // Né en 2011, en 2026 : 14 ou 15 ans selon l'anniversaire. Prudence : mineur.
            var p = new Profile();
            Assert.True(PrivacyRules.IsMinor(p, 2026), "âge inconnu : traité comme un enfant");
            Assert.True(PrivacyRules.NeedsAsking(p, 2026));
            Assert.True(PrivacyRules.SetBirthYear(p, 2011, 2026));
            Assert.True(PrivacyRules.IsMinor(p, 2026));
            Assert.False(PrivacyRules.NeedsAsking(p, 2026), "un mineur n'a pas de choix de pubs à faire");
            Assert.AreEqual((int)AdChoice.Standard, p.adChoice);
            Assert.True(PrivacyRules.ChildDirected(p, 2026));
            // Né en 2010 : 15 ans au moins en 2026.
            var q = new Profile();
            PrivacyRules.SetBirthYear(q, 2010, 2026);
            Assert.False(PrivacyRules.IsMinor(q, 2026));
            Assert.True(PrivacyRules.NeedsAsking(q, 2026), "adulte numérique : on lui demande son choix");
        }

        [Test]
        public void PersonalizedAdsNeedAnAdultAndConsent()
        {
            var p = new Profile();
            PrivacyRules.SetBirthYear(p, 1995, 2026);
            Assert.False(PrivacyRules.Personalized(p, 2026));
            PrivacyRules.SetChoice(p, AdChoice.Personalized, 2026);
            Assert.True(PrivacyRules.Personalized(p, 2026));
            Assert.False(PrivacyRules.ChildDirected(p, 2026));
            PrivacyRules.SetChoice(p, AdChoice.Standard, 2026);
            Assert.False(PrivacyRules.Personalized(p, 2026), "refus : jamais personnalisées");
            // Un mineur ne peut pas consentir seul.
            var kid = new Profile();
            PrivacyRules.SetBirthYear(kid, 2014, 2026);
            PrivacyRules.SetChoice(kid, AdChoice.Personalized, 2026);
            Assert.False(PrivacyRules.Personalized(kid, 2026));
            Assert.False(PrivacyRules.SetBirthYear(kid, 2030, 2026), "année impossible refusée");
            Assert.False(PrivacyRules.SetBirthYear(kid, 1800, 2026));
        }

        [Test]
        public void PrivacyChoiceSurvivesASeasonReset()
        {
            var p = new Profile { birthYear = 1990, adChoice = (int)AdChoice.Personalized, pseudoAsked = true };
            var fresh = Seasons.Reset(p);
            Assert.AreEqual(1990, fresh.birthYear);
            Assert.AreEqual((int)AdChoice.Personalized, fresh.adChoice);
            Assert.True(fresh.pseudoAsked);
            var bad = new Profile { birthYear = 12, adChoice = 9 };
            bad.Sanitize();
            Assert.AreEqual(0, bad.birthYear);
            Assert.AreEqual(0, bad.adChoice);
        }

        [Test]
        public void YearListIsNeutralAndComplete()
        {
            var years = PrivacyRules.Years(2026);
            Assert.AreEqual(2022, years[0]);
            Assert.AreEqual(PrivacyRules.OldestYear, years[years.Count - 1]);
            Assert.AreEqual(years.Count, years.Distinct().Count());
        }
    }

    public class AdRewardTests
    {
        static AdTuning T => new AdTuning();

        [Test]
        public void EightVideosWinTheExclusiveHeadAtAnyPace()
        {
            var p = new Profile();
            var head = AdRewards.ExclusiveHead;
            Assert.NotNull(head);
            Assert.AreEqual(UnlockKind.Ads, head.Unlock);
            Assert.AreEqual(Rarity.Legendary, HeadCatalog.RarityOf(head));
            for (int i = 0; i < 7; i++) Assert.False(AdRewards.WatchedForHead(T, p), "vidéo " + (i + 1));
            Assert.AreEqual(7, AdRewards.HeadProgress(T, p));
            Assert.False(p.unlockedHeads.Contains(head.Id));
            Assert.True(AdRewards.WatchedForHead(T, p), "8e vidéo : gagnée");
            Assert.True(p.unlockedHeads.Contains(head.Id));
            Assert.False(AdRewards.WatchedForHead(T, p), "déjà gagnée : plus rien");
            Assert.AreEqual(8, AdRewards.HeadProgress(T, p));
            Assert.False(HeadCatalog.IsEarned(head, new RunAchievements { Score = 999999, DuelWins = 99 }), "rien d'autre ne la donne");
        }

        [Test]
        public void ExclusivePlankIsWonOnlyWithVideos()
        {
            var p = new Profile { coins = 100000 };
            var plank = AdRewards.ExclusivePlank;
            Assert.NotNull(plank);
            Assert.True(plank.AdExclusive);
            Assert.False(Cosmetics.BuyPlank(p, plank), "ne s'achète pas avec des pièces");
            Assert.AreEqual(100000, p.coins);
            Assert.False(DailyGift.NextPlank(p) == plank, "pas dans le cadeau du jour");
            foreach (var prize in SurpriseBox.Odds(p)) Assert.AreNotEqual(plank, prize.Plank, "pas dans la machine à surprises");
            for (int i = 0; i < 7; i++) Assert.False(AdRewards.WatchedForPlank(T, p));
            Assert.True(AdRewards.WatchedForPlank(T, p));
            Assert.True(Cosmetics.OwnsPlank(p, plank.Id));
            Assert.AreEqual(plank.Id, p.selectedPlank, "posée tout de suite");
        }

        [Test]
        public void VideoChestsAreLimitedPerDayAndDoubleOnce()
        {
            var p = new Profile();
            var t = T;
            for (int i = 0; i < t.adChestsPerDay; i++) Assert.NotNull(AdRewards.OpenChest(t, p, "2026-11-12", 7u));
            Assert.Null(AdRewards.OpenChest(t, p, "2026-11-12", 7u), "plus de coffre aujourd'hui");
            Assert.AreEqual(t.adChestsPerDay, AdRewards.ChestsLeft(t, p, "2026-11-13"), "demain : de nouveau");
            int before = p.coins;
            var loot = new Chest.Loot { Coins = 120 };
            Assert.AreEqual(120, AdRewards.DoubleChest(p, loot));
            Assert.AreEqual(before + 120, p.coins);
            Assert.AreEqual(0, AdRewards.DoubleChest(p, null));
        }

        [Test]
        public void TuningHasVideoRewards()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(dir.FullName, "Assets/Bascule/Resources/bascule_tuning.json")));
            var ads = doc.RootElement.GetProperty("ads");
            Assert.AreEqual(8, ads.GetProperty("adsForExclusiveHead").GetInt32());
            Assert.AreEqual(8, ads.GetProperty("adsForExclusivePlank").GetInt32());
            Assert.GreaterOrEqual(ads.GetProperty("adChestsPerDay").GetInt32(), 1);
            Assert.AreEqual(new AdTuning().adsForExclusiveHead, ads.GetProperty("adsForExclusiveHead").GetInt32(), "code et fichier d'accord");
            Assert.True(Loc.HasKey("unlock_ads"));
            Assert.AreEqual(Loc.Format("unlock_ads", 8), Loc.UnlockText(AdRewards.ExclusiveHead));
        }
    }

    public class LiveOpsTests
    {
        [Test]
        public void BeginnerQuestRewardsEachStepOnce()
        {
            var p = new Profile();
            p.Sanitize();
            Assert.True(BeginnerQuest.Active(p));
            p.gamesPlayed = 1; p.bestScore = 200;
            var r = BeginnerQuest.OnRun(p, 1);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(150, r[0].Coins);
            Assert.AreEqual(150, p.coins);
            Assert.AreEqual(0, BeginnerQuest.OnRun(p, 2).Count, "rien de plus sans progrès");
            // Un gros score et 5 parties d'un coup : les deux étapes suivantes, dans l'ordre.
            p.gamesPlayed = 5; p.bestScore = 800;
            r = BeginnerQuest.OnRun(p, 3);
            Assert.AreEqual(2, r.Count);
            Assert.NotNull(r[0].Chest, "étape 2 : un coffre");
            Assert.NotNull(r[1].Plank, "étape 3 : la planche Bambou");
            Assert.True(Cosmetics.OwnsPlank(p, BeginnerQuest.BambooPlank));
            Assert.False(BeginnerQuest.Active(p));
            Assert.AreEqual(0, BeginnerQuest.OnRun(p, 4).Count);
        }

        [Test]
        public void VeteransDoNotSeeTheBeginnerQuest()
        {
            var p = new Profile { gamesPlayed = 40, bestScore = 3000 };
            p.Sanitize();
            Assert.False(BeginnerQuest.Active(p));
        }

        [Test]
        public void FirstChestOpensAfterTheFirstGame()
        {
            var p = new Profile();
            Assert.NotNull(Chest.OnGameFinished(p, 5), "le tout premier coffre dès la 1re partie");
            Assert.Null(Chest.OnGameFinished(p, 6));
            Assert.Null(Chest.OnGameFinished(p, 7));
            Assert.NotNull(Chest.OnGameFinished(p, 8), "ensuite, toutes les 3 parties");
        }

        [Test]
        public void WeeklyScoreGoalIsFiftyThousandPoints()
        {
            // Une semaine entière de jeu : 50 000 points cumulés (15 000 se faisaient en deux jours).
            bool seen = false;
            var day = new DateTime(2026, 11, 9);
            for (int w = 0; w < 10; w++)
            {
                var d = WeeklyEvent.For(WeeklyEvent.WeekKey(day.AddDays(7 * w)));
                if (d.Kind != WeeklyEvent.Kind.TotalScore) continue;
                seen = true;
                Assert.AreEqual(50000, d.Target);
            }
            Assert.True(seen, "l'objectif de points revient au moins une fois en dix semaines");
            Loc.Language = "fr";
            Assert.AreEqual("Semaine brûlante", Loc.Get("event_name_11"));
        }

        [Test]
        public void WeeklyEventChangesEveryMondayAndPaysThreeTiers()
        {
            Assert.AreEqual("2026-11-09", WeeklyEvent.WeekKey(new DateTime(2026, 11, 12, 10, 0, 0)), "jeudi : lundi de la semaine");
            Assert.AreEqual("2026-11-16", WeeklyEvent.WeekKey(new DateTime(2026, 11, 16, 0, 0, 1)));
            Assert.AreEqual("2026-11-09", WeeklyEvent.WeekKey(new DateTime(2026, 11, 15, 23, 59, 0)), "dimanche soir : même semaine");
            var a = WeeklyEvent.For("2026-11-09");
            var b = WeeklyEvent.For("2026-11-16");
            Assert.AreNotEqual(a.Kind, b.Kind, "un autre objectif chaque semaine");
            Assert.That(a.Theme, Is.InRange(5, Theme.All.Length - 1));
            Assert.NotNull(a.Head);
            Assert.AreEqual(UnlockKind.Shop, a.Head.Unlock);
            var p = new Profile();
            var d = new WeeklyEvent.Def { Week = "2026-11-09", Kind = WeeklyEvent.Kind.Games, Target = 3, Head = a.Head };
            Assert.AreEqual(1, WeeklyEvent.OnRun(p, d, new RunStats()).Count, "palier 1 à 1/3");
            Assert.AreEqual(WeeklyEvent.TierCoins, p.coins);
            var r2 = WeeklyEvent.OnRun(p, d, new RunStats());
            Assert.True(r2[0].Ticket);
            Assert.AreEqual(1, p.surpriseTickets);
            var r3 = WeeklyEvent.OnRun(p, d, new RunStats());
            Assert.AreEqual(a.Head, r3[0].Head);
            Assert.True(p.unlockedHeads.Contains(a.Head.Id));
            Assert.AreEqual(0, WeeklyEvent.OnRun(p, d, new RunStats()).Count, "tout gagné");
            // Nouvelle semaine : tout recommence.
            var d2 = new WeeklyEvent.Def { Week = "2026-11-16", Kind = WeeklyEvent.Kind.Games, Target = 3, Head = a.Head };
            var r4 = WeeklyEvent.OnRun(p, d2, new RunStats());
            Assert.AreEqual(1, p.week.progress);
            Assert.AreEqual(1, r4.Count);
            Assert.That(WeeklyEvent.Left(new DateTime(2026, 11, 15, 12, 0, 0)).TotalHours, Is.EqualTo(12).Within(0.01));
        }

        [Test]
        public void WheelWaitNeverExceeds24HoursEvenIfTheClockGoesBack()
        {
            var p = new Profile();
            double now = 1_800_000_000;
            Assert.NotNull(Wheel.Spin(p, false, now, "2027-01-15", 1));
            Assert.AreEqual(Wheel.Cooldown, Wheel.FreeIn(p.wheel, now), 1e-6);
            Assert.True(Wheel.FreeReady(p.wheel, now + 24 * 3600), "24 h après : un nouveau tour gratuit");
            // L'heure du téléphone recule de 3 jours : on n'attend pas 4 jours, au plus 24 h.
            double back = now - 3 * 86400;
            Assert.LessOrEqual(Wheel.FreeIn(p.wheel, back), Wheel.Cooldown);
            Assert.False(Wheel.FreeReady(p.wheel, back));
            Assert.True(Wheel.FreeReady(p.wheel, back + Wheel.Cooldown));
        }

        [Test]
        public void WheelGivesOneFreeSpinPerDayPlusTwoWithVideos()
        {
            var p = new Profile();
            double now = 1_800_000_000;
            Assert.True(Wheel.FreeReady(p.wheel, now));
            Assert.NotNull(Wheel.Spin(p, false, now, "2027-01-15", 1));
            Assert.False(Wheel.FreeReady(p.wheel, now + 3600));
            Assert.Null(Wheel.Spin(p, false, now + 3600, "2027-01-15", 2), "pas deux tours gratuits en 24 h");
            Assert.NotNull(Wheel.Spin(p, true, now + 3600, "2027-01-15", 3));
            Assert.NotNull(Wheel.Spin(p, true, now + 3700, "2027-01-15", 4));
            Assert.Null(Wheel.Spin(p, true, now + 3800, "2027-01-15", 5), "2 tours en vidéo par jour");
            Assert.AreEqual(2, Wheel.AdSpinsLeft(p.wheel, "2027-01-16"), "le lendemain : de nouveau");
            Assert.True(Wheel.FreeReady(p.wheel, now + Wheel.Cooldown));
        }

        [Test]
        public void WheelOddsAreHonestAndEveryPrizeIsGiven()
        {
            float sum = 0f;
            for (int i = 0; i < Wheel.Slots.Length; i++) sum += Wheel.Chance(i);
            Assert.AreEqual(1f, sum, 1e-4);
            Assert.AreEqual(8, Wheel.Slots.Length);
            Assert.AreEqual(0, Wheel.Pick(0.0));
            Assert.AreEqual(Wheel.Slots.Length - 1, Wheel.Pick(0.99999));
            // Tirage sur beaucoup de tours : chaque lot sort, à peu près à sa chance affichée.
            var counts = new int[Wheel.Slots.Length];
            var rng = new GameRng(42);
            for (int i = 0; i < 20000; i++) counts[Wheel.Pick(rng.NextFloat())]++;
            for (int i = 0; i < counts.Length; i++) Assert.AreEqual(Wheel.Chance(i), counts[i] / 20000f, 0.015f, "part " + i);
            // Chaque lot est vraiment donné.
            var seen = new HashSet<WheelPrize>();
            for (uint s = 1; s < 400 && seen.Count < 6; s++)
            {
                var p = new Profile();
                var r = Wheel.Spin(p, false, 1e9, "d", s);
                seen.Add(r.Kind);
                switch (r.Kind)
                {
                    case WheelPrize.Coins: Assert.AreEqual(r.Coins, p.coins); break;
                    case WheelPrize.Chest: Assert.NotNull(r.Chest); break;
                    case WheelPrize.Ticket: Assert.AreEqual(1, p.surpriseTickets); break;
                    case WheelPrize.Shield: Assert.AreEqual(1, p.streakShields); break;
                    case WheelPrize.Boost: Assert.AreEqual(Wheel.BoostGames, p.coinBoostGames); break;
                    case WheelPrize.Jackpot: Assert.True(p.unlockedHeads.Contains(Wheel.LuckyHead.Id)); break;
                }
            }
            Assert.AreEqual(6, seen.Count, "tous les types de lots sortent");
        }

        [Test]
        public void CoinBoostDoublesCoinsButNeverTheScore()
        {
            var t = new EconomyTuning();
            var p = new Profile { coinBoostGames = 1 };
            var g = EconomyRun.Apply(t, p, new RunStats { Score = 4000 }, "2027-01-15");
            Assert.True(g.Boosted);
            Assert.AreEqual(2 * Wallet.CoinsForScore(t, 4000), g.Coins);
            Assert.AreEqual(0, p.coinBoostGames);
            var g2 = EconomyRun.Apply(t, p, new RunStats { Score = 4000 }, "2027-01-15");
            Assert.False(g2.Boosted);
            Assert.AreEqual(Wallet.CoinsForScore(t, 4000), g2.Coins);
        }

        [Test]
        public void DuoHeadIsGrantedOnce()
        {
            var p = new Profile();
            Assert.True(DuoHead.Grant(p));
            Assert.False(DuoHead.Grant(p));
            Assert.AreEqual(Rarity.Rare, HeadCatalog.RarityOf(DuoHead.Head));
            Assert.AreEqual(Rarity.Epic, HeadCatalog.RarityOf(Wheel.LuckyHead));
            Assert.False(HeadCatalog.IsEarned(Wheel.LuckyHead, new RunAchievements { Score = 999999 }));
        }

        [Test]
        public void WheelCooldownSurvivesASeasonReset()
        {
            var p = new Profile();
            p.wheel.lastFree = 123456;
            var fresh = Seasons.Reset(p);
            Assert.AreEqual(123456, fresh.wheel.lastFree);
        }
    }

    public class AppUpdateTests
    {
        [Test]
        public void VersionsAreComparedNumberByNumber()
        {
            Assert.AreEqual(1, AppUpdate.Compare("1.4.1", "1.4"));
            Assert.AreEqual(0, AppUpdate.Compare("1.4", "1.4.0"));
            Assert.AreEqual(1, AppUpdate.Compare("1.10", "1.9"));
            Assert.AreEqual(-1, AppUpdate.Compare("1.2", "1.4"));
            Assert.AreEqual(0, AppUpdate.Compare("abc", "1.4")); // illisible : jamais de fenêtre
        }

        [Test]
        public void UpdateIsSuggestedOncePerDayAtMostThreeTimesPerVersion()
        {
            var p = new Profile { gamesPlayed = 5 };
            Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "1.4.1", "2026-11-13"), "même version");
            Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "1.4", "2026-11-13"), "le store est derrière (vérification Apple)");
            Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "", "2026-11-13"), "version du store inconnue");
            string[] days = { "2026-11-13", "2026-11-14", "2026-11-15" };
            foreach (var d in days)
            {
                Assert.IsTrue(AppUpdate.ShouldSuggest(p, "1.4.1", "1.5", d), d);
                AppUpdate.MarkSuggested(p, "1.5", d);
                Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "1.5", d), "une fois par jour");
            }
            Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "1.5", "2026-11-20"), "trois fois maximum");
            Assert.IsTrue(AppUpdate.ShouldSuggest(p, "1.4.1", "1.6", "2026-11-20"), "nouvelle version : on repart à zéro");
        }

        [Test]
        public void NewPlayerIsNeverAskedToUpdate()
        {
            var p = new Profile();
            Assert.IsFalse(AppUpdate.ShouldSuggest(p, "1.4.1", "1.5", "2026-11-13"));
        }
    }

    public class UpdateKeepsSavesTests
    {
        [Test]
        public void A12SaveIsKeptIntactWhenUpdating()
        {
            // Sauvegarde de la 1.2 : pas encore de numéro de saison (0), de la progression partout.
            var old = new Profile
            {
                season = 0, bestScore = 4321, gamesPlayed = 87, coins = 1530, coinsEarned = 4200, level = 9, xp = 120,
                pseudo = "Léa", friendCode = "ABC123", tutorialDone = true,
            };
            old.unlockedHeads.AddRange(new[] { 1, 2, 3, 40 });
            old.ownedPlanks.Add(2);
            old.selectedHead = 40;
            old.Sanitize();

            var p = Seasons.OnLoad(old, existed: true, out bool changed, out bool wiped);
            Assert.False(wiped, "la 1.4 ne remet rien à zéro");
            Assert.True(changed, "la sauvegarde est marquée de la saison en cours");
            Assert.AreSame(old, p);
            Assert.AreEqual(Seasons.Current, p.season);
            Assert.AreEqual(4321, p.bestScore);
            Assert.AreEqual(87, p.gamesPlayed);
            Assert.AreEqual(1530, p.coins);
            Assert.AreEqual(9, p.level);
            Assert.AreEqual("Léa", p.pseudo);
            Assert.AreEqual("ABC123", p.friendCode);
            Assert.AreEqual(40, p.selectedHead);
            CollectionAssert.IsSubsetOf(new[] { 0, 1, 2, 3, 40 }, p.unlockedHeads);
            Assert.Contains(2, p.ownedPlanks);
            Assert.False(p.welcomePending, "pas d'écran de bienvenue du lancement pour un joueur déjà installé");

            // Deuxième lancement : rien ne bouge plus.
            Seasons.OnLoad(p, existed: true, out bool again, out _);
            Assert.False(again);

            // Un nouveau joueur, lui, voit l'écran de bienvenue.
            var fresh = Seasons.OnLoad(new Profile(), existed: false, out bool c2, out bool w2);
            Assert.True(c2); Assert.False(w2);
            Assert.True(fresh.welcomePending);
            Assert.AreEqual(Seasons.Current, fresh.season);
        }
    }

    public class PromoTests
    {
        // Code de test (aucun code n'est en service depuis le retrait de Samb'bach) : le mécanisme reste prêt pour un
        // prochain partenaire.
        static readonly PromoCode[] TestCodes = { new PromoCode { Key = "clubx", Display = "Club X", HeadId = 67, Partner = "le club X" } };

        [Test]
        public void PartnerCodeUnlocksItsHeadOnceWhateverTheSpelling()
        {
            foreach (var typed in new[] { "Club X", "CLUB-X", "club x", " Club'X ", "clubx", "CLÜB X" })
                Assert.NotNull(PromoCodes.Find(typed, TestCodes), typed);
            Assert.IsNull(PromoCodes.Find("club", TestCodes));

            var p = new Profile();
            Assert.AreEqual(PromoResult.Empty, PromoCodes.Redeem(p, "  ' ", out _, TestCodes));
            Assert.AreEqual(PromoResult.Unknown, PromoCodes.Redeem(p, "bascule", out _, TestCodes));
            Assert.AreEqual(PromoResult.Ok, PromoCodes.Redeem(p, "Club X", out var promo, TestCodes));
            Assert.AreEqual(67, promo.HeadId);
            Assert.True(p.unlockedHeads.Contains(67));
            Assert.AreEqual(PromoResult.AlreadyUsed, PromoCodes.Redeem(p, "CLUBX", out _, TestCodes), "un code ne sert qu'une fois");
            Assert.AreEqual(1, p.unlockedHeads.Count(id => id == 67));
        }

        [Test]
        public void SambBachIsGoneAndItsNumberIsNeverReused()
        {
            // Partenariat annulé (1.4.1) : le code ne marche plus, la tête n'existe plus, son numéro reste réservé.
            Assert.IsEmpty(PromoCodes.All);
            Assert.AreEqual(PromoResult.Unknown, PromoCodes.Redeem(new Profile(), "Samb'bach", out _));
            Assert.Contains(66, HeadCatalog.RetiredIds);
            Assert.False(HeadCatalog.Exists(66));
            foreach (var h in HeadCatalog.All) CollectionAssert.DoesNotContain(HeadCatalog.RetiredIds, h.Id, "numéro retiré réutilisé : " + h.NameFr);

            // Un joueur de la 1.4 qui l'avait (et la portait) : elle disparaît, il reprend Basile, rien d'autre ne bouge.
            var p = new Profile();
            p.unlockedHeads.AddRange(new[] { 5, 66 });
            p.redeemedCodes.Add("sambbach");
            p.selectedHead = 66;
            p.Sanitize();
            CollectionAssert.DoesNotContain(p.unlockedHeads, 66);
            Assert.Contains(5, p.unlockedHeads);
            Assert.AreEqual(0, p.selectedHead);
        }

        [Test]
        public void PartnerHeadsStayHiddenUntilTheirCodeIsEntered()
        {
            var partner = new HeadDef { Id = 67, Unlock = UnlockKind.Code, NameFr = "Club X" };
            var shop = HeadCatalog.All.First(h => h.Unlock == UnlockKind.Shop);
            var unlocked = new List<int> { 0 };
            Assert.False(HeadCatalog.IsVisible(partner, unlocked), "invisible sans le code");
            Assert.True(HeadCatalog.IsVisible(shop, unlocked), "les autres têtes restent visibles, même pas encore gagnées");
            unlocked.Add(67);
            Assert.True(HeadCatalog.IsVisible(partner, unlocked), "visible une fois le code entré");
            Assert.AreEqual(Rarity.Legendary, HeadCatalog.RarityOf(partner));
            Assert.False(HeadCatalog.IsEarned(partner, new RunAchievements { Score = 999999, Balloons = 99, BestChain = 99, DailyStreak = 99, WorldRank = 1, DuelWins = 99 }));
            // Le total affiché dans la collection ne compte que les têtes visibles.
            Assert.AreEqual(HeadCatalog.All.Count(h => h.Unlock != UnlockKind.Code), HeadCatalog.VisibleCount(new List<int> { 0 }));
            // Chaque code en service donne une tête de partenaire qui existe, avec sa tenue et son logo.
            foreach (var promo in PromoCodes.All)
            {
                Assert.AreEqual(UnlockKind.Code, HeadCatalog.Get(promo.HeadId).Unlock);
                Assert.AreEqual(promo.Key, PromoCodes.Normalize(promo.Display));
                Assert.AreEqual(promo.Patch, OutfitCatalog.For(promo.HeadId).Patch);
            }
            Assert.AreEqual(PromoCodes.All.Length, PromoCodes.All.Select(c => c.Key).Distinct().Count());
        }
    }

    public class FriendRequestTests
    {
        [Test]
        public void SomeoneWhoAddedMeIsARequestNotAFriend()
        {
            var p = new Profile(); p.Sanitize();
            var pending = FriendRequests.Pending(p, new[] { "lea", "me", "lea", "", "tom" }, "me");
            CollectionAssert.AreEqual(new[] { "lea", "tom" }, pending);
            Assert.IsEmpty(p.friends, "personne n'est ajouté sans mon accord");

            Assert.True(FriendRequests.Accept(p, "lea", "me"));
            Assert.Contains("lea", p.friends);
            Assert.True(FriendRequests.Decline(p, "tom"));
            Assert.IsEmpty(FriendRequests.Pending(p, new[] { "lea", "tom" }, "me"), "acceptée ou refusée : la demande ne revient pas");
            CollectionAssert.DoesNotContain(p.friends, "tom");

            // Refusé, puis je l'ajoute moi-même plus tard : il redevient possible.
            Assert.True(FriendCode.Add(p, "tom", "me"));
            Assert.Contains("tom", p.friends);
            // Bloqué ou retiré : pas de demande.
            Moderation.Block(p, "zoe", "me");
            FriendCode.Remove(p, "lea");
            Assert.IsEmpty(FriendRequests.Pending(p, new[] { "zoe", "lea" }, "me"));
        }
    }

    public class LeagueTests
    {
        static readonly LeagueTuning T = new LeagueTuning();

        [Test]
        public void WeekStartsOnMondayUtc()
        {
            Assert.AreEqual("2026-11-09", League.WeekKey(new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc)), "lundi 00:00");
            Assert.AreEqual("2026-11-09", League.WeekKey(new DateTime(2026, 11, 13, 19, 0, 0, DateTimeKind.Utc)), "vendredi");
            Assert.AreEqual("2026-11-09", League.WeekKey(new DateTime(2026, 11, 15, 23, 59, 59, DateTimeKind.Utc)), "dimanche soir");
            Assert.AreEqual("2026-11-16", League.WeekKey(new DateTime(2026, 11, 16, 0, 0, 1, DateTimeKind.Utc)), "lundi suivant");
            Assert.AreEqual("2026-12-28", League.WeekKey(new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc)), "à cheval sur deux années");
            Assert.AreEqual(new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc), League.NextReset(new DateTime(2026, 11, 13, 19, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc), League.WeekStart("2026-11-09"));
            Assert.IsNull(League.WeekStart("pas une date"));
        }

        [Test]
        public void DivisionsFollowTheRank()
        {
            Assert.AreEqual(Division.Legend, League.DivisionOf(1, T));
            Assert.AreEqual(Division.Legend, League.DivisionOf(3, T));
            Assert.AreEqual(Division.Elite, League.DivisionOf(4, T));
            Assert.AreEqual(Division.Elite, League.DivisionOf(10, T));
            Assert.AreEqual(Division.Challenger, League.DivisionOf(11, T));
            Assert.AreEqual(Division.Challenger, League.DivisionOf(30, T));
            Assert.AreEqual(Division.Hopeful, League.DivisionOf(31, T));
            Assert.AreEqual(Division.Hopeful, League.DivisionOf(0, T), "pas classé");
            Assert.AreEqual(500, League.Reward(Division.Legend, T));
            Assert.AreEqual(250, League.Reward(Division.Elite, T));
            Assert.AreEqual(100, League.Reward(Division.Challenger, T));
            Assert.AreEqual(0, League.Reward(Division.Hopeful, T));
            Assert.AreEqual(30, League.LastPlace(Division.Challenger, T));
        }

        [Test]
        public void PointsTable()
        {
            Assert.AreEqual(30, League.Points(DuelOutcome.Win, false, false, false, 60, T));
            Assert.AreEqual(15, League.Points(DuelOutcome.Draw, false, false, false, 60, T));
            Assert.AreEqual(8, League.Points(DuelOutcome.Lose, false, false, false, 60, T));
            Assert.AreEqual(10, League.Points(DuelOutcome.Win, true, false, false, 60, T));
            Assert.AreEqual(5, League.Points(DuelOutcome.Draw, true, false, false, 60, T));
            Assert.AreEqual(2, League.Points(DuelOutcome.Lose, true, false, false, 60, T));
            Assert.AreEqual(0, League.Points(DuelOutcome.Lose, false, true, false, 60, T), "abandon : rien");
            Assert.AreEqual(0, League.Points(DuelOutcome.Win, false, false, true, 5, T), "adversaire parti tout de suite : pas une vraie victoire");
            Assert.True(League.HollowWin(DuelOutcome.Win, true, 5, T));
            Assert.False(League.HollowWin(DuelOutcome.Win, true, 40, T));
            Assert.False(League.HollowWin(DuelOutcome.Win, false, 5, T));
            Assert.AreEqual(30, League.Points(DuelOutcome.Win, false, false, true, 40, T), "adversaire parti après un vrai duel");
        }

        [Test]
        public void PointsResetEachWeekAndTheRewardIsGivenOnce()
        {
            var p = new Profile(); p.Sanitize();
            int coins0 = p.coins;
            Assert.AreEqual(30, League.Award(p.league, "2026-11-09", DuelOutcome.Win, false, false, false, 50, T));
            Assert.AreEqual(8, League.Award(p.league, "2026-11-09", DuelOutcome.Lose, false, false, false, 50, T));
            Assert.AreEqual(38, p.league.points);
            Assert.AreEqual(2, p.league.duels);
            Assert.False(League.RewardPending(p.league, "2026-11-09"), "la semaine n'est pas finie");

            // Lundi suivant : les points repartent de zéro, la semaine finie attend sa récompense.
            Assert.True(League.RewardPending(p.league, "2026-11-16"));
            Assert.AreEqual(0, p.league.points);
            Assert.AreEqual("2026-11-09", p.league.lastWeek);
            Assert.AreEqual(38, p.league.lastPoints);
            Assert.AreEqual(250, League.Claim(p, 5, T, out var div));
            Assert.AreEqual(Division.Elite, div);
            Assert.AreEqual(coins0 + 250, p.coins);
            Assert.False(League.RewardPending(p.league, "2026-11-16"));
            Assert.AreEqual(0, League.Claim(p, 1, T, out _), "jamais deux fois");
            Assert.AreEqual(coins0 + 250, p.coins);
            Assert.AreEqual((int)Division.Elite, p.league.bestDivision);

            // Horloge du téléphone reculée : on ne revient pas à une semaine passée, les points restent.
            League.Award(p.league, "2026-11-16", DuelOutcome.Win, false, false, false, 50, T);
            League.Roll(p.league, "2026-11-02");
            Assert.AreEqual("2026-11-16", p.league.week);
            Assert.AreEqual(30, p.league.points);
            Assert.True(League.RewardPending(p.league, "2026-11-23"));
            Assert.AreEqual(0, League.Claim(p, 40, T, out _));

            // Une semaine sans jouer, puis une semaine jouée : pas de récompense pour la semaine vide.
            Assert.False(League.RewardPending(p.league, "2026-11-30"));
            League.Award(p.league, "2026-11-30", DuelOutcome.Win, true, false, false, 50, T);
            Assert.True(League.RewardPending(p.league, "2026-12-14"), "deux semaines plus tard : celle du 30 attend toujours");
            Assert.AreEqual("2026-11-30", p.league.lastWeek);
            Assert.AreEqual(0, League.Claim(p, 0, T, out div), "introuvable dans l'archive : soldée sans pièces");
            Assert.False(League.RewardPending(p.league, "2026-12-14"));
            Assert.AreEqual((int)Division.Elite, p.league.bestDivision, "une semaine non classée ne change pas la meilleure division");
        }

        [Test]
        public void LeagueStateSurvivesSaveAndOldSaves()
        {
            // Une sauvegarde de la 1.4 n'a pas de ligue : elle en reçoit une vide, rien d'autre ne bouge.
            var old = new Profile { league = null, coins = 123 };
            old.Sanitize();
            Assert.NotNull(old.league);
            Assert.AreEqual(0, old.league.points);
            Assert.AreEqual(123, old.coins);
            for (int i = 0; i < 20; i++) old.league.recentGhosts.Add("g" + i);
            old.league.Sanitize();
            Assert.AreEqual(8, old.league.recentGhosts.Count);
            Assert.AreEqual("g19", old.league.recentGhosts[7]);
        }

        [Test]
        public void GhostPlaybackEmitsEverythingInOrderAndStopsAfterThePlayerFalls()
        {
            var g = new GhostRecording { Seed = 42, Countdown = 3f, Who = new DuelHello { Name = "Lea", Head = 5 } };
            for (uint t = 0; t <= 30000; t += 200) g.Snapshots.Add(new DuelSnapshot { TimeMs = t, Score = (int)(t / 100) });
            g.FellMs = 28000; g.FellScore = 280; g.EndMs = 30000; g.FinalScore = 280; g.SurvivedMs = 25000;
            Assert.True(g.Usable(new DuelTuning(), T));

            // Jusqu'au bout : présentation, départ, photos dans l'ordre, chute à 28 s, fin à 30 s.
            var play = new GhostPlayback(g, 3f);
            var kinds = new List<DuelMsg>();
            var outp = new List<byte[]>();
            float fellAt = -1, finishedAt = -1;
            uint lastSnap = 0;
            for (int i = 0; i < 40 * 60; i++)
            {
                outp.Clear();
                play.Advance(1f / 60f, outp);
                foreach (var m in outp)
                {
                    var k = DuelProtocol.Kind(m);
                    kinds.Add(k);
                    if (k == DuelMsg.Fell && fellAt < 0) fellAt = play.Clock;
                    if (k == DuelMsg.Finished && finishedAt < 0) finishedAt = play.Clock;
                    if (k == DuelMsg.Snapshot && finishedAt < 0) { var s = DuelProtocol.ReadSnapshot(m); Assert.GreaterOrEqual(s.TimeMs, lastSnap); lastSnap = s.TimeMs; }
                }
            }
            Assert.AreEqual(DuelMsg.Hello, kinds[0]);
            Assert.AreEqual(DuelMsg.Start, kinds[1]);
            Assert.That(fellAt, Is.EqualTo(28f).Within(0.05f));
            Assert.That(finishedAt, Is.EqualTo(30f).Within(0.05f));
            Assert.AreEqual(1, kinds.Count(k => k == DuelMsg.Fell));
            Assert.AreEqual(1, kinds.Count(k => k == DuelMsg.Finished));
            Assert.Greater(kinds.Count(k => k == DuelMsg.Snapshot), 150);

            // Le joueur lâche à 10 s : le fantôme s'arrête à 13 s (fenêtre du dernier poids), debout, avec son score du moment.
            play = new GhostPlayback(g, 3f);
            kinds.Clear(); finishedAt = -1; int finishScore = -1;
            for (int i = 0; i < 20 * 60; i++)
            {
                outp.Clear();
                play.Advance(1f / 60f, outp);
                if (play.Clock >= 10f) play.PlayerFell();
                foreach (var m in outp)
                {
                    var k = DuelProtocol.Kind(m);
                    kinds.Add(k);
                    if (k == DuelMsg.Finished && finishedAt < 0) { finishedAt = play.Clock; DuelProtocol.ReadFinished(m, out finishScore, out _); }
                }
            }
            Assert.That(finishedAt, Is.EqualTo(13f).Within(0.1f));
            Assert.AreEqual(0, kinds.Count(k => k == DuelMsg.Fell), "le fantôme n'a pas lâché");
            Assert.That(finishScore, Is.InRange(125, 131));
        }
    }

    public class HeadTests
    {
        [Test]
        public void HeadIdsAreUniqueAndStable()
        {
            Assert.AreEqual(66, HeadCatalog.All.Count);
            Assert.AreEqual(66, HeadCatalog.All.Select(h => h.Id).Distinct().Count());
            Assert.AreEqual(64, HeadCatalog.All.First(h => h.Unlock == UnlockKind.Friends).Id, "Duo");
            Assert.AreEqual(65, HeadCatalog.All.First(h => h.Unlock == UnlockKind.Wheel).Id, "Chanceux");
            Assert.AreEqual(1, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.DuelWins));
            Assert.AreEqual(1, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Ads));
            Assert.AreEqual(63, HeadCatalog.All.First(h => h.Unlock == UnlockKind.Ads).Id, "la Superstar arrive à la fin");
            // Les identifiants sont sauvegardés : jamais renumérotés. 0 à 65 dans l'ordre, puis les numéros retirés (66)
            // sautés, et les nouvelles têtes à la suite.
            for (int i = 0; i <= 65; i++) Assert.AreEqual(i, HeadCatalog.All[i].Id, "les identifiants sont sauvegardés : ne jamais les renuméroter");
            for (int i = 1; i < HeadCatalog.All.Count; i++) Assert.Greater(HeadCatalog.All[i].Id, HeadCatalog.All[i - 1].Id);
            Assert.AreEqual(UnlockKind.Default, HeadCatalog.Get(0).Unlock);
            Assert.AreEqual(20, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Score));
            Assert.AreEqual(13, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Level));
            Assert.AreEqual(16, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Shop));
            Assert.AreEqual(6, HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Premium));
            Assert.True(HeadCatalog.All.Where(h => h.Unlock == UnlockKind.Premium).All(h => Shop.Get(h.Product) != null));
        }

        [Test]
        public void ScoreThresholdsAreIncreasingAndMostlyReachable()
        {
            var t = HeadCatalog.ScoreThresholds();
            Assert.AreEqual(20, t.Count);
            for (int i = 1; i < t.Count; i++) Assert.That(t[i], Is.GreaterThan(t[i - 1]));
            // V18 : des têtes plus rares, mais toujours des objectifs proches pour les débutants (au moins 5 sous 4 000 points)
            // et des têtes réservées aux meilleurs (au moins 4 au-delà de 25 000).
            Assert.That(t.Count(x => x <= 4000), Is.GreaterThanOrEqualTo(5));
            Assert.That(t.Count(x => x >= 25000), Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void HeadsHaveNamesInBothLanguages()
        {
            foreach (var h in HeadCatalog.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(h.NameFr));
                Assert.False(string.IsNullOrWhiteSpace(h.NameEn));
            }
        }

        [Test]
        public void ScoreUnlocksHeads()
        {
            var p = new Profile(); p.Sanitize();
            // 1 600 points : les paliers 300, 700 et 1 200 sont atteints.
            var r = Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 1600 }, "2026-11-02");
            Assert.AreEqual(3, r.NewHeads.Count);
            Assert.True(r.NewBest);
            Assert.AreEqual(1800, r.NextHead.UnlockValue);
            var r2 = Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 900 }, "2026-11-02");
            Assert.AreEqual(0, r2.NewHeads.Count);
            Assert.False(r2.NewBest);
            CollectionAssert.AreEqual(new[] { 1600, 900 }, p.personalTop);
        }

        [Test]
        public void SpecialHeadsUnlock()
        {
            var p = new Profile(); p.Sanitize();
            var r = Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 10, BestSave = 0.98f, Balloons = 1, BestChain = 5 }, "2026-11-02");
            var names = r.NewHeads.Select(h => h.Unlock).ToList();
            CollectionAssert.Contains(names, UnlockKind.NearMiss);
            CollectionAssert.Contains(names, UnlockKind.Balloon);
            CollectionAssert.Contains(names, UnlockKind.Combo);
            CollectionAssert.DoesNotContain(names, UnlockKind.WorldTop);
        }

        [Test]
        public void EveryHeadCanBeDrawn()
        {
            foreach (var h in HeadCatalog.All)
            {
                var img = HeadArtist.Portrait(h, 64);
                Assert.That(img.Pixels.Count(p => p.a > 0), Is.GreaterThan(64 * 64 / 6), h.NameFr);
            }
            foreach (Expression e in Enum.GetValues(typeof(Expression)))
                Assert.That(HeadArtist.Face(e, 64).Pixels.Count(p => p.a > 0), Is.GreaterThan(10), e.ToString());
        }
    }

    public class DailyTests
    {
        [Test]
        public void StreakCountsConsecutiveDays()
        {
            var d = new DailyState();
            Daily.RegisterPlay(d, "2026-11-01");
            Daily.RegisterPlay(d, "2026-11-01");
            Daily.RegisterPlay(d, "2026-11-02");
            Daily.RegisterPlay(d, "2026-11-03");
            Assert.AreEqual(3, d.streak);
            Daily.RegisterPlay(d, "2026-11-05");
            Assert.AreEqual(1, d.streak);
            Assert.AreEqual(3, d.bestStreak);
        }

        [Test]
        public void DisplayedStreakDropsAfterMissedDay()
        {
            var d = new DailyState { lastPlayedDate = "2026-11-01", streak = 5 };
            Assert.AreEqual(5, Daily.CurrentStreak(d, "2026-11-02"));
            Assert.AreEqual(0, Daily.CurrentStreak(d, "2026-11-04"));
        }

        [Test]
        public void StreakCrossesMonthBoundary()
        {
            var d = new DailyState();
            Daily.RegisterPlay(d, "2026-10-31");
            Daily.RegisterPlay(d, "2026-11-01");
            Assert.AreEqual(2, d.streak);
        }

        [Test]
        public void DailyRunUnlocksStreakHead()
        {
            var p = new Profile(); p.Sanitize();
            p.daily = new DailyState { lastPlayedDate = "2026-11-01", streak = 4 };
            var r = Progress.ApplyRun(p, GameMode.Daily, new RunAchievements { Score = 100 }, "2026-11-02");
            Assert.AreEqual(5, r.DailyStreak);
            Assert.True(r.NewHeads.Any(h => h.Unlock == UnlockKind.DailyStreak && h.UnlockValue == 5));
            Assert.AreEqual(100, p.daily.bestScoreToday);
        }
    }

    public class TuningFileTests
    {
        static string TuningPath()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            Assert.NotNull(dir, "racine du projet introuvable");
            return Path.Combine(dir.FullName, "Assets/Bascule/Resources/bascule_tuning.json");
        }

        [Test]
        public void TuningFileParsesAndMatchesCodeFields()
        {
            var json = File.ReadAllText(TuningPath());
            var opts = new JsonSerializerOptions { IncludeFields = true, ReadCommentHandling = JsonCommentHandling.Disallow };
            var t = JsonSerializer.Deserialize<Tuning>(json, opts);
            Assert.NotNull(t);
            t.Sanitize();
            Assert.That(t.spawn.tiers.Length, Is.GreaterThan(0));

            // Toute clé du JSON doit exister dans le code (sinon JsonUtility l'ignore en silence).
            using var doc = JsonDocument.Parse(json);
            CheckKeys(doc.RootElement, typeof(Tuning), "");
        }

        static void CheckKeys(JsonElement el, Type type, string path)
        {
            if (el.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in el.EnumerateObject())
            {
                if (prop.Name.StartsWith("_")) continue; // commentaires
                var field = type.GetField(prop.Name);
                Assert.NotNull(field, $"clé inconnue dans bascule_tuning.json : {path}{prop.Name}");
                var ft = field.FieldType;
                if (ft.IsArray && prop.Value.ValueKind == JsonValueKind.Array && !ft.GetElementType().IsPrimitive)
                    foreach (var item in prop.Value.EnumerateArray()) CheckKeys(item, ft.GetElementType(), path + prop.Name + "[].");
                else if (!ft.IsPrimitive && ft != typeof(string) && !ft.IsArray)
                    CheckKeys(prop.Value, ft, path + prop.Name + ".");
            }
        }
    }

    public class SnapshotTests
    {
        [Test]
        public void SnapshotRoundTripsThroughJson()
        {
            var s = new RunSnapshot
            {
                mode = GameMode.Daily, seed = 77, dayKey = "2026-11-02", rngState = 123456u, dropIndex = 12,
                current = 4, next = 2, plankAngle = 7.5f, score = 840,
                weights = new List<WeightState> { new WeightState { value = 8, x = 1.2f, y = -2f, landed = true } },
            };
            var opts = new JsonSerializerOptions { IncludeFields = true };
            var back = JsonSerializer.Deserialize<RunSnapshot>(JsonSerializer.Serialize(s, opts), opts);
            Assert.AreEqual(s.rngState, back.rngState);
            Assert.AreEqual(8, back.weights[0].value);
            Assert.AreEqual(GameMode.Daily, back.mode);
        }
    }

    public class LocTests
    {
        [Test]
        public void EveryKeyHasFrenchAndEnglish()
        {
            foreach (var key in Loc.Keys)
            {
                Loc.Language = "fr"; var fr = Loc.Get(key);
                Loc.Language = "en"; var en = Loc.Get(key);
                Assert.False(string.IsNullOrEmpty(fr), key);
                Assert.False(string.IsNullOrEmpty(en), key);
            }
            Loc.Language = "fr";
        }

        [Test]
        public void ScoreFormatting()
        {
            Loc.Language = "fr";
            Assert.AreEqual("12 500", Loc.FormatScore(12500));
            Loc.Language = "en";
            Assert.AreEqual("12,500", Loc.FormatScore(12500));
            Loc.Language = "fr";
        }

        [Test]
        public void LanguageResolution()
        {
            Assert.AreEqual("fr", Loc.Resolve("en", "German"), "jeu publié en français uniquement");
            Loc.FrenchOnly = false;
            try
            {
                Assert.AreEqual("fr", Loc.Resolve("", "French"));
                Assert.AreEqual("en", Loc.Resolve("", "German"));
                Assert.AreEqual("fr", Loc.Resolve("fr", "English"));
            }
            finally { Loc.FrenchOnly = true; }
        }
    }

    public class SynthTests
    {
        [Test]
        public void SoundsAreBoundedAndNonEmpty()
        {
            var all = new[] { Synth.Thud(1), Synth.Thud(64), Synth.Pop(2), Synth.Pop(128), Synth.Lift(), Synth.Fall(), Synth.Chime(3), Synth.Phew(), Synth.Lost(), Synth.Click() };
            foreach (var s in all)
            {
                Assert.That(s.Length, Is.GreaterThan(100));
                Assert.That(s.Max(Math.Abs), Is.LessThanOrEqualTo(1f));
                Assert.That(s.Max(Math.Abs), Is.GreaterThan(0.05f));
            }
        }
    }

    public class RasterTests
    {
        [Test]
        public void CircleCoversExpectedArea()
        {
            var r = new Raster(100, 100);
            r.Circle(50, 50, 30, Rgba.White);
            float covered = r.Pixels.Sum(p => p.a / 255f);
            Assert.That(covered, Is.EqualTo(Math.PI * 30 * 30).Within(40));
        }

        [Test]
        public void EdgesAreAntiAliased()
        {
            var r = new Raster(64, 64);
            r.Circle(32, 32, 20.3f, Rgba.White);
            Assert.True(r.Pixels.Any(p => p.a > 0 && p.a < 255));
        }

        [Test]
        public void BleedKeepsAlphaButFillsColor()
        {
            var r = new Raster(16, 16);
            r.Circle(8, 8, 4, new Rgba(255, 0, 0));
            r.BleedEdges();
            var corner = r.Get(8, 13);
            Assert.AreEqual(0, corner.a);
            Assert.AreEqual(255, corner.r);
        }

        [Test]
        public void HexParsing()
        {
            var c = Rgba.FromHex("#3A86FF");
            Assert.AreEqual(0x3A, c.r); Assert.AreEqual(0x86, c.g); Assert.AreEqual(0xFF, c.b); Assert.AreEqual(255, c.a);
        }
    }
}

namespace Bascule.Core.Tests
{
    public class ChallengeCodeTests
    {
        [Test]
        public void RoundTrip()
        {
            foreach (uint seed in new uint[] { 1, 42, 0xDEADBEEF, uint.MaxValue, Daily.Seed("2026-11-02") })
            {
                var code = ChallengeCode.Encode(seed);
                Assert.AreEqual(9, code.Length);
                Assert.True(ChallengeCode.TryDecode(code, out var back), code);
                Assert.AreEqual(seed, back);
                Assert.True(ChallengeCode.TryDecode(code.ToLowerInvariant().Replace("-", " "), out back));
                Assert.AreEqual(seed, back);
            }
        }

        [Test]
        public void TyposAreRejected()
        {
            var code = ChallengeCode.Encode(123456789);
            var chars = code.ToCharArray();
            chars[2] = chars[2] == 'A' ? 'B' : 'A';
            Assert.False(ChallengeCode.TryDecode(new string(chars), out _));
            Assert.False(ChallengeCode.TryDecode("ABC", out _));
            Assert.False(ChallengeCode.TryDecode("", out _));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class ArtTests
    {
        static int Opaque(Raster r) => r.Pixels.Count(p => p.a > 0);

        [Test]
        public void EveryWeightIsDrawn()
        {
            foreach (var v in WeightCatalog.Values)
                Assert.That(Opaque(WeightArtist.Draw(v, 96)), Is.GreaterThan(96 * 96 / 3), v.ToString());
        }

        [Test]
        public void MascotPartsAreDrawnWithPivotInside()
        {
            foreach (var kv in MascotArtist.Build(Rgba.FromHex("#F1C27D")))
            {
                var p = kv.Value;
                Assert.That(Opaque(p.Image), Is.GreaterThan(100), kv.Key);
                Assert.That(p.PivotX, Is.InRange(0, p.Image.Width), kv.Key);
                Assert.That(p.PivotY, Is.InRange(0, p.Image.Height), kv.Key);
            }
        }

        [Test]
        public void NewSeasonResetsProgressButKeepsPurchasesAndIdentity()
        {
            var old = new Profile { season = 0, playerId = "abc", pseudo = "Toto", bestScore = 8850, gamesPlayed = 300, coins = 9204, coinsBought = 1000, level = 19, xp = 50, adsRemoved = true };
            old.unlockedHeads.AddRange(new[] { 1, 2, 3, 40 });
            old.ownedProducts.Add(Shop.Legends);
            old.settings.sound = false;
            old.friends.Add("ami");
            old.personalTop.Add(8850);
            Assert.True(Seasons.IsOld(old));
            var p = Seasons.Reset(old);
            Assert.AreEqual(0, p.bestScore);
            Assert.AreEqual(0, p.gamesPlayed);
            Assert.AreEqual(1, p.level);
            Assert.AreEqual(0, p.personalTop.Count);
            Assert.AreEqual(0, p.coins, "lancement : toutes les pièces repartent de zéro");
            Assert.AreEqual(0, p.coinsBought);
            Assert.AreEqual(0, Seasons.Reset(new Profile { season = 0, coins = 300, coinsBought = 2200 }).coins, "lancement : aucune pièce gardée");
            Assert.False(p.unlockedHeads.Contains(1) || p.unlockedHeads.Contains(40), "têtes gagnées remises à zéro");
            foreach (var h in HeadCatalog.All)
                if (h.Unlock == UnlockKind.Premium && h.Product == Shop.Legends) Assert.True(p.unlockedHeads.Contains(h.Id), "têtes payées gardées");
            Assert.True(p.adsRemoved);
            Assert.AreEqual("abc", p.playerId);
            Assert.AreEqual("Toto", p.pseudo);
            Assert.False(p.settings.sound);
            Assert.AreEqual(1, p.friends.Count);
            Assert.True(p.welcomePending);
            Assert.False(Seasons.IsOld(p));
            Assert.AreEqual(0, HeadNews.Count(p), "pas de pastille « nouveau » pour les têtes rendues");
        }

        [Test]
        public void SanitizeRepairsDamagedSaves()
        {
            var p = new Profile { pseudo = "Un pseudo beaucoup trop long pour le classement", selectedHead = 9999, selectedPlank = 9999, bestScore = -5 };
            p.unlockedHeads.AddRange(new[] { 1, 1, 9999, 2 });
            p.ownedPlanks.AddRange(new[] { 9999, 0 });
            p.gift = null;
            p.Sanitize();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, p.unlockedHeads, "doublons et têtes inconnues retirés, ordre gardé");
            CollectionAssert.AreEqual(new[] { 0 }, p.ownedPlanks);
            Assert.AreEqual(0, p.selectedHead);
            Assert.AreEqual(0, p.selectedPlank);
            Assert.AreEqual(0, p.bestScore);
            Assert.LessOrEqual(p.pseudo.Length, PseudoFilter.MaxLength);
            // Un pseudo valide n'est jamais modifié par la vérification.
            var ok = new Profile { pseudo = "Toto_42" };
            ok.Sanitize(); ok.Sanitize();
            Assert.AreEqual("Toto_42", ok.pseudo);
            var rude = new Profile { pseudo = "connard" };
            rude.Sanitize();
            Assert.AreEqual("", rude.pseudo);
        }

        [Test]
        public void OtherPlayersNamesAreFilteredOnDisplay()
        {
            Assert.AreEqual("Toto", PseudoFilter.Display("Toto", "Joueur"));
            Assert.AreEqual("Joueur", PseudoFilter.Display("gros_connard", "Joueur"));
            Assert.AreEqual("Joueur", PseudoFilter.Display("", "Joueur"));
            Assert.AreEqual("Joueur", PseudoFilter.Display(null, "Joueur"));
            Assert.LessOrEqual(PseudoFilter.Display(new string('a', 200), "Joueur").Length, PseudoFilter.MaxLength + 4);
        }

        [Test]
        public void FrenchTextsUseNonBreakingSpaces()
        {
            const char nb = '\u00A0';
            Assert.AreEqual("Bravo" + nb + "!", Loc.FrenchTypography("Bravo !"));
            Assert.AreEqual("Record" + nb + ": {0}", Loc.FrenchTypography("Record : {0}"));
            Assert.AreEqual("«" + nb + "{0}" + nb + "»", Loc.FrenchTypography("« {0} »"));
            Assert.AreEqual("penche à {1}" + nb + "%" + nb + "!", Loc.FrenchTypography("penche à {1} % !"));
            Assert.AreEqual("https://orpheex.fr 0:08", Loc.FrenchTypography("https://orpheex.fr 0:08"), "liens et durées intacts");
            // Tous les textes français du jeu sont passés par là : plus aucun signe double précédé d'un espace sécable.
            string old = Loc.Language;
            Loc.Language = "fr";
            try
            {
                foreach (var key in new[] { "new_best", "best", "title_label", "chest_open", "welcome_title", "duel_leans" })
                {
                    string s = Loc.Get(key);
                    Assert.False(s.Contains(" !") || s.Contains(" :") || s.Contains(" »") || s.Contains("« ") || s.Contains(" %"), key + " : " + s);
                }
            }
            finally { Loc.Language = old; }
        }

        [Test]
        public void DailyChallengeProgressIsTrackedLive()
        {
            var merges = new DailyChallenge.Def { Key = "merges", A = 60, Met = r => r.Merges >= 60 };
            var p = DailyChallenge.Track(merges, new RunStats { Merges = 32 });
            Assert.AreEqual(32, p.Current); Assert.AreEqual(60, p.Target); Assert.False(p.Done);
            Assert.That(p.Fraction, Is.EqualTo(32f / 60f).Within(1e-4));
            Assert.True(DailyChallenge.Track(merges, new RunStats { Merges = 75 }).Done);
            Assert.AreEqual(60, DailyChallenge.Track(merges, new RunStats { Merges = 75 }).Current, "plafonné à l'objectif");
            var clean = new DailyChallenge.Def { Key = "clean_score", A = 3000, Met = r => r.Score >= 3000 && r.LostWeights == 0 };
            Assert.True(DailyChallenge.Track(clean, new RunStats { Score = 1200, LostWeights = 1 }).Failed);
            var hold = DailyChallenge.Track(new DailyChallenge.Def { Key = "hold", A = 180, Met = r => r.Seconds >= 180 }, new RunStats { Seconds = 90.6f });
            Assert.True(hold.Time); Assert.AreEqual(90, hold.Current);
            // Chacun des 28 défis a une valeur à suivre.
            foreach (var d in DailyChallenge.All)
                Assert.AreEqual(d.A, DailyChallenge.Track(d, new RunStats { Score = 99999, Merges = 999, Seconds = 999, MaxValue = 999, Drops = 999, Balloons = 9, Gusts = 9, BestSave = 1f, BestChain = 9 }).Current, d.Key);
        }

        [Test]
        public void BigRunUnlocksEveryScoreHeadItReaches()
        {
            var p = new Profile();
            var r = Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 20000 }, "2026-11-11");
            foreach (var h in HeadCatalog.All)
                if (h.Unlock == UnlockKind.Score && h.UnlockValue <= 20000) Assert.True(p.unlockedHeads.Contains(h.Id), "palier " + h.UnlockValue);
            Assert.False(r.NewHeads.Exists(h => h.Unlock == UnlockKind.Score && h.UnlockValue > 20000));
        }

        [Test]
        public void DailyBoardCountsEveryRunOfTheDay()
        {
            var p = new Profile();
            Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 1200 }, "2026-11-11");
            Assert.AreEqual(1200, p.daily.bestScoreToday, "une partie normale compte pour le classement du jour");
            Progress.ApplyRun(p, GameMode.Daily, new RunAchievements { Score = 800 }, "2026-11-11");
            Assert.AreEqual(1200, p.daily.bestScoreToday);
            Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 300 }, "2026-11-12");
            Assert.AreEqual(300, p.daily.bestScoreToday, "nouveau jour, nouveau meilleur score");
        }

        [Test]
        public void FriendsAreMutualUnlessRemoved()
        {
            var p = new Profile();
            Assert.True(FriendCode.AddedBy(p, "ami-qui-m-a-ajoute-123", "moi"));
            Assert.True(p.friends.Contains("ami-qui-m-a-ajoute-123"));
            Assert.True(FriendCode.Remove(p, "ami-qui-m-a-ajoute-123"));
            Assert.False(FriendCode.AddedBy(p, "ami-qui-m-a-ajoute-123", "moi"), "un ami retiré ne revient pas tout seul");
            Assert.True(FriendCode.Add(p, "ami-qui-m-a-ajoute-123", "moi"), "mais on peut le rajouter soi-même");
            Assert.False(FriendCode.AddedBy(p, "moi", "moi"));
        }

        [Test]
        public void DuelMissionsProgressWithDuels()
        {
            var s = new MissionState { list = new List<Mission>
            {
                new Mission { kind = MissionKind.PlayDuels, target = 2, reward = 90 },
                new Mission { kind = MissionKind.WinDuels, target = 1, reward = 90 },
            } };
            Assert.AreEqual(0, Missions.ApplyDuel(s, false).Count);
            var done = Missions.ApplyDuel(s, true);
            Assert.AreEqual(2, done.Count, "2 duels joués et 1 gagné");
            Missions.DuelsAvailable = false;
            for (uint seed = 0; seed < 200; seed++)
                foreach (var m in Missions.Generate(seed, 20, 9000, 50))
                    Assert.False(m.kind == MissionKind.PlayDuels || m.kind == MissionKind.WinDuels, "pas de mission de duel sans duel");
        }

        [Test]
        public void EveryHeadHasItsOwnOutfit()
        {
            var seen = new HashSet<string>();
            foreach (var h in HeadCatalog.All)
                Assert.True(seen.Add(OutfitCatalog.For(h.Id).Signature), "tenue en double : tête " + h.Id);
            Assert.That(OutfitCatalog.For(9999), Is.Not.Null);
        }

        [Test]
        public void EveryKindOfOutfitIsDrawnWithPivotInside()
        {
            // Une tête par sorte de haut, de bas et de chaussures (tout dessiner prendrait trop de temps).
            var kinds = new HashSet<string>();
            foreach (var h in HeadCatalog.All)
            {
                var o = OutfitCatalog.For(h.Id);
                if (!kinds.Add("t" + o.Top) & !kinds.Add("b" + o.Bottom) & !kinds.Add("s" + o.Shoe) & !kinds.Add("m" + o.Motif) & !kinds.Add("e" + o.Emblem)) continue;
                var parts = MascotArtist.Build(h.BodyKey);
                Assert.AreEqual(9, parts.Count);
                foreach (var kv in parts)
                {
                    var p = kv.Value;
                    Assert.That(Opaque(p.Image), Is.GreaterThan(100), h.Id + " " + kv.Key);
                    Assert.That(p.PivotX, Is.InRange(0, p.Image.Width), kv.Key);
                    Assert.That(p.PivotY, Is.InRange(0, p.Image.Height), kv.Key);
                }
            }
        }

        [Test]
        public void EveryThemeAndIconIsDrawn()
        {
            foreach (var t in Theme.All)
            {
                Assert.That(Opaque(SceneArtist.Hills(t, false, 192, 60)), Is.GreaterThan(500), t.Name);
                Assert.That(Opaque(SceneArtist.Ground(t, 128, 48)), Is.GreaterThan(1000), t.Name);
                Assert.That(Opaque(SceneArtist.Cloud(0, t, 96, 48)), Is.GreaterThan(300), t.Name);
            }
            foreach (Icon ic in Enum.GetValues(typeof(Icon)))
                Assert.That(Opaque(UiArtist.DrawIcon(ic, 64)), Is.GreaterThan(300), ic.ToString());
            Assert.That(Opaque(SceneArtist.Plank(6.6f, 0.32f, 40f, out int border)), Is.GreaterThan(2000));
            Assert.That(border, Is.GreaterThan(0));
        }

        [Test]
        public void ArmsCanReachThePlankAtAnyTilt()
        {
            // Mains sur la planche jusqu'à l'angle de défaite + dépassement : le bras ne doit jamais être trop court.
            var t = new PlankTuning();
            for (float angle = -(t.maxAngle + t.overshootAngle); angle <= t.maxAngle + t.overshootAngle; angle += 3f)
            {
                double rad = -angle * Math.PI / 180;
                for (int side = -1; side <= 1; side += 2)
                {
                    float hx = (float)(side * Math.Cos(rad) * MascotRig.HandReach), hy = MascotRig.PivotHeight + (float)(side * Math.Sin(rad) * MascotRig.HandReach);
                    float dx = hx - side * MascotRig.ShoulderX, dy = hy - MascotRig.ShoulderY;
                    // Jusqu'à l'angle max : le bras suffit. Au-delà (dépassement avant la chute) : étirement limité de l'avant-bras.
                    float limit = Math.Abs(angle) <= t.maxAngle ? MascotRig.UpperArm + MascotRig.Forearm + 0.02f : MascotRig.UpperArm + MascotRig.Forearm * 1.25f;
                    Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.LessThanOrEqualTo(limit), $"angle {angle}");
                }
            }
        }

        [Test]
        public void TwoBoneKeepsBoneLengths()
        {
            MascotRig.TwoBone(0, 0, 1.2f, 0.8f, 1f, 1f, 1, out float jx, out float jy);
            Assert.AreEqual(1f, Math.Sqrt(jx * jx + jy * jy), 1e-3);
            Assert.AreEqual(1f, Math.Sqrt((1.2f - jx) * (1.2f - jx) + (0.8f - jy) * (0.8f - jy)), 1e-3);
        }
    }
}


namespace Bascule.Core.Tests
{
    public class LostWeightTests
    {
        [Test]
        public void LostWeightCostsPointsAndBreaksTheCombo()
        {
            var t = new ScoreTuning();
            var s = new ScoreKeeper(t);
            s.OnDrop(); s.OnMerge(64, 0);
            int before = s.Score;
            int penalty = s.OnWeightLost(8);
            Assert.AreEqual((int)(8 * t.lostWeightPenalty), penalty);
            Assert.AreEqual(before - penalty, s.Score);
            Assert.AreEqual(0, s.Chain);
            Assert.AreEqual(1, s.LostWeights);
        }

        [Test]
        public void ScoreNeverGoesNegative()
        {
            var s = new ScoreKeeper(new ScoreTuning());
            s.OnWeightLost(16);
            Assert.AreEqual(0, s.Score);
        }
    }
}


namespace Bascule.Core.Tests
{
    public class EconomyTests
    {
        [Test]
        public void NewHeadsBadgeUntilSeen()
        {
            // Ancienne sauvegarde : les têtes déjà gagnées ne font pas de pastille.
            var old = new Profile { unlockedHeads = new List<int> { 0, 3, 5 }, seenHeadsInit = false };
            old.Sanitize();
            Assert.AreEqual(0, HeadNews.Count(old));
            // Nouvelle tête gagnée : pastille 1, jusqu'à ce qu'elle soit vue.
            old.unlockedHeads.Add(7);
            Assert.AreEqual(1, HeadNews.Count(old));
            Assert.True(HeadNews.IsNew(old, 7));
            Assert.True(HeadNews.MarkSeen(old, new[] { 7 }));
            Assert.AreEqual(0, HeadNews.Count(old));
            Assert.False(HeadNews.MarkSeen(old, new[] { 7 }));
            // Profil neuf : la tête de départ est déjà vue.
            var fresh = new Profile(); fresh.Sanitize();
            Assert.AreEqual(0, HeadNews.Count(fresh));
        }

        [Test]
        public void NewMissionsProgress()
        {
            var gust = new Mission { kind = MissionKind.SurviveGusts, target = 2 };
            Missions.Step(gust, new RunStats { Gusts = 1 }); Assert.AreEqual(1, gust.progress);
            Missions.Step(gust, new RunStats { Gusts = 2 }); Assert.AreEqual(2, gust.progress);
            var hold = new Mission { kind = MissionKind.HoldSeconds, target = 120 };
            Missions.Step(hold, new RunStats { Seconds = 125.4f }); Assert.AreEqual(125, hold.progress);
            var clean = new Mission { kind = MissionKind.CleanScore, target = 1500 };
            Missions.Step(clean, new RunStats { Score = 3000, LostWeights = 1 }); Assert.AreEqual(0, clean.progress, "un poids perdu : ne compte pas");
            Missions.Step(clean, new RunStats { Score = 1600, LostWeights = 0 }); Assert.AreEqual(1600, clean.progress);
            Assert.AreEqual("Tiens 2 min sans faire tomber Basile", Loc.MissionText(new Mission { kind = MissionKind.HoldSeconds, target = 120 }));
            Assert.AreEqual("1 min 30", Loc.FormatDuration(90));
        }

        [Test]
        public void GustMissionOnlyOnceTheWindBlows()
        {
            for (uint seed = 1; seed < 200; seed++)
                Assert.False(Missions.Generate(seed, 20, 3000, 100).Exists(m => m.kind == MissionKind.SurviveGusts), "record 3 000 : jamais de vent");
            bool seen = false;
            for (uint seed = 1; seed < 400 && !seen; seed++)
                seen = Missions.Generate(seed, 20, 9000, 100).Exists(m => m.kind == MissionKind.SurviveGusts);
            Assert.True(seen);
        }

        [Test]
        public void DailyChallengesNeverRepeatWithinThreeWeeks()
        {
            Assert.GreaterOrEqual(DailyChallenge.All.Length, 21);
            var ids = new HashSet<string>();
            foreach (var d in DailyChallenge.All) Assert.True(ids.Add(d.Key + "/" + d.A + "/" + d.B), "défi en double : " + d.Key);
            var day = new DateTime(2026, 1, 1);
            for (int start = 0; start < 3 * 365; start++)
            {
                var seen = new HashSet<DailyChallenge.Def>();
                for (int k = 0; k < 21; k++)
                    Assert.True(seen.Add(DailyChallenge.For(day.AddDays(start + k).ToString("yyyy-MM-dd"))), "répété dans les 21 jours à partir du jour " + start);
            }
            // Même défi pour tout le monde le même jour, et un texte pour chacun.
            Assert.AreSame(DailyChallenge.For("2026-11-08"), DailyChallenge.For("2026-11-08"));
            foreach (var d in DailyChallenge.All)
            {
                var text = Loc.DailyText(d);
                Assert.False(string.IsNullOrEmpty(text) || text.StartsWith("dc_"), "texte manquant : " + d.Key);
            }
            Assert.AreEqual("Tiens 3 min sans tomber", Loc.DailyText(DailyChallenge.All[2]));
            Assert.AreEqual("Fais 4 000 points et un combo ×4", Loc.DailyText(DailyChallenge.All[7]).Replace('\u00A0', ' ').Replace('\u202F', ' '));
        }

        [Test]
        public void DailyChallengeHarderAndPaidOnce()
        {
            // Jour où le défi est « 4 000 points et un combo ×4 » : une seule condition ne suffit pas.
            var start = new DateTime(2026, 1, 1);
            string day = null;
            for (int k = 0; k < 28 && day == null; k++) { var key = start.AddDays(k).ToString("yyyy-MM-dd"); if (DailyChallenge.For(key).Key == "score_combo") day = key; }
            Assert.NotNull(day);
            var t = new EconomyTuning();
            var p = new Profile();
            Assert.IsNull(EconomyRun.Apply(t, p, new RunStats { Daily = true, DayKey = day, Score = 5000, BestChain = 3 }, day).DailyObjective);
            Assert.IsNull(EconomyRun.Apply(t, p, new RunStats { Daily = true, DayKey = day, Score = 2000, BestChain = 5 }, day).DailyObjective);
            var win = EconomyRun.Apply(t, p, new RunStats { Daily = true, DayKey = day, Score = 4200, BestChain = 4 }, day);
            Assert.NotNull(win.DailyObjective);
            Assert.AreEqual(DailyChallenge.Reward, win.DailyCoins);
            Assert.IsNull(EconomyRun.Apply(t, p, new RunStats { Daily = true, DayKey = day, Score = 9000, BestChain = 6 }, day).DailyObjective, "une fois par jour");
            Assert.IsNull(EconomyRun.Apply(t, new Profile(), new RunStats { Daily = false, Score = 9000, BestChain = 6 }, day).DailyObjective, "seulement en défi du jour");
        }

        static Profile NewProfile() { var p = new Profile(); p.Sanitize(); return p; }

        [Test]
        public void LevelsNeedMoreXpEachTimeAndPayCoins()
        {
            var t = new EconomyTuning();
            Assert.That(Levels.XpToNext(t, 2), Is.GreaterThan(Levels.XpToNext(t, 1)));
            var p = NewProfile();
            int gained = Levels.AddXp(t, p, Levels.XpToNext(t, 1) + 10, out int coins);
            Assert.AreEqual(1, gained);
            Assert.AreEqual(2, p.level);
            Assert.AreEqual(10, p.xp);
            Assert.AreEqual(Levels.LevelCoins(t, 2), coins);
            Assert.AreEqual(coins, p.coins);
        }

        [Test]
        public void LevelPacingMatchesTheDesign()
        {
            // Partie médiane simulée : environ 1 350 points (docs/RAPPORT.md).
            var t = new EconomyTuning();
            var p = NewProfile();
            int games = 0;
            while (p.level < 10 && games < 1000) { Levels.AddXp(t, p, Levels.GameXp(t, 1350), out _); games++; }
            Assert.That(games, Is.InRange(12, 30), "niveau 10 en une à deux semaines de jeu régulier");
        }

        [Test]
        public void RunGivesCoinsXpAndLevelHeads()
        {
            var t = new EconomyTuning();
            var p = NewProfile();
            p.xp = 0;
            var g = EconomyRun.Apply(t, p, new RunStats { Score = 50000, Merges = 200, MaxValue = 64, BestChain = 3 }, "2026-11-03");
            Assert.AreEqual(50000 / t.scorePerCoin, g.Coins);
            Assert.That(g.LevelsGained, Is.GreaterThan(2));
            Assert.That(g.LevelHeads.Count, Is.GreaterThan(0));
            Assert.True(g.LevelHeads.All(h => p.unlockedHeads.Contains(h.Id)));
            Assert.AreEqual(p.coins, g.Total);
        }

        [Test]
        public void ChestOpensEveryThreeGamesAndPaysCoins()
        {
            var t = new EconomyTuning();
            var p = NewProfile();
            p.chestsOpened = 1; // le tout premier coffre (dès la 1re partie) est vérifié dans LiveOpsTests
            int coins0 = p.coins;
            var g1 = EconomyRun.Apply(t, p, new RunStats { Score = 100, Seed = 7 }, "2026-11-03");
            var g2 = EconomyRun.Apply(t, p, new RunStats { Score = 100, Seed = 8 }, "2026-11-03");
            Assert.Null(g1.Chest); Assert.Null(g2.Chest);
            Assert.AreEqual(2, g2.ChestProgress);
            var g3 = EconomyRun.Apply(t, p, new RunStats { Score = 100, Seed = 9 }, "2026-11-03");
            Assert.NotNull(g3.Chest);
            Assert.AreEqual(0, g3.ChestProgress);
            Assert.That(g3.Chest.Coins, Is.InRange(60, 210));
            Assert.AreEqual(p.coins - coins0, g1.Total + g2.Total + g3.Total);
            // Le deuxième coffre offre toujours une tête de la boutique.
            for (int i = 0; i < 3; i++) EconomyRun.Apply(t, p, new RunStats { Score = 100, Seed = (uint)(20 + i) }, "2026-11-03");
            Assert.AreEqual(3, p.chestsOpened);
        }

        [Test]
        public void ChestIsDeterministicForTheSameSeed()
        {
            var a = NewProfile(); var b = NewProfile();
            a.chestGames = b.chestGames = 2;
            var la = Chest.OnGameFinished(a, 1234); var lb = Chest.OnGameFinished(b, 1234);
            Assert.AreEqual(la.Coins, lb.Coins, "l'aperçu de fin de partie et l'enregistrement donnent le même coffre");
        }

        [Test]
        public void SpecialRunChestBoostCountsTwice()
        {
            var p = NewProfile();
            p.chestsOpened = 1;
            Assert.Null(Chest.OnGameFinished(p, 1, SpecialRun.ChestSteps(SpecialKind.ChestBoost)));
            Assert.AreEqual(2, p.chestGames);
        }

        [Test]
        public void RunGoalIsReachableAndRewarded()
        {
            var p = NewProfile();
            Assert.AreEqual(300, RunGoal.For(p));
            p.bestScore = 10000;
            Assert.AreEqual(6000, RunGoal.For(p));
            Assert.AreEqual(0, RunGoal.For(p) % 50);
            var t = new EconomyTuning();
            var hit = EconomyRun.Apply(t, p, new RunStats { Score = 6000, Goal = 6000 }, "2026-11-03");
            var miss = EconomyRun.Apply(t, p, new RunStats { Score = 5999, Goal = 6000 }, "2026-11-03");
            Assert.True(hit.GoalReached); Assert.AreEqual(RunGoal.Reward, hit.GoalCoins);
            Assert.False(miss.GoalReached); Assert.AreEqual(0, miss.GoalCoins);
        }

        [Test]
        public void SpecialRunsComeEveryFourGamesAfterTheFirstOnes()
        {
            var p = NewProfile();
            var kinds = new List<SpecialKind>();
            for (int n = 0; n < 24; n++) { p.gamesPlayed = n; kinds.Add(SpecialRun.Next(p)); }
            Assert.True(kinds.Take(4).All(k => k == SpecialKind.None), "pas de partie spéciale pendant la découverte");
            Assert.AreEqual(5, kinds.Count(k => k != SpecialKind.None));
            Assert.AreEqual(3, kinds.Where(k => k != SpecialKind.None).Distinct().Count());
            p.gamesPlayed = 6; Assert.AreEqual(1, SpecialRun.GamesUntil(p));
            p.gamesPlayed = 7; Assert.AreEqual(0, SpecialRun.GamesUntil(p));
            foreach (SpecialKind k in Enum.GetValues(typeof(SpecialKind)))
                if (k != SpecialKind.None) { Assert.True(Loc.HasKey("special_" + k)); Assert.True(Loc.HasKey("special_short_" + k)); }
        }

        [Test]
        public void DoubleCoinsSpecialDoublesRunCoinsOnly()
        {
            var t = new EconomyTuning();
            var a = NewProfile(); var b = NewProfile();
            var ga = EconomyRun.Apply(t, a, new RunStats { Score = 5000 }, "2026-11-03");
            var gb = EconomyRun.Apply(t, b, new RunStats { Score = 5000, Special = SpecialKind.DoubleCoins }, "2026-11-03");
            Assert.AreEqual(ga.Coins * 2, gb.Coins);
            var gx = EconomyRun.Apply(t, NewProfile(), new RunStats { Score = 5000, Special = SpecialKind.DoubleXp }, "2026-11-03");
            Assert.AreEqual((Levels.GameXp(t, 5000) + gx.MissionsDone.Count * t.missionXp) * 2, gx.Xp);
        }

        [Test]
        public void MissionsAreThreeDistinctAndStableForTheDay()
        {
            var p = NewProfile();
            Assert.True(Missions.Refresh(p, "2026-11-03"));
            Assert.AreEqual(3, p.missions.list.Count);
            Assert.AreEqual(3, p.missions.list.Select(m => m.kind).Distinct().Count());
            Assert.AreEqual(MissionKind.PlayGames, p.missions.list[0].kind, "un débutant a toujours une mission facile");
            var kinds = p.missions.list.Select(m => m.kind).ToList();
            Assert.False(Missions.Refresh(p, "2026-11-03"));
            p.missions.list.Clear();
            Missions.Refresh(p, "2026-11-03");
            CollectionAssert.AreEqual(kinds, p.missions.list.Select(m => m.kind).ToList(), "mêmes missions toute la journée");
            foreach (var m in p.missions.list) Assert.True(Loc.HasKey("m_" + m.kind));
        }

        [Test]
        public void MissionsProgressAndPayOnce()
        {
            var p = NewProfile();
            p.missions.day = "2026-11-03";
            p.missions.list = new List<Mission>
            {
                new Mission { kind = MissionKind.PlayGames, target = 2, reward = 50 },
                new Mission { kind = MissionKind.Merges, target = 30, reward = 60 },
                new Mission { kind = MissionKind.NoHeartLost, target = 500, reward = 80 },
            };
            var t = new EconomyTuning();
            var g1 = EconomyRun.Apply(t, p, new RunStats { Score = 400, Merges = 20 }, "2026-11-03");
            Assert.AreEqual(0, g1.MissionsDone.Count);
            var g2 = EconomyRun.Apply(t, p, new RunStats { Score = 600, Merges = 20 }, "2026-11-03");
            Assert.AreEqual(3, g2.MissionsDone.Count);
            Assert.True(g2.MissionsBonus);
            Assert.AreEqual(190 + t.missionsBonus, g2.MissionCoins + g2.BonusCoins);
            var g3 = EconomyRun.Apply(t, p, new RunStats { Score = 600, Merges = 20 }, "2026-11-03");
            Assert.AreEqual(0, g3.MissionsDone.Count);
            Assert.False(g3.MissionsBonus);
        }

        [Test]
        public void MissionRerollOncePerDay()
        {
            var p = NewProfile();
            Missions.Refresh(p, "2026-11-03");
            var before = p.missions.list[1].kind;
            Assert.True(Missions.Reroll(p, 1));
            Assert.AreNotEqual(before, p.missions.list[1].kind);
            Assert.AreEqual(3, p.missions.list.Select(m => m.kind).Distinct().Count());
            Assert.False(Missions.Reroll(p, 2));
        }

        [Test]
        public void DailyGiftGrowsAndResetsAfterAMissedDay()
        {
            var t = new EconomyTuning();
            var p = NewProfile();
            Assert.AreEqual(t.dailyGift[0], DailyGift.Claim(t, p, "2026-11-01").Coins);
            Assert.IsNull(DailyGift.Claim(t, p, "2026-11-01"), "une fois par jour");
            Assert.AreEqual(2, DailyGift.NextDay(p.gift, "2026-11-02", 7));
            Assert.AreEqual(GiftItem.Chest, DailyGift.Claim(t, p, "2026-11-02").Item);
            Assert.AreEqual(1, DailyGift.Claim(t, p, "2026-11-04").Day, "un jour manqué : retour au jour 1");
            for (int d = 5; d <= 10; d++) DailyGift.Claim(t, p, $"2026-11-{d:00}");
            Assert.AreEqual(7, p.gift.streak);
            Assert.AreEqual(1, DailyGift.NextDay(p.gift, "2026-11-11", 7), "après le jour 7, le cycle recommence");
        }

        [Test]
        public void DailyGiftGivesChestsATicketAndAPlank()
        {
            var t = new EconomyTuning();
            var p = NewProfile();
            var got = new List<GiftResult>();
            for (int d = 1; d <= 7; d++) got.Add(DailyGift.Claim(t, p, $"2026-11-{d:00}", 7));
            Assert.AreEqual(new[] { GiftItem.Coins, GiftItem.Chest, GiftItem.Coins, GiftItem.Ticket, GiftItem.Coins, GiftItem.Chest, GiftItem.Plank }, got.Select(g => g.Item).ToArray());
            Assert.NotNull(got[1].Chest);
            Assert.Greater(got[1].Chest.Coins, 0);
            Assert.AreEqual(1, p.surpriseTickets, "le jour 4 donne une ouverture gratuite de la machine");
            Assert.NotNull(got[6].Plank);
            Assert.AreEqual(1, got[6].Plank.Id, "la planche la moins chère qu'on n'a pas");
            Assert.Contains(1, p.ownedPlanks);
            // Toutes les planches déjà là : des pièces à la place.
            var rich = NewProfile();
            foreach (var s in PlankCatalog.All) if (!rich.ownedPlanks.Contains(s.Id)) rich.ownedPlanks.Add(s.Id);
            rich.gift.streak = 6; rich.gift.lastDay = "2026-11-06";
            var last = DailyGift.Claim(t, rich, "2026-11-07");
            Assert.AreEqual(GiftItem.Plank, last.Item);
            Assert.IsNull(last.Plank);
            Assert.AreEqual(t.dailyGift[6] + DailyGift.PlankFallbackCoins, last.Coins);
        }

        [Test]
        public void InterstitialRules()
        {
            var t = new AdTuning();
            var s = new AdState();
            var p = NewProfile();
            string day = "2026-11-03";
            double now = 1_000_000;
            p.gamesPlayed = t.firstInterstitialAfterGames - 1;
            for (int i = 0; i < 5; i++) AdPolicy.OnRunEnded(s);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now, day, 10), "pas avant que le joueur ait accroché");
            p.gamesPlayed = t.firstInterstitialAfterGames;
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now, day, 10));
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, true, 60, now, day, 10), "pas après un record");
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 10, now, day, 10), "pas après une partie éclair");
            AdPolicy.OnInterstitialShown(s, now, day);
            for (int i = 0; i < t.interstitialEveryGames; i++) AdPolicy.OnRunEnded(s);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 60, day, 10), "pas deux en 3 minutes");
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + t.minSecondsBetweenInterstitials + 1, day, 10));
            AdPolicy.OnRewardedWatched(s, now + 200, day);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 210, day, 10), "pas juste après une vidéo choisie");
            p.adsRemoved = true;
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 10_000, day, 10));
        }

        [Test]
        public void NoInterstitialDuringFirstFourGamesOfEachLaunch()
        {
            var t = new AdTuning();
            Assert.AreEqual(4, t.sessionGraceGames, "4 parties sans pub imposée à chaque lancement");
            Assert.AreEqual(4, t.interstitialEveryGames, "puis au plus une pub toutes les 4 parties");
            var s = new AdState();
            var p = NewProfile();
            p.gamesPlayed = 500; // joueur de longue date : le compteur gardé dans la sauvegarde est déjà plein
            for (int i = 0; i < 20; i++) AdPolicy.OnRunEnded(s);
            const string day = "2026-11-12";
            for (int game = 1; game < t.sessionGraceGames; game++)
                Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1_000_000, day, game), $"pas de pub après la partie {game} du lancement");
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1_000_000, day, t.sessionGraceGames));
        }

        [Test]
        public void InterstitialDailyCapResetsNextDay()
        {
            var t = new AdTuning { maxInterstitialsPerDay = 1, interstitialEveryGames = 1, minSecondsBetweenInterstitials = 0, firstInterstitialAfterGames = 0 };
            var s = new AdState();
            var p = NewProfile();
            AdPolicy.OnRunEnded(s);
            AdPolicy.OnInterstitialShown(s, 0, "2026-11-03");
            AdPolicy.OnRunEnded(s);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1000, "2026-11-03", 10));
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1000, "2026-11-04", 10));
        }

        [Test]
        public void ShopHeadsCostCoinsAndPremiumComesFromPurchases()
        {
            var p = NewProfile();
            var head = HeadCatalog.NextShopHead(p.unlockedHeads);
            Assert.NotNull(head);
            Assert.False(EconomyRun.BuyHead(p, head));
            p.coins = head.UnlockValue + 5;
            Assert.True(EconomyRun.BuyHead(p, head));
            Assert.AreEqual(5, p.coins);
            Assert.False(EconomyRun.BuyHead(p, head), "déjà achetée");

            Assert.True(Shop.Grant(p, Shop.Legends));
            Assert.AreEqual(5, HeadCatalog.All.Count(h => h.Product == Shop.Legends && p.unlockedHeads.Contains(h.Id)));
            Assert.False(Shop.Grant(p, Shop.Legends), "achat définitif donné une seule fois");
            Assert.True(Shop.Grant(p, Shop.CoinsSmall));
            Assert.True(Shop.Grant(p, Shop.CoinsSmall), "les pièces se rachètent");
            Assert.AreEqual(5 + 1200, p.coins);
            Assert.True(Shop.Grant(p, Shop.Starter));
            Assert.True(p.adsRemoved);
        }

        [Test]
        public void StarterOfferStartsOnceAndExpires()
        {
            var t = new AdTuning();
            var p = NewProfile();
            Assert.False(Offers.CheckStarter(t, p, 100));
            p.gamesPlayed = t.starterOfferAfterGames;
            Assert.True(Offers.CheckStarter(t, p, 100));
            Assert.False(Offers.CheckStarter(t, p, 200));
            Assert.True(Offers.StarterActive(p, 100 + 3600));
            Assert.False(Offers.StarterActive(p, 100 + t.starterOfferHours * 3600 + 1));
        }

        [Test]
        public void PurchasesNeverGiveGameplayAdvantage()
        {
            // Cahier des charges, section 9 : les achats sont cosmétiques ou de confort.
            foreach (var prod in Shop.Products)
            {
                Assert.True(Loc.HasKey(prod.TitleKey) && Loc.HasKey(prod.DescKey), prod.Id);
                if (prod.BadgeKey != null) Assert.True(Loc.HasKey(prod.BadgeKey));
            }
            var fields = typeof(ProductDef).GetFields().Select(f => f.Name).ToList();
            CollectionAssert.AreEquivalent(new[] { "Id", "Kind", "TitleKey", "DescKey", "DefaultPrice", "Coins", "RemovesAds", "HeadsProduct", "BadgeKey" }, fields,
                "un nouveau champ de produit doit rester cosmétique : vérifier puis mettre ce test à jour");
        }

        [Test]
        public void OldSavesKeepWorking()
        {
            var p = JsonSerializer.Deserialize<Profile>("{\"bestScore\":2544,\"unlockedHeads\":[0,1,2],\"ownedProducts\":[\"bascule.removeads\"]}", new JsonSerializerOptions { IncludeFields = true });
            p.Sanitize();
            Assert.AreEqual(1, p.level);
            Assert.NotNull(p.missions);
            Assert.NotNull(p.ads);
            Assert.True(p.adsRemoved);
            Assert.AreEqual(2544, p.bestScore);
        }

        [Test]
        public void EveryMissionKindHasText()
        {
            foreach (MissionKind k in Enum.GetValues(typeof(MissionKind))) Assert.True(Loc.HasKey("m_" + k), k.ToString());
            Assert.AreEqual("Joue 3 parties", Loc.MissionText(new Mission { kind = MissionKind.PlayGames, target = 3 }));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class SoundTests
    {
        [Test]
        public void MusicIsALongLoopNotAShortOne()
        {
            Assert.That(Music.LoopSeconds, Is.GreaterThan(60f), "la boucle doit durer plus d'une minute");
            var calm = Music.CalmLoop();
            var tense = Music.TenseLoop();
            Assert.AreEqual(calm.Length, tense.Length, "les deux couches doivent rester synchronisées");
            Assert.True(calm.All(v => v >= -1f && v <= 1f) && tense.All(v => v >= -1f && v <= 1f));
            // Aucune mesure muette.
            int bar = (int)(Music.BarSeconds * Music.SampleRate);
            for (int b = 0; b < Music.Bars; b++)
            {
                double e = 0;
                for (int i = b * bar; i < (b + 1) * bar; i++) e += calm[i] * calm[i];
                Assert.That(e / bar, Is.GreaterThan(1e-4), "mesure " + b);
            }
        }

        [Test]
        public void HeartbeatIsAudibleOnPhoneSpeakers()
        {
            // Les haut-parleurs de téléphone ne rendent presque rien sous 150 Hz : il faut de l'énergie au-dessus.
            var h = Music.Heartbeat();
            double hi = 0, total = 0, prev = 0;
            foreach (var v in h) { double d = v - prev; prev = v; hi += d * d; total += v * v; }
            // Rapport « énergie de la dérivée / énergie » : 0,0003 pour un pur 60 Hz (ancien battement), ~0,0026 pour 180 Hz.
            Assert.That(hi / total, Is.GreaterThan(0.001));
        }

        [Test]
        public void SoundsAreInTheMusicKey()
        {
            float[] cMajorPenta = { 0, 2, 4, 7, 9 };
            for (int d = -5; d < 20; d++)
            {
                double semis = 12 * Math.Log(Synth.Pentatonic(d) / 261.63, 2);
                double inOctave = ((Math.Round(semis) % 12) + 12) % 12;
                Assert.Contains((float)inOctave, cMajorPenta);
                Assert.That(Math.Abs(semis - Math.Round(semis)), Is.LessThan(0.01));
            }
        }

        [Test]
        public void NoMissionMentionsHearts()
        {
            for (uint seed = 0; seed < 200; seed++)
                Assert.False(Missions.Generate(seed, 30, 9000, 500).Any(m => m.kind == MissionKind.NoHeartLost));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class BackgroundDrawingTests
    {
        [Test]
        public void DrawingInParallelGivesTheSameImages()
        {
            // Les images sont dessinées en arrière-plan (ArtCache.PrepareInBackground) : le dessin ne doit dépendre d'aucun état partagé.
            var reference = WeightCatalog.Values.Select(v => WeightArtist.Draw(v, 64).Pixels).ToList();
            var faces = Enum.GetValues(typeof(Expression)).Cast<Expression>().Select(e => HeadArtist.Face(e, 64).Pixels).ToList();
            var parallel = new System.Collections.Concurrent.ConcurrentDictionary<int, Rgba[]>();
            var tasks = WeightCatalog.Values.Select((v, i) => System.Threading.Tasks.Task.Run(() => parallel[i] = WeightArtist.Draw(v, 64).Pixels))
                .Concat(Enum.GetValues(typeof(Expression)).Cast<Expression>().Select((e, i) => System.Threading.Tasks.Task.Run(() => parallel[100 + i] = HeadArtist.Face(e, 64).Pixels)))
                .ToArray();
            System.Threading.Tasks.Task.WaitAll(tasks);
            for (int i = 0; i < reference.Count; i++) Assert.True(reference[i].SequenceEqual(parallel[i]), "poids " + i);
            for (int i = 0; i < faces.Count; i++) Assert.True(faces[i].SequenceEqual(parallel[100 + i]), "visage " + i);
        }
    }
}


namespace Bascule.Core.Tests
{
    public class SocialTests
    {
        [TestCase("Basile_42")]
        [TestCase("Technique")]
        [TestCase("Habite_Ici")]
        [TestCase("Fichier")]
        [TestCase("Computer")]
        [TestCase("PDG_du_jeu")]
        [TestCase("Integre")]
        [TestCase("Essex")]
        [TestCase("Constance")]
        public void NormalPseudosAreAccepted(string pseudo) => Assert.NotNull(PseudoFilter.Clean(pseudo), pseudo);

        [TestCase("connard")]
        [TestCase("C0NNARD")]
        [TestCase("xXsalopeXx")]
        [TestCase("Pute")]
        [TestCase("ta_mere_la_pute")]
        [TestCase("SuperBite")]
        [TestCase("nique-ta-mere")]
        [TestCase("FDP")]
        [TestCase("hitler88")]
        [TestCase("pd")]
        [TestCase("Mmeerrddee")]
        public void RudePseudosAreRefused(string pseudo) => Assert.IsNull(PseudoFilter.Clean(pseudo), pseudo);

        [Test]
        public void PseudoLengthAndCharacters()
        {
            Assert.IsNull(PseudoFilter.Clean("ab"));
            Assert.AreEqual("Jean_Paul", PseudoFilter.Clean("  Jean Paul  "));
            Assert.AreEqual(PseudoFilter.MaxLength, PseudoFilter.Clean("Abcdefghijklmnopqrstuvwxyz").Length);
            Assert.AreEqual("Zoe", PseudoFilter.Clean("Zoé!!".Replace("é", "e")));
        }

        [Test]
        public void FriendCodesRoundTrip()
        {
            string id = "aB3dE6gH9jK2mN5pQ8sT1vW4yZ7c";
            string shown = FriendCode.Format(id);
            Assert.That(shown, Does.Contain("-"));
            Assert.AreEqual(id, FriendCode.Parse(shown));
            Assert.AreEqual(id, FriendCode.Parse("Ajoute-moi sur Bascule ! Mon code ami : " + shown));
            Assert.IsNull(FriendCode.Parse("bonjour"));
        }

        [Test]
        public void FriendsListRules()
        {
            var p = new Profile(); p.Sanitize();
            Assert.True(FriendCode.Add(p, "ami1ami1ami1ami1", "moi"));
            Assert.False(FriendCode.Add(p, "ami1ami1ami1ami1", "moi"), "déjà ajouté");
            Assert.False(FriendCode.Add(p, "moi", "moi"), "soi-même");
        }

        [Test]
        public void ShortFriendCodeIsStableReadableAndParsable()
        {
            string id = "aB3dE6gH9jK2mN5pQ8sT1vW4yZ7c";
            string code = FriendCode.Short(id);
            Assert.AreEqual(FriendCode.ShortLength, code.Length);
            Assert.AreEqual(code, FriendCode.Short(id), "toujours le même code pour un joueur");
            foreach (char c in code) Assert.That(FriendCode.ShortAlphabet.IndexOf(c), Is.GreaterThanOrEqualTo(0));
            Assert.That(FriendCode.ShortAlphabet, Does.Not.Contain("0").And.Not.Contain("O").And.Not.Contain("1").And.Not.Contain("I").And.Not.Contain("L"));
            string shown = FriendCode.FormatShort(code);
            Assert.AreEqual(7, shown.Length);
            Assert.AreEqual(code, FriendCode.ParseShort(shown));
            Assert.AreEqual(code, FriendCode.ParseShort(shown.ToLowerInvariant().Replace("-", " ")));
            Assert.AreEqual(code, FriendCode.ParseShort("Ajoute-moi sur Bascule ! Mon code ami : " + shown));
            Assert.IsNull(FriendCode.ParseShort("bonjour"));
            Assert.IsNull(FriendCode.ParseShort("ABC-10O"), "caractères ambigus refusés");
            // Les codes se répartissent bien (peu de collisions sur des identifiants proches).
            var seen = new HashSet<string>();
            for (int i = 0; i < 5000; i++) seen.Add(FriendCode.Short("player" + i));
            Assert.That(seen.Count, Is.GreaterThan(4990));
        }

        [Test]
        public void SuggestedShopHeadRotatesAmongAffordableOnes()
        {
            var none = new List<int>();
            var affordable = HeadCatalog.AffordableShopHeads(none, 100000);
            Assert.That(affordable.Count, Is.GreaterThan(1));
            var seen = new HashSet<int>();
            for (int g = 0; g < affordable.Count; g++) seen.Add(HeadCatalog.SuggestedShopHead(none, 100000, g).Id);
            Assert.AreEqual(affordable.Count, seen.Count, "chaque tête payable finit par être proposée");
            // Rien de payable : la moins chère.
            Assert.AreEqual(HeadCatalog.NextShopHead(none).Id, HeadCatalog.SuggestedShopHead(none, 0, 5).Id);
        }

        [Test]
        public void BlockingHidesPlayerAndRemovesFriend()
        {
            var p = new Profile(); p.Sanitize();
            FriendCode.Add(p, "ami1ami1ami1ami1", "moi");
            Assert.True(Moderation.Block(p, "ami1ami1ami1ami1", "moi"));
            Assert.True(Moderation.IsBlocked(p, "ami1ami1ami1ami1"));
            Assert.False(p.friends.Contains("ami1ami1ami1ami1"), "un joueur bloqué n'est plus un ami");
            Assert.False(Moderation.Block(p, "ami1ami1ami1ami1", "moi"), "déjà bloqué");
            Assert.False(Moderation.Block(p, "moi", "moi"), "soi-même");
            Assert.False(Moderation.IsBlocked(p, null));
            Moderation.UnblockAll(p);
            Assert.False(Moderation.IsBlocked(p, "ami1ami1ami1ami1"));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class ArtPackTests
    {
        static string PackDir()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
            return Path.Combine(dir.FullName, "Assets/Bascule/Resources/ArtPack");
        }

        [Test]
        public void EveryImageOfTheGameIsDrawnInAdvance()
        {
            // Sinon le jeu la dessine au lancement (lent, surtout sur le web) : relancer Tools/ArtPack.
            var manifest = new HashSet<string>(File.ReadAllLines(Path.Combine(PackDir(), "manifest.txt")));
            foreach (var j in ArtCatalog.All())
            {
                Assert.True(manifest.Contains(j.Key), "absent du manifeste : " + j.Key);
                Assert.True(File.Exists(Path.Combine(PackDir(), j.Key + ".png")), "image manquante : " + j.Key);
            }
            foreach (var skin in ArtCatalog.Skins())
                Assert.True(File.Exists(Path.Combine(PackDir(), "mascot_" + skin.TrimStart('#') + "_pivots.txt")));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class BragTests
    {
        static BragInfo Run(int score = 2000) => new BragInfo
        {
            Mode = GameMode.Infinite, Seed = 12345, Score = score, Seconds = 90,
            Tension = new List<float> { 0.1f, 0.6f, 0.8f, 0.95f, 0.3f },
        };

        [Test]
        public void TensionStripTellsTheStoryWithoutSpoilers()
        {
            Assert.AreEqual("🟩🟨🟧🟥🟩💥", Brag.Tension(Run().Tension));
            Assert.AreEqual("🟩", Brag.Tension(new List<float> { 0f }, false));
        }

        [Test]
        public void LongRunsAreCompressedKeepingThePeaks()
        {
            var peaks = Enumerable.Repeat(0.1f, 40).ToList();
            peaks[17] = 0.99f;
            var c = Brag.Compress(peaks);
            Assert.AreEqual(Brag.MaxSquares, c.Count);
            Assert.AreEqual(1, c.Count(x => x > 0.9f), "le sauvetage de justesse ne doit pas disparaître");
        }

        [Test]
        public void TitlePicksTheMostRemarkableThing()
        {
            var b = Run();
            Assert.AreEqual("title_balancer", Brag.TitleKey(b));
            b.BestSave = 0.98f;
            Assert.AreEqual("title_tightrope", Brag.TitleKey(b));
            var fast = Run(120); fast.Seconds = 12;
            Assert.AreEqual("title_express", Brag.TitleKey(fast));
            var butter = Run(); butter.LostWeights = 6;
            Assert.AreEqual("title_butter", Brag.TitleKey(butter));
            Assert.AreEqual("title_legend", Brag.TitleKey(Run(9000)));
        }

        [Test]
        public void EveryTitleIsTranslated()
        {
            foreach (var k in new[] { "title_express", "title_tightrope", "title_combo", "title_balloon", "title_butter", "title_survivor",
                         "title_strong", "title_apprentice", "title_balancer", "title_master", "title_legend" })
                Assert.AreNotEqual(k, Loc.Get(k));
        }

        [Test]
        public void SummaryCarriesScoreCodeAndLink()
        {
            var b = Run(); b.BestChain = 3;
            string s = Brag.Summary(b);
            StringAssert.Contains(Loc.FormatScore(2000), s);
            StringAssert.Contains(ChallengeCode.Encode(12345), s);
            StringAssert.Contains(Brag.Site, s);
            StringAssert.DoesNotContain("🟩", s); // la frise est sur l'image, pas dans le message
            Assert.True(s.Length < 400, "un résumé doit tenir dans une légende");
        }

        [Test]
        public void DailySummaryHasTheDayNumberAndNoCode()
        {
            var b = Run(); b.Mode = GameMode.Daily; b.DayKey = "2026-11-12";
            string s = Brag.Summary(b);
            StringAssert.Contains("12", s.Split('\n')[0]);
            StringAssert.DoesNotContain(ChallengeCode.Encode(12345), s);
            Assert.AreEqual(1, Brag.DayNumber("n'importe quoi"));
        }

        [Test]
        public void CloseToBestOnlyWhenItHurts()
        {
            var b = Run(1900); b.PreviousBest = 2000;
            Assert.AreEqual(100, Brag.PointsFromBest(b));
            b.Score = 500;
            Assert.IsNull(Brag.PointsFromBest(b));
            b.Score = 2100; b.NewBest = true;
            Assert.IsNull(Brag.PointsFromBest(b));
        }
    }
}

namespace Bascule.Core.Tests
{
    public class CosmeticsTests
    {
        static Profile Rich(int coins = 100000)
        {
            var p = new Profile { coins = coins, playerId = "test" };
            p.Sanitize();
            return p;
        }

        [Test]
        public void SurpriseNeverGivesADuplicateAndEmptiesThePool()
        {
            var p = Rich(10_000_000);
            var won = new HashSet<string>();
            int opened = 0;
            while (SurpriseBox.Available(p) && opened < 2000)
            {
                var prize = SurpriseBox.Open(p, 42);
                Assert.NotNull(prize);
                opened++;
                if (prize.Kind == PrizeKind.Head) Assert.True(won.Add("h" + prize.Head.Id), "doublon de tête : " + prize.Head.Id);
                if (prize.Kind == PrizeKind.Plank) Assert.True(won.Add("p" + prize.Plank.Id), "doublon de planche : " + prize.Plank.Id);
            }
            Assert.False(SurpriseBox.Available(p), "toutes les têtes et planches finissent par sortir");
            Assert.IsNull(SurpriseBox.Open(p, 42));
            Assert.AreEqual(HeadCatalog.All.Count(h => h.Unlock == UnlockKind.Shop) + PlankCatalog.All.Count(s => s.Price > 0), won.Count);
        }

        [Test]
        public void SurpriseOddsAreShownAndFair()
        {
            var odds = SurpriseBox.Odds(Rich());
            Assert.AreEqual(1f, odds.Sum(o => o.Chance), 1e-4f);
            var heads = odds.Where(o => o.Kind == PrizeKind.Head).OrderBy(o => o.Head.UnlockValue).ToList();
            Assert.Greater(heads.First().Chance, heads.Last().Chance, "les têtes chères sont plus rares");
            Assert.AreEqual(0.55f, heads.Sum(o => o.Chance), 1e-3f);
            Assert.AreEqual(0.15f, odds.Where(o => o.Kind == PrizeKind.Plank).Sum(o => o.Chance), 1e-3f);
            var jackpot = odds.Single(o => o.Jackpot);
            Assert.That(jackpot.Chance, Is.LessThan(0.03f), "le jackpot est rare");
            // Valeur moyenne en pièces bien en dessous du prix : la machine n'est pas une façon de gagner des pièces.
            Assert.Less(odds.Where(o => o.Kind == PrizeKind.Coins).Sum(o => o.Chance * o.Coins), SurpriseBox.Price * 0.5f);
        }

        [Test]
        public void SurpriseTicketOpensForFree()
        {
            var p = Rich(0);
            p.surpriseTickets = 1;
            Assert.IsNull(SurpriseBox.Open(p, 1), "sans ticket demandé, il faut des pièces");
            var prize = SurpriseBox.Open(p, 1, useTicket: true);
            Assert.NotNull(prize);
            Assert.AreEqual(0, p.surpriseTickets);
            Assert.AreEqual(prize.Kind == PrizeKind.Coins ? prize.Coins : 0, p.coins, "aucune pièce dépensée");
        }

        [Test]
        public void SurpriseNeedsEnoughCoins()
        {
            var p = Rich(SurpriseBox.Price - 1);
            Assert.IsNull(SurpriseBox.Open(p, 1));
            Assert.AreEqual(SurpriseBox.Price - 1, p.coins);
        }

        [Test]
        public void PlanksAreBoughtOnceAndChangeNothingElse()
        {
            var p = Rich(1000);
            var bamboo = PlankCatalog.Get(1);
            Assert.True(Cosmetics.BuyPlank(p, bamboo));
            Assert.AreEqual(1000 - bamboo.Price, p.coins);
            Assert.AreEqual(1, p.selectedPlank);
            Assert.False(Cosmetics.BuyPlank(p, bamboo), "déjà achetée");
            Assert.True(Cosmetics.SelectPlank(p, 0));
            Assert.False(Cosmetics.SelectPlank(p, 6), "pas achetée");
            Assert.AreEqual(PlankCatalog.All.Count, PlankCatalog.All.Select(s => s.Id).Distinct().Count());
        }

        [Test]
        public void SanitizeRepairsCosmetics()
        {
            var p = new Profile { selectedPlank = 5, ownedPlanks = null, streakShields = 9 };
            p.Sanitize();
            Assert.Contains(0, p.ownedPlanks);
            Assert.AreEqual(0, p.selectedPlank);
            Assert.AreEqual(StreakShield.Max, p.streakShields);
        }

        [Test]
        public void ShieldKeepsTheGiftStreakWhenADayIsMissed()
        {
            var t = new EconomyTuning();
            var p = Rich(1000);
            DailyGift.Claim(t, p, "2026-11-01");
            DailyGift.Claim(t, p, "2026-11-02");
            Assert.True(StreakShield.Buy(p));
            DailyGift.Claim(t, p, "2026-11-04"); // le 3 est manqué
            Assert.AreEqual(3, p.gift.streak);
            Assert.AreEqual(0, p.streakShields);
            DailyGift.Claim(t, p, "2026-11-06"); // plus de protège-série : la série repart
            Assert.AreEqual(1, p.gift.streak);
        }

        [Test]
        public void ShieldStockIsCapped()
        {
            var p = Rich(5000);
            for (int i = 0; i < StreakShield.Max; i++) Assert.True(StreakShield.Buy(p));
            Assert.False(StreakShield.Buy(p));
        }

        [Test]
        public void RandomHeadNeverRepeatsAndStaysInCollection()
        {
            var owned = new List<int> { 0, 3, 7 };
            var rng = new System.Random(4);
            var seen = new HashSet<int>();
            int current = 0;
            for (int i = 0; i < 200; i++)
            {
                int next = HeadCatalog.PickRandom(owned, current, rng.NextDouble);
                Assert.That(owned, Does.Contain(next));
                Assert.AreNotEqual(current, next);
                seen.Add(next);
                current = next;
            }
            Assert.AreEqual(3, seen.Count);
            Assert.AreEqual(0, HeadCatalog.PickRandom(new List<int> { 0 }, 0, rng.NextDouble)); // une seule tête : on la garde
        }

        [Test]
        public void RarityFollowsDifficulty()
        {
            var counts = HeadCatalog.All.GroupBy(HeadCatalog.RarityOf).ToDictionary(g => g.Key, g => g.Count());
            foreach (Rarity r in Enum.GetValues(typeof(Rarity))) Assert.True(counts.ContainsKey(r), "aucune tête " + r);
            Assert.AreEqual(Rarity.Common, HeadCatalog.RarityOf(HeadCatalog.Get(0)));
            Assert.True(HeadCatalog.All.Where(h => h.Unlock == UnlockKind.Premium).All(h => HeadCatalog.RarityOf(h) == Rarity.Legendary));
            Assert.Less(counts[Rarity.Legendary], counts[Rarity.Common] + counts[Rarity.Rare]);
        }

        [Test]
        public void EveryLipSitsFullyOnThePlank()
        {
            // Chaque colonne de chaque rebord doit reposer sur le dessus plat de la planche : rien dans le vide,
            // rien sur l'arrondi du bout. Vérifié à plusieurs résolutions, à l'épaisseur du jeu.
            var t = new PlankTuning();
            foreach (var s in PlankCatalog.All)
                foreach (float ppu in new[] { 160f, 400f })
                    Assert.AreEqual(0, SceneArtist.LipUnsupportedColumns(s, t.startLength, t.thickness, ppu, t.endLipWidth, t.endLipHeight),
                        $"rebord « {s.Fr} » pas entièrement sur la planche ({ppu} px/unité)");
        }

        [Test]
        public void EveryPlankDraws()
        {
            foreach (var s in PlankCatalog.All)
            {
                var r = SceneArtist.Plank(6.6f, 0.32f, 40f, out int border, s);
                Assert.Greater(r.Width, 0);
                Assert.Greater(border, 0);
                Assert.AreNotEqual(s.Name("fr"), "");
                // Rebords assortis : une image non vide, et l'aperçu de boutique avec les deux rebords.
                var lip = SceneArtist.PlankLip(s, 0.12f, 0.22f, 200f);
                Assert.True(lip.Pixels.Any(px => px.a > 0), "rebord vide : " + s.Fr);
                var preview = SceneArtist.PlankPreview(s, 4.4f, 0.62f, 48f, 0.2f, 0.36f);
                Assert.Greater(preview.Height, r.Height / 40f * 48f);
            }
        }
    }
}
