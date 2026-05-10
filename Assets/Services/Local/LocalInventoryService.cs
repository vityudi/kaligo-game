using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Kaligo.Database;

namespace Kaligo.Services.Local {
    public class LocalInventoryService : IInventoryService {
        readonly DatabaseService _db;
        readonly Guid _characterId;
        List<InventoryItemRow> _cache;

        public event Action OnChanged;

        public LocalInventoryService(DatabaseService db, Guid characterId) {
            _db          = db;
            _characterId = characterId;
            Refresh();
        }

        void Refresh() =>
            _cache = _db.Query<InventoryItemRow>(
                "SELECT * FROM inventory WHERE character_id = @Id",
                new { Id = _characterId }).ToList();

        public IReadOnlyList<InventoryItemRow> GetAll() => _cache;

        public void Add(string itemId, int quantity = 1) {
            var existing = _cache.Find(r => r.ItemId == itemId && r.Slot == null);
            if (existing != null) {
                existing.Quantity += quantity;
                _db.Execute("UPDATE inventory SET quantity = @Quantity WHERE id = @Id",
                    new { existing.Quantity, existing.Id });
            } else {
                _db.Execute(
                    "INSERT INTO inventory (character_id, item_id, quantity) VALUES (@CharId, @ItemId, @Qty)",
                    new { CharId = _characterId, ItemId = itemId, Qty = quantity });
                Refresh();
            }
            OnChanged?.Invoke();
        }

        public void Remove(string itemId, int quantity = 1) {
            var row = _cache.Find(r => r.ItemId == itemId && r.Slot == null);
            if (row == null) return;
            row.Quantity -= quantity;
            if (row.Quantity <= 0) {
                _db.Execute("DELETE FROM inventory WHERE id = @Id", new { row.Id });
                _cache.Remove(row);
            } else {
                _db.Execute("UPDATE inventory SET quantity = @Quantity WHERE id = @Id",
                    new { row.Quantity, row.Id });
            }
            OnChanged?.Invoke();
        }

        public void Equip(int rowId, string slot) {
            using var tx = _db.BeginTransaction();
            _db.Connection.Execute(
                "UPDATE inventory SET slot = NULL WHERE character_id = @CharId AND slot = @Slot::equipment_slot",
                new { CharId = _characterId, Slot = slot }, tx);
            _db.Connection.Execute(
                "UPDATE inventory SET slot = @Slot::equipment_slot WHERE id = @Id",
                new { Slot = slot, Id = rowId }, tx);
            tx.Commit();
            Refresh();
            OnChanged?.Invoke();
        }

        public void Unequip(string slot) {
            _db.Execute(
                "UPDATE inventory SET slot = NULL WHERE character_id = @CharId AND slot = @Slot::equipment_slot",
                new { CharId = _characterId, Slot = slot });
            Refresh();
            OnChanged?.Invoke();
        }
    }
}
