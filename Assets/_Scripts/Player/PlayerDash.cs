using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash Settings")]
        [SerializeField] private float baseCooldown = 1.90f;
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.16f;

        [Header("Visual Feedback")]
        [SerializeField] private Transform visualRoot;

        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private TrailRenderer dashTrail;
        private float cooldownMultiplier = 1f;
        private float lastDashTime = -10f;
        private bool isDashing;
        private Vector3 dashDirection;

        public float BaseCooldown => baseCooldown;
        public float Cooldown => baseCooldown * cooldownMultiplier;
        public float CooldownRemaining => Mathf.Max(0f, (lastDashTime + Cooldown) - Time.time);
        public float CooldownPercent => Mathf.Clamp01(CooldownRemaining / Cooldown);
        public bool IsDashing => isDashing;
        public bool CanDash => !isDashing && Time.time >= lastDashTime + Cooldown && (playerHealth == null || playerHealth.IsAlive);

        public event Action OnDashStarted;
        public event Action OnDashEnded;

        private void Awake()
        {
            baseCooldown = WorldDifficultyConfig.Current.dashBaseCooldown;
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            SetupDashTrail();
        }

        private void SetupDashTrail()
        {
            GameObject trailObj = new GameObject("DashGhostTrail");
            trailObj.transform.SetParent(transform, false);
            trailObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            dashTrail = trailObj.AddComponent<TrailRenderer>();
            dashTrail.time = 0.15f;
            dashTrail.startWidth = 0.55f;
            dashTrail.endWidth = 0.05f;
            dashTrail.autodestruct = false;
            dashTrail.emitting = false;

            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                dashTrail.material = new Material(defaultShader);
            }
            dashTrail.startColor = new Color(0.3f, 0.9f, 1f, 0.75f);
            dashTrail.endColor = new Color(0.1f, 0.4f, 1f, 0f);
        }

        public void SetCooldownMultiplier(float multiplier)
        {
            cooldownMultiplier = Mathf.Max(0.2f, multiplier);
        }

        public void ResetDash()
        {
            isDashing = false;
            lastDashTime = -10f;
        }

        private void Update()
        {
            bool dashRequested = (GameInputManager.Instance != null)
                ? GameInputManager.Instance.ConsumeDashPressed()
                : (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift));

            if (CanDash && dashRequested)
            {
                StartDash();
            }
        }

        public void StartDash()
        {
            if (!CanDash) return;

            Vector3 moveDir = playerController != null ? playerController.MoveInputVector : Vector3.zero;
            if (moveDir.sqrMagnitude < 0.01f)
            {
                moveDir = transform.forward;
            }
            dashDirection = moveDir.normalized;

            lastDashTime = Time.time;
            AudioManager.Instance?.PlaySound(SoundType.PlayerDash, 0.85f, 0.08f);
            MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticType.Light);
            TopDownCameraFollow.Instance?.TriggerShake(0.08f, 0.12f);
            StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            isDashing = true;
            if (dashTrail != null)
            {
                dashTrail.Clear();
                dashTrail.emitting = true;
            }
            OnDashStarted?.Invoke();

            float elapsed = 0f;
            Vector3 originalScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / dashDuration;

                // Move player via controller
                if (playerController != null)
                {
                    playerController.ApplyCustomMovement(dashDirection * dashSpeed * Time.deltaTime);
                }

                // Squash and stretch feedback
                if (visualRoot != null)
                {
                    float stretch = Mathf.Sin(progress * Mathf.PI);
                    visualRoot.localScale = new Vector3(
                        originalScale.x * (1f - stretch * 0.2f),
                        originalScale.y * (1f - stretch * 0.2f),
                        originalScale.z * (1f + stretch * 0.4f)
                    );
                }

                yield return null;
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = originalScale;
            }

            if (dashTrail != null)
            {
                dashTrail.emitting = false;
            }

            isDashing = false;
            OnDashEnded?.Invoke();
        }
    }
}
