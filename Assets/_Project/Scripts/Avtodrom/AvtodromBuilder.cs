using System.Collections.Generic;
using UnityEngine;
using UzbCarParking.Core;
using UzbCarParking.VehiclePhysics;

namespace UzbCarParking.Avtodrom
{
    /// <summary>Qurilgan avtodrom haqida ma'lumot.</summary>
    public sealed class AvtodromLayout
    {
        public Transform Root;
        public ExerciseBeacon Beacon;
        public Vector3 DefaultSpawnPosition;
        public Quaternion DefaultSpawnRotation = Quaternion.identity;
        public readonly Dictionary<string, ExerciseBase> Exercises = new Dictionary<string, ExerciseBase>();
        public readonly Dictionary<string, Pose> SpawnPoses = new Dictionary<string, Pose>();
    }

    /// <summary>
    /// Avtodrom sahnasini koddan quradi (primitive placeholder geometriya):
    /// maydon, start joyi, estakada, parallel parkovka va boks.
    /// O‘lchamlar ExamConfig'dan olinadi. Haqiqiy 3D muhit tayyor bo‘lgach, shu klass o‘rniga
    /// tayyor sahna/prefab ishlatiladi — mashq komponentlari (ExerciseBase va h.k.) o‘zgarmaydi.
    /// </summary>
    public static class AvtodromBuilder
    {
        private const float LineWidth = 0.12f;
        private const float GroundHalfSize = 75f;

        public static AvtodromLayout Build(ExamConfig config)
        {
            AvtodromLayout layout = new AvtodromLayout();
            Transform root = new GameObject("Avtodrom").transform;
            layout.Root = root;

            Transform staticRoot = PrimitiveKit.Child(root, "Static");
            Transform dynamicRoot = PrimitiveKit.Child(root, "Dynamic");
            Transform exerciseRoot = PrimitiveKit.Child(root, "Exercises");

            BuildGround(staticRoot);
            BuildDecor(staticRoot, dynamicRoot);
            BuildStartArea(staticRoot, dynamicRoot, layout);
            BuildEstakada(staticRoot, dynamicRoot, exerciseRoot, config, layout);
            BuildParallelParking(staticRoot, dynamicRoot, exerciseRoot, config, layout);
            BuildBox(staticRoot, dynamicRoot, exerciseRoot, config, layout);
            layout.Beacon = BuildBeacon(root);

            // Qo‘zg‘almas geometriyani birlashtirish — mobil qurilmalarda draw call'larni kamaytiradi.
            StaticBatchingUtility.Combine(staticRoot.gameObject);
            return layout;
        }

        // ------------------------------------------------------------------ Maydon

        private static void BuildGround(Transform parent)
        {
            float size = GroundHalfSize * 2f;
            GameObject asphalt = PrimitiveKit.Solid(PrimitiveType.Cube, "Asphalt", parent,
                new Vector3(0f, -0.1f, 0f), new Vector3(size, 0.2f, size), RuntimeMaterials.Get(MaterialKind.Asphalt));
            asphalt.AddComponent<RoadSurface>().Configure("Asfalt", 1f);

            PrimitiveKit.Visual(PrimitiveType.Cube, "Grass", parent,
                new Vector3(0f, -0.13f, 0f), new Vector3(420f, 0.2f, 420f), RuntimeMaterials.Get(MaterialKind.Grass));

            // Maydon atrofidagi beton to‘siq.
            Material concrete = RuntimeMaterials.Get(MaterialKind.Concrete);
            float edge = GroundHalfSize - 1.5f;
            float span = edge * 2f + 0.6f;
            PrimitiveKit.Solid(PrimitiveType.Cube, "Barrier_N", parent, new Vector3(0f, 0.4f, edge), new Vector3(span, 0.8f, 0.6f), concrete);
            PrimitiveKit.Solid(PrimitiveType.Cube, "Barrier_S", parent, new Vector3(0f, 0.4f, -edge), new Vector3(span, 0.8f, 0.6f), concrete);
            PrimitiveKit.Solid(PrimitiveType.Cube, "Barrier_E", parent, new Vector3(edge, 0.4f, 0f), new Vector3(0.6f, 0.8f, span), concrete);
            PrimitiveKit.Solid(PrimitiveType.Cube, "Barrier_W", parent, new Vector3(-edge, 0.4f, 0f), new Vector3(0.6f, 0.8f, span), concrete);
        }

        private static void BuildDecor(Transform staticRoot, Transform dynamicRoot)
        {
            Material bark = RuntimeMaterials.Get(MaterialKind.Bark);
            Material foliage = RuntimeMaterials.Get(MaterialKind.Foliage);
            Material building = RuntimeMaterials.Get(MaterialKind.Building);
            Material concrete = RuntimeMaterials.Get(MaterialKind.Concrete);
            Material plastic = RuntimeMaterials.Get(MaterialKind.Plastic);

            // Maydon tashqarisidagi daraxtlar.
            const int treeCount = 28;
            for (int i = 0; i < treeCount; i++)
            {
                float angle = i * Mathf.PI * 2f / treeCount;
                float radius = GroundHalfSize + 12f + (i % 3) * 5f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius * 1.25f, 0f, Mathf.Sin(angle) * radius * 1.25f);
                float scale = 0.85f + (i % 4) * 0.12f;
                PrimitiveKit.Visual(PrimitiveType.Cylinder, "TreeTrunk", staticRoot,
                    position + new Vector3(0f, 1.4f * scale, 0f), new Vector3(0.4f, 1.4f, 0.4f) * scale, bark);
                PrimitiveKit.Visual(PrimitiveType.Sphere, "TreeCrown", staticRoot,
                    position + new Vector3(0f, 4.2f * scale, 0f), new Vector3(4f, 4.4f, 4f) * scale, foliage);
            }

            // Ma'muriy bino.
            Vector3 office = new Vector3(-48f, 0f, -58f);
            PrimitiveKit.Solid(PrimitiveType.Cube, "Office", staticRoot, office + new Vector3(0f, 2.2f, 0f), new Vector3(16f, 4.4f, 8f), building);
            PrimitiveKit.Visual(PrimitiveType.Cube, "OfficeRoof", staticRoot, office + new Vector3(0f, 4.55f, 0f), new Vector3(17f, 0.3f, 9f), concrete);
            PrimitiveKit.Visual(PrimitiveType.Cube, "OfficeSign", staticRoot, office + new Vector3(0f, 3.4f, 4.05f), new Vector3(9f, 1.1f, 0.1f), plastic);
            PrimitiveKit.Label("AVTODROM", dynamicRoot, office + new Vector3(0f, 3.4f, 4.12f), Quaternion.Euler(0f, 180f, 0f), 0.11f, Color.white);

            // Yoritish ustunlari (hozircha faqat ko‘rinish).
            float pole = GroundHalfSize - 6f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 basePosition = new Vector3(sx * pole, 0f, sz * pole);
                    PrimitiveKit.Visual(PrimitiveType.Cylinder, "LightPole", staticRoot,
                        basePosition + new Vector3(0f, 4f, 0f), new Vector3(0.2f, 4f, 0.2f), concrete);
                    PrimitiveKit.Visual(PrimitiveType.Cube, "LightHead", staticRoot,
                        basePosition + new Vector3(-sx * 0.6f, 8f, -sz * 0.6f), new Vector3(1.4f, 0.15f, 1.4f), plastic);
                }
            }
        }

        private static void BuildStartArea(Transform staticRoot, Transform dynamicRoot, AvtodromLayout layout)
        {
            Vector3 start = new Vector3(0f, 0f, -52f);
            layout.DefaultSpawnPosition = start + Vector3.up * 0.25f;
            layout.DefaultSpawnRotation = Quaternion.identity;

            // Start katagi (jarimasiz, faqat belgi).
            PaintLine(staticRoot, start + new Vector3(-1.6f, 0f, -3f), start + new Vector3(-1.6f, 0f, 3f), MaterialKind.LineWhite, null);
            PaintLine(staticRoot, start + new Vector3(1.6f, 0f, -3f), start + new Vector3(1.6f, 0f, 3f), MaterialKind.LineWhite, null);
            PaintLine(staticRoot, start + new Vector3(-1.6f, 0f, -3f), start + new Vector3(1.6f, 0f, -3f), MaterialKind.LineWhite, null);
            GroundLabel("START", dynamicRoot, start + new Vector3(0f, 0f, 5.5f), 0.07f);
        }

        // ------------------------------------------------------------------ Estakada

        private static void BuildEstakada(Transform staticRoot, Transform dynamicRoot, Transform exerciseRoot,
            ExamConfig config, AvtodromLayout layout)
        {
            const float rampLength = 10f;
            const float rise = 1.2f;
            const float plateauLength = 6f;
            const float rampWidth = 3.6f;
            const float thickness = 0.3f;
            const float zStart = -20f;

            float zTop = zStart + rampLength;
            float zTopEnd = zTop + plateauLength;
            float zEnd = zTopEnd + rampLength;
            float angle = Mathf.Atan2(rise, rampLength) * Mathf.Rad2Deg;
            float slopeLength = Mathf.Sqrt(rampLength * rampLength + rise * rise);
            Material concrete = RuntimeMaterials.Get(MaterialKind.Concrete);

            // Ko‘tarilish.
            Quaternion upRotation = Quaternion.Euler(-angle, 0f, 0f);
            Vector3 upNormal = upRotation * Vector3.up;
            Vector3 upSurfaceMid = new Vector3(0f, rise * 0.5f, zStart + rampLength * 0.5f);
            GameObject rampUp = PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_RampUp", staticRoot,
                upSurfaceMid - upNormal * (thickness * 0.5f), new Vector3(rampWidth, thickness, slopeLength), concrete, upRotation);
            rampUp.AddComponent<RoadSurface>().Configure("Beton", 0.95f);

            // Tepa maydoncha.
            GameObject plateau = PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_Plateau", staticRoot,
                new Vector3(0f, rise * 0.5f, (zTop + zTopEnd) * 0.5f), new Vector3(rampWidth, rise, plateauLength), concrete);
            plateau.AddComponent<RoadSurface>().Configure("Beton", 0.95f);

            // Tushish.
            Quaternion downRotation = Quaternion.Euler(angle, 0f, 0f);
            Vector3 downNormal = downRotation * Vector3.up;
            Vector3 downSurfaceMid = new Vector3(0f, rise * 0.5f, zTopEnd + rampLength * 0.5f);
            GameObject rampDown = PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_RampDown", staticRoot,
                downSurfaceMid - downNormal * (thickness * 0.5f), new Vector3(rampWidth, thickness, slopeLength), concrete, downRotation);
            rampDown.AddComponent<RoadSurface>().Configure("Beton", 0.95f);

            // Yon to‘siqlar.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (rampWidth * 0.5f + 0.1f);
                PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_Rail", staticRoot,
                    upSurfaceMid + upNormal * 0.12f + Vector3.right * x, new Vector3(0.2f, 0.3f, slopeLength), concrete, upRotation);
                PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_Rail", staticRoot,
                    new Vector3(x, rise + 0.12f, (zTop + zTopEnd) * 0.5f), new Vector3(0.2f, 0.3f, plateauLength), concrete);
                PrimitiveKit.Solid(PrimitiveType.Cube, "Estakada_Rail", staticRoot,
                    downSurfaceMid + downNormal * 0.12f + Vector3.right * x, new Vector3(0.2f, 0.3f, slopeLength), concrete, downRotation);
            }

            // Stop-chiziq va to‘xtash hududi (ko‘tarilish ustida).
            float zoneLength = config.estakadaStopZoneLength;
            float zStopLine = zTop - 2f;
            float zZoneStart = zStopLine - zoneLength;
            float zZoneMid = zStopLine - zoneLength * 0.5f;

            Vector3 stopLinePoint = new Vector3(0f, SlopeHeight(zStopLine, zStart, rampLength, rise), zStopLine);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Estakada_StopLine", staticRoot,
                stopLinePoint + upNormal * 0.008f, new Vector3(rampWidth, 0.01f, 0.18f),
                RuntimeMaterials.Get(MaterialKind.LineWhite), upRotation);

            Vector3 zoneStartPoint = new Vector3(0f, SlopeHeight(zZoneStart, zStart, rampLength, rise), zZoneStart);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Estakada_ZoneLine", staticRoot,
                zoneStartPoint + upNormal * 0.008f, new Vector3(rampWidth, 0.01f, 0.1f),
                RuntimeMaterials.Get(MaterialKind.LineYellow), upRotation);

            Vector3 zoneMidPoint = new Vector3(0f, SlopeHeight(zZoneMid, zStart, rampLength, rise), zZoneMid);
            ParkingZone stopZone = CreateZone(exerciseRoot, "Estakada_StopZone",
                zoneMidPoint + upNormal * 0.6f, upRotation, new Vector3(rampWidth, 3f, zoneLength));

            // Yaqinlashish va chiqish yo‘laklari chiziqlari.
            float laneX = rampWidth * 0.5f + 0.6f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * laneX;
                PaintLine(staticRoot, new Vector3(x, 0f, zStart - 12f), new Vector3(x, 0f, zStart),
                    MaterialKind.LineWhite, GameConstants.ExerciseEstakada);
                PaintLine(staticRoot, new Vector3(x, 0f, zEnd), new Vector3(x, 0f, zEnd + 7f),
                    MaterialKind.LineWhite, GameConstants.ExerciseEstakada);
            }

            ParkingZone startZone = CreateZone(exerciseRoot, "Estakada_StartZone",
                new Vector3(0f, 1f, zStart - 8f), Quaternion.identity, new Vector3(laneX * 2f + 3f, 3f, 4f));
            ParkingZone exitZone = CreateZone(exerciseRoot, "Estakada_ExitZone",
                new Vector3(0f, 1f, zEnd + 6f), Quaternion.identity, new Vector3(laneX * 2f + 3f, 3f, 3f));

            SignBoard("ESTAKADA", staticRoot, dynamicRoot, new Vector3(-5f, 0f, zStart - 9f), 0f);

            EstakadaExercise exercise = new GameObject("Exercise_Estakada").AddComponent<EstakadaExercise>();
            exercise.transform.SetParent(exerciseRoot, false);
            exercise.Configure(startZone, stopZone, exitZone, Vector3.forward);

            layout.Exercises[GameConstants.ExerciseEstakada] = exercise;
            layout.SpawnPoses[GameConstants.ExerciseEstakada] =
                new Pose(layout.DefaultSpawnPosition, layout.DefaultSpawnRotation);
        }

        private static float SlopeHeight(float z, float zStart, float rampLength, float rise)
        {
            return Mathf.Clamp01((z - zStart) / rampLength) * rise;
        }

        // ------------------------------------------------------------------ Parallel parkovka

        private static void BuildParallelParking(Transform staticRoot, Transform dynamicRoot, Transform exerciseRoot,
            ExamConfig config, AvtodromLayout layout)
        {
            string id = GameConstants.ExerciseParallel;
            float bayLength = config.parallelBayLength;
            float bayWidth = config.parallelBayWidth;

            const float laneX = 25f;
            const float bayCenterZ = 7f;
            float bayInnerX = laneX + 1.75f;
            float bayOuterX = bayInnerX + bayWidth;
            float zMin = bayCenterZ - bayLength * 0.5f;
            float zMax = bayCenterZ + bayLength * 0.5f;
            float corridorLeftX = laneX - 3.5f;

            // Katak chiziqlari (yo‘lak tomoni ochiq).
            PaintLine(staticRoot, new Vector3(bayOuterX, 0f, zMin), new Vector3(bayOuterX, 0f, zMax), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(bayInnerX, 0f, zMin), new Vector3(bayOuterX, 0f, zMin), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(bayInnerX, 0f, zMax), new Vector3(bayOuterX, 0f, zMax), MaterialKind.LineWhite, id);

            // Yo‘lak chegaralari.
            PaintLine(staticRoot, new Vector3(bayInnerX, 0f, zMin - 9f), new Vector3(bayInnerX, 0f, zMin), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(bayInnerX, 0f, zMax), new Vector3(bayInnerX, 0f, zMax + 9f), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(corridorLeftX, 0f, zMin - 9f), new Vector3(corridorLeftX, 0f, zMax + 9f), MaterialKind.LineWhite, id);

            // Bordyur.
            PrimitiveKit.Solid(PrimitiveType.Cube, "Parallel_Curb", staticRoot,
                new Vector3(bayOuterX + 0.45f, 0.075f, bayCenterZ), new Vector3(0.3f, 0.15f, bayLength + 18f),
                RuntimeMaterials.Get(MaterialKind.Concrete));

            // Konuslar — katak burchaklarida.
            CreateCone(dynamicRoot, new Vector3(bayInnerX, 0f, zMin - 0.3f));
            CreateCone(dynamicRoot, new Vector3(bayInnerX, 0f, zMax + 0.3f));
            CreateCone(dynamicRoot, new Vector3(bayOuterX, 0f, zMin - 0.3f));
            CreateCone(dynamicRoot, new Vector3(bayOuterX, 0f, zMax + 0.3f));

            ParkingZone bay = CreateZone(exerciseRoot, "Parallel_Bay",
                new Vector3((bayInnerX + bayOuterX) * 0.5f, 1f, bayCenterZ), Quaternion.identity,
                new Vector3(bayWidth, 3f, bayLength));
            ParkingZone startZone = CreateZone(exerciseRoot, "Parallel_StartZone",
                new Vector3(laneX - 0.9f, 1f, zMin - 7f), Quaternion.identity, new Vector3(4f, 3f, 4f));

            GroundLabel("P", dynamicRoot, new Vector3((bayInnerX + bayOuterX) * 0.5f, 0f, bayCenterZ), 0.09f);
            SignBoard("PARALLEL PARKOVKA", staticRoot, dynamicRoot, new Vector3(corridorLeftX - 2.5f, 0f, zMin - 9f), 0f);

            ParallelParkingExercise exercise = new GameObject("Exercise_Parallel").AddComponent<ParallelParkingExercise>();
            exercise.transform.SetParent(exerciseRoot, false);
            exercise.Configure(startZone, bay, Vector3.forward);

            layout.Exercises[id] = exercise;
            layout.SpawnPoses[id] = new Pose(new Vector3(laneX - 0.9f, 0.25f, zMin - 22f), Quaternion.identity);
        }

        // ------------------------------------------------------------------ Boks

        private static void BuildBox(Transform staticRoot, Transform dynamicRoot, Transform exerciseRoot,
            ExamConfig config, AvtodromLayout layout)
        {
            string id = GameConstants.ExerciseBox;
            float boxWidth = config.boxWidth;
            float boxDepth = config.boxDepth;

            const float laneX = -25f;
            const float boxCenterZ = 20f;
            float mouthX = laneX - 2.5f;
            float backX = mouthX - boxDepth;
            float zMin = boxCenterZ - boxWidth * 0.5f;
            float zMax = boxCenterZ + boxWidth * 0.5f;
            float corridorRightX = laneX + 6f;

            // Boks chiziqlari (kirish tomoni ochiq).
            PaintLine(staticRoot, new Vector3(backX, 0f, zMin), new Vector3(backX, 0f, zMax), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(backX, 0f, zMin), new Vector3(mouthX, 0f, zMin), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(backX, 0f, zMax), new Vector3(mouthX, 0f, zMax), MaterialKind.LineWhite, id);

            // Yo‘lak chegaralari.
            PaintLine(staticRoot, new Vector3(mouthX, 0f, zMin - 12f), new Vector3(mouthX, 0f, zMin), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(mouthX, 0f, zMax), new Vector3(mouthX, 0f, zMax + 12f), MaterialKind.LineWhite, id);
            PaintLine(staticRoot, new Vector3(corridorRightX, 0f, zMin - 12f), new Vector3(corridorRightX, 0f, zMax + 12f), MaterialKind.LineWhite, id);

            // Konuslar — kirish burchaklarida va orqa burchaklarda.
            CreateCone(dynamicRoot, new Vector3(mouthX, 0f, zMin - 0.3f));
            CreateCone(dynamicRoot, new Vector3(mouthX, 0f, zMax + 0.3f));
            CreateCone(dynamicRoot, new Vector3(backX - 0.3f, 0f, zMin - 0.3f));
            CreateCone(dynamicRoot, new Vector3(backX - 0.3f, 0f, zMax + 0.3f));

            ParkingZone box = CreateZone(exerciseRoot, "Box_Bay",
                new Vector3((mouthX + backX) * 0.5f, 1f, boxCenterZ), Quaternion.identity,
                new Vector3(boxDepth, 3f, boxWidth));
            ParkingZone startZone = CreateZone(exerciseRoot, "Box_StartZone",
                new Vector3(laneX + 1.5f, 1f, zMin - 9f), Quaternion.identity, new Vector3(4f, 3f, 4f));

            GroundLabel("BOKS", dynamicRoot, new Vector3((mouthX + backX) * 0.5f, 0f, boxCenterZ), 0.06f, -90f);
            SignBoard("BOKS", staticRoot, dynamicRoot, new Vector3(corridorRightX + 2.5f, 0f, zMin - 12f), 0f);

            BoxParkingExercise exercise = new GameObject("Exercise_Box").AddComponent<BoxParkingExercise>();
            exercise.transform.SetParent(exerciseRoot, false);
            exercise.Configure(startZone, box, Vector3.right);

            layout.Exercises[id] = exercise;
            layout.SpawnPoses[id] = new Pose(new Vector3(laneX + 1.5f, 0.25f, zMin - 24f), Quaternion.identity);
        }

        // ------------------------------------------------------------------ Yordamchilar

        /// <summary>Yerga chiziq chizadi. exerciseId null bo‘lsa — faqat bezak (jarimasiz).</summary>
        private static void PaintLine(Transform parent, Vector3 from, Vector3 to, MaterialKind kind, string exerciseId)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Vector3 mid = (from + to) * 0.5f;
            mid.y = 0.006f;
            Quaternion rotation = Quaternion.LookRotation(delta / length, Vector3.up);
            GameObject line = PrimitiveKit.Visual(PrimitiveType.Cube, "Line", parent, mid,
                new Vector3(LineWidth, 0.012f, length + LineWidth), RuntimeMaterials.Get(kind), rotation);

            if (exerciseId != null)
            {
                line.AddComponent<BoundaryLine>().Configure(new Vector3(LineWidth, 0.6f, length), exerciseId);
            }
        }

        private static ParkingZone CreateZone(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            ParkingZone zone = go.AddComponent<ParkingZone>();
            zone.Configure(size);
            return zone;
        }

        private static void CreateCone(Transform parent, Vector3 groundPosition)
        {
            GameObject cone = new GameObject("Cone");
            cone.transform.SetParent(parent, false);
            cone.transform.position = groundPosition + Vector3.up * 0.02f;

            Material orange = RuntimeMaterials.Get(MaterialKind.Cone);
            Material white = RuntimeMaterials.Get(MaterialKind.LineWhite);
            PrimitiveKit.Visual(PrimitiveType.Cube, "Base", cone.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.04f, 0.34f), orange);
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Lower", cone.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.22f, 0.11f, 0.22f), orange);
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Stripe", cone.transform, new Vector3(0f, 0.31f, 0f), new Vector3(0.16f, 0.05f, 0.16f), white);
            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Upper", cone.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.1f, 0.09f, 0.1f), orange);

            BoxCollider collider = cone.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.27f, 0f);
            collider.size = new Vector3(0.3f, 0.54f, 0.3f);

            Rigidbody body = cone.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.linearDamping = 0.5f;
            body.angularDamping = 0.8f;

            cone.AddComponent<TrafficCone>();
        }

        private static void GroundLabel(string text, Transform parent, Vector3 groundPosition, float characterSize, float yaw = 0f)
        {
            // X bo‘yicha 90° — matn yuqoriga qaraydi; yaw — yerda burish.
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
            PrimitiveKit.Label(text, parent, groundPosition + Vector3.up * 0.02f, rotation, characterSize,
                new Color(0.95f, 0.95f, 0.95f));
        }

        /// <summary>Ustunli belgi taxtasi. Matn -Z tomondan (yaw = 0) o‘qiladi.</summary>
        private static void SignBoard(string text, Transform staticRoot, Transform dynamicRoot, Vector3 groundPosition, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Material plastic = RuntimeMaterials.Get(MaterialKind.Plastic);
            float boardWidth = Mathf.Max(2.2f, text.Length * 0.3f);

            PrimitiveKit.Visual(PrimitiveType.Cylinder, "SignPole", staticRoot,
                groundPosition + new Vector3(0f, 1.2f, 0f), new Vector3(0.1f, 1.2f, 0.1f),
                RuntimeMaterials.Get(MaterialKind.Concrete));
            PrimitiveKit.Visual(PrimitiveType.Cube, "SignBoard", staticRoot,
                groundPosition + new Vector3(0f, 2.75f, 0f), new Vector3(boardWidth, 0.7f, 0.08f), plastic, rotation);
            PrimitiveKit.Label(text, dynamicRoot, groundPosition + new Vector3(0f, 2.75f, 0f) + rotation * new Vector3(0f, 0f, -0.06f),
                rotation, 0.045f, Color.white);
        }

        private static ExerciseBeacon BuildBeacon(Transform parent)
        {
            GameObject root = new GameObject("ExerciseBeacon");
            root.transform.SetParent(parent, false);

            Material glow = RuntimeMaterials.CreateInstance(MaterialKind.Emissive, new Color(0.2f, 1f, 0.45f));
            RuntimeMaterials.SetEmission(glow, new Color(0.2f, 1f, 0.45f) * 2.5f);

            PrimitiveKit.Visual(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 2.2f, 0f), new Vector3(0.08f, 2.2f, 0.08f), glow);
            GameObject arrow = PrimitiveKit.Visual(PrimitiveType.Cube, "Arrow", root.transform,
                new Vector3(0f, 5.2f, 0f), new Vector3(0.7f, 0.7f, 0.7f), glow, Quaternion.Euler(45f, 0f, 45f));

            ExerciseBeacon beacon = root.AddComponent<ExerciseBeacon>();
            beacon.Init(arrow.transform);
            return beacon;
        }
    }
}
