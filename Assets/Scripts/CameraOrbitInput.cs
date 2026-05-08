using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo
{
    /// <summary>
    /// Phase 1, Sitting D — mouse-orbit pivot for the third-person camera.
    ///
    /// Attached to the CameraTarget GameObject (a child of the X Bot, positioned
    /// around chest height). Each frame this reads mouse delta from the new
    /// Input System and yaws / pitches the CameraTarget itself.
    ///
    /// Cinemachine's CinemachineThirdPersonFollow rigidly tracks this target,
    /// so as the CameraTarget rotates, the camera orbits the player. The X Bot
    /// rotates independently — toward its movement direction, driven by
    /// PlayerController. Camera and character rotation are intentionally
    /// decoupled.
    ///
    /// IMPORTANT: rotation is written in WORLD space (transform.rotation), not
    /// local. CameraTarget is a CHILD of the X Bot — but a child's world
    /// rotation otherwise cascades from its parent. If we wrote localRotation,
    /// then any time the X Bot rotates, CameraTarget's world rotation rotates
    /// with it, which makes Cinemachine swing the camera, which changes the
    /// camera-relative "forward" PlayerController reads, which makes the X Bot
    /// rotate further — a feedback loop that manifests as the character
    /// spinning instead of running. Writing transform.rotation breaks the
    /// cascade: world orientation is purely mouse-driven, and CameraTarget
    /// just inherits the X Bot's POSITION (which is what we want).
    ///
    /// Why this design over Cinemachine's built-in input axis controller:
    ///   - All camera state lives in a regular Transform — easy to debug
    ///     visually (just watch the CameraTarget rotate in the Hierarchy).
    ///   - No coupling to a specific Cinemachine version's input axis API
    ///     or to "Active Input Handling" project settings.
    ///   - Swapping ThirdPersonFollow → OrbitalFollow later doesn't change
    ///     this script.
    ///
    /// Setup is automated by Kaligo → Setup → Sitting D.
    /// Tuning (sensitivity, pitch range, invert Y) lives in Sitting E.
    /// </summary>
    public class CameraOrbitInput : MonoBehaviour
    {
        [Header("Sensitivity")]
        [Tooltip("Horizontal mouse sensitivity (degrees per pixel of mouse delta).")]
        [SerializeField] private float horizontalSensitivity = 0.2f;

        [Tooltip("Vertical mouse sensitivity (degrees per pixel of mouse delta).")]
        [SerializeField] private float verticalSensitivity = 0.1f;

        [Tooltip("Invert vertical look. When false, mouse-up tilts the camera up.")]
        [SerializeField] private bool invertY = false;

        [Header("Pitch Limits")]
        [Tooltip("Minimum pitch in degrees. Negative = camera can look down.")]
        [SerializeField] private float minPitch = -40f;

        [Tooltip("Maximum pitch in degrees. Positive = camera can look up.")]
        [SerializeField] private float maxPitch = 70f;

        [Header("Cursor")]
        [Tooltip("Hide and lock the cursor while the game is running. " +
                 "Press Esc in the Editor to release it.")]
        [SerializeField] private bool lockCursor = true;

        // ─── Internals ────────────────────────────────────────────────────────

        private float yaw;
        private float pitch;

        // ─── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            // Initialize from current WORLD rotation so the camera starts
            // pointing wherever the player is currently facing — i.e. if the
            // X Bot is rotated 90° in the scene, the camera starts behind
            // them, not at world-Z forward.
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = NormalizePitch(e.x);

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (Mouse.current == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue();

            yaw += delta.x * horizontalSensitivity;

            float dy = -delta.y * verticalSensitivity;
            if (invertY) dy = -dy;
            pitch = Mathf.Clamp(pitch + dy, minPitch, maxPitch);

            // World rotation, NOT localRotation — see class docstring for why.
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// transform.localEulerAngles returns 0..360. To clamp around 0 with
        /// negative values (e.g. -40..70) we shift the upper half into negatives.
        /// </summary>
        private static float NormalizePitch(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
