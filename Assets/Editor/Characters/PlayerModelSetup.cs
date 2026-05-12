#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Linq;

namespace Kaligo.Editor.Characters
{
    /// <summary>
    /// Replaces the player's visual model with Chr_FantasyHero_Preset_1 from
    /// POLYGON Fantasy Hero Characters and wires the full SwordAndShield animation
    /// set to match every trigger the skill system fires.
    ///
    /// Key fixes vs previous version:
    ///   • Destroys old mixamorig:* bone children on the Player root before adding new visual
    ///   • Uses 3 explicit locomotion states (Idle/Walk/Run) driven by Speed float param
    ///   • Adds ALL skill triggers: LightAttack1–3 (SwordCombo), HeavyAttack, Dodge,
    ///     PowerUp, Heal, Jump, IsBlocking, IsDead
    ///   • Converts Standard-shader Synty materials to URP Lit
    ///     (SyntyStudios/CustomCharacter shader is left as-is — already URP-compatible)
    ///
    /// Run via: Kaligo → Setup → Setup Player Visual (Fantasy Hero)
    /// </summary>
    public static class PlayerModelSetup
    {
        // ── Paths ─────────────────────────────────────────────────────────────
        private const string HeroPrefab     = "Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_1.prefab";
        private const string CharactersFbx  = "Assets/PolygonFantasyHeroCharacters/Models/ModularCharacters.fbx";
        private const string ControllerPath = "Assets/Characters/Player/Animations/PlayerAnimator.controller";

        // SwordAndShield clips
        private const string SS = "Assets/Animations/SwordAndShield/";
        private const string ClipIdle        = SS + "Stance/sword and shield idle.fbx";
        private const string ClipWalk        = SS + "Locomotion/sword and shield walk.fbx";
        private const string ClipRun         = SS + "Locomotion/sword and shield run.fbx";
        // SwordCombo 3-hit chain — each step maps to one clip
        private const string ClipAttack1     = SS + "Combat/sword and shield slash.fbx";       // hit 1: slash
        private const string ClipAttack2     = SS + "Combat/sword and shield attack (2).fbx";  // hit 2
        private const string ClipAttack3     = SS + "Combat/sword and shield attack (3).fbx";  // hit 3
        // Other skills
        private const string ClipHeavy       = SS + "Combat/sword and shield attack.fbx";      // HeavySlash uses the base attack clip
        private const string ClipBlockIdle   = SS + "Stance/sword and shield block idle.fbx";
        private const string ClipPowerUp     = SS + "Combat/sword and shield power up.fbx";
        private const string ClipCasting     = SS + "Combat/sword and shield casting.fbx";
        private const string ClipDeath       = SS + "Death/sword and shield death.fbx";
        private const string ClipJump        = SS + "Locomotion/sword and shield jump.fbx";

        // All material folders to scan for Standard-shader materials → URP conversion.
        // SyntyStudios/CustomCharacter mats are skipped automatically (not "Standard").
        private static readonly string[] MatFolders =
        {
            "Assets/PolygonFantasyHeroCharacters/Materials",
            "Assets/PolygonDungeon/Materials",
            "Assets/PolygonFantasyKingdom/Materials",
        };

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Kaligo/Setup/Setup Player Visual (Fantasy Hero)")]
        public static void Run()
        {
            // 1. Find player in open scene
            var playerCtrl = Object.FindObjectOfType<PlayerController>();
            if (playerCtrl == null)
            {
                EditorUtility.DisplayDialog("Scene Required",
                    "Open World_Kaligo scene first, then run this tool.", "OK");
                return;
            }
            var player = playerCtrl.gameObject;

            // 2. Convert Standard-shader Synty materials to URP Lit
            ConvertAllSyntyMaterials();

            // 3. Ensure SwordAndShield FBX files are Humanoid
            EnsureHumanoid(ClipIdle, ClipWalk, ClipRun,
                           ClipAttack1, ClipAttack2, ClipAttack3, ClipHeavy,
                           ClipBlockIdle, ClipPowerUp, ClipCasting,
                           ClipDeath, ClipJump);
            AssetDatabase.Refresh();

            // 4. Remove old visual remnants (mixamorig bones, old PlayerVisual)
            RemoveOldVisuals(player.transform);

            // 5. Add Fantasy Hero as new visual child
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefab);
            if (heroPrefab == null)
            { Debug.LogError("[PlayerSetup] Fantasy Hero prefab not found: " + HeroPrefab); return; }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, player.transform);
            visual.name = "PlayerVisual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale    = Vector3.one;

            // 6. Remove the Knight's own Animator — parent Animator drives it
            var childAnim = visual.GetComponent<Animator>();
            if (childAnim != null) Object.DestroyImmediate(childAnim);

            // 7. Assign Avatar + controller to the parent Animator
            var parentAnim = player.GetComponent<Animator>();
            if (parentAnim == null) parentAnim = player.AddComponent<Animator>();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(CharactersFbx).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) { parentAnim.avatar = avatar; Debug.Log("[PlayerSetup] Avatar set: " + avatar.name); }
            else Debug.LogWarning("[PlayerSetup] Avatar not found in: " + CharactersFbx);

            // 8. Rebuild controller
            var ctrl = BuildPlayerController();
            if (ctrl != null) parentAnim.runtimeAnimatorController = ctrl;

            // 9. Re-enable WeaponHitbox if it was accidentally disabled.
            //    HitboxController manages its OWN collider on/off; the GO must be active
            //    so OnTriggerEnter can fire.
            var weaponHitboxGO = player.transform.Find("WeaponHitbox")?.gameObject;
            if (weaponHitboxGO != null && !weaponHitboxGO.activeSelf)
            {
                weaponHitboxGO.SetActive(true);
                Debug.Log("[PlayerSetup] WeaponHitbox re-enabled (was disabled).");
            }

            // 10. Save scene
            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(player.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[PlayerSetup] Done.");
            EditorUtility.DisplayDialog("Player Setup Complete",
                "Fantasy Hero visual added to: " + player.name + "\n" +
                "Controller: " + ControllerPath + "\n\n" +
                "Press Play and test WASD + LMB combos.", "Nice!");
        }

        // ── Remove old visual children ────────────────────────────────────────

        private static void RemoveOldVisuals(Transform playerRoot)
        {
            // Collect first (can't delete while iterating children)
            var toDestroy = new System.Collections.Generic.List<GameObject>();

            foreach (Transform child in playerRoot)
            {
                // Old mixamorig skeleton bones
                if (child.name.StartsWith("mixamorig") || child.name.StartsWith("mixamorig:"))
                { toDestroy.Add(child.gameObject); continue; }

                // Old PlayerVisual from a previous run
                if (child.name == "PlayerVisual")
                { toDestroy.Add(child.gameObject); continue; }

                // Any X Bot root (has SkinnedMeshRenderer with no scripts)
                if (child.name.Contains("Beta") || child.name.Contains("XBot") || child.name.Contains("X Bot"))
                { toDestroy.Add(child.gameObject); continue; }
            }

            foreach (var go in toDestroy)
            {
                Debug.Log("[PlayerSetup] Removing old visual: " + go.name);
                Object.DestroyImmediate(go);
            }
        }

        // ── URP material conversion ───────────────────────────────────────────

        private static void ConvertAllSyntyMaterials()
        {
            var urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) { Debug.LogError("[PlayerSetup] URP Lit shader not found."); return; }

            int converted = 0;
            foreach (var folder in MatFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    string shaderName = mat.shader.name;

                    if (shaderName.Contains("Standard") && !shaderName.Contains("Universal"))
                    {
                        // Standard (Built-in) → URP Lit
                        Texture mainTex = mat.GetTexture("_MainTex");
                        Color   color   = mat.HasProperty("_Color") ? mat.color : Color.white;
                        mat.shader = urpShader;
                        if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                        mat.SetColor("_BaseColor", color);
                        EditorUtility.SetDirty(mat);
                        converted++;
                    }
                    else if (shaderName == "SyntyStudios/CustomCharacter")
                    {
                        // Amplify Surface Shader (Built-in) — no URP equivalent.
                        // Map: _Texture → _BaseMap, _Color_Primary → _BaseColor.
                        // The mask-based tinting is lost but the model will no longer be pink.
                        Texture baseTex = mat.HasProperty("_Texture") ? mat.GetTexture("_Texture") : null;
                        Color   primary = mat.HasProperty("_Color_Primary") ? mat.GetColor("_Color_Primary") : Color.white;
                        mat.shader = urpShader;
                        if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
                        mat.SetColor("_BaseColor", primary);
                        EditorUtility.SetDirty(mat);
                        converted++;
                    }
                }
            }
            if (converted > 0) AssetDatabase.SaveAssets();
            Debug.Log("[PlayerSetup] Converted " + converted + " materials to URP Lit.");
        }

        // ── Humanoid rig ──────────────────────────────────────────────────────

        private static void EnsureHumanoid(params string[] paths)
        {
            foreach (var path in paths)
            {
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null || imp.animationType == ModelImporterAnimationType.Human) continue;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.SaveAndReimport();
                Debug.Log("[PlayerSetup] Humanoid rig set: " + path);
            }
        }

        // ── Build PlayerAnimator controller ───────────────────────────────────

        private static AnimatorController BuildPlayerController()
        {
            var idle      = LoadClip(ClipIdle,      required: true);
            var walk      = LoadClip(ClipWalk,      required: true);
            var run       = LoadClip(ClipRun,       required: true);
            var attack1   = LoadClip(ClipAttack1,   required: true);
            var attack2   = LoadClip(ClipAttack2,   required: false) ?? attack1;
            var attack3   = LoadClip(ClipAttack3,   required: false) ?? attack2;
            var heavy     = LoadClip(ClipHeavy,     required: false) ?? attack1;
            var blockIdle = LoadClip(ClipBlockIdle, required: false) ?? idle;
            var powerUp   = LoadClip(ClipPowerUp,   required: false) ?? idle;
            var casting   = LoadClip(ClipCasting,   required: false) ?? idle;
            var death     = LoadClip(ClipDeath,     required: false) ?? idle;
            var jump      = LoadClip(ClipJump,      required: false) ?? idle;

            if (idle == null || walk == null || run == null || attack1 == null)
            { Debug.LogError("[PlayerSetup] Missing essential clips."); return null; }

            if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            EnsureFolder("Assets/Characters/Player/Animations");
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // ── Parameters ───────────────────────────────────────────────────
            // Speed: 0=idle, 0–0.6=walk, 0.6–1=run (driven by PlayerController)
            ctrl.AddParameter("Speed",        AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelocityX",    AnimatorControllerParameterType.Float);
            ctrl.AddParameter("VelocityZ",    AnimatorControllerParameterType.Float);
            // SwordCombo — 3 steps
            ctrl.AddParameter("LightAttack1", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("LightAttack2", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("LightAttack3", AnimatorControllerParameterType.Trigger);
            // Other skills (match exact names in SkillData.animatorTrigger)
            ctrl.AddParameter("HeavyAttack",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dodge",        AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("PowerUp",      AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Heal",         AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Jump",         AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("IsBlocking",   AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsDead",       AnimatorControllerParameterType.Bool);

            var root = ctrl.layers[0].stateMachine;

            // ── Locomotion: 3 explicit states driven by Speed ─────────────────
            // Avoids blend tree serialization issues in editor scripts.
            // Speed = horizontalVelocity.magnitude / runSpeed (set by PlayerController)
            var sIdle = AddState(root, "Idle", idle);
            var sWalk = AddState(root, "Walk", walk);
            var sRun  = AddState(root, "Run",  run);
            root.defaultState = sIdle;

            // Idle ↔ Walk
            Tr(sIdle, sWalk, "Speed", AnimatorConditionMode.Greater, hasExitTime: false, dur: 0.15f, threshold: 0.15f);
            Tr(sWalk, sIdle, "Speed", AnimatorConditionMode.Less,    hasExitTime: false, dur: 0.20f, threshold: 0.10f);
            // Walk ↔ Run
            Tr(sWalk, sRun,  "Speed", AnimatorConditionMode.Greater, hasExitTime: false, dur: 0.15f, threshold: 0.60f);
            Tr(sRun,  sWalk, "Speed", AnimatorConditionMode.Less,    hasExitTime: false, dur: 0.20f, threshold: 0.55f);

            // ── Combat / skill states ─────────────────────────────────────────
            var sAttack1 = AddState(root, "LightAttack1", attack1);
            var sAttack2 = AddState(root, "LightAttack2", attack2);
            var sAttack3 = AddState(root, "LightAttack3", attack3);
            var sHeavy   = AddState(root, "HeavyAttack",  heavy);
            var sBlock   = AddState(root, "BlockIdle",    blockIdle);
            var sDodge   = AddState(root, "Dodge",        run);
            var sPowerUp = AddState(root, "PowerUp",      powerUp);
            var sHeal    = AddState(root, "Heal",         casting);
            var sJump    = AddState(root, "Jump",         jump);
            var sDead    = AddState(root, "Dead",         death);

            // ── Transitions ───────────────────────────────────────────────────

            // Block (hold) — from any locomotion state
            foreach (var s in new[] { sIdle, sWalk, sRun })
                Tr(s, sBlock, "IsBlocking", AnimatorConditionMode.If, hasExitTime: false, dur: 0.15f);
            Tr(sBlock, sIdle, "IsBlocking", AnimatorConditionMode.IfNot, hasExitTime: false, dur: 0.15f);

            // ── SwordCombo chain (3 hits) ──────────────────────────────────────
            // AnyState → Attack1 (starts combo from any state)
            AnyTr(root, sAttack1, "LightAttack1", dur: 0.1f);
            // Attack1 → Attack2: direct transition wins over AnyState (checked first)
            Tr(sAttack1, sAttack2, "LightAttack2", AnimatorConditionMode.If, hasExitTime: false, dur: 0.05f);
            Tr(sAttack1, sIdle,    hasExitTime: true, exit: 0.85f, dur: 0.15f);
            // AnyState → Attack2 (in case we're not in Attack1 somehow)
            AnyTr(root, sAttack2, "LightAttack2", dur: 0.05f);
            Tr(sAttack2, sAttack3, "LightAttack3", AnimatorConditionMode.If, hasExitTime: false, dur: 0.05f);
            Tr(sAttack2, sIdle,    hasExitTime: true, exit: 0.85f, dur: 0.15f);
            // AnyState → Attack3
            AnyTr(root, sAttack3, "LightAttack3", dur: 0.05f);
            Tr(sAttack3, sIdle,    hasExitTime: true, exit: 0.9f,  dur: 0.15f);

            // HeavyAttack
            AnyTr(root, sHeavy, "HeavyAttack", dur: 0.1f);
            Tr(sHeavy, sIdle, hasExitTime: true, exit: 0.85f, dur: 0.15f);

            // Dodge
            AnyTr(root, sDodge, "Dodge", dur: 0.05f);
            Tr(sDodge, sIdle, hasExitTime: true, exit: 0.6f, dur: 0.1f);

            // PowerUp
            AnyTr(root, sPowerUp, "PowerUp", dur: 0.15f);
            Tr(sPowerUp, sIdle, hasExitTime: true, exit: 0.9f, dur: 0.15f);

            // Heal
            AnyTr(root, sHeal, "Heal", dur: 0.15f);
            Tr(sHeal, sIdle, hasExitTime: true, exit: 0.9f, dur: 0.15f);

            // Jump
            AnyTr(root, sJump, "Jump", dur: 0.1f);
            Tr(sJump, sIdle, hasExitTime: true, exit: 0.9f, dur: 0.15f);

            // Dead — no return
            var td = root.AddAnyStateTransition(sDead);
            td.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            td.hasExitTime = false; td.duration = 0.1f; td.canTransitionToSelf = false;

            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip)
        {
            var s = sm.AddState(name);
            s.motion = clip;
            return s;
        }

        private static void Tr(AnimatorState from, AnimatorState to,
            string param = null, AnimatorConditionMode mode = AnimatorConditionMode.If,
            bool hasExitTime = true, float exit = 0.9f, float dur = 0.1f,
            float threshold = 0f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExitTime; t.exitTime = exit; t.duration = dur;
            if (param != null) t.AddCondition(mode, threshold, param);
        }

        private static void AnyTr(AnimatorStateMachine sm, AnimatorState to,
            string trigger, float dur = 0.1f)
        {
            var t = sm.AddAnyStateTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false; t.duration = dur; t.canTransitionToSelf = false;
        }

        private static AnimationClip LoadClip(string path, bool required)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                          .OfType<AnimationClip>()
                          .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null && required) Debug.LogError("[PlayerSetup] Missing clip: " + path);
            else if (clip == null)        Debug.LogWarning("[PlayerSetup] Optional clip missing (using fallback): " + path);
            else                          Debug.Log("[PlayerSetup] Loaded: " + clip.name);
            return clip;
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
