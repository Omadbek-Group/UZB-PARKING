using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UzbCarParking.Vehicle;

namespace UzbCarParking.Tests
{
    /// <summary>Runtime (Play) tekshiruvi: Avtodrom sahnasi, mashina va eshiklar ishlaydimi.</summary>
    public class WorldPlayModeTests
    {
        private static CarDoor LoadShowroomDoor(out GameObject car)
        {
            car = GameObject.Find("Showroom_Car");
            return car != null ? car.GetComponent<CarDoor>() : null;
        }

        [UnityTest]
        public IEnumerator Avtodrom_HasShowroomCar_WithFourDoors()
        {
            yield return SceneManager.LoadSceneAsync("Avtodrom");
            yield return null;

            CarDoor door = LoadShowroomDoor(out GameObject car);
            Assert.IsNotNull(car, "Showroom_Car sahnada topilmadi");
            Assert.IsNotNull(door, "CarDoor skripti mashinada yo'q");
            Assert.AreEqual(4, door.doors.Length, "4 ta eshik kutilgan");
            foreach (Transform d in door.doors)
            {
                Assert.IsNotNull(d, "eshik topilmadi (Door_FL/FR/RL/RR nomi bilan)");
            }
        }

        [UnityTest]
        public IEnumerator Doors_Open_WhenToggled()
        {
            yield return SceneManager.LoadSceneAsync("Avtodrom");
            yield return null;

            CarDoor door = LoadShowroomDoor(out GameObject car);
            Assert.IsNotNull(door);

            // Eshikni ochish holatini majburan yoqamiz (E tugmasi testda bosilmaydi)
            FieldInfo open = typeof(CarDoor).GetField("isOpen", BindingFlags.NonPublic | BindingFlags.Instance);
            open.SetValue(door, true);

            yield return new WaitForSeconds(1.5f);

            foreach (Transform d in door.doors)
            {
                float angle = Quaternion.Angle(Quaternion.identity, d.localRotation);
                Assert.Greater(angle, 30f, d.name + " ochilmadi (burchak " + angle + ")");
            }
        }
    }
}
