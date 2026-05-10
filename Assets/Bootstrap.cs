using System;
using Kaligo.Database;
using Kaligo.Items;
using Kaligo.Services;
using UnityEngine;

namespace Kaligo {
    public class Bootstrap : MonoBehaviour {
        [SerializeField, Tooltip("Uncheck to supply a custom connection string below.")]
        bool _useLocalDatabase = true;

        [SerializeField, Tooltip("Overrides the default local connection string when unchecked above.")]
        string _connectionStringOverride;

        // Phase 6: assign the ItemRegistry asset in the Inspector.
        [SerializeField, Tooltip("The ItemRegistry ScriptableObject listing all ItemData assets.")]
        ItemRegistry _itemRegistry;

        DatabaseService _db;

        void Awake() {
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        void InitializeServices() {
            // ItemRegistry works without a database — register it first so loot
            // lookups and the inventory UI work even when Postgres isn't running.
            if (_itemRegistry != null)
                ItemRegistry.SetInstance(_itemRegistry);
            else
                Debug.LogWarning("[Bootstrap] ItemRegistry not assigned — item lookups will return null.");

            // Database is optional for Act I play-testing.
            // Without docker-compose up -d the DB won't be reachable; we log a
            // clear warning and continue in offline mode — XP, loot, and inventory
            // all still work, they just won't persist across restarts.
            try {
                _db = new DatabaseService();
                string connStr = _useLocalDatabase
                    ? DatabaseService.LocalConnectionString
                    : _connectionStringOverride;
                _db.Initialize(connStr);

                // TODO Phase 8: replace with a real character-selection / new-game screen.
                string savedId = PlayerPrefs.GetString("ActiveCharacterId", string.Empty);
                if (string.IsNullOrEmpty(savedId)) {
                    savedId = Guid.NewGuid().ToString();
                    PlayerPrefs.SetString("ActiveCharacterId", savedId);
                    PlayerPrefs.Save();
                }

                GameServices.Initialize(_db, Guid.Parse(savedId));
                Debug.Log($"[Bootstrap] Services initialized (DB) for character {savedId}");
            }
            catch (Exception e) {
                Debug.LogWarning(
                    "[Bootstrap] Database unavailable — running in offline mode (no persistence).\n" +
                    "Run 'docker-compose up -d' in the project root to enable persistence.\n" +
                    $"Detail: {e.Message}");

                // Still give the game functional services so XP, loot, and
                // inventory all work — they just won't survive a restart.
                GameServices.InitializeOffline();
            }
        }

        void OnDestroy() {
            GameServices.Dispose();
            _db?.Dispose();
        }
    }
}
