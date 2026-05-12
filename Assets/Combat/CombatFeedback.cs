using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Kaligo.Combat
{
    /// <summary>
    /// Subscribes to HitboxController.OnHitLanded and applies hit-stop + screen shake
    /// on every confirmed hit. Place on the same GameObject as CinemachineImpulseSource.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CombatFeedback : MonoBehaviour
    {
        [Header("Hit-Stop")]
        [SerializeField] private float lightHitStopDuration = 0.05f;
        [SerializeField] private float heavyHitStopDuration = 0.12f;

        [Header("Screen Shake")]
        [SerializeField] private float lightShakeForce = 0.08f;
        [SerializeField] private float heavyShakeForce = 0.22f;

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
            hitStopRoutine = StartCoroutine(
                HitStop(isHeavy ? heavyHitStopDuration : lightHitStopDuration));

            float force = isHeavy ? heavyShakeForce : lightShakeForce;
            if (force > 0f && impulse != null)
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
