using System;
using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Avtodrom
{
    /// <summary>
    /// Mashinaga qo‘yiladi: g‘ildiraklarning yerga tegish nuqtalari chiziq ustiga tushganini aniqlaydi.
    /// Har bir chiziq uchun hodisa faqat bir marta — chiziqqa kirish paytida — chiqadi.
    /// </summary>
    [DisallowMultipleComponent]
    public class LineCrossDetector : MonoBehaviour
    {
        public event Action<BoundaryLine> LineTouched;

        private readonly List<Vector3> contactPoints = new List<Vector3>(4);
        private HashSet<BoundaryLine> touching = new HashSet<BoundaryLine>();
        private HashSet<BoundaryLine> previous = new HashSet<BoundaryLine>();
        private VehicleController vehicle;

        public void Init(VehicleController vehicleController)
        {
            vehicle = vehicleController;
        }

        private void FixedUpdate()
        {
            if (vehicle == null || vehicle.Config == null)
            {
                return;
            }

            HashSet<BoundaryLine> swap = previous;
            previous = touching;
            touching = swap;
            touching.Clear();

            vehicle.GetWheelContactPoints(contactPoints);
            float tolerance = vehicle.Config.wheelWidth * 0.5f;
            List<BoundaryLine> lines = BoundaryLine.All;

            for (int p = 0; p < contactPoints.Count; p++)
            {
                for (int l = 0; l < lines.Count; l++)
                {
                    BoundaryLine line = lines[l];
                    if (line != null && line.ContainsPoint(contactPoints[p], tolerance))
                    {
                        touching.Add(line);
                    }
                }
            }

            if (LineTouched == null)
            {
                return;
            }

            foreach (BoundaryLine line in touching)
            {
                if (!previous.Contains(line))
                {
                    LineTouched.Invoke(line);
                }
            }
        }
    }
}
