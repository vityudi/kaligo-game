using System;
using UnityEngine;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class DashStrikeEffect : SkillEffect
    {
        [Tooltip("Forward dash impulse (m/s).")]
        public float dashForce = 18f;

        [Tooltip("Damage dealt on contact.")]
        public float damage = 40f;

        public override void OnActivate(SkillExecutor executor)
        {
            executor.DashForward(dashForce);
            executor.Hitbox.Enable(damage, false);
        }

        public override void OnDeactivate(SkillExecutor executor)
        {
            executor.Hitbox.Disable();
        }
    }
}
