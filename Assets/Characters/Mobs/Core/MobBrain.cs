using System.Collections;
using UnityEngine;
using Kaligo.Combat;
using Kaligo.Services;

namespace Kaligo.Mobs
{
    /// <summary>
    /// Abstract base for all mob AI.
    /// Handles: gravity, knockback, health wiring, hit-flash, XP grant, corpse despawn.
    /// Derived classes implement Think() for their state machine.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public abstract class MobBrain : MonoBehaviour
    {
        [Header("Mob Data")]
        [SerializeField] protected MobDefinition definition;

        protected CharacterController controller;
        protected Animator            animator;
        protected HealthSystem        health;

        public bool IsDead { get; private set; }

        private float   verticalVelocity;
        private Vector3 knockbackVelocity;
        private const float KnockbackDecay = 10f;
        private const float Gravity        = -22f;

        private Renderer[] _renderers;
        private Color[]    _originalColors;
        private Coroutine  _flashRoutine;
        private static readonly Color FlashColor = new Color(1f, 0.25f, 0.25f, 1f);

        protected static readonly int HashSpeed  = Animator.StringToHash("Speed");
        protected static readonly int HashIsDead = Animator.StringToHash("IsDead");
        protected static readonly int HashIsHit  = Animator.StringToHash("IsHit");
        protected static readonly int HashAttack = Animator.StringToHash("Attack");

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator   = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            health     = GetComponent<HealthSystem>();

            if (definition != null)
                health.SetMaxHealth(definition.maxHealth);

            health.OnDeath         += HandleDeath;
            health.OnHealthChanged += HandleHit;

            _renderers     = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].sharedMaterial;
                _originalColors[i] = (mat != null && mat.HasProperty("_BaseColor"))
                    ? mat.GetColor("_BaseColor") : Color.white;
            }
        }

        public void Initialize(MobDefinition def)
        {
            definition = def;
            if (health != null && def != null)
                health.SetMaxHealth(def.maxHealth);
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            ApplyGravity();
            ApplyKnockbackDecay();
            Think();
        }

        protected abstract void Think();

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
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            controller.Move(dir.normalized * speed * Time.deltaTime);
            FaceDirection(dir);
        }

        protected void FaceTarget(Vector3 target)
        {
            FaceDirection((target - transform.position).WithY(0f));
        }

        private void FaceDirection(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            float ts = definition != null ? definition.turnSpeed : 360f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir), ts * Time.deltaTime);
        }

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

        protected void AnimSetFloat(int hash, float value, float damp = 0.05f)
        {
            if (animator != null) animator.SetFloat(hash, value, damp, Time.deltaTime);
        }

        protected void AnimSetBool(int hash, bool value)
        {
            if (animator != null) animator.SetBool(hash, value);
        }

        protected void AnimTrigger(int hash)
        {
            if (animator != null) animator.SetTrigger(hash);
        }

        protected virtual void HandleHit(float current, float max)
        {
            if (!IsDead && current > 0f)
            {
                AnimTrigger(HashIsHit);
                TriggerHitFlash();
            }
            OnHit(current, max);
        }

        protected virtual void OnHit(float current, float max) { }

        private void HandleDeath()
        {
            IsDead = true;
            StopAllCoroutines();
            AnimSetBool(HashIsDead, true);
            AnimSetFloat(HashSpeed, 0f, 0f);
            controller.enabled = false;
            if (definition != null && definition.xpReward > 0)
                GameServices.Progression?.GrantXP(definition.xpReward);
            OnDeath();
            StartCoroutine(DespawnAfter(8f));
        }

        protected virtual void OnDeath() { }

        private IEnumerator DespawnAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private void TriggerHitFlash()
        {
            if (_renderers == null || _renderers.Length == 0) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                foreach (var mat in _renderers[i].materials)
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", FlashColor);
            }
            yield return new WaitForSeconds(0.08f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                foreach (var mat in _renderers[i].materials)
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", _originalColors[i]);
            }
            _flashRoutine = null;
        }

        protected float DistanceTo(Vector3 pos) =>
            new Vector2(transform.position.x - pos.x, transform.position.z - pos.z).magnitude;
    }

    internal static class Vector3Extensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
