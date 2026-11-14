using UnityEngine;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Parkovka mashqlarining umumiy mantiqi: mashina gabariti katakka to‘liq sig‘ishi,
    /// kerakli yo‘nalishda turishi, to‘xtashi va mahkamlanishi (qo‘l tormozi yoki P) kerak.
    /// </summary>
    public abstract class ParkingExercise : ExerciseBase
    {
        [Tooltip("Mashina to‘liq sig‘ishi kerak bo‘lgan katak.")]
        public ParkingZone bay;
        [Tooltip("Mashina oxirida qaysi tomonga qarab turishi kerak (gorizontal).")]
        public Vector3 desiredForward = Vector3.forward;

        private readonly Vector3[] corners = new Vector3[4];

        protected abstract string BeginText { get; }
        protected abstract string WrongDirectionText { get; }
        protected abstract float GetMaxHeadingError(ExamConfig examConfig);

        protected void ConfigureParking(string id, string title, ParkingZone start, ParkingZone bayZone, Vector3 forward)
        {
            exerciseId = id;
            displayName = title;
            startZone = start;
            bay = bayZone;
            targetPoint = bayZone != null ? bayZone.transform : null;
            forward.y = 0f;
            desiredForward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        protected override void OnBegin()
        {
            StatusText = BeginText;
        }

        protected override void OnTick(float deltaTime)
        {
            vehicle.GetFootprintCorners(corners);
            if (!bay.ContainsAll(corners))
            {
                StatusText = BeginText;
                return;
            }

            float headingError = ZoneGeometry.HeadingErrorDegrees(vehicle.transform.forward, desiredForward);
            if (headingError > GetMaxHeadingError(config))
            {
                StatusText = headingError > 150f ? WrongDirectionText : "Mashinani tekislang";
                return;
            }

            if (!VehicleStopped)
            {
                StatusText = "Joyida. To‘liq to‘xtang";
                return;
            }

            if (!VehicleSecured)
            {
                StatusText = "Qo‘l tormozini torting (yoki P ga qo‘ying)";
                return;
            }

            Complete();
        }
    }
}
