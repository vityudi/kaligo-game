using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo.Characters.Player
{
    /// <summary>
    /// Minimal WASD + gravity movement for the placeholder player capsule.
    /// Does NOT require an Animator — designed to run on a plain capsule so the
    /// world is immediately playable before the real X Bot rig is imported.
    ///
    /// Camera-relative: W = into the screen from the camera's perspective.
    /// The camera direction is read from the CameraTarget child (same pivot that
    /// SimpleCameraFollow rotates), so yaw changes are reflected immediately.
    ///
    /// Replace this with PlayerController once the X Bot + Animator is in the scene.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMover : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float walkSpeed   = 6f;
        [SerializeField] private float sprintSpeed = 11f;
        [SerializeField] private float turnSpeed   = 720f;   // degrees/sec

        [Header("Physics")]
        [SerializeField] private float gravity = -20f;

        // ── State ─────────────────────────────────────────────────────────────

        private CharacterController cc;
        private Transform           cameraTarget;   // CameraTarget child — owns yaw
        private float               verticalVelocity;
        private Vector3             horizontalVelocity;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            cc           = GetComponent<CharacterController>();
            cameraTarget = transform.Find("CameraTarget");

            if (cameraTarget == null)
                Debug.LogWarning("[PlayerMover] No 'CameraTarget' child found — movement will be world-relative.");
        }

        private void Update()
        {
            Vector2 input = ReadInput();
            bool sprint   = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float speed   = sprint ? sprintSpeed : walkSpeed;

            // Camera-relative direction in world XZ
            Vector3 moveDir = ToWorldDirection(input);

            Vector3 target = moveDir * (input.magnitude * speed);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, 30f * Time.deltaTime);

            // Gravity
            if (cc.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            cc.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // Face movement direction
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion desired = Quaternion.LookRotation(horizontalVelocity.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, desired, turnSpeed * Time.deltaTime);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Vector2 ReadInput()
        {
            if (Keyboard.current == null) return Vector2.zero;
            Vector2 v = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) v.y += 1f;
            if (Keyboard.current.sKey.isPressed) v.y -= 1f;
            if (Keyboard.current.dKey.isPressed) v.x += 1f;
            if (Keyboard.current.aKey.isPressed) v.x -= 1f;
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        private Vector3 ToWorldDirection(Vector2 input)
        {
            // Use CameraTarget's world yaw as "forward"
            Transform pivot = cameraTarget ?? Camera.main?.transform;
            if (pivot == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 fwd   = pivot.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = pivot.right;   right.y = 0f; right.Normalize();
            return fwd * input.y + right * input.x;
        }
    }
}
