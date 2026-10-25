using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace CursedHarvest.Editor
{
    public static class AndroidReleaseRunner
    {
        [MenuItem("Cursed Harvest/Mobile/6. Build Signed Release AAB")]
        public static void BuildSignedReleaseAabMenu()
        {
            BuildSignedReleaseAab(Debug.Log, Debug.LogError);
        }

        public static void BuildSignedReleaseAab(Action<string> log, Action<string> logErr)
        {
            log("=========================================================================");
            log("[RELEASE] Starting CURSED HARVEST. Signed Android Release AAB Build");
            log("=========================================================================");

            AndroidMobilePortraitPipeline.ConfigureAndroidPortraitSettings(log, logErr);

            string outputDir = AndroidMobilePortraitPipeline.AndroidBuildOutputDir;
            string outputPath = AndroidMobilePortraitPipeline.AndroidAabOutputPath;

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            // Verify Release Signing is active
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                logErr("[RELEASE FAILED] Custom Keystore signing is NOT configured!");
                return;
            }

            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;

            log($"  • Target AAB Output: {outputPath}");
            log($"  • Using Custom Keystore: {PlayerSettings.Android.keystoreName}");
            log($"  • Key Alias: {PlayerSettings.Android.keyaliasName}");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new string[]
                {
                    "Assets/_Scenes/MainMenu.unity",
                    "Assets/_Scenes/MainGame.unity"
                },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded && File.Exists(outputPath))
            {
                FileInfo fi = new FileInfo(outputPath);
                log("=========================================================================");
                log($"  ✓ [SIGNED AAB SUCCEEDED] Path: {outputPath}");
                log($"  ✓ Package:      {PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)}");
                log($"  ✓ Product:      {PlayerSettings.productName}");
                log($"  ✓ Company:      {PlayerSettings.companyName}");
                log($"  ✓ Version:      {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})");
                log($"  ✓ Bundle Size:  {fi.Length / (1024f * 1024f):F2} MB ({fi.Length:N0} bytes)");
                log($"  ✓ Build Time:   {summary.totalTime.TotalSeconds:F1} seconds");
                log("=========================================================================");
            }
            else
            {
                logErr($"[SIGNED AAB FAILED] Result={summary.result}, TotalErrors={summary.totalErrors}");
            }
        }
    }
}
