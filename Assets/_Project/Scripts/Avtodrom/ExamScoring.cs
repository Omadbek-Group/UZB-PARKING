using System;
using System.Collections.Generic;

namespace UzbCarParking.Avtodrom
{
    public enum PenaltyType
    {
        ConeHit = 0,
        LineCrossed = 1,
        SpeedLimit = 2,
        NoSeatbelt = 3,
        NoTurnSignal = 4,
        HandbrakeWhileDriving = 5,
        Rollback = 6,
        CriticalRollback = 7,
        NoStopAtLine = 8,
        Collision = 9,
        TimeLimitExceeded = 10
    }

    [Serializable]
    public struct PenaltyRecord
    {
        public PenaltyType type;
        public int points;
        public string label;
        public string exerciseId;
        public float time;
        public bool critical;
    }

    /// <summary>
    /// 100 ballik O‘YIN ICHKI baholash (sof mantiq, Unity'ga bog‘liq emas — unit test qilinadi).
    /// Bu rasmiy imtihon baholash tizimi emas; qiymatlar ExamConfig orqali o‘zgartiriladi.
    /// </summary>
    public class ExamScoring
    {
        private readonly List<PenaltyRecord> penalties = new List<PenaltyRecord>();

        public ExamScoring(int startingScore, int passScore)
        {
            StartingScore = Math.Max(0, startingScore);
            PassScore = Math.Max(0, Math.Min(passScore, StartingScore));
            Score = StartingScore;
        }

        public int StartingScore { get; private set; }
        public int PassScore { get; private set; }
        public int Score { get; private set; }
        public bool CriticalFailure { get; private set; }

        public IReadOnlyList<PenaltyRecord> Penalties
        {
            get { return penalties; }
        }

        public int TotalPenaltyPoints
        {
            get { return StartingScore - Score; }
        }

        public PenaltyRecord Apply(PenaltyType type, int points, string label, string exerciseId, float time, bool critical)
        {
            int safePoints = Math.Max(0, points);
            Score = Math.Max(0, Score - safePoints);
            if (critical)
            {
                CriticalFailure = true;
            }

            PenaltyRecord record = new PenaltyRecord
            {
                type = type,
                points = safePoints,
                label = label ?? string.Empty,
                exerciseId = exerciseId ?? string.Empty,
                time = time,
                critical = critical
            };
            penalties.Add(record);
            return record;
        }

        /// <param name="failOnCritical">true bo‘lsa, bitta qo‘pol xato ham imtihonni yiqitadi.</param>
        public bool IsPassed(bool failOnCritical)
        {
            if (failOnCritical && CriticalFailure)
            {
                return false;
            }

            return Score >= PassScore;
        }

        public void Reset()
        {
            Score = StartingScore;
            CriticalFailure = false;
            penalties.Clear();
        }
    }
}
