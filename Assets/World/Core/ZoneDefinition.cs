using UnityEngine;

namespace Kaligo.World
{
    public enum ZoneType { Village, Wilderness, Dungeon, Special }

    /// <summary>
    /// Data asset describing a game zone (one-to-one with a Unity scene).
    /// Create via: Assets → Create → Kaligo → World → Zone Definition.
    ///
    /// ZonePortals reference these assets to know which scene to load next.
    /// </summary>
    [CreateAssetMenu(fileName = "NewZone", menuName = "Kaligo/World/Zone Definition")]
    public class ZoneDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Friendly display name shown in loading screens and map UI.")]
        public string zoneName;

        [Tooltip("Exact Unity scene name (without .unity extension) to load for this zone.")]
        public string sceneName;

        public ZoneType type = ZoneType.Wilderness;

        [Tooltip("Safe zones suppress mob aggression and disable open-world PvP (Act II).")]
        public bool isSafeZone = false;

        [Header("Atmosphere")]
        [Tooltip("Fog density applied when entering this zone (0 = off).")]
        [Range(0f, 0.1f)]
        public float fogDensity = 0.01f;

        [Tooltip("Fog color blended over the scene's existing fog setting.")]
        public Color fogColor = new Color(0.6f, 0.65f, 0.7f);

        [Tooltip("Directional light color temperature shift. Warm (village) vs. cool (forest).")]
        public Color ambientLight = new Color(0.4f, 0.4f, 0.4f);

        [Header("Audio")]
        [Tooltip("Looping ambient audio (wind, birds, tavern chatter, etc.).")]
        public AudioClip ambientClip;

        [Tooltip("Zone background music.")]
        public AudioClip musicClip;

        [Range(0f, 1f)]
        public float ambientVolume = 0.4f;

        [Range(0f, 1f)]
        public float musicVolume   = 0.5f;

        [Header("Recommended Level")]
        [Tooltip("Suggested player level range. Purely informational — used by map UI.")]
        public int recommendedLevelMin = 1;
        public int recommendedLevelMax = 5;
    }
}
