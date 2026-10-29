using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CursedHarvest
{
    /// <summary>
    /// v1.1.2 DYNAMIC FLOATING JOYSTICK.
    /// This component lives on a large transparent "MoveZone" (left / center-lower gameplay area).
    /// - Hidden by default (no permanent on-screen footprint).
    /// - On touch inside the zone the ring spawns EXACTLY at the touch point; thumb moves relative to it.
    /// - Tracks its own pointerId (multitouch-safe with the fixed Dash button).
    /// - On release the ring fades out fast and movement decays smoothly to zero.
    /// - Never spawns when the touch begins on other UI (EventSystem raycast only reaches this zone if nothing blocks it)
    ///   and is force-reset whenever the touch controls are disabled (Pause, Level Up, Game Over, ads, scene changes).
    /// </summary>
    public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Configuration")]
        [SerializeField] private RectTransform backgroundRect;   // floating ring
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image handleImage;
        [SerializeField] private float handleRange = 96f;
        [SerializeField] private float deadZone = 0.10f;
        [SerializeField] private float ringSize = 250f;
        [SerializeField] private float ringIdleAlpha = 0.62f;
        [SerializeField] private float fadeInSpeed = 14f;
        [SerializeField] private float fadeOutSpeed = 10f;
        [SerializeField] private float releaseDecaySpeed = 18f;

        private const int NoPointer = -999;
        private int activePointerId = NoPointer;
        private Vector2 currentInput = Vector2.zero;
        private Canvas parentCanvas;
        private Camera uiCamera;
        private RectTransform zoneRect;
        private CanvasGroup ringGroup;
        private float targetAlpha;

        private static Sprite _cachedRingSprite;
        private static Sprite _cachedKnobSprite;

        public Vector2 CurrentInput => currentInput;
        public bool IsDragging => activePointerId != NoPointer;
        public bool IsRingVisible => ringGroup != null && ringGroup.alpha > 0.01f;
        public float DeadZone => deadZone;
        public float HandleRange => handleRange;

        public static Sprite GetProceduralCircleSprite(bool isRing)
        {
            if (isRing && _cachedRingSprite != null) return _cachedRingSprite;
            if (!isRing && _cachedKnobSprite != null) return _cachedKnobSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float maxR = center - 2f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float norm = Mathf.Sqrt(dx * dx + dy * dy) / maxR;

                    if (norm > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float edgeAlpha = Mathf.Clamp01((1.0f - norm) * 24f);
                    if (isRing)
                    {
                        // Dark translucent disc + crisp bone-gold outer ring (clear radius, does not hide the scene)
                        if (norm >= 0.93f)
                            pixels[y * size + x] = new Color(0.98f, 0.84f, 0.45f, 0.95f * edgeAlpha);
                        else
                            pixels[y * size + x] = new Color(0.08f, 0.07f, 0.10f, 0.38f * edgeAlpha);
                    }
                    else
                    {
                        bool onRim = norm >= 0.78f;
                        pixels[y * size + x] = onRim
                            ? new Color(0.98f, 0.82f, 0.34f, 0.97f * edgeAlpha)
                            : new Color(0.30f, 0.23f, 0.16f, 0.94f * edgeAlpha);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            if (isRing) _cachedRingSprite = spr;
            else _cachedKnobSprite = spr;
            return spr;
        }

        public void SetupVisuals(RectTransform bg, RectTransform handle, Image bgImg, Image hImg)
        {
            backgroundRect = bg;
            handleRect = handle;
            backgroundImage = bgImg;
            handleImage = hImg;

            if (backgroundImage != null)
            {
                backgroundImage.sprite = GetProceduralCircleSprite(true);
                backgroundImage.color = Color.white;
                backgroundImage.raycastTarget = false;
            }
            if (handleImage != null)
            {
                handleImage.sprite = GetProceduralCircleSprite(false);
                handleImage.color = Color.white;
                handleImage.raycastTarget = false;
            }
        }

        public void EnsureChildVisuals()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            zoneRect = GetComponent<RectTransform>();

            // 1) Convert this object into the big, invisible, raycastable MOVE ZONE (left ~68% width, lower ~58% height).
            //    It never overlaps the Dash button (right) nor the HUD / Pause (top).
            zoneRect.anchorMin = new Vector2(0f, 0f);
            zoneRect.anchorMax = new Vector2(0.68f, 0.58f);
            zoneRect.pivot = new Vector2(0.5f, 0.5f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;

            Image zoneImage = GetComponent<Image>();
            if (zoneImage == null) zoneImage = gameObject.AddComponent<Image>();
            zoneImage.sprite = null;
            zoneImage.color = new Color(0f, 0f, 0f, 0f); // fully transparent but still receives raycasts
            zoneImage.raycastTarget = true;

            // 2) Floating ring (child) - hidden by default
            Transform ringT = transform.Find("FloatingRing");
            if (ringT == null)
            {
                GameObject ringObj = new GameObject("FloatingRing", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                ringObj.transform.SetParent(transform, false);
                ringT = ringObj.transform;
            }
            backgroundRect = ringT.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(ringSize, ringSize);
            backgroundImage = ringT.GetComponent<Image>();
            ringGroup = ringT.GetComponent<CanvasGroup>();
            if (ringGroup == null) ringGroup = ringT.gameObject.AddComponent<CanvasGroup>();
            ringGroup.alpha = 0f;
            ringGroup.blocksRaycasts = false;
            ringGroup.interactable = false;

            // 3) Handle: child of the ring (migrate legacy handle if it exists as a child of the zone)
            Transform h = ringT.Find("JoystickHandle");
            if (h == null) h = transform.Find("JoystickHandle");
            if (h == null)
            {
                GameObject hObj = new GameObject("JoystickHandle", typeof(RectTransform), typeof(Image));
                h = hObj.transform;
            }
            h.SetParent(ringT, false);
            handleRect = h.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(104f, 104f);
            handleRect.anchoredPosition = Vector2.zero;
            handleImage = h.GetComponent<Image>();

            SetupVisuals(backgroundRect, handleRect, backgroundImage, handleImage);
            targetAlpha = 0f;
        }

        private void Awake()
        {
            EnsureChildVisuals();
        }

        private void OnDisable()
        {
            ResetJoystick(true);
        }

        private void Update()
        {
            // Fast fade in / out (unscaled so it also works if timeScale is 0)
            if (ringGroup != null)
            {
                float speed = targetAlpha > ringGroup.alpha ? fadeInSpeed : fadeOutSpeed;
                if (!Mathf.Approximately(ringGroup.alpha, targetAlpha))
                {
                    ringGroup.alpha = Mathf.MoveTowards(ringGroup.alpha, targetAlpha, speed * Time.unscaledDeltaTime);
                }
            }

            // Smooth return of the movement vector to zero after release
            if (activePointerId == NoPointer && currentInput.sqrMagnitude > 0.0001f)
            {
                currentInput = Vector2.Lerp(currentInput, Vector2.zero, Mathf.Clamp01(releaseDecaySpeed * Time.unscaledDeltaTime));
                if (currentInput.sqrMagnitude < 0.003f) currentInput = Vector2.zero;
                if (GameInputManager.Instance != null) GameInputManager.Instance.SetTouchMoveInput(currentInput);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Keep ownership of the first finger; ignore additional fingers in the zone
            if (activePointerId != NoPointer && activePointerId != eventData.pointerId) return;
            if (UIManager.Instance != null && UIManager.Instance.IsModalOpen) return;

            activePointerId = eventData.pointerId;

            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;

            // Spawn the ring EXACTLY where the touch began (clamped so the full ring stays inside the zone)
            if (zoneRect != null && backgroundRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, eventData.position, uiCamera, out Vector2 local))
            {
                float half = ringSize * 0.5f;
                Rect r = zoneRect.rect;
                local.x = Mathf.Clamp(local.x, r.xMin + half, r.xMax - half);
                local.y = Mathf.Clamp(local.y, r.yMin + half, r.yMax - half);
                backgroundRect.anchoredPosition = local;
            }

            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;
            currentInput = Vector2.zero;
            targetAlpha = ringIdleAlpha;
            if (ringGroup != null && ringGroup.alpha < 0.05f) ringGroup.alpha = 0.05f;
            ProcessPointerPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            ProcessPointerPosition(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            ResetJoystick(false);
        }

        private void ProcessPointerPosition(PointerEventData eventData)
        {
            if (backgroundRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect, eventData.position, uiCamera, out Vector2 localPoint))
            {
                ApplyNormalizedInput(localPoint / Mathf.Max(1f, handleRange));
            }
        }

        /// <summary>Raw vector where magnitude 1 = edge of joystick range; applies radial dead zone and feeds GameInputManager.</summary>
        public void ApplyNormalizedInput(Vector2 rawNormalized)
        {
            float magnitude = rawNormalized.magnitude;
            Vector2 direction = magnitude > 0.0001f ? (rawNormalized / magnitude) : Vector2.zero;

            float clampedMag = Mathf.Clamp01(magnitude);
            if (clampedMag < deadZone)
            {
                currentInput = Vector2.zero;
            }
            else
            {
                float remappedMag = (clampedMag - deadZone) / (1f - deadZone);
                currentInput = direction * Mathf.Clamp01(remappedMag);
            }

            if (handleRect != null)
            {
                handleRect.anchoredPosition = direction * (clampedMag * handleRange);
            }

            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.SetTouchMoveInput(currentInput);
            }
        }

        public void SimulateDrag(Vector2 rawNormalized, int pointerId = 1)
        {
            activePointerId = pointerId;
            targetAlpha = ringIdleAlpha;
            if (ringGroup != null) ringGroup.alpha = ringIdleAlpha;
            ApplyNormalizedInput(rawNormalized);
        }

        /// <param name="instant">true = hard reset (modal / disable / scene change): ring hidden and input zero immediately.</param>
        public void ResetJoystick(bool instant = true)
        {
            activePointerId = NoPointer;
            targetAlpha = 0f;
            if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;

            if (instant)
            {
                currentInput = Vector2.zero;
                if (ringGroup != null) ringGroup.alpha = 0f;
                if (GameInputManager.Instance != null) GameInputManager.Instance.SetTouchMoveInput(Vector2.zero);
            }
            // non-instant: Update() fades the ring out and decays currentInput smoothly to zero
        }
    }
}
