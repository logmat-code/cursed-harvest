using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class World5ArenaBuilder
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string MaterialsPath = "Assets/_Materials";
        private const string MainGameScenePath = "Assets/_Scenes/MainGame.unity";

        [MenuItem("Cursed Harvest/World 5/Build World 5 Arena and Bosses")]
        public static void BuildWorld5()
        {
            Debug.Log("[World5ArenaBuilder] ==================================================");
            Debug.Log("[World5ArenaBuilder] Building WORLD 5 — THE ETERNAL ECLIPSE...");
            Debug.Log("[World5ArenaBuilder] ==================================================");

            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            if (!Directory.Exists(MaterialsPath)) Directory.CreateDirectory(MaterialsPath);

            // 1. Create Void Rift Hazard Pool Prefab
            GameObject hazardPrefab = CreateVoidRiftHazardPoolPrefab();

            // 2. Create Eclipse Herald (Mid-Boss Wave 10) Prefab
            GameObject midBossPrefab = CreateEclipseHeraldBossPrefab(hazardPrefab);

            // 3. Create Primeval Horror (Final Boss Wave 20) Prefab
            GameObject finalBossPrefab = CreatePrimevalHorrorBossPrefab(hazardPrefab);

            // 4. Create Eclipse Basalt Ground Material
            Material groundMat = CreateEclipseGroundMaterial();

            // 5. Assemble Arena in MainGame Scene
            AssembleMainGameWorld5(midBossPrefab, finalBossPrefab, groundMat);

            Debug.Log("[World5ArenaBuilder] WORLD 5 Build Complete!");
        }

        public static GameObject CreateVoidRiftHazardPoolPrefab()
        {
            string path = $"{PrefabsPath}/VoidRiftHazardPool.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("VoidRiftHazardPool");
            VoidRiftHazardPool hazard = root.AddComponent<VoidRiftHazardPool>();
            hazard.Initialize(7.5f, 1.75f, 12.0f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World5ArenaBuilder] Saved {path}");
            return saved;
        }

        public static GameObject CreateEclipseHeraldBossPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_EclipseHerald.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            if (charredSource == null) charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Skeleton_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab");
            GameObject wepSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab");

            GameObject root = new GameObject("Boss_EclipseHerald");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Shadow herald silhouette: 1.25x scale
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.25f;
            cc.radius = 0.90f * 1.25f;
            cc.center = new Vector3(0f, 1.8f * 1.25f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.25f;

            Transform headBone = null;
            Transform handBone = null;

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
                }
            }

            // Attach Demonic Mask to head
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_Herald";
                mask.transform.localPosition = new Vector3(0f, 0.05f, 0.06f);
                mask.transform.localRotation = Quaternion.identity;
                mask.transform.localScale = Vector3.one * 1.1f;
            }

            // Attach glowing pickaxe to right hand
            if (wepSource != null && handBone != null)
            {
                GameObject wep = (GameObject)PrefabUtility.InstantiatePrefab(wepSource, handBone);
                wep.name = "Wep_Herald";
                wep.transform.localPosition = new Vector3(0.04f, 0.04f, 0.08f);
                wep.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                wep.transform.localScale = Vector3.one * 1.35f;
            }

            // Gameplay components
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.SetMaxHealth(3400f);
            eh.SetHealthDropSettings(0.50f, guaranteed: true, count: 2);

            EclipseHeraldBoss bossComp = root.AddComponent<EclipseHeraldBoss>();
            if (hazardPrefab != null) bossComp.SetVoidRiftPrefab(hazardPrefab);

            EnemyLocomotion loco = root.AddComponent<EnemyLocomotion>();
            EnemyController ec = root.AddComponent<EnemyController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World5ArenaBuilder] Saved {path}");
            return saved;
        }

        public static GameObject CreatePrimevalHorrorBossPrefab(GameObject hazardPrefab)
        {
            string path = $"{PrefabsPath}/Boss_PrimevalHorror.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Charred_01.prefab");
            if (charredSource == null) charredSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Characters/SM_Gen_Chr_Peasent_Male_01.prefab");
            GameObject maskSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab");
            GameObject swordSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Sword_01.prefab");
            GameObject spireSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab");

            GameObject root = new GameObject("Boss_PrimevalHorror");
            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0) root.layer = enemyLayer;

            // Massive eldritch avatar silhouette: 1.55x scale
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 3.6f * 1.55f;
            cc.radius = 1.10f * 1.55f;
            cc.center = new Vector3(0f, 1.8f * 1.55f, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.55f;

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

            // Attach Demonic Mask
            if (maskSource != null && headBone != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskSource, headBone);
                mask.name = "Mask_Horror";
                mask.transform.localPosition = new Vector3(0f, 0.08f, 0.08f);
                mask.transform.localRotation = Quaternion.identity;
                mask.transform.localScale = Vector3.one * 1.25f;
            }

            // Attach Occult Greatsword
            if (swordSource != null && handBone != null)
            {
                GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordSource, handBone);
                sword.name = "Wep_AbyssalBlade";
                sword.transform.localPosition = new Vector3(0.06f, 0.05f, 0.08f);
                sword.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                sword.transform.localScale = Vector3.one * 1.6f;
            }

            // Attach Void Spires to back
            if (spireSource != null && spineBone != null)
            {
                GameObject spire1 = (GameObject)PrefabUtility.InstantiatePrefab(spireSource, spineBone);
                spire1.name = "Spire_Back_Left";
                spire1.transform.localPosition = new Vector3(-0.15f, 0.20f, -0.15f);
                spire1.transform.localRotation = Quaternion.Euler(-25f, -20f, 0f);
                spire1.transform.localScale = Vector3.one * 0.35f;
                RemoveColliders(spire1);

                GameObject spire2 = (GameObject)PrefabUtility.InstantiatePrefab(spireSource, spineBone);
                spire2.name = "Spire_Back_Right";
                spire2.transform.localPosition = new Vector3(0.15f, 0.20f, -0.15f);
                spire2.transform.localRotation = Quaternion.Euler(-25f, 20f, 0f);
                spire2.transform.localScale = Vector3.one * 0.35f;
                RemoveColliders(spire2);
            }

            // Gameplay components
            EnemyHealth eh = root.AddComponent<EnemyHealth>();
            eh.SetMaxHealth(5200f);
            eh.SetHealthDropSettings(0.70f, guaranteed: true, count: 4);

            PrimevalHorrorBoss bossComp = root.AddComponent<PrimevalHorrorBoss>();
            if (hazardPrefab != null) bossComp.SetVoidRiftPoolPrefab(hazardPrefab);

            EnemyLocomotion loco = root.AddComponent<EnemyLocomotion>();
            EnemyController ec = root.AddComponent<EnemyController>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[World5ArenaBuilder] Saved {path}");
            return saved;
        }

        public static Material CreateEclipseGroundMaterial()
        {
            string path = $"{MaterialsPath}/M_ContinuousBaseGround_W5.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "M_ContinuousBaseGround_W5";
                AssetDatabase.CreateAsset(mat, path);
            }

            // Deep obsidian basalt with cold occult purple tone
            Color eclipseGroundColor = new Color(0.09f, 0.07f, 0.13f, 1.0f);
            mat.SetColor("_BaseColor", eclipseGroundColor);
            mat.SetColor("_Color", eclipseGroundColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[World5ArenaBuilder] Created/Updated {path}");
            return mat;
        }

        public static void AssembleMainGameWorld5(GameObject midBossPrefab, GameObject finalBossPrefab, Material groundMat)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != MainGameScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
            }

            // Find or Create Environment_W5
            GameObject arena = GameObject.Find("Arena");
            GameObject existingW5 = null;
            if (arena != null)
            {
                Transform t = arena.transform.Find("Environment_W5");
                if (t != null) existingW5 = t.gameObject;
            }
            if (existingW5 == null)
            {
                foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (r.name == "Environment_W5") { existingW5 = r; break; }
                }
            }
            if (existingW5 != null)
            {
                UnityEngine.Object.DestroyImmediate(existingW5);
            }

            GameObject envW5 = new GameObject("Environment_W5");
            if (arena != null)
            {
                envW5.transform.SetParent(arena.transform, false);
            }
            envW5.transform.position = Vector3.zero;

            // ==========================================
            // 1. BASE GROUND (86m x 86m Obsidian Basalt Plane)
            // ==========================================
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Continuous_W5";
            ground.transform.SetParent(envW5.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(8.6f, 1f, 8.6f);

            RemoveColliders(ground);
            MeshRenderer gr = ground.GetComponent<MeshRenderer>();
            if (gr != null && groundMat != null) gr.sharedMaterial = groundMat;

            // Load verified Synty assets
            GameObject arch1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Arch_01.prefab");
            GameObject arch2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Arch_02.prefab");

            GameObject cliffPillar1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_Pillar_01.prefab");
            GameObject pillarBase2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Pillar_02.prefab");
            GameObject pillarBase4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Pillar_04.prefab");

            GameObject wallDestroyed1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_Destroyed_01.prefab");
            GameObject wallDestroyed2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Base/SM_Bld_Base_Wall_Destroyed_02.prefab");

            GameObject statue1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Statue_01.prefab");
            GameObject statue2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Statue_02.prefab");

            GameObject cliff1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_01.prefab");
            GameObject cliff2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_02.prefab");
            GameObject cliff3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Cliff_03.prefab");

            GameObject rock1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_01.prefab");
            GameObject rock2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_02.prefab");
            GameObject rock4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_04.prefab");
            GameObject rock6 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_06.prefab");
            GameObject rock8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_08.prefab");

            GameObject stalactite1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_01.prefab");
            GameObject stalactite2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Stalactite_02.prefab");

            // ==========================================
            // 2. FIVE ATMOSPHERIC LANDMARKS
            // ==========================================
            GameObject landmarksRoot = new GameObject("AtmosphericLandmarks_W5");
            landmarksRoot.transform.SetParent(envW5.transform, false);

            // LANDMARK 1: CENTRAL ECLIPSE ALTAR (Center 0, 0, 0)
            // Flat ritual dais (radius 4.2m, height 0.15m) + 6 peripheral pillars at 3.8m radius. Completely open center!
            GameObject landmark1 = new GameObject("Landmark_1_CentralEclipseAltar");
            landmark1.transform.SetParent(landmarksRoot.transform, false);
            landmark1.transform.position = Vector3.zero;

            GameObject altarDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altarDais.name = "EclipseAltar_Dais";
            altarDais.transform.SetParent(landmark1.transform, false);
            altarDais.transform.position = new Vector3(0f, 0.08f, 0f);
            altarDais.transform.localScale = new Vector3(8.5f, 0.08f, 8.5f);
            RemoveColliders(altarDais);
            if (groundMat != null) altarDais.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 6 Outer Runic Pillars
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pillarPos = new Vector3(Mathf.Sin(angle) * 3.8f, 0f, Mathf.Cos(angle) * 3.8f);
                GameObject prefab = (i % 2 == 0 && pillarBase2 != null) ? pillarBase2 : (pillarBase4 != null ? pillarBase4 : pillarBase2);
                if (prefab != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, landmark1.transform);
                    p.transform.position = pillarPos;
                    p.transform.rotation = Quaternion.Euler(0f, -i * 60f, 0f);
                    p.transform.localScale = new Vector3(1.1f, 1.4f, 1.1f);
                    RemoveColliders(p);
                }
            }

            // Central Occult Violet Light
            GameObject altarLight = new GameObject("Altar_VioletLight");
            altarLight.transform.SetParent(landmark1.transform, false);
            altarLight.transform.position = new Vector3(0f, 1.2f, 0f);
            Light cl = altarLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(0.75f, 0.25f, 1.0f);
            cl.intensity = 2.4f;
            cl.range = 9.0f;

            // LANDMARK 2: OBSIDIAN CRATER (North, 0, 0, 22)
            GameObject landmark2 = new GameObject("Landmark_2_ObsidianCrater");
            landmark2.transform.SetParent(landmarksRoot.transform, false);
            landmark2.transform.position = new Vector3(0f, 0f, 22f);

            if (cliffPillar1 != null)
            {
                GameObject cp1 = (GameObject)PrefabUtility.InstantiatePrefab(cliffPillar1, landmark2.transform);
                cp1.transform.localPosition = new Vector3(-3.5f, -0.5f, 2.5f);
                cp1.transform.localRotation = Quaternion.Euler(15f, 35f, 0f);
                cp1.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
                RemoveColliders(cp1);

                GameObject cp2 = (GameObject)PrefabUtility.InstantiatePrefab(cliffPillar1, landmark2.transform);
                cp2.transform.localPosition = new Vector3(3.5f, -0.5f, 2.0f);
                cp2.transform.localRotation = Quaternion.Euler(15f, -40f, 0f);
                cp2.transform.localScale = new Vector3(1.3f, 1.5f, 1.3f);
                RemoveColliders(cp2);
            }

            if (rock4 != null)
            {
                GameObject r1 = (GameObject)PrefabUtility.InstantiatePrefab(rock4, landmark2.transform);
                r1.transform.localPosition = new Vector3(0f, -0.2f, 3.2f);
                r1.transform.localRotation = Quaternion.Euler(0f, 120f, 0f);
                r1.transform.localScale = Vector3.one * 1.6f;
                RemoveColliders(r1);
            }

            if (stalactite1 != null)
            {
                for (int s = 0; s < 4; s++)
                {
                    float angle = s * 90f * Mathf.Deg2Rad;
                    Vector3 sPos = new Vector3(Mathf.Sin(angle) * 2.2f, -0.2f, Mathf.Cos(angle) * 2.2f);
                    GameObject sp = (GameObject)PrefabUtility.InstantiatePrefab(stalactite1, landmark2.transform);
                    sp.transform.localPosition = sPos;
                    sp.transform.localRotation = Quaternion.Euler(180f, s * 45f, 0f); // Inverted rising from earth
                    sp.transform.localScale = Vector3.one * 0.75f;
                    RemoveColliders(sp);
                }
            }

            // LANDMARK 3: SUNKEN CATACOMB RUINS (East, 22, 0, 0)
            GameObject landmark3 = new GameObject("Landmark_3_SunkenCatacombRuins");
            landmark3.transform.SetParent(landmarksRoot.transform, false);
            landmark3.transform.position = new Vector3(22f, 0f, 0f);

            if (arch2 != null)
            {
                GameObject a = (GameObject)PrefabUtility.InstantiatePrefab(arch2, landmark3.transform);
                a.transform.localPosition = new Vector3(0f, 0f, 0f);
                a.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                a.transform.localScale = Vector3.one * 1.35f;
                RemoveColliders(a);
            }

            if (wallDestroyed1 != null)
            {
                GameObject w1 = (GameObject)PrefabUtility.InstantiatePrefab(wallDestroyed1, landmark3.transform);
                w1.transform.localPosition = new Vector3(2.5f, 0f, -2.5f);
                w1.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
                w1.transform.localScale = Vector3.one * 1.2f;
                RemoveColliders(w1);
            }

            if (wallDestroyed2 != null)
            {
                GameObject w2 = (GameObject)PrefabUtility.InstantiatePrefab(wallDestroyed2, landmark3.transform);
                w2.transform.localPosition = new Vector3(2.5f, 0f, 2.5f);
                w2.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
                w2.transform.localScale = Vector3.one * 1.2f;
                RemoveColliders(w2);
            }

            if (pillarBase4 != null)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pillarBase4, landmark3.transform);
                p.transform.localPosition = new Vector3(-2.2f, 0.4f, 1.8f);
                p.transform.localRotation = Quaternion.Euler(80f, 25f, 0f); // Toppled pillar
                p.transform.localScale = Vector3.one * 1.1f;
                RemoveColliders(p);
            }

            // LANDMARK 4: UMBRAL SHRINE (South, 0, 0, -22)
            GameObject landmark4 = new GameObject("Landmark_4_UmbralShrine");
            landmark4.transform.SetParent(landmarksRoot.transform, false);
            landmark4.transform.position = new Vector3(0f, 0f, -22f);

            // Megalithic ring of cliff pillars
            if (cliffPillar1 != null)
            {
                for (int p = 0; p < 5; p++)
                {
                    float angle = (p * 72f + 36f) * Mathf.Deg2Rad;
                    Vector3 pPos = new Vector3(Mathf.Sin(angle) * 3.5f, -0.4f, Mathf.Cos(angle) * 3.5f);
                    GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(cliffPillar1, landmark4.transform);
                    cp.transform.localPosition = pPos;
                    cp.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-5f, 5f), p * 72f, 0f);
                    cp.transform.localScale = Vector3.one * 1.25f;
                    RemoveColliders(cp);
                }
            }

            // Flanking grim statues
            if (statue1 != null)
            {
                GameObject st1 = (GameObject)PrefabUtility.InstantiatePrefab(statue1, landmark4.transform);
                st1.transform.localPosition = new Vector3(-2.2f, 0f, -1.0f);
                st1.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                st1.transform.localScale = Vector3.one * 1.15f;
                RemoveColliders(st1);
            }

            if (statue2 != null)
            {
                GameObject st2 = (GameObject)PrefabUtility.InstantiatePrefab(statue2, landmark4.transform);
                st2.transform.localPosition = new Vector3(2.2f, 0f, -1.0f);
                st2.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
                st2.transform.localScale = Vector3.one * 1.15f;
                RemoveColliders(st2);
            }

            // LANDMARK 5: ABYSSAL GATE (West, -22, 0, 0)
            GameObject landmark5 = new GameObject("Landmark_5_AbyssalGate");
            landmark5.transform.SetParent(landmarksRoot.transform, false);
            landmark5.transform.position = new Vector3(-22f, 0f, 0f);

            if (arch1 != null)
            {
                GameObject ga = (GameObject)PrefabUtility.InstantiatePrefab(arch1, landmark5.transform);
                ga.transform.localPosition = Vector3.zero;
                ga.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                ga.transform.localScale = Vector3.one * 1.55f;
                RemoveColliders(ga);
            }

            if (stalactite2 != null)
            {
                GameObject spL = (GameObject)PrefabUtility.InstantiatePrefab(stalactite2, landmark5.transform);
                spL.transform.localPosition = new Vector3(-3.2f, 0f, 0.4f);
                spL.transform.localRotation = Quaternion.Euler(180f, 15f, 0f);
                spL.transform.localScale = Vector3.one * 1.1f;
                RemoveColliders(spL);

                GameObject spR = (GameObject)PrefabUtility.InstantiatePrefab(stalactite2, landmark5.transform);
                spR.transform.localPosition = new Vector3(3.2f, 0f, 0.4f);
                spR.transform.localRotation = Quaternion.Euler(180f, -15f, 0f);
                spR.transform.localScale = Vector3.one * 1.1f;
                RemoveColliders(spR);
            }

            // ==========================================
            // 3. PERIMETER CLIFF BARRIER (86m x 86m)
            // ==========================================
            GameObject perimeter = new GameObject("Perimeter_Cliffs_W5");
            perimeter.transform.SetParent(envW5.transform, false);

            float halfSpan = 38.5f;
            int segmentsPerSide = 12;

            for (int i = 0; i <= segmentsPerSide; i++)
            {
                float t = ((float)i / segmentsPerSide) * 2f - 1f; // -1 to +1
                float coord = t * halfSpan;

                // North Wall (Z = +halfSpan)
                SpawnPerimeterCliff(new Vector3(coord, 0f, halfSpan + 1.5f), Quaternion.Euler(0f, 180f, 0f), perimeter.transform, cliff1, cliff2, cliffPillar1, rock2);

                // South Wall (Z = -halfSpan)
                SpawnPerimeterCliff(new Vector3(coord, 0f, -halfSpan - 1.5f), Quaternion.Euler(0f, 0f, 0f), perimeter.transform, cliff1, cliff3, cliffPillar1, rock2);

                // East Wall (X = +halfSpan)
                SpawnPerimeterCliff(new Vector3(halfSpan + 1.5f, 0f, coord), Quaternion.Euler(0f, -90f, 0f), perimeter.transform, cliff2, cliff3, cliffPillar1, rock6);

                // West Wall (X = -halfSpan)
                SpawnPerimeterCliff(new Vector3(-halfSpan - 1.5f, 0f, coord), Quaternion.Euler(0f, 90f, 0f), perimeter.transform, cliff1, cliff2, cliffPillar1, rock8);
            }

            // ==========================================
            // 4. ECLIPSE VOID PARTICLES
            // ==========================================
            GameObject particlesObj = new GameObject("Eclipse_Void_Particles");
            particlesObj.transform.SetParent(envW5.transform, false);
            particlesObj.transform.position = new Vector3(0f, 1.5f, 0f);

            ParticleSystem ps = particlesObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 3.0f;
            main.startSpeed = 0.4f;
            main.startSize = 0.18f;
            main.maxParticles = 40;
            main.startColor = new Color(0.70f, 0.25f, 0.95f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(65f, 2.5f, 65f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            ParticleSystemRenderer psr = particlesObj.GetComponent<ParticleSystemRenderer>();
            Material partMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));
            partMat.color = new Color(0.75f, 0.30f, 1.0f, 0.70f);
            psr.sharedMaterial = partMat;

            // ==========================================
            // 5. WIRE TO CONTROLLERS
            // ==========================================
            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            if (wec != null)
            {
                wec.SetEnvironments(
                    wec.EnvironmentW1,
                    wec.EnvironmentW2,
                    wec.EnvironmentW3,
                    wec.EnvironmentW4,
                    envW5,
                    UnityEngine.Object.FindFirstObjectByType<Light>(),
                    wec.AshParticles,
                    wec.MireParticles,
                    wec.TimberParticles,
                    ps
                );
                EditorUtility.SetDirty(wec);
            }

            SpawnDirector sd = UnityEngine.Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                SerializedObject so = new SerializedObject(sd);
                SerializedProperty pMid = so.FindProperty("eclipseHeraldBossPrefab");
                SerializedProperty pFin = so.FindProperty("primevalHorrorBossPrefab");
                if (pMid != null) pMid.objectReferenceValue = midBossPrefab;
                if (pFin != null) pFin.objectReferenceValue = finalBossPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(sd);
            }

            // Always leave default World 1 active in Editor with zero leakage
            if (wec != null)
            {
                wec.ApplyEnvironment(1);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log("[World5ArenaBuilder] Successfully assembled Environment_W5 in MainGame scene!");
        }

        private static void SpawnPerimeterCliff(Vector3 pos, Quaternion rot, Transform parent, GameObject c1, GameObject c2, GameObject pillar, GameObject rock)
        {
            GameObject chosenCliff = (UnityEngine.Random.value < 0.5f && c1 != null) ? c1 : (c2 != null ? c2 : c1);
            if (chosenCliff != null)
            {
                GameObject cliff = (GameObject)PrefabUtility.InstantiatePrefab(chosenCliff, parent);
                cliff.transform.position = pos + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), UnityEngine.Random.Range(-0.3f, 0.1f), UnityEngine.Random.Range(-0.8f, 0.8f));
                cliff.transform.rotation = rot * Quaternion.Euler(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-15f, 15f), 0f);
                cliff.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.3f, 1.7f);
                RemoveColliders(cliff);
            }

            if (pillar != null && UnityEngine.Random.value < 0.35f)
            {
                GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pillar, parent);
                p.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), -0.5f, UnityEngine.Random.Range(-1.2f, 1.2f));
                p.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(0f, 360f), 0f);
                p.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.2f, 1.5f);
                RemoveColliders(p);
            }

            if (rock != null && UnityEngine.Random.value < 0.40f)
            {
                GameObject r = (GameObject)PrefabUtility.InstantiatePrefab(rock, parent);
                r.transform.position = pos + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 0f, UnityEngine.Random.Range(-1.5f, 1.5f));
                r.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                r.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);
                RemoveColliders(r);
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
