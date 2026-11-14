using System;

namespace UzbCarParking.Vehicle
{
    public enum TransmissionType
    {
        Automatic = 0,
        Manual = 1
    }

    /// <summary>Avtomat quti selektori. Tartib muhim: ShiftUp/ShiftDown shu ketma-ketlikda yuradi.</summary>
    public enum AutoGearSelector
    {
        Park = 0,
        Reverse = 1,
        Neutral = 2,
        Drive = 3
    }

    [Serializable]
    public struct VehicleInputState
    {
        /// <summary>0..1</summary>
        public float throttle;

        /// <summary>0..1</summary>
        public float brake;

        /// <summary>-1 (chap) .. 1 (o‘ng)</summary>
        public float steer;
    }

    /// <summary>Uzatmalar qutisining sof (Unity'ga bog‘liq bo‘lmagan) mantiqi — unit test qilinadi.</summary>
    public static class TransmissionLogic
    {
        /// <summary>
        /// Avtomat quti D rejimida keyingi uzatmani tanlaydi (0 dan boshlanadigan indeks).
        /// </summary>
        /// <param name="wheelDrivenRpm">G‘ildirak tezligidan hisoblangan dvigatel aylanishi (joriy uzatmada).</param>
        public static int AutoSelectGear(int currentGear, float wheelDrivenRpm, float shiftUpRpm, float shiftDownRpm,
            int gearCount, float throttle)
        {
            if (gearCount <= 0)
            {
                return 0;
            }

            if (currentGear < 0) currentGear = 0;
            if (currentGear > gearCount - 1) currentGear = gearCount - 1;
            if (throttle < 0f) throttle = 0f;
            if (throttle > 1f) throttle = 1f;

            if (wheelDrivenRpm > shiftUpRpm && currentGear < gearCount - 1)
            {
                return currentGear + 1;
            }

            // Gaz qattiq bosilganda pastki uzatmaga ertaroq o‘tadi (kick-down'ga o‘xshash).
            float downRpm = shiftDownRpm + throttle * (shiftUpRpm - shiftDownRpm) * 0.25f;
            if (wheelDrivenRpm < downRpm && currentGear > 0)
            {
                return currentGear - 1;
            }

            return currentGear;
        }

        public static float WheelRpmToEngineRpm(float wheelRpm, float gearRatio, float finalDriveRatio)
        {
            return Math.Abs(wheelRpm * gearRatio * finalDriveRatio);
        }
    }
}
