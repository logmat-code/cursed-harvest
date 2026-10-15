using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class CoreSetup
    {
        private const string PrefabsPath = "Assets/_Prefabs";
        private const string ScenesPath = "Assets/_Scenes";

        [MenuItem("Cursed Harvest/1. Build Phase 1 Core")]
        public static void BuildPhase1()
        {
            EnsureDirectories();
            GameObject boltPrefab = CreateBoltPrefab();
            GameObject playerPrefab = CreatePlayerPrefab(boltPrefab);
            GameObject gruntPrefab = CreatePumpkinGruntPrefab();
            CreateMainScene(playerPrefab, boltPrefab, gruntPrefab);
            Debug.Log("[CursedHarvest] Phase 1 Core Build Complete!");
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
            if (boltSource == null)
            {
                Debug.LogError("Could not find SM_Wep_Crossbow_Bolt_01.prefab");
                return null;
            }

            GameObject instance = new GameObject("CrossbowBolt");
            instance.tag = "Projectile";
            int projLayer = LayerMask.NameToLayer("Projectile");
            if (projLayer >= 0) instance.layer = projLayer;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(boltSource, instance.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

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
            Object.DestroyImmediate(instance);
            Debug.Log($"Created bolt prefab: {targetPath}");
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

                // Search for Hand_R
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

            CrossbowWeapon crossbow = root.AddComponent<CrossbowWeapon>();
            if (firePoint != null) crossbow.SetFirePoint(firePoint);
            wm.AddWeapon(crossbow);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Created player prefab: {targetPath}");
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
            Object.DestroyImmediate(root);
            Debug.Log($"Created Pumpkin Grunt prefab: {targetPath}");
            return saved;
        }

        private static void CreateMainScene(GameObject playerPrefab, GameObject boltPrefab, GameObject gruntPrefab)
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

            // 2. Directional Light (Spooky moonlight)
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

            // Build 4x4 ground grid
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

            // Ground BoxCollider for safe physics
            BoxCollider groundCollider = arenaRoot.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(90f, 1f, 90f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);

            // Boundary invisible colliders so player/enemies can't fall off
            CreateBoundaryWall(arenaRoot.transform, new Vector3(0, 5, 42), new Vector3(90, 10, 2));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(0, 5, -42), new Vector3(90, 10, 2));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(42, 5, 0), new Vector3(2, 10, 90));
            CreateBoundaryWall(arenaRoot.transform, new Vector3(-42, 5, 0), new Vector3(2, 10, 90));

            // Dead trees and spooky pumpkins scattered
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
                    tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    tree.transform.localScale = Vector3.one * Random.Range(0.8f, 1.2f);
                }

                if (pumpkinPrefab != null)
                {
                    GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(pumpkinPrefab, arenaRoot.transform);
                    p.transform.position = pos + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                    p.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    // Add warm light to lantern
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

            // 4. Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            TopDownCameraFollow camFollow = camObj.AddComponent<TopDownCameraFollow>();

            // 5. Pools
            GameObject poolsRoot = new GameObject("Pools");
            ObjectPool boltPool = CreatePool(poolsRoot.transform, "BoltPool", boltPrefab, 30);
            ObjectPool enemyPool = CreatePool(poolsRoot.transform, "EnemyPool", gruntPrefab, 20);

            // 6. Player Instance
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, 0.1f, 0f);

            // Connect bolt pool to player's crossbow
            CrossbowWeapon crossbow = playerInstance.GetComponent<CrossbowWeapon>();
            if (crossbow != null && boltPool != null)
            {
                crossbow.SetBoltPool(boltPool);
            }

            camFollow.SetTarget(playerInstance.transform);

            // 7. GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();

            SerializedObject soGm = new SerializedObject(gm);
            soGm.FindProperty("player").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            soGm.FindProperty("boltPool").objectReferenceValue = boltPool;
            soGm.FindProperty("enemyPool").objectReferenceValue = enemyPool;
            soGm.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            soGm.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to Build Settings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            Debug.Log($"MainGame scene saved to: {scenePath}");
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
