using System;
using Kaligo.Services.Local;

namespace Kaligo.Services.Local {
    /// <summary>
    /// In-memory progression service used when the database is unavailable.
    /// XP and level are kept for the duration of the play session only —
    /// nothing is persisted across restarts.
    /// </summary>
    public class OfflineProgressionService : IProgressionService {
        int _xp    = 0;
        int _level = 1;

        public int Level => _level;
        public int XP    => _xp;

        public event Action<int> OnLevelUp;
        public event Action<int> OnXPChanged;

        public void GrantXP(int amount) {
            _xp += amount;
            int newLevel  = XPTable.LevelFor(_xp);
            bool leveledUp = newLevel > _level;
            _level = newLevel;

            OnXPChanged?.Invoke(_xp);
            if (leveledUp) OnLevelUp?.Invoke(_level);
        }
    }
}
