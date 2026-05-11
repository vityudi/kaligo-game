using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kaligo.Skills;
using InputSystem = UnityEngine.InputSystem;

namespace Kaligo.UI
{
    /// <summary>
    /// Visual hotbar for all skill slots.
    /// Layout (left to right): [LMB] [RMB]  gap  [Space] [1] [2] [3] [4] [5]
    /// Reads cooldown each frame from SkillExecutor and updates the radial overlay.
    /// Self-builds its slot GameObjects at runtime when none are pre-wired in the Inspector.
    /// In UI mode (Alt), performs direct mouse-position hover detection for the tooltip
    /// so it works regardless of EventSystem state.
    /// </summary>
    public class SkillBarHUD : MonoBehaviour
    {
        [Serializable]
        public class SlotView
        {
            [Tooltip("Input binding this slot displays.")]
            public InputBinding binding;

            [Tooltip("Skill icon image; dimmed when no skill is assigned.")]
            public Image iconImage;

            [Tooltip("Radial fill image (Radial360, fill origin Top). Overlays icon during cooldown.")]
            public Image cooldownOverlay;

            [Tooltip("Key hint label (e.g. LMB, 1, 2).")]
            public TextMeshProUGUI keyLabel;

            [Tooltip("Mana cost label; hidden when 0.")]
            public TextMeshProUGUI manaLabel;

            // Resolved on first use — parent RectTransform of iconImage is the slot root.
            public RectTransform SlotRoot => iconImage != null
                ? iconImage.transform.parent as RectTransform : null;
        }

        [SerializeField] private SlotView[]    slots;
        [SerializeField] private SkillBar      skillBar;
        [SerializeField] private SkillExecutor executor;

        private Canvas canvas;

        // Slot layout constants
        private const float SlotSize    = 52f;
        private const float SlotPad     = 4f;
        private const float GroupGap    = 16f;   // gap between RMB group and key group
        private const float BarY        = 16f;   // distance from bottom edge

        // Key label strings for each binding (same order as InputBinding enum)
        private static readonly string[] BindingLabels =
        {
            "LMB", "RMB", "SPC", "1", "2", "3", "4", "5"
        };

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();

            // Auto-find SkillExecutor + SkillBar on the Player if not wired.
            if (executor == null || skillBar == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    if (executor == null) executor = player.GetComponentInChildren<SkillExecutor>();
                    if (skillBar == null) skillBar  = player.GetComponentInChildren<SkillBar>();
                }
            }

            // Self-build slots if none were pre-wired in the Inspector.
            if (slots == null || slots.Length == 0)
                BuildSlotsUI();
        }

        private void Update()
        {
            if (slots == null) return;
            foreach (var slot in slots)
                RefreshSlot(slot);

            UpdateTooltip();
        }

        private void RefreshSlot(SlotView slot)
        {
            if (slot == null) return;

            SkillData skill = skillBar != null ? skillBar.GetSkill(slot.binding) : null;

            if (slot.iconImage != null)
            {
                bool hasIcon = skill != null && skill.icon != null;
                slot.iconImage.sprite  = hasIcon ? skill.icon : null;
                slot.iconImage.color   = Color.white;
                slot.iconImage.enabled = hasIcon;
            }

            if (slot.cooldownOverlay != null)
            {
                float fraction = (executor != null && skill != null)
                    ? executor.GetCooldownFraction(skill) : 0f;
                slot.cooldownOverlay.fillAmount = fraction;
                slot.cooldownOverlay.enabled    = fraction > 0f;
            }

            if (slot.manaLabel != null)
            {
                if (skill != null && skill.manaCost > 0f)
                {
                    slot.manaLabel.text    = ((int)skill.manaCost).ToString();
                    slot.manaLabel.enabled = true;
                }
                else
                {
                    slot.manaLabel.enabled = false;
                }
            }
        }

        private void UpdateTooltip()
        {
            var tooltip = SkillTooltipUI.Instance;
            if (tooltip == null) return;

            bool inUIMode = Kaligo.CursorController.Instance != null
                         && Kaligo.CursorController.Instance.IsUIMode;

            if (!inUIMode)
            {
                tooltip.Hide();
                return;
            }

            Vector2 mouseScreen = InputSystem.Mouse.current != null
                ? InputSystem.Mouse.current.position.ReadValue()
                : Vector2.zero;

            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera : null;

            foreach (var slot in slots)
            {
                RectTransform root = slot?.SlotRoot;
                if (root == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(root, mouseScreen, uiCamera))
                {
                    SkillData skill = skillBar != null ? skillBar.GetSkill(slot.binding) : null;
                    tooltip.Show(skill, root);
                    return;
                }
            }

            tooltip.Hide();
        }

        // ── Self-building UI ──────────────────────────────────────────────────

        /// <summary>
        /// Builds the full skill hotbar from code.
        /// Layout: [LMB][RMB]  [SPC][1][2][3][4][5]  centred at screen bottom.
        /// Called automatically in Awake when no slots are pre-wired in the Inspector.
        /// </summary>
        private void BuildSlotsUI()
        {
            // All 8 bindings in display order
            var bindings = new InputBinding[]
            {
                InputBinding.LMB,
                InputBinding.RMB,
                InputBinding.Space,
                InputBinding.Key1,
                InputBinding.Key2,
                InputBinding.Key3,
                InputBinding.Key4,
                InputBinding.Key5,
            };

            slots = new SlotView[bindings.Length];

            // Total bar width calculation:
            //   8 slots × SlotSize + 7 gaps × SlotPad + 1 GroupGap (between RMB and SPC)
            float totalWidth = bindings.Length * SlotSize
                             + (bindings.Length - 1) * SlotPad
                             + GroupGap;  // extra gap between index 1 (RMB) and index 2 (Space)

            // Anchor the bar container to bottom-centre
            var barGO = new GameObject("SkillBar");
            barGO.transform.SetParent(transform, false);
            var barRt = barGO.AddComponent<RectTransform>();
            barRt.anchorMin        = new Vector2(0.5f, 0f);
            barRt.anchorMax        = new Vector2(0.5f, 0f);
            barRt.pivot            = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, BarY);
            barRt.sizeDelta        = new Vector2(totalWidth, SlotSize);

            // Dark backing strip
            var barBg   = barGO.AddComponent<Image>();
            barBg.color = new Color(0f, 0f, 0f, 0f);   // invisible container; slots draw their own bg

            // Build each slot
            float xCursor = 0f;
            for (int i = 0; i < bindings.Length; i++)
            {
                // Extra gap after RMB (index 1) before Space
                if (i == 2) xCursor += GroupGap;

                slots[i] = BuildSlot(barGO.transform, bindings[i], BindingLabels[i], xCursor);
                xCursor += SlotSize + SlotPad;
            }
        }

        /// <summary>Creates one slot panel and returns its filled SlotView.</summary>
        private SlotView BuildSlot(Transform parent, InputBinding binding, string keyText, float xOffset)
        {
            // ── Root panel ────────────────────────────────────────────────────
            var slotGO = new GameObject($"Slot_{keyText}");
            slotGO.transform.SetParent(parent, false);
            var slotRt = slotGO.AddComponent<RectTransform>();
            slotRt.anchorMin        = new Vector2(0f, 0f);
            slotRt.anchorMax        = new Vector2(0f, 0f);
            slotRt.pivot            = new Vector2(0f, 0f);
            slotRt.anchoredPosition = new Vector2(xOffset, 0f);
            slotRt.sizeDelta        = new Vector2(SlotSize, SlotSize);

            // Dark background
            var bg   = slotGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            // ── Icon image ────────────────────────────────────────────────────
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRt = iconGO.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.1f);
            iconRt.anchorMax = new Vector2(0.9f, 0.9f);
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.enabled = false;   // hidden until a skill is assigned

            // ── Cooldown radial overlay ───────────────────────────────────────
            var cdGO = new GameObject("Cooldown");
            cdGO.transform.SetParent(slotGO.transform, false);
            var cdRt = cdGO.AddComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero;
            cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = cdRt.offsetMax = Vector2.zero;
            var cdImg = cdGO.AddComponent<Image>();
            cdImg.color      = new Color(0f, 0f, 0f, 0.65f);
            cdImg.type       = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillOrigin = (int)Image.Origin360.Top;
            cdImg.fillAmount = 0f;
            cdImg.enabled    = false;

            // ── Key label (bottom-left) ───────────────────────────────────────
            var keyGO = new GameObject("KeyLabel");
            keyGO.transform.SetParent(slotGO.transform, false);
            var keyRt = keyGO.AddComponent<RectTransform>();
            keyRt.anchorMin        = new Vector2(0f, 0f);
            keyRt.anchorMax        = new Vector2(1f, 0.35f);
            keyRt.offsetMin        = new Vector2(2f, 2f);
            keyRt.offsetMax        = new Vector2(-2f, 0f);
            var keyTMP = keyGO.AddComponent<TextMeshProUGUI>();
            keyTMP.text      = keyText;
            keyTMP.fontSize  = 9f;
            keyTMP.color     = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            keyTMP.alignment = TextAlignmentOptions.BottomLeft;

            // ── Mana cost label (top-right) ───────────────────────────────────
            var manaGO = new GameObject("ManaLabel");
            manaGO.transform.SetParent(slotGO.transform, false);
            var manaRt = manaGO.AddComponent<RectTransform>();
            manaRt.anchorMin        = new Vector2(0f, 0.65f);
            manaRt.anchorMax        = new Vector2(1f, 1f);
            manaRt.offsetMin        = new Vector2(2f, 0f);
            manaRt.offsetMax        = new Vector2(-2f, -2f);
            var manaTMP = manaGO.AddComponent<TextMeshProUGUI>();
            manaTMP.text      = "";
            manaTMP.fontSize  = 9f;
            manaTMP.color     = new Color(0.4f, 0.6f, 1f, 1f);
            manaTMP.alignment = TextAlignmentOptions.TopRight;
            manaTMP.enabled   = false;

            return new SlotView
            {
                binding         = binding,
                iconImage       = iconImg,
                cooldownOverlay = cdImg,
                keyLabel        = keyTMP,
                manaLabel       = manaTMP,
            };
        }
    }
}
