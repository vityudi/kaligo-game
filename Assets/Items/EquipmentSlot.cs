namespace Kaligo.Items
{
    /// <summary>
    /// Equipment slot enum — must stay in sync with the PostgreSQL equipment_slot enum
    /// defined in Database/migrations/002_inventory.sql.
    /// </summary>
    public enum EquipmentSlot
    {
        Weapon  = 0,
        OffHand = 1,
        Helmet  = 2,
        Chest   = 3,
        Legs    = 4,
        Boots   = 5,
        Ring1   = 6,
        Ring2   = 7
    }
}
