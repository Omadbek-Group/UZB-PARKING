using System;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    public enum ExerciseState
    {
        Inactive = 0,
        Waiting = 1,
        Running = 2,
        Completed = 3,
        Failed = 4
    }

    /// <summary>
    /// Avtodrom mashqining asosi. Yangi mashq (zmeyka, 90° burilish va h.k.) qo‘shish uchun
    /// shu klassdan meros oling va OnBegin/OnTick metodlarini yozing — ExamManager o‘zgarmaydi.
    /// </summary>
    public abstract class ExerciseBase : MonoBehaviour
    {
        public string exerciseId = string.Empty;
        public string displayName = string.Empty;

        [Tooltip("Mashina markazi shu hududga kirganda mashq boshlanadi.")]
        public ParkingZone startZone;
        [Tooltip("Mashq davomida yo‘l ko‘rsatkich (mayoq) turadigan nuqta.")]
        public Transform targetPoint;

        public event Action<ExerciseBase> Started;
        public event Action<ExerciseBase> Completed;
        public event Action<ExerciseBase, string> Failed;

        protected ExamManager exam;
        protected VehicleController vehicle;
        protected ExamConfig config;

        public ExerciseState State { get; private set; }
        public float ElapsedTime { get; private set; }
        public float TimeLimit { get; private set; }
        public string StatusText { get; protected set; }

        /// <summary>Mayoq hozir turishi kerak bo‘lgan joy.</summary>
        public Vector3 GuidePosition
        {
            get
            {
                if (State == ExerciseState.Running && targetPoint != null)
                {
                    return CurrentTargetPosition();
                }

                return startZone != null ? startZone.transform.position : transform.position;
            }
        }

        public void Setup(ExamManager examManager, VehicleController vehicleController, ExamConfig examConfig)
        {
            exam = examManager;
            vehicle = vehicleController;
            config = examConfig;
            TimeLimit = GetTimeLimit(examConfig);
            State = ExerciseState.Inactive;
            StatusText = string.Empty;
        }

        /// <summary>Mashqni navbatga qo‘yadi: mashina boshlanish hududiga kirishi kutiladi.</summary>
        public void Arm()
        {
            State = ExerciseState.Waiting;
            ElapsedTime = 0f;
            StatusText = "Mashq boshlanish joyiga boring";
        }

        public void Tick(float deltaTime)
        {
            if (vehicle == null)
            {
                return;
            }

            if (State == ExerciseState.Waiting)
            {
                if (startZone == null || startZone.ContainsPoint(vehicle.transform.position))
                {
                    State = ExerciseState.Running;
                    ElapsedTime = 0f;
                    OnBegin();
                    if (Started != null)
                    {
                        Started.Invoke(this);
                    }
                }

                return;
            }

            if (State != ExerciseState.Running)
            {
                return;
            }

            ElapsedTime += deltaTime;
            if (TimeLimit > 0f && ElapsedTime > TimeLimit)
            {
                exam.ApplyPenalty(PenaltyType.TimeLimitExceeded, exerciseId);
                Fail("Mashq uchun ajratilgan vaqt tugadi");
                return;
            }

            OnTick(deltaTime);
        }

        protected void Complete()
        {
            if (State != ExerciseState.Running)
            {
                return;
            }

            State = ExerciseState.Completed;
            StatusText = "Mashq bajarildi";
            if (Completed != null)
            {
                Completed.Invoke(this);
            }
        }

        protected void Fail(string reason)
        {
            if (State != ExerciseState.Running)
            {
                return;
            }

            State = ExerciseState.Failed;
            StatusText = reason;
            if (Failed != null)
            {
                Failed.Invoke(this, reason);
            }
        }

        protected bool VehicleStopped
        {
            get { return vehicle.SpeedKmh < 0.5f; }
        }

        /// <summary>Mashina joyida mahkamlangan: qo‘l tormozi tortilgan yoki avtomat quti P holatida.</summary>
        protected bool VehicleSecured
        {
            get
            {
                return !config.requireHandbrakeToFinishParking || vehicle.HandbrakeEngaged || vehicle.IsInPark;
            }
        }

        protected virtual Vector3 CurrentTargetPosition()
        {
            return targetPoint.position;
        }

        protected abstract float GetTimeLimit(ExamConfig examConfig);
        protected abstract void OnBegin();
        protected abstract void OnTick(float deltaTime);
    }
}
