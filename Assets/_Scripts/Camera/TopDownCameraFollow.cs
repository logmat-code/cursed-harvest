using UnityEngine;

namespace CursedHarvest
{
    public class TopDownCameraFollow : MonoBehaviour
    {
        [Header("Target & Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 11.0f, -7.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float cameraPitch = 52f;

        [Header("Screen Shake")]
        private float shakeDuration;
        private float shakeMagnitude;
        private Vector3 currentVelocity;

        public static TopDownCameraFollow Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        public void TriggerShake(float duration = 0.18f, float magnitude = 0.18f)
        {
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeMagnitude = Mathf.Clamp(Mathf.Max(shakeMagnitude, magnitude), 0f, 0.20f);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
                else
                {
                    return;
                }
            }

            Vector3 targetPosition = target.position + offset;

            // Apply screen shake if active
            if (shakeDuration > 0f)
            {
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.y = 0f; // Keep vertical height steady for readability
                targetPosition += shakeOffset;

                shakeDuration -= Time.deltaTime;
                if (shakeDuration <= 0f)
                {
                    shakeMagnitude = 0f;
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }
    }
}
