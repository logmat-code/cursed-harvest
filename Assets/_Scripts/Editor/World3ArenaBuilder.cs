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
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Material genericDirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat");
                mat = genericDirt != null ? new Material(genericDirt) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.name = "M_ContinuousBaseGround_W3";
                AssetDatabase.CreateAsset(mat, path);
            }

            Color peatColor = new Color(0.18f, 0.16f, 0.12f, 1.0f); // Dark rich peat mud / bog soil
            mat.SetColor("_BaseColor", peatColor);
            mat.SetColor("_Color", peatColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Albedo_Map")) mat.SetTextureScale("_Albedo_Map", new Vector2(16f, 16f));
            if (mat.HasProperty("_Normal_Map")) mat.SetTextureScale("_Normal_Map", new Vector2(16f, 16f));

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World3ArenaBuilder] Updated {path} with dark peat mud color.");
            return mat;
        }

        public static Material CreateSwampWaterMaterial()
        {
            string path = $"{MaterialsPath}/M_SwampWater_W3.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Material genericWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/Generic_Water.mat");
                mat = genericWater != null ? new Material(genericWater) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.name = "M_SwampWater_W3";
                AssetDatabase.CreateAsset(mat, path);
            }

            // Desaturated murky blue-green bog water
            Color bogWaterColor = new Color(0.14f, 0.28f, 0.24f, 0.88f);
            mat.SetColor("_BaseColor", bogWaterColor);
            mat.SetColor("_Color", bogWaterColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.85f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World3ArenaBuilder] Updated {path} with desaturated blue-green swamp water.");
            return mat;
        }

        public static GameObject CreateBossTheMireMotherPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_TheMireMother.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
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

            Material waterMat = CreateSwampWaterMaterial();

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

            // ==========================================
            // 1. BASE GROUND (86m x 86m Continuous Peat Mud Plane)
            // ==========================================
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Continuous_W3";
            ground.transform.SetParent(envW3.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(8.6f, 1f, 8.6f); // 86m x 86m upward-facing plane

            RemoveColliders(ground);
            MeshRenderer gr = ground.GetComponent<MeshRenderer>();
            if (gr != null && groundMat != null) gr.sharedMaterial = groundMat;

            // Load verified authentic Synty assets
            GameObject deadTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_01.prefab");
            GameObject deadTree2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_02.prefab");
            GameObject deadTree3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Tree_Dead_03.prefab");
            GameObject stump1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_01.prefab");
            GameObject stump2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_02.prefab");
            GameObject stump3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stump_03.prefab");
            GameObject log1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_01.prefab");
            GameObject log2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Log_02.prefab");

            GameObject root1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Root_01.prefab");
            GameObject root2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Root_02.prefab");

            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_05.prefab");
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

            GameObject lilypad1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Lilypads_01.prefab");
            GameObject lilypad2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Lilypads_02.prefab");
            GameObject lilypad3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Lilypads_03.prefab");
            GameObject mushroom1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_01.prefab");
            GameObject mushroom2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Mushroom_02.prefab");
            GameObject unlitPumpkin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab");

            GameObject waterPlanePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Water_Plane_01.prefab");

            // ==========================================
            // 2. STAGNANT SWAMP WATER POOLS & ORGANIC BORDER MASKING
            // ==========================================
            GameObject waterRoot = new GameObject("SwampWaterPlanes");
            waterRoot.transform.SetParent(envW3.transform, false);

            Vector3[] poolCenters = new Vector3[]
            {
                new Vector3(-16f, 0.05f, -15f), // South-West grave marsh pool
                new Vector3(-18f, 0.05f, 16f),  // North-West fisher shack pool
                new Vector3(0f, 0.05f, 3f),     // Center ritual basin pool
                new Vector3(18f, 0.05f, 16f),   // North-East root grove pool
                new Vector3(18f, 0.05f, -15f)   // South-East plague camp pool
            };

            Vector2[] poolSizes = new Vector2[]
            {
                new Vector2(15f, 15f),
                new Vector2(16f, 16f),
                new Vector2(13f, 13f),
                new Vector2(15f, 15f),
                new Vector2(13f, 13f)
            };

            GameObject[] rocksList = new GameObject[] { rock4, rock5, rock6, rock7, rock8, rock9, rock10 };
            GameObject[] rootsList = new GameObject[] { root1, root2, log1, log2 };
            GameObject[] stumpsList = new GameObject[] { stump1, stump2, stump3 };
            GameObject[] lilypadsList = new GameObject[] { lilypad1, lilypad2, lilypad3 };

            if (waterPlanePrefab != null)
            {
                for (int i = 0; i < poolCenters.Length; i++)
                {
                    GameObject wp = (GameObject)PrefabUtility.InstantiatePrefab(waterPlanePrefab, waterRoot.transform);
                    wp.name = $"SwampWaterPool_0{i + 1}";
                    wp.transform.position = poolCenters[i];
                    wp.transform.localScale = new Vector3(poolSizes[i].x / 10f, 1f, poolSizes[i].y / 10f);
                    RemoveColliders(wp);

                    // Apply desaturated blue-green swamp water material
                    MeshRenderer mrW = wp.GetComponentInChildren<MeshRenderer>();
                    if (mrW != null && waterMat != null) mrW.sharedMaterial = waterMat;

                    // Scatter lilypads on surface
                    for (int l = 0; l < 4; l++)
                    {
                        GameObject lpPrefab = lilypadsList[l % lilypadsList.Length];
                        if (lpPrefab != null)
                        {
                            GameObject lp = (GameObject)PrefabUtility.InstantiatePrefab(lpPrefab, wp.transform);
                            lp.transform.position = poolCenters[i] + new Vector3(
                                UnityEngine.Random.Range(-poolSizes[i].x * 0.32f, poolSizes[i].x * 0.32f),
                                0.06f,
                                UnityEngine.Random.Range(-poolSizes[i].y * 0.32f, poolSizes[i].y * 0.32f));
                            lp.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            lp.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.25f);
                            RemoveColliders(lp);
                        }
                    }

                    // CRITICAL: Mask water plane borders with perimeter rocks, mud mounds, roots, and logs
                    MaskWaterPlaneBorders(waterRoot.transform, poolCenters[i], poolSizes[i], rocksList, rootsList, stumpsList);
                }
            }

            // ==========================================
            // 3. 5 DISTINCT ATMOSPHERIC LANDMARKS
            // ==========================================
            GameObject landmarks = new GameObject("AtmosphericLandmarks_W3");
            landmarks.transform.SetParent(envW3.transform, false);

            // --- LANDMARK 1: Sunken Grave Marsh (South-West: -16, 0, -15) ---
            GameObject graveMarsh = new GameObject("Landmark_1_SunkenGraveMarsh");
            graveMarsh.transform.SetParent(landmarks.transform, false);
            Vector3 graveCenter = new Vector3(-16f, 0f, -15f);

            // Submerged tombstones & tilted planks
            Vector3[] graveOffsets = new Vector3[]
            {
                new Vector3(-2.2f, 0f, 1.5f), new Vector3(1.8f, 0f, -1.8f), new Vector3(-1.0f, 0f, -2.5f),
                new Vector3(2.5f, 0f, 2.0f), new Vector3(0f, 0f, 0f), new Vector3(-2.8f, 0f, -0.8f),
                new Vector3(0.8f, 0f, 3.0f), new Vector3(-3.2f, 0f, 1.8f)
            };
            for (int i = 0; i < graveOffsets.Length; i++)
            {
                GameObject gProp = (i % 2 == 0) ? (rock4 ?? rock6) : (plank1 ?? plank2);
                if (gProp != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(gProp, graveMarsh.transform);
                    item.transform.position = graveCenter + graveOffsets[i];
                    item.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(15f, 35f), (i * 57f) % 360f, UnityEngine.Random.Range(-15f, 15f));
                    item.transform.localScale = Vector3.one * (0.9f + (i % 3) * 0.15f);
                    RemoveColliders(item);
                }
            }
            if (deadTree1 != null)
            {
                GameObject weepingWillow = (GameObject)PrefabUtility.InstantiatePrefab(deadTree1, graveMarsh.transform);
                weepingWillow.transform.position = graveCenter + new Vector3(3.2f, 0f, -2.5f);
                weepingWillow.transform.rotation = Quaternion.Euler(5f, 40f, -4f);
                weepingWillow.transform.localScale = Vector3.one * 1.25f;
                RemoveColliders(weepingWillow);
            }

            // --- LANDMARK 2: Rotten Fisher Shack (North-West: -18, 0, +16) ---
            GameObject fisherShack = new GameObject("Landmark_2_RottenFisherShack");
            fisherShack.transform.SetParent(landmarks.transform, false);
            Vector3 shackCenter = new Vector3(-18f, 0f, 16f);

            if (houseDilapidated != null)
            {
                GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(houseDilapidated, fisherShack.transform);
                house.transform.position = shackCenter + new Vector3(-2.5f, -0.55f, 2.5f);
                house.transform.rotation = Quaternion.Euler(3f, 28f, -3f);
                house.transform.localScale = Vector3.one * 0.78f;
                RemoveColliders(house);
            }
            // Decaying dock planks extending into marsh water
            Vector3[] dockOffsets = new Vector3[]
            {
                new Vector3(1.2f, 0.08f, -0.6f), new Vector3(2.6f, 0.07f, -1.0f), new Vector3(4.0f, 0.06f, -1.4f),
                new Vector3(5.2f, 0.05f, -1.8f), new Vector3(1.8f, 0f, -2.2f), new Vector3(3.4f, 0f, -2.6f)
            };
            for (int i = 0; i < dockOffsets.Length; i++)
            {
                GameObject p = (i < 4) ? (plank1 ?? plank2) : barrel;
                if (p != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(p, fisherShack.transform);
                    item.transform.position = shackCenter + dockOffsets[i];
                    item.transform.rotation = Quaternion.Euler(0f, (i * 35f) % 360f, (i % 2 == 0) ? 4f : -4f);
                    item.transform.localScale = Vector3.one * 0.95f;
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 3: Diseased Root Grove (North-East: +18, 0, +16) ---
            GameObject rootGrove = new GameObject("Landmark_3_DiseasedRootGrove");
            rootGrove.transform.SetParent(landmarks.transform, false);
            Vector3 groveCenter = new Vector3(18f, 0f, 16f);

            Vector3[] groveTreeOffsets = new Vector3[]
            {
                new Vector3(-2.8f, 0f, -1.8f), new Vector3(2.2f, 0f, 2.2f),
                new Vector3(2.0f, 0f, -2.8f), new Vector3(-1.8f, 0f, 3.0f)
            };
            for (int i = 0; i < groveTreeOffsets.Length; i++)
            {
                GameObject tPrefab = (i % 2 == 0) ? (deadTree2 ?? deadTree3) : (deadTree3 ?? deadTree1);
                if (tPrefab != null)
                {
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(tPrefab, rootGrove.transform);
                    tree.transform.position = groveCenter + groveTreeOffsets[i];
                    tree.transform.rotation = Quaternion.Euler(0f, (i * 85f + 30f), 0f);
                    tree.transform.localScale = Vector3.one * (1.1f + (i % 3) * 0.15f);
                    RemoveColliders(tree);
                }
            }
            if (log1 != null)
            {
                GameObject l1 = (GameObject)PrefabUtility.InstantiatePrefab(log1, rootGrove.transform);
                l1.transform.position = groveCenter + new Vector3(0f, 0.05f, 0.5f);
                l1.transform.rotation = Quaternion.Euler(0f, 38f, 0f);
                l1.transform.localScale = Vector3.one * 1.2f;
                RemoveColliders(l1);
            }
            if (root1 != null)
            {
                GameObject r1 = (GameObject)PrefabUtility.InstantiatePrefab(root1, rootGrove.transform);
                r1.transform.position = groveCenter + new Vector3(-1.2f, 0.02f, 0.8f);
                r1.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
                r1.transform.localScale = Vector3.one * 1.35f;
                RemoveColliders(r1);
            }
            // Fungal mushroom clusters
            for (int m = 0; m < 4; m++)
            {
                GameObject mPrefab = (m % 2 == 0) ? (mushroom1 ?? mushroom2) : mushroom2;
                if (mPrefab != null)
                {
                    GameObject mush = (GameObject)PrefabUtility.InstantiatePrefab(mPrefab, rootGrove.transform);
                    mush.transform.position = groveCenter + new Vector3(0.5f + (m * 0.6f), 0.08f, -0.4f + (m * 0.4f));
                    mush.transform.localScale = Vector3.one * 1.2f;
                    RemoveColliders(mush);
                }
            }

            // --- LANDMARK 4: Abandoned Plague Camp (South-East: +18, 0, -15) ---
            GameObject plagueCamp = new GameObject("Landmark_4_AbandonedPlagueCamp");
            plagueCamp.transform.SetParent(landmarks.transform, false);
            Vector3 campCenter = new Vector3(18f, 0f, -15f);

            Vector3[] campOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(1.6f, 0f, 1.4f), new Vector3(-1.8f, 0f, 1.2f),
                new Vector3(1.4f, 0f, -1.6f), new Vector3(-1.6f, 0f, -1.4f), new Vector3(2.8f, 0f, -0.5f),
                new Vector3(-2.6f, 0f, -0.2f)
            };
            for (int i = 0; i < campOffsets.Length; i++)
            {
                GameObject cProp = (i == 0) ? (cratePreset ?? crate1) : ((i % 2 == 0) ? (barrel ?? crate2) : (plank2 ?? crate1));
                if (cProp != null)
                {
                    GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(cProp, plagueCamp.transform);
                    item.transform.position = campCenter + campOffsets[i];
                    item.transform.rotation = Quaternion.Euler(0f, (i * 51f) % 360f, (i % 2 == 0) ? 6f : 0f);
                    item.transform.localScale = Vector3.one * (0.92f + (i % 3) * 0.12f);
                    RemoveColliders(item);
                }
            }

            // --- LANDMARK 5: Mire Ritual Basin (Center: 0, 0, 0) ---
            GameObject ritualBasin = new GameObject("Landmark_5_MireRitualBasin");
            ritualBasin.transform.SetParent(landmarks.transform, false);
            Vector3 basinCenter = new Vector3(0f, 0f, 0f);

            // Ring of 8 jagged megalith rocks encircling basin (radius 4.6m, keeping center clear for spawn)
            for (int i = 0; i < 8; i++)
            {
                float a = i * (360f / 8f) * Mathf.Deg2Rad;
                Vector3 rPos = basinCenter + new Vector3(Mathf.Sin(a) * 4.6f, 0f, Mathf.Cos(a) * 4.6f);
                GameObject rPrefab = (i % 2 == 0) ? (rock9 ?? rock10) : (rock7 ?? rock8);
                if (rPrefab != null)
                {
                    GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rPrefab, ritualBasin.transform);
                    r.transform.position = rPos;
                    r.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 10f), (i * 45f + 20f), UnityEngine.Random.Range(-5f, 10f));
                    r.transform.localScale = Vector3.one * (1.25f + (i % 2) * 0.25f);
                    RemoveColliders(r);
                }
            }

            // Central Altar Stone with sickly muted amber/green illumination
            if (rock8 != null)
            {
                GameObject altarRock = (GameObject)PrefabUtility.InstantiatePrefab(rock8, ritualBasin.transform);
                altarRock.name = "CorruptedAltarStone";
                altarRock.transform.position = basinCenter + new Vector3(0f, -0.1f, 3.2f);
                altarRock.transform.rotation = Quaternion.Euler(-10f, 30f, 5f);
                altarRock.transform.localScale = new Vector3(1.6f, 0.7f, 1.4f);
                RemoveColliders(altarRock);
            }

            // Central Corrupted Altar Light (sickly muted amber/green)
            GameObject ritualLightObj = new GameObject("RitualBasinLight");
            ritualLightObj.transform.SetParent(ritualBasin.transform, false);
            ritualLightObj.transform.position = basinCenter + new Vector3(0f, 1.2f, 3.0f);
            Light rLight = ritualLightObj.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = new Color(0.32f, 0.75f, 0.28f); // Sickly muted amber/green
            rLight.range = 8.5f;
            rLight.intensity = 1.9f;

            // Occult dressing around the megalith ring
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 pPos = basinCenter + new Vector3(Mathf.Sin(a) * 4.4f, 0f, Mathf.Cos(a) * 4.4f);
                GameObject pumpkin = (unlitPumpkin ?? crate1);
                if (pumpkin != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pumpkin, ritualBasin.transform);
                    p.transform.position = pPos;
                    p.transform.rotation = Quaternion.Euler(0f, i * 70f, 0f);
                    p.transform.localScale = Vector3.one * 0.85f;
                    RemoveColliders(p);
                }
            }

            // ==========================================
            // 4. VISIBLE PERIMETER WALLS (Dense Swamp Boundary)
            // ==========================================
            GameObject perimeter = new GameObject("VisiblePerimeterWalls_W3");
            perimeter.transform.SetParent(envW3.transform, false);

            float b = 31.5f;
            for (float x = -b; x <= b; x += 5.5f)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, b), deadTree2, rock9);
                SpawnPerimeterNode(perimeter.transform, new Vector3(x, 0f, -b), deadTree3, rock10);
            }
            for (float z = -b + 5.5f; z < b; z += 5.5f)
            {
                SpawnPerimeterNode(perimeter.transform, new Vector3(b, 0f, z), deadTree1, rock7);
                SpawnPerimeterNode(perimeter.transform, new Vector3(-b, 0f, z), deadTree2, rock8);
            }

            // ==========================================
            // 5. MIRE SPORE PARTICLES
            // ==========================================
            GameObject sporeObj = new GameObject("Mire_Spore_Particles");
            sporeObj.transform.SetParent(envW3.transform, false);
            sporeObj.transform.position = new Vector3(0f, 1.5f, 0f);

            ParticleSystem ps = sporeObj.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 5.0f;
            main.startSpeed = 0.4f;
            main.startSize = 0.14f;
            main.startColor = new Color(0.35f, 0.85f, 0.45f, 0.45f);
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 25f;

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
                wec.SetEnvironments(wec.EnvironmentW1, wec.EnvironmentW2, envW3,
                    RenderSettings.sun, wec.AshParticles, ps);
                Debug.Log("[World3ArenaBuilder] Connected Environment_W3 to WorldEnvironmentController.");
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
            if (sd != null && bossPrefab != null)
            {
                sd.SetMireMotherBossPrefab(bossPrefab);
                EditorUtility.SetDirty(sd);
                Debug.Log("[World3ArenaBuilder] Assigned mireMotherBossPrefab to SpawnDirector.");
            }

            // Default to inactive so World 1 remains default on clean start
            envW3.SetActive(false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[World3ArenaBuilder] Environment_W3 rebuilt and saved successfully in MainGame.unity!");
        }

        private static void MaskWaterPlaneBorders(Transform parent, Vector3 center, Vector2 size, GameObject[] rocks, GameObject[] roots, GameObject[] stumps)
        {
            float halfX = size.x * 0.48f;
            float halfY = size.y * 0.48f;
            float step = 2.6f;

            // North & South edges
            for (float x = -halfX; x <= halfX; x += step)
            {
                SpawnMaskEdgeItem(parent, center + new Vector3(x, 0.02f, halfY), rocks, roots, stumps);
                SpawnMaskEdgeItem(parent, center + new Vector3(x, 0.02f, -halfY), rocks, roots, stumps);
            }
            // East & West edges
            for (float z = -halfY + step; z < halfY; z += step)
            {
                SpawnMaskEdgeItem(parent, center + new Vector3(halfX, 0.02f, z), rocks, roots, stumps);
                SpawnMaskEdgeItem(parent, center + new Vector3(-halfX, 0.02f, z), rocks, roots, stumps);
            }
        }

        private static void SpawnMaskEdgeItem(Transform parent, Vector3 pos, GameObject[] rocks, GameObject[] roots, GameObject[] stumps)
        {
            int pick = UnityEngine.Random.Range(0, 3);
            GameObject prefab = null;
            if (pick == 0 && rocks != null && rocks.Length > 0) prefab = rocks[UnityEngine.Random.Range(0, rocks.Length)];
            else if (pick == 1 && roots != null && roots.Length > 0) prefab = roots[UnityEngine.Random.Range(0, roots.Length)];
            else if (stumps != null && stumps.Length > 0) prefab = stumps[UnityEngine.Random.Range(0, stumps.Length)];

            if (prefab != null)
            {
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                item.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 0f, UnityEngine.Random.Range(-0.35f, 0.35f));
                item.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-8f, 8f));
                item.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.25f);
                RemoveColliders(item);
            }
        }

        private static void SpawnPerimeterNode(Transform parent, Vector3 pos, GameObject treePrefab, GameObject rockPrefab)
        {
            if (treePrefab != null)
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parent);
                tree.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
                tree.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                tree.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.05f, 1.35f);
                RemoveColliders(tree);
            }
            if (rockPrefab != null)
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, parent);
                rock.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 0f, UnityEngine.Random.Range(-1.2f, 1.2f));
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
