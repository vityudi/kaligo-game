namespace Kaligo.Services.Local {
    // TODO Phase 5: design the real leveling curve. Placeholder: quadratic, ~100 xp to reach level 2.
    public static class XPTable {
        public static int LevelFor(int xp) {
            int level = 1;
            while (XPForLevel(level + 1) <= xp) level++;
            return level;
        }

        public static int XPForLevel(int level) => 100 * level * level;
    }
}
