using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class World2ArenaBuilder
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string MaterialsPath = "Assets/_Materials";
        private const string MainGameScenePath = "Assets/_Scenes/MainGame.unity";

        [MenuItem("Cursed Harvest/World 2/Build World 2 Arena and Boss")]
        public static void BuildWorld2()
        {
            Debug.Log("[World2ArenaBuilder] ==================================================");
            Debug.Log("[World2ArenaBuilder] Building WORLD 2 — THE ASHEN ORCHARDS...");
            Debug.Log("[World2ArenaBuilder] ==================================================");

            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            if (!Directory.Exists(MaterialsPath)) Directory.CreateDirectory(MaterialsPath);

            // 1. Create Hazard Patch Prefab
            GameObject hazardPrefab = CreateEmberHazardPatchPrefab();

            // 2. Create Ashen Reaper Boss Prefab
            GameObject bossPrefab = CreateBossTheAshenReaperPrefab(hazardPrefab);

            // 3. Create Ashen Ground Material
            Material groundMat = CreateAshenGroundMaterial();

            // 4. Assemble Arena in MainGame Scene
            AssembleMainGameWorld2(bossPrefab, groundMat);

            Debug.Log("[World2ArenaBuilder] WORLD 2 Build Complete!");
        }

        public static GameObject CreateEmberHazardPatchPrefab()
        {
            string path = $"{PrefabsPath}/EmberHazardPatch.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("EmberHazardPatch");
            EmberHazardPatch hazard = root.AddComponent<EmberHazardPatch>();
            hazard.Initialize(5.0f, 1.5f, 8.0f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World2ArenaBuilder] Saved {path}");
            return saved;
        }

        public static Material CreateAshenGroundMaterial()
        {
            string path = $"{MaterialsPath}/M_ContinuousBaseGround_W2.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material genericDirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat");
            Material mat = genericDirt != null ? new Material(genericDirt) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.name = "M_ContinuousBaseGround_W2";
            mat.SetColor("_BaseColor", new Color(0.24f, 0.22f, 0.20f)); // Burnt charcoal soil
            mat.SetColor("_Color", new Color(0.24f, 0.22f, 0.20f));
            if (mat.HasProperty("_Albedo_Map")) mat.SetTextureScale("_Albedo_Map", new Vector2(16f, 16f));
            if (mat.HasProperty("_Normal_Map")) mat.SetTextureScale("_Normal_Map", new Vector2(16f, 16f));

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World2ArenaBuilder] Created {path}");
            return mat;
        }

        public static GameObject CreateBossTheAshenReaperPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_TheAshenReaper.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // Ensure properties are up to date
                AshenReaperBoss existingBoss = existing.GetComponent<AshenReaperBoss>();
                EnemyLocomotion existingLoco = existing.GetComponent<EnemyLocomotion>();
                if (existingBoss != null && existingLoco != null) return existing;
            }

            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            GameObject axeSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab");
            if (maskSource == null) maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab");

            GameObject root = new GameObject("Boss_TheAshenReaper");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Scaled imposing silhouette: 1.35x
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.35f;
            cc.radius = 0.95f * 1.35f;
            cc.center = new Vector3(0f, 1.8f * 1.35f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.35f;

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

            // Attach WoodsClearing Axe to right hand
            if (axeSource != null && handBone != null)
            {
                GameObject axe = (GameObject)PrefabUtility.InstantiatePrefab(axeSource, handBone);
                axe.name = "Wep_Axe_AshenReaper";
                axe.transform.localPosition = new Vector3(0.06f, 0.05f, 0.08f);
                axe.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                axe.transform.localScale = Vector3.one * 1.15f;
            }

            // Attach Demonic/Plague Mask to head
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_AshenReaper";
                mask.transform.localPosition = new Vector3(0f, 0.08f, 0.12f);
                mask.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                mask.transform.localScale = Vector3.one * 0.95f;
            }

            // Health component (1725 HP for World 2)
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            SerializedObject soEh = new SerializedObject(eh);
            soEh.FindProperty("maxHealth").floatValue = 1725f;
            soEh.FindProperty("xpDropValue").floatValue = 350f;
            soEh.FindProperty("seedDropChance").floatValue = 1.0f;
            soEh.FindProperty("guaranteedSeedDrop").boolValue = true;
            soEh.FindProperty("healthDropChance").floatValue = 1.0f;
            soEh.FindProperty("guaranteedHealthDrop").boolValue = true;
            soEh.FindProperty("knockbackResistance").floatValue = 0.95f;
            soEh.ApplyModifiedProperties();

            // Locomotion component (poses arms with weapon and handles strides)
            EnemyLocomotion el = root.AddComponent<EnemyLocomotion>();
            SerializedObject soEl = new SerializedObject(el);
            soEl.FindProperty("hasWeaponInHand").boolValue = true;
            soEl.FindProperty("cycleDistance").floatValue = 1.25f;
            soEl.FindProperty("maxStrideAngle").floatValue = 12.0f;
            soEl.FindProperty("maxKneeBend").floatValue = 6.0f;
            soEl.FindProperty("torsoLeanAngle").floatValue = 2.0f;
            soEl.FindProperty("breathRate").floatValue = 1.8f;
            soEl.ApplyModifiedProperties();

            // AI component
            AshenReaperBoss boss = root.AddComponent<AshenReaperBoss>();
            SerializedObject soBoss = new SerializedObject(boss);
            soBoss.FindProperty("bossName").stringValue = "THE ASHEN REAPER";
            soBoss.FindProperty("moveSpeed").floatValue = 2.8f;
            soBoss.FindProperty("contactDamage").floatValue = 32f;
            soBoss.FindProperty("slamDamage").floatValue = 38f;
            soBoss.FindProperty("ashSweepRange").floatValue = 3.5f;
            soBoss.FindProperty("ashSweepAngle").floatValue = 120f;
            soBoss.FindProperty("ashSweepDamage").floatValue = 32f;
            soBoss.FindProperty("ashBurstRadius").floatValue = 7.0f;
            soBoss.FindProperty("ashBurstDamage").floatValue = 28f;
            if (hazardPrefab != null) soBoss.FindProperty("emberPatchPrefab").objectReferenceValue = hazardPrefab;
            soBoss.ApplyModifiedProperties();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World2ArenaBuilder] Saved {path}");
            return saved;
        }

        public static void AssembleMainGameWorld2(GameObject bossPrefab, Material groundMat)
        {
            bool sceneOpened = false;
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != MainGameScenePath)
            {
                currentScene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
                sceneOpened = true;
            }

            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                Debug.LogError("[World2ArenaBuilder] FAILED: Arena GameObject not found in MainGame!");
                return;
            }

            // 1. Group World 1 environment under Environment_W1 if not already grouped
            Transform envW1Tr = arena.transform.Find("Environment_W1");
            GameObject envW1;
            if (envW1Tr == null)
            {
                envW1 = new GameObject("Environment_W1");
                envW1.transform.SetParent(arena.transform, false);

                // Move existing visual parents into Environment_W1
                string[] w1ChildNames = new string[] { "GroundAndPaths", "AtmosphericLandmarks", "VisiblePerimeterWalls" };
                foreach (string cName in w1ChildNames)
                {
                    Transform t = arena.transform.Find(cName);
                    if (t != null)
                    {
                        t.SetParent(envW1.transform, true);
                    }
                }
                Debug.Log("[World2ArenaBuilder] Grouped World 1 visuals into Environment_W1.");
            }
            else
            {
                envW1 = envW1Tr.gameObject;
            }

            // 2. Build or update Environment_W2
            Transform envW2Tr = arena.transform.Find("Environment_W2");
            GameObject envW2;
            if (envW2Tr != null)
            {
                UnityEngine.Object.DestroyImmediate(envW2Tr.gameObject);
            }
            envW2 = new GameObject("Environment_W2");
            envW2.transform.SetParent(arena.transform, false);

            PopulateEnvironmentW2(envW2.transform, groundMat);

            // 3. Ensure WorldEnvironmentController exists on Arena
            WorldEnvironmentController controller = arena.GetComponent<WorldEnvironmentController>();
            if (controller == null) controller = arena.AddComponent<WorldEnvironmentController>();

            Light dirLight = null;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional) { dirLight = l; break; }
            }

            ParticleSystem ashPs = envW2.GetComponentInChildren<ParticleSystem>(true);
            controller.SetEnvironments(envW1, envW2, dirLight, ashPs);

            // 4. Update SpawnDirector with Ashen Reaper Boss Prefab
            SpawnDirector director = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (director != null && bossPrefab != null)
            {
                director.SetAshenReaperBossPrefab(bossPrefab);
                EditorUtility.SetDirty(director);
            }

            // Initially set active state: default to World 1 active, World 2 inactive
            envW1.SetActive(true);
            envW2.SetActive(false);

            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log("[World2ArenaBuilder] MainGame scene updated and saved successfully!");
        }

        private static void PopulateEnvironmentW2(Transform envW2Root, Material groundMat)
        {
            // ==========================================
            // 1. BASE GROUND (86m x 86m Charred Soil)
            // ==========================================
            GameObject groundGroup = new GameObject("GroundAndPaths_W2");
            groundGroup.transform.SetParent(envW2Root, false);

            GameObject basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            basePlane.name = "ContinuousBaseGround_W2";
            basePlane.transform.SetParent(groundGroup.transform, false);
            basePlane.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            basePlane.transform.localScale = new Vector3(8.6f, 1f, 8.6f);

            MeshCollider mc = basePlane.GetComponent<MeshCollider>();
            if (mc != null) UnityEngine.Object.DestroyImmediate(mc);

            MeshRenderer mr = basePlane.GetComponent<MeshRenderer>();
            if (mr != null && groundMat != null) mr.sharedMaterial = groundMat;

            // ==========================================
            // 2. LANDMARKS & DRESSING
            // ==========================================
            GameObject landmarks = new GameObject("AtmosphericLandmarks_W2");
            landmarks.transform.SetParent(envW2Root, false);

            // Load verified authentic Synty assets
            GameObject deadTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab");
            GameObject deadTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab");
            GameObject deadTree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab");
            GameObject stump1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_01.prefab");
            GameObject stump2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_02.prefab");
            GameObject log1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");
            GameObject logPile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_Pile_01.prefab");

            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_05.prefab");
            GameObject rock8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab");

            GameObject plank1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab");
            GameObject plank2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_02.prefab");
            GameObject crate1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_01.prefab");
            GameObject crate2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_02.prefab");
            GameObject barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_01.prefab");
            GameObject unlitPumpkin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab");

            // --- LANDMARK 1: NORTH — Broken Rows of Charred Fruit Trees (Orchard Grid) ---
            GameObject northLandmark = new GameObject("Landmark_North_OrchardRows");
            northLandmark.transform.SetParent(landmarks.transform, false);

            float[] rowZ = new float[] { 17f, 21.5f, 26f };
            float[] colX = new float[] { -18f, -12f, -6f, 6f, 12f, 18f }; // Central gap at x=0 (-5 to +5) for kiting

            for (int r = 0; r < rowZ.Length; r++)
            {
                for (int c = 0; c < colX.Length; c++)
                {
                    // Skip 1 out of 3 to represent burnt/broken gaps in the rows
                    if ((r + c) % 3 == 1)
                    {
                        // Place a burnt stump instead
                        if (stump1 != null)
                        {
                            GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(stump1, northLandmark.transform);
                            s.transform.position = new Vector3(colX[c], 0f, rowZ[r]);
                            s.transform.rotation = Quaternion.Euler(0f, (r * 73f + c * 31f) % 360f, 0f);
                            s.transform.localScale = Vector3.one * 0.9f;
                            RemoveColliders(s);
                        }
                        continue;
                    }

                    GameObject treePrefab = (c % 2 == 0) ? (deadTree1 ?? deadTree2) : (deadTree2 ?? deadTree3);
                    if (treePrefab != null)
                    {
                        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, northLandmark.transform);
                        tree.transform.position = new Vector3(colX[c], 0f, rowZ[r]);
                        tree.transform.rotation = Quaternion.Euler(0f, (c * 67f + r * 113f) % 360f, 0f);
                        tree.transform.localScale = Vector3.one * (0.95f + ((r + c) % 3) * 0.12f);
                        RemoveColliders(tree);
                    }
                }
            }

            // --- LANDMARK 2: EAST — Collapsed Packing Shed (Planks, Crates, Barrels) ---
            GameObject eastLandmark = new GameObject("Landmark_East_CollapsedPackingShed");
            eastLandmark.transform.SetParent(landmarks.transform, false);
            Vector3 shedCenter = new Vector3(20f, 0f, 0f);

            Vector3[] shedOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1.2f, 0f, 1.4f), new Vector3(-1.1f, 0f, 1.8f),
                new Vector3(1.5f, 0f, -1.2f), new Vector3(-1.4f, 0f, -1.5f), new Vector3(2.2f, 0f, 0.5f),
                new Vector3(-2.0f, 0f, -0.2f), new Vector3(0.5f, 0f, 2.5f), new Vector3(-0.6f, 0f, -2.6f)
            };

            for (int i = 0; i < shedOffsets.Length; i++)
            {
                GameObject p = (i % 3 == 0) ? (crate1 ?? crate2) : ((i % 3 == 1) ? (barrel ?? crate1) : (plank1 ?? plank2));
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, eastLandmark.transform);
                    item.transform.position = shedCenter + shedOffsets[i];
                    item.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 15f), (i * 47f) % 360f, UnityEngine.Random.Range(-5f, 10f));
                    item.transform.localScale = Vector3.one * (0.9f + (i % 3) * 0.15f);
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 3: SOUTH — Ash Piles & Dead Grove (Stumps, Logs, Rocks) ---
            GameObject southLandmark = new GameObject("Landmark_South_DeadGroveAndAshPiles");
            southLandmark.transform.SetParent(landmarks.transform, false);
            Vector3 southCenter = new Vector3(0f, 0f, -20f);

            Vector3[] southOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(2.4f, 0f, 1.2f), new Vector3(-2.8f, 0f, 0.8f),
                new Vector3(1.5f, 0f, -2.0f), new Vector3(-1.8f, 0f, -1.8f), new Vector3(3.5f, 0f, -0.5f),
                new Vector3(-3.6f, 0f, 0.2f), new Vector3(0.5f, 0f, 2.8f)
            };

            for (int i = 0; i < southOffsets.Length; i++)
            {
                GameObject p = (i % 3 == 0) ? (logPile ?? log1) : ((i % 3 == 1) ? (stump2 ?? stump1) : (rock8 ?? rock4));
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, southLandmark.transform);
                    item.transform.position = southCenter + southOffsets[i];
                    item.transform.rotation = Quaternion.Euler(0f, (i * 71f) % 360f, 0f);
                    item.transform.localScale = Vector3.one * (0.95f + (i % 3) * 0.2f);
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 4: WEST — Charred Harvest Storage (Leaning Fences, Smashed Crates, Dark Pumpkins) ---
            GameObject westLandmark = new GameObject("Landmark_West_CharredStorage");
            westLandmark.transform.SetParent(landmarks.transform, false);
            Vector3 westCenter = new Vector3(-20f, 0f, 0f);

            Vector3[] westOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 1.5f), new Vector3(-1.2f, 0f, 1.8f),
                new Vector3(1.8f, 0f, -1.4f), new Vector3(-1.5f, 0f, -1.6f), new Vector3(2.5f, 0f, 0.2f),
                new Vector3(-2.2f, 0f, -0.4f), new Vector3(0.2f, 0f, 2.4f)
            };

            for (int i = 0; i < westOffsets.Length; i++)
            {
                GameObject p = (i % 3 == 0) ? (unlitPumpkin ?? crate1) : ((i % 3 == 1) ? (crate2 ?? plank1) : (plank2 ?? plank1));
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, westLandmark.transform);
                    item.transform.position = westCenter + westOffsets[i];
                    item.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 15f), (i * 59f) % 360f, 0f);
                    item.transform.localScale = Vector3.one * (0.85f + (i % 3) * 0.15f);
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 5: CENTER — Ritual Orchard Altar Dressing ---
            GameObject centerLandmark = new GameObject("Landmark_Center_RitualOrchardAltar");
            centerLandmark.transform.SetParent(landmarks.transform, false);
            Vector3 centerPos = new Vector3(0f, 0f, 0f);

            // Surrounding charred dressing around center circle (radius 4.2m, keeping center (0,0,0) clear for spawn)
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                Vector3 ringPos = centerPos + new Vector3(Mathf.Sin(a) * 4.2f, 0f, Mathf.Cos(a) * 4.2f);
                GameObject p = (i % 2 == 0) ? (stump1 ?? log1) : (unlitPumpkin ?? rock4);
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, centerLandmark.transform);
                    item.transform.position = ringPos;
                    item.transform.rotation = Quaternion.Euler(0f, (i * 61f) % 360f, 0f);
                    item.transform.localScale = Vector3.one * 0.85f;
                    RemoveColliders(item);
                }
            }

            // ==========================================
            // 3. VISIBLE PERIMETER WALLS (Dead Trees & Rocks)
            // ==========================================
            GameObject perimeter = new GameObject("VisiblePerimeterWalls_W2");
            perimeter.transform.SetParent(envW2Root, false);

            float b = 31.5f;
            for (float x = -b; x <= b; x += 5.5f)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, b), deadTree1, rock5);
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, -b), deadTree2, rock8);
            }
            for (float z = -b + 5.5f; z < b; z += 5.5f)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(b, 0f, z), deadTree3, rock4);
                SpawnPerimeterNode(perimeter.transform, new Vector3(-b, 0f, z), deadTree1, rock5);
            }

            // ==========================================
            // 4. ASH PARTICLE SYSTEM (Subtle Falling Ash Flakes)
            // ==========================================
            GameObject ashObj = new GameObject("AshParticleSystem");
            ashObj.transform.SetParent(envW2Root, false);
            ashObj.transform.position = new Vector3(0f, 4f, 0f);

            ParticleSystem ps = ashObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 7.0f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.82f, 0.80f, 0.78f, 0.90f), // Pale grey floating ash flake
                new Color(0.98f, 0.55f, 0.18f, 0.95f)  // Glowing ember spark
            );
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(64f, 8f, 64f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(-0.5f, -0.9f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

            ParticleSystemRenderer psr = ashObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                Material pMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/FX/Generic_Circle_Soft_01.mat");
                if (pMat == null) pMat = new Material(Shader.Find("Sprites/Default"));
                psr.sharedMaterial = pMat;
            }
        }

        private static void SpawnPerimeterNode(Transform parent, Vector3 pos, GameObject treePrefab, GameObject rockPrefab)
        {
            if (treePrefab != null)
            {
                GameObject t = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parent);
                t.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
                t.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                t.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);
                RemoveColliders(t);
            }
            if (rockPrefab != null && UnityEngine.Random.value < 0.6f)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, parent);
                r.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0f, UnityEngine.Random.Range(-1.5f, 1.5f));
                r.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                r.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);
                RemoveColliders(r);
            }
        }

        private static void RemoveColliders(GameObject go)
        {
            if (go == null) return;
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            foreach (var c in cols) UnityEngine.Object.DestroyImmediate(c);
        }
    }
}
