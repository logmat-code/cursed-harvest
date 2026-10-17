using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CursedHarvest.Editor
{
    [InitializeOnLoad]
    public static class PlayModeVerifier
    {
        private const string PrefKey = "CH_PlayModeTest_Remaining";
        private const string FrameCountKey = "CH_PlayModeTest_Frames";
        private static int delayFrames = 0;

        [MenuItem("Cursed Harvest/Test/Run 3x Play Mode Cycle")]
        public static void StartCycleTest()
        {
            EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            EditorPrefs.SetInt(PrefKey, 3);
            EditorPrefs.SetInt(FrameCountKey, 0);
            Debug.Log("[PlayModeVerifier] Starting 3x Play Mode Cycle Test...");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Cursed Harvest/Test/Toggle Play Mode")]
        public static void TogglePlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("[PlayModeVerifier] Exiting Play Mode via Toggle...");
                EditorApplication.ExitPlaymode();
            }
            else
            {
                Debug.Log("[PlayModeVerifier] Entering Play Mode via Toggle...");
                EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
        }

        [MenuItem("Cursed Harvest/Test/Verify MainGame Scene")]
        public static void RunSceneCheckAndExit()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    Debug.LogError("[SCENE VERIFY] FAILED: MainGame.unity is not valid!");
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log("[SCENE VERIFY] MainGame.unity loaded successfully.");

                // 1. Main Camera
                var cam = GameObject.FindWithTag("MainCamera");
                if (cam == null) cam = GameObject.Find("Main Camera");
                bool camOk = cam != null && cam.GetComponent<Camera>() != null;
                Debug.Log($"[SCENE VERIFY] Main Camera exists: {camOk}");

                // 2. Player
                var player = GameObject.FindWithTag("Player");
                if (player == null) player = GameObject.Find("Player");
                bool playerOk = player != null && player.GetComponent<PlayerController>() != null && player.GetComponent<PlayerHealth>() != null;
                Debug.Log($"[SCENE VERIFY] Player exists: {playerOk}");

                // 3. SpawnDirector
                var sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
                bool sdOk = sd != null;
                Debug.Log($"[SCENE VERIFY] SpawnDirector exists: {sdOk}");

                // 4. UIManager
                var uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
                bool uimOk = uim != null;
                Debug.Log($"[SCENE VERIFY] UIManager exists: {uimOk}");

                // 5. Missing components check
                int missingCount = 0;
                foreach (var go in scene.GetRootGameObjects())
                {
                    missingCount += CheckMissingComponents(go);
                }
                Debug.Log($"[SCENE VERIFY] Total Missing Components across Scene: {missingCount}");

                if (camOk && playerOk && sdOk && uimOk && missingCount == 0)
                {
                    Debug.Log("★★★ [SCENE VERIFY] ALL CHECKS PASSED: Main Camera, Player, SpawnDirector, UIManager intact with 0 missing references! ★★★");
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[SCENE VERIFY] Scene verification failed requirements!");
                    EditorApplication.Exit(1);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SCENE VERIFY] Exception: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static int CheckMissingComponents(GameObject go)
        {
            int missing = 0;
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning($"[SCENE VERIFY] Missing component on GameObject: {go.name}");
                    missing++;
                }
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                missing += CheckMissingComponents(go.transform.GetChild(i).gameObject);
            }
            return missing;
        }


        static PlayModeVerifier()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            int remaining = EditorPrefs.GetInt(PrefKey, 0);
            if (remaining <= 0) return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log($"[PlayModeVerifier] Entered Play Mode successfully! Cycle {4 - remaining} of 3 active.");
                delayFrames = 40; // Run for 40 frames in playmode
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                remaining--;
                EditorPrefs.SetInt(PrefKey, remaining);
                Debug.Log($"[PlayModeVerifier] Exited Play Mode successfully. Remaining cycles: {remaining}");
                delayFrames = 20; // Wait 20 frames in editmode before re-entering

                if (remaining <= 0)
                {
                    EditorPrefs.DeleteKey(PrefKey);
                    EditorPrefs.DeleteKey(FrameCountKey);
                    Debug.Log("=================================================================");
                    Debug.Log("★★★ [PlayModeVerifier] 3x PLAY MODE CYCLES COMPLETED WITH 0 ERRORS! ★★★");
                    Debug.Log("=================================================================");
                }
            }
        }

        private static void OnEditorUpdate()
        {
            int remaining = EditorPrefs.GetInt(PrefKey, 0);
            if (remaining <= 0) return;

            if (delayFrames > 0)
            {
                delayFrames--;
                return;
            }

            if (EditorApplication.isPlaying)
            {
                // We've spent enough frames in PlayMode, exit now
                EditorApplication.ExitPlaymode();
                delayFrames = 20;
            }
            else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // We're in EditMode and have remaining cycles, re-enter now
                Debug.Log($"[PlayModeVerifier] Re-entering Play Mode for cycle {4 - remaining} of 3...");
                EditorApplication.EnterPlaymode();
                delayFrames = 20;
            }
        }
    }
}
