using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Authoritative Top-Down Camera Controller supporting both Landscape (WebGL Jam)
    /// and dedicated Mobile Portrait framing (Android / Google Play).
    /// In Portrait mode:
    /// - Adjusts camera elevation, pitch, FOV / Orthographic Size for ultra-tall mobile aspect ratios (16:9 to 20:9+)
    /// - Frames the Player at ~47.5% of screen height (within the 45-55% target range)
    /// - Applies smooth velocity-based omnidirectional look-ahead
    /// - Clamps camera position to prevent exposing outer world edges in Portrait
    /// </summary>
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Layout Mode")]
        [SerializeField] private UILayoutMode cameraLayoutMode = UILayoutMode.Portrait;

        [Header("Target & Landscape Profile")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 landscapeOffset = new Vector3(0f, 15.0f, -10.5f);
        [SerializeField] private float landscapePitch = 54.5f;
        [SerializeField] private float landscapeFov = 60f;
        [SerializeField] private float landscapeOrthoSize = 9.0f;
        [SerializeField] private float smoothTime = 0.14f;

        [Header("Portrait Camera Profile (Mobile Authoritative)")]
        [SerializeField] private Vector3 portraitOffset = new Vector3(0f, 18.4f, -10.9f);
        [SerializeField] private float portraitPitch = 57.5f;
        [SerializeField] private float portraitBaseFov = 64f;
        [SerializeField] private float portraitBaseOrthoSize = 11.8f;
        [SerializeField] private float portraitForwardFramingBias = 0.25f; // Places player at ~49% screen height across all Portrait aspects
        [SerializeField] private float portraitLookAheadDistance = 1.15f;
        [SerializeField] private float lookAheadSmoothSpeed = 5.5f;

        [Header("Arena Framing Clamps")]
        [SerializeField] private Vector2 landscapeClampXZ = new Vector2(21f, 21f);
        [SerializeField] private Vector2 portraitClampXZ = new Vector2(17.5f, 15.0f);

        private Camera cachedCamera;
        private PlayerController cachedPlayerController;
        private Vector3 currentLookAhead;
        private float shakeDuration;
        private float shakeMagnitude;
        private Vector3 currentVelocity;

        public static TopDownCameraFollow Instance { get; private set; }

        public UILayoutMode CameraLayoutMode => cameraLayoutMode;
        public Vector3 ActiveOffset => IsPortraitActive ? GetAdaptivePortraitOffset() : landscapeOffset;
        public float ActivePitch => IsPortraitActive ? portraitPitch : landscapePitch;
        public float PortraitOrthographicSize => portraitBaseOrthoSize;
        public float PortraitFieldOfView => portraitBaseFov;
        public float PortraitForwardBias => portraitForwardFramingBias;
        public float PortraitLookAheadDistance => portraitLookAheadDistance;

        public bool IsPortraitActive
        {
            get
            {
                if (cameraLayoutMode == UILayoutMode.Portrait) return true;
                return Screen.height > Screen.width;
            }
        }

        public bool IsPortrait => IsPortraitActive;

        public void SnapToTargetImmediate()
        {
            ApplyCameraProfileImmediate();
        }

        private void Awake()
        {
            Instance = this;
            cachedCamera = GetComponent<Camera>();
            ApplyCameraProfileImmediate();
        }

        private void Start()
        {
            ApplyCameraProfileImmediate();
        }

        public void SetLayoutMode(UILayoutMode mode)
        {
            cameraLayoutMode = mode;
            ApplyCameraProfileImmediate();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                cachedPlayerController = target.GetComponent<PlayerController>();
                ApplyCameraProfileImmediate();
            }
        }

        private Vector3 GetAdaptivePortraitOffset()
        {
            float aspect = GetEffectiveAspect();
            // Reference 9:16 portrait aspect = 0.5625.
            // On narrower/taller screens (e.g. 1080x2400 = 0.45, 1170x2532 = 0.462),
            // pull camera slightly higher along its view ray so horizontal world width never feels cramped.
            float narrowFactor = Mathf.Clamp(0.5625f / Mathf.Max(0.38f, aspect), 1.0f, 1.24f);
            return new Vector3(
                portraitOffset.x,
                portraitOffset.y * narrowFactor,
                portraitOffset.z * narrowFactor
            );
        }

        private float GetEffectiveAspect()
        {
            if (cachedCamera != null && cachedCamera.aspect > 0.01f)
            {
                return cachedCamera.aspect;
            }
            if (Screen.width > 0 && Screen.height > 0)
            {
                return (float)Screen.width / Screen.height;
            }
            return 0.5625f;
        }

        public void ApplyCameraProfileImmediate()
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();

            // Ensure the serialized field uses our updated 0.25f forward bias if it was saved with an older value
            if (portraitForwardFramingBias > 0.45f)
            {
                portraitForwardFramingBias = 0.25f;
            }
            bool portrait = IsPortraitActive;
            float pitch = portrait ? portraitPitch : landscapePitch;
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            if (cachedCamera != null)
            {
                float aspect = GetEffectiveAspect();
                float narrowScale = portrait ? Mathf.Clamp(0.5625f / Mathf.Max(0.38f, aspect), 1.0f, 1.22f) : 1.0f;

                if (cachedCamera.orthographic)
                {
                    cachedCamera.orthographicSize = portrait ? (portraitBaseOrthoSize * narrowScale) : landscapeOrthoSize;
                }
                else
                {
                    cachedCamera.fieldOfView = portrait ? portraitBaseFov : landscapeFov;
                }
            }

            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                    cachedPlayerController = player.GetComponent<PlayerController>();
                }
            }

            if (target != null)
            {
                Vector3 desired = ComputeDesiredCameraPosition(target.position, Vector3.zero);
                transform.position = desired;
                currentVelocity = Vector3.zero;
            }
        }

        public void TriggerShake(float duration = 0.14f, float magnitude = 0.10f)
        {
            shakeDuration = Mathf.Clamp(Mathf.Max(shakeDuration, duration), 0f, 0.25f);
            shakeMagnitude = Mathf.Clamp(Mathf.Max(shakeMagnitude, magnitude), 0f, 0.12f);
        }

        private Vector3 ComputeDesiredCameraPosition(Vector3 playerPos, Vector3 lookAheadOffset)
        {
            // Ensure the serialized field uses our updated 0.25f forward bias if it was saved with an older value
            if (portraitForwardFramingBias > 0.45f)
            {
                portraitForwardFramingBias = 0.25f;
            }
            bool portrait = IsPortraitActive;
            Vector3 activeOffset = portrait ? GetAdaptivePortraitOffset() : landscapeOffset;

            Vector3 framedTarget = playerPos + lookAheadOffset;
            if (portrait)
            {
                // Shift focal point slightly forward (+Z) so the player sits at ~47.5% of screen height (45-55% target)
                framedTarget.z += portraitForwardFramingBias;
            }

            Vector2 clampXZ = portrait ? portraitClampXZ : landscapeClampXZ;
            framedTarget.x = Mathf.Clamp(framedTarget.x, -clampXZ.x, clampXZ.x);
            framedTarget.z = Mathf.Clamp(framedTarget.z, -clampXZ.y, clampXZ.y);

            return framedTarget + activeOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                    cachedPlayerController = player.GetComponent<PlayerController>();
                }
                else
                {
                    return;
                }
            }

            // Ensure the serialized field uses our updated 0.25f forward bias if it was saved with an older value
            if (portraitForwardFramingBias > 0.45f)
            {
                portraitForwardFramingBias = 0.25f;
            }
            bool portrait = IsPortraitActive;
            float pitch = portrait ? portraitPitch : landscapePitch;
            transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            if (cachedCamera != null)
            {
                float aspect = GetEffectiveAspect();
                float narrowScale = portrait ? Mathf.Clamp(0.5625f / Mathf.Max(0.38f, aspect), 1.0f, 1.22f) : 1.0f;
                if (cachedCamera.orthographic)
                {
                    cachedCamera.orthographicSize = portrait ? (portraitBaseOrthoSize * narrowScale) : landscapeOrthoSize;
                }
                else
                {
                    cachedCamera.fieldOfView = portrait ? portraitBaseFov : landscapeFov;
                }
            }

            // Omnidirectional Look-Ahead in Portrait
            Vector3 targetLookAhead = Vector3.zero;
            if (portrait && cachedPlayerController != null)
            {
                Vector3 moveVec = cachedPlayerController.MoveInputVector;
                if (moveVec.sqrMagnitude > 0.01f)
                {
                    targetLookAhead = new Vector3(moveVec.x * portraitLookAheadDistance * 0.75f, 0f, moveVec.z * portraitLookAheadDistance);
                }
            }
            currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSmoothSpeed);

            Vector3 targetPosition = ComputeDesiredCameraPosition(target.position, currentLookAhead);

            // Apply screen shake if active
            if (shakeDuration > 0f)
            {
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.y = 0f;
                targetPosition += shakeOffset;

                shakeDuration -= Time.deltaTime;
                if (shakeDuration <= 0f)
                {
                    shakeMagnitude = 0f;
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        /// <summary>
        /// Returns the normalized viewport position (0..1 in X and Y) of a world position.
        /// </summary>
        public Vector2 GetViewportPoint(Vector3 worldPos)
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null) return new Vector2(0.5f, 0.5f);
            Vector3 vp = cachedCamera.WorldToViewportPoint(worldPos);
            return new Vector2(vp.x, vp.y);
        }

        /// <summary>
        /// Calculates the visible half-width (X) and half-depth (Z) on the ground plane (y = 0.5f)
        /// around the player so SpawnDirector and Bosses can adapt to Portrait vs Landscape viewports.
        /// </summary>
        public Vector2 GetVisibleGroundHalfExtents()
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null)
            {
                return IsPortraitActive ? new Vector2(8.2f, 13.5f) : new Vector2(13.5f, 9.5f);
            }

            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, 0.5f, 0f));
            Ray centerRay = cachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Ray rightRay = cachedCamera.ViewportPointToRay(new Vector3(1.0f, 0.5f, 0f));
            Ray topRay = cachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.88f, 0f));
            Ray bottomRay = cachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.08f, 0f));

            Vector3 centerPt = target != null ? target.position : Vector3.zero;
            if (groundPlane.Raycast(centerRay, out float dCenter))
            {
                centerPt = centerRay.GetPoint(dCenter);
            }

            float halfX = IsPortraitActive ? 8.2f : 13.5f;
            if (groundPlane.Raycast(rightRay, out float dRight))
            {
                Vector3 rightPt = rightRay.GetPoint(dRight);
                halfX = Mathf.Clamp(Mathf.Abs(rightPt.x - centerPt.x), 6.8f, 18f);
            }

            float halfZ = IsPortraitActive ? 13.0f : 9.5f;
            if (groundPlane.Raycast(topRay, out float dTop) && groundPlane.Raycast(bottomRay, out float dBot))
            {
                Vector3 topPt = topRay.GetPoint(dTop);
                Vector3 botPt = bottomRay.GetPoint(dBot);
                halfZ = Mathf.Clamp(Mathf.Abs(topPt.z - botPt.z) * 0.5f, 8.0f, 18f);
            }

            return new Vector2(halfX, halfZ);
        }

        public Vector2 GetVisibleGroundHalfExtents(float extraPadding)
        {
            Vector2 baseExtents = GetVisibleGroundHalfExtents();
            return new Vector2(baseExtents.x + extraPadding, baseExtents.y + extraPadding);
        }

        public bool IsWorldPositionVisibleInViewport(Vector3 worldPos, float margin = 0.05f)
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null) return true;
            Vector3 vp = cachedCamera.WorldToViewportPoint(worldPos);
            return vp.z > 0f &&
                   vp.x >= margin && vp.x <= (1f - margin) &&
                   vp.y >= margin && vp.y <= (1f - margin);
        }
    }
}
