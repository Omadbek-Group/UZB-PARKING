using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UzbCarParking.Controls;
using UzbCarParking.Core;

namespace UzbCarParking.UserInterface
{
    /// <summary>Interfeys ranglari (premium, qorong‘i uslub).</summary>
    public static class UITheme
    {
        public static readonly Color Backdrop = new Color(0.03f, 0.04f, 0.07f, 0.72f);
        public static readonly Color PanelColor = new Color(0.07f, 0.1f, 0.15f, 0.9f);
        public static readonly Color ButtonColor = new Color(0.15f, 0.19f, 0.27f, 0.96f);
        public static readonly Color ButtonLocked = new Color(0.1f, 0.12f, 0.16f, 0.85f);
        public static readonly Color Accent = new Color(0.16f, 0.83f, 1f, 1f);
        public static readonly Color AccentDark = new Color(0.05f, 0.42f, 0.6f, 1f);
        public static readonly Color Danger = new Color(1f, 0.23f, 0.36f, 1f);
        public static readonly Color Success = new Color(0.18f, 0.8f, 0.44f, 1f);
        public static readonly Color Warning = new Color(1f, 0.72f, 0.1f, 1f);
        public static readonly Color TextColor = new Color(0.92f, 0.95f, 1f, 1f);
        public static readonly Color TextMuted = new Color(0.55f, 0.61f, 0.7f, 1f);
        public static readonly Color Inactive = new Color(0.32f, 0.36f, 0.43f, 1f);
    }

    /// <summary>
    /// uGUI elementlarini koddan yaratadi. Sprite'lar ham koddan chiziladi — tashqi rasm fayllari kerak emas.
    /// Keyingi bosqichda dizayner tayyorlagan prefablar/TextMeshPro bilan almashtiriladi.
    /// </summary>
    public static class UIFactory
    {
        private static Sprite roundedSprite;
        private static Sprite circleSprite;
        private static Sprite steeringWheelSprite;

        // ------------------------------------------------------------------ Asos

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && UZB_HAS_INPUT_SYSTEM
            UnityEngine.InputSystem.UI.InputSystemUIInputModule module =
                go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Bitta langar nuqtaga nisbatan joylashtirish (anchorMin = anchorMax).</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        // ------------------------------------------------------------------ Elementlar

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            return CreateImage(name, parent, color, RoundedSprite);
        }

        public static Text CreateLabel(string name, Transform parent, string content, int fontSize, TextAnchor alignment,
            Color color, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateRect(name, parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = GameFonts.Default;
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Button CreateButton(string name, Transform parent, string caption, int fontSize, Color background,
            UnityAction onClick, out Text captionLabel)
        {
            Image image = CreatePanel(name, parent, background);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            captionLabel = CreateLabel("Label", image.transform, caption, fontSize, TextAnchor.MiddleCenter,
                UITheme.TextColor, FontStyle.Bold);
            Stretch(captionLabel.rectTransform, 6f);

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        public static Button CreateButton(string name, Transform parent, string caption, int fontSize, Color background,
            UnityAction onClick)
        {
            Text ignored;
            return CreateButton(name, parent, caption, fontSize, background, onClick, out ignored);
        }

        /// <summary>Bosib turiladigan tugma (pedal, signal, chap/o‘ng).</summary>
        public static TouchButton CreateHoldButton(string name, Transform parent, string caption, int fontSize,
            Color background, out Text captionLabel)
        {
            Image image = CreatePanel(name, parent, background);
            TouchButton button = image.gameObject.AddComponent<TouchButton>();
            captionLabel = CreateLabel("Label", image.transform, caption, fontSize, TextAnchor.MiddleCenter,
                UITheme.TextColor, FontStyle.Bold);
            Stretch(captionLabel.rectTransform, 6f);
            return button;
        }

        public static Slider CreateSlider(string name, Transform parent, float min, float max, float value,
            UnityAction<float> onChanged)
        {
            RectTransform root = CreateRect(name, parent);
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image background = CreatePanel("Background", root, new Color(1f, 1f, 1f, 0.14f));
            background.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.64f);
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            RectTransform fillArea = CreateRect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.36f);
            fillArea.anchorMax = new Vector2(1f, 0.64f);
            fillArea.offsetMin = new Vector2(8f, 0f);
            fillArea.offsetMax = new Vector2(-8f, 0f);

            Image fill = CreatePanel("Fill", fillArea, UITheme.Accent);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = new Vector2(16f, 0f);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(20f, 0f);
            handleArea.offsetMax = new Vector2(-20f, 0f);

            Image handle = CreateImage("Handle", handleArea, Color.white, CircleSprite);
            handle.rectTransform.anchorMin = Vector2.zero;
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(40f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = Mathf.Clamp(value, min, max);

            Navigation navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;

            if (onChanged != null)
            {
                slider.onValueChanged.AddListener(onChanged);
            }

            return slider;
        }

        public static InputField CreateInputField(string name, Transform parent, string value, string placeholderText,
            int fontSize, int characterLimit, UnityAction<string> onEndEdit)
        {
            Image background = CreatePanel(name, parent, new Color(0.96f, 0.97f, 0.98f, 1f));
            background.raycastTarget = true;
            InputField input = background.gameObject.AddComponent<InputField>();

            Text text = CreateLabel("Text", background.transform, string.Empty, fontSize, TextAnchor.MiddleCenter,
                new Color(0.05f, 0.05f, 0.07f), FontStyle.Bold);
            text.supportRichText = false;
            Stretch(text.rectTransform, 8f);

            Text placeholder = CreateLabel("Placeholder", background.transform, placeholderText, fontSize,
                TextAnchor.MiddleCenter, new Color(0.45f, 0.47f, 0.52f), FontStyle.Italic);
            Stretch(placeholder.rectTransform, 8f);

            input.targetGraphic = background;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = characterLimit;
            input.lineType = InputField.LineType.SingleLine;
            input.onValidateInput = ToUpperAlphanumeric;
            input.text = value ?? string.Empty;

            if (onEndEdit != null)
            {
                input.onEndEdit.AddListener(onEndEdit);
            }

            return input;
        }

        private static char ToUpperAlphanumeric(string text, int charIndex, char addedChar)
        {
            char upper = char.ToUpperInvariant(addedChar);
            bool allowed = (upper >= 'A' && upper <= 'Z') || (upper >= '0' && upper <= '9');
            return allowed ? upper : '\0';
        }

        // ------------------------------------------------------------------ Koddan chizilgan sprite'lar

        public static Sprite RoundedSprite
        {
            get
            {
                if (roundedSprite == null)
                {
                    roundedSprite = BuildRounded(64, 18);
                }

                return roundedSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                {
                    circleSprite = BuildCircle(128);
                }

                return circleSprite;
            }
        }

        public static Sprite SteeringWheelSprite
        {
            get
            {
                if (steeringWheelSprite == null)
                {
                    steeringWheelSprite = BuildSteeringWheel(256);
                }

                return steeringWheelSprite;
            }
        }

        private static Sprite BuildRounded(int size, int radius)
        {
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float dx = Mathf.Max(0f, Mathf.Max(radius - px, px - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - py, py - (size - radius)));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return MakeSprite("UI_Rounded", size, pixels, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite BuildCircle(int size)
        {
            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return MakeSprite("UI_Circle", size, pixels, Vector4.zero);
        }

        // Rul chambaragi: tashqi halqa, markaziy gupchak va uchta kegay (chap, o‘ng, past).
        private static Sprite BuildSteeringWheel(int size)
        {
            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float outer = center - 2f;
            float inner = outer - size * 0.1f;
            float hub = size * 0.11f;
            float spokeHalfWidth = size * 0.035f;
            Vector2[] spokes = { new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, -1f) };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float ring = Mathf.Min(Mathf.Clamp01(outer - distance + 0.5f), Mathf.Clamp01(distance - inner + 0.5f));
                    float hubAlpha = Mathf.Clamp01(hub - distance + 0.5f);
                    float spokeAlpha = 0f;
                    for (int s = 0; s < spokes.Length; s++)
                    {
                        float along = dx * spokes[s].x + dy * spokes[s].y;
                        float across = Mathf.Abs(dx * spokes[s].y - dy * spokes[s].x);
                        if (along > 0f && along < inner + 1f)
                        {
                            spokeAlpha = Mathf.Max(spokeAlpha, Mathf.Clamp01(spokeHalfWidth - across + 0.5f));
                        }
                    }

                    float alpha = Mathf.Max(ring, Mathf.Max(hubAlpha, spokeAlpha));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return MakeSprite("UI_SteeringWheel", size, pixels, Vector4.zero);
        }

        private static Sprite MakeSprite(string name, int size, Color32[] pixels, Vector4 border)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
