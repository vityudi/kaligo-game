#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Kaligo.Mobs;

namespace Kaligo.Editor.WorldBuilder
{
    /// <summary>
    /// Menu: Kaligo → World → Create All Mob Definitions
    ///
    /// Creates a MobDefinition ScriptableObject for every creature species
    /// under Assets/Characters/Mobs/Data/. Run once; safe to re-run (skips
    /// existing assets). Tweak stats in the Inspector afterwards.
    /// </summary>
    public static class MobDataCreator
    {
        private const string DataRoot = "Assets/Characters/Mobs/Data";

        [MenuItem("Kaligo/World/Create All Mob Definitions")]
        public static void CreateAll()
        {
            EnsureFolders();

            // ── Passive mobs ──────────────────────────────────────────────────

            CreateMob(new MobDefinition
            {
                mobId              = "deer",
                displayName        = "Deer",
                type               = MobType.Passive,
                maxHealth          = 60f,
                moveSpeed          = 4.5f,
                turnSpeed          = 400f,
                wanderRadius       = 12f,
                wanderDuration     = 4f,
                wanderPauseDuration = 3f,
                fleeDetectionRange = 10f,
                fleeSpeedMultiplier = 2.0f,
                fleeUntilDistance  = 20f,
                xpReward           = 0,   // peaceful animals give no XP
                placeholderColor   = new Color(0.72f, 0.52f, 0.30f), // tan
                placeholderHeight  = 1.5f,
                placeholderRadius  = 0.4f,
            }, "Passive/Deer");

            CreateMob(new MobDefinition
            {
                mobId              = "chicken",
                displayName        = "Chicken",
                type               = MobType.Passive,
                maxHealth          = 10f,
                moveSpeed          = 3f,
                turnSpeed          = 600f,
                wanderRadius       = 5f,
                wanderDuration     = 2f,
                wanderPauseDuration = 1.5f,
                fleeDetectionRange = 5f,
                fleeSpeedMultiplier = 1.6f,
                fleeUntilDistance  = 10f,
                xpReward           = 0,
                placeholderColor   = new Color(0.95f, 0.9f, 0.8f), // off-white
                placeholderHeight  = 0.55f,
                placeholderRadius  = 0.2f,
            }, "Passive/Chicken");

            CreateMob(new MobDefinition
            {
                mobId              = "sheep",
                displayName        = "Sheep",
                type               = MobType.Passive,
                maxHealth          = 40f,
                moveSpeed          = 2.5f,
                turnSpeed          = 300f,
                wanderRadius       = 10f,
                wanderDuration     = 5f,
                wanderPauseDuration = 4f,
                fleeDetectionRange = 7f,
                fleeSpeedMultiplier = 1.5f,
                fleeUntilDistance  = 15f,
                xpReward           = 0,
                placeholderColor   = new Color(0.9f, 0.9f, 0.9f), // white
                placeholderHeight  = 1.1f,
                placeholderRadius  = 0.35f,
            }, "Passive/Sheep");

            // ── Aggressive mobs ───────────────────────────────────────────────

            CreateMob(new MobDefinition
            {
                mobId               = "rat",
                displayName         = "Giant Rat",
                type                = MobType.Aggressive,
                maxHealth           = 30f,
                moveSpeed           = 4f,
                turnSpeed           = 720f,
                detectionRange      = 8f,
                attackRange         = 1.2f,
                damage              = 8f,
                attackCooldown      = 1.8f,
                attackDuration      = 1.5f,
                damageAtNormalized  = 0.5f,
                fleeAtHpFraction    = 0f,
                alertsNearby        = false,
                xpReward            = 15,
                placeholderColor    = new Color(0.45f, 0.35f, 0.3f), // dark brown
                placeholderHeight   = 0.6f,
                placeholderRadius   = 0.25f,
            }, "Aggressive/Rat");

            CreateMob(new MobDefinition
            {
                mobId               = "wolf",
                displayName         = "Wolf",
                type                = MobType.Aggressive,
                maxHealth           = 80f,
                moveSpeed           = 5f,
                turnSpeed           = 500f,
                detectionRange      = 15f,
                attackRange         = 2f,
                damage              = 18f,
                attackCooldown      = 2.2f,
                attackDuration      = 2f,
                damageAtNormalized  = 0.4f,
                fleeAtHpFraction    = 0f,
                alertsNearby        = true,  // pack behavior!
                alertRadius         = 20f,
                xpReward            = 35,
                placeholderColor    = new Color(0.4f, 0.4f, 0.45f), // grey
                placeholderHeight   = 1.2f,
                placeholderRadius   = 0.35f,
            }, "Aggressive/Wolf");

            CreateMob(new MobDefinition
            {
                mobId               = "bear",
                displayName         = "Bear",
                type                = MobType.Aggressive,
                maxHealth           = 200f,
                moveSpeed           = 3.5f,
                turnSpeed           = 250f,
                detectionRange      = 12f,
                attackRange         = 2.5f,
                damage              = 35f,
                attackCooldown      = 3f,
                attackDuration      = 3f,
                damageAtNormalized  = 0.5f,
                fleeAtHpFraction    = 0.2f, // flees at 20% HP — dangerous but not suicidal
                alertsNearby        = false,
                xpReward            = 80,
                placeholderColor    = new Color(0.35f, 0.22f, 0.12f), // dark brown
                placeholderHeight   = 2.2f,
                placeholderRadius   = 0.6f,
            }, "Aggressive/Bear");

            CreateMob(new MobDefinition
            {
                mobId               = "goblin",
                displayName         = "Goblin",
                type                = MobType.Aggressive,
                maxHealth           = 55f,
                moveSpeed           = 4.2f,
                turnSpeed           = 450f,
                detectionRange      = 12f,
                attackRange         = 1.8f,
                damage              = 14f,
                attackCooldown      = 2f,
                attackDuration      = 1.8f,
                damageAtNormalized  = 0.45f,
                fleeAtHpFraction    = 0.15f, // cowardly — flees at 15% HP
                alertsNearby        = true,  // calls for help
                alertRadius         = 12f,
                xpReward            = 30,
                placeholderColor    = new Color(0.2f, 0.45f, 0.2f), // green
                placeholderHeight   = 1.2f,
                placeholderRadius   = 0.28f,
            }, "Aggressive/Goblin");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Mob Definitions Created",
                $"All 7 mob definitions have been created under {DataRoot}/",
                "OK");

            Debug.Log($"[MobDataCreator] 7 mob definitions written to {DataRoot}/");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Characters/Mobs");
            EnsureFolder($"{DataRoot}");
            EnsureFolder($"{DataRoot}/Passive");
            EnsureFolder($"{DataRoot}/Aggressive");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void CreateMob(MobDefinition def, string relativePath)
        {
            string path = $"{DataRoot}/{relativePath}.asset";

            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<MobDefinition>(path) != null)
            {
                Debug.Log($"[MobDataCreator] Already exists, skipping: {path}");
                return;
            }

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[MobDataCreator] Created: {path}");
        }
    }
}
#endif
