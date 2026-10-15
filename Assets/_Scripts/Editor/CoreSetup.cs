using System;
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
            GameObject boltPrefab = CreateBoltPrefab();
            GameObject bombPrefab = CreatePumpkinBombPrefab();
            GameObject xpPrefab = CreateXpPickupPrefab();
            GameObject seedPrefab = CreateCursedSeedPrefab();
            GameObject plotPrefab = CreateHarvestPlotPrefab();
            GameObject puddlePrefab = CreatePlaguePuddlePrefab();

            GameObject playerPrefab = CreatePlayerPrefab(boltPrefab);

            // Enemies
            GameObject gruntPrefab = CreatePumpkinGruntPrefab();

            GameObject chaserPrefab = CreateGenericEnemyPrefab(
                "Enemy_MaskedChaser",
                EnemyType.MaskedChaser,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Skeleton_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_LED_Mask_01.prefab",
                null,
                1.0f, 25f, 4.8f, 12f, false, 0f, false, 0f, false, null);

            GameObject plaguePrefab = CreateGenericEnemyPrefab(
                "Enemy_PlagueWalker",
                EnemyType.PlagueWalker,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Female_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab",
                null,
                1.0f, 55f, 2.7f, 10f, false, 0f, false, 0.1f, false, puddlePrefab);

            GameObject chargerPrefab = CreateGenericEnemyPrefab(
                "Enemy_DemonCharger",
                EnemyType.DemonCharger,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab",
                null,
                1.05f, 70f, 3.2f, 15f, true, 25f, false, 0.2f, false, null);

            GameObject farmerPrefab = CreateGenericEnemyPrefab(
                "Enemy_CursedFarmer",
                EnemyType.CursedFarmer,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab",
                null,
                "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab",
                1.1f, 130f, 2.2f, 26f, false, 0f, false, 0.5f, false, null);

            GameObject scarecrowPrefab = CreateGenericEnemyPrefab(
                "Enemy_EliteScarecrow",
                EnemyType.EliteScarecrow,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab",
                "Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab",
                null,
                1.45f, 380f, 2.0f, 20f, false, 0f, true, 0.75f, true, null);

            GameObject brutePrefab = CreateGenericEnemyPrefab(
                "Enemy_HarvestBrute",
                EnemyType.HarvestBrute,
                "Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab",
                "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab",
                "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab",
                1.55f, 480f, 2.1f, 32f, false, 0f, false, 1.0f, true, null);

            GameObject bossPrefab = CreateBossPrefab(bombPrefab);

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
                plotPrefab);

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

        private static GameObject CreateBoltPrefab()
        {
            string targetPath = $"{PrefabsPath}/CrossbowBolt.prefab";
            GameObject boltSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonBowCrossbow/Prefabs/SM_Wep_Crossbow_Bolt_01.prefab");

            GameObject instance = new GameObject("CrossbowBolt");
            instance.tag = "Projectile";
            int projLayer = LayerMask.NameToLayer("Projectile");
            if (projLayer >= 0) instance.layer = projLayer;

            if (boltSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(boltSource, instance.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
            }

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(0.3f, 0.3f, 1.2f);
            collider.center = new Vector3(0f, 0f, 0.4f);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            LayerMask hitMask = enemyLayer >= 0 ? (1 << enemyLayer) : ~0;

            Projectile proj = instance.AddComponent<Projectile>();
            proj.SetHitMask(hitMask);
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

        private static GameObject CreateHarvestPlotPrefab()
        {
            string targetPath = $"{PrefabsPath}/HarvestPlot.prefab";
            GameObject dirtSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Dirt_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject luckyVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Fireworks_Green_Large.prefab");
            GameObject cursedVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            GameObject instance = new GameObject("HarvestPlot");
            instance.tag = "HarvestPlot";

            if (dirtSource != null)
            {
                GameObject dirt = (GameObject)PrefabUtility.InstantiatePrefab(dirtSource, instance.transform);
                dirt.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                dirt.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }

            // Growing pumpkin child
            GameObject growingPumpkin = new GameObject("GrowingPumpkin");
            growingPumpkin.transform.SetParent(instance.transform, false);
            growingPumpkin.transform.localPosition = new Vector3(0f, 0.2f, 0f);

            if (pumpkinSource != null)
            {
                GameObject pModel = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, growingPumpkin.transform);
                pModel.transform.localPosition = Vector3.zero;
            }

            // Beacon / plot light
            GameObject lightObj = new GameObject("PlotLight");
            lightObj.transform.SetParent(instance.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.4f, 1f, 0.4f);
            l.range = 6.0f;
            l.intensity = 1.8f;

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

        private static GameObject CreatePlayerPrefab(GameObject boltPrefab)
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

            Transform handSocket = null;
            if (peasantSource != null)
            {
                GameObject peasant = (GameObject)PrefabUtility.InstantiatePrefab(peasantSource, visual.transform);
                peasant.transform.localPosition = Vector3.zero;
                peasant.transform.localRotation = Quaternion.identity;

                Transform[] allChildren = peasant.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.name.Contains("Hand_R") || t.name.Contains("RightHand"))
                    {
                        handSocket = t;
                        break;
                    }
                }
            }

            GameObject bowInstance = null;
            Transform firePoint = null;
            if (bowSource != null)
            {
                Transform parentSocket = handSocket != null ? handSocket : visual.transform;
                bowInstance = (GameObject)PrefabUtility.InstantiatePrefab(bowSource, parentSocket);
                if (handSocket != null)
                {
                    bowInstance.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
                    bowInstance.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                    bowInstance.transform.localScale = Vector3.one * 0.8f;
                }
                else
                {
                    bowInstance.transform.localPosition = new Vector3(0.35f, 0.9f, 0.3f);
                    bowInstance.transform.localRotation = Quaternion.identity;
                }

                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(bowInstance.transform, false);
                fp.transform.localPosition = new Vector3(0f, 0.05f, 0.6f);
                firePoint = fp.transform;
            }

            PlayerController pc = root.AddComponent<PlayerController>();
            PlayerHealth ph = root.AddComponent<PlayerHealth>();
            PlayerDash pd = root.AddComponent<PlayerDash>();
            WeaponManager wm = root.AddComponent<WeaponManager>();
            ExperienceManager xp = root.AddComponent<ExperienceManager>();
            UpgradeManager um = root.AddComponent<UpgradeManager>();
            LuckyLastSystem lucky = root.AddComponent<LuckyLastSystem>();

            GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Fire.prefab");
            if (firePrefab != null)
            {
                SerializedObject soLucky = new SerializedObject(lucky);
                soLucky.FindProperty("luckyLastAuraPrefab").objectReferenceValue = firePrefab;
                soLucky.ApplyModifiedProperties();
            }

            CrossbowWeapon crossbow = root.AddComponent<CrossbowWeapon>();
            if (firePoint != null) crossbow.SetFirePoint(firePoint);
            wm.AddWeapon(crossbow);

            // Additional Weapons (unlocked via upgrades)
            GameObject pickaxeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");
            GameObject explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

            PumpkinBombWeapon bombWeapon = root.AddComponent<PumpkinBombWeapon>();
            bombWeapon.SetExplosionVfx(explosionEffect);
            bombWeapon.enabled = false;
            wm.AddWeapon(bombWeapon);

            HarvestScytheWeapon scytheWeapon = root.AddComponent<HarvestScytheWeapon>();
            if (pickaxeModel != null) scytheWeapon.SetBladeModel(pickaxeModel);
            scytheWeapon.enabled = false;
            wm.AddWeapon(scytheWeapon);

            FireAuraWeapon fireAuraWeapon = root.AddComponent<FireAuraWeapon>();
            if (firePrefab != null) fireAuraWeapon.SetAuraVfx(firePrefab);
            fireAuraWeapon.enabled = false;
            wm.AddWeapon(fireAuraWeapon);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreatePumpkinGruntPrefab()
        {
            string targetPath = $"{PrefabsPath}/Enemy_PumpkinGrunt.prefab";
            GameObject skeletonSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Skeleton_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

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
                Transform parent = headBone != null ? headBone : visual.transform;
                GameObject pumpkin = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinSource, parent);
                if (headBone != null)
                {
                    pumpkin.transform.localPosition = new Vector3(0f, 0.15f, 0.05f);
                    pumpkin.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    pumpkin.transform.localScale = Vector3.one * 0.5f;
                }
                else
                {
                    pumpkin.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                    pumpkin.transform.localRotation = Quaternion.identity;
                    pumpkin.transform.localScale = Vector3.one * 0.55f;
                }
            }

            EnemyController ec = root.AddComponent<EnemyController>();
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            root.AddComponent<PooledObjectTracker>();

            SerializedObject soEh = new SerializedObject(eh);
            SerializedProperty propFx = soEh.FindProperty("deathEffectPrefab");
            if (propFx != null && explosionEffect != null)
            {
                propFx.objectReferenceValue = explosionEffect;
                soEh.ApplyModifiedProperties();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreatePumpkinBombPrefab()
        {
            string targetPath = $"{PrefabsPath}/PumpkinBomb.prefab";
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab");
            GameObject explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

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
            GameObject hazardPrefab)
        {
            string targetPath = $"{PrefabsPath}/{prefabName}.prefab";
            GameObject bodySource = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            GameObject headSource = !string.IsNullOrEmpty(headAttachPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(headAttachPath) : null;
            GameObject handSource = !string.IsNullOrEmpty(handAttachPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(handAttachPath) : null;
            GameObject explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

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
                    if (headBone == null && (b.name.Contains("Head") || b.name.Contains("Neck")))
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
                if (headBone != null)
                {
                    head.transform.localPosition = new Vector3(0f, 0.12f, 0.05f);
                    head.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    head.transform.localScale = Vector3.one * 0.5f;
                }
                else
                {
                    head.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                    head.transform.localRotation = Quaternion.identity;
                    head.transform.localScale = Vector3.one * 0.55f;
                }
            }

            if (handSource != null && handBone != null)
            {
                GameObject handItem = (GameObject)PrefabUtility.InstantiatePrefab(handSource, handBone);
                handItem.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
                handItem.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                handItem.transform.localScale = Vector3.one * 0.85f;
            }

            EnemyController ec = root.AddComponent<EnemyController>();
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
            soEh.FindProperty("knockbackResistance").floatValue = knockbackRes;
            if (hazardPrefab != null) soEh.FindProperty("onDeathHazardPrefab").objectReferenceValue = hazardPrefab;
            if (explosionEffect != null) soEh.FindProperty("deathEffectPrefab").objectReferenceValue = explosionEffect;
            soEh.ApplyModifiedProperties();

            root.AddComponent<PooledObjectTracker>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateBossPrefab(GameObject bombPrefab)
        {
            string targetPath = $"{PrefabsPath}/Boss_TheLastHarvest.prefab";
            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            GameObject pumpkinSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");
            GameObject pickaxeSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");
            GameObject explosionEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimpleFX/Prefabs/FX_Explosion_Rubble.prefab");

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
            soEh.FindProperty("knockbackResistance").floatValue = 1.0f;
            if (explosionEffect != null) soEh.FindProperty("deathEffectPrefab").objectReferenceValue = explosionEffect;
            soEh.ApplyModifiedProperties();

            BossController bc = root.AddComponent<BossController>();
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
            GameObject plotPrefab)
        {
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

            // 2. Directional Light (Moonlight)
            GameObject lightObj = new GameObject("Moonlight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.35f, 0.45f, 0.7f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // 3. Ground & Environment
            GameObject arenaRoot = new GameObject("Arena");
            GameObject groundTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_Generic_Ground_Flat_01.prefab");
            if (groundTilePrefab == null)
            {
                groundTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Ground_Grass_Large_01.prefab");
            }

            if (groundTilePrefab != null)
            {
                for (int x = -2; x < 2; x++)
                {
                    for (int z = -2; z < 2; z++)
                    {
                        GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(groundTilePrefab, arenaRoot.transform);
                        tile.transform.position = new Vector3(x * 20f + 10f, 0f, z * 20f + 10f);
                    }
                }
            }
            else
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "GroundPlane";
                ground.transform.SetParent(arenaRoot.transform);
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(8f, 1f, 8f);
            }

            BoxCollider groundCollider = arenaRoot.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(90f, 1f, 90f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);

            CreateBoundaryWall(arenaRoot.transform, new Vector3(0, 5, 42), new Vector3(90, 10, 2));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(0, 5, -42), new Vector3(90, 10, 2));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(42, 5, 0), new Vector3(2, 10, 90));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(-42, 5, 0), new Vector3(2, 10, 90));

            GameObject deadTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_Generic_TreeDead_01.prefab");
            GameObject pumpkinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab");

            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-25, 0, -25), new Vector3(25, 0, -25),
                new Vector3(-25, 0, 25), new Vector3(25, 0, 25),
                new Vector3(0, 0, -32), new Vector3(0, 0, 32),
                new Vector3(-32, 0, 0), new Vector3(32, 0, 0)
            };

            foreach (var pos in treePositions)
            {
                if (deadTreePrefab != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(deadTreePrefab, arenaRoot.transform);
                    tree.transform.position = pos;
                    tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
                }

                if (pumpkinPrefab != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinPrefab, arenaRoot.transform);
                    p.transform.position = pos + new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));
                    p.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

                    GameObject lanternLight = new GameObject("PumpkinLight");
                    lanternLight.transform.SetParent(p.transform, false);
                    lanternLight.transform.localPosition = new Vector3(0, 0.4f, 0);
                    Light pLight = lanternLight.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1f, 0.6f, 0.2f);
                    pLight.range = 7f;
                    pLight.intensity = 2f;
                }
            }

            // 3b. Harvest Plots (4 arena quadrants)
            if (plotPrefab != null)
            {
                Vector3[] plotPositions = new Vector3[]
                {
                    new Vector3(14f, 0f, 14f),
                    new Vector3(-14f, 0f, 14f),
                    new Vector3(-14f, 0f, -14f),
                    new Vector3(14f, 0f, -14f)
                };

                foreach (var pPos in plotPositions)
                {
                    GameObject plotObj = (GameObject)PrefabUtility.InstantiatePrefab(plotPrefab, arenaRoot.transform);
                    plotObj.transform.position = pPos;
                }
            }

            // 4. Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
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
            soGm.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            soGm.ApplyModifiedProperties();

            // 10. Audio & Polish Managers
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();

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

            // Top-Left: Health & Lucky Last
            GameObject topLeft = CreateUIObject("TopLeftHUD", canvasObj.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(250, 100), new Vector2(180, -70));
            GameObject hpBg = CreateImage(topLeft, "HealthBg", new Color(0.15f, 0.15f, 0.2f, 0.85f));
            hpBg.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 28);
            hpBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

            GameObject hpFill = CreateImage(hpBg, "HealthFill", new Color(0.2f, 0.85f, 0.3f, 1f));
            Image imgHpFill = hpFill.GetComponent<Image>();
            imgHpFill.type = Image.Type.Filled;
            imgHpFill.fillMethod = Image.FillMethod.Horizontal;
            imgHpFill.fillAmount = 1f;
            RectTransform rtHpFill = hpFill.GetComponent<RectTransform>();
            rtHpFill.anchorMin = Vector2.zero;
            rtHpFill.anchorMax = Vector2.one;
            rtHpFill.sizeDelta = Vector2.zero;

            Text hpText = CreateText(hpBg, "HealthText", "100 / 100", 18, TextAnchor.MiddleCenter, Color.white, defaultFont);

            Text luckyText = CreateText(topLeft, "LuckyChargesText", "LUCKY LAST: 1/2", 18, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f), defaultFont);
            luckyText.rectTransform.anchoredPosition = new Vector2(-60, -15);

            // Lucky Last Banner
            GameObject luckyBanner = CreateUIObject("LuckyLastBanner", canvasObj.transform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(400, 60), Vector2.zero);
            CreateImage(luckyBanner, "BannerBg", new Color(0.9f, 0.1f, 0.1f, 0.85f));
            Text bannerText = CreateText(luckyBanner, "BannerText", "★ LUCKY LAST ACTIVE! ★", 26, TextAnchor.MiddleCenter, Color.yellow, defaultFont);
            luckyBanner.SetActive(false);

            // Top-Center: Timer
            GameObject topCenter = CreateUIObject("TopCenterHUD", canvasObj.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(200, 60), new Vector2(0, -40));
            Text timerText = CreateText(topCenter, "TimerText", "00:00", 32, TextAnchor.MiddleCenter, Color.white, defaultFont);

            // Top-Right: Kills & Seeds
            GameObject topRight = CreateUIObject("TopRightHUD", canvasObj.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(250, 80), new Vector2(-150, -50));
            Text killText = CreateText(topRight, "KillText", "KILLS: 0", 20, TextAnchor.MiddleRight, Color.white, defaultFont);
            killText.rectTransform.anchoredPosition = new Vector2(0, 15);
            Text seedText = CreateText(topRight, "SeedText", "SEEDS: 0", 20, TextAnchor.MiddleRight, new Color(0.4f, 1f, 0.4f), defaultFont);
            seedText.rectTransform.anchoredPosition = new Vector2(0, -15);

            // Bottom: XP Bar & Level
            GameObject bottomBar = CreateUIObject("BottomHUD", canvasObj.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(0, 20));
            RectTransform rtBottom = bottomBar.GetComponent<RectTransform>();
            rtBottom.offsetMin = new Vector2(40, 10);
            rtBottom.offsetMax = new Vector2(-40, 40);

            GameObject xpBg = CreateImage(bottomBar, "XpBg", new Color(0.1f, 0.15f, 0.25f, 0.85f));
            RectTransform rtXpBg = xpBg.GetComponent<RectTransform>();
            rtXpBg.anchorMin = Vector2.zero;
            rtXpBg.anchorMax = Vector2.one;
            rtXpBg.sizeDelta = Vector2.zero;

            GameObject xpFill = CreateImage(xpBg, "XpFill", new Color(0.2f, 0.65f, 1f, 1f));
            Image imgXpFill = xpFill.GetComponent<Image>();
            imgXpFill.type = Image.Type.Filled;
            imgXpFill.fillMethod = Image.FillMethod.Horizontal;
            imgXpFill.fillAmount = 0f;
            RectTransform rtXpFill = xpFill.GetComponent<RectTransform>();
            rtXpFill.anchorMin = Vector2.zero;
            rtXpFill.anchorMax = Vector2.one;
            rtXpFill.sizeDelta = Vector2.zero;

            Text lvlText = CreateText(bottomBar, "LevelText", "LVL 1", 20, TextAnchor.MiddleCenter, Color.white, defaultFont);

            // Level Up Modal
            GameObject lvlModal = CreateUIObject("LevelUpModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            CreateImage(lvlModal, "DarkOverlay", new Color(0f, 0f, 0f, 0.75f));

            Text lvlTitle = CreateText(lvlModal, "Title", "CHOOSE AN UPGRADE", 36, TextAnchor.MiddleCenter, Color.yellow, defaultFont);
            lvlTitle.rectTransform.anchoredPosition = new Vector2(0, 260);

            GameObject cardsContainer = CreateUIObject("CardsContainer", lvlModal.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1000, 400), new Vector2(0, 20));
            HorizontalLayoutGroup hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            UpgradeCardUI[] cardUIs = new UpgradeCardUI[3];
            for (int i = 0; i < 3; i++)
            {
                cardUIs[i] = CreateUpgradeCard(cardsContainer.transform, i + 1, defaultFont);
            }

            GameObject rerollBtnObj = CreateButton(lvlModal.transform, "RerollButton", "REROLL (1) [R]", new Color(0.3f, 0.2f, 0.5f), defaultFont);
            rerollBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -240);
            Button btnReroll = rerollBtnObj.GetComponent<Button>();
            Text txtReroll = rerollBtnObj.GetComponentInChildren<Text>();

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

            Text vicTitle = CreateText(vicModal, "Title", "THE LAST HARVEST DEFEATED!", 44, TextAnchor.MiddleCenter, Color.yellow, defaultFont);
            vicTitle.rectTransform.anchoredPosition = new Vector2(0, 140);

            Text vicSub = CreateText(vicModal, "Sub", "YOU SURVIVED THE CURSED HARVEST!\n\nEnter Endless Mode to test your power against limitless hordes.", 24, TextAnchor.MiddleCenter, Color.white, defaultFont);
            vicSub.rectTransform.sizeDelta = new Vector2(800, 120);
            vicSub.rectTransform.anchoredPosition = new Vector2(0, 20);

            GameObject vicBtnObj = CreateButton(vicModal.transform, "EndlessButton", "ENTER ENDLESS MODE [ENTER]", new Color(0.2f, 0.7f, 0.3f), defaultFont);
            vicBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            vicBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 55);
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
            startBtnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -220);
            startBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 56);
            Button btnStart = startBtnObj.GetComponent<Button>();

            titleModal.SetActive(true);

            // Hook into UIManager
            UIManager uim = canvasObj.AddComponent<UIManager>();
            SerializedObject soUim = new SerializedObject(uim);
            soUim.FindProperty("healthBarFill").objectReferenceValue = imgHpFill;
            soUim.FindProperty("healthText").objectReferenceValue = hpText;
            soUim.FindProperty("luckyChargesText").objectReferenceValue = luckyText;
            soUim.FindProperty("luckyLastBanner").objectReferenceValue = luckyBanner;
            soUim.FindProperty("timerText").objectReferenceValue = timerText;
            soUim.FindProperty("xpBarFill").objectReferenceValue = imgXpFill;
            soUim.FindProperty("levelText").objectReferenceValue = lvlText;
            soUim.FindProperty("killCountText").objectReferenceValue = killText;
            soUim.FindProperty("seedCountText").objectReferenceValue = seedText;

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
            soUim.FindProperty("continueEndlessButton").objectReferenceValue = btnVic;

            soUim.FindProperty("titleModal").objectReferenceValue = titleModal;
            soUim.FindProperty("startRunButton").objectReferenceValue = btnStart;

            soUim.ApplyModifiedProperties();
        }

        private static UpgradeCardUI CreateUpgradeCard(Transform parent, int slotIndex, Font font)
        {
            GameObject cardObj = CreateUIObject($"Card_{slotIndex}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280, 360), Vector2.zero);
            CreateImage(cardObj, "CardBg", new Color(0.18f, 0.20f, 0.28f, 0.95f));

            Text cat = CreateText(cardObj, "Category", "WEAPON", 16, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.2f), font);
            cat.rectTransform.anchoredPosition = new Vector2(0, 140);

            Text title = CreateText(cardObj, "Title", "Upgrade Title", 22, TextAnchor.MiddleCenter, Color.white, font);
            title.rectTransform.anchoredPosition = new Vector2(0, 95);

            Text desc = CreateText(cardObj, "Description", "+25% Damage", 17, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f), font);
            desc.rectTransform.sizeDelta = new Vector2(240, 100);
            desc.rectTransform.anchoredPosition = new Vector2(0, 15);

            Text lvl = CreateText(cardObj, "Level", "Lv. 1 / 5", 16, TextAnchor.MiddleCenter, Color.yellow, font);
            lvl.rectTransform.anchoredPosition = new Vector2(0, -70);

            GameObject btnObj = CreateButton(cardObj.transform, "SelectButton", $"SELECT [{slotIndex}]", new Color(0.2f, 0.5f, 0.8f), font);
            btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -125);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 44);

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

        private static GameObject CreateImage(GameObject parent, string name, Color color)
        {
            GameObject imgObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            imgObj.transform.SetParent(parent.transform, false);
            Image img = imgObj.GetComponent<Image>();
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
            btnObj.GetComponent<Image>().color = color;
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240, 50);

            CreateText(btnObj, "Text", label, 20, TextAnchor.MiddleCenter, Color.white, font);
            return btnObj;
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
