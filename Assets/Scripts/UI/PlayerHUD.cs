using UnityEngine;
using UnityEngine.UI;
using Kaligo.Combat;

namespace Kaligo.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private HealthSystem  health;
        [SerializeField] private StaminaSystem stamina;
        [SerializeField] private ManaSystem    mana;

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
        }

        private void OnDestroy()
        {
            if (health  != null) health.OnHealthChanged   -= OnHealthChanged;
            if (stamina != null) stamina.OnStaminaChanged -= OnStaminaChanged;
            if (mana    != null) mana.OnManaChanged       -= OnManaChanged;
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
    }
}
