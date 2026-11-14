using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Core;

namespace UzbCarParking.Avtodrom
{
    [Serializable]
    public class PenaltyRule
    {
        public PenaltyType type;
        public string label;
        public int points;
        [Tooltip("Qo‘pol xato: failOnCriticalPenalty yoqilgan bo‘lsa imtihon yiqiladi.")]
        public bool critical;

        public PenaltyRule(PenaltyType type, string label, int points, bool critical = false)
        {
            this.type = type;
            this.label = label;
            this.points = points;
            this.critical = critical;
        }
    }

    /// <summary>
    /// Avtodrom mashqlari va baholash sozlamalari.
    ///
    /// MUHIM: bu qiymatlar O‘YIN ICHKI dizayn bo‘lib, O‘zbekistondagi rasmiy amaliy imtihon
    /// qoidalari bilan TASDIQLANMAGAN. Rasmiy me'yorlar aniqlangach, alohida ExamConfig asset
    /// yaratib (masalan EC_Official) shu yerdagi qiymatlarni almashtirish kifoya — kod o‘zgarmaydi.
    /// </summary>
    [CreateAssetMenu(menuName = "UZB Car Parking/Exam Config", fileName = "EC_NewExamConfig")]
    public class ExamConfig : ScriptableObject
    {
        [TextArea(2, 4)]
        public string disclaimer =
            "O‘yin ichki baholash tizimi. Rasmiy imtihon qoidalari bilan tasdiqlanmagan.";

        [Header("Umumiy baholash")]
        public int startingScore = 100;
        public int passScore = 80;
        public bool failOnCriticalPenalty = true;
        public bool failExamOnExerciseFail = true;

        [Header("Imtihon ketma-ketligi (mashq identifikatorlari)")]
        public List<string> examOrder = new List<string>
        {
            GameConstants.ExerciseEstakada,
            GameConstants.ExerciseParallel,
            GameConstants.ExerciseBox
        };

        [Header("Harakatni boshlash")]
        [Tooltip("Shu masofaga (m) siljigach 'harakat boshlandi' deb hisoblanadi va tayyorgarlik tekshiriladi.")]
        public float moveOffDistance = 0.5f;
        public bool requireSeatbelt = true;
        public bool requireLeftSignalOnMoveOff = true;

        [Header("Umumiy qoidalar")]
        public float speedLimitKmh = 20f;
        public float speedViolationGraceSeconds = 1f;
        public float speedViolationCooldown = 5f;
        public float handbrakeDrivingSpeedKmh = 3f;
        public float handbrakeDrivingGraceSeconds = 2f;
        [Tooltip("Shu tezlikdan (m/s) kuchli zarba to‘siqqa urilish deb jarimalanadi.")]
        public float collisionMinImpact = 1f;
        public float collisionCooldown = 2f;
        public bool requireHandbrakeToFinishParking = true;

        [Header("Estakada")]
        public float estakadaTimeLimit = 120f;
        public float estakadaStopZoneLength = 1.2f;
        public float estakadaMinStopSeconds = 1f;
        public float estakadaMaxRollback = 0.3f;
        public float estakadaCriticalRollback = 1f;

        [Header("Parallel parkovka")]
        public float parallelTimeLimit = 180f;
        public float parallelBayLength = 6.5f;
        public float parallelBayWidth = 2.5f;
        public float parallelMaxHeadingError = 10f;

        [Header("Boksga orqa bilan kirish")]
        public float boxTimeLimit = 180f;
        public float boxWidth = 3f;
        public float boxDepth = 6f;
        public float boxMaxHeadingError = 12f;

        [Header("Jarima ballari")]
        public List<PenaltyRule> penalties = DefaultPenalties();

        public static List<PenaltyRule> DefaultPenalties()
        {
            return new List<PenaltyRule>
            {
                new PenaltyRule(PenaltyType.ConeHit, "Konusga tegildi", 5),
                new PenaltyRule(PenaltyType.LineCrossed, "Chiziq bosildi", 5),
                new PenaltyRule(PenaltyType.SpeedLimit, "Tezlik chegarasi oshirildi", 5),
                new PenaltyRule(PenaltyType.NoSeatbelt, "Xavfsizlik kamari taqilmagan", 10),
                new PenaltyRule(PenaltyType.NoTurnSignal, "Burilish signali yoqilmagan", 5),
                new PenaltyRule(PenaltyType.HandbrakeWhileDriving, "Qo‘l tormozi bilan harakatlanish", 3),
                new PenaltyRule(PenaltyType.Rollback, "Estakadada orqaga ketish", 10),
                new PenaltyRule(PenaltyType.CriticalRollback, "Estakadada ko‘p orqaga ketish", 25, true),
                new PenaltyRule(PenaltyType.NoStopAtLine, "Belgilangan joyda to‘xtalmadi", 10),
                new PenaltyRule(PenaltyType.Collision, "To‘siqqa urilish", 10),
                new PenaltyRule(PenaltyType.TimeLimitExceeded, "Mashq vaqti tugadi", 25, true)
            };
        }

        public PenaltyRule GetRule(PenaltyType type)
        {
            if (penalties != null)
            {
                for (int i = 0; i < penalties.Count; i++)
                {
                    if (penalties[i] != null && penalties[i].type == type)
                    {
                        return penalties[i];
                    }
                }
            }

            List<PenaltyRule> defaults = DefaultPenalties();
            for (int i = 0; i < defaults.Count; i++)
            {
                if (defaults[i].type == type)
                {
                    return defaults[i];
                }
            }

            return new PenaltyRule(type, type.ToString(), 5);
        }

        private void OnValidate()
        {
            startingScore = Mathf.Max(1, startingScore);
            passScore = Mathf.Clamp(passScore, 0, startingScore);
            speedLimitKmh = Mathf.Max(1f, speedLimitKmh);
            parallelBayLength = Mathf.Max(3f, parallelBayLength);
            parallelBayWidth = Mathf.Max(1.5f, parallelBayWidth);
            boxWidth = Mathf.Max(1.5f, boxWidth);
            boxDepth = Mathf.Max(3f, boxDepth);
            estakadaStopZoneLength = Mathf.Max(0.5f, estakadaStopZoneLength);
        }
    }
}
