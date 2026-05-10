using System;
using System.Collections.Generic;
using UnityEngine;
using Kaligo.Items;
using Kaligo.Services;

namespace Kaligo.Characters
{
    /// <summary>
    /// Manages equipped-item stat bonuses and the visual sockets on the player rig.
    ///
    /// Attach to the Player root GameObject alongside PlayerStats.
    ///
    /// Setup in the Inspector:
    ///   • Assign the ItemRegistry asset.
    ///   • Assign WeaponSocket and ChestSocket Transform references — these are
    ///     child GameObjects on the character rig (e.g. "WeaponSocket" parented
    ///     to the right-hand bone, "ChestSocket" parented to the spine bone).
    ///   • Optionally assign additional sockets for the other slots.
    ///
    /// When IInventoryService.OnChanged fires, this component:
    ///   1. Reads all equipped items from the service.
    ///   2. Sums up stat bonuses.
    ///   3. Calls PlayerStats.RefreshStats() so the new values take effect.
    ///   4. Instantiates the item model in the matching socket (Weapon + Chest
    ///      are visible; others are wired by socket but invisible until set up).
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static EquipmentManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Registry")]
        [Tooltip("The ItemRegistry ScriptableObject that lists all ItemData assets.")]
        [SerializeField] private ItemRegistry itemRegistry;

        [Header("Rig Sockets")]
        [Tooltip("Transform parented to the right-hand bone. Weapon model is spawned here.")]
        [SerializeField] private Transform weaponSocket;

        [Tooltip("Transform parented to the spine/chest bone. Chest armour model is spawned here.")]
        [SerializeField] private Transform chestSocket;

        [Tooltip("Transform for the OffHand socket (shield, off-hand weapon, etc.).")]
        [SerializeField] private Transform offHandSocket;

        [Tooltip("Transform for the Helmet socket (head bone).")]
        [SerializeField] private Transform helmetSocket;

        [Tooltip("Transform for the Legs socket (hip/pelvis bone).")]
        [SerializeField] private Transform legsSocket;

        [Tooltip("Transform for the Boots socket (foot bone).")]
        [SerializeField] private Transform bootsSocket;

        // ── Computed bonuses (read by PlayerStats) ────────────────────────────

        /// <summary>Total HP bonus from all currently equipped items.</summary>
        public float HpBonus     { get; private set; }

        /// <summary>Total mana bonus from all currently equipped items.</summary>
        public float ManaBonus   { get; private set; }

        /// <summary>Total flat damage bonus from all currently equipped items.</summary>
        public float DamageBonus { get; private set; }

        // ── Internal state ────────────────────────────────────────────────────

        // Tracks which GameObject is currently spawned in each socket
        private readonly Dictionary<EquipmentSlot, GameObject> _socketInstances = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (GameServices.Inventory == null) return;

            GameServices.Inventory.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (GameServices.Inventory != null)
                GameServices.Inventory.OnChanged -= Refresh;
        }

        // ── Core refresh ──────────────────────────────────────────────────────

        private void Refresh()
        {
            HpBonus     = 0f;
            ManaBonus   = 0f;
            DamageBonus = 0f;

            if (itemRegistry == null || GameServices.Inventory == null) return;

            // Accumulate bonuses from every equipped row
            var equipped = new HashSet<EquipmentSlot>();

            foreach (var row in GameServices.Inventory.GetAll())
            {
                if (string.IsNullOrEmpty(row.Slot)) continue;  // not equipped

                var data = itemRegistry.Get(row.ItemId);
                if (data == null) continue;

                HpBonus     += data.hpBonus;
                ManaBonus   += data.manaBonus;
                DamageBonus += data.flatDamageBonus;

                if (data.isEquippable && Enum.TryParse<EquipmentSlot>(row.Slot, out var slot))
                    equipped.Add(slot);
            }

            // Rebuild all socket visuals
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                RefreshSocket(slot, equipped.Contains(slot));

            // Push new totals into PlayerStats
            PlayerStats.Instance?.RefreshStats();
        }

        // ── Socket management ─────────────────────────────────────────────────

        private void RefreshSocket(EquipmentSlot slot, bool hasItem)
        {
            // Remove old visual
            if (_socketInstances.TryGetValue(slot, out var existing) && existing != null)
            {
                Destroy(existing);
                _socketInstances.Remove(slot);
            }

            if (!hasItem) return;

            // Find the equipped ItemData for this slot
            ItemData data = FindEquippedItem(slot);
            if (data?.modelPrefab == null) return;

            Transform socket = GetSocket(slot);
            if (socket == null) return;

            var instance = Instantiate(data.modelPrefab, socket);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _socketInstances[slot] = instance;
        }

        private ItemData FindEquippedItem(EquipmentSlot slot)
        {
            if (itemRegistry == null) return null;
            string slotStr = slot.ToString();

            foreach (var row in GameServices.Inventory.GetAll())
            {
                if (row.Slot == slotStr)
                    return itemRegistry.Get(row.ItemId);
            }
            return null;
        }

        private Transform GetSocket(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon  => weaponSocket,
            EquipmentSlot.OffHand => offHandSocket,
            EquipmentSlot.Helmet  => helmetSocket,
            EquipmentSlot.Chest   => chestSocket,
            EquipmentSlot.Legs    => legsSocket,
            EquipmentSlot.Boots   => bootsSocket,
            _                     => null
        };
    }
}
