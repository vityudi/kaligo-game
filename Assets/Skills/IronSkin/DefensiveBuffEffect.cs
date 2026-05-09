using System;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Skills.Effects
{
    /// <summary>
    /// Activates a damage-reduction buff for the duration of the skill step.
    /// Pair with a SkillStep whose duration equals the desired buff window.
    /// </summary>
    [Serializable]
    public class DefensiveBuffEffect : SkillEffect
    {
        [Tooltip("Fraction of incoming damage absorbed while the buff is active (0–1). 0.5 = 50% reduction.")]
        [Range(0f, 1f)]
        public float damageReduction = 0.5f;

        public override void OnActivate(SkillExecutor executor)
        {
            var health = executor.GetComponent<HealthSystem>();
            if (health != null) health.SetBlockReduction(damageReduction);
        }

        public override void OnDeactivate(SkillExecutor executor)
        {
            var health = executor.GetComponent<HealthSystem>();
            if (health != null) health.SetBlockReduction(0f);
        }
    }
}
