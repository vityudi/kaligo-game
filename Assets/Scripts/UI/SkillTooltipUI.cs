using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kaligo.Skills;

namespace Kaligo.UI
{
    /// <summary>
    /// Singleton tooltip panel. Call Show/Hide from SkillSlotHover components.
    /// Positions itself above the hovered slot, clamped within the canvas.
    /// </summary>
    public class SkillTooltipUI : MonoBehaviour
    {
        public static SkillTooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform    panel;
        [SerializeField] private TextMeshProUGUI  nameText;
        [SerializeField] private TextMeshProUGUI  descriptionText;
        [SerializeField] private TextMeshProUGUI  statsText;

        private RectTransform canvasRT;

        private void Awake()
        {
            Instance = this;
            canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            panel.gameObject.SetActive(false);
        }

        public void Show(SkillData skill, RectTransform slotRT)
        {
            if (skill == null) { Hide(); return; }

            nameText.text        = skill.skillName;
            descriptionText.text = string.IsNullOrEmpty(skill.description) ? "" : skill.description;

            var stats = "";
            if (skill.manaCost    > 0f) stats += $"Mana  {(int)skill.manaCost}";
            if (skill.staminaCost > 0f) stats += (stats.Length > 0 ? "   " : "") + $"Stamina  {(int)skill.staminaCost}";
            if (skill.cooldown    > 0f) stats += (stats.Length > 0 ? "   " : "") + $"CD  {skill.cooldown}s";
            statsText.text = stats;

            panel.gameObject.SetActive(true);

            // Position: above the slot
            Vector3[] corners = new Vector3[4];
            slotRT.GetWorldCorners(corners);
            Vector2 slotTop = RectTransformUtility.WorldToScreenPoint(null, corners[1]); // top-left world corner

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, slotTop, null, out Vector2 localPos);

            // Offset upward by panel half-height + small gap
            localPos.y += panel.rect.height * 0.5f + 8f;
            localPos.x += slotRT.rect.width * 0.5f; // horizontal center of slot

            // Clamp so tooltip doesn't leave canvas
            float hw = panel.rect.width  * 0.5f;
            float hh = panel.rect.height * 0.5f;
            float cw = canvasRT.rect.width  * 0.5f;
            float ch = canvasRT.rect.height * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -cw + hw, cw - hw);
            localPos.y = Mathf.Clamp(localPos.y, -ch + hh, ch - hh);

            panel.anchoredPosition = localPos;
        }

        public void Hide()
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }
    }
}
