using UnityEngine;

namespace CursedHarvest
{
    public class PlayerGroundIndicator : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.9f, 1.0f, 0.85f);
        [SerializeField] private Color dashColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        [SerializeField] private Color luckyLastColor = new Color(1.0f, 0.35f, 0.1f, 1.0f);
        [SerializeField] private Color lowHealthColor = new Color(1.0f, 0.2f, 0.2f, 0.9f);

        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private PlayerDash playerDash;
        private LuckyLastSystem luckySystem;

        private LineRenderer ringRenderer;
        private LineRenderer arrowRenderer;
        private Transform arrowTransform;

        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            playerHealth = GetComponentInParent<PlayerHealth>();
            playerDash = GetComponentInParent<PlayerDash>();
            luckySystem = GetComponentInParent<LuckyLastSystem>();

            CreateVisuals();
        }

        private void CreateVisuals()
        {
            // 1. Ground Ring
            GameObject ringObj = new GameObject("GroundRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            ringRenderer = ringObj.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.positionCount = 36;
            ringRenderer.startWidth = 0.07f;
            ringRenderer.endWidth = 0.07f;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            ringRenderer.material = lineMat;

            float radius = 0.85f;
            for (int i = 0; i < 36; i++)
            {
                float angle = i * (360f / 36) * Mathf.Deg2Rad;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius));
            }

            // 2. Directional Arrow / Chevron at front of ring
            GameObject arrowObj = new GameObject("DirectionArrow");
            arrowObj.transform.SetParent(transform, false);
            arrowObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            arrowTransform = arrowObj.transform;

            arrowRenderer = arrowObj.AddComponent<LineRenderer>();
            arrowRenderer.useWorldSpace = false;
            arrowRenderer.positionCount = 3;
            arrowRenderer.startWidth = 0.10f;
            arrowRenderer.endWidth = 0.06f;
            arrowRenderer.material = lineMat;
            arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrowRenderer.receiveShadows = false;

            // Chevron pointing forward at edge of radius
            arrowRenderer.SetPosition(0, new Vector3(-0.25f, 0f, 0.70f));
            arrowRenderer.SetPosition(1, new Vector3(0f, 0f, 1.05f));
            arrowRenderer.SetPosition(2, new Vector3(0.25f, 0f, 0.70f));
        }

        private void Update()
        {
            if (playerController == null) return;

            // Face forward direction of the player
            transform.rotation = Quaternion.Euler(0f, playerController.transform.eulerAngles.y, 0f);

            // Determine active color
            Color targetColor = normalColor;

            if (luckySystem != null && luckySystem.IsActive)
            {
                float pulse = 0.8f + Mathf.PingPong(Time.time * 6f, 0.4f);
                targetColor = luckyLastColor * pulse;
            }
            else if (playerDash != null && playerDash.IsDashing)
            {
                targetColor = dashColor;
            }
            else if (playerHealth != null && playerHealth.HealthPercent <= 0.25f)
            {
                float pulse = 0.7f + Mathf.PingPong(Time.time * 4f, 0.3f);
                targetColor = lowHealthColor * pulse;
            }

            if (ringRenderer != null)
            {
                ringRenderer.startColor = targetColor;
                ringRenderer.endColor = targetColor;
            }
            if (arrowRenderer != null)
            {
                arrowRenderer.startColor = targetColor;
                arrowRenderer.endColor = targetColor;
            }
        }
    }
}
