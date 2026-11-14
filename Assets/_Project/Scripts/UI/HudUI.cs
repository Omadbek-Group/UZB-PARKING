using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UzbCarParking.Avtodrom;
using UzbCarParking.CameraSystem;
using UzbCarParking.Controls;
using UzbCarParking.Core;
using UzbCarParking.Persistence;
using UzbCarParking.Vehicle;

namespace UzbCarParking.UserInterface
{
    /// <summary>
    /// O‘yin ichidagi interfeys: asboblar paneli (spidometr, taxometr, uzatma, yoqilg‘i), imtihon holati,
    /// sensorli boshqaruv (rul, pedallar, uzatmalar, chiroqlar), pauza va natija oynalari.
    /// </summary>
    public class HudUI : MonoBehaviour
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private MobileInputController input;
        private VehicleController vehicle;
        private VehicleLights lights;
        private ExamManager exam;
        private CameraController cameraRig;

        private Text speedLabel;
        private Text gearLabel;
        private Text rpmLabel;
        private Text leftArrow;
        private Text rightArrow;
        private Text iconLights;
        private Text iconHandbrake;
        private Text iconBelt;
        private Text iconEngine;
        private RectTransform rpmFill;
        private Image rpmFillImage;
        private RectTransform fuelFill;

        private Text examTitle;
        private Text examScore;
        private Text examStatus;
        private Text examTimer;
        private Text checklistLabel;

        private GameObject toastRoot;
        private Text toastLabel;
        private float toastTimer;

        private GameObject autoGearRoot;
        private GameObject manualGearRoot;
        private Text manualGearLabel;
        private readonly Image[] autoGearImages = new Image[4];
        private Image handbrakeImage;
        private Image engineImage;
        private Image beltImage;
        private Image lightsImage;
        private Image leftSignalImage;
        private Image rightSignalImage;
        private Image hazardImage;

        private GameObject pausePanel;
        private GameObject resultPanel;
        private Text resultTitle;
        private Text resultBody;

        private readonly StringBuilder builder = new StringBuilder(512);
        private float refreshTimer;
        private int lastSpeed = -1;
        private string lastGear = string.Empty;
        private bool paused;

        public static HudUI Create(MobileInputController inputController, VehicleController vehicleController,
            VehicleLights vehicleLights, ExamManager examManager, CameraController cameraController,
            GameSettingsData settings)
        {
            Canvas canvas = UIFactory.CreateCanvas("HUD", 10);
            HudUI hud = canvas.gameObject.AddComponent<HudUI>();
            hud.Build(inputController, vehicleController, vehicleLights, examManager, cameraController, settings);
            return hud;
        }

        private void Build(MobileInputController inputController, VehicleController vehicleController,
            VehicleLights vehicleLights, ExamManager examManager, CameraController cameraController,
            GameSettingsData settings)
        {
            input = inputController;
            vehicle = vehicleController;
            lights = vehicleLights;
            exam = examManager;
            cameraRig = cameraController;

            Transform root = transform;
            BuildExamPanel(root);
            BuildDashboard(root);
            BuildControls(root, settings);
            BuildTopButtons(root);
            BuildToast(root);
            BuildKeyboardHint(root);
            BuildPausePanel(root);
            BuildResultPanel(root);

            GameEvents.ToastRequested += ShowToast;
            if (exam != null) exam.ExamFinished += OnExamFinished;
            if (input != null) input.PauseRequested += TogglePause;
            if (cameraRig != null) cameraRig.ModeChanged += OnCameraModeChanged;

            RefreshAll();
        }

        private void OnDestroy()
        {
            GameEvents.ToastRequested -= ShowToast;
            if (exam != null) exam.ExamFinished -= OnExamFinished;
            if (input != null) input.PauseRequested -= TogglePause;
            if (cameraRig != null) cameraRig.ModeChanged -= OnCameraModeChanged;
        }

        // ------------------------------------------------------------------ Qurish

        private void BuildExamPanel(Transform root)
        {
            Image panel = UIFactory.CreatePanel("ExamPanel", root, UITheme.PanelColor);
            panel.raycastTarget = false;
            UIFactory.Place(panel.rectTransform, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(640f, 330f));

            examTitle = UIFactory.CreateLabel("Title", panel.transform, string.Empty, 28, TextAnchor.UpperLeft,
                UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(examTitle.rectTransform, TopLeft, TopLeft, new Vector2(22f, -14f), new Vector2(440f, 44f));

            examScore = UIFactory.CreateLabel("Score", panel.transform, string.Empty, 30, TextAnchor.UpperRight,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Place(examScore.rectTransform, TopRight, TopRight, new Vector2(-22f, -14f), new Vector2(170f, 44f));

            examStatus = UIFactory.CreateLabel("Status", panel.transform, string.Empty, 24, TextAnchor.UpperLeft,
                UITheme.TextColor);
            UIFactory.Place(examStatus.rectTransform, TopLeft, TopLeft, new Vector2(22f, -62f), new Vector2(596f, 92f));

            examTimer = UIFactory.CreateLabel("Timer", panel.transform, string.Empty, 22, TextAnchor.UpperLeft,
                UITheme.TextMuted);
            UIFactory.Place(examTimer.rectTransform, TopLeft, TopLeft, new Vector2(22f, -158f), new Vector2(596f, 30f));

            checklistLabel = UIFactory.CreateLabel("Checklist", panel.transform, string.Empty, 22, TextAnchor.UpperLeft,
                UITheme.TextColor);
            UIFactory.Place(checklistLabel.rectTransform, TopLeft, TopLeft, new Vector2(22f, -192f), new Vector2(596f, 130f));
        }

        private void BuildDashboard(Transform root)
        {
            Image panel = UIFactory.CreatePanel("Dashboard", root, UITheme.PanelColor);
            panel.raycastTarget = false;
            UIFactory.Place(panel.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 18f), new Vector2(640f, 214f));
            Transform p = panel.transform;

            gearLabel = UIFactory.CreateLabel("Gear", p, "P", 62, TextAnchor.MiddleCenter, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(gearLabel.rectTransform, TopLeft, TopLeft, new Vector2(18f, -14f), new Vector2(106f, 84f));
            Text gearCaption = UIFactory.CreateLabel("GearCaption", p, "UZATMA", 16, TextAnchor.MiddleCenter, UITheme.TextMuted);
            UIFactory.Place(gearCaption.rectTransform, TopLeft, TopLeft, new Vector2(18f, -98f), new Vector2(106f, 22f));

            speedLabel = UIFactory.CreateLabel("Speed", p, "0", 84, TextAnchor.MiddleCenter, UITheme.TextColor, FontStyle.Bold);
            UIFactory.Place(speedLabel.rectTransform, TopCenter, TopCenter, new Vector2(0f, -4f), new Vector2(230f, 98f));
            Text speedCaption = UIFactory.CreateLabel("SpeedCaption", p, "km/soat", 20, TextAnchor.MiddleCenter, UITheme.TextMuted);
            UIFactory.Place(speedCaption.rectTransform, TopCenter, TopCenter, new Vector2(0f, -98f), new Vector2(230f, 26f));

            leftArrow = UIFactory.CreateLabel("LeftArrow", p, "◄", 46, TextAnchor.MiddleCenter, UITheme.Inactive);
            UIFactory.Place(leftArrow.rectTransform, TopCenter, TopCenter, new Vector2(-150f, -26f), new Vector2(50f, 60f));
            rightArrow = UIFactory.CreateLabel("RightArrow", p, "►", 46, TextAnchor.MiddleCenter, UITheme.Inactive);
            UIFactory.Place(rightArrow.rectTransform, TopCenter, TopCenter, new Vector2(150f, -26f), new Vector2(50f, 60f));

            iconLights = StatusIcon(p, "FARA", 0);
            iconHandbrake = StatusIcon(p, "Q.TORMOZ", 1);
            iconBelt = StatusIcon(p, "KAMAR", 2);
            iconEngine = StatusIcon(p, "DVIGATEL", 3);

            rpmLabel = UIFactory.CreateLabel("RpmLabel", p, string.Empty, 18, TextAnchor.MiddleLeft, UITheme.TextMuted);
            UIFactory.Place(rpmLabel.rectTransform, BottomLeft, BottomLeft, new Vector2(22f, 62f), new Vector2(596f, 24f));

            rpmFill = Bar(p, "RpmBar", new Vector2(22f, 40f), new Vector2(596f, 16f), UITheme.Accent, out rpmFillImage);
            Image fuelImage;
            fuelFill = Bar(p, "FuelBar", new Vector2(22f, 18f), new Vector2(596f, 10f), UITheme.Warning, out fuelImage);
        }

        private Text StatusIcon(Transform parent, string caption, int row)
        {
            Text label = UIFactory.CreateLabel("Icon_" + caption, parent, caption, 18, TextAnchor.MiddleRight,
                UITheme.Inactive, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, TopRight, TopRight, new Vector2(-18f, -14f - row * 26f), new Vector2(120f, 24f));
            return label;
        }

        private static RectTransform Bar(Transform parent, string name, Vector2 position, Vector2 size, Color color,
            out Image fillImage)
        {
            Image background = UIFactory.CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.1f));
            background.raycastTarget = false;
            UIFactory.Place(background.rectTransform, BottomLeft, BottomLeft, position, size);

            fillImage = UIFactory.CreatePanel("Fill", background.transform, color);
            fillImage.raycastTarget = false;
            RectTransform fill = fillImage.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            return fill;
        }

        private static void SetBar(RectTransform fill, float value)
        {
            if (fill != null)
            {
                fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            }
        }

        private void BuildControls(Transform root, GameSettingsData settings)
        {
            float opacity = settings != null ? settings.controlsOpacity : 0.85f;
            float scale = settings != null ? settings.controlsScale : 1f;
            bool useWheel = settings == null || settings.steeringMode == 0;

            RectTransform controls = UIFactory.CreateRect("Controls", root);
            UIFactory.Stretch(controls);
            CanvasGroup group = controls.gameObject.AddComponent<CanvasGroup>();
            group.alpha = opacity;

            // --- Chap: rul ---
            RectTransform left = UIFactory.CreateRect("SteeringGroup", controls);
            UIFactory.Place(left, BottomLeft, BottomLeft, new Vector2(40f, 40f), new Vector2(420f, 400f));
            left.localScale = Vector3.one * scale;

            Image wheelImage = UIFactory.CreateImage("SteeringWheel", left, new Color(0.9f, 0.94f, 1f, 0.9f),
                UIFactory.SteeringWheelSprite);
            UIFactory.Place(wheelImage.rectTransform, BottomLeft, BottomLeft, new Vector2(10f, 0f), new Vector2(380f, 380f));
            // Aylanish markazi to‘g‘ri bo‘lishi uchun pivot markazda.
            wheelImage.rectTransform.pivot = Center;
            wheelImage.rectTransform.anchoredPosition = new Vector2(200f, 190f);
            TouchSteeringWheel wheel = wheelImage.gameObject.AddComponent<TouchSteeringWheel>();
            wheel.wheelGraphic = wheelImage.rectTransform;
            wheelImage.gameObject.SetActive(useWheel);

            RectTransform steerButtons = UIFactory.CreateRect("SteerButtons", left);
            UIFactory.Place(steerButtons, BottomLeft, BottomLeft, Vector2.zero, new Vector2(420f, 200f));
            Text ignored;
            TouchButton steerLeft = UIFactory.CreateHoldButton("SteerLeft", steerButtons, "◄", 70, UITheme.ButtonColor, out ignored);
            UIFactory.Place((RectTransform)steerLeft.transform, BottomLeft, BottomLeft, Vector2.zero, new Vector2(200f, 200f));
            TouchButton steerRight = UIFactory.CreateHoldButton("SteerRight", steerButtons, "►", 70, UITheme.ButtonColor, out ignored);
            UIFactory.Place((RectTransform)steerRight.transform, BottomLeft, BottomLeft, new Vector2(220f, 0f), new Vector2(200f, 200f));
            steerButtons.gameObject.SetActive(!useWheel);

            // --- Amallar qatori ---
            RectTransform actions = UIFactory.CreateRect("ActionRow", controls);
            UIFactory.Place(actions, BottomLeft, BottomLeft, new Vector2(40f, 462f), new Vector2(930f, 86f));
            actions.localScale = Vector3.one * scale;

            engineImage = ActionButton(actions, 0, "DVIG.", input.ToggleEngine);
            beltImage = ActionButton(actions, 1, "KAMAR", input.ToggleSeatbelt);
            lightsImage = ActionButton(actions, 2, "FARA", input.CycleHeadlights);
            leftSignalImage = ActionButton(actions, 3, "◄", input.ToggleLeftSignal);
            hazardImage = ActionButton(actions, 4, "AVAR.", input.ToggleHazard);
            rightSignalImage = ActionButton(actions, 5, "►", input.ToggleRightSignal);

            TouchButton horn = UIFactory.CreateHoldButton("Horn", actions, "SIGNAL", 22, UITheme.ButtonColor, out ignored);
            UIFactory.Place((RectTransform)horn.transform, BottomLeft, BottomLeft, new Vector2(6 * 132f, 0f), new Vector2(122f, 86f));

            // --- O‘ng: pedallar, qo‘l tormozi, uzatmalar ---
            RectTransform right = UIFactory.CreateRect("PedalGroup", controls);
            UIFactory.Place(right, BottomRight, BottomRight, new Vector2(-40f, 40f), new Vector2(620f, 720f));
            right.localScale = Vector3.one * scale;

            TouchButton gas = UIFactory.CreateHoldButton("GasPedal", right, "GAZ", 30, new Color(0.1f, 0.42f, 0.25f, 0.96f), out ignored);
            UIFactory.Place((RectTransform)gas.transform, BottomRight, BottomRight, Vector2.zero, new Vector2(180f, 300f));

            TouchButton brake = UIFactory.CreateHoldButton("BrakePedal", right, "TORMOZ", 28, new Color(0.5f, 0.12f, 0.17f, 0.96f), out ignored);
            UIFactory.Place((RectTransform)brake.transform, BottomRight, BottomRight, new Vector2(-200f, 0f), new Vector2(210f, 210f));

            Button handbrake = UIFactory.CreateButton("Handbrake", right, "QO‘L\nTORMOZI", 22, UITheme.ButtonColor, input.ToggleHandbrake);
            UIFactory.Place((RectTransform)handbrake.transform, BottomRight, BottomRight, new Vector2(-430f, 0f), new Vector2(170f, 150f));
            handbrakeImage = (Image)handbrake.targetGraphic;

            RectTransform autoRoot = UIFactory.CreateRect("AutoGears", right);
            UIFactory.Place(autoRoot, BottomRight, BottomRight, new Vector2(0f, 320f), new Vector2(130f, 390f));
            autoGearRoot = autoRoot.gameObject;
            autoGearImages[0] = GearButton(autoRoot, "P", 300f, AutoGearSelector.Park);
            autoGearImages[1] = GearButton(autoRoot, "R", 200f, AutoGearSelector.Reverse);
            autoGearImages[2] = GearButton(autoRoot, "N", 100f, AutoGearSelector.Neutral);
            autoGearImages[3] = GearButton(autoRoot, "D", 0f, AutoGearSelector.Drive);

            RectTransform manualRoot = UIFactory.CreateRect("ManualGears", right);
            UIFactory.Place(manualRoot, BottomRight, BottomRight, new Vector2(0f, 320f), new Vector2(130f, 290f));
            manualGearRoot = manualRoot.gameObject;
            Button shiftUp = UIFactory.CreateButton("ShiftUp", manualRoot, "+", 46, UITheme.ButtonColor, input.ShiftUp);
            UIFactory.Place((RectTransform)shiftUp.transform, BottomRight, BottomRight, new Vector2(0f, 200f), new Vector2(130f, 90f));
            manualGearLabel = UIFactory.CreateLabel("ManualGear", manualRoot, "N", 50, TextAnchor.MiddleCenter, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(manualGearLabel.rectTransform, BottomRight, BottomRight, new Vector2(0f, 100f), new Vector2(130f, 90f));
            Button shiftDown = UIFactory.CreateButton("ShiftDown", manualRoot, "–", 46, UITheme.ButtonColor, input.ShiftDown);
            UIFactory.Place((RectTransform)shiftDown.transform, BottomRight, BottomRight, Vector2.zero, new Vector2(130f, 90f));

            // Boshqaruv elementlarini kontrollerga ulash.
            input.gasPedal = gas;
            input.brakePedal = brake;
            input.steerLeftButton = steerLeft;
            input.steerRightButton = steerRight;
            input.hornButton = horn;
            input.steeringWheel = wheel;
        }

        private Image ActionButton(Transform parent, int index, string caption, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UIFactory.CreateButton("Action_" + index, parent, caption, 22, UITheme.ButtonColor, onClick);
            UIFactory.Place((RectTransform)button.transform, BottomLeft, BottomLeft, new Vector2(index * 132f, 0f), new Vector2(122f, 86f));
            return (Image)button.targetGraphic;
        }

        private Image GearButton(Transform parent, string caption, float y, AutoGearSelector selector)
        {
            Button button = UIFactory.CreateButton("Gear_" + caption, parent, caption, 38, UITheme.ButtonColor,
                () => input.SelectAutoGear(selector));
            UIFactory.Place((RectTransform)button.transform, BottomRight, BottomRight, new Vector2(0f, y), new Vector2(130f, 90f));
            return (Image)button.targetGraphic;
        }

        private void BuildTopButtons(Transform root)
        {
            Button pause = UIFactory.CreateButton("PauseButton", root, "II", 34, UITheme.ButtonColor, TogglePause);
            UIFactory.Place((RectTransform)pause.transform, TopRight, TopRight, new Vector2(-24f, -24f), new Vector2(110f, 92f));

            Button cameraButton = UIFactory.CreateButton("CameraButton", root, "KAMERA", 22, UITheme.ButtonColor, input.NextCamera);
            UIFactory.Place((RectTransform)cameraButton.transform, TopRight, TopRight, new Vector2(-146f, -24f), new Vector2(170f, 92f));
        }

        private void BuildToast(Transform root)
        {
            Image background = UIFactory.CreatePanel("Toast", root, new Color(0.02f, 0.03f, 0.05f, 0.9f));
            background.raycastTarget = false;
            UIFactory.Place(background.rectTransform, TopCenter, TopCenter, new Vector2(0f, -28f), new Vector2(860f, 64f));
            toastLabel = UIFactory.CreateLabel("Label", background.transform, string.Empty, 26, TextAnchor.MiddleCenter,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Stretch(toastLabel.rectTransform, 8f);
            toastRoot = background.gameObject;
            toastRoot.SetActive(false);
        }

        private void BuildKeyboardHint(Transform root)
        {
            if (Application.isMobilePlatform)
            {
                return;
            }

            Text hint = UIFactory.CreateLabel("KeyboardHint", root,
                "W/S gaz·tormoz   A/D rul   Space qo‘l tormozi   I dvigatel   B kamar   Q/E burilish signali   " +
                "Z avariya   L fara   H signal   C kamera   Shift/Ctrl uzatma   Esc pauza",
                17, TextAnchor.MiddleCenter, UITheme.TextMuted);
            UIFactory.Place(hint.rectTransform, TopCenter, TopCenter, new Vector2(300f, -124f), new Vector2(1140f, 48f));
        }

        private void BuildPausePanel(Transform root)
        {
            Image backdrop = UIFactory.CreateImage("PausePanel", root, UITheme.Backdrop);
            UIFactory.Stretch(backdrop.rectTransform);
            pausePanel = backdrop.gameObject;

            Image panel = UIFactory.CreatePanel("Window", backdrop.transform, UITheme.PanelColor);
            UIFactory.Place(panel.rectTransform, Center, Center, Vector2.zero, new Vector2(560f, 500f));

            Text title = UIFactory.CreateLabel("Title", panel.transform, "PAUZA", 44, TextAnchor.MiddleCenter, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, TopCenter, TopCenter, new Vector2(0f, -30f), new Vector2(500f, 60f));

            PanelButton(panel.transform, "DAVOM ETISH", -130f, UITheme.AccentDark, TogglePause);
            PanelButton(panel.transform, "QAYTA BOSHLASH", -240f, UITheme.ButtonColor, Restart);
            PanelButton(panel.transform, "BOSH MENYU", -350f, UITheme.ButtonColor, GoToMenu);

            pausePanel.SetActive(false);
        }

        private void BuildResultPanel(Transform root)
        {
            Image backdrop = UIFactory.CreateImage("ResultPanel", root, UITheme.Backdrop);
            UIFactory.Stretch(backdrop.rectTransform);
            resultPanel = backdrop.gameObject;

            Image panel = UIFactory.CreatePanel("Window", backdrop.transform, UITheme.PanelColor);
            UIFactory.Place(panel.rectTransform, Center, Center, Vector2.zero, new Vector2(1000f, 820f));

            resultTitle = UIFactory.CreateLabel("Title", panel.transform, string.Empty, 46, TextAnchor.MiddleCenter,
                UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(resultTitle.rectTransform, TopCenter, TopCenter, new Vector2(0f, -28f), new Vector2(920f, 64f));

            resultBody = UIFactory.CreateLabel("Body", panel.transform, string.Empty, 24, TextAnchor.UpperLeft, UITheme.TextColor);
            UIFactory.Place(resultBody.rectTransform, TopCenter, TopCenter, new Vector2(0f, -110f), new Vector2(900f, 560f));

            Button retry = UIFactory.CreateButton("Retry", panel.transform, "QAYTA TOPSHIRISH", 26, UITheme.AccentDark, Restart);
            UIFactory.Place((RectTransform)retry.transform, BottomCenter, BottomCenter, new Vector2(-230f, 30f), new Vector2(420f, 90f));
            Button menu = UIFactory.CreateButton("Menu", panel.transform, "BOSH MENYU", 26, UITheme.ButtonColor, GoToMenu);
            UIFactory.Place((RectTransform)menu.transform, BottomCenter, BottomCenter, new Vector2(230f, 30f), new Vector2(420f, 90f));

            resultPanel.SetActive(false);
        }

        private static void PanelButton(Transform parent, string caption, float y, Color color, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UIFactory.CreateButton("Button_" + caption, parent, caption, 28, color, onClick);
            UIFactory.Place((RectTransform)button.transform, TopCenter, TopCenter, new Vector2(0f, y), new Vector2(440f, 90f));
        }

        // ------------------------------------------------------------------ Yangilash

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f && toastRoot != null)
                {
                    toastRoot.SetActive(false);
                }
            }

            if (vehicle == null)
            {
                return;
            }

            bool leftOn = lights != null && lights.LeftLampOn;
            bool rightOn = lights != null && lights.RightLampOn;
            leftArrow.color = leftOn ? UITheme.Success : UITheme.Inactive;
            rightArrow.color = rightOn ? UITheme.Success : UITheme.Inactive;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.1f;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (vehicle == null)
            {
                return;
            }

            RefreshDashboard();
            RefreshControls();
            RefreshExam();
        }

        private void RefreshDashboard()
        {
            int speed = Mathf.RoundToInt(vehicle.SpeedKmh);
            if (speed != lastSpeed)
            {
                lastSpeed = speed;
                speedLabel.text = speed.ToString();
            }

            string gear = vehicle.GearLabel;
            if (gear != lastGear)
            {
                lastGear = gear;
                gearLabel.text = gear;
            }

            float rpmNormalized = vehicle.RpmNormalized;
            SetBar(rpmFill, rpmNormalized);
            rpmFillImage.color = rpmNormalized > 0.88f ? UITheme.Danger : UITheme.Accent;
            SetBar(fuelFill, vehicle.FuelNormalized);

            int rpm = Mathf.RoundToInt(vehicle.EngineRpm / 50f) * 50;
            builder.Length = 0;
            builder.Append(rpm).Append(" ayl/min    yoqilg‘i ").Append(vehicle.FuelLiters.ToString("0.0")).Append(" l");
            rpmLabel.text = builder.ToString();

            bool headlightsOn = lights != null && lights.Headlights != HeadlightMode.Off;
            iconLights.color = headlightsOn ? UITheme.Accent : UITheme.Inactive;
            iconHandbrake.color = vehicle.HandbrakeEngaged ? UITheme.Danger : UITheme.Inactive;
            iconBelt.color = vehicle.SeatbeltFastened ? UITheme.Success : UITheme.Warning;
            iconEngine.color = vehicle.EngineRunning ? UITheme.Success : UITheme.Inactive;
        }

        private void RefreshControls()
        {
            bool automatic = vehicle.Transmission == TransmissionType.Automatic;
            if (autoGearRoot.activeSelf != automatic) autoGearRoot.SetActive(automatic);
            if (manualGearRoot.activeSelf == automatic) manualGearRoot.SetActive(!automatic);

            if (automatic)
            {
                int selected = (int)vehicle.Selector;
                for (int i = 0; i < autoGearImages.Length; i++)
                {
                    autoGearImages[i].color = i == selected ? UITheme.AccentDark : UITheme.ButtonColor;
                }
            }
            else
            {
                manualGearLabel.text = vehicle.GearLabel;
            }

            handbrakeImage.color = vehicle.HandbrakeEngaged ? UITheme.Danger : UITheme.ButtonColor;
            engineImage.color = vehicle.EngineRunning ? UITheme.Success : UITheme.ButtonColor;
            beltImage.color = vehicle.SeatbeltFastened ? UITheme.Success : UITheme.ButtonColor;

            if (lights != null)
            {
                lightsImage.color = lights.Headlights != HeadlightMode.Off ? UITheme.AccentDark : UITheme.ButtonColor;
                leftSignalImage.color = lights.Indicator == IndicatorMode.Left ? UITheme.Success : UITheme.ButtonColor;
                rightSignalImage.color = lights.Indicator == IndicatorMode.Right ? UITheme.Success : UITheme.ButtonColor;
                hazardImage.color = lights.Indicator == IndicatorMode.Hazard ? UITheme.Warning : UITheme.ButtonColor;
            }
        }

        private void RefreshExam()
        {
            if (exam == null || exam.Scoring == null)
            {
                return;
            }

            ExamConfig config = exam.Config;
            int score = exam.Scoring.Score;
            examScore.text = score + " ball";
            examScore.color = score >= config.passScore ? UITheme.Success : UITheme.Danger;

            switch (exam.State)
            {
                case ExamState.PreCheck:
                    examTitle.text = "TAYYORGARLIK";
                    examStatus.text = "Quyidagilarni bajarib, harakatni boshlang.";
                    examTimer.text = exam.IsFullExam ? "Rejim: to‘liq imtihon" : "Rejim: bitta mashq (mashg‘ulot)";
                    builder.Length = 0;
                    AppendCheck(exam.SeatbeltOk, "Xavfsizlik kamarini taqing");
                    AppendCheck(exam.EngineOk, "Dvigatelni yoqing");
                    AppendCheck(exam.GearOk, vehicle.Transmission == TransmissionType.Automatic
                        ? "Tormozni bosib, D ga o‘tkazing"
                        : "1-uzatmani yoqing");
                    AppendCheck(exam.LeftSignalOk, "Chap burilish signalini yoqing");
                    AppendCheck(exam.HandbrakeReleasedOk, "Qo‘l tormozini tushiring");
                    checklistLabel.text = builder.ToString();
                    break;

                case ExamState.Active:
                    RefreshActiveExam(config);
                    break;

                case ExamState.Finished:
                    examTitle.text = "YAKUNLANDI";
                    examStatus.text = string.Empty;
                    examTimer.text = "Umumiy vaqt: " + FormatTime(exam.ExamTime);
                    checklistLabel.text = string.Empty;
                    break;

                default:
                    examTitle.text = string.Empty;
                    break;
            }
        }

        private void RefreshActiveExam(ExamConfig config)
        {
            ExerciseBase exercise = exam.CurrentExercise;
            if (exercise == null)
            {
                return;
            }

            builder.Length = 0;
            builder.Append("MASHQ ").Append(exam.CurrentExerciseNumber).Append('/').Append(exam.ExerciseCount)
                .Append(": ").Append(exercise.displayName.ToUpperInvariant());
            examTitle.text = builder.ToString();

            if (exercise.State == ExerciseState.Waiting)
            {
                float distance = exam.DistanceToGuide();
                builder.Length = 0;
                builder.Append("Yashil mayoq tomon yuring — mashq boshlanish joyi");
                if (distance >= 0f)
                {
                    builder.Append(" (").Append(Mathf.RoundToInt(distance)).Append(" m)");
                }

                examStatus.text = builder.ToString();
                examTimer.text = "Umumiy vaqt: " + FormatTime(exam.ExamTime);
            }
            else
            {
                examStatus.text = exercise.StatusText;
                examTimer.text = "Mashq vaqti: " + FormatTime(exercise.ElapsedTime) + " / " + FormatTime(exercise.TimeLimit);
            }

            builder.Length = 0;
            builder.Append("Tezlik chegarasi: ").Append(Mathf.RoundToInt(config.speedLimitKmh)).Append(" km/soat\n");
            builder.Append("O‘tish bali: ").Append(config.passScore).Append(" / ").Append(config.startingScore).Append('\n');
            builder.Append("<color=#8A97AB>O‘yin ichki baholash — rasmiy emas</color>");
            checklistLabel.text = builder.ToString();
        }

        private void AppendCheck(bool done, string text)
        {
            builder.Append(done ? "<color=#2ECC71>●</color>  " : "<color=#8A97AB>○</color>  ");
            builder.Append(text).Append('\n');
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        // ------------------------------------------------------------------ Hodisalar

        private void ShowToast(string message)
        {
            if (toastRoot == null)
            {
                return;
            }

            toastLabel.text = message;
            toastRoot.SetActive(true);
            toastTimer = 2.6f;
        }

        private void OnCameraModeChanged(CameraMode mode)
        {
            SaveSystem.Data.settings.cameraMode = (int)mode;
            SaveSystem.Save();
            ShowToast("Kamera: " + CameraController.ModeName(mode));
        }

        private void TogglePause()
        {
            if (resultPanel != null && resultPanel.activeSelf)
            {
                return;
            }

            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            pausePanel.SetActive(paused);
            if (input != null)
            {
                input.InputEnabled = !paused;
            }
        }

        private void Restart()
        {
            SceneLoader.ReloadCurrent();
        }

        private void GoToMenu()
        {
            SceneLoader.LoadMainMenu();
        }

        private void OnExamFinished(ExamResultData result, bool licenseIssuedNow)
        {
            if (input != null)
            {
                input.InputEnabled = false;
            }

            ExamConfig config = exam.Config;
            resultTitle.text = result.passed ? "MUVAFFAQIYATLI O‘TDINGIZ" : "O‘TA OLMADINGIZ";
            resultTitle.color = result.passed ? UITheme.Success : UITheme.Danger;

            builder.Length = 0;
            builder.Append("<b>Ball:</b> ").Append(result.score).Append(" / ").Append(config.startingScore)
                .Append("   (o‘tish bali: ").Append(config.passScore).Append(")\n");
            builder.Append("<b>Vaqt:</b> ").Append(FormatTime(result.durationSeconds)).Append('\n');
            builder.Append("<b>Rejim:</b> ").Append(exam.IsFullExam ? "to‘liq imtihon" : "bitta mashq (mashg‘ulot)").Append("\n\n");

            if (!string.IsNullOrEmpty(exam.FailReason))
            {
                builder.Append("<color=#FF3B5C>").Append(exam.FailReason).Append("</color>\n\n");
            }

            if (result.penalties.Count == 0)
            {
                builder.Append("<color=#2ECC71>Jarima ballari yo‘q. Ajoyib!</color>\n");
            }
            else
            {
                builder.Append("<b>Jarimalar:</b>\n");
                int shown = Mathf.Min(result.penalties.Count, 9);
                for (int i = 0; i < shown; i++)
                {
                    builder.Append("  •  ").Append(result.penalties[i]).Append('\n');
                }

                if (result.penalties.Count > shown)
                {
                    builder.Append("  … va yana ").Append(result.penalties.Count - shown).Append(" ta\n");
                }
            }

            builder.Append('\n');
            SaveData save = SaveSystem.Data;
            if (licenseIssuedNow)
            {
                builder.Append("<color=#29D3FF><b>Virtual guvohnoma berildi: ").Append(save.virtualLicenseNumber)
                    .Append("</b></color>\n");
                builder.Append("<color=#8A97AB>Bu guvohnoma faqat o‘yin ichida amal qiladi va haqiqiy haydovchilik " +
                               "guvohnomasi emas.</color>\n");
            }
            else if (result.passed && !exam.IsFullExam)
            {
                builder.Append("<color=#8A97AB>Virtual guvohnoma faqat to‘liq imtihon uchun beriladi.</color>\n");
            }

            builder.Append("<color=#8A97AB>").Append(config.disclaimer).Append("</color>");
            resultBody.text = builder.ToString();
            resultPanel.SetActive(true);
        }
    }
}
