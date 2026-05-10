using UnityEngine;
using TMPro;
using Kaligo.Services;

namespace Kaligo.Items
{
    /// <summary>
    /// Sits on a ground-loot GameObject.
    /// When the player walks into its trigger collider, the item is added to the
    /// inventory via GameServices.Inventory.Add and the pickup destroys itself.
    ///
    /// A floating label above the pickup shows the item name in rarity colour.
    /// The label is created procedurally (no prefab required) using TextMeshPro.
    ///
    /// LootDrop.SpawnPickup calls Initialize() after instantiation.
    /// </summary>
    public class LootPickup : MonoBehaviour
    {
        // ── Data ──────────────────────────────────────────────────────────────

        private ItemData _item;
        private int      _quantity;

        // ── Label ─────────────────────────────────────────────────────────────

        private GameObject     _labelGO;
        private TextMeshPro    _label;

        // Label bobs gently above the item
        private float _bobTimer;
        private const float BobAmplitude = 0.08f;
        private const float BobSpeed     = 1.4f;
        private const float LabelHeight  = 0.8f;

        // ── Public API ────────────────────────────────────────────────────────

        public void Initialize(ItemData item, int quantity)
        {
            _item     = item;
            _quantity = quantity;
            CreateLabel();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Update()
        {
            // Gentle rotation
            transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

            // Bob the label
            if (_labelGO != null)
            {
                _bobTimer += Time.deltaTime * BobSpeed;
                float yOffset = LabelHeight + Mathf.Sin(_bobTimer) * BobAmplitude;
                _labelGO.transform.position = transform.position + Vector3.up * yOffset;
                _labelGO.transform.LookAt(
                    _labelGO.transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (_item == null) return;

            if (GameServices.Inventory != null)
                GameServices.Inventory.Add(_item.itemId, _quantity);

            // Floating text feedback
            ShowPickupText(other.transform.position + Vector3.up * 1.8f);

            Destroy(_labelGO);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_labelGO != null) Destroy(_labelGO);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void CreateLabel()
        {
            if (_item == null) return;

            _labelGO = new GameObject($"Label_{_item.itemId}");
            _label   = _labelGO.AddComponent<TextMeshPro>();

            string quantityStr = _quantity > 1 ? $" x{_quantity}" : "";
            _label.text      = _item.displayName + quantityStr;
            _label.color     = _item.RarityColor();
            _label.fontSize  = 3f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontStyle = FontStyles.Bold;

            _labelGO.transform.position = transform.position + Vector3.up * LabelHeight;
        }

        private void ShowPickupText(Vector3 worldPos)
        {
            if (_item == null) return;

            var go    = new GameObject("PickupFeedback");
            var label = go.AddComponent<TextMeshPro>();

            string quantityStr = _quantity > 1 ? $" x{_quantity}" : "";
            label.text      = $"+{_item.displayName}{quantityStr}";
            label.color     = _item.RarityColor();
            label.fontSize  = 3.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;

            go.transform.position = worldPos;
            go.AddComponent<PickupFeedbackFloat>();
        }
    }

    /// <summary>
    /// Floats the pickup text upward and fades it out over 1.5 s.
    /// </summary>
    internal class PickupFeedbackFloat : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private float       _elapsed;
        private const float Duration = 1.5f;
        private const float Speed    = 1.2f;

        private void Awake() => _tmp = GetComponent<TextMeshPro>();

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * Speed * Time.deltaTime;

            if (Camera.main != null)
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                                 Camera.main.transform.rotation * Vector3.up);

            float alpha = 1f - (_elapsed / Duration);
            _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, Mathf.Clamp01(alpha));

            if (_elapsed >= Duration) Destroy(gameObject);
        }
    }
}
