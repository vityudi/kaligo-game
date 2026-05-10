using System;
using UnityEngine;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class HitboxEffect : SkillEffect
    {
        [Tooltip("Damage dealt to any HealthSystem this hitbox touches during the step.")]
        public float damage = 20f;

        [Tooltip("Mark as heavy attack — applies stronger knockback, longer hit-stop, and bigger screen shake.")]
        public bool isHeavyAttack = false;

        public override void OnActivate(SkillExecutor executor)
        {
            executor.Hitbox?.Enable(damage, isHeavyAttack);
        }

        public override void OnDeactivate(SkillExecutor executor)
        {
            executor.Hitbox?.Disable();
        }
    }
}
