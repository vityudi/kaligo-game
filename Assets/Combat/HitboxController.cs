using System;
using System.Collections.Generic;
using UnityEngine;
using Kaligo.Characters;
using Kaligo.Mobs;

namespace Kaligo.Combat
{
    [RequireComponent(typeof(Collider))]
    public class HitboxController : MonoBehaviour
    {
        [Header("Impact VFX")]
        [SerializeField] private GameObject hitParticlePrefab;

        [Header("Impact SFX")]
        [SerializeField] private AudioClip hitSfxLight;
        [SerializeField] private AudioClip hitSfxHeavy;

        [Header("Knockback")]
        [SerializeField] private float lightKnockbackForce = 4f;
        [SerializeField] private float heavyKnockbackForce = 9f;

        public static event Action<Vector3, bool> OnHitLanded;

        private Collider hitCollider;
        private float    currentDamage;
        private bool     currentIsHeavy;
        private readonly HashSet<Collider> hitThisSwing = new();

        private void Awake()
        {
            hitCollider           = GetComponent<Collider>();
            hitCollider.isTrigger = true;
            hitCollider.enabled   = false;
        }

        public void Enable(float damage, bool isHeavy = false)
        {
            float mult    = PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : 1f;
            currentDamage  = damage * mult;
            currentIsHeavy = isHeavy;
            hitThisSwing.Clear();
            hitCollider.enabled = true;
        }

        public void Disable()
        {
            hitCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!hitCollider.enabled)         return;
            if (hitThisSwing.Contains(other)) return;

            var health = other.GetComponent<HealthSystem>()
                      ?? other.GetComponentInParent<HealthSystem>();
            if (health == null)                                 return;
            if (health.gameObject == transform.root.gameObject) return;

            hitThisSwing.Add(other);
            health.TakeDamage(currentDamage);

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 knockDir = other.transform.position - transform.position;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude > 0.001f) knockDir.Normalize();

            float kbForce = currentIsHeavy ? heavyKnockbackForce : lightKnockbackForce;
            var mobBrain = other.GetComponentInParent<MobBrain>();
            if (mobBrain != null)
                mobBrain.ApplyKnockback(knockDir * kbForce);
            else
                other.GetComponentInParent<EnemyAI>()?.ApplyKnockback(knockDir * kbForce);

            DamageNumberSpawner.Spawn(hitPoint, currentDamage, currentIsHeavy);

            if (hitParticlePrefab != null)
            {
                var rot = knockDir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(-knockDir)
                    : Quaternion.identity;
                Destroy(Instantiate(hitParticlePrefab, hitPoint, rot), 3f);
            }

            var clip = currentIsHeavy ? hitSfxHeavy : hitSfxLight;
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, hitPoint);

            OnHitLanded?.Invoke(hitPoint, currentIsHeavy);
        }
    }
}
