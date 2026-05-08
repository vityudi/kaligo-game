using System.Collections.Generic;
using UnityEngine;

namespace Kaligo.Skills
{
    [CreateAssetMenu(menuName = "Kaligo/Skill", fileName = "NewSkill")]
    public class SkillData : ScriptableObject
    {
        public string skillName;
        public Sprite icon;

        [Tooltip("Seconds before this skill can be activated again after the last step completes.")]
        public float cooldown = 0f;

        [Tooltip("Stamina cost on activation.")]
        public float staminaCost = 0f;

        [Tooltip("Fraction of normal movement speed while this skill is executing (1 = full speed, 0 = stop). Attack skills ~0.35, Block = 0.")]
        [Range(0f, 1f)] public float movementMultiplier = 1f;

        [Tooltip("Seconds from skill start during which it cannot be interrupted by another skill. 0 = always cancellable.")]
        public float cancelLockDuration = 0f;

        [Tooltip("Prevent the character from rotating to face the movement direction while this skill is executing. Enable for attacks.")]
        public bool lockRotation = false;

        [Tooltip("Ordered list of steps. Pressing the activation key during a step's combo window chains to the next step.")]
        public List<SkillStep> steps = new();
    }
}
