using UnityEngine;
using UzbCarParking.Core;

namespace UzbCarParking.Avtodrom
{
    /// <summary>Parallel parkovka: yo‘l chetidagi katakka orqaga yurib, yo‘lga parallel holda turish.</summary>
    public class ParallelParkingExercise : ParkingExercise
    {
        public void Configure(ParkingZone start, ParkingZone bayZone, Vector3 laneDirection)
        {
            ConfigureParking(GameConstants.ExerciseParallel, "Parallel parkovka", start, bayZone, laneDirection);
        }

        protected override string BeginText
        {
            get { return "Katak yonidan o‘tib, orqaga yurib parallel parkovka qiling"; }
        }

        protected override string WrongDirectionText
        {
            get { return "Mashina teskari qarab qolgan — harakat yo‘nalishi bo‘yicha turing"; }
        }

        protected override float GetTimeLimit(ExamConfig examConfig)
        {
            return examConfig.parallelTimeLimit;
        }

        protected override float GetMaxHeadingError(ExamConfig examConfig)
        {
            return examConfig.parallelMaxHeadingError;
        }
    }
}
