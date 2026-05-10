using System;

namespace Kaligo.Database {
    public class CharacterRow {
        public Guid   Id        { get; set; } = Guid.NewGuid();
        public string Name      { get; set; }
        public int    Level     { get; set; } = 1;
        public int    Xp        { get; set; } = 0;
        public float  MaxHp     { get; set; } = 100f;
        public float  MaxMana   { get; set; } = 100f;
        public float  PosX      { get; set; }
        public float  PosY      { get; set; }
        public float  PosZ      { get; set; }
    }
}
