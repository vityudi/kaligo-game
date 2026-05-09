using System;
using UnityEngine;

namespace Kaligo.Skills
{
    [Serializable]
    public class SkillSlot
    {
        public InputBinding binding;
        public SkillData skill;

        [Tooltip("If true, the skill stays active while the key is held and deactivates on release. Use for Block-type skills.")]
        public bool holdToActivate;
    }
}
