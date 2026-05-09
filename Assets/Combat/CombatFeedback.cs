using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Kaligo.Combat
{
    /// <summary>
    /// Subscribes to HitboxController.OnHitLanded and freezes Time.timeScale briefly on
    /// each confirmed hit (hit-stop). Screen shake is intentionally off — the CinemachineImpulseSource
    /// is kept so it can be re-enabled from the Inspector (set shakeForce > 0) if desired.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CombatFeedback : MonoBehaviour
    {
        [Header("Hit-Stop")]
        [Tooltip("Seconds the game freezes on a light hit (~0.06 = 3-4 frames at 60 fps).")]
        [SerializeField] private float lightHitStopDuration = 0.06f;

        [Tooltip("Seconds the game freezes on a heavy hit.")]
        [SerializeField] private float heavyHitStopDuration = 0.14f;

        [Header("Screen Shake (0 = off)")]
        [Tooltip("Impulse force on a light hit. Keep at 0 unless you want subtle shake.")]
        [SerializeField] private float lightShakeForce = 0f;

        [Tooltip("Impulse force on a heavy hit. Keep at 0 unless you want subtle shake.")]
        [SerializeField] private float heavyShakeForce = 0.1f;

        private CinemachineImpulseSource impulse;
        private Coroutine hitStopRoutine;

        private void Awake() => impulse = GetComponent<CinemachineImpulseSource>();

        private void OnEnable()  => HitboxController.OnHitLanded += HandleHit;
        private void OnDisable() => HitboxController.OnHitLanded -= HandleHit;

        private void HandleHit(Vector3 worldPos, bool isHeavy)
        {
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                Time.timeScale = 1f;
            }

            hitStopRoutine = StartCoroutine(HitStop(isHeavy ? heavyHitStopDuration : lightHitStopDuration));

            float force = isHeavy ? heavyShakeForce : lightShakeForce;
            if (force > 0f)
                impulse.GenerateImpulseAt(worldPos, Vector3.up * force);
        }

        private IEnumerator HitStop(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            hitStopRoutine = null;
        }
    }
}
