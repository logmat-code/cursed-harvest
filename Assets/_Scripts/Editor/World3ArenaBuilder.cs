using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    public static class World3ArenaBuilder
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string MaterialsPath = "Assets/_Materials";
        private const string MainGameScenePath = "Assets/_Scenes/MainGame.unity";

        [MenuItem("Cursed Harvest/World 3/Build World 3 Arena and Boss")]
        public static void BuildWorld3()
        {
            Debug.Log("[World3ArenaBuilder] ==================================================");
            Debug.Log("[World3ArenaBuilder] Building WORLD 3 — THE BLACKENED MIRE...");
            Debug.Log("[World3ArenaBuilder] ==================================================");

            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            if (!Directory.Exists(MaterialsPath)) Directory.CreateDirectory(MaterialsPath);

            // 0. Clean runtime debug buttons from MainMenu.unity
            CleanRuntimeDebugButtons();

            // 1. Create Miasma Hazard Pool Prefab
            GameObject hazardPrefab = CreateMiasmaHazardPoolPrefab();

            // 2. Create The Mire Mother Boss Prefab
            GameObject bossPrefab = CreateBossTheMireMotherPrefab(hazardPrefab);

            // 3. Create Mire Ground Material
            Material groundMat = CreateMireGroundMaterial();

            // 4. Assemble Arena in MainGame Scene
            AssembleMainGameWorld3(bossPrefab, groundMat);

            Debug.Log("[World3ArenaBuilder] WORLD 3 Build Complete!");
        }

        public static void CleanRuntimeDebugButtons()
        {
            Scene mmScene = EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool dirty = false;
            foreach (var b in buttons)
            {
                if (b == null) continue;
                Text txt = b.GetComponentInChildren<Text>(true);
                if ((txt != null && txt.text.ToUpper().Contains("[DEBUG]")) || b.gameObject.name.ToUpper().Contains("DEBUG"))
                {
                    Debug.Log($"[World3ArenaBuilder] Destroying runtime debug button: {b.gameObject.name}");
                    UnityEngine.Object.DestroyImmediate(b.gameObject);
                    dirty = true;
                }
            }

            // Also check any standalone Text or GameObject containing DEBUG
            Text[] texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t != null && t.text.ToUpper().Contains("[DEBUG]"))
                {
                    Transform p = t.transform;
                    while (p.parent != null && p.parent.name.ToUpper().Contains("DEBUG")) p = p.parent;
                    Debug.Log($"[World3ArenaBuilder] Destroying runtime debug object: {p.gameObject.name}");
                    UnityEngine.Object.DestroyImmediate(p.gameObject);
                    dirty = true;
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(mmScene);
                EditorSceneManager.SaveScene(mmScene);
            }
        }

        public static GameObject CreateMiasmaHazardPoolPrefab()
        {
            string path = $"{PrefabsPath}/MiasmaHazardPool.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("MiasmaHazardPool");
            MiasmaHazardPool hazard = root.AddComponent<MiasmaHazardPool>();
            hazard.Initialize(6.0f, 1.75f, 6.0f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World3ArenaBuilder] Saved {path}");
            return saved;
        }

        public static Material CreateMireGroundMaterial()
        {
            string path = $"{MaterialsPath}/M_ContinuousBaseGround_W3.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Material genericDirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat");
            Material mat = genericDirt != null ? new Material(genericDirt) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.name = "M_ContinuousBaseGround_W3";
            mat.SetColor("_BaseColor", new Color(0.14f, 0.17f, 0.13f)); // Stagnant peat mud / bog soil
            mat.SetColor("_Color", new Color(0.14f, 0.17f, 0.13f));
            if (mat.HasProperty("_Albedo_Map")) mat.SetTextureScale("_Albedo_Map", new Vector2(16f, 16f));
            if (mat.HasProperty("_Normal_Map")) mat.SetTextureScale("_Normal_Map", new Vector2(16f, 16f));

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World3ArenaBuilder] Created {path}");
            return mat;
        }

        public static GameObject CreateBossTheMireMotherPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_TheMireMother.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                MireMotherBoss existingBoss = existing.GetComponent<MireMotherBoss>();
                EnemyLocomotion existingLoco = existing.GetComponent<EnemyLocomotion>();
                if (existingBoss != null && existingLoco != null) return existing;
            }

            GameObject peasantSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Female_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Plague_Mask_01.prefab");
            GameObject pickSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Models/SM_Gen_Wep_Pickaxe_01.fbx");

            GameObject root = new GameObject("Boss_TheMireMother");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Imposing witch silhouette: 1.35x scale
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.35f;
            cc.radius = 0.95f * 1.35f;
            cc.center = new Vector3(0f, 1.8f * 1.35f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.35f;

            Transform headBone = null;
            Transform handBone = null;

            if (peasantSource != null)
            {
                GameObject peasant = (GameObject)PrefabUtility.InstantiatePrefab(peasantSource, visual.transform);
                peasant.transform.localPosition = Vector3.zero;
                peasant.transform.localRotation = Quaternion.identity;

                Transform[] bones = peasant.GetComponentsInChildren<Transform>(true);
                foreach (Transform b in bones)
                {
                    if (headBone == null && (b.name.Contains("Head") || b.name.Contains("Neck"))) headBone = b;
                    if (handBone == null && (b.name.Contains("Hand_R") || b.name.Contains("RightHand"))) handBone = b;
                }
            }

            // Attach Plague Mask to head
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_Plague_MireMother";
                mask.transform.localPosition = new Vector3(0f, 0.06f, 0.08f);
                mask.transform.localRotation = Quaternion.identity;
                mask.transform.localScale = Vector3.one * 1.05f;
            }

            // Attach Staff / Pickaxe to right hand
            if (pickSource != null && handBone != null)
            {
                GameObject staff = (GameObject)PrefabUtility.InstantiatePrefab(pickSource, handBone);
                staff.name = "Wep_RitualStaff_MireMother";
                staff.transform.localPosition = new Vector3(0.05f, 0.04f, 0.06f);
                staff.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                staff.transform.localScale = Vector3.one * 1.25f;
            }

            // Gameplay components
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.SetMaxHealth(2250f);
            eh.SetHealthDropSettings(0.50f, guaranteed: true, count: 2);

            MireMotherBoss bossComp = root.AddComponent<MireMotherBoss>();
            bossComp.SetMiasmaPoolPrefab(hazardPrefab);

            EnemyLocomotion loco = root.AddComponent<EnemyLocomotion>();
            EnemyController ec = root.AddComponent<EnemyController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World3ArenaBuilder] Saved {path}");
            return saved;
        }

        public static void AssembleMainGameWorld3(GameObject bossPrefab, Material groundMat)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != MainGameScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
            }

            // Find or Create Environment_W3
            GameObject arena = GameObject.Find("Arena");
            GameObject existingW3 = null;
            if (arena != null)
            {
                Transform t = arena.transform.Find("Environment_W3");
                if (t != null) existingW3 = t.gameObject;
            }
            if (existingW3 == null)
            {
                foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (r.name == "Environment_W3") { existingW3 = r; break; }
                }
            }
            if (existingW3 != null)
            {
                UnityEngine.Object.DestroyImmediate(existingW3);
            }

            GameObject envW3 = new GameObject("Environment_W3");
            if (arena != null)
            {
                envW3.transform.SetParent(arena.transform, false);
            }
            envW3.transform.position = Vector3.zero;

            // 1. Ground Continuous Quad (70x70)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground_Continuous_W3";
            ground.transform.SetParent(envW3.transform, false);
            ground.transform.position = new Vector3(0f, 0.01f, 0f);
            ground.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(70f, 70f, 1f);

            MeshRenderer gr = ground.GetComponent<MeshRenderer>();
            if (gr != null && groundMat != null) gr.material = groundMat;

            // 2. Stagnant Water Planes
            GameObject waterPlanePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Water_Plane_01.prefab");
            GameObject waterRoot = new GameObject("SwampWaterPlanes");
            waterRoot.transform.SetParent(envW3.transform, false);

            if (waterPlanePrefab != null)
            {
                Vector3[] waterPositions = new Vector3[]
                {
                    new Vector3(-15f, 0.06f, -14f), // South-West grave marsh
                    new Vector3(-17f, 0.06f, 15f),  // North-West fisher shack
                    new Vector3(2f, 0.06f, 19f),    // North ritual basin
                    new Vector3(14f, 0.06f, -4f)    // Center-East marsh pool
                };
                Vector3[] waterScales = new Vector3[]
                {
                    new Vector3(1.4f, 1f, 1.4f),
                    new Vector3(1.5f, 1f, 1.5f),
                    new Vector3(1.6f, 1f, 1.6f),
                    new Vector3(1.3f, 1f, 1.3f)
                };

                for (int i = 0; i < waterPositions.Length; i++)
                {
                    GameObject wp = (GameObject)PrefabUtility.InstantiatePrefab(waterPlanePrefab, waterRoot.transform);
                    wp.name = $"WaterPlane_0{i + 1}";
                    wp.transform.position = waterPositions[i];
                    wp.transform.localScale = waterScales[i];
                    RemoveColliders(wp);
                }
            }

            // 3. Landmarks Container
            GameObject landmarks = new GameObject("AtmosphericLandmarks_W3");
            landmarks.transform.SetParent(envW3.transform, false);

            // Load verified Synty assets
            GameObject deadTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab");
            GameObject deadTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab");
            GameObject deadTree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab");
            GameObject log1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");
            GameObject log2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_02.prefab");

            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock6 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_06.prefab");
            GameObject rock7 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_07.prefab");
            GameObject rock8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab");
            GameObject rock9 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_09.prefab");
            GameObject rock10 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_10.prefab");

            GameObject plank1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab");
            GameObject plank2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_02.prefab");
            GameObject crate1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_01.prefab");
            GameObject crate2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_02.prefab");
            GameObject cratePreset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Crate_Preset_01.prefab");
            GameObject barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Barrel_Wood_01.prefab");
            GameObject houseDilapidated = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_PolygonApocalypse_Bld_House_01.prefab");

            // --- LANDMARK 1: Sunken Grave Marsh (South-West: -15, 0, -14) ---
            GameObject graveMarsh = new GameObject("Landmark_1_SunkenGraveMarsh");
            graveMarsh.transform.SetParent(landmarks.transform, false);
            Vector3 graveCenter = new Vector3(-15f, 0f, -14f);

            // Submerged tombstones & planks
            Vector3[] graveOffsets = new Vector3[]
            {
                new Vector3(-1.8f, 0f, 1.2f), new Vector3(1.5f, 0f, -1.4f), new Vector3(-0.8f, 0f, -2.2f),
                new Vector3(2.2f, 0f, 1.8f), new Vector3(0f, 0f, 0f), new Vector3(-2.6f, 0f, -0.6f)
            };
            for (int i = 0; i < graveOffsets.Length; i++)
            {
                GameObject gProp = (i % 2 == 0) ? (rock4 ?? rock6) : (plank1 ?? plank2);
                if (gProp != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(gProp, graveMarsh.transform);
                    item.transform.position = graveCenter + graveOffsets[i];
                    item.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(10f, 30f), (i * 61f) % 360f, UnityEngine.Random.Range(-15f, 15f));
                    item.transform.localScale = Vector3.one * (0.85f + (i % 3) * 0.15f);
                    RemoveColliders(item);
                }
            }
            if (deadTree1 != null)
            {
                GameObject willow = (GameObject)PrefabUtility.InstantiatePrefab(deadTree1, graveMarsh.transform);
                willow.transform.position = graveCenter + new Vector3(3f, 0f, -2f);
                willow.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                willow.transform.localScale = Vector3.one * 1.2f;
                RemoveColliders(willow);
            }

            // --- LANDMARK 2: Rotten Fisher Shack (North-West: -18, 0, +16) ---
            GameObject fisherShack = new GameObject("Landmark_2_RottenFisherShack");
            fisherShack.transform.SetParent(landmarks.transform, false);
            Vector3 shackCenter = new Vector3(-18f, 0f, 16f);

            if (houseDilapidated != null)
            {
                GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(houseDilapidated, fisherShack.transform);
                house.transform.position = shackCenter + new Vector3(-2f, -0.55f, 2f);
                house.transform.rotation = Quaternion.Euler(3f, 25f, -2f);
                house.transform.localScale = Vector3.one * 0.75f;
                RemoveColliders(house);
            }
            // Dock planks extending into water
            Vector3[] dockOffsets = new Vector3[]
            {
                new Vector3(1.5f, 0.08f, -0.5f), new Vector3(2.8f, 0.07f, -0.8f), new Vector3(4.0f, 0.06f, -1.0f),
                new Vector3(1.8f, 0f, -2.0f), new Vector3(3.2f, 0f, -2.2f)
            };
            for (int i = 0; i < dockOffsets.Length; i++)
            {
                GameObject p = (i < 3) ? (plank1 ?? plank2) : barrel;
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, fisherShack.transform);
                    item.transform.position = shackCenter + dockOffsets[i];
                    item.transform.rotation = Quaternion.Euler(0f, (i * 45f) % 360f, (i % 2 == 0) ? 5f : -5f);
                    item.transform.localScale = Vector3.one * 0.95f;
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 3: Diseased Root Grove (North-East: +16, 0, +16) ---
            GameObject rootGrove = new GameObject("Landmark_3_DiseasedRootGrove");
            rootGrove.transform.SetParent(landmarks.transform, false);
            Vector3 groveCenter = new Vector3(16f, 0f, 16f);

            Vector3[] treeOffsets = new Vector3[]
            {
                new Vector3(-2.5f, 0f, -1.5f), new Vector3(2.0f, 0f, 2.0f),
                new Vector3(1.8f, 0f, -2.5f), new Vector3(-1.5f, 0f, 2.8f)
            };
            for (int i = 0; i < treeOffsets.Length; i++)
            {
                GameObject tPrefab = (i % 2 == 0) ? (deadTree2 ?? deadTree3) : (deadTree3 ?? deadTree1);
                if (tPrefab != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab, rootGrove.transform);
                    tree.transform.position = groveCenter + treeOffsets[i];
                    tree.transform.rotation = Quaternion.Euler(0f, (i * 90f + 25f), 0f);
                    tree.transform.localScale = Vector3.one * (1.0f + (i % 3) * 0.15f);
                    RemoveColliders(tree);
                }
            }
            if (log1 != null)
            {
                GameObject l1 = (GameObject)PrefabUtility.InstantiatePrefab(log1, rootGrove.transform);
                l1.transform.position = groveCenter + new Vector3(0f, 0.05f, 0.5f);
                l1.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                l1.transform.localScale = Vector3.one * 1.1f;
                RemoveColliders(l1);
            }

            // --- LANDMARK 4: Abandoned Plague Camp (South-East: +18, 0, -15) ---
            GameObject plagueCamp = new GameObject("Landmark_4_AbandonedPlagueCamp");
            plagueCamp.transform.SetParent(landmarks.transform, false);
            Vector3 campCenter = new Vector3(18f, 0f, -15f);

            Vector3[] campOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0f, 1.2f), new Vector3(-1.6f, 0f, 1.0f),
                new Vector3(1.2f, 0f, -1.4f), new Vector3(-1.4f, 0f, -1.2f), new Vector3(2.5f, 0f, -0.4f)
            };
            for (int i = 0; i < campOffsets.Length; i++)
            {
                GameObject cProp = (i == 0) ? (cratePreset ?? crate1) : ((i % 2 == 0) ? (barrel ?? crate2) : (plank2 ?? crate1));
                if (cProp != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(cProp, plagueCamp.transform);
                    item.transform.position = campCenter + campOffsets[i];
                    item.transform.rotation = Quaternion.Euler(0f, (i * 53f) % 360f, (i % 2 == 0) ? 6f : 0f);
                    item.transform.localScale = Vector3.one * (0.9f + (i % 3) * 0.12f);
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 5: Mire Ritual Basin (North-Center: +2, 0, +20) ---
            GameObject ritualBasin = new GameObject("Landmark_5_MireRitualBasin");
            ritualBasin.transform.SetParent(landmarks.transform, false);
            Vector3 basinCenter = new Vector3(2f, 0f, 20f);

            // Ring of 6 jagged megalith rocks encircling basin
            for (int i = 0; i < 6; i++)
            {
                float a = i * (360f / 6f) * Mathf.Deg2Rad;
                Vector3 rPos = basinCenter + new Vector3(Mathf.Sin(a) * 3.6f, 0f, Mathf.Cos(a) * 3.6f);
                GameObject rPrefab = (i % 2 == 0) ? (rock9 ?? rock10) : (rock7 ?? rock8);
                if (rPrefab != null)
                {
                    GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rPrefab, ritualBasin.transform);
                    r.transform.position = rPos;
                    r.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 10f), (i * 60f + 15f), UnityEngine.Random.Range(-5f, 10f));
                    r.transform.localScale = Vector3.one * (1.2f + (i % 2) * 0.25f);
                    RemoveColliders(r);
                }
            }

            // Central Corrupted Altar Light
            GameObject ritualLightObj = new GameObject("RitualBasinLight");
            ritualLightObj.transform.SetParent(ritualBasin.transform, false);
            ritualLightObj.transform.position = basinCenter + Vector3.up * 1.2f;
            Light rLight = ritualLightObj.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = new Color(0.20f, 0.85f, 0.40f);
            rLight.range = 7.5f;
            rLight.intensity = 1.8f;

            // 4. Perimeter Boundary Dressing (Surrounding dense swamp woods)
            GameObject boundaryProps = new GameObject("BoundaryProps");
            boundaryProps.transform.SetParent(envW3.transform, false);

            for (int i = -30; i <= 30; i += 7)
            {
                // North edge
                SpawnPerimeterProp(deadTree2 ?? deadTree1, new Vector3(i, 0f, 32f), boundaryProps.transform);
                // South edge
                SpawnPerimeterProp(deadTree3 ?? deadTree2, new Vector3(i, 0f, -32f), boundaryProps.transform);
                // East edge
                SpawnPerimeterProp(deadTree1 ?? deadTree3, new Vector3(32f, 0f, i), boundaryProps.transform);
                // West edge
                SpawnPerimeterProp(deadTree2 ?? deadTree1, new Vector3(-32f, 0f, i), boundaryProps.transform);
            }

            // 5. Mire Spore Particles
            GameObject sporeObj = new GameObject("Mire_Spore_Particles");
            sporeObj.transform.SetParent(envW3.transform, false);
            sporeObj.transform.position = new Vector3(0f, 1.5f, 0f);

            ParticleSystem ps = sporeObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 5.0f;
            main.startSpeed = 0.4f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.35f, 0.85f, 0.45f, 0.45f);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 3f, 45f);

            // Connect to WorldEnvironmentController
            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (wec != null)
            {
                wec.FindReferencesIfMissing();
                // Assign via reflection/fields or SetEnvironments
                wec.SetEnvironments(wec.EnvironmentW1, wec.EnvironmentW2, envW3,
                    RenderSettings.sun, wec.AshParticles, ps);
                Debug.Log("[World3ArenaBuilder] Connected Environment_W3 to WorldEnvironmentController.");
            }

            // Connect to SpawnDirector
            SpawnDirector sd = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null && bossPrefab != null)
            {
                SerializedObject so = new SerializedObject(sd);
                SerializedProperty prop = so.FindProperty("mireMotherBossPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = bossPrefab;
                    so.ApplyModifiedProperties();
                    Debug.Log("[World3ArenaBuilder] Assigned mireMotherBossPrefab to SpawnDirector.");
                }
            }

            // Default to inactive so World 1 remains default on clean start
            envW3.SetActive(false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[World3ArenaBuilder] Environment_W3 assembled and saved successfully in MainGame.unity!");
        }

        private static void SpawnPerimeterProp(GameObject prefab, Vector3 pos, Transform parent)
        {
            if (prefab == null) return;
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0f, UnityEngine.Random.Range(-1.5f, 1.5f));
            obj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            obj.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);
            RemoveColliders(obj);
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
