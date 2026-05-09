using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kaligo.Skills;

namespace Kaligo.UI
{
    /// <summary>
    /// Visual hotbar for all skill slots.
    /// Layout (left to right): [LMB] [RMB]  gap  [1] [2] [3] [4]
    /// Reads cooldown each frame from SkillExecutor and updates the radial overlay.
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
        }

        [SerializeField] private SlotView[]    slots;
        [SerializeField] private SkillBar      skillBar;
        [SerializeField] private SkillExecutor executor;

        private void Update()
        {
            if (slots == null) return;
            foreach (var slot in slots)
                RefreshSlot(slot);
        }

        private void RefreshSlot(SlotView slot)
        {
            if (slot == null) return;

            SkillData skill = skillBar != null ? skillBar.GetSkill(slot.binding) : null;

            if (slot.iconImage != null)
            {
                bool hasIcon = skill != null && skill.icon != null;
                slot.iconImage.sprite  = hasIcon ? skill.icon : null;
                slot.iconImage.color   = hasIcon ? Color.white : new Color(1f, 1f, 1f, 0.12f);
                slot.iconImage.enabled = true;
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
    }
}
