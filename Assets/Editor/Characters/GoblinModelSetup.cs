#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

namespace Kaligo.Editor.Characters
{
    /// <summary>
    /// One-shot setup for the Goblin mob using real Mixamo FBX animations and the
    /// POLYGON Dungeon Character_Goblin_Male prefab.
    ///
    /// Fixes vs. previous version:
    ///   • No extra Animator on the wrapper root (which had no Avatar → static model).
    ///     Instead, our controller is assigned directly to the child Goblin's Animator,
    ///     which already carries the humanoid Avatar from Characters.fbx.
    ///   • Converts Dungeons_Material_Characters_01.mat from Standard → URP Lit so
    ///     the Goblin doesn't appear pink in a URP project.
    ///
    /// Run via: Kaligo → Build Models → Setup Goblin (Real Animations)
    /// </summary>
    public static class GoblinModelSetup
    {
        // ── Paths ─────────────────────────────────────────────────────────────
        private const string AnimDir       = "Assets/Characters/Mobs/Animations/Goblin";
        private const string AttackSrc     = "Assets/Animations/SwordAndShield/Combat/sword and shield attack.fbx";
        private const string ControllerPath= AnimDir + "/Goblin_Controller.controller";
        private const string WrapperPath   = "Assets/Characters/Mobs/Models/GoblinVisual.prefab";
        private const string GoblinDefPath = "Assets/Characters/Mobs/Data/Aggressive/Goblin.asset";
        private const string DungeonGoblin = "Assets/PolygonDungeon/Prefabs/Characters/Character_Goblin_Male.prefab";
        private const string CharMat       = "Assets/PolygonDungeon/Materials/Dungeons_Material_Characters_01.mat";

        private static readonly (string rawName, string cleanName)[] ClipRenames =
        {
            ("Breathing Idle",                 "Goblin_Idle"),
            ("Walking",                        "Goblin_Walk"),
            ("Standing React Small From Left", "Goblin_Hit"),
            ("Standing Death Forward 02",      "Goblin_Die"),
        };

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Kaligo/Build Models/Setup Goblin (Real Animations)")]
        public static void Run()
        {
            // 0. Convert Dungeon character material to URP (fixes pink)
            ConvertMaterialToURP(CharMat);

            // 1. Delete old procedural stubs
            DeleteOldStubs();

            // 2. Set all Mixamo FBX files to Humanoid rig
            SetHumanoid(AttackSrc);
            foreach (var (rawName, _) in ClipRenames)
                SetHumanoid(AnimDir + "/" + rawName + ".fbx");
            AssetDatabase.Refresh();

            // 3. Rename messy Mixamo filenames → clean Goblin_*.fbx
            foreach (var (rawName, cleanName) in ClipRenames)
                RenameFbx(AnimDir, rawName, cleanName);
            AssetDatabase.Refresh();

            // 4. Build AnimatorController from real clips
            var ctrl = BuildController();
            if (ctrl == null) { Debug.LogError("[GoblinSetup] Controller build failed."); return; }

            // 5. Build wrapper prefab — controller goes on the child's Animator (has Avatar)
            BuildWrapperPrefab(ctrl);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GoblinSetup] Done.");
            EditorUtility.DisplayDialog("Goblin Setup Complete",
                "Wrapper:    " + WrapperPath + "\nController: " + ControllerPath +
                "\n\nGoblin.asset → prefabOverride updated.", "Nice!");
        }

        // ── Step 0: URP material fix ──────────────────────────────────────────

        private static void ConvertMaterialToURP(string matPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { Debug.LogWarning("[GoblinSetup] Material not found: " + matPath); return; }
            if (!mat.shader.name.Contains("Standard")) { Debug.Log("[GoblinSetup] Material already non-Standard: " + matPath); return; }

            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) { Debug.LogError("[GoblinSetup] URP Lit shader not found — is this a URP project?"); return; }

            // Preserve the texture atlas before changing shader
            Texture mainTex = mat.GetTexture("_MainTex");
            Color   color   = mat.color;

            mat.shader = urpShader;
            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[GoblinSetup] Converted material to URP Lit: " + matPath);
        }

        // ── Step 1: delete old procedural stubs ───────────────────────────────

        private static void DeleteOldStubs()
        {
            string[] stubs = { "Goblin_Idle.anim","Goblin_Walk.anim","Goblin_Attack.anim",
                               "Goblin_Hit.anim","Goblin_Die.anim","Goblin_Controller.controller" };
            foreach (var name in stubs)
            {
                string p = AnimDir + "/" + name;
                if (AssetDatabase.LoadAssetAtPath<Object>(p) != null)
                { AssetDatabase.DeleteAsset(p); Debug.Log("[GoblinSetup] Deleted: " + p); }
            }
        }

        // ── Step 2: set Humanoid rig ──────────────────────────────────────────

        private static void SetHumanoid(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) { Debug.LogWarning("[GoblinSetup] No importer: " + fbxPath); return; }
            if (importer.animationType == ModelImporterAnimationType.Human) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
            Debug.Log("[GoblinSetup] Humanoid rig set on: " + fbxPath);
        }

        // ── Step 3: rename FBX files ──────────────────────────────────────────

        private static void RenameFbx(string dir, string oldName, string newName)
        {
            string oldPath = dir + "/" + oldName + ".fbx";
            string newPath = dir + "/" + newName + ".fbx";
            if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) == null)
            { if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null) return; Debug.LogWarning("[GoblinSetup] Not found for rename: " + oldPath); return; }
            if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
            { AssetDatabase.DeleteAsset(oldPath); return; }
            string err = AssetDatabase.RenameAsset(oldPath, newName + ".fbx");
            if (!string.IsNullOrEmpty(err)) Debug.LogError("[GoblinSetup] Rename failed: " + err);
            else Debug.Log("[GoblinSetup] Renamed: " + oldName + " → " + newName);
        }

        // ── Step 4: build AnimatorController ─────────────────────────────────

        private static AnimatorController BuildController()
        {
            var idle   = LoadClip(AnimDir + "/Goblin_Idle.fbx");
            var walk   = LoadClip(AnimDir + "/Goblin_Walk.fbx");
            var attack = LoadClip(AttackSrc);
            var hit    = LoadClip(AnimDir + "/Goblin_Hit.fbx");
            var die    = LoadClip(AnimDir + "/Goblin_Die.fbx");
            if (idle == null || walk == null || attack == null || hit == null || die == null) return null;

            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Speed",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsHit",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var root    = ctrl.layers[0].stateMachine;
            var sIdle   = root.AddState("Idle");   sIdle.motion   = idle;
            var sWalk   = root.AddState("Walk");   sWalk.motion   = walk;
            var sAttack = root.AddState("Attack"); sAttack.motion = attack;
            var sHit    = root.AddState("Hit");    sHit.motion    = hit;
            var sDie    = root.AddState("Die");    sDie.motion    = die;
            root.defaultState = sIdle;

            AddTransition(sIdle,   sWalk,   "Speed",  AnimatorConditionMode.Greater, 0.1f, false, 0.15f);
            AddTransition(sWalk,   sIdle,   "Speed",  AnimatorConditionMode.Less,    0.1f, false, 0.15f);
            AddAnyTransition(root, sAttack, "Attack", false, 0.10f);
            AddTransition(sAttack, sIdle,   exitTime: 0.9f, duration: 0.1f);
            AddAnyTransition(root, sHit,    "IsHit",  false, 0.05f);
            AddTransition(sHit,    sIdle,   exitTime: 0.9f, duration: 0.1f);
            AddAnyTransition(root, sDie,    "IsDead", false, 0.10f);

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ── Step 5: build wrapper prefab ─────────────────────────────────────

        private static void BuildWrapperPrefab(AnimatorController ctrl)
        {
            var dungeonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonGoblin);
            if (dungeonPrefab == null) { Debug.LogError("[GoblinSetup] Dungeon Goblin not found: " + DungeonGoblin); return; }

            var root = new GameObject("GoblinVisual");

            // Instantiate the Dungeon Goblin as a child — it brings its own Animator + Avatar
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(dungeonPrefab, root.transform);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            mesh.transform.localScale    = Vector3.one;

            // *** Key fix: assign our controller to the CHILD's Animator, which already
            //     has the humanoid Avatar from Characters.fbx baked in. ***
            var childAnim = mesh.GetComponent<Animator>();
            if (childAnim != null)
                childAnim.runtimeAnimatorController = ctrl;
            else
                Debug.LogWarning("[GoblinSetup] No Animator found on child Goblin prefab.");

            // Save wrapper — no Animator on root; MobBrain finds it via GetComponentInChildren
            EnsureFolder("Assets/Characters/Mobs/Models");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, WrapperPath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok) { Debug.LogError("[GoblinSetup] Failed to save wrapper prefab."); return; }

            // Stamp Goblin.asset
            var def = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GoblinDefPath);
            if (def == null) { Debug.LogWarning("[GoblinSetup] Goblin.asset not found."); return; }
            var so = new SerializedObject(def);
            var field = so.FindProperty("prefabOverride");
            if (field != null) { field.objectReferenceValue = prefab; so.ApplyModifiedProperties(); EditorUtility.SetDirty(def); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AnimationClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                          .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) Debug.LogError("[GoblinSetup] No clip in: " + path);
            else Debug.Log("[GoblinSetup] Loaded clip '" + clip.name + "' from " + path);
            return clip;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to,
            string param = null, AnimatorConditionMode mode = AnimatorConditionMode.If,
            float threshold = 0f, bool hasExitTime = true,
            float duration = 0.1f, float exitTime = 0.9f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExitTime; t.duration = duration; t.exitTime = exitTime;
            if (param != null) t.AddCondition(mode, threshold, param);
        }

        private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState to,
            string param, bool hasExitTime, float duration)
        {
            var t = sm.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0, param);
            t.hasExitTime = hasExitTime; t.duration = duration; t.canTransitionToSelf = false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/'); string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            { string next = cur + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]); cur = next; }
        }
    }
}
#endif
