using UnityEngine;

namespace Kaligo.Items
{
    /// <summary>
    /// Data container for a single item type.
    /// Create via Assets → Create → Kaligo → ItemData.
    ///
    /// itemId must match the string stored in the inventory DB table.
    /// Convention: lower-kebab-case, e.g. "iron-sword", "leather-chest".
    /// </summary>
    [CreateAssetMenu(menuName = "Kaligo/ItemData", fileName = "New ItemData")]
    public class ItemData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Tooltip("Must be unique across all items and must match the DB item_id value.")]
        public string itemId;

        [Tooltip("Human-readable name shown in the UI.")]
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        public ItemRarity rarity = ItemRarity.Common;

        // ── Visuals ───────────────────────────────────────────────────────────

        [Tooltip("Icon shown in inventory slots.")]
        public Sprite icon;

        [Tooltip("Prefab placed on the ground as a loot pickup, and socketed on the character when equipped. Optional — a generic gem is used if null.")]
        public GameObject modelPrefab;

        // ── Equipment ─────────────────────────────────────────────────────────

        [Tooltip("Check for wearable/equippable items. Uncheck for consumables and materials.")]
        public bool isEquippable;

        [Tooltip("Which slot this item occupies when equipped. Only relevant when isEquippable is true.")]
        public EquipmentSlot equipSlot;

        // ── Stat Modifiers (added while equipped) ─────────────────────────────

        [Header("Stat Bonuses (applied while equipped)")]
        [Tooltip("Flat HP added to the player's max HP.")]
        public float hpBonus;

        [Tooltip("Flat mana added to the player's max mana.")]
        public float manaBonus;

        [Tooltip("Flat damage added on top of the level-based multiplier.")]
        public float flatDamageBonus;

        // ── Stacking ──────────────────────────────────────────────────────────

        [Tooltip("If true, multiple of this item can stack in one inventory slot.")]
        public bool isStackable = true;

        [Tooltip("Maximum quantity per stack. Only relevant when isStackable is true.")]
        public int maxStackSize = 99;

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Returns the rarity colour used in UI labels and slot borders.</summary>
        public Color RarityColor()
        {
            return rarity switch
            {
                ItemRarity.Common    => new Color(0.78f, 0.78f, 0.78f),
                ItemRarity.Uncommon  => new Color(0.12f, 0.74f, 0.12f),
                ItemRarity.Rare      => new Color(0.00f, 0.44f, 0.87f),
                ItemRarity.Epic      => new Color(0.64f, 0.21f, 0.93f),
                ItemRarity.Legendary => new Color(1.00f, 0.50f, 0.00f),
                _                   => Color.white
            };
        }
    }
}
