using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kaligo.Skills
{
    [Serializable]
    public class SkillStep
    {
        [Tooltip("Animator trigger name set when this step begins.")]
        public string animatorTrigger;

        [Tooltip("How long this step lasts in seconds (should match animation clip length).")]
        public float duration = 0.5f;

        [Tooltip("Normalized time (0-1) within the step when the combo window opens — pressing the skill again during this window chains to the next step.")]
        [Range(0f, 1f)] public float comboWindowStart = 0.4f;

        [Tooltip("Normalized time (0-1) within the step when the combo window closes.")]
        [Range(0f, 1f)] public float comboWindowEnd = 0.9f;

        [SerializeReference]
        public List<SkillEffect> effects = new();
    }
}
