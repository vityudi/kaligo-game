using System;
using System.Collections.Generic;
using Kaligo.Database;

namespace Kaligo.Services.Local {
    /// <summary>
    /// In-memory inventory service used when the database is unavailable.
    /// All item rows are kept in a List for the play session only —
    /// nothing is persisted across restarts.
    /// </summary>
    public class OfflineInventoryService : IInventoryService {
        readonly List<InventoryItemRow> _rows = new();
        int _nextId = 1;

        public event Action OnChanged;

        public IReadOnlyList<InventoryItemRow> GetAll() => _rows;

        public void Add(string itemId, int quantity = 1) {
            // Stack into an existing bag row if possible
            var existing = _rows.Find(r => r.ItemId == itemId && r.Slot == null);
            if (existing != null) {
                existing.Quantity += quantity;
            } else {
                _rows.Add(new InventoryItemRow {
                    Id       = _nextId++,
                    ItemId   = itemId,
                    Quantity = quantity,
                    Slot     = null
                });
            }
            OnChanged?.Invoke();
        }

        public void Remove(string itemId, int quantity = 1) {
            var row = _rows.Find(r => r.ItemId == itemId && r.Slot == null);
            if (row == null) return;
            row.Quantity -= quantity;
            if (row.Quantity <= 0) _rows.Remove(row);
            OnChanged?.Invoke();
        }

        public void Equip(int rowId, string slot) {
            // Clear whatever is already in this slot
            var prev = _rows.Find(r => r.Slot == slot);
            if (prev != null) prev.Slot = null;

            // Equip the new row
            var row = _rows.Find(r => r.Id == rowId);
            if (row != null) row.Slot = slot;

            OnChanged?.Invoke();
        }

        public void Unequip(string slot) {
            var row = _rows.Find(r => r.Slot == slot);
            if (row != null) row.Slot = null;
            OnChanged?.Invoke();
        }
    }
}
