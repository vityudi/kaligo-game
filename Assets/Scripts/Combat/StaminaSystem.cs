using System;
using UnityEngine;

namespace Kaligo.Combat
{
    public class StaminaSystem : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float regenRate = 20f;
        [SerializeField] private float regenDelay = 1f;

        public float MaxStamina     => maxStamina;
        public float CurrentStamina { get; private set; }

        public event Action<float, float> OnStaminaChanged; // (current, max)

        private float lastSpendTime = -999f;

        private void Awake() => CurrentStamina = maxStamina;

        private void Update()
        {
            if (Time.time - lastSpendTime > regenDelay && CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenRate * Time.deltaTime);
                OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            }
        }

        public bool TrySpend(float amount)
        {
            if (CurrentStamina < amount) return false;
            CurrentStamina -= amount;
            lastSpendTime = Time.time;
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            return true;
        }
    }
}
