#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Kaligo.World;
using Kaligo.Mobs;

namespace Kaligo.Editor.WorldBuilder
{
    /// <summary>
    /// Menu: Kaligo → World → ★ Complete World Setup (Run Once)
    ///
    /// One-click setup that does everything:
    ///   1. Creates all AreaDefinition and MobDefinition ScriptableObject assets.
    ///   2. Opens SampleScene (old proof-of-concept) additively.
    ///   3. Detects the player rig and all gameplay roots in SampleScene.
    ///   4. Transfers them into World_Kaligo at the village spawn point.
    ///   5. Removes the FallbackCamera placeholder.
    ///   6. Saves World_Kaligo.
    ///   7. Deletes SampleScene, Village_Millhaven, Zone_Meadowfield scene files.
    ///
    /// Safe to run multiple times — asset creation is idempotent and old scene
    /// deletion is skipped if the files are already gone.
    /// </summary>
    public static class WorldSetup
    {
        private static readonly Vector3 VillageSpawn = new Vector3(0f, 0f, -6f);

        // Root object names from SampleScene that we do NOT want to carry over.
        // These are Unity defaults or scene-specific lights we rebuild in World_Kaligo.
        private static readonly HashSet<string> SkipNames = new HashSet<string>
        {
            "Directional Light",
            "FallbackCamera",
            "Main Camera",           // replaced by the Cinemachine rig
        };

        [MenuItem("Kaligo/World/★ Complete World Setup (Run Once)")]
        public static void RunFullSetup()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Complete World Setup",
                "This will:\n\n" +
                "• Create all mob + area data assets\n" +
                "• Transfer your player rig from SampleScene into World_Kaligo\n" +
                "• Delete SampleScene, Village_Millhaven, Zone_Meadowfield\n\n" +
                "World_Kaligo must already exist (run Build Open World Scene first if not).\n\n" +
                "Continue?",
                "Yes, do it", "Cancel");

            if (!ok) return;

            // Step 1 — Create data assets
            Log("Step 1/5 — Creating area + mob data assets...");
            AreaDataCreator.CreateAll();
            MobDataCreator.CreateAll();

            // Step 2 — Make sure World_Kaligo exists
            const string worldPath = "Assets/Scenes/World_Kaligo.unity";
            if (!File.Exists(Path.Combine(Application.dataPath, "../" + worldPath).Replace('/', Path.DirectorySeparatorChar)))
            {
                Log("World_Kaligo.unity not found — building it now...");
                OpenWorldSceneBuilder.Build();
            }

            // Step 3 — Open World_Kaligo as the active scene
            Log("Step 2/5 — Opening World_Kaligo...");
            var worldScene = EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(worldScene);

            // Step 4 — Open SampleScene additively and transfer player objects
            const string samplePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(Path.Combine(Application.dataPath, "../" + samplePath).Replace('/', Path.DirectorySeparatorChar)))
            {
                Log("Step 3/5 — Opening SampleScene additively...");
                var sampleScene = EditorSceneManager.OpenScene(samplePath, OpenSceneMode.Additive);
                TransferGameplayObjects(sampleScene, worldScene);
                EditorSceneManager.CloseScene(sampleScene, true);
            }
            else
            {
                Log("SampleScene not found — skipping transfer (player may already be in World_Kaligo).");
            }

            // Step 5 — Save World_Kaligo
            Log("Step 4/5 — Saving World_Kaligo...");
            EditorSceneManager.SaveScene(worldScene);

            // Step 6 — Delete old scenes and legacy editor scripts
            Log("Step 5/5 — Cleaning up old scenes + legacy scripts...");
            DeleteScene("Assets/Scenes/SampleScene.unity");
            DeleteScene("Assets/Scenes/Village_Millhaven.unity");
            DeleteScene("Assets/Scenes/Zone_Meadowfield.unity");
            DeleteScene("Assets/Scenes/Zone_DarkwoodForest.unity");
            DeleteLegacyScripts();
            AssetDatabase.Refresh();

            Log("✓ World setup complete.");
            EditorUtility.DisplayDialog("Setup Complete",
                "World_Kaligo is ready.\n\n" +
                "• Press Play to run the game.\n" +
                "• Walk north for Darkwood Forest (aggressive mobs).\n" +
                "• Walk south for Meadowfield (passive mobs).\n\n" +
                "If the player doesn't have full movement controls, make sure\n" +
                "the PlayerController component is on the Player object.",
                "Let's play");
        }

        // ── Transfer logic ────────────────────────────────────────────────────

        private static void TransferGameplayObjects(Scene from, Scene to)
        {
            var transferred = new List<string>();
            var skipped     = new List<string>();

            // Find the [Player] group in World_Kaligo (created by OpenWorldSceneBuilder)
            var playerGroup = FindInScene(to, "[Player]");

            // Walk every root GameObject in SampleScene
            var roots = from.GetRootGameObjects();
            foreach (var go in roots)
            {
                if (SkipNames.Contains(go.name))
                {
                    skipped.Add(go.name);
                    continue;
                }

                // Move into World_Kaligo
                SceneManager.MoveGameObjectToScene(go, to);

                // Nest under [Player] group if it exists and this isn't a canvas/UI
                bool isUI = go.GetComponent<Canvas>() != null;
                if (playerGroup != null && !isUI)
                    go.transform.SetParent(playerGroup.transform, true);

                transferred.Add(go.name);
            }

            Log($"Transferred: {string.Join(", ", transferred)}");
            if (skipped.Count > 0)
                Log($"Skipped (scene defaults): {string.Join(", ", skipped)}");

            // Position the Player at the village spawn
            bool playerFound = PlacePlayer();

            // Only remove FallbackCamera if a real player (with its own camera) was
            // transferred. If no player was found we keep the fallback so the scene
            // doesn't go black. The user can run "Create Placeholder Player" afterward.
            if (playerFound)
                RemoveFallback(to);
            else
                Log("No Player found — keeping FallbackCamera so the scene stays visible.");
        }

        /// <returns>true if a GameObject tagged 'Player' was found and placed.</returns>
        private static bool PlacePlayer()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Log("WARN: No GameObject tagged 'Player' found. Skipping spawn placement.");
                return false;
            }

            // Disable CC to allow teleport without collision conflicts
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = VillageSpawn;
            player.transform.rotation = Quaternion.identity;
            if (cc != null) cc.enabled = true;
            Log($"Player '{player.name}' placed at {VillageSpawn}.");
            return true;
        }

        private static void RemoveFallback(Scene scene)
        {
            // Find the fallback camera by name anywhere in the scene
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                // Check root itself
                if (root.name == "FallbackCamera")
                {
                    Object.DestroyImmediate(root);
                    Log("FallbackCamera removed.");
                    return;
                }
                // Check children recursively (it lives under [Player])
                var found = FindChildByName(root.transform, "FallbackCamera");
                if (found != null)
                {
                    Object.DestroyImmediate(found.gameObject);
                    Log("FallbackCamera removed.");
                    return;
                }
            }
            Log("FallbackCamera not found (already removed or never existed).");
        }

        // ── Placeholder player ────────────────────────────────────────────────

        /// <summary>
        /// Menu: Kaligo → World → Create Placeholder Player
        ///
        /// Drops a blue capsule + follow camera into the active scene so the game
        /// is immediately playable. Replace with the real player rig when ready.
        /// </summary>
        [MenuItem("Kaligo/World/Create Placeholder Player")]
        public static void CreatePlaceholderPlayer()
        {
            if (GameObject.FindWithTag("Player") != null)
            {
                EditorUtility.DisplayDialog("Player Already Exists",
                    "A GameObject tagged 'Player' is already in the scene.", "OK");
                return;
            }

            var playerRoot = GameObject.Find("[Player]");
            Transform parent = playerRoot != null ? playerRoot.transform : null;

            // ── Player body ───────────────────────────────────────────────────
            var player       = new GameObject("Player");
            player.tag       = "Player";
            player.layer     = LayerMask.NameToLayer("Default");
            if (parent != null) player.transform.SetParent(parent);
            player.transform.position = VillageSpawn;

            // Capsule visual
            var body      = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name     = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var mat       = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color     = new Color(0.2f, 0.4f, 0.8f);
            body.GetComponent<Renderer>().material = mat;

            // CharacterController
            var cc        = player.AddComponent<CharacterController>();
            cc.height     = 2f;
            cc.radius     = 0.35f;
            cc.center     = Vector3.up;

            // ── Camera rig ────────────────────────────────────────────────────
            var camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(player.transform, false);
            camTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var camGO     = new GameObject("PlayerCamera");
            camGO.tag     = "MainCamera";
            camGO.transform.SetParent(camTarget.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 0.5f, -5f);
            camGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            var cam           = camGO.AddComponent<Camera>();
            cam.fieldOfView   = 65f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane  = 600f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<Kaligo.Characters.Player.SimpleCameraFollow>();

            // Remove the FallbackCamera if it's still there
            RemoveFallback(player.scene.IsValid() ? player.scene : UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Selection.activeGameObject = player;
            var scene = player.scene;
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);

            Log("Placeholder player created at village spawn. " +
                "Replace with your real player rig when ready.");

            EditorUtility.DisplayDialog("Placeholder Player Created",
                "A blue capsule + follow camera was added at the village spawn.\n\n" +
                "It has a CharacterController but no PlayerController.\n" +
                "Use WASD + mouse to verify the camera works, then swap in the real rig.",
                "Got it");
        }

        // ── Legacy script cleanup ─────────────────────────────────────────────

        /// <summary>
        /// Menu: Kaligo → World → Delete Legacy Editor Scripts
        ///
        /// Removes old proof-of-concept editor files that have been superseded
        /// by the data-driven WorldBuilder tools. Safe to run at any time.
        /// </summary>
        [MenuItem("Kaligo/World/Delete Legacy Editor Scripts")]
        public static void DeleteLegacyScripts()
        {
            var toDelete = new[]
            {
                "Assets/Editor/SittingCSetup.cs",
                "Assets/Editor/SittingDSetup.cs",
                "Assets/Editor/WorldBuilder/SceneBuilder.cs",
                "Assets/Editor/WorldBuilder/ZoneDataCreator.cs",
                "Assets/Editor/WorldBuilder/PlayerTransfer.cs",
            };

            int deleted = 0;
            foreach (var path in toDelete)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                {
                    Log($"Already gone: {path}");
                    continue;
                }
                bool ok = AssetDatabase.DeleteAsset(path);
                Log(ok ? $"Deleted: {path}" : $"WARN: Could not delete {path}");
                if (ok) deleted++;
            }

            AssetDatabase.Refresh();
            Log($"Legacy script cleanup done — {deleted} file(s) removed.");
        }

        // ── Scene deletion ────────────────────────────────────────────────────

        private static void DeleteScene(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) == null)
            {
                Log($"Not found (already deleted): {assetPath}");
                return;
            }
            bool deleted = AssetDatabase.DeleteAsset(assetPath);
            Log(deleted ? $"Deleted: {assetPath}" : $"WARN: Could not delete {assetPath}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                var found = FindChildByName(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void Log(string msg) =>
            Debug.Log($"[WorldSetup] {msg}");
    }
}
#endif
