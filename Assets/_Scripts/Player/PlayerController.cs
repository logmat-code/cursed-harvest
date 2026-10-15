using UnityEngine;

namespace CursedHarvest
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 6.8f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float gravity = 20f;

        [Header("Visual Juice")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float bobFrequency = 14f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float tiltAngle = 5f;

        private CharacterController characterController;
        private PlayerHealth playerHealth;
        private PlayerDash playerDash;

        private Vector3 moveInput;
        private float verticalVelocity;
        private float speedMultiplier = 1f;
        private float walkCycleTimer;
        private Vector3 visualOriginalPos;
        private Quaternion visualOriginalRot;

        public Vector3 MoveInputVector => moveInput;
        public float MoveSpeed => baseMoveSpeed * speedMultiplier;
        public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerHealth = GetComponent<PlayerHealth>();
            playerDash = GetComponent<PlayerDash>();

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (visualRoot != null)
            {
                visualOriginalPos = visualRoot.localPosition;
                visualOriginalRot = visualRoot.localRotation;
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ApplyCustomMovement(Vector3 delta)
        {
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(delta);
            }
        }

        private void Update()
        {
            if (playerHealth != null && !playerHealth.IsAlive)
            {
                ApplyGravity();
                ResetVisualJuice();
                return;
            }

            // Read desktop inputs (WASD / Arrow Keys)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            moveInput = new Vector3(h, 0f, v).normalized;

            // If dashing, dash routine handles horizontal displacement
            if (playerDash != null && playerDash.IsDashing)
            {
                ApplyGravity();
                return;
            }

            // Standard movement
            Vector3 velocity = moveInput * MoveSpeed;

            // Ground check & gravity
            if (characterController.isGrounded)
            {
                verticalVelocity = -2f; // Slight downward push to keep grounded
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            // Handle rotation
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                UpdateVisualJuice(true);
            }
            else
            {
                UpdateVisualJuice(false);
            }
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }
            characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }

        private void UpdateVisualJuice(bool moving)
        {
            if (visualRoot == null) return;

            if (moving)
            {
                walkCycleTimer += Time.deltaTime * bobFrequency * (MoveSpeed / baseMoveSpeed);
                float bobY = Mathf.Abs(Mathf.Sin(walkCycleTimer)) * bobAmplitude;
                visualRoot.localPosition = visualOriginalPos + new Vector3(0f, bobY, 0f);

                float tiltZ = Mathf.Sin(walkCycleTimer * 0.5f) * tiltAngle;
                visualRoot.localRotation = visualOriginalRot * Quaternion.Euler(0f, 0f, tiltZ);
            }
            else
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualOriginalPos, Time.deltaTime * 10f);
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualOriginalRot, Time.deltaTime * 10f);
            }
        }

        private void ResetVisualJuice()
        {
            if (visualRoot == null) return;
            visualRoot.localPosition = visualOriginalPos;
            visualRoot.localRotation = visualOriginalRot;
        }

        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                direction.y = 0f;
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * 1.5f * Time.deltaTime);
            }
        }
    }
}
