using System;
using UnityEngine;

namespace Kaligo.Skills
{
    [Serializable]
    public abstract class SkillEffect
    {
        public abstract void OnActivate(SkillExecutor executor);
        public abstract void OnDeactivate(SkillExecutor executor);
    }
}
