using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kaligo.Combat
{
    /// <summary>
    /// World-space health bar that billboards toward the camera.
    /// Expected hierarchy: this script on the Canvas GO, with BG and Fill children.
    /// Auto-discovers HealthSystem from parent and shows mob display name above the bar.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private HealthSystem  health;

        private const float BarWidth  = 1.4f;
        private const float BarHeight = 0.12f;
        private Camera mainCam;

        private void Awake()
        {
            var rt       = (RectTransform)transform;
            rt.sizeDelta = new Vector2(BarWidth * 100f, BarHeight * 100f);
            transform.localScale = Vector3.one * 0.01f;
        }

        private void Start()
        {
            mainCam = Camera.main;

            if (health == null)
                health = GetComponentInParent<HealthSystem>();

            if (fillRect == null)
            {
                var fillGO = transform.Find("Fill");
                if (fillGO != null)
                    fillRect = fillGO.GetComponent<RectTransform>();
            }

            TryAddNameLabel();

            if (health != null)
            {
                health.OnHealthChanged += OnHealthChanged;
                health.OnDeath         += () => gameObject.SetActive(false);
                OnHealthChanged(health.CurrentHealth, health.MaxHealth);
            }
            else
            {
                Debug.LogWarning("[EnemyHealthBar] No HealthSystem found on parent.", this);
            }
        }

        private void TryAddNameLabel()
        {
            if (transform.Find("Label") != null) return;

            // Parse name from the root mob GO: "[Mob] Goblin" -> "Goblin"
            string mobName = null;
            string goName  = transform.root.name;
            if (goName.StartsWith("[Mob] "))
                mobName = goName.Substring(6);

            if (string.IsNullOrEmpty(mobName)) return;

            var go            = new GameObject("Label");
            go.transform.SetParent(transform, false);
            var rt            = go.AddComponent<RectTransform>();
            rt.anchorMin      = new Vector2(0f, 1f);
            rt.anchorMax      = new Vector2(1f, 1f);
            rt.pivot          = new Vector2(0.5f, 0f);
            rt.sizeDelta      = new Vector2(0f, 25f);
            rt.anchoredPosition = new Vector2(0f, 4f);

            var tmp        = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = mobName;
            tmp.fontSize   = 18f;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.color      = new Color(1f, 0.9f, 0.75f, 1f);
            tmp.fontStyle  = FontStyles.Bold;
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
