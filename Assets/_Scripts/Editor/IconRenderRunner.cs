using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class IconRenderRunner
    {
        private const string BrandingDir = "Assets/_Documentation/Branding";
        private const string ArtifactsDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";
        private const string IconScenePath = "Assets/_Scenes/IconRenderScene.unity";

        // Real Synty Asset Paths
        private const string PumpkinLantern01 = "Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_01.prefab";
        private const string PumpkinLantern02 = "Assets/Synty/PolygonPumpkins/Prefabs/SM_Prop_Pumpkin_Lanturn_02.prefab";
        private const string PickaxePrefab = "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Pickaxe_01.prefab";
        private const string AxePrefab = "Assets/Synty/PolygonGeneric/Prefabs/Weapons/SM_Gen_Wep_Axe_01.prefab";
        private const string DemonicMaskPrefab = "Assets/Synty/PolygonHalloweenMasks/Prefabs/SM_Prop_Demonic_Mask_01.prefab";
        private const string CrossbowPrefab = "Assets/Synty/PolygonBowCrossbow/Prefabs/SM_Wep_Rigged_CrossBow_01.prefab";
        private const string BoltPrefab = "Assets/Synty/PolygonBowCrossbow/Prefabs/SM_Wep_Crossbow_Bolt_01.prefab";
        private const string DeadTree01Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Tree_Dead_01.fbx";
        private const string DeadTree02Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Tree_Dead_02.fbx";
        private const string Rock04Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Rock_04.fbx";
        private const string Rock10Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Rock_10.fbx";
        private const string Leaves01Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Env_Leaves_01.fbx";
        private const string Skull01Fbx = "Assets/Synty/PolygonGeneric/Models/SM_Gen_Prop_Skull_01.fbx";
        private const string GroundMatPath = "Assets/_Materials/M_ContinuousBaseGround.mat";

        [MenuItem("Cursed Harvest/Branding/Render All Icons & Cover")]
        public static void RenderAllIconsAndCover()
        {
            List<string> logs = new List<string>();
            void Log(string msg) { Debug.Log(msg); logs.Add(msg); }
            void LogErr(string msg) { Debug.LogError(msg); logs.Add("[ERROR] " + msg); }

            Log("=================================================================");
            Log("[BRANDING] Starting Official Cursed Harvest Icon & Cover Rendering...");
            Log("=================================================================");

            if (!Directory.Exists(BrandingDir)) Directory.CreateDirectory(BrandingDir);
            if (!Directory.Exists(ArtifactsDir)) Directory.CreateDirectory(ArtifactsDir);

            // Create or open dedicated IconRenderScene
            Scene iconScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            iconScene.name = "IconRenderScene";

            try
            {
                // 1. Setup Environment Root
                GameObject root = new GameObject("IconRenderRoot");

                // 2. Setup Three-Point Studio Lighting for Cinematic Low-Poly Emblems
                // KEY LIGHT (Rich warm harvest amber/gold from front-right)
                GameObject keyLightObj = new GameObject("Icon_KeyLight");
                keyLightObj.transform.SetParent(root.transform, false);
                Light keyLight = keyLightObj.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(1.0f, 0.74f, 0.38f);
                keyLight.intensity = 2.4f;
                keyLightObj.transform.rotation = Quaternion.Euler(24f, -35f, 0f);

                // FILL LIGHT (Deep atmospheric midnight indigo from lateral left)
                GameObject fillLightObj = new GameObject("Icon_FillLight");
                fillLightObj.transform.SetParent(root.transform, false);
                Light fillLight = fillLightObj.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.20f, 0.32f, 0.58f);
                fillLight.intensity = 0.85f;
                fillLightObj.transform.rotation = Quaternion.Euler(12f, 135f, 0f);

                // RIM LIGHT (Crisp silvery/cyan moonlight outlining silhouette against dark background)
                GameObject rimLightObj = new GameObject("Icon_RimLight");
                rimLightObj.transform.SetParent(root.transform, false);
                Light rimLight = rimLightObj.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0.68f, 0.88f, 1.0f);
                rimLight.intensity = 2.2f;
                rimLightObj.transform.rotation = Quaternion.Euler(-20f, 155f, 0f);

                // 3. Setup Dedicated Icon Camera (Centered Macro Portrait)
                GameObject camObj = new GameObject("IconCamera");
                camObj.transform.SetParent(root.transform, false);
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                // Rich obsidian folk-horror dark background for maximum icon contrast
                cam.backgroundColor = new Color(0.045f, 0.038f, 0.052f, 1.0f);
                cam.fieldOfView = 32f; // Macro portrait to eliminate edge distortion
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.transform.position = new Vector3(0f, 0f, -2.4f);
                cam.transform.LookAt(Vector3.zero);

                // Container for option-specific props
                GameObject propsContainer = new GameObject("OptionPropsContainer");
                propsContainer.transform.SetParent(root.transform, false);

                // =================================================================
                // OPTION A: CURSED HARVEST CLASSIC (Pumpkin Lantern + Harvesting Mattock Pickaxe)
                // =================================================================
                Log("\n[OPTION A] Building & Rendering 'Cursed Harvest Classic' (Option A)...");
                ClearChildren(propsContainer);
                BuildOptionA(propsContainer);
                cam.transform.position = new Vector3(0f, 0f, -2.4f);
                cam.transform.LookAt(Vector3.zero);
                RenderAndExportOption(cam, "CursedHarvest_Icon_A", Log);

                // =================================================================
                // OPTION B: RITUAL HORROR (Demonic Mask + Bloodwood Executioner Axe)
                // =================================================================
                Log("\n[OPTION B] Building & Rendering 'Ritual Horror' (Option B)...");
                ClearChildren(propsContainer);
                BuildOptionB(propsContainer);
                cam.transform.position = new Vector3(0f, 0f, -2.4f);
                cam.transform.LookAt(Vector3.zero);
                RenderAndExportOption(cam, "CursedHarvest_Icon_B", Log);

                // =================================================================
                // OPTION C: HUNTER / SURVIVOR (Crossbow Hero Weapon + Cursed Pumpkin)
                // =================================================================
                Log("\n[OPTION C] Building & Rendering 'Hunter / Survivor' (Option C)...");
                ClearChildren(propsContainer);
                BuildOptionC(propsContainer);
                cam.transform.position = new Vector3(0f, 0f, -2.4f);
                cam.transform.LookAt(Vector3.zero);
                RenderAndExportOption(cam, "CursedHarvest_Icon_C", Log);

                // =================================================================
                // RECOMMENDATION & FINAL EXPORT (OPTION A: CURSED HARVEST CLASSIC)
                // =================================================================
                Log("\n--- EXPORTING RECOMMENDED FINAL MASTER (OPTION A) ---");
                ExportFinalMaster("CursedHarvest_Icon_A", Log);

                // =================================================================
                // RENDER COVER 630x500 (1.26:1 ASPECT RATIO)
                // =================================================================
                Log("\n--- RENDERING OFFICIAL COVER (630x500) ---");
                ClearChildren(propsContainer);
                BuildOptionA(propsContainer); // Hero Option A setup
                cam.transform.position = new Vector3(0f, 0f, -2.85f);
                cam.transform.LookAt(Vector3.zero);
                RenderCover630x500(cam, Log);

                // Save Icon Scene
                EditorSceneManager.SaveScene(iconScene, IconScenePath);
                Log($"[BRANDING] Dedicated icon render scene saved to {IconScenePath}");

                // =================================================================
                // APPLY TO PROJECT / PLAYER SETTINGS
                // =================================================================
                ApplyBrandingToProject(Log);

                Log("\n=================================================================");
                Log("[BRANDING] Official Game Icon & WebGL Branding Pass COMPLETE!");
                Log("=================================================================");
            }
            catch (Exception ex)
            {
                LogErr($"Exception during icon rendering: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // ==================== OPTION BUILDERS ====================

        private static void BuildOptionA(GameObject container)
        {
            // 1. Primary Focal: SM_Prop_Pumpkin_Lanturn_01 (Cursed Jack-o'-Lantern)
            GameObject pumpkinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PumpkinLantern01);
            if (pumpkinPrefab != null)
            {
                GameObject p = UnityEngine.Object.Instantiate(pumpkinPrefab, container.transform);
                p.name = "Focal_PumpkinLantern_01";
                // Centered emblem, filling ~72-75% of canvas with breathing room for weapon
                // Y=274 deg faces the sinister carved face in menacing 3/4 angle towards key light
                p.transform.localPosition = new Vector3(-0.06f, -0.05f, 0f);
                p.transform.localRotation = Quaternion.Euler(6f, 274f, -4f);
                p.transform.localScale = Vector3.one * 1.72f;

                // Intense molten orange interior flame glowing through carved eyes and jagged mouth
                GameObject lightObj = new GameObject("Pumpkin_InteriorLight");
                lightObj.transform.SetParent(p.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.20f, 0f);
                Light pl = lightObj.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1.0f, 0.45f, 0.05f);
                pl.intensity = 5.5f;
                pl.range = 3.0f;
            }

            // 2. Complementary Farming Weapon: SM_Gen_Wep_Pickaxe_01 (Harvest Mattock)
            GameObject pickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickaxePrefab);
            if (pickPrefab != null)
            {
                GameObject pick = UnityEngine.Object.Instantiate(pickPrefab, container.transform);
                pick.name = "Complementary_HarvestPickaxe";
                // Angled crossing behind pumpkin so iron pick head crowns the right shoulder
                pick.transform.localPosition = new Vector3(0.08f, -0.15f, 0.25f);
                pick.transform.localRotation = Quaternion.Euler(-12f, 160f, 38f);
                pick.transform.localScale = Vector3.one * 1.75f;
            }
        }

        private static void BuildOptionB(GameObject container)
        {
            // 1. Primary Focal: SM_Prop_Demonic_Mask_01 (Horned Demonic Ritual Mask)
            GameObject maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DemonicMaskPrefab);
            if (maskPrefab != null)
            {
                GameObject m = UnityEngine.Object.Instantiate(maskPrefab, container.transform);
                m.name = "Focal_DemonicMask_01";
                m.transform.localPosition = new Vector3(-0.04f, 0.02f, 0f);
                m.transform.localRotation = Quaternion.Euler(6f, 18f, -4f);
                m.transform.localScale = Vector3.one * 3.10f;

                // Blood-crimson underlight casting sinister upward ritual glow
                GameObject lightObj = new GameObject("Mask_CrimsonLight");
                lightObj.transform.SetParent(m.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, -0.20f, -0.2f);
                Light ml = lightObj.AddComponent<Light>();
                ml.type = LightType.Point;
                ml.color = new Color(1.0f, 0.15f, 0.05f);
                ml.intensity = 4.8f;
                ml.range = 2.5f;
            }

            // 2. Complementary Executioner Weapon: SM_Gen_Wep_Axe_01 (Bloodwood Axe)
            GameObject axePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AxePrefab);
            if (axePrefab != null)
            {
                GameObject axe = UnityEngine.Object.Instantiate(axePrefab, container.transform);
                axe.name = "Complementary_RitualAxe";
                // Lowered so forged axe head crowns the right horn of the mask
                axe.transform.localPosition = new Vector3(0.32f, -0.85f, 0.15f);
                axe.transform.localRotation = Quaternion.Euler(8f, -25f, -12f);
                axe.transform.localScale = Vector3.one * 2.05f;
            }

            // 3. Altar Skull: SM_Gen_Prop_Skull_01 (Ritual trophy at bottom left)
            string skullPrefabPath = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Skull_01.prefab";
            GameObject skullPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(skullPrefabPath);
            if (skullPrefab != null)
            {
                GameObject skull = UnityEngine.Object.Instantiate(skullPrefab, container.transform);
                skull.name = "Altar_Skull";
                skull.transform.localPosition = new Vector3(-0.32f, -0.32f, -0.08f);
                skull.transform.localRotation = Quaternion.Euler(-18f, 38f, 12f);
                skull.transform.localScale = Vector3.one * 1.25f;
            }
        }

        private static void BuildOptionC(GameObject container)
        {
            // 1. Primary Focal: SM_Wep_Rigged_CrossBow_01 (Hero Survivor Crossbow)
            GameObject crossbowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrossbowPrefab);
            if (crossbowPrefab != null)
            {
                GameObject cb = UnityEngine.Object.Instantiate(crossbowPrefab, container.transform);
                cb.name = "Focal_HeroCrossbow";
                // Rotated to point forward-left towards camera in dramatic heroic action angle
                cb.transform.localPosition = new Vector3(0.06f, -0.16f, 0f);
                cb.transform.localRotation = Quaternion.Euler(-16f, 136f, 22f);
                cb.transform.localScale = Vector3.one * 1.85f;
            }

            // 2. Iron-tipped Crossbow Bolt loaded in flight groove
            GameObject boltPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefab);
            if (boltPrefab != null)
            {
                GameObject bolt = UnityEngine.Object.Instantiate(boltPrefab, container.transform);
                bolt.name = "Crossbow_Bolt";
                bolt.transform.localPosition = new Vector3(0.06f, -0.14f, -0.06f);
                bolt.transform.localRotation = Quaternion.Euler(-16f, 136f, 22f);
                bolt.transform.localScale = Vector3.one * 1.70f;
            }

            // 3. Complementary Cursed Pumpkin Lantern nestled in background
            GameObject pumpkinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PumpkinLantern01);
            if (pumpkinPrefab != null)
            {
                GameObject p = UnityEngine.Object.Instantiate(pumpkinPrefab, container.transform);
                p.name = "Complementary_PumpkinLantern";
                p.transform.localPosition = new Vector3(-0.48f, 0.02f, 0.42f);
                p.transform.localRotation = Quaternion.Euler(6f, 265f, -4f);
                p.transform.localScale = Vector3.one * 1.30f;

                // Interior flame glow
                GameObject lightObj = new GameObject("Pumpkin_InteriorLight");
                lightObj.transform.SetParent(p.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.15f, 0.02f);
                Light pl = lightObj.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1.0f, 0.48f, 0.08f);
                pl.intensity = 4.5f;
                pl.range = 2.5f;
            }
        }

        // ==================== RENDERING UTILITIES ====================

        private static void RenderAndExportOption(Camera cam, string baseName, Action<string> log)
        {
            // Shield against Line/Trail renderer issues
            ShieldRenderers(() =>
            {
                // 1. Render 1024x1024 Master
                RenderTexture rt1024 = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt1024;
                cam.Render();

                RenderTexture.active = rt1024;
                Texture2D masterTex = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
                masterTex.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                masterTex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;

                byte[] masterPng = masterTex.EncodeToPNG();
                string masterDocPath = Path.Combine(BrandingDir, $"{baseName}_1024.png");
                string masterArtPath = Path.Combine(ArtifactsDir, $"{baseName}_1024.png");
                File.WriteAllBytes(masterDocPath, masterPng);
                File.WriteAllBytes(masterArtPath, masterPng);
                log($"  ✓ Rendered 1024 Master: {baseName}_1024.png ({masterPng.Length / 1024} KB)");

                // 2. Downscale tests (512, 256, 128, 64) using Bilinear Blit
                int[] sizes = new int[] { 512, 256, 128, 64 };
                foreach (int s in sizes)
                {
                    RenderTexture rtDown = RenderTexture.GetTemporary(s, s, 0, RenderTextureFormat.ARGB32);
                    rtDown.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(rt1024, rtDown);

                    RenderTexture.active = rtDown;
                    Texture2D texDown = new Texture2D(s, s, TextureFormat.RGB24, false);
                    texDown.ReadPixels(new Rect(0, 0, s, s), 0, 0);
                    texDown.Apply();

                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rtDown);

                    byte[] downPng = texDown.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(texDown);

                    string downDocPath = Path.Combine(BrandingDir, $"{baseName}_{s}.png");
                    string downArtPath = Path.Combine(ArtifactsDir, $"{baseName}_{s}.png");
                    File.WriteAllBytes(downDocPath, downPng);
                    File.WriteAllBytes(downArtPath, downPng);
                    log($"  ✓ Downscaled preview: {baseName}_{s}.png ({s}x{s}, {downPng.Length / 1024} KB)");
                }

                UnityEngine.Object.DestroyImmediate(rt1024);
                UnityEngine.Object.DestroyImmediate(masterTex);
            });
        }

        private static void ExportFinalMaster(string sourceBaseName, Action<string> log)
        {
            int[] sizes = new int[] { 1024, 512, 256, 128 };
            foreach (int s in sizes)
            {
                string src = Path.Combine(BrandingDir, $"{sourceBaseName}_{s}.png");
                string dst = Path.Combine(BrandingDir, $"Icon_Final_{s}.png");
                string artDst = Path.Combine(ArtifactsDir, $"Icon_Final_{s}.png");

                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                    File.Copy(src, artDst, true);
                    log($"  ★ Final Export: Icon_Final_{s}.png ({s}x{s})");
                }
            }
        }

        private static void RenderCover630x500(Camera cam, Action<string> log)
        {
            ShieldRenderers(() =>
            {
                int width = 630;
                int height = 500;
                RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string docPath = Path.Combine(BrandingDir, "CursedHarvest_Cover_630x500.png");
                string artPath = Path.Combine(ArtifactsDir, "CursedHarvest_Cover_630x500.png");
                File.WriteAllBytes(docPath, bytes);
                File.WriteAllBytes(artPath, bytes);

                log($"  ✓ Rendered Itch/Jam Cover: CursedHarvest_Cover_630x500.png ({width}x{height}, {bytes.Length / 1024} KB)");
            });
        }

        [MenuItem("Cursed Harvest/Branding/Apply Recommended Icon to PlayerSettings")]
        public static void ApplyBrandingToProject(Action<string> log = null)
        {
            log?.Invoke("\n[BRANDING] Applying recommended icon and branding to Unity PlayerSettings...");

            // 1. Set Product Name
            PlayerSettings.productName = "Cursed Harvest";
            log?.Invoke($"  ✓ PlayerSettings.productName set to: '{PlayerSettings.productName}'");

            // 2. Import Icon_Final_1024 as Texture2D asset
            string iconAssetPath = $"{BrandingDir}/Icon_Final_1024.png";
            AssetDatabase.ImportAsset(iconAssetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconAssetPath);
            if (iconTex != null)
            {
                // Assign to Unknown (default icon for all platforms including WebGL)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { iconTex });
                // Assign specifically to WebGL
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.WebGL, new Texture2D[] { iconTex });
                log?.Invoke($"  ✓ PlayerSettings default & WebGL icons assigned with {iconAssetPath}!");
            }
            else
            {
                log?.Invoke($"  [WARNING] Could not load Texture2D at {iconAssetPath}!");
            }

            AssetDatabase.SaveAssets();
        }

        private static void ClearChildren(GameObject parent)
        {
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }
        }

        private static void ShieldRenderers(Action renderAction)
        {
            var lines = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var prevLineState = new Dictionary<LineRenderer, bool>();
            foreach (var l in lines)
            {
                if (l != null)
                {
                    prevLineState[l] = l.enabled;
                    l.enabled = false;
                }
            }

            var trails = UnityEngine.Object.FindObjectsByType<TrailRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var prevTrailState = new Dictionary<TrailRenderer, bool>();
            foreach (var t in trails)
            {
                if (t != null)
                {
                    prevTrailState[t] = t.enabled;
                    t.enabled = false;
                }
            }

            try
            {
                renderAction?.Invoke();
            }
            finally
            {
                foreach (var kvp in prevLineState) if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
                foreach (var kvp in prevTrailState) if (kvp.Key != null) kvp.Key.enabled = kvp.Value;
            }
        }
    }
}
