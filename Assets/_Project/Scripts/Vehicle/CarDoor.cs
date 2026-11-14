using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UzbCarParking.Vehicle
{
    /// <summary>
    /// Mashina eshiklari. E tugmasi bilan ochiladi/yopiladi.
    /// Eshik origin'i menteshа (old chet, vertikal). Chap eshik chapga, o‘ng eshik o‘ngga ochiladi.
    /// Eshiklar bo‘sh qolsa, "Door_FL/FR/RL/RR" nomli bolalar avtomatik topiladi.
    /// </summary>
    public class CarDoor : MonoBehaviour
    {
        public Transform[] doors;
        [Tooltip("Ochilganda burchak (daraja)")]
        public float openAngle = 60f;
        [Tooltip("Burilish tezligi (daraja/s)")]
        public float speed = 120f;

        private static readonly string[] DoorNames = { "Door_FL", "Door_FR", "Door_RL", "Door_RR" };
        private float[] angles;
        private bool isOpen;

        private void Awake()
        {
            if (doors == null || doors.Length == 0)
            {
                doors = new Transform[DoorNames.Length];
                for (int i = 0; i < DoorNames.Length; i++)
                {
                    doors[i] = FindDeep(transform, DoorNames[i]);
                }
            }
            angles = new float[doors.Length];
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                isOpen = !isOpen;
            }

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;
                float target = isOpen ? openAngle : 0f;
                angles[i] = Mathf.MoveTowards(angles[i], target, speed * Time.deltaTime);
                // Chap tomondagi eshik (x<0) musbat burchakka, o‘ngdagisi manfiyga ochiladi
                float sign = doors[i].localPosition.x < 0f ? 1f : -1f;
                doors[i].localRotation = Quaternion.Euler(0f, sign * angles[i], 0f);
            }
        }

        private static bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
