using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo
{
    /// <summary>
    /// Owns cursor lock state.
    /// Alt   → enter UI mode (cursor free, camera/skills paused).
    /// Esc   → return to game mode from UI mode.
    /// Targeting.cs handles Esc for releasing lock-on independently.
    /// </summary>
    public class CursorController : MonoBehaviour
    {
        public static CursorController Instance { get; private set; }

        public bool IsUIMode { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
        }

        private void Start() => SetGameMode();

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.leftAltKey.wasPressedThisFrame)
            {
                if (IsUIMode) SetGameMode();
                else SetUIMode();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame && IsUIMode)
                SetGameMode();
        }

        public void SetGameMode()
        {
            IsUIMode         = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public void SetUIMode()
        {
            IsUIMode         = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }
}
