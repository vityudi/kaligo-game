#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Kaligo.World;
using Kaligo.Mobs;
using Kaligo.Items;
using Kaligo.Skills;
using Kaligo.Characters;
using Kaligo.Combat;
using Kaligo.UI;

namespace Kaligo.Editor.WorldBuilder
{
    /// <summary>
    /// Menu: Kaligo → World → Build Open World Scene
    ///
    /// Builds a single, contiguous open-world scene called "World_Kaligo".
    /// No loading screens. All areas (village, meadow, forest) exist in the
    /// same scene separated by AreaTrigger volumes.
    ///
    /// World layout (top-down, north = +Z):
    ///
    ///        [ DarkwoodForest ]          Wolf/Bear/Goblin/Rat
    ///              ↑
    ///        [ Millhaven ]               Safe village hub
    ///              ↓
    ///        [ Meadowfield ]             Deer/Sheep/Chicken
    ///
    /// The terrain is ~500×500 units. Areas are separated by tree lines and
    /// natural-looking ground color shifts. No hard walls, no portals.
    ///
    /// Prerequisites: run the following first so AreaDefinition assets exist:
    ///   Kaligo → World → Create Area Definitions
    ///   Kaligo → World → Create All Mob Definitions
    /// </summary>
    public static class OpenWorldSceneBuilder
    {
        // ── World dimensions ──────────────────────────────────────────────────

        // Area centers (XZ)
        private static readonly Vector3 VillageCenter  = new Vector3(  0f, 0f,   0f);
        private static readonly Vector3 MeadowCenter   = new Vector3(  0f, 0f, -120f);
        private static readonly Vector3 ForestCenter   = new Vector3(  0f, 0f,  140f);

        // Terrain plane sizes (width, depth)
        private const float VillageSize  = 100f;
        private const float MeadowSize   = 180f;
        private const float ForestSize   = 200f;
        private const float ConnectorSize = 60f; // transition strips between areas

        // ── Menu item ─────────────────────────────────────────────────────────

        [MenuItem("Kaligo/World/Build Open World Scene")]
        public static void Build()
        {
            EnsureScenesFolder();
            const string scenePath = "Assets/Scenes/World_Kaligo.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            // ── Root organisers ───────────────────────────────────────────────
            var terrainRoot    = new GameObject("[Terrain]");
            var structureRoot  = new GameObject("[Structures]");
            var vegetationRoot = new GameObject("[Vegetation]");
            var lightingRoot   = new GameObject("[Lighting]");
            var systemsRoot    = new GameObject("[Systems]");
            var spawnRoot      = new GameObject("[Spawners]");
            var areaRoot       = new GameObject("[Areas]");
            var playerRoot     = new GameObject("[Player]");

            // ── Lighting ──────────────────────────────────────────────────────
            BuildLighting(lightingRoot.transform);

            // ── Systems ───────────────────────────────────────────────────────
            BuildSystems(systemsRoot.transform);

            // ── Player (X Bot rig + full gameplay stack) ──────────────────────
            BuildPlaceholderPlayer(playerRoot.transform);

            // ── Terrain ───────────────────────────────────────────────────────
            BuildTerrain(terrainRoot.transform);

            // ── Village ───────────────────────────────────────────────────────
            BuildVillage(structureRoot.transform, vegetationRoot.transform);

            // ── Meadow ───────────────────────────────────────────────────────
            BuildMeadow(vegetationRoot.transform);

            // ── DarkForest ────────────────────────────────────────────────────
            BuildForest(vegetationRoot.transform);

            // ── Treeline corridors (visual separators) ────────────────────────
            BuildTreeCorridor(vegetationRoot.transform,
                VillageCenter + new Vector3(0, 0, -VillageSize * 0.5f - 10f),
                new Vector3(VillageSize, 0, 20f),
                "Treeline_South",
                new Color(0.25f, 0.52f, 0.18f));

            BuildTreeCorridor(vegetationRoot.transform,
                VillageCenter + new Vector3(0, 0, VillageSize * 0.5f + 10f),
                new Vector3(VillageSize, 0, 20f),
                "Treeline_North",
                new Color(0.15f, 0.30f, 0.12f));

            // ── Mob spawners ──────────────────────────────────────────────────
            BuildSpawners(spawnRoot.transform);

            // ── Spawn points ──────────────────────────────────────────────────
            BuildSpawnPoints(spawnRoot.transform);

            // ── Area triggers ─────────────────────────────────────────────────
            BuildAreaTriggers(areaRoot.transform);

            // ── HUD + UI canvas ───────────────────────────────────────────────
            BuildCanvas(systemsRoot.transform);

            // ── Save ──────────────────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[OpenWorldSceneBuilder] World_Kaligo.unity saved to {scenePath}");
            EditorUtility.DisplayDialog("World Built — Ready to Play",
                "World_Kaligo.unity created!\n\n" +
                "Press Play now. You have:\n" +
                "  • WASD = sprint, Shift = walk\n" +
                "  • Mouse to orbit camera\n" +
                "  • LMB = SwordCombo, RMB = Block\n" +
                "  • Space = Jump, 1 = Dodge, 2 = DashStrike\n" +
                "  • 3 = Whirlwind, 4 = IronSkin, 5 = QuickMend\n" +
                "  • I = Inventory, Alt = cursor / UI mode\n" +
                "  • Tab = cycle lock-on target\n\n" +
                "Walk north for Darkwood Forest, south for Meadowfield.",
                "Let's go");
        }

        // ── Terrain layers ────────────────────────────────────────────────────

        private static void BuildTerrain(Transform root)
        {
            // Village cobblestone centre
            MakePlane("Terrain_Village", root, VillageCenter,
                new Vector3(VillageSize / 10f, 1f, VillageSize / 10f),
                new Color(0.48f, 0.43f, 0.36f));

            // Village outskirts (dirt paths)
            MakePlane("Terrain_VillageEdge", root,
                VillageCenter,
                new Vector3((VillageSize + 40f) / 10f, 1f, (VillageSize + 40f) / 10f),
                new Color(0.42f, 0.36f, 0.24f));

            // Meadow — bright grassland
            MakePlane("Terrain_Meadow", root, MeadowCenter,
                new Vector3(MeadowSize / 10f, 1f, MeadowSize / 10f),
                new Color(0.30f, 0.55f, 0.20f));

            // Forest — dark earth
            MakePlane("Terrain_Forest", root, ForestCenter,
                new Vector3(ForestSize / 10f, 1f, ForestSize / 10f),
                new Color(0.16f, 0.13f, 0.09f));

            // Connector strips (smooth transition zones)
            MakePlane("Terrain_Connector_South", root,
                (VillageCenter + MeadowCenter) / 2f,
                new Vector3(VillageSize / 10f, 1f, ConnectorSize / 10f),
                new Color(0.35f, 0.48f, 0.22f));

            MakePlane("Terrain_Connector_North", root,
                (VillageCenter + ForestCenter) / 2f,
                new Vector3(VillageSize / 10f, 1f, ConnectorSize / 10f),
                new Color(0.22f, 0.32f, 0.16f));
        }

        // ── Village buildings ─────────────────────────────────────────────────

        private static void BuildVillage(Transform structRoot, Transform vegRoot)
        {
            // Five buildings arranged around the central square
            MakeBuilding("Inn",           structRoot, VillageCenter + new Vector3( 0,   0,  16f), new Vector3(10, 6, 7));
            MakeBuilding("Blacksmith",    structRoot, VillageCenter + new Vector3(-18f, 0,  8f),  new Vector3(8, 5, 6));
            MakeBuilding("GeneralStore",  structRoot, VillageCenter + new Vector3( 18f, 0,  8f),  new Vector3(7, 5, 6));
            MakeBuilding("House_West",    structRoot, VillageCenter + new Vector3(-16f, 0, -8f),  new Vector3(6, 4, 6));
            MakeBuilding("House_East",    structRoot, VillageCenter + new Vector3( 16f, 0, -8f),  new Vector3(6, 4, 6));
            MakeBuilding("Warehouse",     structRoot, VillageCenter + new Vector3( -4f, 0, -18f), new Vector3(9, 4, 7));

            // Central well
            MakeWell("Well", structRoot, VillageCenter + new Vector3(0, 0, 2f));

            // Village fountain (decorative)
            MakeFountain("Fountain", structRoot, VillageCenter + new Vector3(0, 0, -5f));

            // Fence perimeter (suggests boundary without hard wall)
            BuildFenceRing(structRoot, VillageCenter, 38f, 32);

            // Village lanterns / light poles
            float[] angles = { 0, 60, 120, 180, 240, 300 };
            for (int i = 0; i < angles.Length; i++)
            {
                float r = angles[i] * Mathf.Deg2Rad;
                Vector3 pos = VillageCenter + new Vector3(Mathf.Cos(r) * 14f, 0, Mathf.Sin(r) * 14f);
                MakeLanternPost($"Lantern_{i}", structRoot, pos);
            }

            // Decorative trees inside village (but outside the square)
            var treeAngles = new[] { 20f, 80f, 160f, 200f, 280f, 340f };
            foreach (float a in treeAngles)
            {
                float r = a * Mathf.Deg2Rad;
                MakeTree($"VillageTree_{(int)a}", vegRoot,
                    VillageCenter + new Vector3(Mathf.Cos(r) * 30f, 0, Mathf.Sin(r) * 30f),
                    new Color(0.25f, 0.55f, 0.16f), 3f, 2f);
            }
        }

        // ── Meadow decoration ─────────────────────────────────────────────────

        private static void BuildMeadow(Transform vegRoot)
        {
            var rng = new System.Random(101);
            for (int i = 0; i < 18; i++)
            {
                float x = (float)(rng.NextDouble() * 160 - 80);
                float z = (float)(rng.NextDouble() * 140 - 70) + MeadowCenter.z;
                // Clear a path down the centre
                if (Mathf.Abs(x) < 12f) continue;
                MakeTree($"MeadowTree_{i}", vegRoot,
                    new Vector3(x, 0, z),
                    new Color(0.22f, 0.55f, 0.15f), 2.5f, 1.8f);
            }

            // Scattered boulders for landmarks
            for (int i = 0; i < 6; i++)
            {
                float x = (float)(rng.NextDouble() * 120 - 60);
                float z = (float)(rng.NextDouble() * 100 - 50) + MeadowCenter.z;
                MakeRock($"MeadowRock_{i}", vegRoot, new Vector3(x, 0, z));
            }
        }

        // ── Forest decoration ─────────────────────────────────────────────────

        private static void BuildForest(Transform vegRoot)
        {
            var rng = new System.Random(202);
            for (int i = 0; i < 55; i++)
            {
                float x = (float)(rng.NextDouble() * 180 - 90);
                float z = (float)(rng.NextDouble() * 170 - 85) + ForestCenter.z;
                // Clear space near the entry path
                if (Mathf.Abs(x) < 10f && z < ForestCenter.z - 20f) continue;

                float height = 3f + (float)(rng.NextDouble() * 2.5f);
                MakeTree($"ForestTree_{i}", vegRoot,
                    new Vector3(x, 0, z),
                    new Color(0.10f, 0.25f, 0.09f), height, height * 0.8f);
            }

            // Ancient ruins (decorative) — scattered stone blocks
            MakeRuin("Ruin_A", vegRoot, ForestCenter + new Vector3( 25f, 0,  20f));
            MakeRuin("Ruin_B", vegRoot, ForestCenter + new Vector3(-30f, 0, -15f));
            MakeRuin("Ruin_C", vegRoot, ForestCenter + new Vector3( 10f, 0, -40f));
        }

        // ── Tree corridor (visual separator between areas) ────────────────────

        private static void BuildTreeCorridor(
            Transform root, Vector3 center, Vector3 extents, string label, Color foliageColor)
        {
            var rng = new System.Random(label.GetHashCode());
            int count = (int)(extents.x / 6f);
            for (int i = 0; i < count; i++)
            {
                float xOff = (float)(rng.NextDouble() * extents.x - extents.x / 2f);
                float zOff = (float)(rng.NextDouble() * extents.z - extents.z / 2f);
                MakeTree($"{label}_T{i}", root,
                    center + new Vector3(xOff, 0, zOff),
                    foliageColor, 3.5f, 2.2f);
            }
        }

        // ── Mob spawners ──────────────────────────────────────────────────────

        private static void BuildSpawners(Transform root)
        {
            // ── Meadow (passive) ──────────────────────────────────────────────
            PlaceSpawner("Spawn_Deer_A",    root, "Passive/Deer",     MeadowCenter + new Vector3(-35f, 0,  20f), 4, 18f, 45f);
            PlaceSpawner("Spawn_Deer_B",    root, "Passive/Deer",     MeadowCenter + new Vector3( 40f, 0,  -5f), 3, 15f, 45f);
            PlaceSpawner("Spawn_Sheep_A",   root, "Passive/Sheep",    MeadowCenter + new Vector3( 10f, 0, -35f), 5, 15f, 60f);
            PlaceSpawner("Spawn_Sheep_B",   root, "Passive/Sheep",    MeadowCenter + new Vector3(-25f, 0, -20f), 4, 12f, 60f);
            PlaceSpawner("Spawn_Chicken_A", root, "Passive/Chicken",  MeadowCenter + new Vector3(-50f, 0,  10f), 6, 8f,  30f);
            PlaceSpawner("Spawn_Chicken_B", root, "Passive/Chicken",  MeadowCenter + new Vector3( 30f, 0,  30f), 5, 8f,  30f);

            // ── Forest (aggressive) ───────────────────────────────────────────
            PlaceSpawner("Spawn_Rat_A",    root, "Aggressive/Rat",    ForestCenter + new Vector3(-20f, 0, -25f), 5, 8f,  20f);
            PlaceSpawner("Spawn_Rat_B",    root, "Aggressive/Rat",    ForestCenter + new Vector3( 30f, 0,  15f), 4, 8f,  20f);
            PlaceSpawner("Spawn_Wolf_A",   root, "Aggressive/Wolf",   ForestCenter + new Vector3(  5f, 0, -30f), 3, 14f, 60f);
            PlaceSpawner("Spawn_Wolf_B",   root, "Aggressive/Wolf",   ForestCenter + new Vector3(-35f, 0,  10f), 2, 12f, 60f);
            PlaceSpawner("Spawn_Goblin_A", root, "Aggressive/Goblin", ForestCenter + new Vector3( 25f, 0, -20f), 3, 12f, 50f);
            PlaceSpawner("Spawn_Goblin_B", root, "Aggressive/Goblin", ForestCenter + new Vector3(-15f, 0,  35f), 3, 10f, 50f);
            // Bear — one, deep in the forest, long respawn
            PlaceSpawner("Spawn_Bear",     root, "Aggressive/Bear",   ForestCenter + new Vector3(  0f, 0,  20f), 1, 8f,  180f);
        }

        // ── Spawn points ──────────────────────────────────────────────────────

        private static void BuildSpawnPoints(Transform root)
        {
            // Village inn — main spawn
            MakeSpawnPoint("Spawn_Village_Default", root,
                VillageCenter + new Vector3(0, 0, -6f), "default", isDefault: true);

            // Meadow and forest waypoints (for future respawn shrines)
            MakeSpawnPoint("Spawn_Meadow",  root,
                MeadowCenter + new Vector3(0, 0, 70f), "meadow",  isDefault: false);
            MakeSpawnPoint("Spawn_Forest",  root,
                ForestCenter + new Vector3(0, 0, -70f), "forest", isDefault: false);
        }

        // ── Area triggers ─────────────────────────────────────────────────────

        private static void BuildAreaTriggers(Transform root)
        {
            var villageArea = LoadArea("village");
            var meadowArea  = LoadArea("meadow");
            var forestArea  = LoadArea("darkforest");

            // Village — safe zone covering the built-up area and a little beyond
            if (villageArea != null)
                MakeAreaTrigger("AreaTrigger_Village", root, villageArea,
                    VillageCenter, new Vector3(90f, 20f, 90f));

            // Meadow — south half of the world
            if (meadowArea != null)
                MakeAreaTrigger("AreaTrigger_Meadow", root, meadowArea,
                    MeadowCenter, new Vector3(200f, 20f, 200f));

            // Forest — north half of the world
            if (forestArea != null)
                MakeAreaTrigger("AreaTrigger_Forest", root, forestArea,
                    ForestCenter, new Vector3(220f, 20f, 220f));
        }

        // ── Systems ───────────────────────────────────────────────────────────

        private static void BuildSystems(Transform root)
        {
            // AtmosphereManager — handles fog/audio transitions between areas
            var atmoGO = new GameObject("AtmosphereManager");
            atmoGO.transform.SetParent(root);
            atmoGO.AddComponent<AtmosphereManager>();

            // ZoneTransitionManager — kept for dungeon instancing in future phases
            var ztmGO = new GameObject("ZoneTransitionManager");
            ztmGO.transform.SetParent(root);
            ztmGO.AddComponent<ZoneTransitionManager>();

            // Bootstrap — initialises services (ItemRegistry + DB) on Awake
            var bootGO    = new GameObject("Bootstrap");
            bootGO.transform.SetParent(root);
            var bootstrap = bootGO.AddComponent<Bootstrap>();
            var itemReg   = AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/Items/ItemRegistry.asset");
            if (itemReg != null)
            {
                var so   = new SerializedObject(bootstrap);
                var prop = so.FindProperty("_itemRegistry");
                if (prop != null) prop.objectReferenceValue = itemReg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Player rig ────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates the X Bot character, adds every gameplay component
        /// (PlayerController, SkillExecutor, SkillBar with all skills wired,
        /// combat systems, CameraOrbitInput orbit camera) and positions the
        /// rig at the village spawn point.
        /// </summary>
        private static void BuildPlaceholderPlayer(Transform root)
        {
            // Y + 0.15 ensures the CharacterController bottom clears the terrain surface
            // so isGrounded is true from the very first frame (avoids the walk-on-spawn glitch).
            var spawnPos = VillageCenter + new Vector3(0f, 0.15f, -6f);

            // ── Load X Bot FBX ────────────────────────────────────────────────
            var xBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/XBot/X Bot.fbx");
            if (xBotPrefab == null)
            {
                Debug.LogError("[OpenWorldSceneBuilder] X Bot.fbx not found at Assets/Characters/XBot/X Bot.fbx");
                return;
            }

            var player = (GameObject)Object.Instantiate(xBotPrefab, spawnPos, Quaternion.identity);
            player.name  = "Player";
            player.tag   = "Player";
            player.layer = LayerMask.NameToLayer("Default");
            player.transform.SetParent(root);

            // ── Animator ──────────────────────────────────────────────────────
            var animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Characters/Player/Animations/PlayerAnimator.controller");
            if (controller != null) animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // ── CharacterController ───────────────────────────────────────────
            // [RequireComponent] on PlayerController will auto-add this; we configure it
            // after adding PlayerController so the CC is guaranteed to exist.
            player.AddComponent<CharacterController>();   // ensures it exists first
            var cc    = player.GetComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.3f;

            // ── Combat systems ────────────────────────────────────────────────
            var health  = player.AddComponent<HealthSystem>();
            health.SetMaxHealth(100f);
            player.AddComponent<StaminaSystem>();
            player.AddComponent<ManaSystem>();
            player.AddComponent<PlayerStats>();
            player.AddComponent<Targeting>();

            // ── CursorController (singleton) ──────────────────────────────────
            player.AddComponent<CursorController>();

            // ── Movement + Skills ─────────────────────────────────────────────
            // PlayerController requires Animator (already present) + CharacterController
            player.AddComponent<PlayerController>();
            var skillExec = player.AddComponent<SkillExecutor>();
            var skillBar  = player.AddComponent<SkillBar>();

            // ── Equipment manager ─────────────────────────────────────────────
            var equipMgr = player.AddComponent<EquipmentManager>();
            var itemReg  = AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/Items/ItemRegistry.asset");
            if (itemReg != null)
            {
                var so = new SerializedObject(equipMgr);
                var regProp = so.FindProperty("itemRegistry");
                if (regProp != null) regProp.objectReferenceValue = itemReg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── Wire skill slots ──────────────────────────────────────────────
            // Default loadout: LMB=SwordCombo, RMB=Block(hold), Space=Jump,
            //                  1=Dodge, 2=DashStrike, 3=Whirlwind, 4=IronSkin, 5=QuickMend
            WireSkillBar(skillBar, skillExec);

            // ── Weapon hitbox child ───────────────────────────────────────────
            // HitboxController needs a trigger Collider. We create a placeholder
            // sword hitbox — replace this child with the real weapon socket later.
            var hitboxGO = new GameObject("WeaponHitbox");
            hitboxGO.transform.SetParent(player.transform, false);
            hitboxGO.transform.localPosition = new Vector3(0.5f, 1.0f, 0.8f);
            var hitboxCol     = hitboxGO.AddComponent<CapsuleCollider>();
            hitboxCol.isTrigger = true;
            hitboxCol.height    = 1.2f;
            hitboxCol.radius    = 0.15f;
            hitboxCol.direction = 2; // Z-axis (forward)
            hitboxGO.AddComponent<HitboxController>();
            hitboxGO.SetActive(false); // off by default; SkillExecutor enables it during attacks

            // Wire SkillExecutor → HitboxController
            {
                var so  = new SerializedObject(skillExec);
                var prop = so.FindProperty("hitbox");
                if (prop != null) prop.objectReferenceValue = hitboxGO.GetComponent<HitboxController>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── CameraTarget (orbit pivot) ────────────────────────────────────
            var camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(player.transform, false);
            camTarget.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camTarget.AddComponent<CameraOrbitInput>();

            // ── Camera ────────────────────────────────────────────────────────
            var camGO = new GameObject("PlayerCamera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(camTarget.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 0f, -5f);
            camGO.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            var cam           = camGO.AddComponent<Camera>();
            cam.fieldOfView   = 65f;
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane  = 600f;
            camGO.AddComponent<AudioListener>();

            Debug.Log("[OpenWorldSceneBuilder] Player rig built — X Bot + full gameplay systems.");
        }

        /// <summary>
        /// Wires SkillBar slots with default loadout via SerializedObject so they
        /// survive save/load.  Slot order: LMB, RMB, Space, Key1–Key5.
        /// </summary>
        private static void WireSkillBar(
            SkillBar skillBar, SkillExecutor exec)
        {
            // Pair: (asset path relative to Assets/, binding enum value, holdToActivate)
            var loadout = new (string path, InputBinding binding, bool hold)[]
            {
                ("Assets/Skills/SwordCombo/SwordCombo.asset",  InputBinding.LMB,   false),
                ("Assets/Skills/Block/Block.asset",            InputBinding.RMB,   true),
                ("Assets/Skills/Jump/Jump.asset",              InputBinding.Space,  false),
                ("Assets/Skills/Dodge/Dodge.asset",           InputBinding.Key1,   false),
                ("Assets/Skills/DashStrike/DashStrike.asset",  InputBinding.Key2,   false),
                ("Assets/Skills/Whirlwind/Whirlwind.asset",   InputBinding.Key3,   false),
                ("Assets/Skills/IronSkin/IronSkin.asset",     InputBinding.Key4,   false),
                ("Assets/Skills/QuickMend/QuickMend.asset",   InputBinding.Key5,   false),
            };

            var so       = new SerializedObject(skillBar);
            var execProp = so.FindProperty("executor");
            if (execProp != null) execProp.objectReferenceValue = exec;

            var slotsProp = so.FindProperty("slots");
            slotsProp.ClearArray();
            slotsProp.arraySize = loadout.Length;

            for (int i = 0; i < loadout.Length; i++)
            {
                var (path, binding, hold) = loadout[i];
                var skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                var elem  = slotsProp.GetArrayElementAtIndex(i);

                elem.FindPropertyRelative("binding").enumValueIndex = (int)binding;
                elem.FindPropertyRelative("holdToActivate").boolValue = hold;
                var skillProp = elem.FindPropertyRelative("skill");
                if (skillProp != null && skill != null)
                    skillProp.objectReferenceValue = skill;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── UI Canvas ─────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the complete HUD canvas: EventSystem, InventoryUI (self-building),
        /// LootWindowUI (self-building), PlayerHUD (auto-finds player stats),
        /// SkillBarHUD (auto-finds SkillExecutor + SkillBar).
        /// </summary>
        private static void BuildCanvas(Transform root)
        {
            // EventSystem (required for all UI interaction)
            var eventSys = new GameObject("EventSystem");
            eventSys.transform.SetParent(root);
            eventSys.AddComponent<EventSystem>();
            eventSys.AddComponent<StandaloneInputModule>();

            // Canvas
            var canvasGO = new GameObject("HUD Canvas");
            canvasGO.transform.SetParent(root);
            var canvas           = canvasGO.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 0;
            var scaler           = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode   = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // PlayerHUD — auto-finds health/stamina/mana from Player tag in Awake
            var hudGO = new GameObject("PlayerHUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            var rt = hudGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            hudGO.AddComponent<PlayerHUD>();

            // SkillBarHUD — auto-finds SkillExecutor + SkillBar in Awake
            // Must be full-screen so its internally-built slots can anchor to bottom-centre.
            var skillHudGO = new GameObject("SkillBarHUD");
            skillHudGO.transform.SetParent(canvasGO.transform, false);
            var srt = skillHudGO.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            skillHudGO.AddComponent<SkillBarHUD>();

            // InventoryUI — builds its own bag + equipment panel from code on press I
            var invGO = new GameObject("InventoryUI");
            invGO.transform.SetParent(canvasGO.transform, false);
            var invRt = invGO.AddComponent<RectTransform>();
            invRt.anchorMin = Vector2.zero;
            invRt.anchorMax = Vector2.one;
            invRt.offsetMin = invRt.offsetMax = Vector2.zero;
            var invUI = invGO.AddComponent<InventoryUI>();

            // Wire ItemRegistry into InventoryUI
            var itemReg = AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/Items/ItemRegistry.asset");
            if (itemReg != null)
            {
                var so = new SerializedObject(invUI);
                var regProp = so.FindProperty("itemRegistry");
                if (regProp != null) regProp.objectReferenceValue = itemReg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // LootWindowUI — builds itself lazily when LootWindowUI.Open(container) is called
            var lootGO = new GameObject("LootWindowUI");
            lootGO.transform.SetParent(canvasGO.transform, false);
            var lootRt = lootGO.AddComponent<RectTransform>();
            lootRt.anchorMin = Vector2.zero;
            lootRt.anchorMax = Vector2.one;
            lootRt.offsetMin = lootRt.offsetMax = Vector2.zero;
            lootGO.AddComponent<LootWindowUI>();

            // LevelUpNotification (CanvasGroup starts at alpha 0)
            var lvlGO = new GameObject("LevelUpNotification");
            lvlGO.transform.SetParent(canvasGO.transform, false);
            var lvlRt = lvlGO.AddComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0.5f, 0.7f);
            lvlRt.anchorMax = new Vector2(0.5f, 0.7f);
            lvlRt.sizeDelta = new Vector2(400f, 80f);
            lvlRt.anchoredPosition = Vector2.zero;
            lvlGO.AddComponent<CanvasGroup>();
            lvlGO.AddComponent<LevelUpNotification>();

            Debug.Log("[OpenWorldSceneBuilder] HUD canvas built.");
        }

        // ── Lighting ─────────────────────────────────────────────────────────

        private static void BuildLighting(Transform root)
        {
            var sunGO        = new GameObject("Sun");
            sunGO.transform.SetParent(root);
            var light        = sunGO.AddComponent<Light>();
            light.type       = LightType.Directional;
            light.color      = new Color(1f, 0.96f, 0.82f);
            light.intensity  = 1.15f;
            light.shadows    = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            // Starting atmosphere
            RenderSettings.fog         = true;
            RenderSettings.fogMode     = FogMode.Exponential;
            RenderSettings.fogDensity  = 0.003f;
            RenderSettings.fogColor    = new Color(0.62f, 0.66f, 0.68f);
            RenderSettings.ambientLight = new Color(0.38f, 0.38f, 0.42f);
        }

        // ── Primitive helpers ─────────────────────────────────────────────────

        private static GameObject MakePlane(
            string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            go.isStatic             = true;
            ApplyColor(go, color);
            return go;
        }

        private static void MakeBuilding(string name, Transform parent, Vector3 pos, Vector3 size)
        {
            var body                   = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name                  = name;
            body.transform.SetParent(parent);
            body.transform.position    = pos + Vector3.up * (size.y / 2f);
            body.transform.localScale  = size;
            body.isStatic              = true;
            ApplyColor(body, new Color(0.78f, 0.65f, 0.48f));

            var roof                   = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name                  = "Roof";
            roof.transform.SetParent(body.transform, false);
            roof.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            roof.transform.localScale    = new Vector3(1.14f, 0.20f, 1.14f);
            roof.isStatic               = true;
            ApplyColor(roof, new Color(0.38f, 0.20f, 0.10f));

            var door                   = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name                  = "Door";
            door.transform.SetParent(body.transform, false);
            door.transform.localPosition = new Vector3(0f, -0.28f, 0.51f);
            door.transform.localScale    = new Vector3(0.16f, 0.48f, 0.04f);
            ApplyColor(door, new Color(0.28f, 0.16f, 0.08f));
        }

        private static void MakeWell(string name, Transform parent, Vector3 pos)
        {
            var well                   = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            well.name                  = name;
            well.transform.SetParent(parent);
            well.transform.position    = pos + Vector3.up * 0.5f;
            well.transform.localScale  = new Vector3(1.4f, 0.5f, 1.4f);
            well.isStatic              = true;
            ApplyColor(well, new Color(0.52f, 0.52f, 0.52f));
        }

        private static void MakeFountain(string name, Transform parent, Vector3 pos)
        {
            var base1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            base1.name = name;
            base1.transform.SetParent(parent);
            base1.transform.position   = pos + Vector3.up * 0.2f;
            base1.transform.localScale = new Vector3(3f, 0.2f, 3f);
            base1.isStatic             = true;
            ApplyColor(base1, new Color(0.55f, 0.55f, 0.6f));
        }

        private static void BuildFenceRing(Transform parent, Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * step * Mathf.Deg2Rad;
                float next  = (i + 1) * step * Mathf.Deg2Rad;

                Vector3 posA = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 posB = center + new Vector3(Mathf.Cos(next)  * radius, 0, Mathf.Sin(next)  * radius);
                Vector3 mid  = (posA + posB) / 2f;
                float   len  = Vector3.Distance(posA, posB);

                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = $"Fence_{i}";
                post.transform.SetParent(parent);
                post.transform.position    = mid + Vector3.up * 0.75f;
                post.transform.localScale  = new Vector3(0.12f, 1.5f, len * 0.98f);
                post.transform.LookAt(posB + Vector3.up * 0.75f);
                post.isStatic              = true;
                ApplyColor(post, new Color(0.40f, 0.28f, 0.16f));
            }
        }

        private static void MakeLanternPost(string name, Transform parent, Vector3 pos)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = name;
            pole.transform.SetParent(parent);
            pole.transform.position   = pos + Vector3.up * 1.5f;
            pole.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
            pole.isStatic             = true;
            ApplyColor(pole, new Color(0.25f, 0.18f, 0.10f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "LanternHead";
            head.transform.SetParent(pole.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            head.transform.localScale    = new Vector3(1.5f, 0.5f, 1.5f);
            ApplyColor(head, new Color(0.9f, 0.8f, 0.4f)); // warm glow colour
        }

        private static void MakeTree(
            string name, Transform parent, Vector3 pos,
            Color foliageColor, float height = 3f, float trunkHeight = 2f)
        {
            var root                   = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position    = pos;
            root.isStatic              = true;

            var trunk                  = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name                 = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight / 2f, 0f);
            trunk.transform.localScale = new Vector3(0.28f, trunkHeight / 2f, 0.28f);
            trunk.isStatic             = true;
            ApplyColor(trunk, new Color(0.32f, 0.20f, 0.10f));

            var canopy                 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name                = "Canopy";
            canopy.transform.SetParent(root.transform, false);
            canopy.transform.localPosition = new Vector3(0f, trunkHeight + height * 0.45f, 0f);
            canopy.transform.localScale = new Vector3(height, height, height);
            canopy.isStatic            = true;
            ApplyColor(canopy, foliageColor);
        }

        private static void MakeRock(string name, Transform parent, Vector3 pos)
        {
            var rock                   = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name                  = name;
            rock.transform.SetParent(parent);
            rock.transform.position    = pos + Vector3.up * 0.4f;
            rock.transform.localScale  = new Vector3(1.5f, 0.8f, 1.2f);
            rock.isStatic              = true;
            ApplyColor(rock, new Color(0.48f, 0.46f, 0.44f));
        }

        private static void MakeRuin(string name, Transform parent, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            // Scattered broken stone blocks of varying sizes
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < 5; i++)
            {
                float x = (float)(rng.NextDouble() * 8 - 4);
                float z = (float)(rng.NextDouble() * 8 - 4);
                float h = 0.3f + (float)(rng.NextDouble() * 1.4f);
                float w = 0.8f + (float)(rng.NextDouble() * 1.2f);

                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Block_{i}";
                block.transform.SetParent(root.transform, false);
                block.transform.localPosition = new Vector3(x, h / 2f, z);
                block.transform.localScale    = new Vector3(w, h, w * 0.9f);
                block.transform.localRotation = Quaternion.Euler(0, (float)(rng.NextDouble() * 30), 0);
                block.isStatic = true;
                ApplyColor(block, new Color(0.38f, 0.36f, 0.33f));
            }
        }

        // ── Spawn helpers ─────────────────────────────────────────────────────

        private static void MakeSpawnPoint(
            string goName, Transform parent, Vector3 pos, string spawnId, bool isDefault)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var sp   = go.AddComponent<PlayerSpawnPoint>();
            sp.spawnId   = spawnId;
            sp.isDefault = isDefault;
        }

        private static void MakeAreaTrigger(
            string goName, Transform parent, AreaDefinition def, Vector3 center, Vector3 size)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = center;

            var col    = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center    = Vector3.up * (size.y / 2f);
            col.size      = size;

            var trigger = go.AddComponent<AreaTrigger>();
            var so      = new SerializedObject(trigger);
            var defProp = so.FindProperty("definition");
            if (defProp != null) defProp.objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceSpawner(
            string goName, Transform parent, string defRelativePath,
            Vector3 pos, int maxAlive, float radius, float respawnDelay)
        {
            string assetPath = $"Assets/Characters/Mobs/Data/{defRelativePath}.asset";
            var    def       = AssetDatabase.LoadAssetAtPath<MobDefinition>(assetPath);
            if (def == null)
            {
                Debug.LogWarning($"[OpenWorldSceneBuilder] Missing mob definition at {assetPath}");
                return;
            }

            var go      = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            var spawner = go.AddComponent<MobSpawner>();
            var so      = new SerializedObject(spawner);
            Set(so, "definition",    def);
            Set(so, "maxAlive",      maxAlive);
            Set(so, "spawnRadius",   radius);
            Set(so, "respawnDelay",  respawnDelay);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                ?? Shader.Find("Standard"));
            mat.color = color;
            r.sharedMaterial = mat;
        }

        private static AreaDefinition LoadArea(string areaId)
        {
            var guids = AssetDatabase.FindAssets($"t:AreaDefinition");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def  = AssetDatabase.LoadAssetAtPath<AreaDefinition>(path);
                if (def != null && def.areaId == areaId) return def;
            }
            Debug.LogWarning($"[OpenWorldSceneBuilder] AreaDefinition with areaId='{areaId}' not found. " +
                             "Run Kaligo → World → Create Area Definitions first.");
            return null;
        }

        private static void Set(SerializedObject so, string field, object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null) return;
            switch (value)
            {
                case UnityEngine.Object obj: prop.objectReferenceValue = obj; break;
                case int    i: prop.intValue   = i; break;
                case float  f: prop.floatValue = f; break;
                case bool   b: prop.boolValue  = b; break;
                case string s: prop.stringValue = s; break;
            }
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
#endif
