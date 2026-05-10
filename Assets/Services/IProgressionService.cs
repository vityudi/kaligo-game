using System;

namespace Kaligo.Services {
    public interface IProgressionService {
        int  Level { get; }
        int  XP    { get; }
        void GrantXP(int amount);
        event Action<int> OnLevelUp;    // passes new level
        event Action<int> OnXPChanged;
    }
}
