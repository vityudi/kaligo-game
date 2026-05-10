using UnityEngine;
using Kaligo.Combat;
using Kaligo.Services;

namespace Kaligo.Characters
{
    /// <summary>
    /// Applies level-based stat scaling to the player's HealthSystem and ManaSystem.
    /// Listens to IProgressionService.OnLevelUp to update stats in real time.
    /// Also exposes DamageMultiplier so HitboxController can apply it to outgoing damage.
    ///
    /// Phase 6: RefreshStats() is called by EquipmentManager whenever the equipped
    /// items change, layering equipment bonuses on top of level-based values.
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

        /// <summary>
        /// Final damage multiplier = level scaling + equipment flat bonus.
        /// Read by HitboxController before applying outgoing damage.
        /// </summary>
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

        /// <summary>
        /// Re-applies current level stats + equipment bonuses.
        /// Called by EquipmentManager when the player's equipment changes.
        /// </summary>
        public void RefreshStats()
        {
            if (GameServices.Progression != null)
                ApplyStatsForLevel(GameServices.Progression.Level);
        }

        private void ApplyStatsForLevel(int level)
        {
            // Equipment bonuses (zero if EquipmentManager isn't in the scene)
            float equipHp     = EquipmentManager.Instance?.HpBonus     ?? 0f;
            float equipMana   = EquipmentManager.Instance?.ManaBonus   ?? 0f;
            float equipDamage = EquipmentManager.Instance?.DamageBonus ?? 0f;

            float newMaxHp   = baseMaxHp   + hpPerLevel   * (level - 1) + equipHp;
            float newMaxMana = baseMaxMana + manaPerLevel  * (level - 1) + equipMana;
            DamageMultiplier = 1f + damagePerLevel * (level - 1) + equipDamage;

            health.SetMaxHealth(newMaxHp);
            mana.SetMaxMana(newMaxMana);
        }

        // Static accessor so HitboxController and EquipmentManager can look it up
        public static PlayerStats Instance { get; private set; }

        private void OnEnable()  { if (Instance == null) Instance = this; }
        private void OnDisable() { if (Instance == this) Instance = null; }
    }
}
