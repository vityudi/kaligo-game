using System;
using System.Collections.Generic;
using UnityEngine;
using Kaligo.Characters;

namespace Kaligo.Combat
{
    /// <summary>
    /// Attach to the weapon GameObject (or a trigger child of it).
    /// SkillExecutor calls Enable/Disable to open and close the damage window.
    /// Each collider can only be hit once per swing to prevent multi-tick damage.
    ///
    /// On a confirmed hit this component:
    ///   • Applies damage via HealthSystem.TakeDamage
    ///   • Pushes the target via EnemyAI.ApplyKnockback
    ///   • Spawns an optional hit-particle prefab at the impact point
    ///   • Plays an optional impact AudioClip via AudioSource.PlayClipAtPoint
    ///   • Fires the static OnHitLanded event so CombatFeedback can run
    ///     hit-stop and screen shake without a hard dependency on this class
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitboxController : MonoBehaviour
    {
        [Header("Impact VFX")]
        [Tooltip("Particle prefab instantiated at the hit point. Auto-destroyed after 3 s. Optional.")]
        [SerializeField] private GameObject hitParticlePrefab;

        [Header("Impact SFX")]
        [Tooltip("Clip played at the impact point for light attacks.")]
        [SerializeField] private AudioClip hitSfxLight;

        [Tooltip("Clip played at the impact point for heavy attacks.")]
        [SerializeField] private AudioClip hitSfxHeavy;

        [Header("Knockback")]
        [Tooltip("Horizontal push applied to the struck enemy on a light hit.")]
        [SerializeField] private float lightKnockbackForce = 3f;

        [Tooltip("Horizontal push applied to the struck enemy on a heavy hit.")]
        [SerializeField] private float heavyKnockbackForce = 7f;

        // ── Global event ──────────────────────────────────────────────────────
        // (worldPosition, isHeavy) — CombatFeedback subscribes to this
        public static event Action<Vector3, bool> OnHitLanded;

        // ── State ─────────────────────────────────────────────────────────────

        private Collider hitCollider;
        private float    currentDamage;
        private bool     currentIsHeavy;
        private readonly HashSet<Collider> hitThisSwing = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            hitCollider.isTrigger = true;
            hitCollider.enabled   = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Enable(float damage, bool isHeavy = false)
        {
            float multiplier = PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : 1f;
            currentDamage  = damage * multiplier;
            currentIsHeavy = isHeavy;
            hitThisSwing.Clear();
            hitCollider.enabled = true;
        }

        public void Disable()
        {
            hitCollider.enabled = false;
        }

        // ── Hit detection ─────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!hitCollider.enabled)         return;
            if (hitThisSwing.Contains(other)) return;

            var health = other.GetComponentInParent<HealthSystem>();
            if (health == null)                                   return;
            if (health.gameObject == transform.root.gameObject)   return;

            hitThisSwing.Add(other);
            health.TakeDamage(currentDamage);

            // Geometry for knockback / particles
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 knockDir = other.transform.position - transform.position;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude > 0.001f) knockDir.Normalize();

            // Knockback
            float kbForce = currentIsHeavy ? heavyKnockbackForce : lightKnockbackForce;
            other.GetComponentInParent<EnemyAI>()?.ApplyKnockback(knockDir * kbForce);

            // Hit particle
            if (hitParticlePrefab != null)
            {
                var rotation = knockDir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(-knockDir)
                    : Quaternion.identity;
                Destroy(Instantiate(hitParticlePrefab, hitPoint, rotation), 3f);
            }

            // Impact SFX
            var clip = currentIsHeavy ? hitSfxHeavy : hitSfxLight;
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, hitPoint);

            // Broadcast for hit-stop + screen shake
            OnHitLanded?.Invoke(hitPoint, currentIsHeavy);
        }
    }
}
