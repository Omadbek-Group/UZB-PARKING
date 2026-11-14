using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UzbCarParking.Core;
using UzbCarParking.Garage;
using UzbCarParking.Persistence;

namespace UzbCarParking.UserInterface
{
    /// <summary>
    /// Bosh menyu: O‘ynash, Avtodrom, Mening garajim, Sozlamalar, Profil.
    /// Keyingi bosqichlarga tegishli bo‘limlar (Ochiq dunyo, Multiplayer, Skaner, Karyera, Reyting)
    /// qulflangan holda ko‘rsatiladi — ular hali ishlab chiqilmagan.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        private static readonly string[] PaintColors =
        {
            "#E8E8E8", "#0E0E10", "#8E9399", "#C0392B", "#1F4E9E", "#1E8449", "#F1C40F", "#E67E22", "#6C3483", "#2AB7CA"
        };

        private static readonly string[] RimColors =
        {
            "#9EA3AB", "#1A1A1C", "#E5E7EA", "#B8860B", "#C0392B", "#29D3FF"
        };

        private GarageShowroom showroom;
        private SaveData save;

        private GameObject mainPanel;
        private GameObject avtodromPanel;
        private GameObject garagePanel;
        private GameObject settingsPanel;
        private GameObject profilePanel;

        private Text licenseBadge;
        private Text plateRegionLabel;
        private Text plateStatusLabel;
        private InputField plateInput;
        private Text profileBody;
        private Text resetLabel;
        private bool resetArmed;

        private readonly Image[] transmissionImages = new Image[2];
        private readonly Image[] graphicsImages = new Image[4];
        private readonly Image[] steeringImages = new Image[2];

        private GameObject toastRoot;
        private Text toastLabel;
        private float toastTimer;

        public static MainMenuUI Create(GarageShowroom garageShowroom)
        {
            Canvas canvas = UIFactory.CreateCanvas("MainMenu", 10);
            MainMenuUI menu = canvas.gameObject.AddComponent<MainMenuUI>();
            menu.Build(garageShowroom);
            return menu;
        }

        private void Build(GarageShowroom garageShowroom)
        {
            showroom = garageShowroom;
            save = SaveSystem.Data;

            BuildMainPanel();
            BuildAvtodromPanel();
            BuildGaragePanel();
            BuildSettingsPanel();
            BuildProfilePanel();
            BuildToast();

            GameEvents.ToastRequested += ShowToast;
            ShowPanel(mainPanel);
        }

        private void OnDestroy()
        {
            GameEvents.ToastRequested -= ShowToast;
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f)
                {
                    toastRoot.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------ Bosh panel

        private void BuildMainPanel()
        {
            RectTransform panel = UIFactory.CreateRect("MainPanel", transform);
            UIFactory.Stretch(panel);
            mainPanel = panel.gameObject;

            Text title = UIFactory.CreateLabel("Title", panel, GameConstants.GameTitle, 64, TextAnchor.MiddleLeft,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, TopLeft, TopLeft, new Vector2(60f, -56f), new Vector2(820f, 80f));

            Text subtitle = UIFactory.CreateLabel("Subtitle", panel, GameConstants.GameSubtitle, 30, TextAnchor.MiddleLeft,
                UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(subtitle.rectTransform, TopLeft, TopLeft, new Vector2(64f, -136f), new Vector2(820f, 40f));

            MenuButton(panel, 0, 0, "O‘YNASH", UITheme.AccentDark, StartFullExam);
            MenuButton(panel, 1, 0, "AVTODROM", UITheme.ButtonColor, () => ShowPanel(avtodromPanel));
            LockedButton(panel, 0, 1, "OCHIQ DUNYO", "3-bosqich");
            LockedButton(panel, 1, 1, "MULTIPLAYER", "4-bosqich");
            MenuButton(panel, 0, 2, "MENING GARAJIM", UITheme.ButtonColor, () => ShowPanel(garagePanel));
            LockedButton(panel, 1, 2, "MASHINA SKANERI", "2-bosqich");
            LockedButton(panel, 0, 3, "KARYERA", "3-bosqich");
            LockedButton(panel, 1, 3, "REYTING", "4-bosqich");
            MenuButton(panel, 0, 4, "SOZLAMALAR", UITheme.ButtonColor, () => ShowPanel(settingsPanel));
            MenuButton(panel, 1, 4, "PROFIL", UITheme.ButtonColor, () => ShowPanel(profilePanel));

            licenseBadge = UIFactory.CreateLabel("LicenseBadge", panel, string.Empty, 22, TextAnchor.MiddleLeft, UITheme.TextMuted);
            UIFactory.Place(licenseBadge.rectTransform, BottomLeft, BottomLeft, new Vector2(64f, 84f), new Vector2(900f, 60f));

            Text version = UIFactory.CreateLabel("Version", panel,
                "v" + GameConstants.Version + "  ·  1-bosqich (MVP)  ·  placeholder grafika", 18, TextAnchor.MiddleLeft,
                UITheme.TextMuted);
            UIFactory.Place(version.rectTransform, BottomLeft, BottomLeft, new Vector2(64f, 40f), new Vector2(900f, 30f));
        }

        private static void MenuButton(Transform parent, int column, int row, string caption, Color color, UnityAction onClick)
        {
            Button button = UIFactory.CreateButton("Menu_" + caption, parent, caption, 28, color, onClick);
            UIFactory.Place((RectTransform)button.transform, TopLeft, TopLeft,
                new Vector2(60f + column * 350f, -230f - row * 110f), new Vector2(330f, 94f));
        }

        private static void LockedButton(Transform parent, int column, int row, string caption, string stage)
        {
            string message = caption + " — " + stage + "da qo‘shiladi";
            Text label;
            Button button = UIFactory.CreateButton("Locked_" + caption, parent,
                caption + "\n<size=17>" + stage + "da</size>", 24, UITheme.ButtonLocked,
                () => GameEvents.Toast(message), out label);
            label.color = UITheme.TextMuted;
            UIFactory.Place((RectTransform)button.transform, TopLeft, TopLeft,
                new Vector2(60f + column * 350f, -230f - row * 110f), new Vector2(330f, 94f));
        }

        private void RefreshLicenseBadge()
        {
            if (save.hasVirtualLicense)
            {
                licenseBadge.color = UITheme.Success;
                licenseBadge.text = "Virtual guvohnoma: " + save.virtualLicenseNumber +
                                    "\n<size=17><color=#8A97AB>Faqat o‘yin ichida amal qiladi — haqiqiy hujjat emas.</color></size>";
            }
            else
            {
                licenseBadge.color = UITheme.TextMuted;
                licenseBadge.text = "Virtual guvohnoma yo‘q — to‘liq imtihondan o‘ting.";
            }
        }

        // ------------------------------------------------------------------ Avtodrom

        private void BuildAvtodromPanel()
        {
            Transform content;
            avtodromPanel = CreateWindow("AVTODROM", out content);

            Text note = UIFactory.CreateLabel("Note", content,
                "B toifali amaliy mashqlarga tayyorgarlik. Baholash — o‘yin ichki tizim bo‘lib, rasmiy imtihon " +
                "qoidalari bilan tasdiqlanmagan.", 22, TextAnchor.UpperLeft, UITheme.TextMuted);
            UIFactory.Place(note.rectTransform, TopLeft, TopLeft, new Vector2(40f, -100f), new Vector2(720f, 90f));

            Item(content, "TO‘LIQ IMTIHON — 3 ta mashq", 40f, -210f, 720f, 104f, UITheme.AccentDark, StartFullExam, 28);
            Item(content, "ESTAKADA  (mashg‘ulot)", 40f, -340f, 720f, 92f, UITheme.ButtonColor,
                () => StartExercise(GameConstants.ExerciseEstakada));
            Item(content, "PARALLEL PARKOVKA  (mashg‘ulot)", 40f, -446f, 720f, 92f, UITheme.ButtonColor,
                () => StartExercise(GameConstants.ExerciseParallel));
            Item(content, "BOKSGA ORQA BILAN KIRISH  (mashg‘ulot)", 40f, -552f, 720f, 92f, UITheme.ButtonColor,
                () => StartExercise(GameConstants.ExerciseBox));

            Text later = UIFactory.CreateLabel("Later", content,
                "Zmeyka, 90° burilish, cheklangan hududda qayrilish va boshqa mashqlar keyingi yangilanishlarda.",
                20, TextAnchor.UpperLeft, UITheme.TextMuted);
            UIFactory.Place(later.rectTransform, TopLeft, TopLeft, new Vector2(40f, -670f), new Vector2(720f, 70f));
        }

        private void StartFullExam()
        {
            SaveSystem.Save();
            SessionContext.SelectFullExam();
            SceneLoader.LoadAvtodrom();
        }

        private void StartExercise(string exerciseId)
        {
            SaveSystem.Save();
            SessionContext.SelectExercise(exerciseId);
            SceneLoader.LoadAvtodrom();
        }

        // ------------------------------------------------------------------ Garaj

        private void BuildGaragePanel()
        {
            Transform content;
            garagePanel = CreateWindow("MENING GARAJIM", out content);

            Section(content, "Kuzov rangi", -104f);
            for (int i = 0; i < PaintColors.Length; i++)
            {
                string hex = PaintColors[i];
                Swatch(content, hex, 40f + i * 70f, -146f, () => SetPaint(hex));
            }

            Section(content, "Disk rangi", -232f);
            for (int i = 0; i < RimColors.Length; i++)
            {
                string hex = RimColors[i];
                Swatch(content, hex, 40f + i * 70f, -274f, () => SetRims(hex));
            }

            Section(content, "Davlat raqami (virtual)", -360f);
            Item(content, "◄", 40f, -402f, 64f, 66f, UITheme.ButtonColor, () => ChangeRegion(-1), 30);
            plateRegionLabel = UIFactory.CreateLabel("Region", content, string.Empty, 24, TextAnchor.MiddleCenter,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Place(plateRegionLabel.rectTransform, TopLeft, TopLeft, new Vector2(110f, -402f), new Vector2(290f, 66f));
            Item(content, "►", 406f, -402f, 64f, 66f, UITheme.ButtonColor, () => ChangeRegion(1), 30);

            plateInput = UIFactory.CreateInputField("PlateInput", content, save.vehicle.plateNumber, "A777AA", 30, 6,
                OnPlateEdited);
            UIFactory.Place((RectTransform)plateInput.transform, TopLeft, TopLeft, new Vector2(490f, -402f), new Vector2(270f, 66f));

            plateStatusLabel = UIFactory.CreateLabel("PlateStatus", content, string.Empty, 20, TextAnchor.MiddleLeft,
                UITheme.TextMuted);
            UIFactory.Place(plateStatusLabel.rectTransform, TopLeft, TopLeft, new Vector2(40f, -474f), new Vector2(720f, 30f));

            Section(content, "Uzatmalar qutisi", -528f);
            transmissionImages[0] = Item(content, "AVTOMAT  (P·R·N·D)", 40f, -570f, 350f, 80f, UITheme.ButtonColor,
                () => SetTransmission(0));
            transmissionImages[1] = Item(content, "MEXANIK  (1–5, R)", 410f, -570f, 350f, 80f, UITheme.ButtonColor,
                () => SetTransmission(1));

            Text note = UIFactory.CreateLabel("Note", content,
                "Hozirgi mashina — vaqtinchalik (placeholder) model. Haqiqiy 3D modellar, disklar, tonirovka, neon " +
                "va osma sozlamalari keyingi bosqichlarda qo‘shiladi.", 20, TextAnchor.UpperLeft, UITheme.TextMuted);
            UIFactory.Place(note.rectTransform, TopLeft, TopLeft, new Vector2(40f, -680f), new Vector2(720f, 100f));

            RefreshGarage();
        }

        private void RefreshGarage()
        {
            int index = PlateNumber.RegionIndex(save.vehicle.plateRegion);
            plateRegionLabel.text = PlateNumber.Regions[index] + "  —  " + PlateNumber.RegionNames[index];
            plateStatusLabel.color = UITheme.TextMuted;
            plateStatusLabel.text = "Ko‘rinishi: " + PlateNumber.Format(save.vehicle.plateRegion, save.vehicle.plateNumber) +
                                    "    (format: A777AA yoki 777AAA)";
            SetSelected(transmissionImages, save.vehicle.transmission);
        }

        private void SetPaint(string hex)
        {
            save.vehicle.paintColorHex = hex;
            if (showroom != null && showroom.Customization != null)
            {
                showroom.Customization.SetPaint(ColorUtil.FromHex(hex, Color.white));
            }
        }

        private void SetRims(string hex)
        {
            save.vehicle.rimColorHex = hex;
            if (showroom != null && showroom.Customization != null)
            {
                showroom.Customization.SetRims(ColorUtil.FromHex(hex, Color.gray));
            }
        }

        private void ChangeRegion(int direction)
        {
            int count = PlateNumber.Regions.Length;
            int index = (PlateNumber.RegionIndex(save.vehicle.plateRegion) + direction + count) % count;
            save.vehicle.plateRegion = PlateNumber.Regions[index];
            ApplyPlate();
            RefreshGarage();
        }

        private void OnPlateEdited(string value)
        {
            if (PlateNumber.IsValidNumber(value))
            {
                save.vehicle.plateNumber = PlateNumber.Normalize(value);
                ApplyPlate();
                RefreshGarage();
            }
            else
            {
                plateStatusLabel.color = UITheme.Danger;
                plateStatusLabel.text = "Noto‘g‘ri format. To‘g‘ri misollar: A777AA yoki 777AAA";
            }

            plateInput.text = save.vehicle.plateNumber;
        }

        private void ApplyPlate()
        {
            if (showroom != null && showroom.Customization != null)
            {
                showroom.Customization.SetPlate(save.vehicle.plateRegion, save.vehicle.plateNumber);
            }
        }

        private void SetTransmission(int value)
        {
            save.vehicle.transmission = Mathf.Clamp(value, 0, 1);
            SetSelected(transmissionImages, save.vehicle.transmission);
        }

        // ------------------------------------------------------------------ Sozlamalar

        private void BuildSettingsPanel()
        {
            Transform content;
            settingsPanel = CreateWindow("SOZLAMALAR", out content);
            GameSettingsData settings = save.settings;

            Section(content, "Grafika sifati", -104f);
            graphicsImages[0] = Item(content, "AVTO", 40f, -146f, 170f, 72f, UITheme.ButtonColor, () => SetGraphics(-1), 22);
            graphicsImages[1] = Item(content, "PAST", 224f, -146f, 170f, 72f, UITheme.ButtonColor, () => SetGraphics(0), 22);
            graphicsImages[2] = Item(content, "O‘RTA", 408f, -146f, 170f, 72f, UITheme.ButtonColor, () => SetGraphics(1), 22);
            graphicsImages[3] = Item(content, "YUQORI", 592f, -146f, 168f, 72f, UITheme.ButtonColor, () => SetGraphics(2), 22);

            Section(content, "Rul boshqaruvi", -238f);
            steeringImages[0] = Item(content, "RUL CHAMBARAGI", 40f, -280f, 350f, 72f, UITheme.ButtonColor, () => SetSteeringMode(0), 22);
            steeringImages[1] = Item(content, "CHAP / O‘NG TUGMALAR", 410f, -280f, 350f, 72f, UITheme.ButtonColor, () => SetSteeringMode(1), 22);

            SliderRow(content, "Rul sezgirligi", -384f, 0.5f, 2f, settings.steeringSensitivity, "0.0",
                value => save.settings.steeringSensitivity = value);
            SliderRow(content, "Ovoz balandligi", -454f, 0f, 1f, settings.masterVolume, "0%",
                value =>
                {
                    save.settings.masterVolume = value;
                    AudioListener.volume = value;
                });
            SliderRow(content, "Tugmalar shaffofligi", -524f, 0.3f, 1f, settings.controlsOpacity, "0%",
                value => save.settings.controlsOpacity = value);
            SliderRow(content, "Tugmalar o‘lchami", -594f, 0.7f, 1.3f, settings.controlsScale, "0%",
                value => save.settings.controlsScale = value);

            Text language = UIFactory.CreateLabel("Language", content,
                "Til: O‘zbekcha   <color=#8A97AB>(rus va ingliz tillari keyingi bosqichda)</color>", 22,
                TextAnchor.MiddleLeft, UITheme.TextColor);
            UIFactory.Place(language.rectTransform, TopLeft, TopLeft, new Vector2(40f, -672f), new Vector2(720f, 36f));

            Button reset = UIFactory.CreateButton("Reset", content, "PROGRESSNI TOZALASH", 22,
                new Color(0.45f, 0.1f, 0.15f, 0.95f), OnResetClicked, out resetLabel);
            UIFactory.Place((RectTransform)reset.transform, TopLeft, TopLeft, new Vector2(40f, -740f), new Vector2(720f, 72f));

            SetSelected(graphicsImages, settings.graphicsLevel + 1);
            SetSelected(steeringImages, settings.steeringMode);
        }

        private void SliderRow(Transform parent, string caption, float y, float min, float max, float value,
            string format, UnityAction<float> onChanged)
        {
            Text label = UIFactory.CreateLabel("Label_" + caption, parent, caption, 22, TextAnchor.MiddleLeft, UITheme.TextColor);
            UIFactory.Place(label.rectTransform, TopLeft, TopLeft, new Vector2(40f, y), new Vector2(300f, 50f));

            Text valueLabel = UIFactory.CreateLabel("Value_" + caption, parent, value.ToString(format), 22,
                TextAnchor.MiddleRight, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(valueLabel.rectTransform, TopLeft, TopLeft, new Vector2(670f, y), new Vector2(90f, 50f));

            Slider slider = UIFactory.CreateSlider("Slider_" + caption, parent, min, max, value, v =>
            {
                valueLabel.text = v.ToString(format);
                onChanged(v);
            });
            UIFactory.Place((RectTransform)slider.transform, TopLeft, TopLeft, new Vector2(340f, y), new Vector2(320f, 50f));
        }

        private void SetGraphics(int level)
        {
            save.settings.graphicsLevel = Mathf.Clamp(level, -1, 2);
            SetSelected(graphicsImages, save.settings.graphicsLevel + 1);
            GraphicsQuality.Apply(GraphicsQuality.Resolve(save.settings.graphicsLevel));
        }

        private void SetSteeringMode(int mode)
        {
            save.settings.steeringMode = Mathf.Clamp(mode, 0, 1);
            SetSelected(steeringImages, save.settings.steeringMode);
        }

        private void OnResetClicked()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                resetLabel.text = "TASDIQLASH UCHUN YANA BOSING — BARCHA NATIJALAR O‘CHADI";
                return;
            }

            SaveSystem.ResetAll();
            SceneLoader.ReloadCurrent();
        }

        // ------------------------------------------------------------------ Profil

        private void BuildProfilePanel()
        {
            Transform content;
            profilePanel = CreateWindow("PROFIL", out content);
            profileBody = UIFactory.CreateLabel("Body", content, string.Empty, 24, TextAnchor.UpperLeft, UITheme.TextColor);
            UIFactory.Place(profileBody.rectTransform, TopLeft, TopLeft, new Vector2(40f, -104f), new Vector2(720f, 740f));
        }

        private void RefreshProfile()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append("<b>Haydovchi:</b> ").Append(save.profile.playerName).Append('\n');
            builder.Append("<b>To‘liq imtihonlar:</b> ").Append(save.profile.examsTaken)
                .Append("   <b>O‘tilgan:</b> ").Append(save.profile.examsPassed).Append('\n');
            builder.Append("<b>Eng yaxshi natija:</b> ")
                .Append(save.bestExamScore >= 0 ? save.bestExamScore + " ball" : "—").Append("\n\n");

            if (save.hasVirtualLicense)
            {
                builder.Append("<color=#2ECC71><b>Virtual guvohnoma:</b> ").Append(save.virtualLicenseNumber).Append("</color>\n");
            }
            else
            {
                builder.Append("<b>Virtual guvohnoma:</b> yo‘q\n");
            }

            builder.Append("<size=19><color=#8A97AB>Virtual guvohnoma faqat o‘yin ichida amal qiladi va haqiqiy " +
                           "haydovchilik guvohnomasi emas.</color></size>\n\n");

            builder.Append("<b>So‘nggi natijalar:</b>\n");
            int count = save.examResults.Count;
            if (count == 0)
            {
                builder.Append("<color=#8A97AB>Hali natijalar yo‘q.</color>\n");
            }

            for (int i = count - 1; i >= 0 && i >= count - 8; i--)
            {
                ExamResultData result = save.examResults[i];
                string date = result.dateUtc != null && result.dateUtc.Length >= 10 ? result.dateUtc.Substring(0, 10) : "—";
                builder.Append(result.passed ? "<color=#2ECC71>●</color>  " : "<color=#FF3B5C>●</color>  ");
                builder.Append(date).Append("   ").Append(ModeName(result.mode)).Append("   ")
                    .Append(result.score).Append(" ball   ")
                    .Append(result.passed ? "o‘tdi" : "o‘tmadi").Append('\n');
            }

            profileBody.text = builder.ToString();
        }

        private static string ModeName(string mode)
        {
            if (mode == GameConstants.ExerciseEstakada) return "Estakada";
            if (mode == GameConstants.ExerciseParallel) return "Parallel parkovka";
            if (mode == GameConstants.ExerciseBox) return "Boks";
            return "To‘liq imtihon";
        }

        // ------------------------------------------------------------------ Umumiy

        private GameObject CreateWindow(string title, out Transform content)
        {
            Image panel = UIFactory.CreatePanel("Window_" + title, transform, UITheme.PanelColor);
            UIFactory.Place(panel.rectTransform, TopLeft, TopLeft, new Vector2(50f, -50f), new Vector2(800f, 980f));

            Text titleLabel = UIFactory.CreateLabel("Title", panel.transform, title, 44, TextAnchor.MiddleLeft,
                UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(titleLabel.rectTransform, TopLeft, TopLeft, new Vector2(40f, -26f), new Vector2(720f, 60f));

            Button back = UIFactory.CreateButton("Back", panel.transform, "ORQAGA", 26, UITheme.ButtonColor, BackToMain);
            UIFactory.Place((RectTransform)back.transform, BottomLeft, BottomLeft, new Vector2(40f, 30f), new Vector2(300f, 84f));

            content = panel.transform;
            return panel.gameObject;
        }

        private static void Section(Transform parent, string caption, float y)
        {
            Text label = UIFactory.CreateLabel("Section_" + caption, parent, caption, 24, TextAnchor.MiddleLeft,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Place(label.rectTransform, TopLeft, TopLeft, new Vector2(40f, y), new Vector2(720f, 36f));
        }

        private static Image Item(Transform parent, string caption, float x, float y, float width, float height,
            Color color, UnityAction onClick, int fontSize = 24)
        {
            Button button = UIFactory.CreateButton("Item_" + caption, parent, caption, fontSize, color, onClick);
            UIFactory.Place((RectTransform)button.transform, TopLeft, TopLeft, new Vector2(x, y), new Vector2(width, height));
            return (Image)button.targetGraphic;
        }

        private static void Swatch(Transform parent, string hex, float x, float y, UnityAction onClick)
        {
            Image frame = UIFactory.CreatePanel("SwatchFrame_" + hex, parent, new Color(1f, 1f, 1f, 0.25f));
            frame.raycastTarget = false;
            UIFactory.Place(frame.rectTransform, TopLeft, TopLeft, new Vector2(x - 2f, y + 2f), new Vector2(64f, 64f));

            Button button = UIFactory.CreateButton("Swatch_" + hex, parent, string.Empty, 10,
                ColorUtil.FromHex(hex, Color.white), onClick);
            UIFactory.Place((RectTransform)button.transform, TopLeft, TopLeft, new Vector2(x, y), new Vector2(60f, 60f));
        }

        private static void SetSelected(Image[] images, int selected)
        {
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = i == selected ? UITheme.AccentDark : UITheme.ButtonColor;
                }
            }
        }

        private void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            avtodromPanel.SetActive(panel == avtodromPanel);
            garagePanel.SetActive(panel == garagePanel);
            settingsPanel.SetActive(panel == settingsPanel);
            profilePanel.SetActive(panel == profilePanel);

            if (panel == mainPanel) RefreshLicenseBadge();
            if (panel == profilePanel) RefreshProfile();
            if (panel == garagePanel) RefreshGarage();
        }

        private void BackToMain()
        {
            resetArmed = false;
            if (resetLabel != null)
            {
                resetLabel.text = "PROGRESSNI TOZALASH";
            }

            SaveSystem.Save();
            ShowPanel(mainPanel);
        }

        private void BuildToast()
        {
            Image background = UIFactory.CreatePanel("Toast", transform, new Color(0.02f, 0.03f, 0.05f, 0.92f));
            background.raycastTarget = false;
            UIFactory.Place(background.rectTransform, BottomCenter, BottomCenter, new Vector2(0f, 150f), new Vector2(820f, 64f));
            toastLabel = UIFactory.CreateLabel("Label", background.transform, string.Empty, 24, TextAnchor.MiddleCenter,
                UITheme.TextColor, FontStyle.Bold);
            UIFactory.Stretch(toastLabel.rectTransform, 8f);
            toastRoot = background.gameObject;
            toastRoot.SetActive(false);
        }

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
    }
}
