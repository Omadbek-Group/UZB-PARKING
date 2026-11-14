using NUnit.Framework;
using UnityEngine;
using UzbCarParking.Avtodrom;
using UzbCarParking.Garage;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Tests
{
    public class TransmissionLogicTests
    {
        private const float ShiftUp = 4800f;
        private const float ShiftDown = 1800f;

        [Test]
        public void ShiftsUpAboveShiftUpRpm()
        {
            Assert.AreEqual(1, TransmissionLogic.AutoSelectGear(0, 5000f, ShiftUp, ShiftDown, 5, 1f));
        }

        [Test]
        public void DoesNotShiftUpInTopGear()
        {
            Assert.AreEqual(4, TransmissionLogic.AutoSelectGear(4, 6000f, ShiftUp, ShiftDown, 5, 1f));
        }

        [Test]
        public void ShiftsDownBelowShiftDownRpm()
        {
            Assert.AreEqual(1, TransmissionLogic.AutoSelectGear(2, 1200f, ShiftUp, ShiftDown, 5, 0f));
        }

        [Test]
        public void DoesNotShiftDownInFirstGear()
        {
            Assert.AreEqual(0, TransmissionLogic.AutoSelectGear(0, 300f, ShiftUp, ShiftDown, 5, 0f));
        }

        [Test]
        public void HoldsGearInsideBand()
        {
            Assert.AreEqual(2, TransmissionLogic.AutoSelectGear(2, 3200f, ShiftUp, ShiftDown, 5, 0.5f));
        }

        [Test]
        public void FullThrottleDownshiftsEarlier()
        {
            // 2200 ayl/min: gazsiz — uzatma saqlanadi, to‘liq gazda — pastga o‘tadi.
            Assert.AreEqual(2, TransmissionLogic.AutoSelectGear(2, 2200f, ShiftUp, ShiftDown, 5, 0f));
            Assert.AreEqual(1, TransmissionLogic.AutoSelectGear(2, 2200f, ShiftUp, ShiftDown, 5, 1f));
        }

        [Test]
        public void NoGearHuntingAfterUpshift()
        {
            // 1→2 o‘tgandan keyin aylanish 4800 × (2.05 / 3.73) ≈ 2638 ga tushadi; qayta pastga o‘tmasligi kerak.
            float rpmAfterShift = 4800f * (2.05f / 3.73f);
            Assert.AreEqual(1, TransmissionLogic.AutoSelectGear(1, rpmAfterShift, ShiftUp, ShiftDown, 5, 1f));
        }

        [Test]
        public void WheelRpmConvertsToEngineRpm()
        {
            Assert.AreEqual(1566.6f, TransmissionLogic.WheelRpmToEngineRpm(100f, 3.73f, 4.2f), 0.1f);
            Assert.AreEqual(1470f, TransmissionLogic.WheelRpmToEngineRpm(-100f, 3.5f, 4.2f), 0.1f);
        }
    }

    public class ZoneGeometryTests
    {
        private static readonly Vector3 HalfExtents = new Vector3(1.25f, 1.5f, 3.25f);

        [Test]
        public void PointInsideAxisAlignedZone()
        {
            Assert.IsTrue(ZoneGeometry.ContainsPoint(Vector3.zero, Quaternion.identity, HalfExtents, new Vector3(1f, 0f, 3f)));
        }

        [Test]
        public void PointOutsideAxisAlignedZone()
        {
            Assert.IsFalse(ZoneGeometry.ContainsPoint(Vector3.zero, Quaternion.identity, HalfExtents, new Vector3(1.4f, 0f, 0f)));
            Assert.IsFalse(ZoneGeometry.ContainsPoint(Vector3.zero, Quaternion.identity, HalfExtents, new Vector3(0f, 0f, 3.4f)));
        }

        [Test]
        public void RotatedZoneUsesLocalAxes()
        {
            // 90° burilgan zonada uzun tomon X bo‘ylab yotadi.
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
            Assert.IsTrue(ZoneGeometry.ContainsPoint(Vector3.zero, rotation, HalfExtents, new Vector3(3f, 0f, 0f)));
            Assert.IsFalse(ZoneGeometry.ContainsPoint(Vector3.zero, rotation, HalfExtents, new Vector3(0f, 0f, 3f)));
        }

        [Test]
        public void CarFootprintFitsInsideBay()
        {
            Vector3 center = new Vector3(28f, 1f, 7f);
            Vector3[] corners =
            {
                new Vector3(27.2f, 0.3f, 8.8f), new Vector3(28.8f, 0.3f, 8.8f),
                new Vector3(27.2f, 0.3f, 5.2f), new Vector3(28.8f, 0.3f, 5.2f)
            };

            Assert.IsTrue(ZoneGeometry.ContainsAll(center, Quaternion.identity, HalfExtents, corners));

            corners[1] = new Vector3(29.4f, 0.3f, 8.8f);
            Assert.IsFalse(ZoneGeometry.ContainsAll(center, Quaternion.identity, HalfExtents, corners));
        }

        [Test]
        public void EmptyPointListIsNotContained()
        {
            Assert.IsFalse(ZoneGeometry.ContainsAll(Vector3.zero, Quaternion.identity, HalfExtents, new Vector3[0]));
        }

        [Test]
        public void HeadingErrorIgnoresVerticalComponent()
        {
            Assert.AreEqual(0f, ZoneGeometry.HeadingErrorDegrees(new Vector3(0f, 0.5f, 1f), Vector3.forward), 0.01f);
            Assert.AreEqual(90f, ZoneGeometry.HeadingErrorDegrees(Vector3.right, Vector3.forward), 0.01f);
            Assert.AreEqual(180f, ZoneGeometry.HeadingErrorDegrees(Vector3.back, Vector3.forward), 0.01f);
        }
    }

    public class PlateNumberTests
    {
        [Test]
        public void AcceptsIndividualAndLegalFormats()
        {
            Assert.IsTrue(PlateNumber.IsValidNumber("A777AA"));
            Assert.IsTrue(PlateNumber.IsValidNumber("777AAA"));
        }

        [Test]
        public void NormalizesCaseAndSeparators()
        {
            Assert.AreEqual("A123BC", PlateNumber.Normalize(" a 123-bc "));
            Assert.IsTrue(PlateNumber.IsValidNumber("a 123 bc"));
        }

        [Test]
        public void RejectsInvalidNumbers()
        {
            Assert.IsFalse(PlateNumber.IsValidNumber(null));
            Assert.IsFalse(PlateNumber.IsValidNumber(string.Empty));
            Assert.IsFalse(PlateNumber.IsValidNumber("AAAAAA"));
            Assert.IsFalse(PlateNumber.IsValidNumber("123456"));
            Assert.IsFalse(PlateNumber.IsValidNumber("A77AA"));
            Assert.IsFalse(PlateNumber.IsValidNumber("A7777AA"));
        }

        [Test]
        public void ValidatesRegions()
        {
            Assert.IsTrue(PlateNumber.IsValidRegion("01"));
            Assert.IsTrue(PlateNumber.IsValidRegion("95"));
            Assert.IsFalse(PlateNumber.IsValidRegion("02"));
            Assert.IsFalse(PlateNumber.IsValidRegion(null));
            Assert.AreEqual(PlateNumber.Regions.Length, PlateNumber.RegionNames.Length);
        }

        [Test]
        public void FormatsForDisplay()
        {
            Assert.AreEqual("01 A 777 AA", PlateNumber.Format("01", "a777aa"));
            Assert.AreEqual("30 777 AAA", PlateNumber.Format("30", "777AAA"));
            Assert.AreEqual("01 A 777 AA", PlateNumber.Format("xx", "A777AA"));
        }
    }
}
