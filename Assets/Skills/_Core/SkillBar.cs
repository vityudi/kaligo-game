using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo.Skills
{
    /// <summary>
    /// Reads input and fires skill slots on the SkillExecutor.
    ///
    /// Custom mapping:
    ///   AssignSkill(binding, skill) — put a skill in a slot.
    ///   SwapSkills(bindingA, bindingB) — swap two slots.
    ///   GetSkill(binding) — query what's in a slot.
    ///
    /// Slots marked holdToActivate activate on press and deactivate on release (Block-type skills).
    /// All other slots activate on press only (combo, dodge, jump).
    /// </summary>
    public class SkillBar : MonoBehaviour
    {
        [SerializeField] private List<SkillSlot> slots = new();
        [SerializeField] private SkillExecutor executor;

        private void Awake()
        {
            if (executor == null)
                executor = GetComponent<SkillExecutor>();
        }

        private void Update()
        {
            if (Kaligo.CursorController.Instance != null && Kaligo.CursorController.Instance.IsUIMode)
                return;

            foreach (var slot in slots)
            {
                if (slot.skill == null) continue;

                if (slot.holdToActivate)
                {
                    if (WasPressedThisFrame(slot.binding))
                        executor.TryActivate(slot.skill);
                    else if (WasReleasedThisFrame(slot.binding))
                        executor.ForceDeactivate(slot.skill);
                }
                else
                {
                    if (WasPressedThisFrame(slot.binding))
                        executor.TryActivate(slot.skill);
                }
            }
        }

        // ── Custom mapping API ────────────────────────────────────────────────

        public void AssignSkill(InputBinding binding, SkillData skill)
        {
            var slot = slots.Find(s => s.binding == binding);
            if (slot != null)
                slot.skill = skill;
            else
                slots.Add(new SkillSlot { binding = binding, skill = skill });
        }

        public void SwapSkills(InputBinding a, InputBinding b)
        {
            var slotA = slots.Find(s => s.binding == a);
            var slotB = slots.Find(s => s.binding == b);
            if (slotA == null || slotB == null) return;
            (slotA.skill, slotB.skill) = (slotB.skill, slotA.skill);
        }

        public SkillData GetSkill(InputBinding binding)
            => slots.Find(s => s.binding == binding)?.skill;

        // ── Input helpers ─────────────────────────────────────────────────────

        private static bool WasPressedThisFrame(InputBinding binding)
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            return binding switch
            {
                InputBinding.LMB   => mouse != null && mouse.leftButton.wasPressedThisFrame,
                InputBinding.RMB   => mouse != null && mouse.rightButton.wasPressedThisFrame,
                InputBinding.Space => kb != null && kb.spaceKey.wasPressedThisFrame,
                InputBinding.Key1  => kb != null && kb.digit1Key.wasPressedThisFrame,
                InputBinding.Key2  => kb != null && kb.digit2Key.wasPressedThisFrame,
                InputBinding.Key3  => kb != null && kb.digit3Key.wasPressedThisFrame,
                InputBinding.Key4  => kb != null && kb.digit4Key.wasPressedThisFrame,
                InputBinding.Key5  => kb != null && kb.digit5Key.wasPressedThisFrame,
                _                  => false,
            };
        }

        private static bool WasReleasedThisFrame(InputBinding binding)
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            return binding switch
            {
                InputBinding.LMB   => mouse != null && mouse.leftButton.wasReleasedThisFrame,
                InputBinding.RMB   => mouse != null && mouse.rightButton.wasReleasedThisFrame,
                InputBinding.Space => kb != null && kb.spaceKey.wasReleasedThisFrame,
                InputBinding.Key1  => kb != null && kb.digit1Key.wasReleasedThisFrame,
                InputBinding.Key2  => kb != null && kb.digit2Key.wasReleasedThisFrame,
                InputBinding.Key3  => kb != null && kb.digit3Key.wasReleasedThisFrame,
                InputBinding.Key4  => kb != null && kb.digit4Key.wasReleasedThisFrame,
                InputBinding.Key5  => kb != null && kb.digit5Key.wasReleasedThisFrame,
                _                  => false,
            };
        }
    }
}
