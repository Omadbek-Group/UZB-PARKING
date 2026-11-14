using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UzbCarParking.Controls
{
    /// <summary>
    /// Bosib turiladigan sensorli tugma (gaz, tormoz, chap/o‘ng, signal).
    /// Barmoq tugmadan chiqib ketsa ham, ko‘tarilmaguncha bosilgan hisoblanadi — pedallar uchun qulay.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Range(0.3f, 1f)] public float pressedBrightness = 0.65f;

        public event Action Pressed;
        public event Action Released;

        private const int NoPointer = int.MinValue;

        private Graphic graphic;
        private Color normalColor;
        private int activePointerId = NoPointer;

        public bool IsPressed { get; private set; }

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                normalColor = graphic.color;
            }
        }

        /// <summary>Tugma rangi tashqaridan o‘zgartirilganda (masalan, holat ko‘rsatkichi) chaqiriladi.</summary>
        public void SetBaseColor(Color color)
        {
            normalColor = color;
            ApplyVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed)
            {
                return;
            }

            IsPressed = true;
            activePointerId = eventData.pointerId;
            ApplyVisual();
            if (Pressed != null)
            {
                Pressed.Invoke();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsPressed || eventData.pointerId != activePointerId)
            {
                return;
            }

            Release();
        }

        private void OnDisable()
        {
            if (IsPressed)
            {
                Release();
            }
        }

        private void Release()
        {
            IsPressed = false;
            activePointerId = NoPointer;
            ApplyVisual();
            if (Released != null)
            {
                Released.Invoke();
            }
        }

        private void ApplyVisual()
        {
            if (graphic == null)
            {
                return;
            }

            if (IsPressed)
            {
                Color pressed = normalColor * pressedBrightness;
                pressed.a = normalColor.a;
                graphic.color = pressed;
            }
            else
            {
                graphic.color = normalColor;
            }
        }
    }
}
