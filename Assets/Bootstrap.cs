using System;
using Kaligo.Database;
using Kaligo.Services;
using UnityEngine;

namespace Kaligo {
    public class Bootstrap : MonoBehaviour {
        [SerializeField, Tooltip("Uncheck to supply a custom connection string below.")]
        bool _useLocalDatabase = true;

        [SerializeField, Tooltip("Overrides the default local connection string when unchecked above.")]
        string _connectionStringOverride;

        DatabaseService _db;

        void Awake() {
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        void InitializeServices() {
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
            Debug.Log($"[Bootstrap] Services initialized for character {savedId}");
        }

        void OnDestroy() {
            GameServices.Dispose();
            _db?.Dispose();
        }
    }
}
