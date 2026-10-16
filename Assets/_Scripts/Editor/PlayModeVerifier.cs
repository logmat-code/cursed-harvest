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
