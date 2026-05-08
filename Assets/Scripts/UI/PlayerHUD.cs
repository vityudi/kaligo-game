using UnityEngine;
using UnityEngine.UI;
using Kaligo.Combat;

namespace Kaligo.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Image hpFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private HealthSystem  health;
        [SerializeField] private StaminaSystem stamina;

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
        }

        private void OnDestroy()
        {
            if (health  != null) health.OnHealthChanged   -= OnHealthChanged;
            if (stamina != null) stamina.OnStaminaChanged -= OnStaminaChanged;
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
    }
}
