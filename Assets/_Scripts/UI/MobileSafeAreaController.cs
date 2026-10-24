using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Dynamically adapts a full-screen UI container RectTransform to Screen.safeArea
    /// so notches, camera cutouts, rounded corners, status bars, and Android gesture navigation bars
    /// never clip or overlap interactive UI or HUD widgets.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class MobileSafeAreaController : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        [Header("Editor Simulation (For QA Testing Only)")]
        [SerializeField] private bool simulateCutoutWhenFullSafeAreaInEditor = false;
        [SerializeField] private float simulatedTopInsetRatio = 0.038f;
        [SerializeField] private float simulatedBottomInsetRatio = 0.024f;

        public Rect LastAppliedSafeArea => lastSafeArea;
        public Vector2 NormalizedAnchorMin => rectTransform != null ? rectTransform.anchorMin : Vector2.zero;
        public Vector2 NormalizedAnchorMax => rectTransform != null ? rectTransform.anchorMax : Vector2.one;

        public void SetEditorCutoutSimulation(bool enabled, float topRatio = 0.038f, float bottomRatio = 0.024f)
        {
            simulateCutoutWhenFullSafeAreaInEditor = enabled;
            simulatedTopInsetRatio = Mathf.Clamp(topRatio, 0f, 0.12f);
            simulatedBottomInsetRatio = Mathf.Clamp(bottomRatio, 0f, 0.08f);
            ApplySafeArea(true);
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea(true);
        }

        private void OnEnable()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            ApplySafeArea(true);
        }

        private void Update()
        {
            ApplySafeArea(false);
        }

        public void ApplySafeArea(bool force = false)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;

            int screenW = Screen.width;
            int screenH = Screen.height;
            if (screenW <= 0 || screenH <= 0) return;

            Rect safeArea = Screen.safeArea;

#if UNITY_EDITOR
            if (simulateCutoutWhenFullSafeAreaInEditor &&
                Mathf.Approximately(safeArea.x, 0f) &&
                Mathf.Approximately(safeArea.y, 0f) &&
                Mathf.Approximately(safeArea.width, screenW) &&
                Mathf.Approximately(safeArea.height, screenH) &&
                screenH > screenW)
            {
                float bottomPx = Mathf.Round(screenH * simulatedBottomInsetRatio);
                float topPx = Mathf.Round(screenH * simulatedTopInsetRatio);
                safeArea = new Rect(0f, bottomPx, screenW, screenH - topPx - bottomPx);
            }
#endif

            if (!force &&
                safeArea == lastSafeArea &&
                lastScreenSize.x == screenW &&
                lastScreenSize.y == screenH &&
                lastOrientation == Screen.orientation)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(screenW, screenH);
            lastOrientation = Screen.orientation;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x = Mathf.Clamp01(anchorMin.x / screenW);
            anchorMin.y = Mathf.Clamp01(anchorMin.y / screenH);
            anchorMax.x = Mathf.Clamp01(anchorMax.x / screenW);
            anchorMax.y = Mathf.Clamp01(anchorMax.y / screenH);

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
