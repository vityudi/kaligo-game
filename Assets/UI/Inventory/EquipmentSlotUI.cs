using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Kaligo.Database;
using Kaligo.Items;
using Kaligo.Services;

namespace Kaligo.UI
{
    /// <summary>
    /// One equipment slot on the character panel (Weapon, Chest, etc.).
    ///
    /// Displays the equipped item's icon with a rarity border.
    /// • Drop a bag item here → equips it.
    /// • Right-click → unequips.
    /// • Double-click → unequips (same as right-click).
    ///
    /// InventoryUI creates and positions these slots at runtime.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class EquipmentSlotUI : MonoBehaviour,
        IDropHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        // ── References ────────────────────────────────────────────────────────

        [HideInInspector] public InventoryUI   inventoryUI;
        [HideInInspector] public EquipmentSlot slot;

        // ── Sub-elements ──────────────────────────────────────────────────────

        private Image           _background;
        private Image           _icon;
        private Image           _rarityBorder;
        private TextMeshProUGUI _slotLabel;

        // Currently displayed row
        private InventoryItemRow _row;
        private ItemData         _data;

        // ── Initialisation ────────────────────────────────────────────────────

        private void Awake()
        {
            _background       = GetComponent<Image>();
            _background.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            // Rarity border
            var borderGO = new GameObject("RarityBorder", typeof(RectTransform), typeof(Image));
            borderGO.transform.SetParent(transform, false);
            var bRT = borderGO.GetComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
            bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
            _rarityBorder = borderGO.GetComponent<Image>();
            _rarityBorder.color = Color.clear;

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(transform, false);
            var iRT = iconGO.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.1f, 0.2f);
            iRT.anchorMax = new Vector2(0.9f, 0.9f);
            iRT.offsetMin = Vector2.zero; iRT.offsetMax = Vector2.zero;
            _icon = iconGO.GetComponent<Image>();
            _icon.color = Color.clear;
            _icon.raycastTarget = false;

            // Slot label (bottom)
            var labelGO = new GameObject("SlotLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(transform, false);
            var lRT = labelGO.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 0f);
            lRT.anchorMax = new Vector2(1f, 0.25f);
            lRT.offsetMin = Vector2.zero; lRT.offsetMax = Vector2.zero;
            _slotLabel = labelGO.GetComponent<TextMeshProUGUI>();
            _slotLabel.fontSize  = 9f;
            _slotLabel.alignment = TextAlignmentOptions.Center;
            _slotLabel.color     = new Color(0.6f, 0.6f, 0.6f);
            _slotLabel.raycastTarget = false;
        }

        // ── Data binding ──────────────────────────────────────────────────────

        public void SetSlot(EquipmentSlot equipSlot)
        {
            slot = equipSlot;
            _slotLabel.text = equipSlot.ToString();
        }

        public void Populate(InventoryItemRow itemRow, ItemData itemData)
        {
            _row  = itemRow;
            _data = itemData;

            if (itemData == null) { ClearIcon(); return; }

            _rarityBorder.color = itemData.RarityColor() * new Color(1, 1, 1, 0.6f);
            _icon.sprite = itemData.icon;
            _icon.color  = itemData.icon != null ? Color.white : new Color(1, 1, 1, 0.2f);
        }

        private void ClearIcon()
        {
            _row  = null;
            _data = null;
            _rarityBorder.color = Color.clear;
            _icon.color         = Color.clear;
        }

        // ── Drop ──────────────────────────────────────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            var source = InventorySlotUI.DragSource;
            if (source?.row == null || source?.data == null) return;
            if (!source.data.isEquippable) return;
            if (source.data.equipSlot != slot) return;

            inventoryUI?.TryEquipRow(source.row.Id, slot);
        }

        // ── Click ─────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_row == null) return;

            bool rightClick   = eventData.button == PointerEventData.InputButton.Right;
            bool doubleClick  = eventData.clickCount >= 2;

            if (rightClick || doubleClick)
                inventoryUI?.TryUnequip(slot);
        }

        // ── Tooltip ───────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_data != null)
                inventoryUI?.ShowTooltip(_data, transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            inventoryUI?.HideTooltip();
        }
    }
}
