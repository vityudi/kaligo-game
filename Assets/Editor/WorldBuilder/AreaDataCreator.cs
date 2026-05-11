#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Kaligo.World;

namespace Kaligo.Editor.WorldBuilder
{
    /// <summary>
    /// Menu: Kaligo → World → Create Area Definitions
    ///
    /// Creates AreaDefinition assets for all three starting open-world areas.
    /// Run before OpenWorldSceneBuilder (area triggers reference these assets).
    /// </summary>
    public static class AreaDataCreator
    {
        private const string DataRoot = "Assets/World/Data";

        [MenuItem("Kaligo/World/Create Area Definitions")]
        public static void CreateAll()
        {
            EnsureFolders();

            Create(new AreaDefinition
            {
                areaId               = "village",
                displayName          = "Millhaven",
                type                 = AreaType.Village,
                isSafeZone           = true,
                fogDensity           = 0.002f,
                fogColor             = new Color(0.70f, 0.72f, 0.68f),
                ambientLight         = new Color(0.45f, 0.42f, 0.38f),
                ambientVolume        = 0.35f,
                musicVolume          = 0.45f,
                nameBannerDuration   = 3f,
                recommendedLevelMin  = 1,
                recommendedLevelMax  = 99,
            }, "Area_Village_Millhaven");

            Create(new AreaDefinition
            {
                areaId               = "meadow",
                displayName          = "Meadowfield",
                type                 = AreaType.Wilderness,
                isSafeZone           = false,
                fogDensity           = 0.004f,
                fogColor             = new Color(0.65f, 0.72f, 0.60f),
                ambientLight         = new Color(0.42f, 0.48f, 0.36f),
                ambientVolume        = 0.45f,
                musicVolume          = 0.40f,
                nameBannerDuration   = 3f,
                recommendedLevelMin  = 1,
                recommendedLevelMax  = 5,
            }, "Area_Meadowfield");

            Create(new AreaDefinition
            {
                areaId               = "darkforest",
                displayName          = "Darkwood Forest",
                type                 = AreaType.Wilderness,
                isSafeZone           = false,
                fogDensity           = 0.020f,
                fogColor             = new Color(0.18f, 0.20f, 0.16f),
                ambientLight         = new Color(0.18f, 0.20f, 0.22f),
                ambientVolume        = 0.55f,
                musicVolume          = 0.50f,
                nameBannerDuration   = 3f,
                recommendedLevelMin  = 3,
                recommendedLevelMax  = 8,
            }, "Area_DarkwoodForest");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Area Definitions Created",
                "3 area definitions created.\nNow run:\n" +
                "  Kaligo → World → Create All Mob Definitions\n" +
                "  Kaligo → World → Build Open World Scene",
                "OK");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/World"))
                AssetDatabase.CreateFolder("Assets", "World");
            if (!AssetDatabase.IsValidFolder(DataRoot))
                AssetDatabase.CreateFolder("Assets/World", "Data");
        }

        private static void Create(AreaDefinition def, string fileName)
        {
            string path = $"{DataRoot}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<AreaDefinition>(path) != null)
            {
                Debug.Log($"[AreaDataCreator] Already exists: {path}");
                return;
            }
            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[AreaDataCreator] Created: {path}");
        }
    }
}
#endif
