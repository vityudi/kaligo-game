using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Kaligo.Items;
using Kaligo.Skills;
using Kaligo.Services;

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

        [Header("Rewards")]
        [SerializeField] private int       xpReward  = 75;
        [SerializeField] private LootTable lootTable;

        // Auto-assign BasicEnemyLootTable in the Editor so the field is
        // never accidentally left blank after a recompile.
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lootTable == null)
                lootTable = UnityEditor.AssetDatabase.LoadAssetAtPath<LootTable>(
                    "Assets/Items/LootTables/BasicEnemyLootTable.asset");
        }
#endif

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

        private float   verticalVelocity;
        private float   cooldownTimer;
        private bool    attackInProgress;
        private Vector3 knockbackVelocity;

        private const float KnockbackDecay = 12f;

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
            ApplyKnockbackDecay();
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

        public void ApplyKnockback(Vector3 impulse)
        {
            if (state == State.Dead) return;
            knockbackVelocity = impulse;
        }

        private void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.sqrMagnitude < 0.01f) return;
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero,
                KnockbackDecay * Time.deltaTime);
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

            // Freeze any Rigidbodies so the body doesn't fall through the floor
            foreach (var rb in GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;

            // Grant XP
            if (xpReward > 0 && GameServices.Progression != null)
            {
                GameServices.Progression.GrantXP(xpReward);
                SpawnXPFeedback();
            }

            // Resolve loot table: Inspector field → Resources folder → nothing.
            var resolvedTable = lootTable
                ?? Resources.Load<LootTable>("BasicEnemyLootTable");

            if (resolvedTable == null)
                Debug.LogWarning($"[EnemyAI] {name}: no LootTable found — body will be empty.");

            var drops = resolvedTable != null
                ? resolvedTable.Roll()
                : new List<(ItemData, int)>();

            Debug.Log($"[EnemyAI] {name}: loot roll → {drops.Count} item(s).");

            var container = gameObject.AddComponent<LootContainer>();
            container.Initialize(drops);

            StartCoroutine(DeathFallRoutine());
        }

        /// <summary>
        /// Lets the death clip start, then disables the Animator and smoothly
        /// rotates the body to lie flat on the ground.
        /// The Y position is locked throughout so the body never sinks below
        /// the surface regardless of physics settings.
        /// </summary>
        private IEnumerator DeathFallRoutine()
        {
            // Give the death animation a moment to begin
            yield return new WaitForSeconds(0.55f);

            animator.enabled = false;

            // Record ground-level Y so the body can't drift underground
            float groundY = transform.position.y;

            const float duration = 0.55f;
            float elapsed = 0f;

            Quaternion startRot = transform.rotation;
            // Fall to the right relative to the character's facing direction
            Quaternion endRot   = transform.rotation * Quaternion.Euler(0f, 0f, -90f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.rotation = Quaternion.Lerp(startRot, endRot, t);

                // Keep the body glued to the surface
                var pos = transform.position;
                pos.y = groundY;
                transform.position = pos;

                yield return null;
            }
            transform.rotation = endRot;

            // Final snap — ensure nothing slipped
            var finalPos = transform.position;
            finalPos.y   = groundY;
            transform.position = finalPos;
        }

        // ── XP feedback ───────────────────────────────────────────────────────

        private void SpawnXPFeedback()
        {
            var go = new GameObject("XPFeedback");
            go.transform.position = transform.position + Vector3.up * 2.8f;
            var tmp          = go.AddComponent<TextMeshPro>();
            tmp.text         = $"+{xpReward} XP";
            tmp.color        = new Color(0.4f, 1f, 0.4f);
            tmp.fontSize     = 5f;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.sortingOrder = 10;
            go.AddComponent<XPFeedbackFloat>();
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

    // ── Floating XP feedback ───────────────────────────────────────────────────

    internal class XPFeedbackFloat : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private float       _t;
        private const float Duration = 2.0f;
        private const float Speed    = 0.9f;

        private void Awake() => _tmp = GetComponent<TextMeshPro>();

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * Speed * Time.deltaTime;

            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            float alpha = Mathf.Clamp01(1f - _t / Duration);
            _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, alpha);

            if (_t >= Duration) Destroy(gameObject);
        }
    }
}
