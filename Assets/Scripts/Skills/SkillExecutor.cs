using System.Collections;
using UnityEngine;
using Kaligo.Combat;

namespace Kaligo.Skills
{
    [RequireComponent(typeof(Animator))]
    public class SkillExecutor : MonoBehaviour
    {
        [SerializeField] private HitboxController hitbox;

        public HitboxController Hitbox             => hitbox;
        public Animator         Animator           => animator;
        public bool             IsExecuting        => executionRoutine != null;
        public bool             IsInvincible       { get; private set; }
        public float            MovementMultiplier { get; private set; } = 1f;
        public bool             LockRotation       { get; private set; }

        private float skillStartTime;

        private Animator          animator;
        private PlayerController  playerController;
        private StaminaSystem     stamina;
        private SkillData         currentSkill;
        private int               currentStep;
        private bool              inComboWindow;
        private bool              nextStepQueued;
        private Coroutine         executionRoutine;
        private Coroutine         iFrameRoutine;
        private float             cooldownRemaining;

        private void Awake()
        {
            animator         = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            stamina          = GetComponent<StaminaSystem>();
        }

        private void Update()
        {
            if (cooldownRemaining > 0f)
                cooldownRemaining -= Time.deltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool TryActivate(SkillData skill)
        {
            if (skill == null || skill.steps.Count == 0) return false;
            if (cooldownRemaining > 0f) return false;

            if (stamina != null && skill.staminaCost > 0f && !stamina.TrySpend(skill.staminaCost))
                return false;

            if (currentSkill == skill && inComboWindow && currentStep + 1 < skill.steps.Count)
            {
                nextStepQueued = true;
                return true;
            }

            if (!IsExecuting)
            {
                executionRoutine = StartCoroutine(ExecuteSkill(skill));
                return true;
            }

            // Interrupt current skill once its cancel-lock window has passed
            if (Time.time - skillStartTime >= currentSkill.cancelLockDuration)
            {
                ForceDeactivate(currentSkill);
                executionRoutine = StartCoroutine(ExecuteSkill(skill));
                return true;
            }

            return false;
        }

        /// <summary>Stop the running skill immediately (used by hold-type slots on key release).</summary>
        public void ForceDeactivate(SkillData skill)
        {
            if (currentSkill != skill) return;

            if (executionRoutine != null)
            {
                StopCoroutine(executionRoutine);
                executionRoutine = null;
            }

            if (currentSkill != null && currentStep < currentSkill.steps.Count)
            {
                foreach (var effect in currentSkill.steps[currentStep].effects)
                    effect.OnDeactivate(this);
            }

            MovementMultiplier = 1f;
            LockRotation       = false;
            currentSkill       = null;
        }

        // ── Execution coroutine ───────────────────────────────────────────────

        private IEnumerator ExecuteSkill(SkillData skill)
        {
            currentSkill       = skill;
            currentStep        = 0;
            skillStartTime     = Time.time;
            MovementMultiplier = skill.movementMultiplier;
            LockRotation       = skill.lockRotation;

            while (currentStep < skill.steps.Count)
            {
                var step = skill.steps[currentStep];
                nextStepQueued = false;
                inComboWindow  = false;

                // Snap to camera before the trigger so the character faces the right direction.
                // We snap AGAIN after the transition frames (below) because OnAnimatorMove can
                // drift the rotation during the animator blend-out of the previous state.
                if (skill.lockRotation)
                    SnapToCameraForward();

                if (!string.IsNullOrEmpty(step.animatorTrigger))
                    animator.SetTrigger(step.animatorTrigger);

                foreach (var effect in step.effects)
                    effect.OnActivate(this);

                // Wait for the animator transition to settle (~2 frames at 60 fps)
                yield return null;
                yield return null;

                // Re-snap: clears any rotation drift accumulated during the blend transition
                if (skill.lockRotation)
                    SnapToCameraForward();

                float elapsed = 0f;

                while (elapsed < step.duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / step.duration;

                    inComboWindow = t >= step.comboWindowStart && t <= step.comboWindowEnd;

                    if (nextStepQueued && inComboWindow)
                        break;

                    yield return null;
                }

                inComboWindow = false;

                foreach (var effect in step.effects)
                    effect.OnDeactivate(this);

                if (nextStepQueued && currentStep + 1 < skill.steps.Count)
                    currentStep++;
                else
                    break;
            }

            cooldownRemaining  = skill.cooldown;
            MovementMultiplier = 1f;
            LockRotation       = false;
            currentSkill       = null;
            executionRoutine   = null;
        }

        // ── Hooks called by effect types ──────────────────────────────────────

        public void Jump(float force)
        {
            playerController?.ApplyJumpImpulse(force);
        }

        public void StartDodge(float impulseForce, float iFrameDuration)
        {
            playerController?.ApplyDodgeImpulse(impulseForce);

            if (iFrameRoutine != null) StopCoroutine(iFrameRoutine);
            iFrameRoutine = StartCoroutine(IFrames(iFrameDuration));
        }

        public void StopDodge() { }

        public void StartBlock(float damageReduction)
        {
            var health = GetComponent<HealthSystem>();
            if (health != null) health.SetBlockReduction(damageReduction);
        }

        public void StopBlock()
        {
            var health = GetComponent<HealthSystem>();
            if (health != null) health.SetBlockReduction(0f);
        }

        private IEnumerator IFrames(float duration)
        {
            IsInvincible = true;
            yield return new WaitForSeconds(duration);
            IsInvincible = false;
            iFrameRoutine = null;
        }

        private void SnapToCameraForward()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward.normalized);
        }
    }
}
