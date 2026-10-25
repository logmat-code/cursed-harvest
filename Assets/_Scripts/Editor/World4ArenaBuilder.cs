using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class World4ArenaBuilder
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string MaterialsPath = "Assets/_Materials";
        private const string MainGameScenePath = "Assets/_Scenes/MainGame.unity";

        [MenuItem("Cursed Harvest/World 4/Build World 4 Arena and Bosses")]
        public static void BuildWorld4()
        {
            Debug.Log("[World4ArenaBuilder] ==================================================");
            Debug.Log("[World4ArenaBuilder] Building WORLD 4 — THE BLOODWOOD TIMBER...");
            Debug.Log("[World4ArenaBuilder] ==================================================");

            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            if (!Directory.Exists(MaterialsPath)) Directory.CreateDirectory(MaterialsPath);

            // 1. Create Blood Sap Hazard Pool Prefab
            GameObject hazardPrefab = CreateBloodSapHazardPoolPrefab();

            // 2. Create Timber Goliath (Mid-Boss Wave 10) Prefab
            GameObject midBossPrefab = CreateTimberGoliathBossPrefab();

            // 3. Create Bloodwood Sovereign (Final Boss Wave 15) Prefab
            GameObject finalBossPrefab = CreateBloodwoodSovereignBossPrefab(hazardPrefab);

            // 4. Create Timber Ground Material
            Material groundMat = CreateTimberGroundMaterial();

            // 5. Assemble Arena in MainGame Scene
            AssembleMainGameWorld4(midBossPrefab, finalBossPrefab, groundMat);

            Debug.Log("[World4ArenaBuilder] WORLD 4 Build Complete!");
        }

        public static GameObject CreateBloodSapHazardPoolPrefab()
        {
            string path = $"{PrefabsPath}/BloodSapHazardPool.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("BloodSapHazardPool");
            BloodSapHazardPool hazard = root.AddComponent<BloodSapHazardPool>();
            hazard.Initialize(7.0f, 1.6f, 8.0f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World4ArenaBuilder] Saved {path}");
            return saved;
        }

        public static GameObject CreateTimberGoliathBossPrefab()
        {
            string path = $"{PrefabsPath}/Boss_TimberGoliath.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject peasantSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab");
            if (peasantSource == null) peasantSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Street_Male_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab");
            GameObject axeSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab");

            GameObject root = new GameObject("Boss_TimberGoliath");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Heavy lumberjack silhouette: 1.45x scale
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.45f;
            cc.radius = 0.95f * 1.45f;
            cc.center = new Vector3(0f, 1.8f * 1.45f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.45f;

            Transform headBone = null;
            Transform handBone = null;

            if (peasantSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(peasantSource, visual.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                Transform[] bones = model.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (headBone == null && (b.name.Contains("Head") || b.name.Contains("Neck"))) headBone = b;
                    if (handBone == null && (b.name.Contains("Hand_R") || b.name.Contains("RightHand"))) handBone = b;
                }
            }

            // Attach Demonic Mask to head
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_Demonic_Goliath";
                mask.transform.localPosition = new Vector3(0f, 0.05f, 0.08f);
                mask.transform.localRotation = Quaternion.identity;
                mask.transform.localScale = Vector3.one * 1.05f;
            }

            // Attach Giant Logging Axe to right hand (scaled 2.2x)
            if (axeSource != null && handBone != null)
            {
                GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(axeSource, handBone);
                axe.name = "Wep_LoggingAxe_Goliath";
                axe.transform.localPosition = new Vector3(0.05f, 0.04f, 0.08f);
                axe.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                axe.transform.localScale = Vector3.one * 2.2f;
            }

            // Gameplay components
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.SetMaxHealth(2800f);
            eh.SetHealthDropSettings(0.50f, guaranteed: true, count: 2);

            TimberGoliathBoss bossComp = root.AddComponent<TimberGoliathBoss>();
            EnemyLocomotion loco = root.AddComponent<EnemyLocomotion>();
            EnemyController ec = root.AddComponent<EnemyController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World4ArenaBuilder] Saved {path}");
            return saved;
        }

        public static GameObject CreateBloodwoodSovereignBossPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_BloodwoodSovereign.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            if (charredSource == null) charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Pumpkin_Mask_01.prefab");
            GameObject staffSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Models/SM_Gen_Wep_Pickaxe_01.fbx");
            GameObject logSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");

            GameObject root = new GameObject("Boss_BloodwoodSovereign");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Towering monarch silhouette: 1.65x scale
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.65f;
            cc.radius = 1.05f * 1.65f;
            cc.center = new Vector3(0f, 1.8f * 1.65f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.65f;

            Transform headBone = null;
            Transform handBone = null;
            Transform spineBone = null;

            if (charredSource != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(charredSource, visual.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                Transform[] bones = model.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (headBone == null && (b.name.Contains("Head") || b.name.Contains("Neck"))) headBone = b;
                    if (handBone == null && (b.name.Contains("Hand_R") || b.name.Contains("RightHand"))) handBone = b;
                    if (spineBone == null && (b.name.Contains("Spine") || b.name.Contains("Chest"))) spineBone = b;
                }
            }

            // Attach Pumpkin Mask to head
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_Pumpkin_Sovereign";
                mask.transform.localPosition = new Vector3(0f, 0.06f, 0.08f);
                mask.transform.localRotation = Quaternion.identity;
                mask.transform.localScale = Vector3.one * 1.15f;
            }

            // Attach Ritual Staff to right hand
            if (staffSource != null && handBone != null)
            {
                GameObject staff = (GameObject)PrefabUtility.InstantiatePrefab(staffSource, handBone);
                staff.name = "Wep_TimberStaff_Sovereign";
                staff.transform.localPosition = new Vector3(0.05f, 0.05f, 0.08f);
                staff.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                staff.transform.localScale = Vector3.one * 1.45f;
            }

            // Attach Timber Branch/Log to back for spiny silhouette
            if (logSource != null && spineBone != null)
            {
                GameObject logBack = (GameObject)PrefabUtility.InstantiatePrefab(logSource, spineBone);
                logBack.name = "Prop_SpineLog_Sovereign";
                logBack.transform.localPosition = new Vector3(0f, 0.15f, -0.15f);
                logBack.transform.localRotation = Quaternion.Euler(15f, 0f, 25f);
                logBack.transform.localScale = Vector3.one * 0.40f;
                RemoveColliders(logBack);
            }

            // Gameplay components
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.SetMaxHealth(3600f);
            eh.SetHealthDropSettings(0.60f, guaranteed: true, count: 3);

            BloodwoodSovereignBoss bossComp = root.AddComponent<BloodwoodSovereignBoss>();
            if (hazardPrefab != null) bossComp.SetSapPoolPrefab(hazardPrefab);

            EnemyLocomotion loco = root.AddComponent<EnemyLocomotion>();
            EnemyController ec = root.AddComponent<EnemyController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World4ArenaBuilder] Saved {path}");
            return saved;
        }

        public static Material CreateTimberGroundMaterial()
        {
            string path = $"{MaterialsPath}/M_ContinuousBaseGround_W4.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "M_ContinuousBaseGround_W4";
                AssetDatabase.CreateAsset(mat, path);
            }

            // Dark redwood sawdust & pine needle loam
            Color timberGroundColor = new Color(0.24f, 0.12f, 0.08f, 1.0f);
            mat.SetColor("_BaseColor", timberGroundColor);
            mat.SetColor("_Color", timberGroundColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World4ArenaBuilder] Created/Updated {path}");
            return mat;
        }

        public static void AssembleMainGameWorld4(GameObject midBossPrefab, GameObject finalBossPrefab, Material groundMat)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != MainGameScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
            }

            // Find or Create Environment_W4
            GameObject arena = GameObject.Find("Arena");
            GameObject existingW4 = null;
            if (arena != null)
            {
                Transform t = arena.transform.Find("Environment_W4");
                if (t != null) existingW4 = t.gameObject;
            }
            if (existingW4 == null)
            {
                foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (r.name == "Environment_W4") { existingW4 = r; break; }
                }
            }
            if (existingW4 != null)
            {
                UnityEngine.Object.DestroyImmediate(existingW4);
            }

            GameObject envW4 = new GameObject("Environment_W4");
            if (arena != null)
            {
                envW4.transform.SetParent(arena.transform, false);
            }
            envW4.transform.position = Vector3.zero;

            // ==========================================
            // 1. BASE GROUND (86m x 86m Redwood Sawdust Plane)
            // ==========================================
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Continuous_W4";
            ground.transform.SetParent(envW4.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(8.6f, 1f, 8.6f);

            RemoveColliders(ground);
            MeshRenderer gr = ground.GetComponent<MeshRenderer>();
            if (gr != null && groundMat != null) gr.sharedMaterial = groundMat;

            // Load verified authentic Synty assets
            GameObject pine1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_01.prefab");
            GameObject pine2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_02.prefab");
            GameObject pine3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Pine_03.prefab");

            GameObject deadTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab");
            GameObject deadTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab");
            GameObject deadTree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab");

            GameObject stump1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_01.prefab");
            GameObject stump2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_02.prefab");
            GameObject stump3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_03.prefab");

            GameObject log1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");
            GameObject log2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_02.prefab");

            GameObject plank1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab");
            GameObject plank2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_02.prefab");
            GameObject crate1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_01.prefab");
            GameObject crate2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_02.prefab");
            GameObject cratePreset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_Preset_01.prefab");
            GameObject barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_01.prefab");
            GameObject axe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab");

            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock7 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_07.prefab");
            GameObject rock8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab");

            // ==========================================
            // 2. FIVE DISTINCT ATMOSPHERIC LANDMARKS
            // ==========================================
            GameObject landmarksRoot = new GameObject("AtmosphericLandmarks_W4");
            landmarksRoot.transform.SetParent(envW4.transform, false);

            // Landmark 1: Central Timber Ritual Altar (0, 0, 0)
            GameObject lmCenter = new GameObject("Landmark_1_CentralTimberRitual");
            lmCenter.transform.SetParent(landmarksRoot.transform, false);
            lmCenter.transform.position = Vector3.zero;

            if (stump2 != null)
            {
                GameObject centralStump = (GameObject)PrefabUtility.InstantiatePrefab(stump2, lmCenter.transform);
                centralStump.name = "Ritual_CentralStump";
                centralStump.transform.position = new Vector3(0f, 0f, 0f);
                centralStump.transform.localScale = new Vector3(2.6f, 0.55f, 2.6f);
                RemoveColliders(centralStump);
            }
            else if (stump1 != null)
            {
                GameObject centralStump = (GameObject)PrefabUtility.InstantiatePrefab(stump1, lmCenter.transform);
                centralStump.name = "Ritual_CentralStump";
                centralStump.transform.position = new Vector3(0f, 0f, 0f);
                centralStump.transform.localScale = new Vector3(2.0f, 0.45f, 2.0f);
                RemoveColliders(centralStump);
            }

            // Radial logs & embedded axes around central altar
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector3 logPos = new Vector3(Mathf.Cos(angle) * 3.6f, 0f, Mathf.Sin(angle) * 3.6f);
                GameObject chosenLog = (i % 2 == 0) ? log1 : log2;
                if (chosenLog != null)
                {
                    GameObject l = (GameObject)PrefabUtility.InstantiatePrefab(chosenLog, lmCenter.transform);
                    l.transform.position = logPos;
                    l.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), angle * Mathf.Rad2Deg + 90f, 0f);
                    l.transform.localScale = Vector3.one * 1.35f;
                    RemoveColliders(l);
                }
                if (axe != null && i % 2 == 1)
                {
                    GameObject ax = (GameObject)PrefabUtility.InstantiatePrefab(axe, lmCenter.transform);
                    ax.transform.position = logPos + Vector3.up * 0.45f;
                    ax.transform.rotation = Quaternion.Euler(-35f, angle * Mathf.Rad2Deg, 0f);
                    ax.transform.localScale = Vector3.one * 1.5f;
                    RemoveColliders(ax);
                }
            }

            // Crimson Altar Light
            GameObject altarLight = new GameObject("Altar_CrimsonLight");
            altarLight.transform.SetParent(lmCenter.transform, false);
            altarLight.transform.position = new Vector3(0f, 1.3f, 0f);
            Light cl = altarLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(0.95f, 0.22f, 0.12f);
            cl.range = 9.5f;
            cl.intensity = 2.2f;

            // Landmark 2: Abandoned Sawmill (0, 0, 22) - North
            GameObject lmNorth = new GameObject("Landmark_2_AbandonedSawmill");
            lmNorth.transform.SetParent(landmarksRoot.transform, false);
            lmNorth.transform.position = new Vector3(0f, 0f, 22f);

            // Lumber stacks and timber ramp
            if (plank1 != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    GameObject pl = (GameObject)PrefabUtility.InstantiatePrefab(plank1, lmNorth.transform);
                    pl.transform.position = new Vector3(-2.5f + i * 1.2f, 0.1f * i, 22f + (i % 2) * 0.4f);
                    pl.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-10f, 10f), 0f);
                    pl.transform.localScale = new Vector3(1.4f, 1.4f, 2.0f);
                    RemoveColliders(pl);
                }
            }
            if (cratePreset != null)
            {
                GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(cratePreset, lmNorth.transform);
                cp.transform.position = new Vector3(3.2f, 0f, 22.5f);
                cp.transform.localScale = Vector3.one * 1.35f;
                RemoveColliders(cp);
            }
            if (barrel != null)
            {
                GameObject b1 = (GameObject)PrefabUtility.InstantiatePrefab(barrel, lmNorth.transform);
                b1.transform.position = new Vector3(-3.5f, 0f, 23.2f);
                RemoveColliders(b1);
                GameObject b2 = (GameObject)PrefabUtility.InstantiatePrefab(barrel, lmNorth.transform);
                b2.transform.position = new Vector3(-2.8f, 0f, 24.0f);
                RemoveColliders(b2);
            }
            if (deadTree1 != null)
            {
                GameObject dt = (GameObject)PrefabUtility.InstantiatePrefab(deadTree1, lmNorth.transform);
                dt.transform.position = new Vector3(0f, 0f, 25.5f);
                dt.transform.localScale = Vector3.one * 1.6f;
                RemoveColliders(dt);
            }

            // Landmark 3: Log Storage Yard (22, 0, -4) - East
            GameObject lmEast = new GameObject("Landmark_3_LogStorageYard");
            lmEast.transform.SetParent(landmarksRoot.transform, false);
            lmEast.transform.position = new Vector3(22f, 0f, -4f);

            // Heavy pyramid log piles
            if (log1 != null && log2 != null)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    int logsInLayer = 4 - layer;
                    for (int j = 0; j < logsInLayer; j++)
                    {
                        GameObject logPrefab = (j % 2 == 0) ? log1 : log2;
                        GameObject lp = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, lmEast.transform);
                        lp.transform.position = new Vector3(21f + layer * 0.7f, layer * 0.55f, -6f + j * 1.4f + layer * 0.7f);
                        lp.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        lp.transform.localScale = Vector3.one * 1.3f;
                        RemoveColliders(lp);
                    }
                }
            }
            if (stump2 != null)
            {
                GameObject stp = (GameObject)PrefabUtility.InstantiatePrefab(stump2, lmEast.transform);
                stp.transform.position = new Vector3(24.5f, 0f, -2.5f);
                stp.transform.localScale = Vector3.one * 1.6f;
                RemoveColliders(stp);
            }
            if (axe != null)
            {
                GameObject ax = (GameObject)PrefabUtility.InstantiatePrefab(axe, lmEast.transform);
                ax.transform.position = new Vector3(24.5f, 0.8f, -2.5f);
                ax.transform.rotation = Quaternion.Euler(-30f, 45f, 0f);
                ax.transform.localScale = Vector3.one * 1.6f;
                RemoveColliders(ax);
            }

            // Landmark 4: Bloodroot Ancient Pine Grove (4, 0, -22) - South
            GameObject lmSouth = new GameObject("Landmark_4_BloodrootGrove");
            lmSouth.transform.SetParent(landmarksRoot.transform, false);
            lmSouth.transform.position = new Vector3(4f, 0f, -22f);

            GameObject[] pinePrefabs = new GameObject[] { pine1, pine2, pine3 };
            Vector3[] pineOffsets = new Vector3[]
            {
                new Vector3(-3.2f, 0f, -1.5f),
                new Vector3(2.8f, 0f, 1.2f),
                new Vector3(0.5f, 0f, 3.5f),
                new Vector3(-1.8f, 0f, -3.4f),
                new Vector3(3.6f, 0f, -2.8f)
            };
            for (int i = 0; i < pineOffsets.Length; i++)
            {
                GameObject pp = pinePrefabs[i % pinePrefabs.Length];
                if (pp != null)
                {
                    GameObject pTree = (GameObject)PrefabUtility.InstantiatePrefab(pp, lmSouth.transform);
                    pTree.transform.position = new Vector3(4f, 0f, -22f) + pineOffsets[i];
                    pTree.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-4f, 4f));
                    pTree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.7f, 2.3f);
                    RemoveColliders(pTree);
                }
            }
            if (stump3 != null)
            {
                GameObject stp = (GameObject)PrefabUtility.InstantiatePrefab(stump3, lmSouth.transform);
                stp.transform.position = new Vector3(4f, 0f, -22f);
                stp.transform.localScale = Vector3.one * 2.2f;
                RemoveColliders(stp);
            }

            // Landmark 5: Broken Logging Camp (-22, 0, 2) - West
            GameObject lmWest = new GameObject("Landmark_5_BrokenLoggingCamp");
            lmWest.transform.SetParent(landmarksRoot.transform, false);
            lmWest.transform.position = new Vector3(-22f, 0f, 2f);

            if (crate1 != null)
            {
                GameObject c1 = (GameObject)PrefabUtility.InstantiatePrefab(crate1, lmWest.transform);
                c1.transform.position = new Vector3(-21f, 0f, 1.2f);
                RemoveColliders(c1);
            }
            if (crate2 != null)
            {
                GameObject c2 = (GameObject)PrefabUtility.InstantiatePrefab(crate2, lmWest.transform);
                c2.transform.position = new Vector3(-22.4f, 0f, 3.1f);
                c2.transform.rotation = Quaternion.Euler(15f, 35f, 0f);
                RemoveColliders(c2);
            }
            if (barrel != null)
            {
                GameObject b1 = (GameObject)PrefabUtility.InstantiatePrefab(barrel, lmWest.transform);
                b1.transform.position = new Vector3(-23.5f, 0f, 0.8f);
                RemoveColliders(b1);
                GameObject b2 = (GameObject)PrefabUtility.InstantiatePrefab(barrel, lmWest.transform);
                b2.transform.position = new Vector3(-22.8f, 0.2f, 1.8f);
                b2.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                RemoveColliders(b2);
            }
            if (stump1 != null)
            {
                GameObject stp = (GameObject)PrefabUtility.InstantiatePrefab(stump1, lmWest.transform);
                stp.transform.position = new Vector3(-24f, 0f, 4.2f);
                stp.transform.localScale = Vector3.one * 1.5f;
                RemoveColliders(stp);
            }
            if (deadTree2 != null)
            {
                GameObject dt = (GameObject)PrefabUtility.InstantiatePrefab(deadTree2, lmWest.transform);
                dt.transform.position = new Vector3(-25.5f, 0f, 2f);
                dt.transform.localScale = Vector3.one * 1.55f;
                RemoveColliders(dt);
            }

            // ==========================================
            // 3. PERIMETER PALISADES & DENSE PINE TREE LINE
            // ==========================================
            GameObject perimeter = new GameObject("PerimeterPalisades_W4");
            perimeter.transform.SetParent(envW4.transform, false);

            float b = 31.5f;
            float step = 3.6f;

            // North & South perimeter lines
            for (float x = -b; x <= b; x += step)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, b), pine1, log1, rock7);
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, -b), pine2, log2, rock8);
            }
            // East & West perimeter lines
            for (float z = -b + step; z < b; z += step)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(b, 0f, z), pine3, log1, rock4);
                SpawnPerimeterNode(perimeter.transform, new Vector3(-b, 0f, z), pine1, log2, rock7);
            }

            // ==========================================
            // 4. BLOODWOOD SAWDUST & SAP EMBER PARTICLES
            // ==========================================
            GameObject emberObj = new GameObject("Bloodwood_Sap_Embers");
            emberObj.transform.SetParent(envW4.transform, false);
            emberObj.transform.position = new Vector3(0f, 1.5f, 0f);

            ParticleSystem ps = emberObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 4.0f;
            main.startSpeed = 0.30f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.88f, 0.32f, 0.15f, 0.55f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 12f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 3f, 50f);

            // Connect to WorldEnvironmentController
            WorldEnvironmentController wec = UnityEngine.Object.FindAnyObjectByType<WorldEnvironmentController>(FindObjectsInactive.Include);
            if (wec == null)
            {
                foreach (var r in activeScene.GetRootGameObjects())
                {
                    wec = r.GetComponentInChildren<WorldEnvironmentController>(true);
                    if (wec != null) break;
                }
            }
            if (wec != null)
            {
                wec.FindReferencesIfMissing();
                wec.SetEnvironments(wec.EnvironmentW1, wec.EnvironmentW2, wec.EnvironmentW3, envW4,
                    RenderSettings.sun, wec.AshParticles, wec.MireParticles, ps);
                Debug.Log("[World4ArenaBuilder] Connected Environment_W4 to WorldEnvironmentController.");
            }

            // Connect to SpawnDirector
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>(FindObjectsInactive.Include);
            if (sd == null)
            {
                foreach (var r in activeScene.GetRootGameObjects())
                {
                    sd = r.GetComponentInChildren<SpawnDirector>(true);
                    if (sd != null) break;
                }
            }
            if (sd != null)
            {
                if (midBossPrefab != null) sd.SetTimberGoliathBossPrefab(midBossPrefab);
                if (finalBossPrefab != null) sd.SetBloodwoodSovereignBossPrefab(finalBossPrefab);
                EditorUtility.SetDirty(sd);
                Debug.Log("[World4ArenaBuilder] Assigned timberGoliathBossPrefab & bloodwoodSovereignBossPrefab to SpawnDirector.");
            }

            // Default to inactive so World 1 remains default on clean start
            envW4.SetActive(false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[World4ArenaBuilder] Environment_W4 rebuilt and saved successfully in MainGame.unity!");
        }

        private static void SpawnPerimeterNode(Transform parent, Vector3 pos, GameObject treePrefab, GameObject logPrefab, GameObject rockPrefab)
        {
            if (treePrefab != null)
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parent);
                tree.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
                tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.8f);
                RemoveColliders(tree);
            }
            if (logPrefab != null && UnityEngine.Random.value < 0.65f)
            {
                GameObject log = (GameObject)PrefabUtility.InstantiatePrefab(logPrefab, parent);
                log.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                log.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), 0f);
                log.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.0f, 1.3f);
                RemoveColliders(log);
            }
            if (rockPrefab != null && UnityEngine.Random.value < 0.45f)
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, parent);
                rock.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0f, UnityEngine.Random.Range(-1.0f, 1.0f));
                rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 15f), UnityEngine.Random.Range(0f, 360f), 0f);
                rock.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);
                RemoveColliders(rock);
            }
        }

        private static void RemoveColliders(GameObject obj)
        {
            if (obj == null) return;
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
        }
    }
}
