using UnityEngine;
using Kaligo.Items;

namespace Kaligo.Mobs
{
    public enum MobType { Passive, Aggressive }

    /// <summary>
    /// Data contract for a single creature species.
    /// Create via: Assets → Create → Kaligo → World → Mob Definition.
    /// Drop one MobSpawner into any scene, assign the definition, done.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMob", menuName = "Kaligo/World/Mob Definition")]
    public class MobDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Tooltip("Unique lowercase key — used to identify this mob in code. E.g. 'deer', 'goblin'.")]
        public string mobId;

        [Tooltip("Display name shown in health bars and UI.")]
        public string displayName;

        public MobType type = MobType.Passive;

        // ── Stats ─────────────────────────────────────────────────────────────

        [Header("Stats")]
        public float maxHealth  = 50f;
        public float moveSpeed  = 3f;
        public float turnSpeed  = 360f;

        // ── Passive behavior (Deer, Chicken, Sheep) ───────────────────────────

        [Header("Passive Behavior")]
        [Tooltip("Radius around home in which the mob wanders.")]
        public float wanderRadius        = 8f;

        [Tooltip("How long (seconds) the mob walks toward its wander target before picking a new one.")]
        public float wanderDuration      = 5f;

        [Tooltip("Pause time between wander walks.")]
        public float wanderPauseDuration = 2f;

        [Tooltip("Distance at which this mob notices a threat (player) and starts fleeing.")]
        public float fleeDetectionRange  = 8f;

        [Tooltip("Speed multiplier applied on top of moveSpeed while fleeing.")]
        public float fleeSpeedMultiplier = 1.8f;

        [Tooltip("Mob stops fleeing when it has put this much distance between itself and the last known threat position.")]
        public float fleeUntilDistance   = 16f;

        // ── Aggressive behavior (Rat, Wolf, Bear, Goblin) ────────────────────

        [Header("Aggressive Behavior")]
        [Tooltip("Range at which the mob spots the player and enters Chase.")]
        public float detectionRange      = 12f;

        [Tooltip("Range at which the mob can land a melee hit.")]
        public float attackRange         = 2f;

        [Tooltip("Base damage per hit.")]
        public float damage              = 15f;

        [Tooltip("Minimum seconds between attacks.")]
        public float attackCooldown      = 2.5f;

        [Tooltip("Total animation duration of one attack cycle.")]
        public float attackDuration      = 2.33f;

        [Tooltip("Normalized time (0–1) within the attack animation when damage lands (telegraph window).")]
        [Range(0f, 1f)]
        public float damageAtNormalized  = 0.45f;

        [Tooltip("If > 0, mob flees when its HP falls below this fraction of max. Bears run at 20% HP.")]
        [Range(0f, 0.5f)]
        public float fleeAtHpFraction    = 0f;

        [Tooltip("When entering Chase, alert nearby same-species mobs to chase too (wolf pack behavior).")]
        public bool alertsNearby         = false;

        [Tooltip("Radius of the alert pulse.")]
        public float alertRadius         = 15f;

        // ── Rewards ───────────────────────────────────────────────────────────

        [Header("Rewards")]
        public int       xpReward  = 25;
        public LootTable lootTable;

        // ── Visual (placeholder) ──────────────────────────────────────────────

        [Header("Placeholder Visual")]
        [Tooltip("Tint applied to the capsule primitive until a real model is assigned.")]
        public Color placeholderColor  = Color.gray;

        [Tooltip("Height of the capsule primitive in world units.")]
        public float placeholderHeight = 1.8f;

        [Tooltip("Radius of the capsule primitive.")]
        public float placeholderRadius = 0.35f;
    }
}
