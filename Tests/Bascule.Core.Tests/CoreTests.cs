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
        public void SceneryChangesEarlyThenCycles()
        {
            Assert.AreEqual(0, Difficulty.BackgroundIndex(0, 5));
            Assert.AreEqual(0, Difficulty.BackgroundIndex(299, 5));
            Assert.AreEqual(1, Difficulty.BackgroundIndex(300, 5), "premier changement dès 300");
            Assert.AreEqual(2, Difficulty.BackgroundIndex(800, 5));
            Assert.AreEqual(3, Difficulty.BackgroundIndex(2000, 5));
            Assert.AreEqual(4, Difficulty.BackgroundIndex(4000, 5));
            Assert.AreEqual(4, Difficulty.BackgroundIndex(6999, 5));
            Assert.AreEqual(0, Difficulty.BackgroundIndex(7000, 5), "une nouvelle journée après la nuit");
            Assert.AreEqual(1, Difficulty.BackgroundIndex(10000, 5));
            for (int sc = 0; sc < 100000; sc += 250) Assert.That(Difficulty.BackgroundIndex(sc, 5), Is.InRange(0, 4));
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
            var t = new PlankTuning();
            Assert.AreEqual(t.startLength, Difficulty.PlankLength(0, t));
            Assert.That(Difficulty.PlankLength(4000, t), Is.LessThan(t.startLength));
            Assert.AreEqual(t.minLength, Difficulty.PlankLength(1_000_000, t));
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

    public class HeadTests
    {
        [Test]
        public void HeadIdsAreUniqueAndStable()
        {
            Assert.AreEqual(62, HeadCatalog.All.Count);
            Assert.AreEqual(62, HeadCatalog.All.Select(h => h.Id).Distinct().Count());
            for (int i = 0; i < HeadCatalog.All.Count; i++) Assert.AreEqual(i, HeadCatalog.All[i].Id, "les identifiants sont sauvegardés : ne jamais les renuméroter");
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
            var r = Progress.ApplyRun(p, GameMode.Infinite, new RunAchievements { Score = 1600 }, "2026-11-02");
            Assert.AreEqual(4, r.NewHeads.Count);
            Assert.True(r.NewBest);
            Assert.AreEqual(2000, r.NextHead.UnlockValue);
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
            Assert.AreEqual(2, p.chestsOpened);
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
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now, day), "pas avant que le joueur ait accroché");
            p.gamesPlayed = t.firstInterstitialAfterGames;
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now, day));
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, true, 60, now, day), "pas après un record");
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 10, now, day), "pas après une partie éclair");
            AdPolicy.OnInterstitialShown(s, now, day);
            for (int i = 0; i < t.interstitialEveryGames; i++) AdPolicy.OnRunEnded(s);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 60, day), "pas deux en 3 minutes");
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + t.minSecondsBetweenInterstitials + 1, day));
            AdPolicy.OnRewardedWatched(s, now + 200, day);
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 210, day), "pas juste après une vidéo choisie");
            p.adsRemoved = true;
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, now + 10_000, day));
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
            Assert.False(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1000, "2026-11-03"));
            Assert.True(AdPolicy.ShouldShowInterstitial(t, s, p, false, 60, 1000, "2026-11-04"));
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
