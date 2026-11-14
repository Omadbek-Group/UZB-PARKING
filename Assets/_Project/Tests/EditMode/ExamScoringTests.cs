using NUnit.Framework;
using UzbCarParking.Avtodrom;

namespace UzbCarParking.Tests
{
    public class ExamScoringTests
    {
        [Test]
        public void StartsWithFullScoreAndPasses()
        {
            ExamScoring scoring = new ExamScoring(100, 80);

            Assert.AreEqual(100, scoring.Score);
            Assert.AreEqual(0, scoring.Penalties.Count);
            Assert.IsTrue(scoring.IsPassed(true));
        }

        [Test]
        public void PenaltyReducesScoreAndIsRecorded()
        {
            ExamScoring scoring = new ExamScoring(100, 80);

            PenaltyRecord record = scoring.Apply(PenaltyType.ConeHit, 5, "Konus", "parallel", 12.5f, false);

            Assert.AreEqual(95, scoring.Score);
            Assert.AreEqual(5, scoring.TotalPenaltyPoints);
            Assert.AreEqual(1, scoring.Penalties.Count);
            Assert.AreEqual(PenaltyType.ConeHit, record.type);
            Assert.AreEqual("parallel", record.exerciseId);
        }

        [Test]
        public void ScoreNeverDropsBelowZero()
        {
            ExamScoring scoring = new ExamScoring(100, 80);

            for (int i = 0; i < 30; i++)
            {
                scoring.Apply(PenaltyType.LineCrossed, 5, "Chiziq", string.Empty, i, false);
            }

            Assert.AreEqual(0, scoring.Score);
        }

        [Test]
        public void NegativePointsAreIgnored()
        {
            ExamScoring scoring = new ExamScoring(100, 80);

            scoring.Apply(PenaltyType.SpeedLimit, -20, "Xato qiymat", string.Empty, 0f, false);

            Assert.AreEqual(100, scoring.Score);
        }

        [Test]
        public void FailsBelowPassScore()
        {
            ExamScoring scoring = new ExamScoring(100, 80);

            scoring.Apply(PenaltyType.Rollback, 10, "Orqaga ketish", "estakada", 0f, false);
            scoring.Apply(PenaltyType.Collision, 10, "Urilish", "estakada", 0f, false);
            Assert.IsTrue(scoring.IsPassed(true), "80 ball — o‘tish chegarasi, o‘tishi kerak");

            scoring.Apply(PenaltyType.ConeHit, 5, "Konus", "box", 0f, false);
            Assert.IsFalse(scoring.IsPassed(true));
        }

        [Test]
        public void CriticalPenaltyFailsOnlyWhenConfigured()
        {
            ExamScoring scoring = new ExamScoring(100, 50);

            scoring.Apply(PenaltyType.CriticalRollback, 25, "Qo‘pol xato", "estakada", 0f, true);

            Assert.IsTrue(scoring.CriticalFailure);
            Assert.IsFalse(scoring.IsPassed(true));
            Assert.IsTrue(scoring.IsPassed(false));
        }

        [Test]
        public void ResetRestoresInitialState()
        {
            ExamScoring scoring = new ExamScoring(100, 80);
            scoring.Apply(PenaltyType.TimeLimitExceeded, 25, "Vaqt", "box", 0f, true);

            scoring.Reset();

            Assert.AreEqual(100, scoring.Score);
            Assert.IsFalse(scoring.CriticalFailure);
            Assert.AreEqual(0, scoring.Penalties.Count);
        }

        [Test]
        public void PassScoreIsClampedToStartingScore()
        {
            ExamScoring scoring = new ExamScoring(100, 250);

            Assert.AreEqual(100, scoring.PassScore);
            Assert.IsTrue(scoring.IsPassed(true));
        }
    }
}
