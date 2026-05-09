using System;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class AOESwingEffect : SkillEffect
    {
        [Tooltip("Radius of the damage area around the player.")]
        public float radius = 4f;

        [Tooltip("Damage dealt to every enemy in range.")]
        public float damage = 55f;

        [Tooltip("Knockback force applied to each struck enemy.")]
        public float knockbackForce = 10f;

        [Tooltip("Layer mask used to detect enemy colliders.")]
        public LayerMask enemyLayer = ~0; // all layers by default; narrow in the Inspector

        public override void OnActivate(SkillExecutor executor)
        {
            var hits = Physics.OverlapSphere(executor.transform.position, radius, enemyLayer);
            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<HealthSystem>();
                if (health == null || health.gameObject == executor.gameObject) continue;

                health.TakeDamage(damage);

                var ai = hit.GetComponentInParent<EnemyAI>();
                if (ai != null)
                {
                    Vector3 dir = (hit.transform.position - executor.transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f) dir.Normalize();
                    ai.ApplyKnockback(dir * knockbackForce);
                }
            }
        }

        public override void OnDeactivate(SkillExecutor executor) { }
    }
}
