using System;
using System.Collections.Generic;
using Kaligo.Database;

namespace Kaligo.Services {
    public interface IInventoryService {
        IReadOnlyList<InventoryItemRow> GetAll();
        void Add(string itemId, int quantity = 1);
        void Remove(string itemId, int quantity = 1);
        void Equip(int rowId, string slot);
        void Unequip(string slot);
        event Action OnChanged;
    }
}
