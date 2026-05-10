using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Kaligo.Characters;
using Kaligo.Items;
using Kaligo.Services;
using Kaligo.Database;

namespace Kaligo.UI
{
    /// <summary>
    /// Inventory screen — press I (or the configured key) to toggle.
    ///
    /// Builds the bag grid and equipment panel entirely from code.
    /// The window is draggable by its title bar.
    ///
    /// ── Scene setup ─────────────────────────────────────────────────────────
    ///   • This component should sit on a child GameObject inside a Canvas.
    ///   • Assign the ItemRegistry asset in the Inspector.
    ///   • Press Play → press I.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Registry")]
        [SerializeField] private ItemRegistry itemRegistry;

        [Header("Layout")]
        [SerializeField] private int   bagColumns   = 6;
        [SerializeField] private int   bagRows      = 5;
        [SerializeField] private float slotSize     = 56f;
        [SerializeField] private float slotPadding  = 4f;

        [Header("Input")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        // ── Runtime ───────────────────────────────────────────────────────────

        private GameObject   _root;
        private bool         _isOpen;

        private List<InventorySlotUI>                       _bagSlots  = new();
        private Dictionary<EquipmentSlot, EquipmentSlotUI> _equipSlots = new();

        private GameObject      _tooltip;
        private TextMeshProUGUI _tooltipText;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Stretch this RectTransform to fill the canvas so centre anchors
            // on child panels correctly resolve to screen centre.
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private void Start()
        {
            BuildUI();
            SetVisible(false);

            if (GameServices.Inventory != null)
                GameServices.Inventory.OnChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (GameServices.Inventory != null)
                GameServices.Inventory.OnChanged -= Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        // ── Toggle ────────────────────────────────────────────────────────────

        public void Toggle()
        {
            _isOpen = !_isOpen;
            SetVisible(_isOpen);
            if (_isOpen) Refresh();
        }

        private void SetVisible(bool v)
        {
            _isOpen = v;
            if (_root != null) _root.SetActive(v);
        }

        // ── Build UI ──────────────────────────────────────────────────────────

        private void BuildUI()
        {
            const float titleH  = 36f;
            const float eqW     = 216f;
            const float statsH  = 60f;
            const float pad     = 8f;

            float bw = bagColumns * (slotSize + slotPadding) + slotPadding;
            float bh = bagRows    * (slotSize + slotPadding) + slotPadding;

            float panelW = pad + bw + pad + eqW + pad;
            float panelH = titleH + pad + bh + pad + statsH + pad;

            // ── Root panel — centred on screen ────────────────────────────────
            _root = MakeBox(transform, "InventoryRoot",
                pivot:  new Vector2(0.5f, 0.5f),
                anchor: new Vector2(0.5f, 0.5f),
                pos:    Vector2.zero,
                size:   new Vector2(panelW, panelH));
            Img(_root, new Color(0.05f, 0.05f, 0.08f, 0.96f));

            // ── Title bar (stretches full width, draggable) ───────────────────
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(_root.transform, false);
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0f, 1f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.pivot     = new Vector2(0.5f, 1f);
            tbRT.offsetMin = new Vector2(0f, -titleH);
            tbRT.offsetMax = new Vector2(0f, 0f);
            titleBar.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // Drag handle — moves _root when the title bar is dragged
            var drag = titleBar.AddComponent<DraggableWindow>();
            drag.Target = _root.GetComponent<RectTransform>();

            // Title text
            var titleLbl = MakeLabel(titleBar.transform, "TitleText", "INVENTORY",
                Vector2.zero, Vector2.one, 14f, FontStyles.Bold,
                new Color(0.85f, 0.75f, 0.5f));

            // Close button (top-right corner of root)
            var closeGO = MakeBox(_root.transform, "CloseBtn",
                pivot:  new Vector2(1f, 1f),
                anchor: new Vector2(1f, 1f),
                pos:    Vector2.zero,
                size:   new Vector2(titleH, titleH));
            Img(closeGO, new Color(0.35f, 0.1f, 0.1f, 1f));
            MakeLabel(closeGO.transform, "X", "✕",
                Vector2.zero, Vector2.one, 14f, FontStyles.Bold, Color.white);
            closeGO.AddComponent<Button>().onClick.AddListener(() => SetVisible(false));

            // ── Bag panel ─────────────────────────────────────────────────────
            float bagX = pad;
            float bagY = -(titleH + pad);   // negative = down from top

            var bagPanel = MakeBox(_root.transform, "BagPanel",
                pivot:  new Vector2(0f, 1f),
                anchor: new Vector2(0f, 1f),
                pos:    new Vector2(bagX, bagY),
                size:   new Vector2(bw, bh));
            Img(bagPanel, new Color(0.07f, 0.07f, 0.10f, 1f));

            for (int i = 0; i < bagColumns * bagRows; i++)
            {
                int col = i % bagColumns;
                int row = i / bagColumns;
                float sx = slotPadding + col * (slotSize + slotPadding);
                float sy = -(slotPadding + row * (slotSize + slotPadding));

                var slotGO = MakeBox(bagPanel.transform, $"Slot_{i}",
                    pivot:  new Vector2(0f, 1f),
                    anchor: new Vector2(0f, 1f),
                    pos:    new Vector2(sx, sy),
                    size:   new Vector2(slotSize, slotSize));
                Img(slotGO, new Color(0.1f, 0.1f, 0.1f, 0.85f));

                var slot = slotGO.AddComponent<InventorySlotUI>();
                slot.inventoryUI = this;
                _bagSlots.Add(slot);
            }

            // ── Equipment panel ───────────────────────────────────────────────
            float eqX = pad + bw + pad;

            var eqPanel = MakeBox(_root.transform, "EquipPanel",
                pivot:  new Vector2(0f, 1f),
                anchor: new Vector2(0f, 1f),
                pos:    new Vector2(eqX, bagY),
                size:   new Vector2(eqW, bh));
            Img(eqPanel, new Color(0.07f, 0.07f, 0.10f, 1f));

            // Equipment slot positions relative to equipment panel top-left
            var slotPositions = new Dictionary<EquipmentSlot, Vector2>
            {
                { EquipmentSlot.Helmet,  new Vector2(80f,  -8f)   },
                { EquipmentSlot.Chest,   new Vector2(80f,  -72f)  },
                { EquipmentSlot.Weapon,  new Vector2(8f,   -72f)  },
                { EquipmentSlot.OffHand, new Vector2(152f, -72f)  },
                { EquipmentSlot.Legs,    new Vector2(80f,  -136f) },
                { EquipmentSlot.Boots,   new Vector2(80f,  -200f) },
                { EquipmentSlot.Ring1,   new Vector2(8f,   -200f) },
                { EquipmentSlot.Ring2,   new Vector2(152f, -200f) },
            };

            foreach (var kvp in slotPositions)
            {
                var eqSlotGO = MakeBox(eqPanel.transform, $"EqSlot_{kvp.Key}",
                    pivot:  new Vector2(0f, 1f),
                    anchor: new Vector2(0f, 1f),
                    pos:    kvp.Value,
                    size:   new Vector2(slotSize, slotSize));
                Img(eqSlotGO, new Color(0.08f, 0.08f, 0.12f, 0.9f));

                var slotUI = eqSlotGO.AddComponent<EquipmentSlotUI>();
                slotUI.inventoryUI = this;
                slotUI.SetSlot(kvp.Key);
                _equipSlots[kvp.Key] = slotUI;
            }

            // ── Stats preview (below equipment panel) ─────────────────────────
            float statsY = bagY - bh - pad;

            var statsGO = MakeBox(_root.transform, "StatsPreview",
                pivot:  new Vector2(0f, 1f),
                anchor: new Vector2(0f, 1f),
                pos:    new Vector2(eqX, statsY),
                size:   new Vector2(eqW, statsH));
            Img(statsGO, new Color(0.06f, 0.06f, 0.09f, 1f));

            var statsLbl = MakeLabel(statsGO.transform, "StatsLabel", "",
                Vector2.zero, Vector2.one, 10f, FontStyles.Normal,
                new Color(0.75f, 0.75f, 0.75f));
            statsLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
            var statsLblRT = statsLbl.GetComponent<RectTransform>();
            statsLblRT.offsetMin = new Vector2(6f, 4f);
            statsLblRT.offsetMax = new Vector2(-6f, -4f);

            // ── Tooltip (parented to canvas root so it renders on top) ─────────
            _tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            _tooltip.transform.SetParent(transform.root, false);
            var ttRT = _tooltip.GetComponent<RectTransform>();
            ttRT.anchorMin = ttRT.anchorMax = ttRT.pivot = Vector2.zero;
            ttRT.sizeDelta = new Vector2(200f, 110f);
            _tooltip.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
            _tooltip.GetComponent<Image>().raycastTarget = false;

            var ttTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            ttTextGO.transform.SetParent(_tooltip.transform, false);
            var ttTextRT = ttTextGO.GetComponent<RectTransform>();
            ttTextRT.anchorMin = Vector2.zero; ttTextRT.anchorMax = Vector2.one;
            ttTextRT.offsetMin = new Vector2(6f, 6f); ttTextRT.offsetMax = new Vector2(-6f, -6f);
            _tooltipText = ttTextGO.GetComponent<TextMeshProUGUI>();
            _tooltipText.fontSize     = 11f;
            _tooltipText.raycastTarget = false;

            _tooltip.SetActive(false);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (!_isOpen || GameServices.Inventory == null) return;

            var allRows        = GameServices.Inventory.GetAll();
            var bagRows        = new List<InventoryItemRow>();
            var equippedBySlot = new Dictionary<EquipmentSlot, (InventoryItemRow, ItemData)>();

            foreach (var row in allRows)
            {
                var data = itemRegistry?.Get(row.ItemId);
                if (string.IsNullOrEmpty(row.Slot))
                    bagRows.Add(row);
                else if (Enum.TryParse<EquipmentSlot>(row.Slot, out var slot))
                    equippedBySlot[slot] = (row, data);
            }

            for (int i = 0; i < _bagSlots.Count; i++)
            {
                if (i < bagRows.Count)
                    _bagSlots[i].Populate(bagRows[i], itemRegistry?.Get(bagRows[i].ItemId));
                else
                    _bagSlots[i].Clear();
            }

            foreach (var kvp in _equipSlots)
            {
                if (equippedBySlot.TryGetValue(kvp.Key, out var pair))
                    kvp.Value.Populate(pair.Item1, pair.Item2);
                else
                    kvp.Value.Populate(null, null);
            }

            RefreshStats();
        }

        private void RefreshStats()
        {
            var lbl = _root?.transform.Find("StatsPreview/StatsLabel");
            if (lbl == null) return;
            var tmp = lbl.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return;

            var eq   = EquipmentManager.Instance;
            var ps   = PlayerStats.Instance;
            if (ps == null) { tmp.text = ""; return; }

            tmp.text =
                $"HP bonus:    {Mathf.RoundToInt(eq?.HpBonus ?? 0):+0;-#;0}\n" +
                $"Mana bonus: {Mathf.RoundToInt(eq?.ManaBonus ?? 0):+0;-#;0}\n" +
                $"Dmg bonus:  {(ps.DamageMultiplier - 1f) * 100f:+0.0;-#.0;0}%";
        }

        // ── Equip / Unequip (called by slot UIs) ──────────────────────────────

        public void TryEquip(InventoryItemRow row)
        {
            if (row == null || GameServices.Inventory == null) return;
            var data = itemRegistry?.Get(row.ItemId);
            if (data == null || !data.isEquippable) return;
            GameServices.Inventory.Equip(row.Id, data.equipSlot.ToString());
            Refresh();
        }

        public void TryEquipRow(int rowId, EquipmentSlot slot)
        {
            if (GameServices.Inventory == null) return;
            GameServices.Inventory.Equip(rowId, slot.ToString());
            Refresh();
        }

        public void TryUnequip(EquipmentSlot slot)
        {
            if (GameServices.Inventory == null) return;
            GameServices.Inventory.Unequip(slot.ToString());
            Refresh();
        }

        // ── Tooltip ───────────────────────────────────────────────────────────

        public void ShowTooltip(ItemData data, Vector3 screenPos)
        {
            if (_tooltip == null || data == null) return;

            string stats = "";
            if (data.hpBonus         != 0) stats += $"\n+{data.hpBonus} HP";
            if (data.manaBonus       != 0) stats += $"\n+{data.manaBonus} Mana";
            if (data.flatDamageBonus != 0) stats += $"\n+{data.flatDamageBonus * 100f:0}% Damage";

            string slotStr = data.isEquippable ? $"\n[{data.equipSlot}]" : "";
            _tooltipText.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(data.RarityColor())}><b>{data.displayName}</b></color>" +
                slotStr +
                $"\n<size=90%><color=#aaaaaa>{data.description}</color></size>" +
                stats;

            // Position the tooltip near the cursor, clamped inside screen
            var rt = _tooltip.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;   // reset; we'll use world pos
            _tooltip.transform.position = screenPos + new Vector3(12f, -12f, 0f);
            _tooltip.SetActive(true);
        }

        public void HideTooltip()
        {
            if (_tooltip != null) _tooltip.SetActive(false);
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        /// <summary>Creates a GO with a RectTransform using anchoredPosition + sizeDelta.</summary>
        private static GameObject MakeBox(Transform parent, string name,
            Vector2 pivot, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.pivot              = pivot;
            rt.anchorMin          = anchor;
            rt.anchorMax          = anchor;
            rt.anchoredPosition   = pos;
            rt.sizeDelta          = size;
            return go;
        }

        private static void Img(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = color;
        }

        /// <summary>Creates a TextMeshProUGUI label that stretches between anchorMin/Max.</summary>
        private static GameObject MakeLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return go;
        }
    }

    // ── Draggable window helper ────────────────────────────────────────────────

    /// <summary>
    /// Attach to a title bar; set Target to the panel RectTransform to move.
    /// Dragging the title bar repositions the panel.
    /// </summary>
    internal class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform Target;
        private Vector2 _offset;

        public void OnBeginDrag(PointerEventData e)
        {
            var parent = Target.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, e.position, e.pressEventCamera, out var local);
            _offset = Target.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData e)
        {
            var parent = Target.parent as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, e.position, e.pressEventCamera, out var local))
                Target.anchoredPosition = local + _offset;
        }
    }
}
