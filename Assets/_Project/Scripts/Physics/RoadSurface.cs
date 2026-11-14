using UnityEngine;

namespace UzbCarParking.VehiclePhysics
{
    /// <summary>
    /// Yo‘l qoplamasi. Kollayderi bor obyektga qo‘yiladi; VehicleController g‘ildirak ostidagi
    /// qoplamaga qarab shina ishqalanishini ko‘paytiradi yoki kamaytiradi.
    /// Misol: asfalt 1.0, beton 0.95, ho‘l asfalt 0.7, shag‘al 0.6, muz 0.25.
    /// </summary>
    public class RoadSurface : MonoBehaviour
    {
        public string surfaceName = "Asfalt";
        [Range(0.1f, 1.5f)] public float gripMultiplier = 1f;

        public void Configure(string displayName, float grip)
        {
            surfaceName = displayName;
            gripMultiplier = Mathf.Clamp(grip, 0.1f, 1.5f);
        }
    }
}
