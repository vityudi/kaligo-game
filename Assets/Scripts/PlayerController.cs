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
        [Tooltip("Speed (m/s) when walking. Matches the 'walk' threshold in the blend tree.")]
        [SerializeField] private float walkSpeed = 2f;

        [Tooltip("Speed (m/s) when sprinting (Left Shift). Matches the 'run' threshold in the blend tree.")]
        [SerializeField] private float runSpeed = 6f;

        [Tooltip("How fast horizontal velocity changes (m/s²). Higher = snappier; lower = floatier.")]
        [SerializeField] private float acceleration = 20f;

        [Tooltip("How fast the character rotates to face the movement direction (degrees/sec).")]
        [SerializeField] private float turnSpeedDegPerSec = 720f;

        [Header("Gravity")]
        [Tooltip("Downward acceleration applied each frame (m/s²). -9.81 is realistic; -20 feels better for action games.")]
        [SerializeField] private float gravity = -20f;

        [Header("Animator")]
        [Tooltip("Smoothing time (seconds) for the Speed parameter. Avoids snapping between idle/walk/run.")]
        [SerializeField] private float animSpeedDampTime = 0.1f;

        // ─── Internals ────────────────────────────────────────────────────────

        private CharacterController controller;
        private Animator animator;
        private Camera mainCamera;

        private Vector3 horizontalVelocity;   // current planar velocity in m/s
        private float verticalVelocity;       // current vertical velocity in m/s (gravity)
        private float currentAnimSpeed;       // smoothed value written to Animator
        private float animSpeedDampVelocity;  // ref param for Mathf.SmoothDamp

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        // ─── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            mainCamera = Camera.main;

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
            Vector2 input = ReadMoveInput();
            bool sprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float targetSpeed = (sprinting ? runSpeed : walkSpeed) * input.magnitude;

            Vector3 moveDir = CameraRelativeDirection(input);
            Vector3 desiredHorizontalVelocity = moveDir * targetSpeed;

            // Smooth horizontal velocity toward the desired vector
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                desiredHorizontalVelocity,
                acceleration * Time.deltaTime
            );

            // Gravity: stick to ground when grounded, accumulate when in the air
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // small negative to keep us grounded on slopes
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            // Move
            Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            // Rotate to face the movement direction (only while we're actually moving)
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeedDegPerSec * Time.deltaTime
                );
            }

            // Feed animator: smoothed magnitude of horizontal velocity
            float animTarget = horizontalVelocity.magnitude;
            currentAnimSpeed = Mathf.SmoothDamp(
                currentAnimSpeed,
                animTarget,
                ref animSpeedDampVelocity,
                animSpeedDampTime
            );
            animator.SetFloat(SpeedHash, currentAnimSpeed);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

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
