using System.Collections.Generic;
using UnityEngine;

namespace Kaligo.Items
{
    /// <summary>
    /// Central catalogue of every ItemData asset in the project.
    /// Create one instance via Assets → Create → Kaligo → ItemRegistry and
    /// assign it to the Bootstrap or a persistent manager GameObject.
    ///
    /// Usage:
    ///   ItemRegistry.Instance.Get("iron-sword")  // returns ItemData or null
    /// </summary>
    [CreateAssetMenu(menuName = "Kaligo/ItemRegistry", fileName = "ItemRegistry")]
    public class ItemRegistry : ScriptableObject
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static ItemRegistry Instance { get; private set; }

        /// <summary>
        /// Call once at startup (e.g. from Bootstrap or a RuntimeInitializeOnLoadMethod).
        /// </summary>
        public static void SetInstance(ItemRegistry registry) => Instance = registry;

        // ── Data ──────────────────────────────────────────────────────────────

        [Tooltip("Drag all ItemData assets here. Each itemId must be unique.")]
        public List<ItemData> items = new();

        // Lazy-built lookup dictionary
        private Dictionary<string, ItemData> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemData>(items.Count);
            foreach (var item in items)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.itemId))
                {
                    Debug.LogWarning($"[ItemRegistry] ItemData '{item.name}' has no itemId — skipped.");
                    continue;
                }
                if (!_lookup.TryAdd(item.itemId, item))
                    Debug.LogWarning($"[ItemRegistry] Duplicate itemId '{item.itemId}' — second entry ignored.");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Returns the ItemData for the given id, or null if not found.</summary>
        public ItemData Get(string itemId)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(itemId, out var data) ? data : null;
        }

        /// <summary>Rebuild the lookup (call after hot-reloading assets in the editor).</summary>
        public void Invalidate() => _lookup = null;

        private void OnValidate()
        {
            _lookup = null;

#if UNITY_EDITOR
            // Auto-populate: find every ItemData asset in the project and ensure it's in the list.
            // Runs whenever Unity validates this asset (on load, on script recompile, on inspector change).
            bool dirty = false;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
            foreach (var guid in guids)
            {
                var path      = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (candidate == null || items.Contains(candidate)) continue;
                items.Add(candidate);
                dirty = true;
                Debug.Log($"[ItemRegistry] Auto-added: {candidate.name} from {path}");
            }
            if (dirty) UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
