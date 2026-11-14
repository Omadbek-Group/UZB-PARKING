using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.Persistence;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    public enum ExamState
    {
        NotStarted = 0,
        PreCheck = 1,
        Active = 2,
        Finished = 3
    }

    /// <summary>
    /// Imtihon jarayonini boshqaradi: tayyorgarlik tekshiruvi → mashqlar ketma-ketligi → natija.
    /// Jarimalarni hisoblaydi (ExamScoring), natijani saqlaydi va virtual guvohnoma beradi.
    /// Virtual guvohnoma faqat o‘yin ichida amal qiladi — haqiqiy hujjat emas.
    /// </summary>
    public class ExamManager : MonoBehaviour
    {
        public event Action<PenaltyRecord> PenaltyApplied;
        public event Action<ExerciseBase> ExerciseChanged;

        /// <summary>Natija va "virtual guvohnoma shu safar berildimi" belgisi.</summary>
        public event Action<ExamResultData, bool> ExamFinished;

        private readonly List<ExerciseBase> exercises = new List<ExerciseBase>();
        private ExamConfig config;
        private VehicleController vehicle;
        private VehicleLights lights;
        private LineCrossDetector lineDetector;
        private ExerciseBeacon beacon;

        private int currentIndex = -1;
        private int completedCount;
        private bool anyExerciseFailed;
        private string failReason = string.Empty;
        private Vector3 startPosition;
        private float speedOverTimer;
        private float speedCooldown;
        private float handbrakeTimer;
        private bool handbrakePenalized;
        private bool seatbeltPenalized;
        private float collisionCooldown;

        public ExamState State { get; private set; }
        public ExamScoring Scoring { get; private set; }
        public ExerciseBase CurrentExercise { get; private set; }
        public float ExamTime { get; private set; }
        public bool IsFullExam { get; private set; }
        public string ModeId { get; private set; }
        public ExamConfig Config { get { return config; } }
        public int ExerciseCount { get { return exercises.Count; } }
        public int CurrentExerciseNumber { get { return Mathf.Clamp(currentIndex + 1, 0, exercises.Count); } }
        public string FailReason { get { return failReason; } }

        // Tayyorgarlik ro‘yxati (HUD ko‘rsatadi)
        public bool SeatbeltOk { get { return vehicle != null && vehicle.SeatbeltFastened; } }
        public bool EngineOk { get { return vehicle != null && vehicle.EngineRunning; } }
        public bool HandbrakeReleasedOk { get { return vehicle != null && !vehicle.HandbrakeEngaged; } }
        public bool LeftSignalOk { get { return lights != null && lights.Indicator == IndicatorMode.Left; } }
        public bool GearOk { get { return vehicle != null && vehicle.IsForwardGearSelected; } }

        public void Initialize(ExamConfig examConfig, VehicleController vehicleController, VehicleLights vehicleLights,
            LineCrossDetector detector, VehicleDamage damage, ExerciseBeacon exerciseBeacon,
            List<ExerciseBase> orderedExercises, bool fullExam, string modeId)
        {
            config = examConfig;
            vehicle = vehicleController;
            lights = vehicleLights;
            lineDetector = detector;
            beacon = exerciseBeacon;
            IsFullExam = fullExam;
            ModeId = modeId;

            exercises.Clear();
            if (orderedExercises != null)
            {
                for (int i = 0; i < orderedExercises.Count; i++)
                {
                    ExerciseBase exercise = orderedExercises[i];
                    if (exercise == null)
                    {
                        continue;
                    }

                    exercise.Setup(this, vehicle, config);
                    exercise.Completed += OnExerciseCompleted;
                    exercise.Failed += OnExerciseFailed;
                    exercises.Add(exercise);
                }
            }

            Scoring = new ExamScoring(config.startingScore, config.passScore);

            if (lineDetector != null) lineDetector.LineTouched += OnLineTouched;
            if (vehicle != null) vehicle.Collided += OnVehicleCollided;
            TrafficCone.ConeHit += OnConeHit;

            startPosition = vehicle.transform.position;
            State = ExamState.PreCheck;
            UpdateBeacon();
        }

        private void OnDestroy()
        {
            TrafficCone.ConeHit -= OnConeHit;
            if (lineDetector != null) lineDetector.LineTouched -= OnLineTouched;
            if (vehicle != null) vehicle.Collided -= OnVehicleCollided;

            for (int i = 0; i < exercises.Count; i++)
            {
                if (exercises[i] == null)
                {
                    continue;
                }

                exercises[i].Completed -= OnExerciseCompleted;
                exercises[i].Failed -= OnExerciseFailed;
            }
        }

        private void Update()
        {
            if (State != ExamState.PreCheck && State != ExamState.Active)
            {
                return;
            }

            float dt = Time.deltaTime;
            ExamTime += dt;
            if (collisionCooldown > 0f) collisionCooldown -= dt;

            if (State == ExamState.PreCheck)
            {
                Vector3 offset = vehicle.transform.position - startPosition;
                offset.y = 0f;
                if (offset.magnitude > config.moveOffDistance)
                {
                    EvaluateMoveOff();
                    State = ExamState.Active;
                    ActivateExercise(0);
                }

                return;
            }

            MonitorGeneralRules(dt);
            if (State == ExamState.Active && CurrentExercise != null)
            {
                CurrentExercise.Tick(dt);
            }

            UpdateBeacon();
        }

        // ------------------------------------------------------------------ Qoidalar

        private void EvaluateMoveOff()
        {
            if (config.requireSeatbelt && !vehicle.SeatbeltFastened)
            {
                seatbeltPenalized = true;
                ApplyPenalty(PenaltyType.NoSeatbelt, string.Empty);
            }

            if (config.requireLeftSignalOnMoveOff && !LeftSignalOk)
            {
                ApplyPenalty(PenaltyType.NoTurnSignal, string.Empty);
            }
        }

        private void MonitorGeneralRules(float dt)
        {
            float speed = vehicle.SpeedKmh;

            // Tezlik chegarasi
            if (speedCooldown > 0f) speedCooldown -= dt;
            if (speed > config.speedLimitKmh)
            {
                speedOverTimer += dt;
                if (speedOverTimer > config.speedViolationGraceSeconds && speedCooldown <= 0f)
                {
                    speedCooldown = config.speedViolationCooldown;
                    ApplyPenalty(PenaltyType.SpeedLimit, CurrentExerciseId());
                }
            }
            else
            {
                speedOverTimer = 0f;
            }

            // Qo‘l tormozi bilan harakatlanish
            if (vehicle.HandbrakeEngaged && speed > config.handbrakeDrivingSpeedKmh)
            {
                handbrakeTimer += dt;
                if (!handbrakePenalized && handbrakeTimer > config.handbrakeDrivingGraceSeconds)
                {
                    handbrakePenalized = true;
                    ApplyPenalty(PenaltyType.HandbrakeWhileDriving, CurrentExerciseId());
                }
            }
            else
            {
                handbrakeTimer = 0f;
                if (speed < 0.5f)
                {
                    handbrakePenalized = false;
                }
            }

            // Kamar harakat davomida yechilgan bo‘lsa
            if (config.requireSeatbelt && !seatbeltPenalized && !vehicle.SeatbeltFastened && speed > 3f)
            {
                seatbeltPenalized = true;
                ApplyPenalty(PenaltyType.NoSeatbelt, CurrentExerciseId());
            }
        }

        private void OnLineTouched(BoundaryLine line)
        {
            if (State != ExamState.Active || line == null)
            {
                return;
            }

            // Mashqqa tegishli chiziq faqat shu mashq bajarilayotganda jarimalanadi.
            if (!string.IsNullOrEmpty(line.exerciseId))
            {
                if (CurrentExercise == null || CurrentExercise.State != ExerciseState.Running ||
                    CurrentExercise.exerciseId != line.exerciseId)
                {
                    return;
                }
            }

            ApplyPenalty(PenaltyType.LineCrossed, CurrentExerciseId());
        }

        private void OnConeHit(TrafficCone cone)
        {
            if (State != ExamState.Active && State != ExamState.PreCheck)
            {
                return;
            }

            ApplyPenalty(PenaltyType.ConeHit, CurrentExerciseId());
        }

        private void OnVehicleCollided(Collision collision)
        {
            if (State != ExamState.Active || collisionCooldown > 0f || collision.collider == null)
            {
                return;
            }

            // Konuslar alohida hisoblanadi.
            if (collision.collider.GetComponentInParent<TrafficCone>() != null)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude < config.collisionMinImpact)
            {
                return;
            }

            collisionCooldown = config.collisionCooldown;
            ApplyPenalty(PenaltyType.Collision, CurrentExerciseId());
        }

        private string CurrentExerciseId()
        {
            return CurrentExercise != null ? CurrentExercise.exerciseId : string.Empty;
        }

        /// <summary>Jarima qo‘llaydi. Mashqlar ham shu metodni chaqiradi.</summary>
        public void ApplyPenalty(PenaltyType type, string exerciseId)
        {
            if (State == ExamState.Finished || Scoring == null)
            {
                return;
            }

            PenaltyRule rule = config.GetRule(type);
            PenaltyRecord record = Scoring.Apply(type, rule.points, rule.label, exerciseId, ExamTime, rule.critical);
            GameEvents.Toast("-" + record.points + " ball: " + record.label);
            if (PenaltyApplied != null)
            {
                PenaltyApplied.Invoke(record);
            }
        }

        // ------------------------------------------------------------------ Mashqlar ketma-ketligi

        private void ActivateExercise(int index)
        {
            currentIndex = index;
            if (index >= exercises.Count)
            {
                CurrentExercise = null;
                Finish();
                return;
            }

            CurrentExercise = exercises[index];
            CurrentExercise.Arm();
            UpdateBeacon();
            if (ExerciseChanged != null)
            {
                ExerciseChanged.Invoke(CurrentExercise);
            }
        }

        private void OnExerciseCompleted(ExerciseBase exercise)
        {
            if (State != ExamState.Active || exercise != CurrentExercise)
            {
                return;
            }

            completedCount++;
            GameEvents.Toast(exercise.displayName + ": bajarildi");
            ActivateExercise(currentIndex + 1);
        }

        private void OnExerciseFailed(ExerciseBase exercise, string reason)
        {
            if (State != ExamState.Active || exercise != CurrentExercise)
            {
                return;
            }

            anyExerciseFailed = true;
            failReason = exercise.displayName + ": " + reason;
            GameEvents.Toast(failReason);

            if (config.failExamOnExerciseFail)
            {
                Finish();
            }
            else
            {
                ActivateExercise(currentIndex + 1);
            }
        }

        private void UpdateBeacon()
        {
            if (beacon == null)
            {
                return;
            }

            bool show = State != ExamState.Finished;
            if (beacon.gameObject.activeSelf != show)
            {
                beacon.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            ExerciseBase guide = CurrentExercise;
            if (guide == null && exercises.Count > 0 && State == ExamState.PreCheck)
            {
                guide = exercises[0];
            }

            if (guide != null)
            {
                beacon.MoveTo(guide.GuidePosition);
            }
        }

        /// <summary>Keyingi mo‘ljalgacha masofa (m). Mo‘ljal bo‘lmasa -1.</summary>
        public float DistanceToGuide()
        {
            ExerciseBase guide = CurrentExercise;
            if (guide == null && exercises.Count > 0 && State == ExamState.PreCheck)
            {
                guide = exercises[0];
            }

            if (guide == null || vehicle == null)
            {
                return -1f;
            }

            Vector3 offset = guide.GuidePosition - vehicle.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        // ------------------------------------------------------------------ Yakun

        private void Finish()
        {
            if (State == ExamState.Finished)
            {
                return;
            }

            State = ExamState.Finished;
            UpdateBeacon();

            bool allCompleted = exercises.Count > 0 && completedCount >= exercises.Count;
            bool passed = allCompleted && !anyExerciseFailed && Scoring.IsPassed(config.failOnCriticalPenalty);

            ExamResultData result = new ExamResultData
            {
                dateUtc = DateTime.UtcNow.ToString("o"),
                mode = ModeId,
                score = Scoring.Score,
                passed = passed,
                durationSeconds = ExamTime
            };

            IReadOnlyList<PenaltyRecord> records = Scoring.Penalties;
            for (int i = 0; i < records.Count; i++)
            {
                result.penalties.Add(records[i].label + " (-" + records[i].points + ")");
            }

            bool licenseIssuedNow = false;
            SaveData save = SaveSystem.Data;
            SaveSystem.AddExamResult(result);

            if (IsFullExam)
            {
                save.profile.examsTaken++;
                if (passed)
                {
                    save.profile.examsPassed++;
                }

                if (result.score > save.bestExamScore)
                {
                    save.bestExamScore = result.score;
                }

                if (passed && !save.hasVirtualLicense)
                {
                    save.hasVirtualLicense = true;
                    save.virtualLicenseNumber = "UZB-VIRT-" + UnityEngine.Random.Range(100000, 1000000);
                    save.virtualLicenseIssuedUtc = result.dateUtc;
                    licenseIssuedNow = true;
                }
            }

            SaveSystem.Save();

            if (ExamFinished != null)
            {
                ExamFinished.Invoke(result, licenseIssuedNow);
            }
        }
    }
}
