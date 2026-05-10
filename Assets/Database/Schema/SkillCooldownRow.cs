using System;

namespace Kaligo.Database {
    public class SkillCooldownRow {
        public int      Id          { get; set; }
        public Guid     CharacterId { get; set; }
        public string   SkillId     { get; set; }
        public DateTime ReadyAtUtc  { get; set; }
    }
}
