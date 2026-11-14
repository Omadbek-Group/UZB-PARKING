# Mashina prefabini yaratish yo‘riqnomasi

Maqsad: yangi 3D modelni (masalan, Chevrolet Spark) kodni o‘zgartirmasdan mashinaga aylantirish.

## 1. Model tayyorligi
- Format: FBX yoki GLB (Unity 6 ikkalasini ham import qiladi).
- Birlik: metr. Mashina pivoti yerda, old tomoni +Z ga qaragan bo‘lsin.
- **Litsenziya:** model muallif huquqi bilan himoyalangan bo‘lishi mumkin. Faqat o‘zingiz yaratgan yoki
  qonuniy litsenziyalangan modelni ishlating. Uchinchi tomon modelini ishlatishdan oldin litsenziyasini tekshiring.

## 2. Ierarxiya (tavsiya etilgan)
```
Vehicle_<id>                 ← Rigidbody, VehicleController, VehicleLights, VehicleCustomization,
│                              VehicleDamage, VehicleAudio, LineCrossDetector (ixtiyoriy)
├─ Body                      ← kuzov mesh (renderer: paint slotlari)
├─ Rims                      ← disklar (rim slot)
├─ Wheels/
│  ├─ Wheel_FL  (visual)
│  ├─ Wheel_FR
│  ├─ Wheel_RL
│  └─ Wheel_RR
├─ WheelColliders/
│  ├─ WC_FL  (WheelCollider, steer = true)
│  ├─ WC_FR  (WheelCollider, steer = true)
│  ├─ WC_RL  (WheelCollider)
│  └─ WC_RR  (WheelCollider)
├─ SteeringWheel             ← rul chambaragi (Z o‘qi rul ustuni bo‘ylab)
├─ Lights/
│  ├─ Headlights  (Renderer'lar + Light'lar)
│  ├─ BrakeLights, ReverseLights
│  └─ Indicators_L, Indicators_R
└─ Plates/                   ← TextMesh'lar (davlat raqami, old va orqa)
```

## 3. Inspector sozlamalari
1. **VehicleConfig** asset yarating: `Create ▸ UZB Car Parking ▸ Vehicle Config`. Parametrlarni shu yerda sozlang.
2. Prefab'dagi `VehicleController` ga `config` ni biriktiring. `wheels` massivida 4 ta `WheelSetup` bo‘lsin
   (`collider`, `visual`, `isFront`, `steer`).
3. `steeringWheelVisual` ga rul chambaragi transformini bering.
4. `VehicleLights` da `headlights`, `brakeLights`, `reverseLights`, `leftIndicators`, `rightIndicators`
   guruhlarini to‘ldiring (renderers + lights).
5. `VehicleCustomization` da `paintRenderers`, `rimRenderers`, `plateTexts` ni bering.
6. Rigidbody: `Interpolate`, `Continuous` (kod o‘zi ham o‘rnatadi).

## 4. Sahnaga ulash
- `VehicleConfig.vehiclePrefab` ga prefabni biriktiring. `VehicleSpawner` shu prefabdan foydalanadi.
- Bo‘sh qoldirilsa — primitive placeholder mashina yaratiladi (`PlaceholderVehicleFactory`).

## 5. Tekshirish
- Play rejimida Console'da `[UZB]` xatolari bo‘lmasligi kerak.
- `VehicleController` da "VehicleConfig biriktirilmagan" yoki "prefabida VehicleController yo‘q" xabari chiqsa,
  yuqoridagi 2–3 bandlarni qayta tekshiring.
- Gaz/tormoz bosilganda g‘ildiraklar aylanishi va tezlik ko‘rsatkichi o‘zgarishi kerak.

## 6. Yangi mashina qo‘shish (o‘zgarishsiz)
1. Model + prefab yarating (yuqoridagi tuzilma).
2. Yangi `VehicleConfig` asset yarating va `vehiclePrefab` ga prefabni bering.
3. Bosh menyu / garajda shu config'ni tanlang (keyingi bosqichda ro‘yxat qo‘shiladi).
