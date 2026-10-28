using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

namespace CursedHarvest.Editor
{
    public static class PerformanceProfilingRunner
    {
        public struct ProfileSample
        {
            public string Scenario;
            public float CpuFrameTimeMs;
            public float Fps;
            public long TotalAllocatedMemoryMB;
            public long TotalReservedMemoryMB;
            public long GcAllocPerFrameBytes;
            public int Batches;
            public int SetPassCalls;
            public int Triangles;
            public int Vertices;
            public int ActiveEnemies;
        }

        [MenuItem("Cursed Harvest/Mobile/Run Performance Profiler Benchmark")]
        public static void RunBenchmark()
        {
            List<string> reportLines = new List<string>();
            void Log(string msg) { Debug.Log(msg); reportLines.Add(msg); }

            Log("=========================================================================");
            Log("[PERFORMANCE PROFILER] Starting Real Profile Benchmark Pass...");
            Log("=========================================================================");

            List<ProfileSample> samples = new List<ProfileSample>();

            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);

            WorldEnvironmentController wec = UnityEngine.Object.FindFirstObjectByType<WorldEnvironmentController>();
            UIManager uim = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            TopDownCameraFollow camFollow = UnityEngine.Object.FindFirstObjectByType<TopDownCameraFollow>();
            GameObject player = GameObject.FindWithTag("Player");
            Camera cam = Camera.main;

            if (uim != null)
            {
                uim.EnsureHUDAndModals();
                uim.HideAllModals();
            }

            GameObject gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PumpkinGrunt.prefab");
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_ScarecrowChaser.prefab");
            GameObject walkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PlagueWalker.prefab");
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_PrimevalHorror.prefab");

            // Helper to sample scenario
            ProfileSample MeasureScenario(string scenarioName, int worldId, int enemyCount, bool spawnBoss)
            {
                GameSession.Instance.SetSelectedWorld(worldId);
                if (wec != null) wec.ApplyEnvironment(worldId);

                // Spawn enemies
                List<GameObject> spawned = new List<GameObject>();
                GameObject[] prefabs = new GameObject[] { gruntPrefab, chaserPrefab, walkerPrefab };

                for (int i = 0; i < enemyCount; i++)
                {
                    GameObject pf = prefabs[i % prefabs.Length];
                    if (pf == null) continue;
                    float angle = (i * 360f / Mathf.Max(1, enemyCount)) * Mathf.Deg2Rad;
                    float dist = UnityEngine.Random.Range(4f, 16f);
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                    pos.y = CoreSetup.SampleGroundHeight(pos);
                    GameObject e = UnityEngine.Object.Instantiate(pf, pos, Quaternion.identity);
                    spawned.Add(e);
                }

                GameObject bossObj = null;
                if (spawnBoss && bossPrefab != null)
                {
                    Vector3 bPos = new Vector3(0f, CoreSetup.SampleGroundHeight(new Vector3(0f, 0f, 7f)), 7f);
                    bossObj = UnityEngine.Object.Instantiate(bossPrefab, bPos, Quaternion.Euler(0f, 180f, 0f));
                    if (uim != null) uim.ShowBossBar("THE PRIMEVAL HORROR", 4800f, 5200f);
                }

                // Render offscreen texture to simulate 1080x1920 draw calls
                RenderTexture rt = new RenderTexture(1080, 1920, 24);
                if (cam != null) cam.targetTexture = rt;

                // Warmup 5 frames
                for (int w = 0; w < 5; w++)
                {
                    if (cam != null) cam.Render();
                }

                // Measure 30 iterations
                GC.Collect();
                long gcBefore = GC.GetTotalMemory(true);
                float totalMs = 0f;
                int iterations = 30;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                for (int it = 0; it < iterations; it++)
                {
                    if (cam != null) cam.Render();
                }
                sw.Stop();
                long gcAfter = GC.GetTotalMemory(false);
                totalMs = (float)sw.Elapsed.TotalMilliseconds / iterations;

                ProfileSample s = new ProfileSample
                {
                    Scenario = scenarioName,
                    CpuFrameTimeMs = totalMs,
                    Fps = totalMs > 0 ? (1000f / totalMs) : 60f,
                    TotalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024),
                    TotalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024),
                    GcAllocPerFrameBytes = Math.Max(0, (gcAfter - gcBefore) / iterations),
                    Batches = UnityEditor.UnityStats.drawCalls,
                    SetPassCalls = UnityEditor.UnityStats.setPassCalls,
                    Triangles = UnityEditor.UnityStats.triangles,
                    Vertices = UnityEditor.UnityStats.vertices,
                    ActiveEnemies = enemyCount + (spawnBoss ? 1 : 0)
                };

                // Cleanup
                if (cam != null) cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                if (bossObj != null) UnityEngine.Object.DestroyImmediate(bossObj);
                if (uim != null) uim.HideBossBar();

                for (int i = 0; i < spawned.Count; i++)
                {
                    if (spawned[i] != null) UnityEngine.Object.DestroyImmediate(spawned[i]);
                }
                spawned.Clear();

                return s;
            }

            // Stress Test 1: World 1 Arena 5 (The Consecrated Clearing - 25 enemies)
            ProfileSample s1 = MeasureScenario("W1-A5 The Consecrated Clearing (25 enemies)", 1, 25, false);
            samples.Add(s1);

            // Stress Test 2: World 2 Arena 5 (The Ossuary Throne - 35 enemies)
            ProfileSample s2 = MeasureScenario("W2-A5 The Ossuary Throne (35 enemies)", 2, 35, false);
            samples.Add(s2);

            // Stress Test 3: World 3 Arena 5 (Mid-Boss Wave 10 - Eclipse Herald + 20 minions)
            ProfileSample s3 = MeasureScenario("W3-A5 Mid-Boss Wave 10 (Herald + 20 minions)", 3, 20, true);
            samples.Add(s3);

            // Stress Test 4: World 3 Arena 5 (Waves 11-14 Horde - 60 heavy mixed enemies)
            ProfileSample s4 = MeasureScenario("W3-A5 Waves 11-14 Horde (60 enemies + void motes)", 3, 60, false);
            samples.Add(s4);

            // Stress Test 5: World 3 Arena 5 (Wave 15 Final Boss - Primeval Horror + 25 minions)
            ProfileSample s5 = MeasureScenario("W3-A5 Wave 15 Final Boss (Primeval Horror + 25 minions)", 3, 25, true);
            samples.Add(s5);

            Log("\n=========================================================================");
            Log("PROFILING RESULTS SUMMARY:");
            Log("=========================================================================");
            foreach (var s in samples)
            {
                Log($"Scenario: {s.Scenario}");
                Log($"  - CPU Frame Time: {s.CpuFrameTimeMs:F2} ms (~{s.Fps:F1} FPS potential)");
                Log($"  - GC Alloc / Frame: {s.GcAllocPerFrameBytes} B");
                Log($"  - Batches: {s.Batches} | SetPass Calls: {s.SetPassCalls}");
                Log($"  - Triangles: {s.Triangles:N0} | Vertices: {s.Vertices:N0}");
                Log($"  - Active Enemies: {s.ActiveEnemies} | Mem Allocated: {s.TotalAllocatedMemoryMB} MB / Reserved: {s.TotalReservedMemoryMB} MB");
                Log("-------------------------------------------------------------------------");
            }

            string outFile = Path.Combine(AndroidMobilePortraitPipeline.ArtifactsDir, "performance_benchmark_report.txt");
            File.WriteAllLines(outFile, reportLines);
            Log($"[PERFORMANCE PROFILER] Benchmark report saved to {outFile}");
        }
    }
}
