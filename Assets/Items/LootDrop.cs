using Kaligo.Combat;
using Kaligo.Mobs;
using UnityEngine;

namespace Kaligo.Items
{
    /// <summary>
    /// Attach to any mob that should drop loot on death.
    /// MobFactory calls SetLootTable() to inject the table without reflection.
    /// On death, rolls the LootTable and adds a LootContainer so the player
    /// can loot the body with [F].
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class LootDrop : MonoBehaviour
    {
        [Header("Loot")]
        [SerializeField] private LootTable lootTable;

        /// <summary>Called by MobFactory to inject the loot table — no reflection needed.</summary>
        public void SetLootTable(LootTable table) => lootTable = table;

        private void Awake()
        {
            // Legacy EnemyAI manages its own loot — skip to avoid duplicates.
            if (GetComponent<EnemyAI>() != null) return;
            GetComponent<HealthSystem>().OnDeath += OnDeath;
        }

        private void OnDeath()
        {
            if (lootTable == null)
            {
                Debug.LogWarning($"[LootDrop] {name}: lootTable not assigned — no loot created.");
                return;
            }

            var drops = lootTable.Roll();
            if (drops.Count == 0) return;

            var container = gameObject.AddComponent<LootContainer>();
            container.Initialize(drops);
        }
    }
}
