using UnityEngine;

namespace Kaligo.World
{
    /// <summary>
    /// Named anchor that <see cref="ZoneTransitionManager"/> places the player on
    /// when entering a zone.
    ///
    /// Every zone scene should have at least one spawn point.
    /// Portal exit transitions specify which spawn point ID to use.
    ///
    /// Naming convention: "from_[source_zone]" — e.g. "from_village", "from_meadow".
    /// Zones may also have a "default" spawn used as a fallback.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Unique identifier within this scene. Convention: 'from_[zone]' or 'default'.")]
        public string spawnId = "default";

        [Tooltip("If true, this is the fallback when no matching spawnId is found.")]
        public bool isDefault = false;

        // ── Editor Gizmo ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 pos = transform.position;

            // Draw a simple directional marker
            Gizmos.DrawSphere(pos, 0.25f);
            Gizmos.DrawLine(pos, pos + transform.forward * 1.2f);
            Gizmos.DrawLine(pos + transform.forward * 1.2f,
                            pos + transform.forward * 0.8f + transform.right * 0.3f);
            Gizmos.DrawLine(pos + transform.forward * 1.2f,
                            pos + transform.forward * 0.8f - transform.right * 0.3f);

            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"Spawn: {spawnId}");
        }
#endif
    }
}
