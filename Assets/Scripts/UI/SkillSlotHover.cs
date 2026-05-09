using UnityEngine;
using UnityEngine.EventSystems;
using Kaligo.Skills;

namespace Kaligo.UI
{
    /// <summary>
    /// Attach to each skill slot UI GameObject.
    /// Shows/hides the SkillTooltipUI singleton on pointer enter/exit.
    /// </summary>
    public class SkillSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public SkillBar      skillBar;
        public InputBinding  binding;

        private RectTransform slotRT;

        private void Awake() => slotRT = GetComponent<RectTransform>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            var skill = skillBar != null ? skillBar.GetSkill(binding) : null;
            SkillTooltipUI.Instance?.Show(skill, slotRT);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SkillTooltipUI.Instance?.Hide();
        }
    }
}
