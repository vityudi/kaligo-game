using UnityEngine;
using Kaligo.Mobs;

namespace Kaligo.World
{
    /// <summary>
    /// Marks a trigger volume as a safe zone.
    ///
    /// While the player is inside:
    ///   • <see cref="AggressiveMobBrain"/> components will not enter Chase state.
    ///   • A static flag <see cref="PlayerIsInSafeZone"/> is set to true.
    ///
    /// AggressiveMobBrain checks this flag in its detection logic.
    ///
    /// Typical use: surround the village with a large trigger on a dedicated
    /// "SafeZone" layer so monsters spawned just outside cannot aggro inside.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        /// <summary>True while a player is inside any active SafeZone trigger.</summary>
        public static bool PlayerIsInSafeZone { get; private set; }

        private static int _playerCount; // supports nested / overlapping safe zones

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerCount++;
            PlayerIsInSafeZone = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _playerCount = Mathf.Max(0, _playerCount - 1);
            PlayerIsInSafeZone = _playerCount > 0;
        }

        private void OnDisable()
        {
            // If the zone object is disabled mid-play, clear the flag to avoid stale state
            PlayerIsInSafeZone = false;
            _playerCount       = 0;
        }

        // ── Editor ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.08f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider bc)
                Gizmos.DrawCube(transform.position + bc.center, bc.size);
            else
                Gizmos.DrawSphere(transform.position, 5f);

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            if (col is BoxCollider bc2)
                Gizmos.DrawWireCube(transform.position + bc2.center, bc2.size);

            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 1f);
            UnityEditor.Handles.Label(transform.position + Vector3.up, "⚑ Safe Zone");
        }
#endif
    }
}
