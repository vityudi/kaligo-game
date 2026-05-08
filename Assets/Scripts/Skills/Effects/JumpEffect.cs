using System;
using UnityEngine;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class JumpEffect : SkillEffect
    {
        [Tooltip("Upward velocity applied on jump (m/s). ~8 feels good at gravity -20.")]
        public float force = 8f;

        public override void OnActivate(SkillExecutor executor)
        {
            executor.Jump(force);
        }

        public override void OnDeactivate(SkillExecutor executor) { }
    }
}
