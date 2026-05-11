using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Mobs
{
    /// <summary>
    /// Drop this into any scene to spawn and respawn a mob species.
    ///
    /// Usage:
    ///   1. Add MobSpawner to an empty GameObject.
    ///   2. Assign a MobDefinition asset.
    ///   3. Set maxAlive, spawnRadius, respawnDelay.
    ///   That's it — mobs will appear, roam or chase, die, and respawn automatically.
    ///
    /// To place mobs manually instead: set <c>spawnOnStart = false</c> and call
    /// <see cref="SpawnOne"/> from editor tools or scene-setup scripts.
    /// </summary>
    public class MobSpawner : MonoBehaviour
    {
        [Header("Mob")]
        [SerializeField] private MobDefinition definition;

        [Header("Spawn Settings")]
        [Tooltip("How many of this mob can be alive at the same time in this spawner.")]
        [SerializeField] private int   maxAlive      = 3;

        [Tooltip("Random radius around this GameObject's position for spawn points.")]
        [SerializeField] private float spawnRadius   = 10f;

        [Tooltip("Seconds between a mob's death and its respawn.")]
        [SerializeField] private float respawnDelay  = 30f;

        [Tooltip("Spawn mobs automatically when the scene starts.")]
        [SerializeField] private bool  spawnOnStart  = true;

        [Tooltip("If true, defeated mobs never respawn (use for unique/boss spawners).")]
        [SerializeField] private bool  noRespawn     = false;

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<GameObject> aliveMobs = new List<GameObject>();
        private int aliveCount => CountAlive();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (spawnOnStart)
                StartCoroutine(InitialSpawnBurst());
        }

        private IEnumerator InitialSpawnBurst()
        {
            // Stagger initial spawns to avoid a sudden pop-in at scene load
            for (int i = 0; i < maxAlive; i++)
            {
                SpawnOne();
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Spawn one mob immediately (if under the cap).
        /// Returns the new mob's GameObject, or null if the cap is reached.
        /// </summary>
        public GameObject SpawnOne()
        {
            if (definition == null)
            {
                Debug.LogWarning($"[MobSpawner] {name}: no MobDefinition assigned.", this);
                return null;
            }

            if (aliveCount >= maxAlive) return null;

            Vector3    pos = RandomSpawnPoint();
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject mob = MobFactory.Create(definition, pos, rot);

            if (mob == null) return null;

            // Subscribe to death for respawn tracking
            var health = mob.GetComponent<HealthSystem>();
            if (health != null)
                health.OnDeath += () => OnMobDied(mob);

            aliveMobs.Add(mob);
            return mob;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnMobDied(GameObject mob)
        {
            // Remove from list (the GameObject will be destroyed by MobBrain after its corpse delay)
            aliveMobs.Remove(mob);

            if (!noRespawn && gameObject != null && gameObject.activeInHierarchy)
                StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay + Random.Range(-5f, 5f));

            // Only respawn if we're still under the cap
            if (aliveCount < maxAlive)
                SpawnOne();
        }

        private int CountAlive()
        {
            // Clean up any entries that were destroyed
            aliveMobs.RemoveAll(m => m == null);
            return aliveMobs.Count;
        }

        private Vector3 RandomSpawnPoint()
        {
            // Random point inside spawn radius, kept at the spawner's Y
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(circle.x, 0f, circle.y);
        }

        // ── Editor Gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (definition == null) return;

            // Draw spawn radius disk
            Gizmos.color = definition.type == MobType.Passive
                ? new Color(0f, 0.8f, 0f, 0.15f)
                : new Color(0.9f, 0.1f, 0.1f, 0.15f);

            DrawDisk(transform.position, spawnRadius, 32);

            // Label at the spawner
            UnityEditor.Handles.color = definition.type == MobType.Passive ? Color.green : Color.red;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"{definition.displayName} ×{maxAlive}");
        }

        private static void DrawDisk(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float   angle = i * step * Mathf.Deg2Rad;
                Vector3 next  = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
