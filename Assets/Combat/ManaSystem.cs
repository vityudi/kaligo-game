using System;
using UnityEngine;

namespace Kaligo.Combat
{
    public class ManaSystem : MonoBehaviour
    {
        [SerializeField] private float maxMana    = 100f;
        [SerializeField] private float regenRate  = 5f;
        [Tooltip("Seconds after spending before regeneration resumes.")]
        [SerializeField] private float regenDelay = 2f;

        public float MaxMana     => maxMana;
        public float CurrentMana { get; private set; }

        public event Action<float, float> OnManaChanged; // (current, max)

        private float lastSpendTime = -99f;

        private void Awake() => CurrentMana = maxMana;

        private void Update()
        {
            if (CurrentMana < maxMana && Time.time - lastSpendTime > regenDelay)
            {
                CurrentMana = Mathf.Min(maxMana, CurrentMana + regenRate * Time.deltaTime);
                OnManaChanged?.Invoke(CurrentMana, maxMana);
            }
        }

        public bool TrySpend(float amount)
        {
            if (CurrentMana < amount) return false;
            CurrentMana  -= amount;
            lastSpendTime = Time.time;
            OnManaChanged?.Invoke(CurrentMana, maxMana);
            return true;
        }

        public void Restore(float amount)
        {
            CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        }
    }
}
