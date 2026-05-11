using UnityEngine;

namespace Kaligo.World
{
    /// <summary>
    /// Data asset describing a named region inside the open world.
    ///
    /// Unlike <see cref="ZoneDefinition"/> (which points to a separate Unity scene),
    /// an AreaDefinition describes a contiguous region of the single open-world scene.
    /// Atmosphere transitions between areas are seamless — no loading screens.
    ///
    /// Create via: Assets → Create → Kaligo → World → Area Definition.
    ///
    /// MMO note: these areas map 1-to-1 to server-side interest-management zones.
    /// When the server partitions the world for Act II, each AreaDefinition's
    /// <see cref="areaId"/> becomes the key the server uses to group entities.
    /// </summary>
    [CreateAssetMenu(fileName = "NewArea", menuName = "Kaligo/World/Area Definition")]
    public class AreaDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Tooltip("Unique lowercase key — used by the server for interest management. E.g. 'village', 'meadow', 'darkforest'.")]
        public string areaId;

        [Tooltip("Display name shown on the screen as the player enters.")]
        public string displayName;

        public AreaType type = AreaType.Wilderness;

        [Tooltip("Safe areas suppress mob aggression and, later, open-world PvP.")]
        public bool isSafeZone = false;

        // ── Atmosphere ────────────────────────────────────────────────────────

        [Header("Atmosphere")]
        [Range(0f, 0.08f)]
        public float fogDensity    = 0.005f;
        public Color fogColor      = new Color(0.6f, 0.65f, 0.7f);
        public Color ambientLight  = new Color(0.4f, 0.4f, 0.4f);

        [Header("Audio")]
        public AudioClip ambientClip;
        public AudioClip musicClip;
        [Range(0f, 1f)] public float ambientVolume = 0.4f;
        [Range(0f, 1f)] public float musicVolume   = 0.5f;

        // ── HUD hint ──────────────────────────────────────────────────────────

        [Header("UI")]
        [Tooltip("How long (seconds) the area name banner stays on screen when entering.")]
        public float nameBannerDuration = 3f;

        [Tooltip("Recommended level range — shown in the area name banner.")]
        public int recommendedLevelMin = 1;
        public int recommendedLevelMax = 5;
    }

    public enum AreaType { Village, Wilderness, Dungeon, Special }
}
