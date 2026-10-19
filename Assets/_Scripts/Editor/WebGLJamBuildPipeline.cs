using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CursedHarvest.Editor
{
    public static class WebGLJamBuildPipeline
    {
        public const string BuildOutputDirectory = "Builds/WebGL_Jam_Final";
        public const string ZipOutputPath = "Builds/CursedHarvest_Jam_WebGL.zip";

        private static readonly string[] BuildScenes = new string[]
        {
            "Assets/_Scenes/MainMenu.unity",
            "Assets/_Scenes/MainGame.unity"
        };

        [MenuItem("Cursed Harvest/Build/Configure WebGL Settings")]
        public static void ConfigureWebGLSettings()
        {
            Debug.Log("[BUILD-PIPELINE] Configuring Production WebGL Settings...");

            // 1. Branding & Product Info
            PlayerSettings.productName = "Cursed Harvest";
            PlayerSettings.companyName = "DefaultCompany";

            // 2. WebGL Template & Display
            PlayerSettings.WebGL.template = "PROJECT:CursedHarvest";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;

            // 3. Release & Debugging Flags (Strictly Production)
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildScriptsOnly = false;

            // 4. Compression & Hosting Compatibility
            // Gzip with Decompression Fallback ensures 100% compatibility with Itch.io and local HTTP servers
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            // 5. Memory Configuration (Unity 6 WebGL)
            PlayerSettings.WebGL.initialMemorySize = 32;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;

            // 6. Ensure Master Icon is Assigned
            string iconPath = "Assets/_Documentation/Branding/Icon_Final_1024.png";
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (iconTex != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { iconTex });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.WebGL, new Texture2D[] { iconTex });
                Debug.Log($"[BUILD-PIPELINE] Master icon confirmed from {iconPath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BUILD-PIPELINE] WebGL Settings successfully configured and saved!");
        }

        [MenuItem("Cursed Harvest/Build/Build WebGL Jam Final")]
        public static void BuildWebGLJamFinal()
        {
            ConfigureWebGLSettings();

            Debug.Log("\n=======================================================");
            Debug.Log("[BUILD-PIPELINE] Starting Production WebGL Build Process...");
            Debug.Log("=======================================================");

            // Ensure destination directory is clean
            if (Directory.Exists(BuildOutputDirectory))
            {
                Debug.Log($"[BUILD-PIPELINE] Cleaning existing build directory: {BuildOutputDirectory}");
                Directory.Delete(BuildOutputDirectory, true);
            }
            Directory.CreateDirectory(BuildOutputDirectory);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = BuildOutputDirectory,
                target = BuildTarget.WebGL,
                options = BuildOptions.None // No development / no profiling
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("\n=======================================================");
                Debug.Log($"[BUILD-PIPELINE] SUCCESS! WebGL Build completed in {summary.totalTime.TotalSeconds:F1} seconds.");
                Debug.Log($"[BUILD-PIPELINE] Output Directory: {BuildOutputDirectory}");
                Debug.Log($"[BUILD-PIPELINE] Total Uncompressed Output Size: {summary.totalSize / (1024 * 1024):F2} MB");
                Debug.Log("=======================================================\n");

                // Package into deployable Itch.io ZIP
                CreateJamZip();
            }
            else
            {
                Debug.LogError($"\n[BUILD-PIPELINE] FAILED! WebGL Build finished with result: {summary.result}");
                Debug.LogError($"[BUILD-PIPELINE] Total Errors: {summary.totalErrors}");
            }
        }

        [MenuItem("Cursed Harvest/Build/Package Jam ZIP")]
        public static void CreateJamZip()
        {
            Debug.Log($"[BUILD-PIPELINE] Packaging Itch.io deployable ZIP: {ZipOutputPath}...");

            if (!Directory.Exists(BuildOutputDirectory))
            {
                Debug.LogError($"[BUILD-PIPELINE] Cannot package ZIP: Build directory does not exist: {BuildOutputDirectory}");
                return;
            }

            string zipDir = Path.GetDirectoryName(ZipOutputPath);
            if (!string.IsNullOrEmpty(zipDir) && !Directory.Exists(zipDir))
            {
                Directory.CreateDirectory(zipDir);
            }

            if (File.Exists(ZipOutputPath))
            {
                File.Delete(ZipOutputPath);
            }

            // Zip the contents of Builds/WebGL_Jam_Final directly at the zip root
            // (index.html, Build/, TemplateData/ at the root of the archive)
            ZipFile.CreateFromDirectory(BuildOutputDirectory, ZipOutputPath, System.IO.Compression.CompressionLevel.Optimal, false);

            FileInfo fi = new FileInfo(ZipOutputPath);
            Debug.Log($"[BUILD-PIPELINE] Jam ZIP created successfully: {ZipOutputPath} ({fi.Length / (1024 * 1024):F2} MB, {fi.Length} bytes)");
        }
    }
}
