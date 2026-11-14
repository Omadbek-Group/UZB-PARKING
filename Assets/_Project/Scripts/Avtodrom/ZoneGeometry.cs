using System.Collections.Generic;
using UnityEngine;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Hudud (yo‘naltirilgan quti) geometriyasi — sof matematika, unit test qilinadi.
    /// Trigger kollayderlar ishlatilmaydi: WheelCollider'lar trigger'larga "chiqib qolishi" mumkin.
    /// </summary>
    public static class ZoneGeometry
    {
        public static bool ContainsPoint(Vector3 center, Quaternion rotation, Vector3 halfExtents, Vector3 point)
        {
            Vector3 local = Quaternion.Inverse(rotation) * (point - center);
            return Mathf.Abs(local.x) <= halfExtents.x &&
                   Mathf.Abs(local.y) <= halfExtents.y &&
                   Mathf.Abs(local.z) <= halfExtents.z;
        }

        public static bool ContainsAll(Vector3 center, Quaternion rotation, Vector3 halfExtents, IList<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (!ContainsPoint(center, rotation, halfExtents, points[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Gorizontal tekislikdagi ikki yo‘nalish orasidagi burchak (0..180 gradus).</summary>
        public static float HeadingErrorDegrees(Vector3 forward, Vector3 desired)
        {
            forward.y = 0f;
            desired.y = 0f;
            if (forward.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f)
            {
                return 0f;
            }

            return Vector3.Angle(forward, desired);
        }
    }
}
