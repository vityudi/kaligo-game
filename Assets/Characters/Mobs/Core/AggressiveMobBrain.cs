using System.Collections;
using UnityEngine;
using Kaligo.Combat;
using Kaligo.Skills;
using Kaligo.World;

namespace Kaligo.Mobs
{
    /// <summary>
    /// AI for hostile creatures: Rat, Wolf, Bear, Goblin.
    ///
    /// State machine:
    ///   Idle ──(player in range)──► Chase ──(in attack range)──► Attacking ──► Chase
    ///      └────────────────────────────────────────── Dead
    ///   Chase ──(low HP + fleeAtHpFraction > 0)──► Fleeing ──(safe)──► Idle
    ///
    /// Pack alerting (wolves): entering Chase sends a pulse to nearby same-species
    /// <see cref="AggressiveMobBrain"/>s, forcing them to Chase too.
    /// </summary>
    public class AggressiveMobBrain : MobBrain
    {
        // ── State ─────────────────────────────────────────────────────────────

        private enum State { Idle, Chase, Attacking, Fleeing }
        private State state = State.Idle;

        // ── Cached references ─────────────────────────────────────────────────

        private Transform      player;
        private SkillExecutor  playerExecutor;
        private HealthSystem   playerHealth;

        // ── Timers / flags ────────────────────────────────────────────────────

        private float cooldownTimer;
        private bool  attackInProgress;
        private bool  alertSent;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                player         = playerGO.transform;
                playerExecutor = playerGO.GetComponent<SkillExecutor>();
                playerHealth   = playerGO.GetComponent<HealthSystem>();
            }
        }

        // ── AI tick ───────────────────────────────────────────────────────────

        protected override void Think()
        {
            if (player == null) return;

            cooldownTimer -= Time.deltaTime;
            float dist = DistanceTo(player.position);

            switch (state)
            {
                case State.Idle:
                    // Never aggro while the player is in a safe zone (village, etc.)
                    if (dist <= definition.detectionRange && !SafeZone.PlayerIsInSafeZone)
                        EnterChase();
                    break;

                case State.Chase:
                    // Check low-HP flee
                    if (ShouldFlee())
                    {
                        EnterFlee();
                        break;
                    }

                    if (dist > definition.detectionRange + 3f) // leash — give up if player runs far
                    {
                        EnterIdle();
                        break;
                    }

                    if (dist <= definition.attackRange && cooldownTimer <= 0f && !attackInProgress)
                    {
                        StartCoroutine(AttackRoutine());
                        break;
                    }

                    if (!attackInProgress)
                    {
                        MoveToward(player.position, definition.moveSpeed);
                        AnimSetFloat(HashSpeed, 0.5f);
                    }
                    break;

                case State.Attacking:
                    // Face player while winding up
                    if (!attackInProgress) EnterChase();
                    else FaceTarget(player.position);
                    break;

                case State.Fleeing:
                    if (DistanceTo(player.position) >= definition.detectionRange * 1.5f)
                    {
                        EnterIdle();
                        break;
                    }
                    MoveAwayFrom(player.position, definition.moveSpeed * 1.4f);
                    AnimSetFloat(HashSpeed, 1f);
                    break;
            }
        }

        // ── State transitions ─────────────────────────────────────────────────

        private void EnterIdle()
        {
            state = State.Idle;
            alertSent = false;
            AnimSetFloat(HashSpeed, 0f);
        }

        private void EnterChase()
        {
            state = State.Chase;
            AnimSetFloat(HashSpeed, 0.5f);

            // Pack alerting — wolves wake up nearby allies
            if (!alertSent && definition != null && definition.alertsNearby)
            {
                alertSent = true;
                AlertNearby();
            }
        }

        private void EnterFlee()
        {
            state = State.Fleeing;
            AnimSetFloat(HashSpeed, 1f);
        }

        // ── Attack ────────────────────────────────────────────────────────────

        private IEnumerator AttackRoutine()
        {
            state            = State.Attacking;
            attackInProgress = true;
            AnimTrigger(HashAttack);

            // Telegraph: wait for the "strike frame" before dealing damage
            float telegraphTime = definition.attackDuration * definition.damageAtNormalized;
            yield return new WaitForSeconds(telegraphTime);

            // Damage check at strike frame
            if (!IsDead)
            {
                float   dist       = DistanceTo(player.position);
                bool    inRange    = dist <= definition.attackRange + 0.5f;
                bool    invincible = playerExecutor != null && playerExecutor.IsInvincible;

                if (inRange && !invincible && playerHealth != null)
                    playerHealth.TakeDamage(definition.damage);
            }

            // Wait for the rest of the swing animation
            float recoveryTime = definition.attackDuration * (1f - definition.damageAtNormalized);
            yield return new WaitForSeconds(recoveryTime);

            cooldownTimer    = definition.attackCooldown;
            attackInProgress = false;

            if (!IsDead)
                EnterChase();
        }

        // ── Pack alerting ─────────────────────────────────────────────────────

        /// <summary>
        /// Pulse to nearby same-species AggressiveMobBrains.
        /// Called once the first time this mob enters Chase.
        /// </summary>
        private void AlertNearby()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position, definition.alertRadius);

            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                var other = col.GetComponent<AggressiveMobBrain>();
                if (other == null || other.IsDead) continue;

                // Only alert the same species (same definition asset reference)
                if (other.definition != definition) continue;

                other.ForceChase();
            }
        }

        /// <summary>
        /// Force this mob into Chase immediately (called by pack-mates via AlertNearby).
        /// </summary>
        public void ForceChase()
        {
            if (IsDead || state == State.Attacking) return;
            EnterChase();
        }

        // ── Overrides ─────────────────────────────────────────────────────────

        protected override void OnHit(float current, float max)
        {
            // Getting hit wakes an idle mob
            if (state == State.Idle)
                EnterChase();
        }

        protected override void OnDeath()
        {
            StopAllCoroutines();
            attackInProgress = false;
            AnimSetFloat(HashSpeed, 0f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool ShouldFlee()
        {
            if (definition.fleeAtHpFraction <= 0f) return false;
            return health.CurrentHealth / definition.maxHealth <= definition.fleeAtHpFraction;
        }

        // ── Debug ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;

            // Detection range
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, definition.detectionRange);

            // Attack range
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, definition.attackRange);

            // Alert radius (wolves)
            if (definition.alertsNearby)
            {
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.1f);
                Gizmos.DrawSphere(transform.position, definition.alertRadius);
            }
        }
#endif
    }
}
