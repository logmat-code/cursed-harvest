using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    public static class ScreenshotHelper
    {
        private static string ScreenshotDir => Path.Combine(Application.dataPath, "_Documentation/Screenshots");

        [MenuItem("Cursed Harvest/Capture Screenshot")]
        public static void CaptureScreenshot()
        {
            CaptureCustom("Current_View_16x9", 1920, 1080);
        }

        [MenuItem("Cursed Harvest/Capture LevelUp All Aspect Ratios")]
        public static void CaptureLevelUpAllAspects()
        {
            if (Object.FindAnyObjectByType<UIManager>() == null)
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            UIManager uim = Object.FindAnyObjectByType<UIManager>();
            if (uim == null)
            {
                Debug.LogError("[ScreenshotHelper] UIManager not found in scene!");
                return;
            }

            uim.HideAllModals();

            GameObject lvlModal = uim.transform.Find("LevelUpModal")?.gameObject;
            if (lvlModal == null)
            {
                Debug.LogError("[ScreenshotHelper] LevelUpModal not found!");
                return;
            }

            bool wasActive = lvlModal.activeSelf;
            lvlModal.SetActive(true);

            // Populate sample data on cards
            UpgradeCardUI[] cards = lvlModal.GetComponentsInChildren<UpgradeCardUI>(true);
            if (cards.Length >= 3)
            {
                cards[0].Setup(new UpgradeData(UpgradeType.PumpkinBomb, "pumpkin_bomb", "PUMPKIN BOMB", "Hurls volatile explosive pumpkins that detonate into fiery shrapnel on impact.", "Weapon", 5, true), () => {});
                cards[1].Setup(new UpgradeData(UpgradeType.SecondChance, "second_chance", "LUCKY INTERCEPTION", "Increases lethal damage interception duration by +2.5 seconds and adds +25% berserk speed.", "Utility", 5, true), () => {});
                cards[2].Setup(new UpgradeData(UpgradeType.HarvestScythe, "harvest_scythe", "HARVESTER SCYTHE", "Sweeps a deadly ghostly scythe in a wide arc dealing lethal cleaving damage.", "Weapon", 5, false), () => {});
            }

            // Capture in 16:9 (1920x1080), 16:10 (1680x1050), 4:3 (1024x768)
            CaptureWithCanvas("LevelUp_16x9", 1920, 1080);
            CaptureWithCanvas("LevelUp_16x10", 1680, 1050);
            CaptureWithCanvas("LevelUp_4x3", 1024, 768);

            lvlModal.SetActive(wasActive);
            AssetDatabase.Refresh();
            Debug.Log("[ScreenshotHelper] LevelUp modal captured at 16:9, 16:10, and 4:3!");
        }

        [MenuItem("Cursed Harvest/Capture Player Closeup")]
        public static void CapturePlayerCloseup()
        {
            if (GameObject.FindWithTag("Player") == null)
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[ScreenshotHelper] Player not found!");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            Vector3 origPlayerPos = player.transform.position;

            // Clear lingering damage numbers or test objects
            DamageNumberManager dnm = Object.FindAnyObjectByType<DamageNumberManager>();
            if (dnm != null) dnm.ClearActiveNumbers();

            // Position player cleanly on open ground in front of central altar
            float gy = CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, -3.2f));
            player.transform.position = new Vector3(0f, gy, -3.2f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            PlayerCombatPose pcp = player.GetComponent<PlayerCombatPose>();
            if (pcp != null)
            {
                pcp.FindBones();
                pcp.ApplyCombatPose();
                Debug.Log($"[DEBUG POSE] Player: {player.transform.position}, CrossbowRoot: {pcp.CrossbowRoot?.position}, parent: {pcp.CrossbowRoot?.parent?.name}, VisualBow: {pcp.VisualBow?.position}, parent: {pcp.VisualBow?.parent?.name}, rot: {pcp.VisualBow?.eulerAngles}, RightGrip: {pcp.RightHandGrip?.position}, LeftGrip: {pcp.LeftHandGrip?.position}");
            }

            // Hide UI canvas during character closeup
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            bool canvasWasActive = canvas != null && canvas.gameObject.activeSelf;
            if (canvas != null) canvas.gameObject.SetActive(false);

            // 1. Elevated 3/4 front view clearly showing right hand trigger grip, left hand under stock, and compact elbows
            cam.transform.position = player.transform.position + new Vector3(0.65f, 1.35f, 1.25f);
            cam.transform.LookAt(player.transform.position + new Vector3(0.08f, 1.04f, 0.22f));
            CaptureCustom("Player_Crossbow_Closeup", 1920, 1080);

            // 2. Right-side profile view showing trigger grip and stock resting naturally
            cam.transform.position = player.transform.position + new Vector3(1.65f, 1.15f, 0.28f);
            cam.transform.LookAt(player.transform.position + new Vector3(0.08f, 1.04f, 0.24f));
            CaptureCustom("Player_Crossbow_Side", 1920, 1080);

            // 3. Over-the-shoulder aim view showing forward weapon alignment
            cam.transform.position = player.transform.position + new Vector3(0.38f, 1.48f, -0.90f);
            cam.transform.LookAt(player.transform.position + new Vector3(0.11f, 1.06f, 0.70f));
            CaptureCustom("Player_Crossbow_Aim", 1920, 1080);

            // 4. Ingame Gameplay Camera View (Top-down survivor perspective)
            cam.transform.position = player.transform.position + new Vector3(0f, 13.5f, -8.5f);
            cam.transform.LookAt(player.transform.position + new Vector3(0f, 0.6f, 0.8f));
            CaptureCustom("Player_Crossbow_Gameplay", 1920, 1080);

            if (canvas != null) canvas.gameObject.SetActive(canvasWasActive);

            player.transform.position = origPlayerPos;
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            AssetDatabase.Refresh();
            Debug.Log("[ScreenshotHelper] Player closeup and gameplay angles captured!");
        }

        public static string CaptureCustom(string name, int width = 1920, int height = 1080)
        {
            return CaptureWithCanvas(name, width, height);
        }

        public static string CaptureWithCanvas(string name, int width, int height)
        {
            if (!Directory.Exists(ScreenshotDir))
            {
                Directory.CreateDirectory(ScreenshotDir);
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return null;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            RenderMode origMode = RenderMode.ScreenSpaceOverlay;
            Camera origCam = null;
            float origPlaneDist = 100f;
            if (canvas != null)
            {
                origMode = canvas.renderMode;
                origCam = canvas.worldCamera;
                origPlaneDist = canvas.planeDistance;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                // Place UI directly in front of camera lens so 3D scenery never occludes it
                canvas.planeDistance = Mathf.Max(0.2f, cam.nearClipPlane + 0.1f);
            }

            LineRenderer[] lines = Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<LineRenderer> disabledLines = new List<LineRenderer>();
            foreach (var l in lines)
            {
                if (l != null && l.enabled)
                {
                    l.enabled = false;
                    disabledLines.Add(l);
                }
            }

            TrailRenderer[] trails = Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<TrailRenderer> disabledTrails = new List<TrailRenderer>();
            foreach (var t in trails)
            {
                if (t != null && t.enabled)
                {
                    t.enabled = false;
                    disabledTrails.Add(t);
                }
            }

            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            try
            {
                cam.Render();
            }
            finally
            {
                foreach (var l in disabledLines) if (l != null) l.enabled = true;
                foreach (var t in disabledTrails) if (t != null) t.enabled = true;
            }

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            if (canvas != null)
            {
                canvas.renderMode = origMode;
                canvas.worldCamera = origCam;
                canvas.planeDistance = origPlaneDist;
            }

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string path = Path.Combine(ScreenshotDir, $"{name}.png");
            File.WriteAllBytes(path, bytes);
            return path;
        }

        [MenuItem("Cursed Harvest/Capture Center Dressing and Enemy Arms Pass")]
        public static void CaptureCenterDressingAndEnemyArmsPass()
        {
            if (GameObject.FindWithTag("Player") == null)
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            bool canvasWasActive = canvas != null && canvas.gameObject.activeSelf;
            if (canvas != null) canvas.gameObject.SetActive(false);

            // Wipe any lingering damage numbers or test objects from scene before captures
            DamageNumberManager dnm = Object.FindAnyObjectByType<DamageNumberManager>();
            if (dnm != null) dnm.ClearActiveNumbers();
            TextMesh[] allTm = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach (var tm in allTm)
            {
                if (tm != null && tm.name.Contains("DamageNum")) Object.DestroyImmediate(tm.gameObject);
            }

            EnemyController[] existingEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var e in existingEnemies)
            {
                if (e != null) Object.DestroyImmediate(e.gameObject);
            }

            GameObject player = GameObject.FindWithTag("Player");
            Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;

            // 1. Center Arena Overview (Top-down gameplay perspective showing player advancing toward central altar and inner ring micro-clusters)
            float groundAtPlayer = CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, -3.2f));
            if (player != null)
            {
                player.transform.position = new Vector3(0f, groundAtPlayer, -3.2f);
                PlayerCombatPose pcp = player.GetComponent<PlayerCombatPose>();
                if (pcp != null) pcp.ApplyCombatPose();
            }
            cam.transform.position = new Vector3(0f, 15.0f, -12.5f);
            cam.transform.LookAt(new Vector3(0f, 0f, 0.8f));
            CaptureCustom("CenterArena_Gameplay_Overview", 1920, 1080);

            // Temporarily hide player during environment altar and enemy anatomy closeups
            if (player != null) player.SetActive(false);

            // 2. Central Landmark Closeup (Sunken stone ritual slab, flat stones, pickaxe, leaves, ground mist)
            float gyCenter = CoreSetup.SampleGroundHeight(Vector3.zero);
            Vector3 altarCenter = new Vector3(0f, gyCenter, 0f);
            cam.transform.position = altarCenter + new Vector3(1.6f, 1.15f, -1.85f);
            cam.transform.LookAt(altarCenter + new Vector3(0f, 0.22f, 0f));
            CaptureCustom("CenterLandmark_Altar_Closeup", 1920, 1080);

            // 3. Harvest Plot Ritual Landmark Closeup (Front-facing SW view: tilled dark soil, roots, glowing amber beacon, weathered statue, stones)
            HarvestPlot targetPlot = null;
            HarvestPlot[] allPlots = Object.FindObjectsByType<HarvestPlot>(FindObjectsSortMode.None);
            foreach (var p in allPlots)
            {
                if (p.transform.position.x > 0 && p.transform.position.z > 0)
                {
                    targetPlot = p;
                    break;
                }
            }
            if (targetPlot != null)
            {
                Vector3 pPos = targetPlot.transform.position;
                cam.transform.position = pPos + new Vector3(-2.2f, 1.8f, -2.4f);
                cam.transform.LookAt(pPos + new Vector3(0f, 0.40f, 0.15f));
                CaptureCustom("HarvestPlot_Ritual_Closeup", 1920, 1080);
            }

            // 4. Enemy Archetypes Closeups (Verifying zero broken arms, steady weapons, natural downward rest-pose bias)
            string[] enemyPrefabPaths = new string[]
            {
                "Assets/_Prefabs/Enemy_PumpkinGrunt.prefab",
                "Assets/_Prefabs/Enemy_MaskedChaser.prefab",
                "Assets/_Prefabs/Enemy_PlagueWalker.prefab",
                "Assets/_Prefabs/Enemy_DemonCharger.prefab",
                "Assets/_Prefabs/Enemy_CursedFarmer.prefab",
                "Assets/_Prefabs/Enemy_HarvestBrute.prefab"
            };
            string[] enemyNames = new string[]
            {
                "Enemy_Grunt_Arms_Closeup",
                "Enemy_Chaser_Arms_Closeup",
                "Enemy_Walker_Arms_Closeup",
                "Enemy_Charger_WeaponArm_Closeup",
                "Enemy_Farmer_WeaponArm_Closeup",
                "Enemy_Brute_WeaponArm_Closeup"
            };

            for (int i = 0; i < enemyPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPaths[i]);
                if (prefab != null)
                {
                    GameObject tempEnemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    float surfY = CoreSetup.SampleGroundHeight(Vector3.zero);
                    tempEnemy.transform.position = new Vector3(0f, surfY, 0f);
                    tempEnemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face camera

                    EnemyLocomotion el = tempEnemy.GetComponent<EnemyLocomotion>();
                    if (el != null)
                    {
                        el.FindBones();
                        el.ApplyLocomotionPose(0f, 0f); // Apply natural rest pose
                    }

                    // Elevated 3/4 front view focusing on arms and upper body
                    cam.transform.position = new Vector3(0.55f, surfY + 1.25f, -1.85f);
                    cam.transform.LookAt(new Vector3(0f, surfY + 0.95f, 0f));
                    CaptureCustom(enemyNames[i], 1920, 1080);

                    Object.DestroyImmediate(tempEnemy);
                }
            }

            if (player != null)
            {
                player.SetActive(true);
                player.transform.position = origPlayerPos;
            }
            if (canvas != null) canvas.gameObject.SetActive(canvasWasActive);

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            AssetDatabase.Refresh();
            Debug.Log("[ScreenshotHelper] Center dressing, Harvest Plot, and Enemy Arm closeups captured successfully!");
        }

        [MenuItem("Cursed Harvest/Capture Health & Mask Visuals")]
        public static void CaptureHealthAndMaskVisuals()
        {
            if (GameObject.FindWithTag("Player") == null)
            {
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            bool canvasWasActive = canvas != null && canvas.gameObject.activeSelf;
            if (canvas != null) canvas.gameObject.SetActive(false);

            DamageNumberManager dnm = Object.FindAnyObjectByType<DamageNumberManager>();
            if (dnm != null) dnm.ClearActiveNumbers();

            EnemyController[] existingEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            foreach (var e in existingEnemies)
            {
                if (e != null) Object.DestroyImmediate(e.gameObject);
            }

            GameObject player = GameObject.FindWithTag("Player");
            Vector3 origPlayerPos = player != null ? player.transform.position : Vector3.zero;
            float gy = CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, -3.0f));

            if (player != null)
            {
                player.SetActive(true);
                player.transform.position = new Vector3(0f, gy, -3.0f);
                player.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                PlayerCombatPose pcp = player.GetComponent<PlayerCombatPose>();
                if (pcp != null) pcp.ApplyCombatPose();
            }

            // 1. Health Pickup in Arena (Closeup with potion flask, emerald light, and player nearby)
            GameObject healthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/HealthRestorePickup.prefab");
            GameObject pickupObj = null;
            if (healthPrefab != null)
            {
                pickupObj = (GameObject)PrefabUtility.InstantiatePrefab(healthPrefab);
                pickupObj.transform.position = new Vector3(0.85f, gy + 0.25f, -2.6f);
            }

            cam.transform.position = new Vector3(0.45f, gy + 1.25f, -4.6f);
            cam.transform.LookAt(new Vector3(0.45f, gy + 0.70f, -2.8f));
            CaptureCustom("HealthPickup_InArena", 1920, 1080);

            // 2. Active Healing Feedback (Player green healing flash + floating +20 HP number)
            GameObject healTextObj = new GameObject("HealFloatingText");
            healTextObj.transform.position = new Vector3(0f, gy + 2.2f, -3.0f);
            TextMesh tm = healTextObj.AddComponent<TextMesh>();
            tm.text = "+20 HP";
            tm.fontSize = 28;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.35f, 0.95f, 0.45f, 1f); // Vibrant emerald green
            healTextObj.transform.rotation = cam.transform.rotation;

            // Soft emerald body flash
            if (player != null)
            {
                Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>();
                MaterialPropertyBlock pb = new MaterialPropertyBlock();
                pb.SetColor("_BaseColor", new Color(0.35f, 0.95f, 0.45f, 1f));
                foreach (var r in playerRenderers) if (r != null) r.SetPropertyBlock(pb);
            }

            cam.transform.position = new Vector3(0f, gy + 1.35f, -4.9f);
            cam.transform.LookAt(new Vector3(0f, gy + 1.25f, -3.0f));
            CaptureCustom("Healing_Feedback_Active", 1920, 1080);

            if (healTextObj != null) Object.DestroyImmediate(healTextObj);
            if (pickupObj != null) Object.DestroyImmediate(pickupObj);

            // Reset player renderers
            if (player != null)
            {
                Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>();
                foreach (var r in playerRenderers) if (r != null) r.SetPropertyBlock(null);
                player.SetActive(false);
            }

            // 3. Mask Variants Lineup (5 Masked Chasers side-by-side: Pumpkin, Plague, Demonic, LED, Witch)
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_MaskedChaser.prefab");
            List<GameObject> chasers = new List<GameObject>();
            if (chaserPrefab != null)
            {
                float[] xPositions = new float[] { -2.4f, -1.2f, 0.0f, 1.2f, 2.4f };
                for (int m = 0; m < 5; m++)
                {
                    GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(chaserPrefab);
                    float cGy = CoreSetup.SampleGroundHeight(new Vector3(xPositions[m], 0f, -3.2f));
                    c.transform.position = new Vector3(xPositions[m], cGy, -3.2f);
                    c.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face camera

                    EnemyLocomotion el = c.GetComponent<EnemyLocomotion>();
                    if (el != null)
                    {
                        el.FindBones();
                        el.ApplyLocomotionPose(0f, 0f);
                    }

                    EnemyMaskVariant emv = c.GetComponent<EnemyMaskVariant>();
                    if (emv != null)
                    {
                        emv.ApplyRandomMask(m);
                    }
                    chasers.Add(c);
                }
            }

            cam.transform.position = new Vector3(0f, 1.55f, -6.5f);
            cam.transform.LookAt(new Vector3(0f, 1.35f, -3.2f));
            CaptureCustom("Mask_Variants_Lineup", 1920, 1080);

            foreach (var c in chasers) if (c != null) Object.DestroyImmediate(c);
            chasers.Clear();

            // 4. Masked Farmer (with hat) and Masked Brute (Demonic)
            GameObject farmerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_CursedFarmer.prefab");
            GameObject brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_HarvestBrute.prefab");

            GameObject farmerInst = null;
            if (farmerPrefab != null)
            {
                farmerInst = (GameObject)PrefabUtility.InstantiatePrefab(farmerPrefab);
                float fGy = CoreSetup.SampleGroundHeight(new Vector3(-0.95f, 0f, -3.2f));
                farmerInst.transform.position = new Vector3(-0.95f, fGy, -3.2f);
                farmerInst.transform.rotation = Quaternion.Euler(0f, 175f, 0f);

                EnemyLocomotion el = farmerInst.GetComponent<EnemyLocomotion>();
                if (el != null) { el.FindBones(); el.ApplyLocomotionPose(0f, 0f); }

                EnemyMaskVariant emv = farmerInst.GetComponent<EnemyMaskVariant>();
                if (emv != null) emv.ApplyRandomMask(0); // Pumpkin mask under hat
            }

            GameObject bruteInst = null;
            if (brutePrefab != null)
            {
                bruteInst = (GameObject)PrefabUtility.InstantiatePrefab(brutePrefab);
                float bGy = CoreSetup.SampleGroundHeight(new Vector3(0.95f, 0f, -3.2f));
                bruteInst.transform.position = new Vector3(0.95f, bGy, -3.2f);
                bruteInst.transform.rotation = Quaternion.Euler(0f, 185f, 0f);

                EnemyLocomotion el = bruteInst.GetComponent<EnemyLocomotion>();
                if (el != null) { el.FindBones(); el.ApplyLocomotionPose(0f, 0f); }

                EnemyMaskVariant emv = bruteInst.GetComponent<EnemyMaskVariant>();
                if (emv != null) emv.ApplyRandomMask(0); // Demonic mask
            }

            cam.transform.position = new Vector3(0f, 2.30f, -7.6f);
            cam.transform.LookAt(new Vector3(0f, 1.80f, -3.2f));
            CaptureCustom("Masked_Farmer_And_Brute", 1920, 1080);

            if (farmerInst != null) Object.DestroyImmediate(farmerInst);
            if (bruteInst != null) Object.DestroyImmediate(bruteInst);

            // Restore player & camera
            if (player != null)
            {
                player.SetActive(true);
                player.transform.position = origPlayerPos;
            }
            if (canvas != null) canvas.gameObject.SetActive(canvasWasActive);

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            AssetDatabase.Refresh();
            Debug.Log("[ScreenshotHelper] Health and Mask visual captures completed successfully!");
        }
    }
}
