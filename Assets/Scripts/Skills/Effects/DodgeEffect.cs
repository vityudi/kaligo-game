using System;
using UnityEngine;

namespace Kaligo.Skills.Effects
{
    [Serializable]
    public class DodgeEffect : SkillEffect
    {
        [Tooltip("Speed burst applied in the current movement direction (m/s).")]
        public float impulseForce = 10f;

        [Tooltip("Duration of invincibility frames during the dodge (seconds).")]
        public float iFrameDuration = 0.4f;

        public override void OnActivate(SkillExecutor executor)
        {
            executor.StartDodge(impulseForce, iFrameDuration);
        }

        public override void OnDeactivate(SkillExecutor executor)
        {
            executor.StopDodge();
        }
    }
}
