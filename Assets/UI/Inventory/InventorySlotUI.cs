using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Kaligo.Database;
using Kaligo.Items;

namespace Kaligo.UI
{
    /// <summary>
    /// One slot in the bag grid.
    ///
    /// Displays an item icon, quantity, and a coloured rarity border.
    /// Implements drag-and-drop handlers so the player can drag items
    /// from the bag into equipment slots.
    ///
    /// InventoryUI creates and populates these at runtime — no prefab needed,
    /// though you can also design one in the editor and assign it to
    /// InventoryUI.slotPrefab for more control over the visual layout.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InventorySlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ── References (assigned by InventoryUI) ──────────────────────────────

        [HideInInspector] public InventoryUI inventoryUI;
        [HideInInspector] public InventoryItemRow row;
        [HideInInspector] public ItemData          data;

        // ── Sub-elements (created procedurally) ───────────────────────────────

        private Image        _background;
        private Image        _icon;
        private Image        _rarityBorder;
        private TextMeshProUGUI _quantityLabel;

        // Drag state
        private static GameObject _dragGhost;
        private static InventorySlotUI _dragSource;

        // ── Initialisation ────────────────────────────────────────────────────

        private void Awake()
        {
            _background = GetComponent<Image>();
            _background.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // Rarity border (full-size image behind icon)
            var borderGO = new GameObject("RarityBorder", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(transform, false);
            var rt = borderGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _rarityBorder = borderGO.GetComponent<Image>();
            _rarityBorder.color = Color.clear;

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(transform, false);
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.1f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            _icon = iconGO.GetComponent<Image>();
            _icon.color = Color.clear;

            // Quantity label (bottom-right corner)
            var qGO = new GameObject("QuantityLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            qGO.transform.SetParent(transform, false);
            var qRT = qGO.GetComponent<RectTransform>();
            qRT.anchorMin = new Vector2(0.5f, 0f);
            qRT.anchorMax = new Vector2(1f, 0.45f);
            qRT.offsetMin = Vector2.zero;
            qRT.offsetMax = Vector2.zero;
            _quantityLabel = qGO.GetComponent<TextMeshProUGUI>();
            _quantityLabel.fontSize = 11f;
            _quantityLabel.alignment = TextAlignmentOptions.BottomRight;
            _quantityLabel.color = Color.white;
        }

        // ── Data binding ──────────────────────────────────────────────────────

        public void Populate(InventoryItemRow itemRow, ItemData itemData)
        {
            row  = itemRow;
            data = itemData;

            if (data == null)
            {
                // ItemData reference broken — show a minimal fallback so items aren't invisible
                if (itemRow != null)
                {
                    _rarityBorder.color = Color.clear;
                    _icon.color         = Color.clear;
                    _quantityLabel.text = itemRow.Quantity > 1 ? $"{itemRow.ItemId}\nx{itemRow.Quantity}" : itemRow.ItemId;
                    _quantityLabel.fontSize  = 8f;
                    _quantityLabel.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    Clear();
                }
                return;
            }

            _rarityBorder.color     = data.RarityColor() * new Color(1, 1, 1, 0.5f);
            _icon.sprite            = data.icon;
            _icon.color             = data.icon != null ? Color.white : new Color(1, 1, 1, 0.15f);
            _quantityLabel.fontSize  = 11f;
            _quantityLabel.alignment = TextAlignmentOptions.BottomRight;
            _quantityLabel.text      = itemRow.Quantity > 1 ? itemRow.Quantity.ToString() : "";
        }

        public void Clear()
        {
            row  = null;
            data = null;
            _rarityBorder.color = Color.clear;
            _icon.color         = Color.clear;
            _quantityLabel.text = "";
        }

        // ── Drag ──────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (data == null) return;

            _dragSource = this;
            _dragGhost  = CreateGhost();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragGhost == null) return;
            _dragGhost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragGhost != null) { Destroy(_dragGhost); _dragGhost = null; }
            _dragSource = null;
        }

        // ── Click (double-click to equip) ─────────────────────────────────────

        private float _lastClickTime;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (data == null || !data.isEquippable) return;

            float now = Time.unscaledTime;
            if (now - _lastClickTime < 0.3f)
                inventoryUI?.TryEquip(row);

            _lastClickTime = now;
        }

        // ── Hover tooltip ─────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (data == null) return;
            inventoryUI?.ShowTooltip(data, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            inventoryUI?.HideTooltip();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private GameObject CreateGhost()
        {
            var ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(inventoryUI.transform.root, false);  // top of canvas
            var rt = ghost.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 48f);

            var img = ghost.GetComponent<Image>();
            img.sprite = data.icon;
            img.color  = data.icon != null ? new Color(1, 1, 1, 0.7f) : data.RarityColor() * new Color(1, 1, 1, 0.7f);
            img.raycastTarget = false;

            ghost.transform.position = transform.position;
            return ghost;
        }

        // ── Static helpers for drop targets ───────────────────────────────────

        public static InventorySlotUI DragSource => _dragSource;
    }
}
