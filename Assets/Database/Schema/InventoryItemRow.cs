using System;

namespace Kaligo.Database {
    public class InventoryItemRow {
        public int    Id          { get; set; }
        public Guid   CharacterId { get; set; }
        public string ItemId      { get; set; }
        public int    Quantity    { get; set; } = 1;
        public string Slot        { get; set; }  // null = in bag; matches equipment_slot enum values
    }
}
