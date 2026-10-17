using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    public static class CoreSetup
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string ScenesPath = "Assets/_Scenes";

        [MenuItem("Cursed Harvest/1. Build Phase 1 Core")]
        public static void BuildPhase1()
        {
            BuildCompleteGame();
        }

        [MenuItem("Cursed Harvest/2. Build Phase 2 Survivor Loop")]
        public static void BuildPhase2()
        {
            BuildCompleteGame();
        }

        [MenuItem("Cursed Harvest/3. Build Phase 4 Harvest")]
        public static void BuildPhase4()
        {
            BuildCompleteGame();
        }

        [MenuItem("Cursed Harvest/Build Complete Game (All Phases)")]
        public static void BuildCompleteGame()
        {
            EnsureDirectories();

            // Create 3-tier clean explosion VFX prefabs
            CreateExplosionPrefabs(out GameObject smallFx, out GameObject mediumFx, out GameObject largeFx);

            GameObject boltPrefab = CreateBoltPrefab();
            GameObject bombPrefab = CreatePumpkinBombPrefab(smallFx);
            GameObject xpPrefab = CreateXpPickupPrefab();
            GameObject seedPrefab = CreateCursedSeedPrefab();
            GameObject healthPrefab = CreateHealthPickupPrefab();
            GameObject plotPrefab = CreateHarvestPlotPrefab(mediumFx);
            GameObject puddlePrefab = CreatePlaguePuddlePrefab();

            // Mask Variants Definitions from POLYGON Halloween Horror Masks
            string[] chaserMasks = new string[]
            {
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Pumpkin_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_LED_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Alpaca_Mask_01_Witch.prefab"
            };

            string[] farmerMasks = new string[]
            {
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Pumpkin_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_LED_Mask_01.prefab"
            };

            string[] bruteMasks = new string[]
            {
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Alpaca_Mask_01_Devil.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab"
            };

            GameObject playerPrefab = CreatePlayerPrefab(boltPrefab, smallFx);

            // Enemies
            GameObject gruntPrefab = CreatePumpkinGruntPrefab(smallFx);

            GameObject chaserPrefab = CreateGenericEnemyPrefab(
                "Enemy_MaskedChaser",
                EnemyType.MaskedChaser,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab",
                null,
                null,
                1.0f, 25f, 4.8f, 12f, false, 0f, false, 0f, false, null, smallFx,
                chaserMasks, 1.0f, 0.04f, false);

            GameObject plaguePrefab = CreateGenericEnemyPrefab(
                "Enemy_PlagueWalker",
                EnemyType.PlagueWalker,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Female_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab",
                null,
                1.05f, 55f, 2.7f, 10f, false, 0f, false, 0.1f, false, puddlePrefab, smallFx,
                null, 0f, 0.04f, false);

            GameObject chargerPrefab = CreateGenericEnemyPrefab(
                "Enemy_DemonCharger",
                EnemyType.DemonCharger,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab",
                1.28f, 70f, 3.2f, 15f, true, 25f, false, 0.2f, false, null, mediumFx,
                null, 0f, 0.04f, false);

            GameObject farmerPrefab = CreateGenericEnemyPrefab(
                "Enemy_CursedFarmer",
                EnemyType.CursedFarmer,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/Attachments/SM_Gen_Chr_Attach_Hat_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Spade_01.prefab",
                1.30f, 130f, 2.2f, 26f, false, 0f, false, 0.5f, false, null, mediumFx,
                farmerMasks, 0.30f, 0.04f, false);

            GameObject scarecrowPrefab = CreateGenericEnemyPrefab(
                "Enemy_EliteScarecrow",
                EnemyType.EliteScarecrow,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab",
                "Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab",
                null,
                1.55f, 380f, 2.0f, 20f, false, 0f, true, 0.75f, true, null, mediumFx,
                null, 0f, 0.35f, false);

            GameObject brutePrefab = CreateGenericEnemyPrefab(
                "Enemy_HarvestBrute",
                EnemyType.HarvestBrute,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab",
                null,
                "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab",
                1.70f, 480f, 2.1f, 32f, false, 0f, false, 1.0f, true, null, mediumFx,
                bruteMasks, 0.45f, 0.35f, false);

            GameObject bossPrefab = CreateBossPrefab(bombPrefab, largeFx);

            CreateMainScene(
                playerPrefab,
                boltPrefab,
                gruntPrefab,
                chaserPrefab,
                plaguePrefab,
                chargerPrefab,
                farmerPrefab,
                bossPrefab,
                bombPrefab,
                xpPrefab,
                seedPrefab,
                plotPrefab,
                healthPrefab,
                scarecrowPrefab,
                brutePrefab);

            Debug.Log("[CursedHarvest] Complete Game Build (Phases 1-6) Finished Successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Scripts")) AssetDatabase.CreateFolder("Assets", "_Scripts");
            if (!AssetDatabase.IsValidFolder("Assets/_Prefabs")) AssetDatabase.CreateFolder("Assets", "_Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/_Scenes")) AssetDatabase.CreateFolder("Assets", "_Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/_Materials")) AssetDatabase.CreateFolder("Assets", "_Materials");
            AssetDatabase.SaveAssets();
        }

        private static void CreateExplosionPrefabs(out GameObject smallFx, out GameObject mediumFx, out GameObject largeFx)
        {
            string smallPath = $"{PrefabsPath}/VFX_Explosion_Small.prefab";
            string mediumPath = $"{PrefabsPath}/VFX_Explosion_Medium.prefab";
            string largePath = $"{PrefabsPath}/VFX_Explosion_Large.prefab";

            Material baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleFX/Materials/FX_Base.mat");
            if (baseMat == null) baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleFX/Materials/FX_GunFlash.mat");

            smallFx = BuildExplosionPrefab("VFX_Explosion_Small", smallPath, 0.35f, 0.28f, 3.5f, 0.5f, 14, 2.5f, 2.0f, new Color(1f, 0.6f, 0.1f, 0.9f), baseMat);
            mediumFx = BuildExplosionPrefab("VFX_Explosion_Medium", mediumPath, 0.50f, 0.42f, 4.5f, 0.85f, 24, 4.5f, 3.2f, new Color(0.9f, 0.25f, 0.95f, 0.9f), baseMat);
            largeFx = BuildExplosionPrefab("VFX_Explosion_Large", largePath, 0.70f, 0.58f, 6.0f, 1.3f, 38, 7.0f, 4.5f, new Color(1f, 0.4f, 0.05f, 0.95f), baseMat);
        }

        private static GameObject BuildExplosionPrefab(
            string name,
            string path,
            float duration,
            float lifetime,
            float speed,
            float size,
            int burstCount,
            float lightRange,
            float lightIntensity,
            Color color,
            Material mat)
        {
            GameObject instance = new GameObject(name);
            ParticleSystem ps = instance.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = instance.GetComponent<ParticleSystemRenderer>();
            if (mat != null) psr.material = mat;

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = burstCount + 8;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.4f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.0f);
            curve.AddKey(0.6f, 0.7f);
            curve.AddKey(1f, 0.05f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            // Flash point light
            GameObject lightObj = new GameObject("ExplosionFlashLight");
            lightObj.transform.SetParent(instance.transform, false);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = lightRange;
            l.intensity = lightIntensity;

            VfxAutoDestroy vad = instance.AddComponent<VfxAutoDestroy>();
            vad.SetLifetime(duration + 0.05f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateBoltPrefab()
        {
            string targetPath = $"{PrefabsPath}/CrossbowBolt.prefab";
            GameObject boltSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonBowCrossbow/Prefabs/SM_Wep_Crossbow_Bolt_01.prefab");
            GameObject impactSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_DirtSplatter.prefab");

            GameObject instance = new GameObject("CrossbowBolt");
            instance.tag = "Projectile";
            int projLayer = LayerMask.NameToLayer("Projectile");
            if (projLayer >= 0) instance.layer = projLayer;

            if (boltSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(boltSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = new Vector3(1.6f, 1.6f, 2.2f); // Readable, substantial bolt size
            }

            // Short crisp ribbon trail
            GameObject trailObj = new GameObject("BoltTrail");
            trailObj.transform.SetParent(instance.transform, false);
            trailObj.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            TrailRenderer tr = trailObj.AddComponent<TrailRenderer>();
            tr.time = 0.12f;
            tr.startWidth = 0.13f;
            tr.endWidth = 0.02f;
            tr.autodestruct = false;
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                tr.material = new Material(defaultShader);
            }
            tr.startColor = new Color(1f, 0.85f, 0.25f, 0.95f);
            tr.endColor = new Color(1f, 0.45f, 0.05f, 0f);

            // Subtle warm glowing tip
            GameObject glowObj = new GameObject("BoltGlow");
            glowObj.transform.SetParent(instance.transform, false);
            glowObj.transform.localPosition = new Vector3(0f, 0f, 0.4f);
            Light glow = glowObj.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.82f, 0.35f);
            glow.range = 1.8f;
            glow.intensity = 1.5f;

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.35f, 0.35f, 1.2f);
            collider.center = new Vector3(0f, 0f, 0.4f);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            LayerMask hitMask = enemyLayer >= 0 ? (1 << enemyLayer) : ~0;

            Projectile proj = instance.AddComponent<Projectile>();
            proj.SetHitMask(hitMask);
            if (impactSource != null)
            {
                proj.SetImpactVfx(impactSource);
            }
            instance.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateXpPickupPrefab()
        {
            string targetPath = $"{PrefabsPath}/XpPickup.prefab";
            GameObject coinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Icon_Coin_01.prefab");
            if (coinSource == null)
            {
                coinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Coin_01.prefab");
            }

            GameObject instance = new GameObject("XpPickup");
            instance.tag = "Pickup";
            int pickupLayer = LayerMask.NameToLayer("Pickup");
            if (pickupLayer >= 0) instance.layer = pickupLayer;

            if (coinSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(coinSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 0.7f;
            }
            else
            {
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                placeholder.transform.SetParent(instance.transform, false);
                placeholder.transform.localScale = Vector3.one * 0.4f;
                UnityEngine.Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            }

            SphereCollider col = instance.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // Small glowing point light
            GameObject lightObj = new GameObject("GlowLight");
            lightObj.transform.SetParent(instance.transform, false);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 0.8f, 1f);
            l.range = 2.5f;
            l.intensity = 1.5f;

            instance.AddComponent<ExperiencePickup>();
            instance.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateCursedSeedPrefab()
        {
            string targetPath = $"{PrefabsPath}/CursedSeed.prefab";
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");

            GameObject instance = new GameObject("CursedSeed");
            instance.tag = "Pickup";
            int pickupLayer = LayerMask.NameToLayer("Pickup");
            if (pickupLayer >= 0) instance.layer = pickupLayer;

            if (pumpkinSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 0.35f;
            }

            SphereCollider col = instance.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // Spooky purple/magenta glowing light
            GameObject lightObj = new GameObject("SeedLight");
            lightObj.transform.SetParent(instance.transform, false);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.2f, 1f);
            l.range = 3.0f;
            l.intensity = 2.0f;

            instance.AddComponent<CursedSeedPickup>();
            instance.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateHealthPickupPrefab()
        {
            string targetPath = $"{PrefabsPath}/HealthRestorePickup.prefab";
            GameObject potionSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Potion_01.prefab");

            GameObject instance = new GameObject("HealthRestorePickup");
            instance.tag = "Pickup";
            int pickupLayer = LayerMask.NameToLayer("Pickup");
            if (pickupLayer >= 0) instance.layer = pickupLayer;

            if (potionSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(potionSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 0.95f;
                RemoveColliders(model);
            }
            else
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(instance.transform, false);
                sphere.transform.localScale = Vector3.one * 0.4f;
                RemoveColliders(sphere);
            }

            SphereCollider col = instance.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            Rigidbody rb = instance.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Soft restorative healing green light
            GameObject lightObj = new GameObject("HealLight");
            lightObj.transform.SetParent(instance.transform, false);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.38f, 1.0f, 0.55f);
            l.range = 2.8f;
            l.intensity = 1.8f;

            // Gentle ascending healing particles
            GameObject psObj = new GameObject("HealWisps");
            psObj.transform.SetParent(instance.transform, false);
            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = 0.65f;
            main.startSpeed = 0.35f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.45f, 1.0f, 0.60f, 0.75f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 4f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var rend = psObj.GetComponent<ParticleSystemRenderer>();
            Material sparkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleFX/Materials/FX_GunFlash.mat");
            if (sparkMat == null) sparkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleFX/Materials/FX_Base.mat");
            if (sparkMat != null) rend.material = sparkMat;

            instance.AddComponent<HealthRestorePickup>();
            instance.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateHarvestPlotPrefab(GameObject mediumFx = null)
        {
            string targetPath = $"{PrefabsPath}/HarvestPlot.prefab";
            GameObject dirtSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject luckyVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Fireworks_Green_Large.prefab");
            GameObject cursedVfx = mediumFx != null ? mediumFx : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");
            GameObject rootSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Root_01.prefab");
            GameObject leavesSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Leaves_01.prefab");
            GameObject fogSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Fog_01.prefab");
            if (fogSource == null) fogSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Steam.prefab");

            GameObject instance = new GameObject("HarvestPlot");
            instance.tag = "HarvestPlot";

            if (dirtSource != null)
            {
                GameObject dirt = (GameObject)PrefabUtility.InstantiatePrefab(dirtSource, instance.transform);
                dirt.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                dirt.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
                RemoveColliders(dirt);
            }

            // Corrupt ritual roots emerging from soil
            if (rootSource != null)
            {
                GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(rootSource, instance.transform);
                root.transform.localPosition = new Vector3(-0.55f, 0.06f, -0.35f);
                root.transform.localRotation = Quaternion.Euler(0f, 50f, 0f);
                root.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                RemoveColliders(root);
            }

            // Autumn dead leaves around plot rim
            if (leavesSource != null)
            {
                GameObject leaves = (GameObject)PrefabUtility.InstantiatePrefab(leavesSource, instance.transform);
                leaves.transform.localPosition = new Vector3(0.45f, 0.06f, 0.40f);
                leaves.transform.localRotation = Quaternion.Euler(0f, 130f, 0f);
                leaves.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(leaves);
            }

            // Stone shrine statue landmark behind the plot
            GameObject statueSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Statue_04.prefab");
            if (statueSource != null)
            {
                GameObject statue = (GameObject)PrefabUtility.InstantiatePrefab(statueSource, instance.transform);
                statue.transform.localPosition = new Vector3(0f, 0.06f, 0.75f);
                statue.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                statue.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                RemoveColliders(statue);
            }

            // Local faint ground mist
            if (fogSource != null)
            {
                GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogSource, instance.transform);
                mist.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                mist.transform.localScale = new Vector3(1.8f, 0.4f, 1.8f);
            }

            // Growing pumpkin child
            GameObject growingPumpkin = new GameObject("GrowingPumpkin");
            growingPumpkin.transform.SetParent(instance.transform, false);
            growingPumpkin.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            if (pumpkinSource != null)
            {
                GameObject pModel = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, growingPumpkin.transform);
                pModel.transform.localPosition = Vector3.zero;
                RemoveColliders(pModel);
            }

            // Beacon / plot light (IDLE starts in soft atmospheric warm amber)
            GameObject lightObj = new GameObject("PlotLight");
            lightObj.transform.SetParent(instance.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.65f, 0.25f);
            l.range = 7.5f;
            l.intensity = 1.6f;

            HarvestPlot plot = instance.AddComponent<HarvestPlot>();
            SerializedObject soPlot = new SerializedObject(plot);
            soPlot.FindProperty("growingPumpkin").objectReferenceValue = growingPumpkin;
            soPlot.FindProperty("plotLight").objectReferenceValue = l;
            if (luckyVfx != null) soPlot.FindProperty("luckyVfxPrefab").objectReferenceValue = luckyVfx;
            if (cursedVfx != null) soPlot.FindProperty("cursedVfxPrefab").objectReferenceValue = cursedVfx;
            soPlot.ApplyModifiedProperties();

            growingPumpkin.SetActive(false);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreatePlayerPrefab(GameObject boltPrefab, GameObject smallExplosionFx = null)
        {
            string targetPath = $"{PrefabsPath}/Player.prefab";
            GameObject peasantSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab");
            GameObject bowSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonBowCrossbow/Prefabs/SM_Wep_Rigged_CrossBow_01.prefab");

            GameObject root = new GameObject("Player");
            root.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) root.layer = playerLayer;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.15f; // +15% heroic silhouette (Paso 2)

            Transform headSocket = null;
            if (peasantSource != null)
            {
                GameObject peasant = (GameObject)PrefabUtility.InstantiatePrefab(peasantSource, visual.transform);
                peasant.transform.localPosition = Vector3.zero;
                peasant.transform.localRotation = Quaternion.identity;

                Transform[] allChildren = peasant.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (headSocket == null && (t.name.Contains("Head") || t.name.Contains("Neck")))
                    {
                        headSocket = t;
                        break;
                    }
                }
            }

            // Attach hairstyle for recognizable silhouette
            GameObject hairSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/Attachments/SM_Gen_Chr_Attach_Hair_04.prefab");
            if (hairSource != null && headSocket != null)
            {
                GameObject hair = (GameObject)PrefabUtility.InstantiatePrefab(hairSource, headSocket);
                hair.transform.localPosition = new Vector3(0f, 0.12f, 0.02f);
                hair.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                hair.transform.localScale = Vector3.one * 0.5f;
            }

            // Two-handed combat stance and dedicated WeaponSocket
            PlayerCombatPose combatPose = root.AddComponent<PlayerCombatPose>();
            combatPose.FindBones();

            GameObject bowInstance = null;
            Transform firePoint = null;
            if (bowSource != null)
            {
                bowInstance = (GameObject)PrefabUtility.InstantiatePrefab(bowSource, visual.transform);
                GameObject fp = new GameObject("FirePoint");
                firePoint = fp.transform;
                combatPose.SetupWeapon(bowInstance.transform, firePoint);
            }

            PlayerController pc = root.AddComponent<PlayerController>();
            PlayerLocomotion playerLoco = root.AddComponent<PlayerLocomotion>();
            playerLoco.FindBones();
            PlayerHealth ph = root.AddComponent<PlayerHealth>();
            PlayerDash pd = root.AddComponent<PlayerDash>();
            WeaponManager wm = root.AddComponent<WeaponManager>();
            ExperienceManager xp = root.AddComponent<ExperienceManager>();
            UpgradeManager um = root.AddComponent<UpgradeManager>();
            LuckyLastSystem lucky = root.AddComponent<LuckyLastSystem>();
            root.AddComponent<PlayerGroundIndicator>(); // Real-time glowing ground circle & forward arrow (Paso 2)

            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Fire.prefab");
            GameObject gunFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_GunFlash.prefab");

            if (firePrefab != null)
            {
                SerializedObject soLucky = new SerializedObject(lucky);
                soLucky.FindProperty("luckyLastAuraPrefab").objectReferenceValue = firePrefab;
                soLucky.ApplyModifiedProperties();
            }

            CrossbowWeapon crossbow = root.AddComponent<CrossbowWeapon>();
            if (firePoint != null) crossbow.SetFirePoint(firePoint);
            if (bowInstance != null) crossbow.SetVisualBowModel(bowInstance.transform);
            if (gunFlashPrefab != null) crossbow.SetMuzzleFlashPrefab(gunFlashPrefab);
            wm.AddWeapon(crossbow);

            // Additional Weapons (unlocked via upgrades)
            GameObject pickaxeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");
            GameObject explosionEffect = smallExplosionFx != null ? smallExplosionFx : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            PumpkinBombWeapon bombWeapon = root.AddComponent<PumpkinBombWeapon>();
            bombWeapon.SetExplosionVfx(explosionEffect);
            bombWeapon.enabled = false;
            wm.AddWeapon(bombWeapon);

            HarvestScytheWeapon scytheWeapon = root.AddComponent<HarvestScytheWeapon>();
            if (pickaxeModel != null) scytheWeapon.SetBladeModel(pickaxeModel);
            scytheWeapon.enabled = false;
            wm.AddWeapon(scytheWeapon);

            FireAuraWeapon fireAuraWeapon = root.AddComponent<FireAuraWeapon>();
            fireAuraWeapon.enabled = false;
            wm.AddWeapon(fireAuraWeapon);

            // Strip any editor-time cloned objects or inactive weapon containers so Player starts with a clean body
            Transform[] allKids = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform kid in allKids)
            {
                if (kid != null && (kid.name.Contains("(Clone)") || kid.name.Contains("ScytheBladesContainer")))
                {
                    UnityEngine.Object.DestroyImmediate(kid.gameObject);
                }
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreatePumpkinGruntPrefab(GameObject smallFx = null)
        {
            string targetPath = $"{PrefabsPath}/Enemy_PumpkinGrunt.prefab";
            GameObject skeletonSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Skeleton_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject explosionEffect = smallFx != null ? smallFx : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            GameObject root = new GameObject("Enemy_PumpkinGrunt");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.8f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.85f; // Compact, hunched skeleton silhouette

            Transform headBone = null;
            if (skeletonSource != null)
            {
                GameObject skeleton = (GameObject)PrefabUtility.InstantiatePrefab(skeletonSource, visual.transform);
                skeleton.transform.localPosition = Vector3.zero;
                skeleton.transform.localRotation = Quaternion.identity;

                Transform[] bones = skeleton.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (b.name.Contains("Head") || b.name.Contains("Neck"))
                    {
                        headBone = b;
                        break;
                    }
                }
            }

            if (pumpkinSource != null)
            {
                // Parent pumpkin head directly to visual.transform so it maintains clean character-forward alignment and rests squarely on the neck
                GameObject pumpkin = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, visual.transform);
                pumpkin.transform.localPosition = new Vector3(0f, 1.34f, 0.02f);
                pumpkin.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                pumpkin.transform.localScale = Vector3.one * 0.46f;
                RemoveColliders(pumpkin);
            }

            EnemyController ec = root.AddComponent<EnemyController>();
            EnemyLocomotion el = root.AddComponent<EnemyLocomotion>();
            el.SetupProfile(EnemyType.PumpkinGrunt);
            el.FindBones();
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            root.AddComponent<PooledObjectTracker>();

            SerializedObject soEh = new SerializedObject(eh);
            SerializedProperty propFx = soEh.FindProperty("deathEffectPrefab");
            if (propFx != null && explosionEffect != null)
            {
                propFx.objectReferenceValue = explosionEffect;
            }
            soEh.FindProperty("healthDropChance").floatValue = 0.04f;
            soEh.ApplyModifiedProperties();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreatePumpkinBombPrefab(GameObject smallFx = null)
        {
            string targetPath = $"{PrefabsPath}/PumpkinBomb.prefab";
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab");
            GameObject explosionEffect = smallFx != null ? smallFx : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            GameObject instance = new GameObject("PumpkinBomb");
            instance.tag = "Projectile";

            if (pumpkinSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * 0.45f;
            }

            SphereCollider col = instance.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;

            PumpkinBombProjectile proj = instance.AddComponent<PumpkinBombProjectile>();
            if (explosionEffect != null)
            {
                SerializedObject soProj = new SerializedObject(proj);
                soProj.FindProperty("explosionVfxPrefab").objectReferenceValue = explosionEffect;
                soProj.ApplyModifiedProperties();
            }
            instance.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreatePlaguePuddlePrefab()
        {
            string targetPath = $"{PrefabsPath}/PlaguePuddle.prefab";
            GameObject steamEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Steam.prefab");

            GameObject instance = new GameObject("PlaguePuddle");

            if (steamEffect != null)
            {
                GameObject fx = (GameObject)PrefabUtility.InstantiatePrefab(steamEffect, instance.transform);
                fx.transform.localPosition = Vector3.zero;
                fx.transform.localScale = Vector3.one * 0.8f;
            }

            GameObject lightObj = new GameObject("PuddleLight");
            lightObj.transform.SetParent(instance.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 1f, 0.2f);
            l.range = 3.5f;
            l.intensity = 1.5f;

            PlaguePuddle puddle = instance.AddComponent<PlaguePuddle>();
            puddle.Initialize(5f, 2.2f, 6f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static GameObject CreateGenericEnemyPrefab(
            string prefabName,
            EnemyType type,
            string bodyPath,
            string headAttachPath,
            string handAttachPath,
            float scale,
            float hp,
            float speed,
            float contactDmg,
            bool canCharge,
            float chargeDmg,
            bool canSummon,
            float knockbackRes,
            bool guaranteedSeed,
            GameObject hazardPrefab,
            GameObject deathEffect = null,
            string[] maskVariants = null,
            float maskChance = 0f,
            float healthDropChance = 0.04f,
            bool guaranteedHealth = false)
        {
            string targetPath = $"{PrefabsPath}/{prefabName}.prefab";
            GameObject bodySource = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            GameObject headSource = !string.IsNullOrEmpty(headAttachPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(headAttachPath) : null;
            GameObject handSource = !string.IsNullOrEmpty(handAttachPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(handAttachPath) : null;
            GameObject explosionEffect = deathEffect != null ? deathEffect : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            GameObject root = new GameObject(prefabName);
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.6f * scale;
            cc.radius = 0.4f * scale;
            cc.center = new Vector3(0f, 0.8f * scale, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * scale;

            Transform headBone = null;
            Transform handBone = null;

            if (bodySource != null)
            {
                GameObject body = (GameObject)PrefabUtility.InstantiatePrefab(bodySource, visual.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;

                Transform[] bones = body.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (headBone == null && (b.name == "Head" || b.name.Contains("Head")))
                    {
                        headBone = b;
                    }
                    if (handBone == null && (b.name.Contains("Hand_R") || b.name.Contains("RightHand")))
                    {
                        handBone = b;
                    }
                }
            }

            if (headSource != null)
            {
                Transform parent = headBone != null ? headBone : visual.transform;
                GameObject head = (GameObject)PrefabUtility.InstantiatePrefab(headSource, parent);
                RemoveColliders(head);
                if (headBone != null)
                {
                    head.transform.localPosition = Vector3.zero;
                    head.transform.localRotation = Quaternion.identity;
                    head.transform.localScale = Vector3.one;
                }
                else
                {
                    head.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                    head.transform.localRotation = Quaternion.identity;
                    head.transform.localScale = Vector3.one;
                }
            }

            // Mask Variants System
            if (maskVariants != null && maskVariants.Length > 0 && headBone != null)
            {
                GameObject maskSocket = new GameObject("MaskSocket");
                maskSocket.transform.SetParent(headBone, false);
                maskSocket.transform.localPosition = Vector3.zero;
                maskSocket.transform.localRotation = Quaternion.identity;
                maskSocket.transform.localScale = Vector3.one;

                List<GameObject> instantiatedMasks = new List<GameObject>();
                for (int i = 0; i < maskVariants.Length; i++)
                {
                    string mPath = maskVariants[i];
                    GameObject mSource = AssetDatabase.LoadAssetAtPath<GameObject>(mPath);
                    if (mSource != null)
                    {
                        GameObject mObj = (GameObject)PrefabUtility.InstantiatePrefab(mSource, maskSocket.transform);
                        RemoveColliders(mObj);

                        float maskScale = (type == EnemyType.CursedFarmer) ? 0.95f : 1.0f;
                        mObj.transform.localPosition = Vector3.zero;
                        mObj.transform.localRotation = Quaternion.identity;
                        mObj.transform.localScale = Vector3.one * maskScale;
                        mObj.SetActive(false);
                        instantiatedMasks.Add(mObj);
                    }
                }

                if (instantiatedMasks.Count > 0)
                {
                    EnemyMaskVariant emv = root.AddComponent<EnemyMaskVariant>();
                    SerializedObject soEmv = new SerializedObject(emv);
                    SerializedProperty spList = soEmv.FindProperty("maskVariants");
                    spList.arraySize = instantiatedMasks.Count;
                    for (int m = 0; m < instantiatedMasks.Count; m++)
                    {
                        spList.GetArrayElementAtIndex(m).objectReferenceValue = instantiatedMasks[m];
                    }
                    soEmv.FindProperty("maskChance").floatValue = maskChance;
                    soEmv.ApplyModifiedProperties();
                }
            }

            if (handSource != null && handBone != null)
            {
                GameObject handItem = (GameObject)PrefabUtility.InstantiatePrefab(handSource, handBone);
                RemoveColliders(handItem);
                handItem.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
                handItem.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                handItem.transform.localScale = Vector3.one * 0.85f;
            }

            EnemyController ec = root.AddComponent<EnemyController>();
            EnemyLocomotion el = root.AddComponent<EnemyLocomotion>();
            el.SetupProfile(type);
            el.FindBones();
            SerializedObject soEc = new SerializedObject(ec);
            soEc.FindProperty("enemyType").enumValueIndex = (int)type;
            soEc.FindProperty("baseMoveSpeed").floatValue = speed;
            soEc.FindProperty("contactDamage").floatValue = contactDmg;
            soEc.FindProperty("canCharge").boolValue = canCharge;
            soEc.FindProperty("chargeDamage").floatValue = chargeDmg;
            soEc.FindProperty("canSummonMinions").boolValue = canSummon;
            soEc.ApplyModifiedProperties();

            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = hp;
            soEh.FindProperty("xpDropValue").floatValue = hp * 0.35f;
            soEh.FindProperty("seedDropChance").floatValue = 0.25f;
            soEh.FindProperty("guaranteedSeedDrop").boolValue = guaranteedSeed;
            soEh.FindProperty("healthDropChance").floatValue = healthDropChance;
            soEh.FindProperty("guaranteedHealthDrop").boolValue = guaranteedHealth;
            soEh.FindProperty("knockbackResistance").floatValue = knockbackRes;
            if (hazardPrefab != null) soEh.FindProperty("onDeathHazardPrefab").objectReferenceValue = hazardPrefab;
            if (explosionEffect != null) soEh.FindProperty("deathEffectPrefab").objectReferenceValue = explosionEffect;
            soEh.ApplyModifiedProperties();

            root.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateBossPrefab(GameObject bombPrefab, GameObject largeFx = null)
        {
            string targetPath = $"{PrefabsPath}/Boss_TheLastHarvest.prefab";
            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject pickaxeSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");
            GameObject explosionEffect = largeFx != null ? largeFx : AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            GameObject root = new GameObject("Boss_TheLastHarvest");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f;
            cc.radius = 1.0f;
            cc.center = new Vector3(0f, 1.8f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 2.2f;

            Transform headBone = null;
            Transform handBone = null;

            if (charredSource != null)
            {
                GameObject charred = (GameObject)PrefabUtility.InstantiatePrefab(charredSource, visual.transform);
                charred.transform.localPosition = Vector3.zero;
                charred.transform.localRotation = Quaternion.identity;

                Transform[] bones = charred.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (headBone == null && (b.name.Contains("Head") || b.name.Contains("Neck"))) headBone = b;
                    if (handBone == null && (b.name.Contains("Hand_R") || b.name.Contains("RightHand"))) handBone = b;
                }
            }

            if (pumpkinSource != null && headBone != null)
            {
                GameObject pumpkin = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, headBone);
                pumpkin.transform.localPosition = new Vector3(0f, 0.15f, 0.05f);
                pumpkin.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                pumpkin.transform.localScale = Vector3.one * 0.65f;
            }

            if (pickaxeSource != null && handBone != null)
            {
                GameObject pick = (GameObject)PrefabUtility.InstantiatePrefab(pickaxeSource, handBone);
                pick.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
                pick.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                pick.transform.localScale = Vector3.one * 1.0f;
            }

            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 2000f;
            soEh.FindProperty("xpDropValue").floatValue = 300f;
            soEh.FindProperty("seedDropChance").floatValue = 1.0f;
            soEh.FindProperty("guaranteedSeedDrop").boolValue = true;
            soEh.FindProperty("healthDropChance").floatValue = 1.0f;
            soEh.FindProperty("guaranteedHealthDrop").boolValue = true;
            soEh.FindProperty("healthDropCount").intValue = 2;
            soEh.FindProperty("knockbackResistance").floatValue = 1.0f;
            if (explosionEffect != null) soEh.FindProperty("deathEffectPrefab").objectReferenceValue = explosionEffect;
            soEh.ApplyModifiedProperties();

            BossController bc = root.AddComponent<BossController>();
            EnemyLocomotion el = root.AddComponent<EnemyLocomotion>();
            el.SetupProfile(EnemyType.PumpkinGrunt);
            el.FindBones();
            SerializedObject soBc = new SerializedObject(bc);
            soBc.FindProperty("bossName").stringValue = "THE LAST HARVEST";
            soBc.FindProperty("moveSpeed").floatValue = 2.6f;
            soBc.FindProperty("contactDamage").floatValue = 30f;
            soBc.FindProperty("slamRadius").floatValue = 6.5f;
            soBc.FindProperty("slamDamage").floatValue = 35f;
            if (explosionEffect != null) soBc.FindProperty("slamVfxPrefab").objectReferenceValue = explosionEffect;
            if (bombPrefab != null) soBc.FindProperty("bombPrefab").objectReferenceValue = bombPrefab;
            soBc.ApplyModifiedProperties();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateMainScene(
            GameObject playerPrefab,
            GameObject boltPrefab,
            GameObject gruntPrefab,
            GameObject chaserPrefab,
            GameObject plaguePrefab,
            GameObject chargerPrefab,
            GameObject farmerPrefab,
            GameObject bossPrefab,
            GameObject bombPrefab,
            GameObject xpPrefab,
            GameObject seedPrefab,
            GameObject plotPrefab,
            GameObject healthPrefab = null,
            GameObject scarecrowPrefab = null,
            GameObject brutePrefab = null)
        {
            if (scarecrowPrefab == null) scarecrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_EliteScarecrow.prefab");
            if (brutePrefab == null) brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_HarvestBrute.prefab");
            string scenePath = $"{ScenesPath}/MainGame.unity";
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. SkyDome
            GameObject skyDomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleSky/Prefabs/SkyDome.prefab");
            if (skyDomePrefab == null)
            {
                skyDomePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_SimpleSky_Dome_01.prefab");
            }
            if (skyDomePrefab != null)
            {
                GameObject sky = (GameObject)PrefabUtility.InstantiatePrefab(skyDomePrefab);
                sky.transform.position = Vector3.zero;
                sky.transform.localScale = Vector3.one * 5f;
            }

            // 2. RenderSettings: Rural Folk Horror Midnight Fog & Atmospheric Lighting
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.16f, 0.22f); // Nocturnal ambient bounce, readable ground
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 16.0f; // Near player is crystal clear (camera is ~18.3m away)
            RenderSettings.fogEndDistance = 36.0f;   // Dense shrouded mist at arena boundary (~35m)
            RenderSettings.fogColor = new Color(0.095f, 0.135f, 0.19f); // Visible cold misty night blue (#182230)

            // 3. Directional Light (Pale Cold Moonlight)
            GameObject lightObj = new GameObject("Moonlight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.44f, 0.52f, 0.70f); // Pale cold moonlight
            light.intensity = 0.70f; // Readable contrast with deep shadows
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // 3. Ground, Paths, Landmarks & Environment
            GameObject arenaRoot = new GameObject("Arena");
            BuildGroundAndPaths(arenaRoot.transform);
            BuildAtmosphericLandmarks(arenaRoot.transform, plotPrefab);
            BuildVisiblePerimeter(arenaRoot.transform);

            // 4. Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.045f, 0.06f, 0.09f); // Deep midnight void (#0b0f16) giving visible contrast to the fog
            camObj.AddComponent<AudioListener>();
            TopDownCameraFollow camFollow = camObj.AddComponent<TopDownCameraFollow>();

            // 5. Pools
            GameObject poolsRoot = new GameObject("Pools");
            ObjectPool boltPool = CreatePool(poolsRoot.transform, "BoltPool", boltPrefab, 30);
            ObjectPool gruntPool = CreatePool(poolsRoot.transform, "GruntPool", gruntPrefab, 40);
            ObjectPool chaserPool = chaserPrefab != null ? CreatePool(poolsRoot.transform, "ChaserPool", chaserPrefab, 25) : null;
            ObjectPool plaguePool = plaguePrefab != null ? CreatePool(poolsRoot.transform, "PlaguePool", plaguePrefab, 20) : null;
            ObjectPool chargerPool = chargerPrefab != null ? CreatePool(poolsRoot.transform, "ChargerPool", chargerPrefab, 15) : null;
            ObjectPool farmerPool = farmerPrefab != null ? CreatePool(poolsRoot.transform, "FarmerPool", farmerPrefab, 15) : null;
            ObjectPool bombPool = bombPrefab != null ? CreatePool(poolsRoot.transform, "BombPool", bombPrefab, 20) : null;
            ObjectPool xpPool = CreatePool(poolsRoot.transform, "XpPool", xpPrefab, 60);
            ObjectPool seedPool = CreatePool(poolsRoot.transform, "SeedPool", seedPrefab, 25);
            ObjectPool healthPool = healthPrefab != null ? CreatePool(poolsRoot.transform, "HealthPool", healthPrefab, 20) : null;

            // 6. Player Instance
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, 0.1f, 0f);

            CrossbowWeapon crossbow = playerInstance.GetComponent<CrossbowWeapon>();
            if (crossbow != null && boltPool != null)
            {
                crossbow.SetBoltPool(boltPool);
            }

            PumpkinBombWeapon bombWeapon = playerInstance.GetComponent<PumpkinBombWeapon>();
            if (bombWeapon != null && bombPool != null)
            {
                bombWeapon.SetBombPool(bombPool);
            }

            camFollow.SetTarget(playerInstance.transform);

            // 7. SpawnDirector
            GameObject sdObj = new GameObject("SpawnDirector");
            SpawnDirector spawnDirector = sdObj.AddComponent<SpawnDirector>();
            spawnDirector.SetPlayer(playerInstance.transform);
            spawnDirector.SetPools(gruntPool, chaserPool, plaguePool, chargerPool, farmerPool);
            if (bossPrefab != null) spawnDirector.SetBossPrefab(bossPrefab);
            spawnDirector.SetElitePrefabs(scarecrowPrefab, brutePrefab);
            if (healthPrefab != null) spawnDirector.SetHealthPickupPrefab(healthPrefab);

            // 8. UI Canvas & HUD
            CreateUICanvas();

            // 9. GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();

            SerializedObject soGm = new SerializedObject(gm);
            soGm.FindProperty("player").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            soGm.FindProperty("boltPool").objectReferenceValue = boltPool;
            soGm.FindProperty("enemyPool").objectReferenceValue = gruntPool;
            soGm.FindProperty("xpPool").objectReferenceValue = xpPool;
            soGm.FindProperty("seedPool").objectReferenceValue = seedPool;
            if (healthPool != null) soGm.FindProperty("healthPool").objectReferenceValue = healthPool;
            soGm.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            soGm.FindProperty("autoSpawnEnemies").boolValue = false;
            soGm.ApplyModifiedProperties();

            // 10. Audio & Polish Managers
            GameObject audioObj = new GameObject("AudioManager");
            AudioManager audioMgr = audioObj.AddComponent<AudioManager>();
            SerializedObject soAudio = new SerializedObject(audioMgr);
            AudioClip gpMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Music/Echoes_of_Time_v2.mp3");
            AudioClip bsMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Music/Grave_Blow.mp3");
            if (gpMusic != null) soAudio.FindProperty("gameplayMusicClip").objectReferenceValue = gpMusic;
            if (bsMusic != null) soAudio.FindProperty("bossMusicClip").objectReferenceValue = bsMusic;
            soAudio.ApplyModifiedProperties();

            GameObject dmgNumObj = new GameObject("DamageNumberManager");
            dmgNumObj.AddComponent<DamageNumberManager>();

            EditorSceneManager.SaveScene(scene, scenePath);

            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;
        }

        private static void CreateUICanvas()
        {
            GameObject canvasObj = new GameObject("UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Font defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 20);

            // Top-Left: Health & Lucky Last (Compact folk horror plate)
            GameObject topLeft = CreateUIObject("TopLeftHUD", canvasObj.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, 48), new Vector2(24, -20));
            CreateImage(topLeft, "PanelBg", new Color(0.06f, 0.07f, 0.10f, 0.80f));

            // [HP ICON]
            Text hpIcon = CreateText(topLeft, "HpIcon", "♥", 13, TextAnchor.MiddleCenter, new Color(0.90f, 0.20f, 0.20f), defaultFont);
            RectTransform rtHpIcon = hpIcon.rectTransform;
            rtHpIcon.anchorMin = new Vector2(0, 1);
            rtHpIcon.anchorMax = new Vector2(0, 1);
            rtHpIcon.pivot = new Vector2(0, 1);
            rtHpIcon.sizeDelta = new Vector2(20, 20);
            rtHpIcon.anchoredPosition = new Vector2(8, -6);

            // Health bar: 170x14 px (dark charcoal bg, deep blood crimson fill)
            GameObject hpBg = CreateImage(topLeft, "HealthBg", new Color(0.12f, 0.13f, 0.17f, 0.95f));
            RectTransform rtHpBg = hpBg.GetComponent<RectTransform>();
            rtHpBg.anchorMin = new Vector2(0, 1);
            rtHpBg.anchorMax = new Vector2(0, 1);
            rtHpBg.pivot = new Vector2(0, 1);
            rtHpBg.sizeDelta = new Vector2(170, 14);
            rtHpBg.anchoredPosition = new Vector2(32, -9);

            GameObject hpFill = CreateImage(hpBg, "HealthFill", new Color(0.68f, 0.10f, 0.10f, 1f));
            Image imgHpFill = hpFill.GetComponent<Image>();
            imgHpFill.type = Image.Type.Filled;
            imgHpFill.fillMethod = Image.FillMethod.Horizontal;
            imgHpFill.fillAmount = 1f;
            RectTransform rtHpFill = hpFill.GetComponent<RectTransform>();
            rtHpFill.anchorMin = Vector2.zero;
            rtHpFill.anchorMax = Vector2.one;
            rtHpFill.sizeDelta = Vector2.zero;

            Text hpText = CreateText(hpBg, "HealthText", "100 / 100", 11, TextAnchor.MiddleCenter, new Color(0.96f, 0.95f, 0.92f), defaultFont);
            hpText.fontStyle = FontStyle.Bold;

            // Lucky Last: below health bar: "★ 1" (clean minimalist)
            Text luckyText = CreateText(topLeft, "LuckyChargesText", "★ 1", 13, TextAnchor.MiddleLeft, new Color(0.96f, 0.76f, 0.26f), defaultFont);
            luckyText.fontStyle = FontStyle.Bold;
            RectTransform rtLucky = luckyText.rectTransform;
            rtLucky.anchorMin = new Vector2(0, 0);
            rtLucky.anchorMax = new Vector2(0, 0);
            rtLucky.pivot = new Vector2(0, 0);
            rtLucky.sizeDelta = new Vector2(120, 18);
            rtLucky.anchoredPosition = new Vector2(32, 4);

            // Lucky Last Banner
            GameObject luckyBanner = CreateUIObject("LuckyLastBanner", canvasObj.transform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(360, 48), Vector2.zero);
            CreateImage(luckyBanner, "BannerBg", new Color(0.75f, 0.12f, 0.12f, 0.90f));
            Text bannerText = CreateText(luckyBanner, "BannerText", "★ LUCKY LAST ACTIVE! ★", 20, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.3f), defaultFont);
            luckyBanner.SetActive(false);

            // Top-Center: Wave & Survival Timer Pill (integrated folk horror HUD)
            GameObject topCenter = CreateUIObject("TopCenterHUD", canvasObj.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(210, 50), new Vector2(0, -32));
            CreateImage(topCenter, "TimerBg", new Color(0.06f, 0.07f, 0.10f, 0.85f));

            Text waveText = CreateText(topCenter, "WaveText", "WAVE 1", 15, TextAnchor.MiddleCenter, new Color(0.96f, 0.76f, 0.26f), defaultFont);
            waveText.fontStyle = FontStyle.Bold;
            waveText.rectTransform.anchoredPosition = new Vector2(-48, 10);

            Text timerText = CreateText(topCenter, "TimerText", "00:00", 14, TextAnchor.MiddleCenter, new Color(0.94f, 0.93f, 0.88f), defaultFont);
            timerText.fontStyle = FontStyle.Bold;
            timerText.rectTransform.anchoredPosition = new Vector2(48, 10);

            Text waveRemText = CreateText(topCenter, "WaveRemainingText", "8 REMAINING", 12, TextAnchor.MiddleCenter, new Color(0.90f, 0.88f, 0.82f), defaultFont);
            waveRemText.fontStyle = FontStyle.Bold;
            waveRemText.rectTransform.anchoredPosition = new Vector2(0, -11);

            // Top-Right: Dedicated Container inside SafeArea (anchoredPosition -28, -24, size 180x90)
            GameObject topRight = CreateUIObject("TopRightHUD", canvasObj.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 90), new Vector2(-28, -24));

            // Isolated HUD Pause Button (46x46 at top-right corner, local 0, 0)
            GameObject pauseBtnObj = CreateButton(topRight.transform, "HudPauseButton", "||", new Color(0.12f, 0.14f, 0.18f, 0.90f), defaultFont);
            RectTransform rtPauseBtn = pauseBtnObj.GetComponent<RectTransform>();
            rtPauseBtn.anchorMin = new Vector2(1, 1);
            rtPauseBtn.anchorMax = new Vector2(1, 1);
            rtPauseBtn.pivot = new Vector2(1, 1);
            rtPauseBtn.sizeDelta = new Vector2(46, 46);
            rtPauseBtn.anchoredPosition = Vector2.zero;
            Text txtPause = pauseBtnObj.GetComponentInChildren<Text>();
            if (txtPause != null)
            {
                txtPause.fontSize = 22;
                txtPause.fontStyle = FontStyle.Bold;
                txtPause.color = new Color(0.95f, 0.85f, 0.40f);
            }
            Button hudPauseBtn = pauseBtnObj.GetComponent<Button>();

            // Cursed Seeds Badge: 84x32 pill to the left of pause button (local -54, -7)
            GameObject seedBadge = CreateUIObject("SeedBadge", topRight.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(84, 32), new Vector2(-54, -7));
            CreateImage(seedBadge, "StatsBg", new Color(0.06f, 0.07f, 0.10f, 0.85f));
            Text seedText = CreateText(seedBadge, "SeedText", "✦ 0", 15, TextAnchor.MiddleCenter, new Color(0.35f, 0.95f, 0.55f), defaultFont);
            seedText.fontStyle = FontStyle.Bold;

            // Status Effects Container: 180x32 below buttons at local (0, -52), right aligned
            GameObject statusEffects = CreateUIObject("StatusEffectsContainer", topRight.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 32), new Vector2(0, -52));
            HorizontalLayoutGroup hlgStatus = statusEffects.AddComponent<HorizontalLayoutGroup>();
            hlgStatus.childAlignment = TextAnchor.MiddleRight;
            hlgStatus.spacing = 6;
            hlgStatus.childControlWidth = false;
            hlgStatus.childControlHeight = false;

            // Hidden kill counter element so UIManager reference is satisfied without cluttering top HUD
            GameObject hiddenKill = CreateUIObject("HiddenKill", canvasObj.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            hiddenKill.SetActive(false);
            Text killText = CreateText(hiddenKill, "KillText", "0", 1, TextAnchor.MiddleCenter, Color.clear, defaultFont);

            // Bottom: XP Spirit Line & Level (65% screen width, 10px height, moved up to y = 24)
            GameObject bottomBar = CreateUIObject("BottomHUD", canvasObj.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(1250, 10), new Vector2(0, 24));
            GameObject xpBg = CreateImage(bottomBar, "XpBg", new Color(0.06f, 0.07f, 0.10f, 0.85f));
            RectTransform rtXpBg = xpBg.GetComponent<RectTransform>();
            rtXpBg.anchorMin = Vector2.zero;
            rtXpBg.anchorMax = Vector2.one;
            rtXpBg.sizeDelta = Vector2.zero;

            GameObject xpFill = CreateImage(xpBg, "XpFill", new Color(0.18f, 0.58f, 0.70f, 1f));
            Image imgXpFill = xpFill.GetComponent<Image>();
            imgXpFill.type = Image.Type.Filled;
            imgXpFill.fillMethod = Image.FillMethod.Horizontal;
            imgXpFill.fillAmount = 0f;
            RectTransform rtXpFill = xpFill.GetComponent<RectTransform>();
            rtXpFill.anchorMin = Vector2.zero;
            rtXpFill.anchorMax = Vector2.one;
            rtXpFill.sizeDelta = Vector2.zero;

            Text lvlText = CreateText(bottomBar, "LevelText", "LVL 1", 13, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.45f), defaultFont);
            lvlText.fontStyle = FontStyle.Bold;
            lvlText.rectTransform.anchoredPosition = new Vector2(0, 16);

            // Level Up Modal (90% uniform dark overlay, safe area margins, adaptive responsive layout)
            GameObject lvlModal = CreateUIObject("LevelUpModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(lvlModal, "DarkOverlay", new Color(0.03f, 0.04f, 0.06f, 0.92f)); // 92% uniform dark overlay

            // Dedicated LevelUpSafeArea container with relative margins (6% horizontal, 8% vertical)
            GameObject safeArea = CreateUIObject("LevelUpSafeArea", lvlModal.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            Text lvlTitle = CreateText(safeArea, "Title", "CHOOSE AN UPGRADE", 38, TextAnchor.MiddleCenter, new Color(0.96f, 0.95f, 0.92f), defaultFont);
            lvlTitle.fontStyle = FontStyle.Bold;
            lvlTitle.rectTransform.sizeDelta = new Vector2(850, 52);
            lvlTitle.rectTransform.anchoredPosition = new Vector2(0, 255);

            Text lvlSub = CreateText(safeArea, "Subtitle", "Death whispers of renewed power...", 18, TextAnchor.MiddleCenter, new Color(0.68f, 0.75f, 0.82f), defaultFont);
            lvlSub.rectTransform.sizeDelta = new Vector2(750, 28);
            lvlSub.rectTransform.anchoredPosition = new Vector2(0, 218);

            GameObject cardsContainer = CreateUIObject("CardsContainer", safeArea.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1020, 395), new Vector2(0, -6));
            HorizontalLayoutGroup hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            UpgradeCardUI[] cardUIs = new UpgradeCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                cardUIs[i] = CreateUpgradeCard(cardsContainer.transform, i + 1, defaultFont);
            }

            GameObject rerollBtnObj = CreateButton(safeArea.transform, "RerollButton", "REROLL — 1 LEFT [R]", new Color(0.12f, 0.13f, 0.17f), defaultFont);
            RectTransform rtReroll = rerollBtnObj.GetComponent<RectTransform>();
            rtReroll.anchoredPosition = new Vector2(0, -242);
            rtReroll.sizeDelta = new Vector2(320, 46);
            Button btnReroll = rerollBtnObj.GetComponent<Button>();
            Text txtReroll = rerollBtnObj.GetComponentInChildren<Text>();
            if (txtReroll != null)
            {
                txtReroll.fontSize = 19;
                txtReroll.fontStyle = FontStyle.Bold;
                txtReroll.color = new Color(0.95f, 0.93f, 0.88f); // ivory
            }

            lvlModal.SetActive(false);

            // Game Over Modal
            GameObject goModal = CreateUIObject("GameOverModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(goModal, "DarkOverlay", new Color(0.2f, 0f, 0f, 0.85f));
            Text goTitle = CreateText(goModal, "Title", "RUN OVER", 48, TextAnchor.MiddleCenter, Color.red, defaultFont);
            goTitle.rectTransform.anchoredPosition = new Vector2(0, 160);

            Text goStats = CreateText(goModal, "StatsText", "TIME: 00:00\nKILLS: 0\nLEVEL: 1\n\nFINAL SCORE: 0", 26, TextAnchor.MiddleCenter, Color.white, defaultFont);
            goStats.rectTransform.anchoredPosition = new Vector2(0, 20);

            GameObject retryBtnObj = CreateButton(goModal.transform, "RetryButton", "RETRY RUN", new Color(0.2f, 0.6f, 0.3f), defaultFont);
            retryBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);
            Button btnRetry = retryBtnObj.GetComponent<Button>();

            goModal.SetActive(false);

            // Boss HUD
            GameObject bossBarRoot = CreateUIObject("BossHUD", canvasObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(600, 50), new Vector2(0, -90));
            GameObject bossBg = CreateImage(bossBarRoot, "BossBg", new Color(0.1f, 0.1f, 0.15f, 0.9f));
            RectTransform rtBossBg = bossBg.GetComponent<RectTransform>();
            rtBossBg.sizeDelta = new Vector2(600, 24);
            rtBossBg.anchoredPosition = new Vector2(0, -12);

            GameObject bossFill = CreateImage(bossBg, "BossFill", new Color(0.85f, 0.15f, 0.15f, 1f));
            Image imgBossFill = bossFill.GetComponent<Image>();
            imgBossFill.type = Image.Type.Filled;
            imgBossFill.fillMethod = Image.FillMethod.Horizontal;
            imgBossFill.fillAmount = 1f;
            RectTransform rtBossFill = bossFill.GetComponent<RectTransform>();
            rtBossFill.anchorMin = Vector2.zero;
            rtBossFill.anchorMax = Vector2.one;
            rtBossFill.sizeDelta = Vector2.zero;

            Text bossName = CreateText(bossBarRoot, "BossName", "THE LAST HARVEST", 20, TextAnchor.MiddleCenter, Color.red, defaultFont);
            bossName.rectTransform.anchoredPosition = new Vector2(0, 10);
            bossBarRoot.SetActive(false);

            // Victory / Endless Modal
            GameObject vicModal = CreateUIObject("VictoryModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(vicModal, "DarkOverlay", new Color(0.05f, 0.15f, 0.05f, 0.88f));

            Text vicTitle = CreateText(vicModal, "Title", "YOU GOT LUCKY.", 44, TextAnchor.MiddleCenter, Color.yellow, defaultFont);
            vicTitle.rectTransform.sizeDelta = new Vector2(850, 70);
            vicTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            vicTitle.rectTransform.anchoredPosition = new Vector2(0, 140);

            Text vicSub = CreateText(vicModal, "Sub", "THE LAST HARVEST HAS FALLEN!\n\nONE LAST NIGHT?", 24, TextAnchor.MiddleCenter, Color.white, defaultFont);
            vicSub.rectTransform.sizeDelta = new Vector2(850, 120);
            vicSub.rectTransform.anchoredPosition = new Vector2(0, 20);

            GameObject vicBtnObj = CreateButton(vicModal.transform, "EndlessButton", "ENTER ONE LAST NIGHT [ENTER]", new Color(0.2f, 0.7f, 0.3f), defaultFont);
            vicBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            vicBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 55);
            Text btnVicText = vicBtnObj.GetComponentInChildren<Text>();
            if (btnVicText != null)
            {
                btnVicText.rectTransform.sizeDelta = new Vector2(460, 55);
                btnVicText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            Button btnVic = vicBtnObj.GetComponent<Button>();

            vicModal.SetActive(false);

            // Title / Instructions Modal
            GameObject titleModal = CreateUIObject("TitleModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(titleModal, "DarkOverlay", new Color(0.04f, 0.04f, 0.08f, 0.94f));

            Text titleHeader = CreateText(titleModal, "Title", "CURSED HARVEST: LUCKY LAST", 44, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.1f), defaultFont);
            titleHeader.rectTransform.anchoredPosition = new Vector2(0, 240);

            Text titleSubtitle = CreateText(titleModal, "Subtitle", "Synty Spooky Jam Oct 2026", 20, TextAnchor.MiddleCenter, new Color(0.9f, 0.8f, 0.4f), defaultFont);
            titleSubtitle.rectTransform.anchoredPosition = new Vector2(0, 195);

            string guide = "HOW TO SURVIVE:\n\n" +
                           "• [W][A][S][D]: 360° Movement\n" +
                           "• [SPACE] / [SHIFT]: Dash with invulnerability\n" +
                           "• WEAPONS AUTO-AIM & FIRE AT FOES CONTINUOUSLY\n" +
                           "• Slay enemies to gather EXP (Level Up) & Cursed Seeds\n" +
                           "• Approach GRAVESTONES to plant seeds & gamble:\n" +
                           "   - 70% LUCKY: EXP bursts, HP heals, lethal damage buffs\n" +
                           "   - 30% CURSED: Monster ambushes, toxic puddles, slow\n" +
                           "• LUCKY LAST: Fatal hits are intercepted! Enter an 8s\n" +
                           "   berserk frenzy with +50% damage & +50% attack speed!\n" +
                           "• Survive 12 minutes to face 'THE LAST HARVEST' boss!";

            Text titleGuide = CreateText(titleModal, "GuideText", guide, 20, TextAnchor.MiddleLeft, Color.white, defaultFont);
            titleGuide.rectTransform.sizeDelta = new Vector2(850, 280);
            titleGuide.rectTransform.anchoredPosition = new Vector2(0, -10);

            GameObject startBtnObj = CreateButton(titleModal.transform, "StartRunButton", "START RUN [SPACE / CLICK]", new Color(0.85f, 0.35f, 0.1f), defaultFont);
            startBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -210);
            startBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 52);
            Button btnStart = startBtnObj.GetComponent<Button>();

            GameObject creditsBtnObj = CreateButton(titleModal.transform, "CreditsButton", "CREDITS [C]", new Color(0.22f, 0.25f, 0.32f), defaultFont);
            creditsBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -270);
            creditsBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 44);
            Button btnCredits = creditsBtnObj.GetComponent<Button>();

            titleModal.SetActive(false);

            // Credits Modal
            GameObject credModal = CreateUIObject("CreditsModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(credModal, "DarkOverlay", new Color(0.04f, 0.04f, 0.08f, 0.95f));

            Text credHeader = CreateText(credModal, "Title", "CURSED HARVEST: LUCKY LAST", 36, TextAnchor.MiddleCenter, new Color(1f, 0.65f, 0.15f), defaultFont);
            credHeader.fontStyle = FontStyle.Bold;
            credHeader.rectTransform.anchoredPosition = new Vector2(0, 240);

            Text credSubtitle = CreateText(credModal, "Subtitle", "Synty Spooky Jam — October 2026 — Theme: \"Lucky Last\"", 18, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.5f), defaultFont);
            credSubtitle.rectTransform.anchoredPosition = new Vector2(0, 195);

            string creditsText =
                "<b>3D ART & ASSETS:</b>\n" +
                "• Synty Studios — POLYGON Generic, Bow & Crossbow, Halloween Masks, Pumpkins, Simple Sky, Simple FX\n\n" +
                "<b>BACKGROUND MUSIC (CC BY 4.0):</b>\n" +
                "• \"Echoes of Time v2\" by Kevin MacLeod (incompetech.com)\n" +
                "• \"Grave Blow\" by Kevin MacLeod (incompetech.com)\n" +
                "• \"The Dread\" by Kevin MacLeod (incompetech.com)\n" +
                "  Licensed under Creative Commons: By Attribution 4.0 License\n" +
                "  http://creativecommons.org/licenses/by/4.0/\n\n" +
                "<b>AUDIO & SFX:</b>\n" +
                "• Procedural Synthesis & Audio Engine by Antigravity / Cursed Harvest Team\n\n" +
                "<b>GAME DESIGN & CODE:</b>\n" +
                "• Built for Synty Spooky Jam Oct 2026 in Unity 6";

            Text credBody = CreateText(credModal, "CreditsContent", creditsText, 16, TextAnchor.MiddleLeft, new Color(0.9f, 0.92f, 0.95f), defaultFont);
            credBody.rectTransform.sizeDelta = new Vector2(880, 320);
            credBody.rectTransform.anchoredPosition = new Vector2(0, 0);

            GameObject closeCredBtnObj = CreateButton(credModal.transform, "CloseCreditsButton", "CLOSE [ESC / C]", new Color(0.75f, 0.25f, 0.15f), defaultFont);
            closeCredBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -220);
            closeCredBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 48);
            Button btnCloseCred = closeCredBtnObj.GetComponent<Button>();

            credModal.SetActive(false);

            // Hook into UIManager
            UIManager uim = canvasObj.AddComponent<UIManager>();
            SerializedObject soUim = new SerializedObject(uim);
            soUim.FindProperty("healthBarFill").objectReferenceValue = imgHpFill;
            soUim.FindProperty("healthText").objectReferenceValue = hpText;
            soUim.FindProperty("luckyChargesText").objectReferenceValue = luckyText;
            soUim.FindProperty("luckyLastBanner").objectReferenceValue = luckyBanner;
            soUim.FindProperty("luckyLastTimerText").objectReferenceValue = bannerText;
            soUim.FindProperty("timerText").objectReferenceValue = timerText;
            soUim.FindProperty("waveText").objectReferenceValue = waveText;
            soUim.FindProperty("waveRemainingText").objectReferenceValue = waveRemText;
            soUim.FindProperty("xpBarFill").objectReferenceValue = imgXpFill;
            soUim.FindProperty("levelText").objectReferenceValue = lvlText;
            soUim.FindProperty("killCountText").objectReferenceValue = killText;
            soUim.FindProperty("seedCountText").objectReferenceValue = seedText;
            soUim.FindProperty("hudPauseButton").objectReferenceValue = hudPauseBtn;
            soUim.FindProperty("topRightHUDContainer").objectReferenceValue = topRight.GetComponent<RectTransform>();
            soUim.FindProperty("seedBadgeRoot").objectReferenceValue = seedBadge.GetComponent<RectTransform>();
            soUim.FindProperty("statusEffectsContainer").objectReferenceValue = statusEffects.GetComponent<RectTransform>();

            soUim.FindProperty("levelUpModal").objectReferenceValue = lvlModal;
            SerializedProperty propCards = soUim.FindProperty("upgradeCards");
            propCards.arraySize = 3;
            for (int i = 0; i < 3; i++) propCards.GetArrayElementAtIndex(i).objectReferenceValue = cardUIs[i];
            soUim.FindProperty("rerollButton").objectReferenceValue = btnReroll;
            soUim.FindProperty("rerollButtonText").objectReferenceValue = txtReroll;

            soUim.FindProperty("gameOverModal").objectReferenceValue = goModal;
            soUim.FindProperty("gameOverStatsText").objectReferenceValue = goStats;
            soUim.FindProperty("retryButton").objectReferenceValue = btnRetry;

            soUim.FindProperty("bossBarRoot").objectReferenceValue = bossBarRoot;
            soUim.FindProperty("bossBarFill").objectReferenceValue = imgBossFill;
            soUim.FindProperty("bossNameText").objectReferenceValue = bossName;

            soUim.FindProperty("victoryModal").objectReferenceValue = vicModal;
            soUim.FindProperty("victoryTitleText").objectReferenceValue = vicTitle;
            soUim.FindProperty("victorySubtitleText").objectReferenceValue = vicSub;
            soUim.FindProperty("continueEndlessButton").objectReferenceValue = btnVic;

            soUim.FindProperty("titleModal").objectReferenceValue = titleModal;
            soUim.FindProperty("startRunButton").objectReferenceValue = btnStart;
            soUim.FindProperty("openCreditsButton").objectReferenceValue = btnCredits;

            soUim.FindProperty("creditsModal").objectReferenceValue = credModal;
            soUim.FindProperty("closeCreditsButton").objectReferenceValue = btnCloseCred;

            soUim.ApplyModifiedProperties();
        }

        private static UpgradeCardUI CreateUpgradeCard(Transform parent, int slotIndex, Font font)
        {
            // Responsive 315x390 card with antique copper border (#614d33) fitting 16:9, 16:10, and 4:3 viewports
            GameObject cardObj = CreateUIObject($"Card_{slotIndex}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(315, 390), Vector2.zero);
            CreateImage(cardObj, "CardBorder", new Color(0.38f, 0.30f, 0.20f, 1f));

            // Inner body inset by 2px (dark charcoal #14171c)
            GameObject bodyObj = CreateUIObject("CardBody", cardObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform rtBody = bodyObj.GetComponent<RectTransform>();
            rtBody.offsetMin = new Vector2(2, 2);
            rtBody.offsetMax = new Vector2(-2, -2);
            CreateImage(bodyObj, "BodyBg", new Color(0.08f, 0.09f, 0.11f, 0.95f));

            // Badge at top y = -14 (290x26) - font size 16 bold
            Text cat = CreateText(bodyObj, "Category", "★ NEW WEAPON ★", 16, TextAnchor.MiddleCenter, new Color(0.25f, 0.95f, 0.50f), font);
            cat.fontStyle = FontStyle.Bold;
            RectTransform rtCat = cat.rectTransform;
            rtCat.anchorMin = new Vector2(0.5f, 1f);
            rtCat.anchorMax = new Vector2(0.5f, 1f);
            rtCat.pivot = new Vector2(0.5f, 1f);
            rtCat.sizeDelta = new Vector2(290, 26);
            rtCat.anchoredPosition = new Vector2(0, -14);

            // Title at y = -46 (290x38): font size 22 bold ivory
            Text title = CreateText(bodyObj, "Title", "Upgrade Title", 22, TextAnchor.MiddleCenter, new Color(0.96f, 0.95f, 0.90f), font);
            title.fontStyle = FontStyle.Bold;
            RectTransform rtTitle = title.rectTransform;
            rtTitle.anchorMin = new Vector2(0.5f, 1f);
            rtTitle.anchorMax = new Vector2(0.5f, 1f);
            rtTitle.pivot = new Vector2(0.5f, 1f);
            rtTitle.sizeDelta = new Vector2(290, 38);
            rtTitle.anchoredPosition = new Vector2(0, -46);

            // Horizontal 2px divider at y = -86 (280x2)
            GameObject divObj = CreateImage(bodyObj, "Divider", new Color(0.29f, 0.24f, 0.17f, 0.90f));
            RectTransform rtDiv = divObj.GetComponent<RectTransform>();
            rtDiv.anchorMin = new Vector2(0.5f, 1f);
            rtDiv.anchorMax = new Vector2(0.5f, 1f);
            rtDiv.pivot = new Vector2(0.5f, 1f);
            rtDiv.sizeDelta = new Vector2(280, 2);
            rtDiv.anchoredPosition = new Vector2(0, -86);

            // Description at y = -96 (280x135): font size 17, soft ivory (#dcd7cc), wrapped cleanly
            Text desc = CreateText(bodyObj, "Description", "+25% Damage", 17, TextAnchor.UpperCenter, new Color(0.88f, 0.86f, 0.82f), font);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Truncate;
            desc.lineSpacing = 1.15f;
            RectTransform rtDesc = desc.rectTransform;
            rtDesc.anchorMin = new Vector2(0.5f, 1f);
            rtDesc.anchorMax = new Vector2(0.5f, 1f);
            rtDesc.pivot = new Vector2(0.5f, 1f);
            rtDesc.sizeDelta = new Vector2(280, 135);
            rtDesc.anchoredPosition = new Vector2(0, -96);

            // Level text at y = 66 from bottom (280x24): font size 16 bold aged gold
            Text lvl = CreateText(bodyObj, "Level", "LEVEL 1 / 5", 16, TextAnchor.MiddleCenter, new Color(0.85f, 0.72f, 0.32f), font);
            lvl.fontStyle = FontStyle.Bold;
            RectTransform rtLvl = lvl.rectTransform;
            rtLvl.anchorMin = new Vector2(0.5f, 0f);
            rtLvl.anchorMax = new Vector2(0.5f, 0f);
            rtLvl.pivot = new Vector2(0.5f, 0f);
            rtLvl.sizeDelta = new Vector2(280, 24);
            rtLvl.anchoredPosition = new Vector2(0, 66);

            // Select button at y = 12 from bottom (270x46): label SELECT [{slotIndex}], font size 19 bold
            GameObject btnObj = CreateButton(bodyObj.transform, "SelectButton", $"SELECT [{slotIndex}]", new Color(0.12f, 0.14f, 0.18f), font);
            RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
            rtBtn.anchorMin = new Vector2(0.5f, 0f);
            rtBtn.anchorMax = new Vector2(0.5f, 0f);
            rtBtn.pivot = new Vector2(0.5f, 0f);
            rtBtn.anchoredPosition = new Vector2(0, 12);
            rtBtn.sizeDelta = new Vector2(270, 46);
            Text btnTxt = btnObj.GetComponentInChildren<Text>();
            if (btnTxt != null)
            {
                btnTxt.fontSize = 19;
                btnTxt.fontStyle = FontStyle.Bold;
                btnTxt.color = new Color(0.96f, 0.95f, 0.90f);
            }

            UpgradeCardUI card = cardObj.AddComponent<UpgradeCardUI>();
            SerializedObject so = new SerializedObject(card);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("categoryText").objectReferenceValue = cat;
            so.FindProperty("descriptionText").objectReferenceValue = desc;
            so.FindProperty("levelText").objectReferenceValue = lvl;
            so.FindProperty("selectButton").objectReferenceValue = btnObj.GetComponent<Button>();
            so.ApplyModifiedProperties();

            return card;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return obj;
        }

        private static Sprite _whiteSprite;
        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite == null)
            {
                string path = "Assets/_Textures/UI_White.png";
                if (!File.Exists(path))
                {
                    if (!Directory.Exists("Assets/_Textures")) Directory.CreateDirectory("Assets/_Textures");
                    Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                    Color[] pixels = new Color[32 * 32];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }

                _whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return _whiteSprite;
        }

        private static GameObject CreateImage(GameObject parent, string name, Color color, Sprite customSprite = null)
        {
            GameObject imgObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            imgObj.transform.SetParent(parent.transform, false);
            Image img = imgObj.GetComponent<Image>();
            img.sprite = customSprite != null ? customSprite : GetWhiteSprite();
            img.color = color;
            RectTransform rt = imgObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return imgObj;
        }

        private static Text CreateText(GameObject parent, string name, string text, int fontSize, TextAnchor alignment, Color color, Font font)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(parent.transform, false);
            Text t = txtObj.GetComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.font = font;
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 50);
            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color, Font font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = color;
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 50);

            CreateText(btnObj, "Text", label, 20, TextAnchor.MiddleCenter, Color.white, font);
            return btnObj;
        }

        private static void BuildGroundAndPaths(Transform arenaRoot)
        {
            GameObject groundParent = new GameObject("GroundAndPaths");
            groundParent.transform.SetParent(arenaRoot, false);

            // ==========================================
            // 1. CONTINUOUS BASE GROUND (86m x 86m)
            // ==========================================
            // Eliminates all voids/black holes completely.
            GameObject basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            basePlane.name = "ContinuousBaseGround";
            basePlane.transform.SetParent(groundParent.transform, false);
            basePlane.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            basePlane.transform.localScale = new Vector3(8.6f, 1f, 8.6f); // 86m x 86m

            // Remove default mesh collider; arena root BoxCollider handles physics
            MeshCollider planeCollider = basePlane.GetComponent<MeshCollider>();
            if (planeCollider != null) UnityEngine.Object.DestroyImmediate(planeCollider);

            // Material: Textured Synty ground material
            string groundMatPath = "Assets/_Materials/M_ContinuousBaseGround.mat";
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
            if (groundMat == null)
            {
                if (!Directory.Exists("Assets/_Materials")) Directory.CreateDirectory("Assets/_Materials");
                Material genericDirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat");
                if (genericDirt != null)
                {
                    groundMat = new Material(genericDirt);
                    groundMat.name = "M_ContinuousBaseGround";
                    groundMat.SetTextureScale("_Albedo_Map", new Vector2(16f, 16f));
                    groundMat.SetTextureScale("_Normal_Map", new Vector2(16f, 16f));
                    groundMat.SetColor("_BaseColor", new Color(0.65f, 0.58f, 0.48f)); // Dark weathered earth, never black
                    AssetDatabase.CreateAsset(groundMat, groundMatPath);
                }
            }
            if (groundMat != null)
            {
                MeshRenderer mr = basePlane.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = groundMat;
            }

            // ==========================================
            // 2. MODULAR SURFACE DETAILS & OVERLAYS
            // ==========================================
            // Mesh variations from Synty PolygonGeneric
            GameObject grassLarge1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Grass_Large_01.prefab");
            GameObject grassLarge2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Grass_Large_02.prefab");
            GameObject grassLarge3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Grass_Large_03.prefab");

            GameObject dirtLarge1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_Large_01.prefab");
            GameObject dirtLarge2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_Large_02.prefab");
            GameObject dirtLarge3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_Large_03.prefab");

            GameObject gravelRoad1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Road_Gravel_Straight_01.prefab");
            GameObject gravelRoad3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Road_Gravel_Straight_03.prefab");

            GameObject[] grassPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_03.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_Tall_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Grass_Tall_04.prefab")
            };
            List<GameObject> validGrass = new List<GameObject>();
            foreach (var g in grassPrefabs) if (g != null) validGrass.Add(g);

            GameObject leaves1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Leaves_Pile_01.prefab");
            GameObject leaves2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Leaves_Pile_02.prefab");

            GameObject pebbles1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_Pebbles_01.prefab");
            GameObject pebbles3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_Pebbles_03.prefab");
            GameObject pebbles5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_Pebbles_05.prefab");

            // Dense 5x5 grid of modular terrain mounds with overlap covering [-30 to +30]
            float[] gridSteps = new float[] { -30f, -15f, 0f, 15f, 30f };
            for (int xi = 0; xi < gridSteps.Length; xi++)
            {
                for (int zi = 0; zi < gridSteps.Length; zi++)
                {
                    float gx = gridSteps[xi];
                    float gz = gridSteps[zi];
                    Vector3 tilePos = new Vector3(gx, 0f, gz);
                    float distFromCenter = tilePos.magnitude;

                    GameObject tileSource;
                    if (distFromCenter < 12f)
                    {
                        // Center hub: dirt variation 1 and 2
                        tileSource = ((xi + zi) % 2 == 0) ? (dirtLarge1 ?? dirtLarge2) : (dirtLarge2 ?? dirtLarge1);
                    }
                    else
                    {
                        // Near harvest plots or outer perimeter: mix of dirtLarge3 and grassLarge1/2/3
                        int seed = Mathf.Abs(xi * 5 + zi * 11);
                        if (seed % 3 == 0) tileSource = dirtLarge3 ?? dirtLarge2;
                        else if (seed % 3 == 1) tileSource = grassLarge2 ?? grassLarge1;
                        else tileSource = grassLarge3 ?? grassLarge1;
                    }

                    if (tileSource != null)
                    {
                        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tileSource, groundParent.transform);
                        tile.transform.position = tilePos;
                        int rotStep = Mathf.Abs((xi * 3 + zi * 7)) % 4;
                        tile.transform.rotation = Quaternion.Euler(0f, rotStep * 90f, 0f);
                        tile.transform.localScale = new Vector3(1.15f, 1.0f, 1.15f);
                    }
                }
            }

            // ==========================================
            // 3. ORGANIC HARVEST PLOT BLENDING
            // ==========================================
            Vector3[] targetPlots = new Vector3[]
            {
                new Vector3(14f, 0f, 14f),
                new Vector3(-14f, 0f, 14f),
                new Vector3(-14f, 0f, -14f),
                new Vector3(14f, 0f, -14f)
            };
            GameObject dirtPatchSource = dirtLarge1 ?? dirtLarge2;
            if (dirtPatchSource != null)
            {
                for (int p = 0; p < targetPlots.Length; p++)
                {
                    GameObject plotDirt = (GameObject)PrefabUtility.InstantiatePrefab(dirtPatchSource, groundParent.transform);
                    plotDirt.transform.position = targetPlots[p] + new Vector3(0f, 0.01f, 0f);
                    plotDirt.transform.rotation = Quaternion.Euler(0f, p * 45f, 0f);
                    plotDirt.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
                }
            }

            // ==========================================
            // 4. DIAGONAL GRAVEL PATHS
            // ==========================================
            GameObject gravelSource = gravelRoad1 ?? gravelRoad3;
            if (gravelSource != null)
            {
                foreach (var plotPos in targetPlots)
                {
                    Vector3 dir = plotPos.normalized;
                    Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                    for (float d = 4.0f; d <= 11.0f; d += 4.0f)
                    {
                        GameObject road = (GameObject)PrefabUtility.InstantiatePrefab(gravelSource, groundParent.transform);
                        road.transform.position = dir * d + new Vector3(0f, 0.02f, 0f);
                        road.transform.rotation = rot;
                        road.transform.localScale = new Vector3(0.95f, 1f, 1.05f);
                    }
                }
            }

            // 3. Ground detail scatters (30+ dead grass tufts, leaf piles, and pebble clusters)
            Vector3[] detailPositions = new Vector3[]
            {
                new Vector3(3.5f, 0.02f, 2.0f), new Vector3(-3.0f, 0.02f, 4.5f),
                new Vector3(-4.5f, 0.02f, -3.2f), new Vector3(5.5f, 0.02f, -3.8f),
                new Vector3(8.0f, 0.02f, 6.5f), new Vector3(-7.2f, 0.02f, 7.8f),
                new Vector3(-8.5f, 0.02f, -6.8f), new Vector3(7.5f, 0.02f, -7.5f),
                new Vector3(11.5f, 0.02f, 2.5f), new Vector3(-10.8f, 0.02f, 2.2f),
                new Vector3(2.2f, 0.02f, 11.5f), new Vector3(-3.2f, 0.02f, -10.5f),
                new Vector3(12.5f, 0.02f, 16.0f), new Vector3(16.0f, 0.02f, 12.0f),
                new Vector3(-12.5f, 0.02f, 16.0f), new Vector3(-16.0f, 0.02f, 12.0f),
                new Vector3(-12.5f, 0.02f, -16.0f), new Vector3(-16.0f, 0.02f, -12.0f),
                new Vector3(12.5f, 0.02f, -16.0f), new Vector3(16.0f, 0.02f, -12.0f),
                new Vector3(0.5f, 0.02f, 17.5f), new Vector3(-0.5f, 0.02f, -17.5f),
                new Vector3(17.5f, 0.02f, 0.5f), new Vector3(-17.5f, 0.02f, -0.5f),
                new Vector3(9.5f, 0.02f, 9.5f), new Vector3(-9.5f, 0.02f, 9.5f),
                new Vector3(-9.5f, 0.02f, -9.5f), new Vector3(9.5f, 0.02f, -9.5f),
                new Vector3(21.0f, 0.02f, 5.0f), new Vector3(-21.0f, 0.02f, -5.0f),
                new Vector3(5.0f, 0.02f, -21.0f), new Vector3(-5.0f, 0.02f, 21.0f)
            };

            for (int i = 0; i < detailPositions.Length; i++)
            {
                GameObject detailPrefab = null;
                int category = i % 3;
                if (category == 0 && validGrass.Count > 0)
                {
                    detailPrefab = validGrass[i % validGrass.Count];
                }
                else if (category == 1)
                {
                    detailPrefab = (i % 2 == 0) ? (leaves1 ?? leaves2) : (leaves2 ?? leaves1);
                }
                else
                {
                    detailPrefab = (i % 3 == 0) ? (pebbles1 ?? pebbles3) : ((i % 3 == 1) ? (pebbles3 ?? pebbles5) : pebbles5);
                }

                if (detailPrefab != null)
                {
                    GameObject scatter = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, groundParent.transform);
                    scatter.transform.position = detailPositions[i];
                    scatter.transform.rotation = Quaternion.Euler(0f, (i * 53f) % 360f, 0f);
                    scatter.transform.localScale = Vector3.one * (0.85f + (i % 4) * 0.15f);
                }
            }
        }

        private static void RemoveColliders(GameObject go)
        {
            if (go == null) return;
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            foreach (var c in cols) UnityEngine.Object.DestroyImmediate(c);
        }

        public static float SampleGroundHeight(Vector3 pos)
        {
            Physics.SyncTransforms();
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, 20f, pos.z), Vector3.down, out hit, 40f))
            {
                return hit.point.y;
            }
            return 0f;
        }

        private static void CreateRusticFence(Transform parent, Vector3 startPos, Vector3 endPos, GameObject logPrefab, GameObject plank1, GameObject plank2, bool isBroken)
        {
            if (logPrefab == null || plank1 == null) return;
            Vector3 diff = endPos - startPos;
            float length = diff.magnitude;
            if (length < 0.1f) return;
            Vector3 dir = diff / length;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Vertical post 1
            GameObject post1 = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, parent);
            post1.transform.position = startPos;
            post1.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-3f, 3f));
            post1.transform.localScale = new Vector3(0.22f, 0.70f, 0.22f);
            RemoveColliders(post1);

            // Vertical post 2
            GameObject post2 = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, parent);
            post2.transform.position = endPos;
            post2.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-3f, 3f));
            post2.transform.localScale = new Vector3(0.22f, 0.70f, 0.22f);
            RemoveColliders(post2);

            // Top rail plank
            Vector3 mid = (startPos + endPos) * 0.5f;
            GameObject topPlank = (GameObject)PrefabUtility.InstantiatePrefab(plank1, parent);
            topPlank.transform.position = mid + new Vector3(0f, isBroken ? 0.22f : 0.46f, 0f);
            topPlank.transform.rotation = rot * Quaternion.Euler(isBroken ? 18f : UnityEngine.Random.Range(-3f, 3f), 90f, UnityEngine.Random.Range(-3f, 3f));
            topPlank.transform.localScale = new Vector3(length * 0.33f, 0.75f, 0.85f);
            RemoveColliders(topPlank);

            // Bottom rail plank
            GameObject btmPlank = (GameObject)PrefabUtility.InstantiatePrefab(plank2 ?? plank1, parent);
            btmPlank.transform.position = mid + new Vector3(0f, 0.18f, 0f);
            btmPlank.transform.rotation = rot * Quaternion.Euler(UnityEngine.Random.Range(-4f, 4f), 90f, UnityEngine.Random.Range(-3f, 3f));
            btmPlank.transform.localScale = new Vector3(length * 0.31f, 0.75f, 0.85f);
            RemoveColliders(btmPlank);
        }

        private static void CreatePumpkinCluster(Transform parent, Vector3 centerPos, GameObject unlitPrefab, GameObject litPrefab, int count, float radius)
        {
            if (unlitPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 pos = centerPos + new Vector3(circle.x, 0f, circle.y);
                pos.y = SampleGroundHeight(pos);
                // 1 out of 4 can be a soft lit lantern, the rest unlit
                bool isLit = litPrefab != null && (i == 0 && UnityEngine.Random.value < 0.25f);
                GameObject prefab = isLit ? litPrefab : unlitPrefab;

                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                p.transform.position = pos;
                float yaw = UnityEngine.Random.Range(0f, 360f);
                float tiltX = UnityEngine.Random.Range(-8f, 8f);
                float tiltZ = UnityEngine.Random.Range(-8f, 8f);
                p.transform.rotation = Quaternion.Euler(tiltX, yaw, tiltZ);
                float scale = UnityEngine.Random.Range(0.55f, 0.85f);
                p.transform.localScale = Vector3.one * scale;
                RemoveColliders(p);

                if (isLit)
                {
                    GameObject pLight = new GameObject("SoftClusterLanternLight");
                    pLight.transform.SetParent(p.transform, false);
                    pLight.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    Light l = pLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.55f, 0.18f); // Soft warm amber
                    l.range = 3.2f;
                    l.intensity = 0.9f;
                }
            }
        }

        private static void SpawnInnerRingCluster(Transform parent, Vector3 centerPos, GameObject[] prefabs, Vector3[] localOffsets, float[] scales)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == null) continue;
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], parent);
                Vector3 off = i < localOffsets.Length ? localOffsets[i] : Vector3.zero;
                Vector3 targetPos = centerPos + off;
                targetPos.y = SampleGroundHeight(targetPos) + off.y;
                item.transform.position = targetPos;
                item.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                float sc = i < scales.Length ? scales[i] : 1f;
                item.transform.localScale = Vector3.one * sc;
                RemoveColliders(item);
            }
        }

        private static void BuildAtmosphericLandmarks(Transform arenaRoot, GameObject plotPrefab)
        {
            GameObject landmarksParent = new GameObject("AtmosphericLandmarks");
            landmarksParent.transform.SetParent(arenaRoot, false);

            // Load verified authentic Synty Polygon assets
            GameObject fogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Fog_01.prefab");
            if (fogPrefab == null) fogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Steam.prefab");

            GameObject lanternLitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject lanternUnlitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab");

            GameObject logPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");
            GameObject logPrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_02.prefab");
            GameObject logPilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_Pile_01.prefab");
            GameObject plankPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab");
            GameObject plankPrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_02.prefab");

            GameObject deadTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab");
            GameObject deadTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab");
            GameObject deadTree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab");
            GameObject pineTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_01.prefab");
            GameObject pineTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_02.prefab");

            GameObject mossyRock8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab");
            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab");
            GameObject rock5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_05.prefab");
            GameObject rock7 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_07.prefab");

            GameObject root1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Root_01.prefab");
            GameObject stump1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_01.prefab");
            GameObject stump2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_02.prefab");
            GameObject mushroom1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_01.prefab");
            GameObject mushroom2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_02.prefab");
            GameObject shrub1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Shrub_01.prefab");
            GameObject shrub2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Shrub_02.prefab");
            GameObject vines1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Vines_01.prefab");

            GameObject dirtPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_01.prefab");
            GameObject leavesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Leaves_01.prefab");
            GameObject pebbles1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_Pebbles_01.prefab");
            GameObject pebbles3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_Pebbles_03.prefab");
            GameObject twigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Twig_02.prefab");

            GameObject statue1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Statue_01.prefab");
            GameObject statue4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Statue_04.prefab");
            GameObject plinth1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plinth_01.prefab");
            GameObject plinth2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plinth_02.prefab");
            GameObject skullPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Skull_01.prefab");
            GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Chest_01.prefab");

            GameObject cratePreset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_Preset_01.prefab");
            GameObject barrel1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_01.prefab");
            GameObject barrel2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_02.prefab");
            GameObject barrel3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_03.prefab");
            GameObject sack1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Sack_01.prefab");
            GameObject sackStack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Sack_Stack_01.prefab");
            GameObject pot1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Pot_01.prefab");
            GameObject pot2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Pot_02.prefab");
            GameObject pot3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Pot_03.prefab");

            GameObject spadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Spade_01.prefab");
            GameObject axePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab");
            GameObject pickaxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");
            GameObject ropePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Rope_01.prefab");
            GameObject ropeKnot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Rope_Knot_01.prefab");

            // ==========================================
            // 1. QUADRANT NW: ANCIENT DEAD FOREST & OAK
            // ==========================================
            if (deadTree1 != null)
            {
                GameObject bigTree = (GameObject)PrefabUtility.InstantiatePrefab(deadTree1, landmarksParent.transform);
                bigTree.transform.position = new Vector3(-18f, 0f, 18f);
                bigTree.transform.rotation = Quaternion.Euler(0f, 130f, 0f);
                bigTree.transform.localScale = Vector3.one * 1.9f;
            }
            if (deadTree2 != null)
            {
                GameObject bgTree1 = (GameObject)PrefabUtility.InstantiatePrefab(deadTree2, landmarksParent.transform);
                bgTree1.transform.position = new Vector3(-23f, 0f, 20f);
                bgTree1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                bgTree1.transform.localScale = Vector3.one * 1.45f;
            }
            if (pineTree1 != null)
            {
                GameObject bgPine = (GameObject)PrefabUtility.InstantiatePrefab(pineTree1, landmarksParent.transform);
                bgPine.transform.position = new Vector3(-16f, 0f, 24f);
                bgPine.transform.rotation = Quaternion.Euler(0f, 85f, 0f);
                bgPine.transform.localScale = Vector3.one * 1.6f;
            }
            if (mossyRock8 != null)
            {
                GameObject r1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyRock8, landmarksParent.transform);
                r1.transform.position = new Vector3(-19.5f, 0f, 17f);
                r1.transform.localScale = Vector3.one * 1.25f;
            }
            if (rock4 != null)
            {
                GameObject r2 = (GameObject)PrefabUtility.InstantiatePrefab(rock4, landmarksParent.transform);
                r2.transform.position = new Vector3(-16.8f, 0f, 19.5f);
                r2.transform.localScale = Vector3.one * 0.95f;
            }
            if (root1 != null)
            {
                GameObject rootObj = (GameObject)PrefabUtility.InstantiatePrefab(root1, landmarksParent.transform);
                rootObj.transform.position = new Vector3(-17.2f, 0.05f, 17.2f);
                rootObj.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                rootObj.transform.localScale = Vector3.one * 1.2f;
                RemoveColliders(rootObj);
            }
            if (stump2 != null)
            {
                GameObject st = (GameObject)PrefabUtility.InstantiatePrefab(stump2, landmarksParent.transform);
                st.transform.position = new Vector3(-15.5f, 0f, 19.5f);
                st.transform.localScale = Vector3.one * 1.1f;
                RemoveColliders(st);
            }
            if (mushroom1 != null)
            {
                GameObject m1 = (GameObject)PrefabUtility.InstantiatePrefab(mushroom1, landmarksParent.transform);
                m1.transform.position = new Vector3(-15.8f, 0.15f, 19.2f);
                m1.transform.localScale = Vector3.one * 0.8f;
                RemoveColliders(m1);
            }
            if (lanternUnlitPrefab != null)
            {
                GameObject p1 = (GameObject)PrefabUtility.InstantiatePrefab(lanternUnlitPrefab, landmarksParent.transform);
                p1.transform.position = new Vector3(-18.6f, 0.1f, 16.8f);
                p1.transform.rotation = Quaternion.Euler(5f, 40f, -5f);
                p1.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(p1);

                GameObject p2 = (GameObject)PrefabUtility.InstantiatePrefab(lanternUnlitPrefab, landmarksParent.transform);
                p2.transform.position = new Vector3(-19.2f, 0.05f, 17.4f);
                p2.transform.rotation = Quaternion.Euler(0f, 120f, 10f);
                p2.transform.localScale = Vector3.one * 0.65f;
                RemoveColliders(p2);
            }
            if (fogPrefab != null)
            {
                GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                mist.transform.position = new Vector3(-18f, 0.05f, 18f);
                mist.transform.localScale = new Vector3(3.4f, 0.6f, 3.4f);
            }

            // ==========================================
            // 2. QUADRANT NE: DESECRATED CEMETERY & SHRINE
            // ==========================================
            if (plinth1 != null && statue1 != null)
            {
                GameObject plinthObj = (GameObject)PrefabUtility.InstantiatePrefab(plinth1, landmarksParent.transform);
                plinthObj.transform.position = new Vector3(18f, 0f, 18f);
                plinthObj.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
                plinthObj.transform.localScale = Vector3.one * 1.2f;

                GameObject statueObj = (GameObject)PrefabUtility.InstantiatePrefab(statue1, landmarksParent.transform);
                statueObj.transform.position = new Vector3(18f, 0.6f, 18f);
                statueObj.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
                statueObj.transform.localScale = Vector3.one * 1.35f;
            }
            if (statue4 != null)
            {
                GameObject statueKneel = (GameObject)PrefabUtility.InstantiatePrefab(statue4, landmarksParent.transform);
                statueKneel.transform.position = new Vector3(21f, -0.15f, 19f);
                statueKneel.transform.rotation = Quaternion.Euler(5f, 195f, -8f);
                statueKneel.transform.localScale = Vector3.one * 0.95f;
            }
            if (chestPrefab != null)
            {
                GameObject chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, landmarksParent.transform);
                chest.transform.position = new Vector3(19.5f, 0f, 15.5f);
                chest.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
                chest.transform.localScale = Vector3.one * 0.9f;
                RemoveColliders(chest);
            }
            if (skullPrefab != null)
            {
                GameObject skull = (GameObject)PrefabUtility.InstantiatePrefab(skullPrefab, landmarksParent.transform);
                skull.transform.position = new Vector3(18.3f, 0.85f, 17.9f);
                skull.transform.rotation = Quaternion.Euler(-10f, 160f, 5f);
                skull.transform.localScale = Vector3.one * 0.65f;
                RemoveColliders(skull);
            }
            if (rock5 != null)
            {
                GameObject slab1 = (GameObject)PrefabUtility.InstantiatePrefab(rock5, landmarksParent.transform);
                slab1.transform.position = new Vector3(16.8f, 0f, 17.2f);
                slab1.transform.rotation = Quaternion.Euler(0f, 35f, 10f);
                slab1.transform.localScale = Vector3.one * 0.9f;
                RemoveColliders(slab1);
            }
            // Broken cemetery rustic fence
            CreateRusticFence(landmarksParent.transform, new Vector3(16.0f, 0f, 19.5f), new Vector3(19.0f, 0f, 21.0f), logPrefab1, plankPrefab1, plankPrefab2, true);

            if (lanternLitPrefab != null)
            {
                GameObject lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternLitPrefab, landmarksParent.transform);
                lantern.transform.position = new Vector3(19.2f, 0f, 16.8f);
                lantern.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(lantern);

                GameObject pLight = new GameObject("CemeteryPumpkinLight");
                pLight.transform.SetParent(lantern.transform, false);
                pLight.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                Light l = pLight.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.55f, 0.15f);
                l.range = 6.5f;
                l.intensity = 2.0f;
            }
            if (fogPrefab != null)
            {
                GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                mist.transform.position = new Vector3(18f, 0.05f, 18f);
                mist.transform.localScale = new Vector3(3.2f, 0.6f, 3.2f);
            }

            // ==========================================
            // 3. QUADRANT SW: ABANDONED FARM DEPOT
            // ==========================================
            if (cratePreset != null)
            {
                GameObject crates = (GameObject)PrefabUtility.InstantiatePrefab(cratePreset, landmarksParent.transform);
                crates.transform.position = new Vector3(-18f, 0f, -18f);
                crates.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                crates.transform.localScale = Vector3.one * 1.15f;
            }
            if (barrel1 != null)
            {
                GameObject b1 = (GameObject)PrefabUtility.InstantiatePrefab(barrel1, landmarksParent.transform);
                b1.transform.position = new Vector3(-19.5f, 0f, -17.2f);
                b1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            }
            if (barrel2 != null)
            {
                GameObject b2 = (GameObject)PrefabUtility.InstantiatePrefab(barrel2, landmarksParent.transform);
                b2.transform.position = new Vector3(-16.8f, 0f, -19.0f);
                b2.transform.rotation = Quaternion.Euler(0f, 75f, 85f);
            }
            if (sackStack != null)
            {
                GameObject sacks = (GameObject)PrefabUtility.InstantiatePrefab(sackStack, landmarksParent.transform);
                sacks.transform.position = new Vector3(-17.2f, 0f, -16.5f);
                sacks.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
                RemoveColliders(sacks);
            }
            if (spadePrefab != null)
            {
                GameObject spade = (GameObject)PrefabUtility.InstantiatePrefab(spadePrefab, landmarksParent.transform);
                spade.transform.position = new Vector3(-19.3f, 0.1f, -16.8f);
                spade.transform.rotation = Quaternion.Euler(0f, 30f, -35f);
                spade.transform.localScale = Vector3.one * 0.85f;
                RemoveColliders(spade);
            }
            if (pot1 != null)
            {
                GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(pot1, landmarksParent.transform);
                pot.transform.position = new Vector3(-16.0f, 0f, -17.5f);
                pot.transform.rotation = Quaternion.Euler(15f, 45f, 0f);
                pot.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(pot);
            }
            if (lanternUnlitPrefab != null)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(lanternUnlitPrefab, landmarksParent.transform);
                p.transform.position = new Vector3(-17.6f, 0f, -15.8f);
                p.transform.rotation = Quaternion.Euler(0f, 50f, 0f);
                p.transform.localScale = Vector3.one * 0.7f;
                RemoveColliders(p);
            }
            if (deadTree3 != null)
            {
                GameObject bgTree = (GameObject)PrefabUtility.InstantiatePrefab(deadTree3, landmarksParent.transform);
                bgTree.transform.position = new Vector3(-23f, 0f, -21f);
                bgTree.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
                bgTree.transform.localScale = Vector3.one * 1.4f;
            }
            if (fogPrefab != null)
            {
                GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                mist.transform.position = new Vector3(-18f, 0.05f, -18f);
                mist.transform.localScale = new Vector3(3.0f, 0.5f, 3.0f);
            }

            // ==========================================
            // 4. QUADRANT SE: WOODCUTTER'S QUARRY
            // ==========================================
            if (rock4 != null)
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rock4, landmarksParent.transform);
                rock.transform.position = new Vector3(19f, 0f, -19f);
                rock.transform.rotation = Quaternion.Euler(-5f, 40f, 10f);
                rock.transform.localScale = Vector3.one * 1.45f;
            }
            if (stump1 != null)
            {
                GameObject stump = (GameObject)PrefabUtility.InstantiatePrefab(stump1, landmarksParent.transform);
                stump.transform.position = new Vector3(17f, 0f, -17f);
                stump.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
                stump.transform.localScale = Vector3.one * 1.15f;

                if (axePrefab != null)
                {
                    GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(axePrefab, stump.transform);
                    axe.transform.localPosition = new Vector3(0.05f, 0.45f, 0f);
                    axe.transform.localRotation = Quaternion.Euler(0f, 0f, -40f);
                    axe.transform.localScale = Vector3.one * 0.85f;
                    RemoveColliders(axe);
                }
            }
            if (logPilePrefab != null)
            {
                GameObject logs = (GameObject)PrefabUtility.InstantiatePrefab(logPilePrefab, landmarksParent.transform);
                logs.transform.position = new Vector3(16.5f, 0f, -19f);
                logs.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                logs.transform.localScale = Vector3.one * 1.05f;
            }
            if (logPrefab1 != null)
            {
                GameObject l1 = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab1, landmarksParent.transform);
                l1.transform.position = new Vector3(15.2f, 0f, -17.5f);
                l1.transform.rotation = Quaternion.Euler(0f, 80f, 0f);
                l1.transform.localScale = Vector3.one * 0.8f;
                RemoveColliders(l1);
            }
            if (pickaxePrefab != null)
            {
                GameObject pick = (GameObject)PrefabUtility.InstantiatePrefab(pickaxePrefab, landmarksParent.transform);
                pick.transform.position = new Vector3(19.8f, 0.1f, -18.2f);
                pick.transform.rotation = Quaternion.Euler(0f, 45f, 25f);
                pick.transform.localScale = Vector3.one * 0.85f;
                RemoveColliders(pick);
            }
            if (ropePrefab != null)
            {
                GameObject rope = (GameObject)PrefabUtility.InstantiatePrefab(ropePrefab, landmarksParent.transform);
                rope.transform.position = new Vector3(16.8f, 0.35f, -18.8f);
                rope.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                rope.transform.localScale = Vector3.one * 0.9f;
                RemoveColliders(rope);
            }
            if (pineTree2 != null)
            {
                GameObject bgPine = (GameObject)PrefabUtility.InstantiatePrefab(pineTree2, landmarksParent.transform);
                bgPine.transform.position = new Vector3(23f, 0f, -21f);
                bgPine.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                bgPine.transform.localScale = Vector3.one * 1.5f;
            }
            if (fogPrefab != null)
            {
                GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                mist.transform.position = new Vector3(18f, 0.05f, -18f);
                mist.transform.localScale = new Vector3(3.0f, 0.5f, 3.0f);
            }

            // ==========================================
            // 5. THE 4 HARVEST PLOTS (BEACONS & FRAMING)
            // ==========================================
            Vector3[] plotPositions = new Vector3[]
            {
                new Vector3(14f, 0f, 14f),   // NE
                new Vector3(-14f, 0f, 14f),  // NW
                new Vector3(-14f, 0f, -14f), // SW
                new Vector3(14f, 0f, -14f)   // SE
            };

            for (int pIdx = 0; pIdx < plotPositions.Length; pIdx++)
            {
                Vector3 pPos = plotPositions[pIdx];
                pPos.y = SampleGroundHeight(pPos);

                if (plotPrefab != null)
                {
                    GameObject plotObj = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab, arenaRoot);
                    plotObj.transform.position = pPos;
                }

                // Tilled dark earth foundation beneath each plot
                if (dirtPrefab != null)
                {
                    GameObject pDirt = (GameObject)PrefabUtility.InstantiatePrefab(dirtPrefab, landmarksParent.transform);
                    pDirt.transform.position = pPos + new Vector3(0f, 0.02f, 0f);
                    pDirt.transform.localScale = new Vector3(0.68f, 0.68f, 0.68f);
                    RemoveColliders(pDirt);
                }

                // Plot-specific thematic dressing
                if (pIdx == 1) // NW Plot: Blighted vines & rustic fence
                {
                    CreateRusticFence(landmarksParent.transform, pPos + new Vector3(-2.2f, 0f, 0.8f), pPos + new Vector3(-0.6f, 0f, 2.2f), logPrefab1, plankPrefab1, plankPrefab2, true);
                    if (vines1 != null)
                    {
                        GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(vines1, landmarksParent.transform);
                        v.transform.position = pPos + new Vector3(-1.8f, 0.1f, 1.2f);
                        v.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                        v.transform.localScale = Vector3.one * 0.9f;
                        RemoveColliders(v);
                    }
                    if (root1 != null)
                    {
                        GameObject rt = (GameObject)PrefabUtility.InstantiatePrefab(root1, landmarksParent.transform);
                        rt.transform.position = pPos + new Vector3(1.4f, 0.02f, 1.4f);
                        rt.transform.rotation = Quaternion.Euler(0f, 120f, 0f);
                        rt.transform.localScale = Vector3.one * 0.6f;
                        RemoveColliders(rt);
                    }
                }
                else if (pIdx == 0) // NE Plot: Shrine stone & pot
                {
                    CreateRusticFence(landmarksParent.transform, pPos + new Vector3(-0.8f, 0f, 2.2f), pPos + new Vector3(1.6f, 0f, 1.8f), logPrefab1, plankPrefab1, plankPrefab2, false);
                    if (plinth2 != null)
                    {
                        GameObject pl = (GameObject)PrefabUtility.InstantiatePrefab(plinth2, landmarksParent.transform);
                        pl.transform.position = pPos + new Vector3(1.8f, 0f, -1.2f);
                        pl.transform.localScale = Vector3.one * 0.7f;
                        RemoveColliders(pl);
                    }
                    if (pebbles1 != null)
                    {
                        GameObject pb = (GameObject)PrefabUtility.InstantiatePrefab(pebbles1, landmarksParent.transform);
                        pb.transform.position = pPos + new Vector3(1.2f, 0.02f, -1.6f);
                        pb.transform.localScale = Vector3.one * 0.65f;
                        RemoveColliders(pb);
                    }
                }
                else if (pIdx == 2) // SW Plot: Spade & sack
                {
                    CreateRusticFence(landmarksParent.transform, pPos + new Vector3(-2.0f, 0f, -1.0f), pPos + new Vector3(-0.5f, 0f, -2.2f), logPrefab1, plankPrefab1, plankPrefab2, false);
                    if (spadePrefab != null)
                    {
                        GameObject sp = (GameObject)PrefabUtility.InstantiatePrefab(spadePrefab, landmarksParent.transform);
                        sp.transform.position = pPos + new Vector3(-2.1f, 0.05f, -0.9f);
                        sp.transform.rotation = Quaternion.Euler(0f, 30f, -30f);
                        sp.transform.localScale = Vector3.one * 0.8f;
                        RemoveColliders(sp);
                    }
                    if (plankPrefab1 != null)
                    {
                        GameObject pl = (GameObject)PrefabUtility.InstantiatePrefab(plankPrefab1, landmarksParent.transform);
                        pl.transform.position = pPos + new Vector3(1.5f, 0.02f, -1.2f);
                        pl.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
                        pl.transform.localScale = Vector3.one * 0.75f;
                        RemoveColliders(pl);
                    }
                }
                else // SE Plot: Timber rail & leaves
                {
                    CreateRusticFence(landmarksParent.transform, pPos + new Vector3(0.6f, 0f, -2.2f), pPos + new Vector3(2.2f, 0f, -0.8f), logPrefab1, plankPrefab1, plankPrefab2, true);
                    if (pot2 != null)
                    {
                        GameObject pot = (GameObject)PrefabUtility.InstantiatePrefab(pot2, landmarksParent.transform);
                        pot.transform.position = pPos + new Vector3(1.9f, 0f, -1.2f);
                        pot.transform.localScale = Vector3.one * 0.75f;
                        RemoveColliders(pot);
                    }
                    if (leavesPrefab != null)
                    {
                        GameObject lv = (GameObject)PrefabUtility.InstantiatePrefab(leavesPrefab, landmarksParent.transform);
                        lv.transform.position = pPos + new Vector3(-1.4f, 0.02f, 1.4f);
                        lv.transform.localScale = Vector3.one * 0.8f;
                        RemoveColliders(lv);
                    }
                }

                // Ambient corner pumpkin lantern at each plot (soft accent, does not wash out ritual state light)
                if (lanternLitPrefab != null)
                {
                    GameObject lantern = (GameObject)PrefabUtility.InstantiatePrefab(lanternLitPrefab, landmarksParent.transform);
                    lantern.transform.position = pPos + new Vector3(-1.6f, 0f, -1.6f);
                    lantern.transform.rotation = Quaternion.Euler(0f, 30f + pIdx * 90f, 0f);
                    lantern.transform.localScale = Vector3.one * 0.70f;
                    RemoveColliders(lantern);

                    GameObject pLight = new GameObject("PlotPumpkinLight");
                    pLight.transform.SetParent(lantern.transform, false);
                    pLight.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                    Light l = pLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.58f, 0.18f);
                    l.range = 4.2f;
                    l.intensity = 1.0f;
                }

                // Unlit decorative pumpkins resting by plot perimeter
                if (lanternUnlitPrefab != null)
                {
                    GameObject up = (GameObject)PrefabUtility.InstantiatePrefab(lanternUnlitPrefab, landmarksParent.transform);
                    up.transform.position = pPos + new Vector3(-1.95f, 0f, -1.25f);
                    up.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(0f, 360f), 0f);
                    up.transform.localScale = Vector3.one * 0.62f;
                    RemoveColliders(up);
                }

                if (fogPrefab != null)
                {
                    GameObject mist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                    mist.transform.position = pPos + new Vector3(0f, 0.05f, 0f);
                    mist.transform.localScale = new Vector3(2.6f, 0.5f, 2.6f);
                }
            }

            // ==========================================
            // CENTER ARENA DRESSING & SUBTLE LANDMARK
            // ==========================================
            GameObject centerParent = new GameObject("CenterArenaDressing");
            centerParent.transform.SetParent(landmarksParent.transform, false);

            // 1. Subtle Central Landmark (Height < 0.35m, 0 colliders, open kiting)
            // Sunken rural ritual plinth / ancient stone base embedded in dark tilled dirt
            float gy = SampleGroundHeight(Vector3.zero);

            if (dirtPrefab != null)
            {
                GameObject cDirt = (GameObject)PrefabUtility.InstantiatePrefab(dirtPrefab, centerParent.transform);
                cDirt.transform.position = new Vector3(0f, gy + 0.02f, 0f);
                cDirt.transform.localScale = new Vector3(0.72f, 0.72f, 0.72f);
                RemoveColliders(cDirt);
            }

            if (plinth2 != null)
            {
                GameObject cPlinth = (GameObject)PrefabUtility.InstantiatePrefab(plinth2, centerParent.transform);
                cPlinth.transform.position = new Vector3(0f, gy + 0.04f, 0f);
                cPlinth.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                cPlinth.transform.localScale = new Vector3(0.85f, 0.42f, 0.85f);
                RemoveColliders(cPlinth);
            }

            // Flat perimeter stone slabs & pebbles around central rim
            Vector3[] centerStoneOffsets = new Vector3[]
            {
                new Vector3(0.95f, 0.02f, 0.95f),
                new Vector3(-0.95f, 0.02f, 0.95f),
                new Vector3(-0.95f, 0.02f, -0.95f),
                new Vector3(0.95f, 0.02f, -0.95f)
            };
            foreach (var stoneOff in centerStoneOffsets)
            {
                GameObject sPrefab = pebbles1 ?? rock4;
                if (sPrefab != null)
                {
                    GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(sPrefab, centerParent.transform);
                    Vector3 sPos = stoneOff;
                    sPos.y = SampleGroundHeight(sPos) + 0.02f;
                    s.transform.position = sPos;
                    s.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    s.transform.localScale = Vector3.one * 0.7f;
                    RemoveColliders(s);
                }
            }

            // Autumn dead leaves, flat rusted tool, and unlit pumpkin on the central slab
            if (leavesPrefab != null)
            {
                GameObject l = (GameObject)PrefabUtility.InstantiatePrefab(leavesPrefab, centerParent.transform);
                l.transform.position = new Vector3(0.2f, gy + 0.14f, -0.15f);
                l.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                l.transform.localScale = Vector3.one * 0.7f;
                RemoveColliders(l);
            }
            if (pickaxePrefab != null)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pickaxePrefab, centerParent.transform);
                p.transform.position = new Vector3(0.05f, gy + 0.16f, 0.12f);
                p.transform.rotation = Quaternion.Euler(0f, 35f, 90f);
                p.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(p);
            }
            if (lanternUnlitPrefab != null)
            {
                GameObject up = (GameObject)PrefabUtility.InstantiatePrefab(lanternUnlitPrefab, centerParent.transform);
                up.transform.position = new Vector3(0.72f, gy + 0.08f, -0.65f);
                up.transform.rotation = Quaternion.Euler(6f, 220f, -4f);
                up.transform.localScale = Vector3.one * 0.65f;
                RemoveColliders(up);
            }
            if (fogPrefab != null)
            {
                GameObject cMist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, centerParent.transform);
                cMist.transform.position = new Vector3(0f, gy + 0.08f, 0f);
                cMist.transform.localScale = new Vector3(1.6f, 0.35f, 1.6f);
            }

            // 2. Inner Ring Micro-Clusters (r = 6m - 18m, 9 micro-clusters of 2-4 items, 0 colliders)
            // Cluster 1: NNE Diagonal (4.5, 0, 9.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(4.5f, 0f, 9.5f),
                new GameObject[] { dirtPrefab, pebbles3 ?? rock2, plankPrefab1 },
                new Vector3[] { Vector3.zero, new Vector3(0.4f, 0.02f, -0.3f), new Vector3(-0.35f, 0.02f, 0.35f) },
                new float[] { 0.55f, 0.75f, 0.8f });

            // Cluster 2: ENE Diagonal (9.5, 0, 4.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(9.5f, 0f, 4.5f),
                new GameObject[] { leavesPrefab, pot1, shrub1 },
                new Vector3[] { Vector3.zero, new Vector3(0.35f, 0f, -0.3f), new Vector3(-0.4f, 0f, 0.25f) },
                new float[] { 0.75f, 0.65f, 0.45f });

            // Cluster 3: ESE Diagonal (9.5, 0, -4.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(9.5f, 0f, -4.5f),
                new GameObject[] { pebbles1 ?? rock4, ropePrefab, spadePrefab },
                new Vector3[] { Vector3.zero, new Vector3(-0.35f, 0.02f, 0.3f), new Vector3(0.3f, 0.04f, -0.25f) },
                new float[] { 0.7f, 0.6f, 0.75f });

            // Cluster 4: SSE Diagonal (4.5, 0, -9.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(4.5f, 0f, -9.5f),
                new GameObject[] { dirtPrefab, plankPrefab2, leavesPrefab },
                new Vector3[] { Vector3.zero, new Vector3(0.4f, 0.02f, 0.25f), new Vector3(-0.3f, 0.02f, -0.35f) },
                new float[] { 0.55f, 0.8f, 0.7f });

            // Cluster 5: SSW Diagonal (-4.5, 0, -9.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(-4.5f, 0f, -9.5f),
                new GameObject[] { skullPrefab, pebbles3 ?? rock5, twigPrefab ?? plankPrefab1 },
                new Vector3[] { new Vector3(0f, -0.05f, 0f), new Vector3(0.4f, 0.02f, -0.25f), new Vector3(-0.35f, 0.02f, 0.3f) },
                new float[] { 0.8f, 0.7f, 0.7f });

            // Cluster 6: WSW Diagonal (-9.5, 0, -4.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(-9.5f, 0f, -4.5f),
                new GameObject[] { root1, dirtPrefab, pebbles1 ?? rock4 },
                new Vector3[] { Vector3.zero, new Vector3(0.35f, -0.02f, 0.35f), new Vector3(-0.3f, 0.02f, -0.3f) },
                new float[] { 0.5f, 0.55f, 0.7f });

            // Cluster 7: WNW Diagonal (-9.5, 0, 4.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(-9.5f, 0f, 4.5f),
                new GameObject[] { pot2, leavesPrefab, plankPrefab1 },
                new Vector3[] { Vector3.zero, new Vector3(-0.35f, 0.02f, -0.3f), new Vector3(0.4f, 0.02f, 0.25f) },
                new float[] { 0.65f, 0.7f, 0.8f });

            // Cluster 8: NNW Diagonal (-4.5, 0, 9.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(-4.5f, 0f, 9.5f),
                new GameObject[] { pebbles1 ?? rock2, shrub2 ?? shrub1, lanternUnlitPrefab },
                new Vector3[] { Vector3.zero, new Vector3(0.4f, 0f, 0.3f), new Vector3(-0.35f, 0.02f, -0.35f) },
                new float[] { 0.7f, 0.45f, 0.6f });

            // Cluster 9: North Axis Midpoint (0, 0, 11.5)
            SpawnInnerRingCluster(centerParent.transform, new Vector3(0f, 0f, 11.5f),
                new GameObject[] { dirtPrefab, plankPrefab2, leavesPrefab },
                new Vector3[] { Vector3.zero, new Vector3(0.45f, 0.02f, -0.2f), new Vector3(-0.35f, 0.02f, 0.3f) },
                new float[] { 0.5f, 0.85f, 0.75f });

            // ==========================================
            // NON-INTERACTIVE DECORATIVE PUMPKIN CLUSTERS
            // ==========================================
            GameObject pumpkinClustersParent = new GameObject("DecorativePumpkinClusters");
            pumpkinClustersParent.transform.SetParent(landmarksParent.transform, false);

            Vector3[] clusterLocations = new Vector3[]
            {
                new Vector3(16.5f, 0f, 8.5f),    // NE path bend near fence
                new Vector3(-4.0f, 0f, 19.0f),   // North path entrance near rustic stone
                new Vector3(-18.5f, 0f, 10.0f),  // NW blighted perimeter near dead tree
                new Vector3(-19.0f, 0f, -4.5f),  // West path edge near timber pile
                new Vector3(-10.5f, 0f, -17.5f), // SW farm quadrant near rustic fence
                new Vector3(5.5f, 0f, -19.0f),   // South path entrance near fence rail
                new Vector3(18.5f, 0f, -9.5f),   // SE quadrant near weathered plinth
                new Vector3(19.0f, 0f, 4.0f)     // East path bend near old pine tree
            };

            foreach (var cLoc in clusterLocations)
            {
                int pCount = UnityEngine.Random.Range(3, 5);
                CreatePumpkinCluster(pumpkinClustersParent.transform, cLoc, lanternUnlitPrefab, lanternLitPrefab, pCount, 1.2f);
            }

            // ==========================================
            // 6. CARDINAL NARRATIVE VIGNETTES
            // ==========================================
            // North Waystation (0, 0, 20.5)
            if (spadePrefab != null)
            {
                GameObject sp = (GameObject)PrefabUtility.InstantiatePrefab(spadePrefab, landmarksParent.transform);
                sp.transform.position = new Vector3(0.5f, 0.05f, 20.5f);
                sp.transform.rotation = Quaternion.Euler(0f, 15f, -25f);
                sp.transform.localScale = Vector3.one * 0.85f;
                RemoveColliders(sp);
            }
            if (lanternLitPrefab != null)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(lanternLitPrefab, landmarksParent.transform);
                p.transform.position = new Vector3(-0.6f, 0f, 20.8f);
                p.transform.rotation = Quaternion.Euler(0f, 190f, 0f);
                p.transform.localScale = Vector3.one * 0.8f;
                RemoveColliders(p);

                GameObject pLight = new GameObject("NorthWaystationLight");
                pLight.transform.SetParent(p.transform, false);
                pLight.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                Light l = pLight.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.58f, 0.18f);
                l.range = 5.5f;
                l.intensity = 1.8f;
            }

            // South Overlook (0, 0, -20.5)
            if (barrel3 != null)
            {
                GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(barrel3, landmarksParent.transform);
                b.transform.position = new Vector3(0.6f, 0f, -20.5f);
                b.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            }
            if (plankPrefab1 != null)
            {
                GameObject pl = (GameObject)PrefabUtility.InstantiatePrefab(plankPrefab1, landmarksParent.transform);
                pl.transform.position = new Vector3(-0.5f, 0.02f, -20.2f);
                pl.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
                pl.transform.localScale = Vector3.one * 0.8f;
                RemoveColliders(pl);
            }

            // West Crossroads (-20.5, 0, 0)
            CreateRusticFence(landmarksParent.transform, new Vector3(-20.5f, 0f, -1.8f), new Vector3(-20.5f, 0f, 1.8f), logPrefab1, plankPrefab1, plankPrefab2, true);
            if (mossyRock8 != null)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(mossyRock8, landmarksParent.transform);
                r.transform.position = new Vector3(-22f, 0f, 0.8f);
                r.transform.localScale = Vector3.one * 1.1f;
            }

            // East Ancient Marker (20.5, 0, 0)
            if (plinth1 != null)
            {
                GameObject pl = (GameObject)PrefabUtility.InstantiatePrefab(plinth1, landmarksParent.transform);
                pl.transform.position = new Vector3(20.5f, 0f, 0f);
                pl.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                pl.transform.localScale = Vector3.one * 0.9f;
                RemoveColliders(pl);
            }
            if (pot3 != null)
            {
                GameObject pt = (GameObject)PrefabUtility.InstantiatePrefab(pot3, landmarksParent.transform);
                pt.transform.position = new Vector3(20.9f, 0.45f, 0.1f);
                pt.transform.localScale = Vector3.one * 0.65f;
                RemoveColliders(pt);
            }

            // ==========================================
            // 7. BACKGROUND TREE RING (PERIMETER DEPTH)
            // ==========================================
            GameObject[] bgTreePrefabs = new GameObject[] { deadTree1, deadTree2, deadTree3, pineTree1, pineTree2 };
            List<GameObject> validBgTrees = new List<GameObject>();
            foreach (var bt in bgTreePrefabs) if (bt != null) validBgTrees.Add(bt);

            if (validBgTrees.Count > 0)
            {
                float[] treeAngles = new float[] { 15f, 40f, 65f, 105f, 130f, 155f, 195f, 220f, 250f, 285f, 310f, 340f };
                for (int ti = 0; ti < treeAngles.Length; ti++)
                {
                    float rad = treeAngles[ti] * Mathf.Deg2Rad;
                    float dist = UnityEngine.Random.Range(24.5f, 27.0f);
                    Vector3 treePos = new Vector3(Mathf.Cos(rad) * dist, 0f, Mathf.Sin(rad) * dist);
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(validBgTrees[ti % validBgTrees.Count], landmarksParent.transform);
                    tree.transform.position = treePos;
                    tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.35f, 1.85f);
                }
            }

            // Perimeter Cardinal Fog Banks (Edge of playable arena)
            if (fogPrefab != null)
            {
                Vector3[] edgeFogPositions = new Vector3[]
                {
                    new Vector3(0f, 0.05f, 22f),
                    new Vector3(0f, 0.05f, -22f),
                    new Vector3(22f, 0.05f, 0f),
                    new Vector3(-22f, 0.05f, 0f)
                };
                foreach (var edgePos in edgeFogPositions)
                {
                    GameObject edgeMist = (GameObject)PrefabUtility.InstantiatePrefab(fogPrefab, landmarksParent.transform);
                    edgeMist.transform.position = edgePos;
                    edgeMist.transform.localScale = new Vector3(3.5f, 0.6f, 3.5f);
                }
            }
        }

        private static void BuildVisiblePerimeter(Transform arenaRoot)
        {
            GameObject[] cliffPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_09.prefab")
            };
            List<GameObject> validCliffs = new List<GameObject>();
            foreach (var c in cliffPrefabs) if (c != null) validCliffs.Add(c);

            GameObject[] treePrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_Generic_TreeDead_01.prefab")
            };
            List<GameObject> validTrees = new List<GameObject>();
            foreach (var t in treePrefabs) if (t != null) validTrees.Add(t);

            GameObject pumpkinLantern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");

            GameObject perimeterParent = new GameObject("VisiblePerimeterWalls");
            perimeterParent.transform.SetParent(arenaRoot, false);

            float bound = 35.0f;
            float step = 5.0f;

            void SpawnEdge(Vector3 start, Vector3 end, Vector3 inwardNormal)
            {
                int count = Mathf.RoundToInt(Vector3.Distance(start, end) / step);
                for (int i = 0; i <= count; i++)
                {
                    float t = (float)i / count;
                    Vector3 basePos = Vector3.Lerp(start, end, t);

                    // 1. Cliff Rock Wall
                    if (validCliffs.Count > 0)
                    {
                        GameObject cliffPrefab = validCliffs[i % validCliffs.Count];
                        GameObject cliff = (GameObject)PrefabUtility.InstantiatePrefab(cliffPrefab, perimeterParent.transform);
                        Vector3 jitter = Vector3.Cross(inwardNormal, Vector3.up) * UnityEngine.Random.Range(-0.5f, 0.5f);
                        cliff.transform.position = basePos + jitter;
                        cliff.transform.rotation = Quaternion.LookRotation(inwardNormal, Vector3.up) * Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(-5f, 5f));
                        cliff.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.8f);
                    }

                    // 2. Towering dark pine trees behind rock wall
                    if (validTrees.Count > 0)
                    {
                        GameObject treePrefab = validTrees[(i + 1) % validTrees.Count];
                        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, perimeterParent.transform);
                        Vector3 treePos = basePos - inwardNormal * 2.5f + Vector3.Cross(inwardNormal, Vector3.up) * UnityEngine.Random.Range(-1.5f, 1.5f);
                        tree.transform.position = treePos;
                        tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                        tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.4f, 2.2f);
                    }

                    // 3. Spooky pumpkin lanterns along rock wall (accents with emissive glow; light only at corners)
                    if (i % 3 == 0 && pumpkinLantern != null)
                    {
                        GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinLantern, perimeterParent.transform);
                        p.transform.position = basePos + inwardNormal * 0.8f;
                        p.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                        p.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.15f);

                        // Only add point light at corner posts to prevent light clutter and wash out
                        if (i == 0 || i == count)
                        {
                            GameObject lanternLight = new GameObject("CornerPumpkinLight");
                            lanternLight.transform.SetParent(p.transform, false);
                            lanternLight.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                            Light pLight = lanternLight.AddComponent<Light>();
                            pLight.type = LightType.Point;
                            pLight.color = new Color(1f, 0.55f, 0.15f);
                            pLight.range = 7.0f;
                            pLight.intensity = 1.8f;
                        }
                    }
                }
            }

            // North: Z = +bound
            SpawnEdge(new Vector3(-bound, 0, bound), new Vector3(bound, 0, bound), -Vector3.forward);
            // South: Z = -bound
            SpawnEdge(new Vector3(-bound, 0, -bound), new Vector3(bound, 0, -bound), Vector3.forward);
            // East: X = +bound
            SpawnEdge(new Vector3(bound, 0, -bound), new Vector3(bound, 0, bound), -Vector3.right);
            // West: X = -bound
            SpawnEdge(new Vector3(-bound, 0, -bound), new Vector3(-bound, 0, bound), Vector3.right);

            // Ground physical collider
            BoxCollider groundCollider = arenaRoot.GetComponent<BoxCollider>();
            if (groundCollider == null) groundCollider = arenaRoot.gameObject.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(72f, 1f, 72f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);

            // Physical boundary colliders aligned directly with rock wall faces
            CreateBoundaryWall(arenaRoot, new Vector3(0, 5, bound + 0.5f), new Vector3(74, 10, 3));
            CreateBoundaryWall(arenaRoot, new Vector3(0, 5, -bound - 0.5f), new Vector3(74, 10, 3));
            CreateBoundaryWall(arenaRoot, new Vector3(bound + 0.5f, 5, 0), new Vector3(3, 10, 74));
            CreateBoundaryWall(arenaRoot, new Vector3(-bound - 0.5f, 5, 0), new Vector3(3, 10, 74));
        }

        private static void CreateBoundaryWall(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject wall = new GameObject("BoundaryWall");
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = size;
        }

        private static ObjectPool CreatePool(Transform parent, string name, GameObject prefab, int capacity)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            ObjectPool pool = obj.AddComponent<ObjectPool>();

            SerializedObject so = new SerializedObject(pool);
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("initialCapacity").intValue = capacity;
            so.ApplyModifiedProperties();

            return pool;
        }
    }
}
