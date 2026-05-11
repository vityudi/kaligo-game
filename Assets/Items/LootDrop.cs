using Kaligo.Combat;
using Kaligo.Mobs;
using UnityEngine;

namespace Kaligo.Items
{
    /// <summary>
    /// Attach to any enemy or mob that should drop loot on death.
    /// On death: rolls the LootTable, then adds a LootContainer component to
    /// this GameObject so the player can loot the body with [F].
    ///
    /// Note: EnemyAI (legacy) handles its own loot container — we skip for those.
    /// New MobBrain-based mobs use this component directly; MobFactory injects
    /// the loot table from the MobDefinition.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class LootDrop : MonoBehaviour
    {
        [Header("Loot")]
        [Tooltip("What this enemy can drop. Leave null for no drops.")]
        [SerializeField] private LootTable lootTable;

        private void Awake()
        {
            // Legacy EnemyAI handles its own loot container — skip to avoid duplicates.
            // New MobBrain mobs do NOT self-manage loot; this component handles it.
            if (GetComponent<EnemyAI>() != null) return;
            GetComponent<HealthSystem>().OnDeath += OnDeath;
        }

        private void OnDeath()
        {
            if (lootTable == null)
            {
                Debug.LogWarning($"[LootDrop] {name}: lootTable is not assigned — no loot container created.");
                return;
            }

            var drops = lootTable.Roll();
            if (drops.Count == 0)
            {
                Debug.Log($"[LootDrop] {name}: loot roll returned nothing (dropChance or empty entries).");
                return;
            }

            Debug.Log($"[LootDrop] {name}: creating LootContainer with {drops.Count} item(s).");
            var container = gameObject.AddComponent<LootContainer>();
            container.Initialize(drops);
        }
    }
}
