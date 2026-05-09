using System.Collections.Generic;
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
            mainCam      = Camera.main;
            playerHealth = GetComponent<HealthSystem>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
                CycleTarget();

            if (Keyboard.current.escapeKey.wasPressedThisFrame && IsLocked)
                LockedTarget = null;

            // Clear dead targets automatically
            if (LockedTarget != null)
            {
                var hs = LockedTarget.GetComponent<HealthSystem>();
                if (hs == null || hs.IsDead)
                    LockedTarget = null;
            }
        }

        private void CycleTarget()
        {
            List<Transform> candidates = GetCandidatesSortedByDistance();
            if (candidates.Count == 0)
            {
                LockedTarget = null;
                return;
            }

            if (!IsLocked)
            {
                LockedTarget = candidates[0];
                return;
            }

            int currentIndex = candidates.IndexOf(LockedTarget);
            if (currentIndex < 0)
            {
                // Current target no longer in range — start from closest
                LockedTarget = candidates[0];
                return;
            }

            // Advance to next, wrap around to closest
            LockedTarget = candidates[(currentIndex + 1) % candidates.Count];
        }

        private List<Transform> GetCandidatesSortedByDistance()
        {
            HealthSystem[] all = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude);
            var result = new List<(Transform t, float dist)>();

            foreach (var hs in all)
            {
                if (hs == playerHealth || hs.IsDead) continue;
                float dist = Vector3.Distance(transform.position, hs.transform.position);
                if (dist <= detectionRadius)
                    result.Add((hs.transform, dist));
            }

            result.Sort((a, b) => a.dist.CompareTo(b.dist));

            var transforms = new List<Transform>(result.Count);
            foreach (var entry in result)
                transforms.Add(entry.t);
            return transforms;
        }
    }
}
