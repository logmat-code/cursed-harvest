using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CursedHarvest
{
    /// <summary>
    /// 360-degree Fixed Virtual Joystick for bottom-left mobile portrait control.
    /// Tracks its dedicated pointerId so simultaneous right-thumb Dash/UI touches never interrupt movement.
    /// </summary>
    public class VirtualJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Configuration")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image handleImage;
        [SerializeField] private float handleRange = 82f;
        [SerializeField] private float deadZone = 0.12f;

        private const int NoPointer = -999;
        private int activePointerId = NoPointer;
        private Vector2 currentInput = Vector2.zero;
        private Canvas parentCanvas;
        private Camera uiCamera;

        private static Sprite _cachedRingSprite;
        private static Sprite _cachedKnobSprite;

        public Vector2 CurrentInput => currentInput;
        public bool IsDragging => activePointerId != NoPointer;
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
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float norm = dist / maxR;

                    if (norm > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float edgeAlpha = Mathf.Clamp01((1.0f - norm) * 24f);
                        if (isRing)
                        {
                            // Subtle dark interior + crisp outer rune ring
                            bool onBorder = norm >= 0.88f && norm <= 0.98f;
                            bool onInnerRing = norm >= 0.28f && norm <= 0.31f;
                            if (onBorder)
                                pixels[y * size + x] = new Color(0.88f, 0.72f, 0.34f, 0.85f * edgeAlpha);
                            else if (onInnerRing)
                                pixels[y * size + x] = new Color(0.75f, 0.60f, 0.28f, 0.25f);
                            else
                                pixels[y * size + x] = new Color(0.10f, 0.09f, 0.13f, 0.52f * edgeAlpha);
                        }
                        else
                        {
                            // Tactile thumb knob with bright antique gold rim and warm dark center
                            bool onRim = norm >= 0.78f;
                            if (onRim)
                                pixels[y * size + x] = new Color(0.98f, 0.82f, 0.34f, 0.95f * edgeAlpha);
                            else
                                pixels[y * size + x] = new Color(0.28f, 0.22f, 0.16f, 0.92f * edgeAlpha);
                        }
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
            }
            if (handleImage != null)
            {
                handleImage.sprite = GetProceduralCircleSprite(false);
                handleImage.color = Color.white;
            }
        }

        public void EnsureChildVisuals()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (backgroundRect == null) backgroundRect = GetComponent<RectTransform>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            if (backgroundImage == null) backgroundImage = gameObject.AddComponent<Image>();

            if (handleRect == null)
            {
                Transform h = transform.Find("JoystickHandle");
                if (h == null)
                {
                    GameObject hObj = new GameObject("JoystickHandle", typeof(RectTransform), typeof(Image));
                    hObj.transform.SetParent(transform, false);
                    h = hObj.transform;
                }
                handleRect = h.GetComponent<RectTransform>();
                handleRect.anchorMin = new Vector2(0.5f, 0.5f);
                handleRect.anchorMax = new Vector2(0.5f, 0.5f);
                handleRect.pivot = new Vector2(0.5f, 0.5f);
                handleRect.sizeDelta = new Vector2(96f, 96f);
                handleRect.anchoredPosition = Vector2.zero;
                handleImage = h.GetComponent<Image>();
            }

            SetupVisuals(backgroundRect, handleRect, backgroundImage, handleImage);
        }

        private void Awake()
        {
            EnsureChildVisuals();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer && activePointerId != eventData.pointerId)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            if (handleImage != null)
            {
                handleImage.color = new Color(1.1f, 1.05f, 0.9f, 1f);
            }
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
            ResetJoystick();
        }

        private void ProcessPointerPosition(PointerEventData eventData)
        {
            if (backgroundRect == null) return;

            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? parentCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect, eventData.position, uiCamera, out Vector2 localPoint))
            {
                Vector2 rawNormalized = localPoint / Mathf.Max(1f, handleRange);
                ApplyNormalizedInput(rawNormalized);
            }
        }

        /// <summary>
        /// Processes a raw 2D vector (where magnitude 1.0 = edge of joystick range),
        /// applies radial deadZone, updates knob position, and feeds GameInputManager.
        /// </summary>
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
                // Remap [deadZone..1] smoothly to [0..1] for responsive 360-degree control
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
            ApplyNormalizedInput(rawNormalized);
        }

        public void ResetJoystick()
        {
            activePointerId = NoPointer;
            currentInput = Vector2.zero;
            if (handleRect != null)
            {
                handleRect.anchoredPosition = Vector2.zero;
            }
            if (handleImage != null)
            {
                handleImage.color = Color.white;
            }
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.SetTouchMoveInput(Vector2.zero);
            }
        }
    }
}
