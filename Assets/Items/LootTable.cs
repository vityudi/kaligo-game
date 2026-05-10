using System.Collections.Generic;
using UnityEngine;

namespace Kaligo.Items
{
    /// <summary>
    /// One entry in a LootTable: an item, its relative weight, and how many can drop.
    /// </summary>
    [System.Serializable]
    public class LootEntry
    {
        [Tooltip("The item that can drop (drag asset here).")]
        public ItemData item;

        [Tooltip("Fallback: itemId string used to look up the item via ItemRegistry at runtime " +
                 "if the direct reference above is null. Must match ItemData.itemId exactly.")]
        public string itemId;

        [Tooltip("Relative weight for this entry. Higher = more likely relative to other entries.")]
        [Min(0.01f)]
        public float weight = 1f;

        [Tooltip("Minimum quantity dropped if this entry is selected.")]
        [Min(1)]
        public int minQuantity = 1;

        [Tooltip("Maximum quantity dropped if this entry is selected.")]
        [Min(1)]
        public int maxQuantity = 1;

        /// <summary>Resolves the item — direct reference first, then ItemRegistry fallback.</summary>
        public ItemData ResolveItem()
        {
            if (item != null) return item;
            if (!string.IsNullOrEmpty(itemId) && ItemRegistry.Instance != null)
                return ItemRegistry.Instance.Get(itemId);
            return null;
        }
    }

    /// <summary>
    /// Defines what an enemy (or chest) can drop when killed/opened.
    /// Create via Assets → Create → Kaligo → LootTable.
    ///
    /// Each kill performs <see cref="rollCount"/> independent rolls.
    /// Each roll first checks <see cref="dropChance"/>, then picks a
    /// weighted-random entry from <see cref="entries"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Kaligo/LootTable", fileName = "New LootTable")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("Probability (0–1) that anything drops at all per roll.")]
        [Range(0f, 1f)]
        public float dropChance = 1f;

        [Tooltip("How many independent loot rolls to make on kill.")]
        [Min(1)]
        public int rollCount = 1;

        [Tooltip("Items that can drop with their relative weights.")]
        public List<LootEntry> entries = new();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the items (and quantities) that should be given to the player.
        /// May return an empty list if the drop chance fails or entries is empty.
        /// </summary>
        public List<(ItemData item, int quantity)> Roll()
        {
            var result = new List<(ItemData, int)>();
            if (entries == null || entries.Count == 0) return result;

            for (int i = 0; i < rollCount; i++)
            {
                if (Random.value > dropChance) continue;

                var entry = PickWeighted();
                if (entry == null) continue;

                var resolvedItem = entry.ResolveItem();
                if (resolvedItem == null)
                {
                    Debug.LogWarning($"[LootTable] {name}: entry has no item — direct ref is null and itemId='{entry.itemId}' not found in ItemRegistry.");
                    continue;
                }

                int qty = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                result.Add((resolvedItem, qty));
            }
            return result;
        }

        // ── Editor: auto-wire item references from itemId ─────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (entries == null) return;
            bool dirty = false;
            foreach (var entry in entries)
            {
                if (entry.item != null || string.IsNullOrEmpty(entry.itemId)) continue;

                // Search all ItemData assets for one whose itemId matches
                var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
                foreach (var guid in guids)
                {
                    var path      = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (candidate != null && candidate.itemId == entry.itemId)
                    {
                        entry.item = candidate;
                        dirty = true;
                        Debug.Log($"[LootTable] {name}: auto-wired '{entry.itemId}' → {path}");
                        break;
                    }
                }
            }
            if (dirty) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        // ── Internals ─────────────────────────────────────────────────────────

        private LootEntry PickWeighted()
        {
            float total = 0f;
            foreach (var e in entries) total += e.weight;
            if (total <= 0f) return null;

            float roll = Random.Range(0f, total);
            float cumulative = 0f;
            foreach (var e in entries)
            {
                cumulative += e.weight;
                if (roll <= cumulative) return e;
            }
            return entries[^1];
        }
    }
}
