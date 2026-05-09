using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo.Combat
{
    public class Targeting : MonoBehaviour
    {
        [SerializeField] private float detectionRadius = 20f;

        public Transform LockedTarget { get; private set; }
        public bool IsLocked => LockedTarget != null;

        private Camera mainCam;
        private HealthSystem playerHealth;

        private void Awake()
        {
            mainCam       = Camera.main;
            playerHealth  = GetComponent<HealthSystem>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                ToggleLock();

            if (LockedTarget != null)
            {
                var hs = LockedTarget.GetComponent<HealthSystem>();
                if (hs == null || hs.IsDead)
                    LockedTarget = null;
            }
        }

        private void ToggleLock()
        {
            if (IsLocked)
            {
                LockedTarget = null;
                return;
            }

            Vector3 origin = mainCam != null ? mainCam.transform.forward : transform.forward;
            origin.y = 0f;
            origin.Normalize();

            HealthSystem[] candidates = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude);
            Transform best      = null;
            float     bestScore = float.MaxValue; // lower = better (angle, then distance as tiebreak)

            foreach (var hs in candidates)
            {
                if (hs == playerHealth || hs.IsDead) continue;

                float dist = Vector3.Distance(transform.position, hs.transform.position);
                if (dist > detectionRadius) continue;

                Vector3 toTarget = hs.transform.position - transform.position;
                toTarget.y = 0f;
                toTarget.Normalize();

                float angle = Vector3.Angle(origin, toTarget);
                float score = angle * 100f + dist; // angle dominates; distance breaks ties
                if (score < bestScore)
                {
                    bestScore = score;
                    best      = hs.transform;
                }
            }

            LockedTarget = best;
        }
    }
}
