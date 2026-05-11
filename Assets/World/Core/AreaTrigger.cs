using UnityEngine;

namespace Kaligo.World
{
    /// <summary>
    /// Invisible trigger volume that marks the boundary of an open-world area.
    ///
    /// When the player walks in:
    ///   • <see cref="AtmosphereManager"/> fades to the new area's fog/audio settings.
    ///   • <see cref="SafeZone"/> flag is updated if <see cref="definition.isSafeZone"/> is set.
    ///   • An area-name banner is shown on the HUD (if one exists).
    ///
    /// Placement: cover the entire area with a large BoxCollider trigger.
    /// Overlapping areas are supported — the last one entered wins.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AreaTrigger : MonoBehaviour
    {
        [SerializeField] private AreaDefinition definition;

        // The safe-zone component lives alongside us if this area is safe.
        private SafeZone safeZoneComponent;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            // Add or confirm SafeZone component
            if (definition != null && definition.isSafeZone)
            {
                safeZoneComponent = GetComponent<SafeZone>();
                if (safeZoneComponent == null)
                    safeZoneComponent = gameObject.AddComponent<SafeZone>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (definition == null) return;

            AtmosphereManager.Instance?.TransitionTo(definition);
            AreaNameBanner.Show(definition);
        }

        // ── Gizmo ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (definition == null) return;

            Color c = definition.type switch
            {
                AreaType.Village    => new Color(0f, 1f, 0.5f, 0.06f),
                AreaType.Dungeon    => new Color(0.6f, 0f, 0.8f, 0.06f),
                _                  => new Color(0.9f, 0.7f, 0.1f, 0.06f),
            };

            Gizmos.color = c;
            var col = GetComponent<BoxCollider>();
            if (col != null)
                Gizmos.DrawCube(transform.position + col.center, col.size);

            c.a = 0.5f;
            Gizmos.color = c;
            if (col != null)
                Gizmos.DrawWireCube(transform.position + col.center, col.size);

            UnityEditor.Handles.color = c;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"[Area] {definition.displayName}" +
                (definition.isSafeZone ? " ⚑" : "") +
                $"\n  Lv {definition.recommendedLevelMin}–{definition.recommendedLevelMax}");
        }
#endif
    }
}
