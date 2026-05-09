using System.Collections;
using UnityEngine;

namespace Kaligo.Combat
{
    /// <summary>
    /// Add to any enemy to give it a persistent frontal shield.
    /// Light attacks deal reduced damage; a heavy hit OR an AOE stagger breaks the shield
    /// for a brief window so the player can burst it down.
    ///
    /// Works by setting HealthSystem.SetBlockReduction each time the shield state changes.
    /// Subscribes to the global HitboxController.OnHitLanded event and filters by proximity.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public class ShieldComponent : MonoBehaviour
    {
        [Tooltip("Damage reduction while the shield is active (0–1). 0.75 = 75% mitigation.")]
        [Range(0f, 1f)]
        [SerializeField] private float shieldReduction = 0.75f;

        [Tooltip("Seconds the shield stays broken after a heavy hit.")]
        [SerializeField] private float staggerDuration = 3f;

        [Tooltip("Maximum distance from this enemy for a hit-landed event to be considered 'landing on me'.")]
        [SerializeField] private float hitProximity = 3f;

        [Tooltip("Optional renderer whose material color tints to indicate shield/stagger state.")]
        [SerializeField] private Renderer shieldVisual;

        [Tooltip("Color shown on the shield visual while the shield is active.")]
        [SerializeField] private Color shieldActiveColor = new Color(0.4f, 0.6f, 1f, 1f);

        [Tooltip("Color shown on the shield visual while staggered.")]
        [SerializeField] private Color shieldBrokenColor = new Color(1f, 0.3f, 0.1f, 1f);

        public bool IsStaggered { get; private set; }

        private HealthSystem health;
        private Coroutine    staggerRoutine;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
            health.OnDeath += OnDeath;
        }

        private void OnEnable()
        {
            HitboxController.OnHitLanded += OnHitLanded;
            ApplyShield();
        }

        private void OnDisable()
        {
            HitboxController.OnHitLanded -= OnHitLanded;
        }

        private void OnHitLanded(Vector3 hitPoint, bool isHeavy)
        {
            if (IsStaggered) return;
            if (health.IsDead) return;

            // Only respond to hits that land near this enemy
            if (Vector3.Distance(hitPoint, transform.position) > hitProximity) return;

            if (isHeavy)
                BeginStagger();
        }

        // ── Called by AOESwingEffect (or any code) to externally break the shield ──
        public void BreakShield()
        {
            if (!IsStaggered)
                BeginStagger();
        }

        private void BeginStagger()
        {
            if (staggerRoutine != null) StopCoroutine(staggerRoutine);
            staggerRoutine = StartCoroutine(StaggerRoutine());
        }

        private IEnumerator StaggerRoutine()
        {
            IsStaggered = true;
            health.SetBlockReduction(0f);
            SetVisualColor(shieldBrokenColor);

            yield return new WaitForSeconds(staggerDuration);

            IsStaggered    = false;
            staggerRoutine = null;

            if (!health.IsDead)
                ApplyShield();
        }

        private void ApplyShield()
        {
            health.SetBlockReduction(shieldReduction);
            SetVisualColor(shieldActiveColor);
        }

        private void SetVisualColor(Color color)
        {
            if (shieldVisual != null)
                shieldVisual.material.color = color;
        }

        private void OnDeath()
        {
            if (staggerRoutine != null)
            {
                StopCoroutine(staggerRoutine);
                staggerRoutine = null;
            }
            health.SetBlockReduction(0f);
        }
    }
}
