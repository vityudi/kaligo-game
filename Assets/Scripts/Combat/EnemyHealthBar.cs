using UnityEngine;
using UnityEngine.UI;

namespace Kaligo.Combat
{
    /// <summary>
    /// World-space health bar that billboards toward the camera.
    /// Fixes its own canvas dimensions on Awake so it is always visible at the correct scale
    /// regardless of how the canvas was created.
    /// Expected hierarchy: this script is on the Canvas GO, with Background and Fill children.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private HealthSystem  health;

        // Physical world-space size of the bar (meters)
        private const float BarWidth  = 1.4f;
        private const float BarHeight = 0.12f;

        private Camera mainCam;

        private void Awake()
        {
            // World-space canvas: sizeDelta in "canvas pixels", localScale converts to meters.
            // We use 1 canvas unit = 1 cm (scale = 0.01) so sizeDelta = (140, 12) → 1.4m × 0.12m.
            var rt = (RectTransform)transform;
            rt.sizeDelta  = new Vector2(BarWidth * 100f, BarHeight * 100f);
            transform.localScale = Vector3.one * 0.01f;
        }

        private void Start()
        {
            mainCam = Camera.main;

            if (health != null)
            {
                health.OnHealthChanged += OnHealthChanged;
                // Initialise fill to current health fraction
                OnHealthChanged(health.CurrentHealth, health.MaxHealth);
            }
        }

        private void LateUpdate()
        {
            if (mainCam != null)
                transform.forward = mainCam.transform.forward;
        }

        private void OnHealthChanged(float current, float max)
        {
            if (fillRect == null) return;
            var anchor    = fillRect.anchorMax;
            anchor.x      = max > 0f ? current / max : 0f;
            fillRect.anchorMax = anchor;
        }
    }
}
