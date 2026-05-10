using System;

namespace Kaligo.Database {
    public enum QuestState { NotStarted, InProgress, Complete }

    public class QuestFlagRow {
        public int        Id          { get; set; }
        public Guid       CharacterId { get; set; }
        public string     QuestId     { get; set; }
        public QuestState State       { get; set; } = QuestState.NotStarted;
    }
}
