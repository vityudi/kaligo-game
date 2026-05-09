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
    /// Layout (left to right): [LMB] [RMB]  gap  [1] [2] [3] [4]
    /// Reads cooldown each frame from SkillExecutor and updates the radial overlay.
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

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
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
    }
}
