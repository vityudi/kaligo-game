using System.Collections.Generic;
using UnityEngine;

namespace Kaligo.Combat
{
    /// <summary>
    /// Attach to the weapon GameObject (or a trigger child of it).
    /// SkillExecutor calls Enable/Disable to open and close the damage window.
    /// Each collider can only be hit once per swing to prevent multi-tick damage.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HitboxController : MonoBehaviour
    {
        private Collider hitCollider;
        private float currentDamage;
        private readonly HashSet<Collider> hitThisSwing = new();

        private void Awake()
        {
            hitCollider = GetComponent<Collider>();
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }

        public void Enable(float damage)
        {
            currentDamage = damage;
            hitThisSwing.Clear();
            hitCollider.enabled = true;
        }

        public void Disable()
        {
            hitCollider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!hitCollider.enabled) return;
            if (hitThisSwing.Contains(other)) return;

            var health = other.GetComponentInParent<HealthSystem>();
            if (health == null) return;

            // Don't hit the owner
            if (health.gameObject == transform.root.gameObject) return;

            hitThisSwing.Add(other);
            health.TakeDamage(currentDamage);
        }
    }
}
