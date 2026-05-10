using UnityEngine;
using Kaligo.Combat;
using Kaligo.Services;

namespace Kaligo.Characters
{
    /// <summary>
    /// Applies level-based stat scaling to the player's HealthSystem and ManaSystem.
    /// Listens to IProgressionService.OnLevelUp to update stats in real time.
    /// Also exposes DamageMultiplier so HitboxController can apply it to outgoing damage.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(ManaSystem))]
    public class PlayerStats : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float baseMaxHp   = 100f;
        [SerializeField] private float baseMaxMana = 100f;

        [Header("Per-Level Scaling")]
        [SerializeField] private float hpPerLevel   = 15f;
        [SerializeField] private float manaPerLevel = 10f;
        [Tooltip("Multiplicative damage bonus added per level (0.05 = +5%/level).")]
        [SerializeField] private float damagePerLevel = 0.05f;

        public float DamageMultiplier { get; private set; } = 1f;

        public event System.Action<int> OnLevelUp;

        private HealthSystem health;
        private ManaSystem   mana;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
            mana   = GetComponent<ManaSystem>();
        }

        private void Start()
        {
            if (GameServices.Progression == null) return;

            ApplyStatsForLevel(GameServices.Progression.Level);
            GameServices.Progression.OnLevelUp += HandleLevelUp;
        }

        private void OnDestroy()
        {
            if (GameServices.Progression != null)
                GameServices.Progression.OnLevelUp -= HandleLevelUp;
        }

        private void HandleLevelUp(int newLevel)
        {
            ApplyStatsForLevel(newLevel);
            OnLevelUp?.Invoke(newLevel);
        }

        private void ApplyStatsForLevel(int level)
        {
            float newMaxHp   = baseMaxHp   + hpPerLevel   * (level - 1);
            float newMaxMana = baseMaxMana + manaPerLevel  * (level - 1);
            DamageMultiplier = 1f + damagePerLevel * (level - 1);

            health.SetMaxHealth(newMaxHp);
            mana.SetMaxMana(newMaxMana);
        }

        // Static accessor so HitboxController can look it up without a hard reference
        public static PlayerStats Instance { get; private set; }

        private void OnEnable()  { if (Instance == null) Instance = this; }
        private void OnDisable() { if (Instance == this) Instance = null; }
    }
}
