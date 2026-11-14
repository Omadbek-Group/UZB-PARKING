using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Bosish mumkin bo‘lmagan chiziq. exerciseId bo‘sh bo‘lmasa, faqat shu mashq bajarilayotganda jarimalanadi.
    /// Lokal Z — chiziq uzunligi, lokal X — eni.
    /// </summary>
    public class BoundaryLine : MonoBehaviour
    {
        public static readonly List<BoundaryLine> All = new List<BoundaryLine>();

        public Vector3 size = new Vector3(0.12f, 0.6f, 5f);
        public string exerciseId = string.Empty;

        public void Configure(Vector3 lineSize, string ownerExerciseId)
        {
            size = lineSize;
            exerciseId = ownerExerciseId ?? string.Empty;
        }

        private void OnEnable()
        {
            if (!All.Contains(this))
            {
                All.Add(this);
            }
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        /// <param name="sideTolerance">Shina enining yarmi — shina cheti chiziqqa tegsa ham hisoblanadi.</param>
        public bool ContainsPoint(Vector3 point, float sideTolerance)
        {
            Vector3 half = new Vector3(size.x * 0.5f + sideTolerance, size.y * 0.5f, size.z * 0.5f + sideTolerance);
            return ZoneGeometry.ContainsPoint(transform.position, transform.rotation, half, point);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
