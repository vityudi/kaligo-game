using System.Collections;
using UnityEngine;
using Kaligo.Skills;

namespace Kaligo.Combat
{
    /// <summary>
    /// Simple three-state enemy: Idle → Chase → Attacking → Dead.
    /// Uses CharacterController for movement (no NavMesh required).
    /// Detects the player by "Player" tag.
    /// Respects the player's i-frames when dealing damage.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(HealthSystem))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRange = 12f;
        [SerializeField] private float attackRange    = 2f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed  = 3.5f;
        [SerializeField] private float turnSpeed  = 360f;
        [SerializeField] private float gravity    = -20f;

        [Header("Attack")]
        [SerializeField] private float damage           = 15f;
        [SerializeField] private float attackCooldown   = 2.5f;
        [SerializeField] private float attackDuration   = 2.33f;
        [Tooltip("Normalized time within the attack animation when damage is applied (telegraph window).")]
        [Range(0f, 1f)]
        [SerializeField] private float damageAtNormalized = 0.45f;

        // ── State ─────────────────────────────────────────────────────────────

        private enum State { Idle, Chase, Attacking, Dead }
        private State state = State.Idle;

        private CharacterController controller;
        private Animator             animator;
        private HealthSystem         health;
        private Transform            player;
        private SkillExecutor        playerExecutor;

        private float verticalVelocity;
        private float cooldownTimer;
        private bool  attackInProgress;

        private static readonly int SpeedHash   = Animator.StringToHash("Speed");
        private static readonly int AttackHash  = Animator.StringToHash("Attack");
        private static readonly int IsHitHash   = Animator.StringToHash("IsHit");
        private static readonly int IsDeadHash  = Animator.StringToHash("IsDead");

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator   = GetComponent<Animator>();
            health     = GetComponent<HealthSystem>();

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                player         = playerGO.transform;
                playerExecutor = playerGO.GetComponent<SkillExecutor>();
            }

            health.OnDeath         += OnDeath;
            health.OnHealthChanged += OnHit;
        }

        private void Update()
        {
            if (state == State.Dead || player == null) return;

            cooldownTimer -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.position);

            UpdateState(dist);
            ApplyGravity();
            UpdateAnimator(dist);
        }

        // ── State machine ─────────────────────────────────────────────────────

        private void UpdateState(float dist)
        {
            switch (state)
            {
                case State.Idle:
                    if (dist <= detectionRange)
                        state = State.Chase;
                    break;

                case State.Chase:
                    if (dist > detectionRange)
                    {
                        state = State.Idle;
                    }
                    else if (dist <= attackRange && cooldownTimer <= 0f && !attackInProgress)
                    {
                        state = State.Attacking;
                        StartCoroutine(AttackRoutine());
                    }
                    else if (!attackInProgress)
                    {
                        MoveToward(player.position);
                    }
                    break;

                case State.Attacking:
                    FaceTarget(player.position);
                    break;
            }
        }

        private IEnumerator AttackRoutine()
        {
            attackInProgress = true;
            animator.SetTrigger(AttackHash);

            // Telegraph window — player can dodge/block
            yield return new WaitForSeconds(attackDuration * damageAtNormalized);

            // Damage check at the strike frame
            float dist = Vector3.Distance(transform.position, player.position);
            bool  inRange      = dist <= attackRange + 0.5f;
            bool  invincible   = playerExecutor != null && playerExecutor.IsInvincible;
            var   playerHealth = player.GetComponent<HealthSystem>();

            if (inRange && !invincible && playerHealth != null)
                playerHealth.TakeDamage(damage);

            // Wait for animation to finish
            yield return new WaitForSeconds(attackDuration * (1f - damageAtNormalized));

            cooldownTimer    = attackCooldown;
            attackInProgress = false;
            state            = State.Chase;
        }

        // ── Movement ──────────────────────────────────────────────────────────

        private void MoveToward(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            controller.Move(dir.normalized * moveSpeed * Time.deltaTime);
            FaceTarget(target);
        }

        private void FaceTarget(Vector3 target)
        {
            Vector3 dir = target - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                turnSpeed * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void UpdateAnimator(float dist)
        {
            float speed = (state == State.Chase && dist > attackRange) ? 0.5f : 0f;
            animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        }

        // ── Health callbacks ──────────────────────────────────────────────────

        private void OnHit(float current, float max)
        {
            // Don't trigger hit react on the killing blow — OnDeath handles that transition
            if (state != State.Dead && current > 0f)
                animator.SetTrigger(IsHitHash);
        }

        private void OnDeath()
        {
            state = State.Dead;
            StopAllCoroutines();
            animator.SetBool(IsDeadHash, true);
            controller.enabled = false;
        }

        // ── Editor gizmos ─────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
