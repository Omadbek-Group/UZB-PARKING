using UnityEngine;
#if ENABLE_INPUT_SYSTEM && UZB_HAS_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UzbCarParking.Controls
{
    public enum KeyAction
    {
        Engine,
        Handbrake,
        Seatbelt,
        Lights,
        LeftSignal,
        RightSignal,
        Hazard,
        Horn,
        Camera,
        ShiftUp,
        ShiftDown,
        Pause
    }

    /// <summary>
    /// Editor va Windows'da sinash uchun klaviatura. Yangi Input System ham, eski Input Manager ham
    /// qo‘llab-quvvatlanadi (Player Settings ▸ Active Input Handling qaysi bo‘lishidan qat'i nazar).
    ///
    /// W/↑ gaz · S/↓ tormoz · A/D yoki ←/→ rul · Space qo‘l tormozi · I dvigatel · B kamar ·
    /// L faralar · Q/E chap/o‘ng signal · Z avariya · H signal · C kamera · Shift/Ctrl uzatma · Esc pauza
    /// </summary>
    public static class KeyboardInput
    {
#if ENABLE_INPUT_SYSTEM && UZB_HAS_INPUT_SYSTEM
        private static bool IsDown(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].isPressed;
        }

        private static Key Map(KeyAction action)
        {
            switch (action)
            {
                case KeyAction.Engine: return Key.I;
                case KeyAction.Handbrake: return Key.Space;
                case KeyAction.Seatbelt: return Key.B;
                case KeyAction.Lights: return Key.L;
                case KeyAction.LeftSignal: return Key.Q;
                case KeyAction.RightSignal: return Key.E;
                case KeyAction.Hazard: return Key.Z;
                case KeyAction.Horn: return Key.H;
                case KeyAction.Camera: return Key.C;
                case KeyAction.ShiftUp: return Key.LeftShift;
                case KeyAction.ShiftDown: return Key.LeftCtrl;
                default: return Key.Escape;
            }
        }

        public static float Throttle()
        {
            return IsDown(Key.W) || IsDown(Key.UpArrow) ? 1f : 0f;
        }

        public static float Brake()
        {
            return IsDown(Key.S) || IsDown(Key.DownArrow) ? 1f : 0f;
        }

        public static float Steer()
        {
            float value = 0f;
            if (IsDown(Key.A) || IsDown(Key.LeftArrow)) value -= 1f;
            if (IsDown(Key.D) || IsDown(Key.RightArrow)) value += 1f;
            return value;
        }

        public static bool Held(KeyAction action)
        {
            return IsDown(Map(action));
        }

        public static bool Pressed(KeyAction action)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[Map(action)].wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private static KeyCode Map(KeyAction action)
        {
            switch (action)
            {
                case KeyAction.Engine: return KeyCode.I;
                case KeyAction.Handbrake: return KeyCode.Space;
                case KeyAction.Seatbelt: return KeyCode.B;
                case KeyAction.Lights: return KeyCode.L;
                case KeyAction.LeftSignal: return KeyCode.Q;
                case KeyAction.RightSignal: return KeyCode.E;
                case KeyAction.Hazard: return KeyCode.Z;
                case KeyAction.Horn: return KeyCode.H;
                case KeyAction.Camera: return KeyCode.C;
                case KeyAction.ShiftUp: return KeyCode.LeftShift;
                case KeyAction.ShiftDown: return KeyCode.LeftControl;
                default: return KeyCode.Escape;
            }
        }

        public static float Throttle()
        {
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
        }

        public static float Brake()
        {
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;
        }

        public static float Steer()
        {
            float value = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) value -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) value += 1f;
            return value;
        }

        public static bool Held(KeyAction action)
        {
            return Input.GetKey(Map(action));
        }

        public static bool Pressed(KeyAction action)
        {
            return Input.GetKeyDown(Map(action));
        }
#else
        public static float Throttle() { return 0f; }
        public static float Brake() { return 0f; }
        public static float Steer() { return 0f; }
        public static bool Held(KeyAction action) { return false; }
        public static bool Pressed(KeyAction action) { return false; }
#endif
    }
}
