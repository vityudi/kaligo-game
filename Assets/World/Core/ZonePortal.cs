using UnityEngine;

namespace Kaligo.World
{
    /// <summary>
    /// Trigger collider that transitions the player to another zone/scene.
    ///
    /// Setup:
    ///   1. Add a child GameObject with a trigger Collider (BoxCollider works well).
    ///   2. Attach ZonePortal to the parent.
    ///   3. Set <see cref="targetZone"/> and <see cref="targetSpawnId"/>.
    ///
    /// The portal fires once and then ignores the player for <see cref="cooldownSeconds"/>
    /// to prevent instant back-and-forth transitions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZonePortal : MonoBehaviour
    {
        [Header("Destination")]
        [Tooltip("Zone to load when the player enters this portal.")]
        [SerializeField] private ZoneDefinition targetZone;

        [Tooltip("SpawnPoint ID in the target zone where the player will appear. " +
                 "Defaults to 'default' if empty or not found.")]
        [SerializeField] private string targetSpawnId = "default";

        [Header("Settings")]
        [Tooltip("Seconds before the portal can fire again after a transition.")]
        [SerializeField] private float cooldownSeconds = 3f;

        [Tooltip("Optional particle/VFX to enable when portal is active.")]
        [SerializeField] private GameObject activeVFX;

        // ── State ─────────────────────────────────────────────────────────────

        private float lastTriggerTime = -999f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure the collider on this object (or the first child) is a trigger
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (targetZone == null)
            {
                Debug.LogWarning($"[ZonePortal] {name}: targetZone not assigned.", this);
                return;
            }
            if (Time.time - lastTriggerTime < cooldownSeconds) return;

            lastTriggerTime = Time.time;
            ZoneTransitionManager.Instance?.TransitionTo(targetZone, targetSpawnId);
        }

        // ── Editor ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
                Gizmos.DrawCube(transform.position + col.center, col.size);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                transform.position + (GetComponent<BoxCollider>()?.center ?? Vector3.zero),
                GetComponent<BoxCollider>()?.size ?? Vector3.one);

            if (targetZone != null)
            {
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"→ {targetZone.zoneName}\n  spawn: {targetSpawnId}");
            }
        }
#endif
    }
}
