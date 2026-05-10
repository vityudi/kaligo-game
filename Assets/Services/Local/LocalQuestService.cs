using System;
using System.Collections.Generic;
using Kaligo.Database;

namespace Kaligo.Services.Local {
    public class LocalQuestService : IQuestService {
        readonly DatabaseService _db;
        readonly Guid _characterId;
        readonly Dictionary<string, QuestState> _cache = new();

        public event Action<string, QuestState> OnQuestStateChanged;

        public LocalQuestService(DatabaseService db, Guid characterId) {
            _db          = db;
            _characterId = characterId;
            foreach (var row in db.Query<QuestFlagRow>(
                "SELECT * FROM quest_flags WHERE character_id = @Id", new { Id = characterId }))
                _cache[row.QuestId] = row.State;
        }

        public QuestState GetState(string questId) =>
            _cache.TryGetValue(questId, out var s) ? s : QuestState.NotStarted;

        public void SetState(string questId, QuestState state) {
            _cache[questId] = state;
            _db.Execute(@"
                INSERT INTO quest_flags (character_id, quest_id, state)
                VALUES (@CharId, @QuestId, @State::quest_state)
                ON CONFLICT (character_id, quest_id)
                DO UPDATE SET state = EXCLUDED.state, updated_at = NOW()",
                new { CharId = _characterId, QuestId = questId, State = state.ToString() });
            OnQuestStateChanged?.Invoke(questId, state);
        }
    }
}
