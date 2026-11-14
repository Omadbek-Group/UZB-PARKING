using UnityEngine;
using UzbCarParking.Core;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Estakada: ko‘tarilishda stop-chiziq oldida to‘xtash va orqaga ketmasdan joyidan qo‘zg‘alish.
    /// Bosqichlar: yaqinlashish → to‘xtab turish → qo‘zg‘alish → tushish.
    /// </summary>
    public class EstakadaExercise : ExerciseBase
    {
        private enum Phase
        {
            Approach,
            Holding,
            Descending
        }

        [Tooltip("Mashinaning old bamperi shu hududda to‘xtashi kerak.")]
        public ParkingZone stopZone;
        [Tooltip("Mashina markazi shu hududga yetganda mashq tugaydi.")]
        public ParkingZone exitZone;
        [Tooltip("Ko‘tarilish yo‘nalishi (gorizontal).")]
        public Vector3 uphillDirection = Vector3.forward;
        [Tooltip("To‘xtagandan keyin shu masofaga (m) oldinga yurilsa 'qo‘zg‘aldi' deb hisoblanadi.")]
        public float moveOffDistance = 1.2f;

        private Phase phase;
        private float stopTimer;
        private Vector3 holdPosition;
        private float maxRollback;
        private bool rollbackPenalized;

        public void Configure(ParkingZone start, ParkingZone stop, ParkingZone exit, Vector3 uphill)
        {
            exerciseId = GameConstants.ExerciseEstakada;
            displayName = "Estakada";
            startZone = start;
            stopZone = stop;
            exitZone = exit;
            targetPoint = stop != null ? stop.transform : null;
            uphill.y = 0f;
            uphillDirection = uphill.sqrMagnitude > 0.0001f ? uphill.normalized : Vector3.forward;
        }

        protected override float GetTimeLimit(ExamConfig examConfig)
        {
            return examConfig.estakadaTimeLimit;
        }

        protected override Vector3 CurrentTargetPosition()
        {
            if (phase == Phase.Descending && exitZone != null)
            {
                return exitZone.transform.position;
            }

            return base.CurrentTargetPosition();
        }

        protected override void OnBegin()
        {
            phase = Phase.Approach;
            stopTimer = 0f;
            maxRollback = 0f;
            rollbackPenalized = false;
            StatusText = "Estakadaga chiqing va stop-chiziq oldida to‘xtang";
        }

        protected override void OnTick(float deltaTime)
        {
            switch (phase)
            {
                case Phase.Approach:
                    TickApproach(deltaTime);
                    break;
                case Phase.Holding:
                    TickHolding();
                    break;
                default:
                    TickDescending();
                    break;
            }
        }

        private void TickApproach(float deltaTime)
        {
            Vector3 front = vehicle.FrontPoint;

            if (stopZone.ContainsPoint(front) && VehicleStopped)
            {
                stopTimer += deltaTime;
                StatusText = "To‘xtab turing...";
                if (stopTimer >= config.estakadaMinStopSeconds)
                {
                    phase = Phase.Holding;
                    holdPosition = vehicle.transform.position;
                    maxRollback = 0f;
                    StatusText = "Endi orqaga ketmasdan joyidan qo‘zg‘aling";
                }

                return;
            }

            stopTimer = 0f;
            StatusText = "Estakadaga chiqing va stop-chiziq oldida to‘xtang";

            // Stop-chiziqdan to‘xtamasdan o‘tib ketdi.
            float along = Vector3.Dot(front - stopZone.transform.position, uphillDirection);
            if (along > stopZone.size.z * 0.5f + 0.5f)
            {
                exam.ApplyPenalty(PenaltyType.NoStopAtLine, exerciseId);
                phase = Phase.Descending;
                StatusText = "Estakadadan ehtiyotkorlik bilan tushing";
            }
        }

        private void TickHolding()
        {
            float rollback = Vector3.Dot(holdPosition - vehicle.transform.position, uphillDirection);
            if (rollback > maxRollback)
            {
                maxRollback = rollback;
            }

            if (maxRollback > config.estakadaCriticalRollback)
            {
                exam.ApplyPenalty(PenaltyType.CriticalRollback, exerciseId);
                Fail("Mashina orqaga ketib qoldi");
                return;
            }

            if (!rollbackPenalized && maxRollback > config.estakadaMaxRollback)
            {
                rollbackPenalized = true;
                exam.ApplyPenalty(PenaltyType.Rollback, exerciseId);
            }

            if (-rollback > moveOffDistance)
            {
                phase = Phase.Descending;
                StatusText = "Estakadadan ehtiyotkorlik bilan tushing";
            }
        }

        private void TickDescending()
        {
            if (exitZone == null || exitZone.ContainsPoint(vehicle.transform.position))
            {
                Complete();
            }
        }
    }
}
