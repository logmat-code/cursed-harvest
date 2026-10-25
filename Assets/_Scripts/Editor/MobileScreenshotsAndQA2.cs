using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class MobileScreenshotsAndQA2
    {
        private static readonly string ArtifactsDir = "/Users/logmat/.gemini/antigravity/brain/da9b4771-589a-4d93-9ba9-83bb63f9ad9b";
        private static readonly string MobileScreenshotsDir = "Assets/_Documentation/Screenshots/MobilePortrait";

        [MenuItem("Cursed Harvest/Mobile/5. Capture Quality & Meta 2.0 Screenshots")]
        public static void CaptureMenu()
        {
            CaptureAll(Debug.Log, Debug.LogError);
        }

        public static void CaptureAll(Action<string> log, Action<string> logErr)
        {
            log("=========================================================================");
            log("[SCREENSHOTS] Capturing High Resolution & Meta Upgrades 2.0 Screenshot Set");
            log("=========================================================================");

            if (!Directory.Exists(MobileScreenshotsDir)) Directory.CreateDirectory(MobileScreenshotsDir);
            if (!Directory.Exists(ArtifactsDir)) Directory.CreateDirectory(ArtifactsDir);

            // 1. FRONTEND: Meta Upgrades 2.0 Screenshots
            EditorSceneManager.OpenScene("Assets/_Scenes/MainMenu.unity", OpenSceneMode.Single);
            MainMenuController mmc = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if (mmc != null)
            {
                mmc.Ensure3DPortraitBackdrop();
                mmc.EnsureMainMenuUI();

                // Setup representative essence and upgrades for Meta screenshots
                MetaProgressionManager meta = MetaProgressionManager.Instance;
                meta.ResetAllProgression();
                meta.AddEssence(1250, false);
                meta.TryUpgradeBranch(MetaUpgradeBranch.Vitality); // Lv1
                meta.TryUpgradeBranch(MetaUpgradeBranch.Vitality); // Lv2
                meta.TryUpgradeBranch(MetaUpgradeBranch.Power);    // Lv1
                meta.TryUpgradeBranch(MetaUpgradeBranch.Survival); // Lv1

                mmc.OpenUpgrades();
                mmc.RefreshUpgradesUI();

                // 1. MetaUpgrades_Main.png (1080x1920)
                CaptureRender("MetaUpgrades_Main", 1080, 1920, log);

                // 2. MetaUpgrades_Selected.png
                CaptureRender("MetaUpgrades_Selected", 1080, 1920, log);

                // 3. MetaUpgrades_Purchase.png (simulate purchase of Power Lv2)
                meta.TryUpgradeBranch(MetaUpgradeBranch.Power);
                mmc.RefreshUpgradesUI();
                CaptureRender("MetaUpgrades_Purchase", 1080, 1920, log);

                // 4. MetaUpgrades_Max.png (consecrate Vitality to Lv5 MAX)
                for (int i = 0; i < 3; i++) meta.TryUpgradeBranch(MetaUpgradeBranch.Vitality);
                mmc.RefreshUpgradesUI();
                CaptureRender("MetaUpgrades_Max", 1080, 1920, log);

                // 5. MetaUpgrades_Stats.png
                CaptureRender("MetaUpgrades_Stats", 1080, 1920, log);

                // Clean state
                meta.ResetAllProgression();
                mmc.ShowScreen(MainMenuScreen.Home);
            }

            // 2. GAMEPLAY: High Resolution Comparisons (1080x1920 vs 1440x3200)
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            TopDownCameraFollow camFollow = UnityEngine.Object.FindFirstObjectByType<TopDownCameraFollow>();
            GameObject player = GameObject.FindWithTag("Player");

            if (uim != null)
            {
                uim.EnsureHUDAndModals();
                uim.HideAllModals();
            }

            // Resolution_High_W1_1080x1920.png
            GameSession.Instance.SetSelectedWorld(1);
            if (wec != null) wec.ApplyEnvironment(1);
            AlignCamera(camFollow, player, 1080, 1920);
            CaptureRender("Resolution_High_W1_1080x1920", 1080, 1920, log);

            // Resolution_High_W5_1080x1920.png
            GameSession.Instance.SetSelectedWorld(5);
            if (wec != null) wec.ApplyEnvironment(5);
            AlignCamera(camFollow, player, 1080, 1920);
            CaptureRender("Resolution_High_W5_1080x1920", 1080, 1920, log);

            // Resolution_High_W5_1440x3200.png (WQHD+ Ultra Crisp Flagship Profile)
            AlignCamera(camFollow, player, 1440, 3200);
            CaptureRender("Resolution_High_W5_1440x3200", 1440, 3200, log);

            // Restore clean state
            GameSession.Instance.SetSelectedWorld(1);
            if (wec != null) wec.ApplyEnvironment(1);
            AlignCamera(camFollow, player, 1080, 1920);

            log("  ✓ All High-Resolution & Meta Upgrades 2.0 screenshots captured cleanly!");
        }

        private static void AlignCamera(TopDownCameraFollow camFollow, GameObject player, int width, int height)
        {
            Camera cam = Camera.main;
            if (camFollow != null && player != null && cam != null)
            {
                RenderTexture tempRt = new RenderTexture(width, height, 24);
                cam.targetTexture = tempRt;
                camFollow.SetTarget(player.transform);
                camFollow.SnapToTargetImmediate();
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(tempRt);
            }
        }

        private static void CaptureRender(string name, int width, int height, Action<string> log)
        {
            Canvas.ForceUpdateCanvases();

            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<(Canvas c, RenderMode mode, Camera wCam, float dist)> savedCanvases = new List<(Canvas, RenderMode, Camera, float)>();

            foreach (var c in canvases)
            {
                if (c != null && c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    savedCanvases.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = Mathf.Max(0.25f, cam.nearClipPlane + 0.12f);
                }
            }

            Canvas.ForceUpdateCanvases();

            // Temporarily disable LineRenderers without runtime materials
            LineRenderer[] allLineRenderers = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<LineRenderer> disabledLines = new List<LineRenderer>();
            foreach (var lr in allLineRenderers)
            {
                if (lr != null && lr.enabled)
                {
                    lr.enabled = false;
                    disabledLines.Add(lr);
                }
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            foreach (var lr in disabledLines)
            {
                if (lr != null) lr.enabled = true;
            }

            foreach (var item in savedCanvases)
            {
                if (item.c != null)
                {
                    item.c.renderMode = item.mode;
                    item.c.worldCamera = item.wCam;
                    item.c.planeDistance = item.dist;
                }
            }

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string mobilePath = Path.Combine(MobileScreenshotsDir, $"{name}.png");
            File.WriteAllBytes(mobilePath, pngBytes);

            if (Directory.Exists(ArtifactsDir))
            {
                string artPath = Path.Combine(ArtifactsDir, $"{name}.png");
                File.WriteAllBytes(artPath, pngBytes);
            }

            log($"  ✓ Captured {name}.png ({width}x{height}, {pngBytes.Length / 1024} KB)");
        }
    }
}
