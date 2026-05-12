using System.Collections;
using UnityEngine;

namespace Kaligo.Mobs
{
    /// <summary>
    /// AI for non-aggressive creatures: Deer, Chicken, Sheep.
    ///
    /// State machine:
    ///   Idle ──(timer)──► Wandering ──(arrived / timeout)──► Idle
    ///      └──(threat near)──────► Fleeing ──(safe)──────────┘
    ///
    /// Threat detection: player within <see cref="MobDefinition.fleeDetectionRange"/>.
    /// Also flees immediately on any hit (<see cref="OnHit"/>).
    /// </summary>
    public class PassiveMobBrain : MobBrain
    {
        // ── State ─────────────────────────────────────────────────────────────

        private enum State { Idle, Wandering, Fleeing }
        private State state = State.Idle;

        // ── Cached ────────────────────────────────────────────────────────────

        private Transform player;
        private Vector3   homePosition;      // position at spawn — wandering stays near here
        private Vector3   wanderTarget;
        private float     stateTimer;
        private Vector3   lastThreatPosition;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            homePosition = transform.position;

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                player = playerGO.transform;
        }

        // ── AI tick ───────────────────────────────────────────────────────────

        protected override void Think()
        {
            if (player == null || definition == null) return;

            stateTimer -= Time.deltaTime;
            float distToPlayer = DistanceTo(player.position);

            switch (state)
            {
                case State.Idle:
                    // Start fleeing if player gets too close
                    if (distToPlayer <= FleeRange)
                    {
                        EnterFlee(player.position);
                        break;
                    }
                    // After idle pause, pick a wander destination
                    if (stateTimer <= 0f)
                        EnterWander();
                    break;

                case State.Wandering:
                    // Abort wander immediately if threatened
                    if (distToPlayer <= FleeRange)
                    {
                        EnterFlee(player.position);
                        break;
                    }

                    float distToTarget = DistanceTo(wanderTarget);
                    if (distToTarget < 0.6f || stateTimer <= 0f)
                    {
                        // Arrived or timed out
                        EnterIdle();
                        break;
                    }

                    MoveToward(wanderTarget, definition.moveSpeed);
                    AnimSetFloat(HashSpeed, 0.4f); // walk
                    break;

                case State.Fleeing:
                    float distToThreat = DistanceTo(lastThreatPosition);
                    if (distToThreat >= definition.fleeUntilDistance)
                    {
                        EnterIdle();
                        break;
                    }

                    MoveAwayFrom(lastThreatPosition, definition.moveSpeed * definition.fleeSpeedMultiplier);
                    AnimSetFloat(HashSpeed, 1f); // run
                    break;
            }
        }

        // ── State transitions ─────────────────────────────────────────────────

        private void EnterIdle()
        {
            state      = State.Idle;
            stateTimer = definition.wanderPauseDuration + Random.Range(-0.5f, 0.5f);
            AnimSetFloat(HashSpeed, 0f);
        }

        private void EnterWander()
        {
            state        = State.Wandering;
            stateTimer   = definition.wanderDuration;
            wanderTarget = PickWanderPoint();
            AnimSetFloat(HashSpeed, 0.4f);
        }

        private void EnterFlee(Vector3 threat)
        {
            state             = State.Fleeing;
            lastThreatPosition = threat;
            AnimSetFloat(HashSpeed, 1f);
        }

        // ── Overrides ─────────────────────────────────────────────────────────

        /// <summary>Any damage immediately triggers flee regardless of current state.</summary>
        protected override void OnHit(float current, float max)
        {
            if (player != null)
                EnterFlee(player.position);
        }

        protected override void OnDeath()
        {
            AnimSetFloat(HashSpeed, 0f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private float FleeRange => definition != null ? definition.fleeDetectionRange : 8f;

        private Vector3 PickWanderPoint()
        {
            // Pick a random point within wanderRadius of home (not current position — avoids drift)
            Vector2 circle    = Random.insideUnitCircle * (definition != null ? definition.wanderRadius : 8f);
            Vector3 candidate = homePosition + new Vector3(circle.x, 0f, circle.y);

            // Keep on roughly the same Y as home (flat terrain assumption)
            candidate.y = homePosition.y;
            return candidate;
        }

        // ── Debug ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;

            // Wander radius (around home)
            Vector3 home = Application.isPlaying ? homePosition : transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(home, definition.wanderRadius);

            // Flee detection
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, definition.fleeDetectionRange);
        }
#endif
    }
}
