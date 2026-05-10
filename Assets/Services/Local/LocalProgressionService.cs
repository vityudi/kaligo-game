using System;
using Dapper;
using Kaligo.Database;

namespace Kaligo.Services.Local {
    public class LocalProgressionService : IProgressionService {
        readonly DatabaseService _db;
        CharacterRow _character;

        public int Level => _character.Level;
        public int XP    => _character.Xp;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXPChanged;

        public LocalProgressionService(DatabaseService db, Guid characterId) {
            _db        = db;
            _character = db.QueryFirst<CharacterRow>(
                "SELECT * FROM characters WHERE id = @Id", new { Id = characterId });
        }

        public void GrantXP(int amount) {
            _character.Xp   += amount;
            int newLevel     = XPTable.LevelFor(_character.Xp);
            bool leveledUp   = newLevel > _character.Level;
            _character.Level = newLevel;

            _db.Execute(
                "UPDATE characters SET xp = @Xp, level = @Level, updated_at = NOW() WHERE id = @Id",
                new { _character.Xp, _character.Level, _character.Id });

            OnXPChanged?.Invoke(_character.Xp);
            if (leveledUp) OnLevelUp?.Invoke(_character.Level);
        }
    }
}
