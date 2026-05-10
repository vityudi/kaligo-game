using System;

namespace Kaligo.Services.Local {
    // Cumulative XP to reach level n = floor(100 * n^1.85)
    //   Level  2: ~360 XP  (~5 basic kills)
    //   Level  5: ~1520 XP (~20 kills total)
    //   Level 10: ~5180 XP (~70 kills total)
    //   Level 20: ~16500 XP
    public static class XPTable {
        private const double BaseMultiplier = 100.0;
        private const double Exponent       = 1.85;

        public static int LevelFor(int xp) {
            int level = 1;
            while (XPForLevel(level + 1) <= xp) level++;
            return level;
        }

        public static int XPForLevel(int level) =>
            (int)Math.Floor(BaseMultiplier * Math.Pow(level, Exponent));

        public static int XPToNextLevel(int currentXp) {
            int level = LevelFor(currentXp);
            return XPForLevel(level + 1) - currentXp;
        }

        // 0–1 progress fraction within the current level band
        public static float LevelProgress(int xp) {
            int level   = LevelFor(xp);
            int bandMin = XPForLevel(level);
            int bandMax = XPForLevel(level + 1);
            return bandMax > bandMin ? (float)(xp - bandMin) / (bandMax - bandMin) : 0f;
        }
    }
}
