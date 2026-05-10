using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kaligo.Combat;
using Kaligo.Services;
using Kaligo.Services.Local;

namespace Kaligo.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Combat Bars")]
        [SerializeField] private Image hpFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private HealthSystem  health;
        [SerializeField] private StaminaSystem stamina;
        [SerializeField] private ManaSystem    mana;

        [Header("Progression")]
        [SerializeField] private Image    xpFill;
        [SerializeField] private TextMeshProUGUI levelLabel;

        private void Start()
        {
            if (health != null)
            {
                health.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(health.CurrentHealth, health.MaxHealth);
            }

            if (stamina != null)
            {
                stamina.OnStaminaChanged += OnStaminaChanged;
                OnStaminaChanged(stamina.CurrentStamina, stamina.MaxStamina);
            }

            if (mana != null)
            {
                mana.OnManaChanged += OnManaChanged;
                OnManaChanged(mana.CurrentMana, mana.MaxMana);
            }

            if (GameServices.Progression != null)
            {
                GameServices.Progression.OnXPChanged += OnXPChanged;
                GameServices.Progression.OnLevelUp   += OnLevelUp;
                RefreshXP(GameServices.Progression.XP, GameServices.Progression.Level);
            }
        }

        private void OnDestroy()
        {
            if (health  != null) health.OnHealthChanged   -= OnHealthChanged;
            if (stamina != null) stamina.OnStaminaChanged -= OnStaminaChanged;
            if (mana    != null) mana.OnManaChanged       -= OnManaChanged;

            if (GameServices.Progression != null)
            {
                GameServices.Progression.OnXPChanged -= OnXPChanged;
                GameServices.Progression.OnLevelUp   -= OnLevelUp;
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (hpFill != null)
                hpFill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void OnStaminaChanged(float current, float max)
        {
            if (staminaFill != null)
                staminaFill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void OnManaChanged(float current, float max)
        {
            if (manaFill != null)
                manaFill.fillAmount = max > 0f ? current / max : 0f;
        }

        private void OnXPChanged(int totalXp)
        {
            RefreshXP(totalXp, GameServices.Progression.Level);
        }

        private void OnLevelUp(int newLevel)
        {
            RefreshXP(GameServices.Progression.XP, newLevel);
        }

        private void RefreshXP(int xp, int level)
        {
            if (xpFill != null)
                xpFill.fillAmount = XPTable.LevelProgress(xp);

            if (levelLabel != null)
                levelLabel.text = $"Lv {level}";
        }
    }
}
