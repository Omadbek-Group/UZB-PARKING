using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// To‘g‘ri burchakli hudud: mashq boshlanish joyi, to‘xtash joyi, parkovka katagi.
    /// Hudud obyektning pozitsiyasi va burilishidan olinadi; o‘lcham — "size" maydonida (scale hisobga olinmaydi).
    /// </summary>
    public class ParkingZone : MonoBehaviour
    {
        public Vector3 size = new Vector3(2.5f, 3f, 6.5f);
        public Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.9f);

        public void Configure(Vector3 zoneSize)
        {
            size = zoneSize;
        }

        public bool ContainsPoint(Vector3 point)
        {
            return ZoneGeometry.ContainsPoint(transform.position, transform.rotation, size * 0.5f, point);
        }

        public bool ContainsAll(IList<Vector3> points)
        {
            return ZoneGeometry.ContainsAll(transform.position, transform.rotation, size * 0.5f, points);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
