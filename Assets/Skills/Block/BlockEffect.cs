using System;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class BlockEffect : SkillEffect
    {
        [Tooltip("Fraction of incoming damage absorbed while blocking (0 = none, 1 = all).")]
        [Range(0f, 1f)] public float damageReduction = 0.8f;

        public override void OnActivate(SkillExecutor executor)
        {
            executor.Animator.SetBool("IsBlocking", true);
            executor.StartBlock(damageReduction);
            executor.FaceCameraAlways = true;
        }

        public override void OnDeactivate(SkillExecutor executor)
        {
            executor.Animator.SetBool("IsBlocking", false);
            executor.StopBlock();
            executor.FaceCameraAlways = false;
        }
    }
}
