using System.Collections;
using UnityEngine;
using Kaligo.Combat;
using Kaligo.Skills;
using Kaligo.World;

namespace Kaligo.Mobs
{
    /// <summary>
    /// AI for hostile creatures: Rat, Wolf, Bear, Goblin.
    /// State machine: Idle / Roam / Chase / Attacking / Fleeing / Dead
    /// Pack alerting: entering Chase pulses nearby same-species brains.
    /// </summary>
    public class AggressiveMobBrain : MobBrain
    {
        private enum State { Idle, Roam, Chase, Attacking, Fleeing }
        private State state = State.Idle;

        private Transform     player;
        private SkillExecutor playerExecutor;
        private HealthSystem  playerHealth;

        private float cooldownTimer;
        private bool  attackInProgress;
        private bool  alertSent;

        private Vector3 spawnPoint;
        private Vector3 roamTarget;
        private float   roamTimer;
        private const float RoamRadius = 5f;

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

        private void Start()
        {
            spawnPoint = transform.position;
            PickNewRoamTarget();
            roamTimer = Random.Range(3f, 6f);
        }

        protected override void Think()
        {
            if (player == null || definition == null) return;

            cooldownTimer -= Time.deltaTime;
            float dist = DistanceTo(player.position);

            switch (state)
            {
                case State.Idle:
                    AnimSetFloat(HashSpeed, 0f);
                    if (dist <= definition.detectionRange && !SafeZone.PlayerIsInSafeZone)
                    {
                        EnterChase();
                        break;
                    }
                    roamTimer -= Time.deltaTime;
                    if (roamTimer <= 0f) EnterRoam();
                    break;

                case State.Roam:
                    if (dist <= definition.detectionRange && !SafeZone.PlayerIsInSafeZone)
                    {
                        EnterChase();
                        break;
                    }
                    float roamDist = new UnityEngine.Vector2(
                        transform.position.x - roamTarget.x,
                        transform.position.z - roamTarget.z).magnitude;
                    if (roamDist < 0.6f)
                    {
                        EnterIdle();
                        break;
                    }
                    MoveToward(roamTarget, definition.moveSpeed * 0.5f);
                    AnimSetFloat(HashSpeed, 0.4f);
                    break;

                case State.Chase:
                    if (ShouldFlee()) { EnterFlee(); break; }
                    if (dist > definition.detectionRange + 4f) { EnterIdle(); break; }
                    if (dist <= definition.attackRange && cooldownTimer <= 0f && !attackInProgress)
                    {
                        StartCoroutine(AttackRoutine());
                        break;
                    }
                    if (!attackInProgress)
                    {
                        MoveToward(player.position, definition.moveSpeed);
                        AnimSetFloat(HashSpeed, 1f);
                    }
                    break;

                case State.Attacking:
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

        private void EnterIdle()
        {
            state     = State.Idle;
            alertSent = false;
            AnimSetFloat(HashSpeed, 0f);
            roamTimer = Random.Range(2f, 5f);
        }

        private void EnterRoam()
        {
            state = State.Roam;
            PickNewRoamTarget();
        }

        private void EnterChase()
        {
            state = State.Chase;
            AnimSetFloat(HashSpeed, 1f);
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

        private void PickNewRoamTarget()
        {
            Vector2 circle = Random.insideUnitCircle * RoamRadius;
            roamTarget = spawnPoint + new Vector3(circle.x, 0f, circle.y);
        }

        private IEnumerator AttackRoutine()
        {
            state            = State.Attacking;
            attackInProgress = true;
            AnimSetFloat(HashSpeed, 0f);
            AnimTrigger(HashAttack);

            float telegraphTime = definition.attackDuration * definition.damageAtNormalized;
            yield return new WaitForSeconds(telegraphTime);

            if (!IsDead)
            {
                float dist      = DistanceTo(player.position);
                bool  inRange   = dist <= definition.attackRange + 0.6f;
                bool  invincible = playerExecutor != null && playerExecutor.IsInvincible;
                if (inRange && !invincible && playerHealth != null)
                    playerHealth.TakeDamage(definition.damage);
            }

            float recoveryTime = definition.attackDuration * (1f - definition.damageAtNormalized);
            yield return new WaitForSeconds(recoveryTime);

            cooldownTimer    = definition.attackCooldown;
            attackInProgress = false;
            if (!IsDead) EnterChase();
        }

        private void AlertNearby()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, definition.alertRadius);
            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                var other = col.GetComponent<AggressiveMobBrain>()
                         ?? col.GetComponentInParent<AggressiveMobBrain>();
                if (other == null || other.IsDead) continue;
                if (other.definition != definition) continue;
                other.ForceChase();
            }
        }

        public void ForceChase()
        {
            if (IsDead || state == State.Attacking) return;
            EnterChase();
        }

        protected override void OnHit(float current, float max)
        {
            if (state == State.Idle || state == State.Roam)
                EnterChase();
        }

        protected override void OnDeath()
        {
            StopAllCoroutines();
            attackInProgress = false;
            AnimSetFloat(HashSpeed, 0f, 0f);
        }

        private bool ShouldFlee()
        {
            if (definition == null || definition.fleeAtHpFraction <= 0f) return false;
            if (health == null) return false;
            float maxHp = health.MaxHealth;
            return maxHp > 0f && health.CurrentHealth / maxHp <= definition.fleeAtHpFraction;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, definition.detectionRange);
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, definition.attackRange);
            if (definition.alertsNearby)
            {
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.1f);
                Gizmos.DrawSphere(transform.position, definition.alertRadius);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, roamTarget);
            Gizmos.DrawWireSphere(roamTarget, 0.3f);
        }
#endif
    }
}
