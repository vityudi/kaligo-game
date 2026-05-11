using UnityEngine;

namespace Kaligo.Characters.Player
{
    /// <summary>
    /// Minimal third-person camera used on the placeholder player only.
    /// Follows a parent CameraTarget that is a child of the player.
    ///
    /// Mouse X → rotates the CameraTarget (and therefore this camera) around Y.
    /// Mouse Y → pitches the camera up/down.
    ///
    /// This is intentionally simple — it will be replaced by the full
    /// Cinemachine rig + CameraOrbitInput when the real player rig is
    /// transferred into the scene.
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 2.5f;
        [SerializeField] private float pitchMin = -20f;
        [SerializeField] private float pitchMax =  60f;

        private float pitch;
        private Transform cameraTarget; // parent of this camera

        private void Awake()
        {
            cameraTarget = transform.parent;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void LateUpdate()
        {
            if (cameraTarget == null) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Yaw — rotate the entire CameraTarget around Y (affects movement direction)
            cameraTarget.Rotate(Vector3.up, mouseX, Space.World);

            // Pitch — tilt just this camera up/down
            pitch = Mathf.Clamp(pitch - mouseY, pitchMin, pitchMax);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
