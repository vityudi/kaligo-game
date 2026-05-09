using System;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class HealEffect : SkillEffect
    {
        [Tooltip("HP restored on activation.")]
        public float healAmount = 40f;

        [Tooltip("Optional particle effect spawned at the player's feet. Auto-destroyed after 3 s.")]
        public GameObject vfxPrefab;

        public override void OnActivate(SkillExecutor executor)
        {
            var health = executor.GetComponent<HealthSystem>();
            if (health != null) health.Heal(healAmount);

            if (vfxPrefab != null)
                UnityEngine.Object.Destroy(
                    UnityEngine.Object.Instantiate(vfxPrefab, executor.transform.position, Quaternion.identity), 3f);
        }

        public override void OnDeactivate(SkillExecutor executor) { }
    }
}
