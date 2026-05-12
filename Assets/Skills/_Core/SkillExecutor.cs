using System.Collections;
using System.Collections.Generic;
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
        /// <summary>While true, the character continuously rotates to face the camera's horizontal forward.</summary>
        public bool             FaceCameraAlways   { get; set; }

        private float skillStartTime;

        private Animator          animator;
        private PlayerController  playerController;
        private StaminaSystem     stamina;
        private ManaSystem        mana;
        private SkillData         currentSkill;
        private int               currentStep;
        private bool              inComboWindow;
        private bool              nextStepQueued;
        private Coroutine         executionRoutine;
        private Coroutine         iFrameRoutine;

        // Per-skill independent cooldown tracking
        private readonly Dictionary<SkillData, float> cooldowns = new();

        private void Awake()
        {
            animator         = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();
            stamina          = GetComponent<StaminaSystem>();
            mana             = GetComponent<ManaSystem>();
        }

        private void Update()
        {
            if (cooldowns.Count == 0) return;
            var keys = new List<SkillData>(cooldowns.Keys);
            foreach (var skill in keys)
            {
                cooldowns[skill] -= Time.deltaTime;
                if (cooldowns[skill] <= 0f)
                    cooldowns.Remove(skill);
            }
        }

        // ── Cooldown query for UI ─────────────────────────────────────────────

        /// <summary>Remaining cooldown in seconds (0 if ready).</summary>
        public float GetCooldownRemaining(SkillData skill)
        {
            return (skill != null && cooldowns.TryGetValue(skill, out float remaining))
                ? Mathf.Max(0f, remaining) : 0f;
        }

        /// <summary>0 = ready, 1 = full cooldown. Safe to use as radial fill amount.</summary>
        public float GetCooldownFraction(SkillData skill)
        {
            if (skill == null || skill.cooldown <= 0f) return 0f;
            return GetCooldownRemaining(skill) / skill.cooldown;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public bool TryActivate(SkillData skill)
        {
            if (skill == null || skill.steps.Count == 0) return false;
            if (cooldowns.ContainsKey(skill) && cooldowns[skill] > 0f) return false;

            if (stamina != null && skill.staminaCost > 0f && !stamina.TrySpend(skill.staminaCost))
                return false;

            if (mana != null && skill.manaCost > 0f && !mana.TrySpend(skill.manaCost))
                return false;

            if (currentSkill == skill && inComboWindow && currentStep + 1 < skill.steps.Count)
            {
                nextStepQueued = true;
                return true;
            }

            // Same skill re-pressed outside the combo window: ignore rather than restarting.
            // Without this guard the cancel-lock check below would restart the combo on any
            // early LMB press (>0.25 s in), making it appear as if only 1 combo step exists.
            if (currentSkill == skill && IsExecuting)
                return false;

            if (!IsExecuting)
            {
                executionRoutine = StartCoroutine(ExecuteSkill(skill));
                return true;
            }

            // Interrupt a DIFFERENT skill once its cancel-lock window has passed
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

                if (skill.lockRotation)
                    SnapToCameraForward();

                if (!string.IsNullOrEmpty(step.animatorTrigger))
                    animator.SetTrigger(step.animatorTrigger);

                foreach (var effect in step.effects)
                    effect.OnActivate(this);

                yield return null;
                yield return null;

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

            if (skill.cooldown > 0f)
                cooldowns[skill] = skill.cooldown;

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

        public void DashForward(float force)
        {
            playerController?.ApplyDashForward(force);
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
            // CameraTarget child stores the pure orbit yaw written by CameraOrbitInput.
            // The main camera Y angle is wrong — it faces *toward* the player and drifts with shoulder offset.
            var pivot = transform.Find("CameraTarget");
            float yaw = pivot != null ? pivot.eulerAngles.y : (Camera.main != null ? Camera.main.transform.eulerAngles.y : transform.eulerAngles.y);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
