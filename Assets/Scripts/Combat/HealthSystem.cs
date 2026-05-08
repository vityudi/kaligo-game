using System;
using UnityEngine;

namespace Kaligo.Combat
{
    public class HealthSystem : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private float blockDamageReduction; // 0 = no reduction, 1 = full block

        public void SetBlockReduction(float reduction) => blockDamageReduction = Mathf.Clamp01(reduction);

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            amount *= (1f - blockDamageReduction);
            if (amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth == 0f)
                OnDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
