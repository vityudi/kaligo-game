using System;
using Kaligo.Database;
using Kaligo.Services.Local;

namespace Kaligo.Services {
    public static class GameServices {
        public static IProgressionService Progression { get; private set; }
        public static IInventoryService   Inventory   { get; private set; }
        public static IQuestService       Quest       { get; private set; }

        // Act I: networked = false (default)
        // Act II Phase 10+: networked = true — swaps every implementation at once
        public static void Initialize(DatabaseService db, Guid characterId, bool networked = false) {
            if (networked)
                throw new NotImplementedException("Networked services are implemented in Act II (Phase 10+).");

            Progression = new LocalProgressionService(db, characterId);
            Inventory   = new LocalInventoryService(db, characterId);
            Quest       = new LocalQuestService(db, characterId);
        }

        public static void Dispose() { }
    }
}
