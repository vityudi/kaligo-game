using System.Collections;
using UnityEngine;
using Kaligo.Combat;
using Kaligo.Services;

namespace Kaligo.Mobs
{
    /// <summary>
    /// Abstract base for all mob AI.
    ///
    /// Responsibilities:
    ///   • Gravity + grounding
    ///   • Knockback impulse + decay
    ///   • Health wiring (hit react / death)
    ///   • XP grant on death
    ///   • Corpse despawn
    ///
    /// Derived classes implement <see cref="Think"/> for their state machine.
    /// The mob does NOT need an Animator — all animator calls are null-guarded.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public abstract class MobBrain : MonoBehaviour
    {
        [Header("Mob Data")]
        [SerializeField] protected MobDefinition definition;

        // ── References ────────────────────────────────────────────────────────
        protected CharacterController controller;
        protected Animator            animator;   // may be null
        protected HealthSystem        health;

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsDead { get; private set; }

        // ── Physics ───────────────────────────────────────────────────────────
        private float   verticalVelocity;
        private Vector3 knockbackVelocity;
        private const float KnockbackDecay = 12f;
        private const float Gravity        = -20f;

        // ── Animator hashes (guarded — mobs without animators just skip) ──────
        protected static readonly int HashSpeed  = Animator.StringToHash("Speed");
        protected static readonly int HashIsDead = Animator.StringToHash("IsDead");
        protected static readonly int HashIsHit  = Animator.StringToHash("IsHit");
        protected static readonly int HashAttack = Animator.StringToHash("Attack");

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator   = GetComponent<Animator>(); // optional
            health     = GetComponent<HealthSystem>();

            if (definition != null)
                health.SetMaxHealth(definition.maxHealth);

            health.OnDeath         += HandleDeath;
            health.OnHealthChanged += HandleHit;
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            ApplyGravity();
            ApplyKnockbackDecay();
            Think();
        }

        /// <summary>
        /// Implement the AI state machine here in derived classes.
        /// Called every frame while alive.
        /// </summary>
        protected abstract void Think();

        // ── Movement helpers ──────────────────────────────────────────────────

        protected void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = (target - transform.position).WithY(0f);
            if (dir.sqrMagnitude < 0.01f) return;
            controller.Move(dir.normalized * speed * Time.deltaTime);
            FaceDirection(dir);
        }

        protected void MoveAwayFrom(Vector3 threat, float speed)
        {
            Vector3 dir = (transform.position - threat).WithY(0f);
            if (dir.sqrMagnitude < 0.01f)
                dir = transform.forward; // fallback: keep going
            controller.Move(dir.normalized * speed * Time.deltaTime);
            FaceDirection(dir);
        }

        protected void FaceTarget(Vector3 target)
        {
            Vector3 dir = (target - transform.position).WithY(0f);
            FaceDirection(dir);
        }

        private void FaceDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            float ts = (definition != null ? definition.turnSpeed : 360f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                ts * Time.deltaTime);
        }

        /// <summary>Apply an external force impulse (e.g. knockback from player hit).</summary>
        public void ApplyKnockback(Vector3 impulse)
        {
            if (IsDead) return;
            knockbackVelocity = impulse;
        }

        private void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.sqrMagnitude < 0.01f) return;
            controller.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.MoveTowards(
                knockbackVelocity, Vector3.zero, KnockbackDecay * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += Gravity * Time.deltaTime;

            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        // ── Animator helpers ──────────────────────────────────────────────────

        protected void AnimSetFloat(int hash, float value, float damp = 0.1f)
        {
            if (animator != null)
                animator.SetFloat(hash, value, damp, Time.deltaTime);
        }

        protected void AnimSetBool(int hash, bool value)
        {
            if (animator != null)
                animator.SetBool(hash, value);
        }

        protected void AnimTrigger(int hash)
        {
            if (animator != null)
                animator.SetTrigger(hash);
        }

        // ── Health callbacks ──────────────────────────────────────────────────

        protected virtual void HandleHit(float current, float max)
        {
            if (!IsDead && current > 0f)
                AnimTrigger(HashIsHit);

            OnHit(current, max);
        }

        /// <summary>Override to react to damage in derived classes (e.g. trigger flee).</summary>
        protected virtual void OnHit(float current, float max) { }

        private void HandleDeath()
        {
            IsDead = true;
            StopAllCoroutines();
            AnimSetBool(HashIsDead, true);
            controller.enabled = false;

            // Grant XP
            if (definition != null && definition.xpReward > 0)
                GameServices.Progression?.GrantXP(definition.xpReward);

            OnDeath();
            StartCoroutine(DespawnAfter(8f));
        }

        /// <summary>Override in derived class for death side effects (stop coroutines, etc.).</summary>
        protected virtual void OnDeath() { }

        private IEnumerator DespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>How far is this mob from a given position (XZ plane only)?</summary>
        protected float DistanceTo(Vector3 pos) =>
            new Vector2(transform.position.x - pos.x, transform.position.z - pos.z).magnitude;
    }

    /// <summary>Extension so we can write <c>v.WithY(0f)</c> instead of the verbose alternative.</summary>
    internal static class Vector3Extensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
