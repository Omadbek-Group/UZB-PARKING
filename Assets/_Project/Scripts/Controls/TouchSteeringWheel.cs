using UnityEngine;
using UnityEngine.EventSystems;

namespace UzbCarParking.Controls
{
    /// <summary>
    /// Sensorli rul chambaragi: barmoq bilan aylantiriladi, qo‘yib yuborilganda markazga qaytadi.
    /// Value: -1 (to‘liq chap) .. 1 (to‘liq o‘ng).
    /// </summary>
    public class TouchSteeringWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("Bir tomonga to‘liq burilish uchun chambarak necha gradus aylanadi.")]
        public float maxWheelAngle = 270f;
        [Tooltip("Qo‘yib yuborilganda markazga qaytish tezligi (gradus/soniya).")]
        public float returnSpeed = 600f;
        [Range(0.5f, 2f)] public float sensitivity = 1f;
        public RectTransform wheelGraphic;

        private const int NoPointer = int.MinValue;

        private RectTransform rect;
        private float wheelAngle;
        private bool held;
        private int activePointerId = NoPointer;
        private Vector2 lastDirection;

        public float Value
        {
            get { return maxWheelAngle > 0.01f ? Mathf.Clamp(wheelAngle / maxWheelAngle, -1f, 1f) : 0f; }
        }

        public bool IsHeld
        {
            get { return held; }
        }

        private void Awake()
        {
            rect = (RectTransform)transform;
            if (wheelGraphic == null)
            {
                wheelGraphic = rect;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (held)
            {
                return;
            }

            held = true;
            activePointerId = eventData.pointerId;
            lastDirection = LocalDirection(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!held || eventData.pointerId != activePointerId)
            {
                return;
            }

            Vector2 direction = LocalDirection(eventData);
            if (direction.sqrMagnitude < 4f || lastDirection.sqrMagnitude < 4f)
            {
                lastDirection = direction;
                return;
            }

            // SignedAngle: soat miliga teskari — musbat. O‘ngga burish uchun ishorasi teskari olinadi.
            float delta = Vector2.SignedAngle(lastDirection, direction);
            wheelAngle = Mathf.Clamp(wheelAngle - delta * sensitivity, -maxWheelAngle, maxWheelAngle);
            lastDirection = direction;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            held = false;
            activePointerId = NoPointer;
        }

        private void OnDisable()
        {
            held = false;
            activePointerId = NoPointer;
            wheelAngle = 0f;
        }

        private void Update()
        {
            if (!held)
            {
                wheelAngle = Mathf.MoveTowards(wheelAngle, 0f, returnSpeed * Time.unscaledDeltaTime);
            }

            if (wheelGraphic != null)
            {
                wheelGraphic.localEulerAngles = new Vector3(0f, 0f, -wheelAngle);
            }
        }

        private Vector2 LocalDirection(PointerEventData eventData)
        {
            // Chambarak grafigi aylansa ham hisob to‘g‘ri bo‘lishi uchun ota RectTransform ishlatiladi.
            RectTransform reference = rect.parent as RectTransform;
            if (reference == null)
            {
                reference = rect;
            }

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, eventData.position,
                eventData.pressEventCamera, out local);
            Vector2 center = reference == rect ? Vector2.zero : (Vector2)rect.localPosition;
            return local - center;
        }
    }
}
