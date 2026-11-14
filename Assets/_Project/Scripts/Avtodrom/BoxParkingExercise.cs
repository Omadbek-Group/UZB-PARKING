using UnityEngine;
using UzbCarParking.Core;

namespace UzbCarParking.Avtodrom
{
    /// <summary>Boks: yo‘lga perpendikulyar katakka orqa bilan kirish (old tomoni chiqish tomonga qaraydi).</summary>
    public class BoxParkingExercise : ParkingExercise
    {
        public void Configure(ParkingZone start, ParkingZone boxZone, Vector3 exitDirection)
        {
            ConfigureParking(GameConstants.ExerciseBox, "Boksga orqa bilan kirish", start, boxZone, exitDirection);
        }

        protected override string BeginText
        {
            get { return "Boks yonidan o‘tib, orqaga yurib boksga kiring"; }
        }

        protected override string WrongDirectionText
        {
            get { return "Boksga old bilan emas, orqa bilan kirish kerak"; }
        }

        protected override float GetTimeLimit(ExamConfig examConfig)
        {
            return examConfig.boxTimeLimit;
        }

        protected override float GetMaxHeadingError(ExamConfig examConfig)
        {
            return examConfig.boxMaxHeadingError;
        }
    }
}
