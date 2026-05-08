using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo
{
    /// <summary>
    /// Phase 1, Sitting C — third-person planar movement.
    ///
    /// Reads WASD via the Input System, moves the character with CharacterController,
    /// rotates them smoothly toward the movement direction, and feeds the magnitude
    /// of horizontal velocity into the Animator's "Speed" parameter — driving the
    /// idle ↔ walk ↔ run blend tree from real movement.
    ///
    /// Movement is camera-relative: W = "into the screen" from the Main Camera's
    /// perspective. When Cinemachine replaces the Main Camera in Sitting D, this
    /// script does NOT need to change.
    ///
    /// Setup:
    ///   1. Attach to the X Bot. CharacterController auto-adds via [RequireComponent].
    ///   2. On the new CharacterController: set Center Y ≈ 0.9 (waist height) so the
    ///      capsule sits around the body, not under the feet.
    ///   3. Press Play, mash WASD. Hold Shift to sprint.
    ///
    /// Not included yet (intentional): jump, dodge, lock-on. Those land in later sittings.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        // ─── Tuning ───────────────────────────────────────────────────────────

        [Header("Movement")]
        [Tooltip("Speed (m/s) when walking.")]
        [SerializeField] private float walkSpeed = 7f;

        [Tooltip("Speed (m/s) when sprinting (Left Shift).")]
        [SerializeField] private float runSpeed = 12f;

        [Tooltip("How fast horizontal velocity changes (m/s²). Higher = snappier; lower = floatier.")]
        [SerializeField] private float acceleration = 20f;

        [Tooltip("How fast the character rotates to face the movement direction (degrees/sec).")]
        [SerializeField] private float turnSpeedDegPerSec = 720f;

        [Header("Gravity")]
        [Tooltip("Downward acceleration applied each frame (m/s²). -9.81 is realistic; -20 feels better for action games.")]
        [SerializeField] private float gravity = -20f;

        // ─── Internals ────────────────────────────────────────────────────────

        private CharacterController controller;
        private Animator animator;
        private Camera mainCamera;

        private Vector3 horizontalVelocity;
        private float   verticalVelocity;

        public bool    IsGrounded          => controller.isGrounded;
        public Vector3 CurrentMoveDirection => horizontalVelocity.sqrMagnitude > 0.01f
                                                ? horizontalVelocity.normalized
                                                : transform.forward;

        private static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");

        private Kaligo.Skills.SkillExecutor skillExecutor;

        // ─── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            controller     = GetComponent<CharacterController>();
            animator       = GetComponent<Animator>();
            mainCamera     = Camera.main;
            skillExecutor  = GetComponent<Kaligo.Skills.SkillExecutor>();

            if (mainCamera == null)
            {
                Debug.LogWarning(
                    "PlayerController: no Camera tagged 'MainCamera' found. " +
                    "Movement will fall back to world-relative axes."
                );
            }
        }

        private void Update()
        {
            Vector2 input    = ReadMoveInput();
            bool    slowWalk = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

            float skillMult  = skillExecutor != null ? skillExecutor.MovementMultiplier : 1f;
            float topSpeed   = slowWalk ? walkSpeed : runSpeed;
            float targetSpeed = topSpeed * input.magnitude * skillMult;

            Vector3 moveDir = CameraRelativeDirection(input);
            Vector3 desiredHorizontalVelocity = moveDir * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                desiredHorizontalVelocity,
                acceleration * Time.deltaTime
            );

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            bool rotLocked = skillExecutor != null && skillExecutor.LockRotation;
            if (!rotLocked && horizontalVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeedDegPerSec * Time.deltaTime
                );
            }

            // Local velocity normalized to runSpeed for the 2D blend tree
            float localX = Vector3.Dot(horizontalVelocity, transform.right)   / runSpeed;
            float localZ = Vector3.Dot(horizontalVelocity, transform.forward) / runSpeed;
            animator.SetFloat(VelocityXHash, localX);
            animator.SetFloat(VelocityZHash, localZ);
        }

        // ─── Root motion ──────────────────────────────────────────────────────

        /// <summary>
        /// Called by Unity instead of auto-applying root motion.
        /// During attacks (lockRotation=true) we apply only the root ROTATION delta from
        /// the clip — this is what makes spinning/turning attack animations look correct.
        /// Root position is always ignored: CharacterController.Move() owns all movement.
        /// </summary>
        private void OnAnimatorMove()
        {
            if (skillExecutor != null && skillExecutor.LockRotation)
                transform.rotation *= animator.deltaRotation;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        public void ApplyJumpImpulse(float force)
        {
            if (controller.isGrounded)
                verticalVelocity = force;
        }

        public void ApplyDodgeImpulse(float force)
        {
            horizontalVelocity = CurrentMoveDirection * force;
        }

        /// <summary>Read WASD as a 2D input vector in [-1, 1]. Diagonals normalized.</summary>
        private Vector2 ReadMoveInput()
        {
            if (Keyboard.current == null) return Vector2.zero;

            Vector2 v = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) v.y += 1f;
            if (Keyboard.current.sKey.isPressed) v.y -= 1f;
            if (Keyboard.current.dKey.isPressed) v.x += 1f;
            if (Keyboard.current.aKey.isPressed) v.x -= 1f;

            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        /// <summary>
        /// Convert a 2D input vector to a 3D world direction relative to the camera's
        /// horizontal facing. W moves you the way the camera is looking (Y-flattened).
        /// </summary>
        private Vector3 CameraRelativeDirection(Vector2 input)
        {
            if (mainCamera == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            return forward * input.y + right * input.x;
        }
    }
}
