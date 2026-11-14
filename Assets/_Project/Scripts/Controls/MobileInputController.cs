using System;
using UnityEngine;
using UzbCarParking.CameraSystem;
using UzbCarParking.Persistence;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Controls
{
    public enum SteeringMode
    {
        Wheel = 0,
        Buttons = 1
    }

    /// <summary>
    /// Sensorli tugmalar va klaviaturadan kelgan boshqaruvni birlashtirib, VehicleController'ga uzatadi.
    /// UI tugmalari (HudUI) shu klassning ommaviy metodlarini chaqiradi.
    /// </summary>
    public class MobileInputController : MonoBehaviour
    {
        [Header("Sensorli elementlar (HudUI biriktiradi)")]
        public TouchButton gasPedal;
        public TouchButton brakePedal;
        public TouchButton steerLeftButton;
        public TouchButton steerRightButton;
        public TouchButton hornButton;
        public TouchSteeringWheel steeringWheel;

        [Header("Sozlamalar")]
        public SteeringMode steeringMode = SteeringMode.Wheel;
        [Range(0.5f, 2f)] public float steeringSensitivity = 1f;
        [Tooltip("Tugma/klaviatura bilan rul to‘liq burilish tezligi (1/soniya).")]
        public float buttonSteerSpeed = 2.2f;
        public float steerReturnSpeed = 4f;
        public float throttleRampSpeed = 4f;
        public float brakeRampSpeed = 6f;

        public event Action PauseRequested;

        private VehicleController vehicle;
        private VehicleLights lights;
        private CameraController cameraController;
        private float throttle;
        private float brake;
        private float steer;

        /// <summary>false bo‘lsa boshqaruv bloklanadi (pauza, natija oynasi).</summary>
        public bool InputEnabled { get; set; }

        public VehicleController Vehicle
        {
            get { return vehicle; }
        }

        private void Awake()
        {
            InputEnabled = true;
        }

        public void Bind(VehicleController vehicleController, VehicleLights vehicleLights, CameraController cameraRig)
        {
            vehicle = vehicleController;
            lights = vehicleLights;
            cameraController = cameraRig;
        }

        public void ApplySettings(GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            steeringMode = (SteeringMode)Mathf.Clamp(settings.steeringMode, 0, 1);
            steeringSensitivity = Mathf.Clamp(settings.steeringSensitivity, 0.5f, 2f);
            if (steeringWheel != null)
            {
                steeringWheel.sensitivity = steeringSensitivity;
            }
        }

        private void Update()
        {
            if (vehicle == null)
            {
                return;
            }

            if (KeyboardInput.Pressed(KeyAction.Pause) && PauseRequested != null)
            {
                PauseRequested.Invoke();
            }

            if (!InputEnabled)
            {
                // Boshqaruv bloklanganda (natija oynasi) mashina tormozlanadi.
                throttle = 0f;
                steer = 0f;
                brake = 1f;
                vehicle.HornActive = false;
                vehicle.SetInput(new VehicleInputState { throttle = 0f, brake = 1f, steer = 0f });
                return;
            }

            float dt = Time.deltaTime;

            float targetThrottle = Mathf.Max(IsPressed(gasPedal) ? 1f : 0f, KeyboardInput.Throttle());
            float targetBrake = Mathf.Max(IsPressed(brakePedal) ? 1f : 0f, KeyboardInput.Brake());
            throttle = Mathf.MoveTowards(throttle, targetThrottle, throttleRampSpeed * dt);
            brake = Mathf.MoveTowards(brake, targetBrake, brakeRampSpeed * dt);

            UpdateSteering(dt);

            vehicle.SetInput(new VehicleInputState { throttle = throttle, brake = brake, steer = steer });
            vehicle.HornActive = IsPressed(hornButton) || KeyboardInput.Held(KeyAction.Horn);

            HandleKeyboardActions();
        }

        private void UpdateSteering(float dt)
        {
            float keyboardSteer = KeyboardInput.Steer();
            bool wheelActive = steeringMode == SteeringMode.Wheel && steeringWheel != null &&
                               steeringWheel.isActiveAndEnabled && Mathf.Abs(keyboardSteer) < 0.01f;

            if (wheelActive)
            {
                // Chambarak analog qiymat beradi — qo‘shimcha silliqlash kerak emas.
                steer = steeringWheel.Value;
                return;
            }

            float target = keyboardSteer;
            if (Mathf.Abs(target) < 0.01f)
            {
                if (IsPressed(steerLeftButton)) target -= 1f;
                if (IsPressed(steerRightButton)) target += 1f;
            }

            float speed = Mathf.Abs(target) < 0.01f ? steerReturnSpeed : buttonSteerSpeed * steeringSensitivity;
            steer = Mathf.MoveTowards(steer, target, speed * dt);
        }

        private void HandleKeyboardActions()
        {
            if (KeyboardInput.Pressed(KeyAction.Engine)) ToggleEngine();
            if (KeyboardInput.Pressed(KeyAction.Handbrake)) ToggleHandbrake();
            if (KeyboardInput.Pressed(KeyAction.Seatbelt)) ToggleSeatbelt();
            if (KeyboardInput.Pressed(KeyAction.Lights)) CycleHeadlights();
            if (KeyboardInput.Pressed(KeyAction.LeftSignal)) ToggleLeftSignal();
            if (KeyboardInput.Pressed(KeyAction.RightSignal)) ToggleRightSignal();
            if (KeyboardInput.Pressed(KeyAction.Hazard)) ToggleHazard();
            if (KeyboardInput.Pressed(KeyAction.Camera)) NextCamera();
            if (KeyboardInput.Pressed(KeyAction.ShiftUp)) ShiftUp();
            if (KeyboardInput.Pressed(KeyAction.ShiftDown)) ShiftDown();
        }

        private static bool IsPressed(TouchButton button)
        {
            return button != null && button.IsPressed;
        }

        // ------------------------------------------------------------------ UI tugmalari uchun amallar

        public void ToggleEngine()
        {
            if (CanAct()) vehicle.ToggleEngine();
        }

        public void ToggleHandbrake()
        {
            if (CanAct()) vehicle.ToggleHandbrake();
        }

        public void ToggleSeatbelt()
        {
            if (CanAct()) vehicle.ToggleSeatbelt();
        }

        public void CycleHeadlights()
        {
            if (CanAct() && lights != null) lights.CycleHeadlights();
        }

        public void ToggleLeftSignal()
        {
            if (CanAct() && lights != null) lights.ToggleLeft();
        }

        public void ToggleRightSignal()
        {
            if (CanAct() && lights != null) lights.ToggleRight();
        }

        public void ToggleHazard()
        {
            if (CanAct() && lights != null) lights.ToggleHazard();
        }

        public void NextCamera()
        {
            if (cameraController != null) cameraController.NextMode();
        }

        public void ShiftUp()
        {
            if (CanAct()) vehicle.ShiftUp();
        }

        public void ShiftDown()
        {
            if (CanAct()) vehicle.ShiftDown();
        }

        public void SelectAutoGear(AutoGearSelector selector)
        {
            if (CanAct()) vehicle.TrySelectAutoGear(selector);
        }

        private bool CanAct()
        {
            return vehicle != null && InputEnabled;
        }
    }
}
