#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Kaligo.Editor.Characters
{
    /// <summary>
    /// Procedurally builds the Giant Rat visual prefab, five animation clips
    /// (Idle, Walk, Attack, Hit, Die) and a fully-wired AnimatorController, then
    /// stamps the result onto the Rat MobDefinition asset.
    ///
    /// Run via: Kaligo → Build Models → Create Rat Model
    /// </summary>
    public static class RatModelCreator
    {
        // ── Asset paths ───────────────────────────────────────────────────────

        private const string ModelFolder    = "Assets/Characters/Mobs/Models";
        private const string AnimFolder     = "Assets/Characters/Mobs/Animations/Rat";
        private const string PrefabPath     = "Assets/Characters/Mobs/Models/RatVisual.prefab";
        private const string ControllerPath = "Assets/Characters/Mobs/Animations/Rat/Rat_Controller.controller";
        private const string RatAssetPath   = "Assets/Characters/Mobs/Data/Aggressive/Rat.asset";

        // Visual prefab root name — animation paths are relative to this object
        // (Animator sits on the mob root which is the *parent* of this visual).
        private const string VisualName = "RatVisual";

        // ── Entry point ───────────────────────────────────────────────────────

        [MenuItem("Kaligo/Build Models/Create Rat Model")]
        public static void CreateRatModel()
        {
            EnsureFolders();

            // 1. Build in-memory visual hierarchy
            var visualGO = BuildVisualHierarchy();

            // 2. Create + save animation clips
            var idleClip   = BuildIdleClip();
            var walkClip   = BuildWalkClip();
            var attackClip = BuildAttackClip();
            var hitClip    = BuildHitClip();
            var dieClip    = BuildDieClip();

            SaveClip(idleClip,   AnimFolder + "/Rat_Idle.anim");
            SaveClip(walkClip,   AnimFolder + "/Rat_Walk.anim");
            SaveClip(attackClip, AnimFolder + "/Rat_Attack.anim");
            SaveClip(hitClip,    AnimFolder + "/Rat_Hit.anim");
            SaveClip(dieClip,    AnimFolder + "/Rat_Die.anim");

            // 3. Build AnimatorController (saves itself to disk internally)
            var controller = BuildAnimatorController(idleClip, walkClip, attackClip, hitClip, dieClip);

            // 4. Wire Animator on visual root → controller
            var anim = visualGO.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // 5. Save materials to disk FIRST so prefab references valid assets
            SaveMaterialsToDisk(visualGO, MatFolder);

            // 6. Save prefab (all material refs now point to real .mat files)
            bool success;
            var prefab = PrefabUtility.SaveAsPrefabAsset(visualGO, PrefabPath, out success);
            Object.DestroyImmediate(visualGO);

            if (!success)
            {
                Debug.LogError("[RatModelCreator] Failed to save prefab.");
                return;
            }

            // 6. Stamp Rat.asset
            StampDefinition(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RatModelCreator] Done — Rat model, animations and controller created.");
            EditorUtility.DisplayDialog(
                "Rat Model Created",
                "Prefab:     " + PrefabPath + "\n" +
                "Controller: " + ControllerPath + "\n" +
                "Clips:      " + AnimFolder + "/\n\n" +
                "Rat.asset has been updated with the new prefabOverride.",
                "Nice!");
        }

        // ═════════════════════════════════════════════════════════════════════
        // Visual Hierarchy
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildVisualHierarchy()
        {
            var root = new GameObject(VisualName);

            // ── Colour palette ──────────────────────────────────────────────
            var cBody   = new Color(0.33f, 0.22f, 0.14f);   // dark warm brown
            var cHead   = new Color(0.40f, 0.27f, 0.17f);   // slightly lighter
            var cEar    = new Color(0.72f, 0.47f, 0.42f);   // pinkish
            var cEye    = new Color(0.06f, 0.04f, 0.04f);   // near black
            var cSnout  = new Color(0.50f, 0.31f, 0.24f);   // warm mid-brown
            var cTail   = new Color(0.60f, 0.50f, 0.45f);   // pinkish-grey

            // ── Body (elongated sphere, sits low to ground) ─────────────────
            // Rat CharacterController: height=0.6m, radius=0.25m
            // Visual body intentionally stays well inside that hull.
            MakePart(root.transform, "Body",
                PrimitiveType.Sphere,
                pos:   new Vector3( 0f,    0.13f,  0f),
                scale: new Vector3( 0.28f, 0.20f,  0.40f),
                color: cBody);

            // ── Head ────────────────────────────────────────────────────────
            MakePart(root.transform, "Head",
                PrimitiveType.Sphere,
                pos:   new Vector3( 0f,    0.21f,  0.22f),
                scale: new Vector3( 0.19f, 0.18f,  0.20f),
                color: cHead);

            // ── Snout (protrudes forward from head) ─────────────────────────
            MakePart(root.transform, "Snout",
                PrimitiveType.Sphere,
                pos:   new Vector3( 0f,    0.17f,  0.32f),
                scale: new Vector3( 0.10f, 0.08f,  0.12f),
                color: cSnout);

            // ── Ears (flat ovals on top of head) ────────────────────────────
            MakePart(root.transform, "LeftEar",
                PrimitiveType.Sphere,
                pos:   new Vector3(-0.07f, 0.30f,  0.20f),
                scale: new Vector3( 0.07f, 0.10f,  0.04f),
                color: cEar);

            MakePart(root.transform, "RightEar",
                PrimitiveType.Sphere,
                pos:   new Vector3( 0.07f, 0.30f,  0.20f),
                scale: new Vector3( 0.07f, 0.10f,  0.04f),
                color: cEar);

            // ── Eyes ────────────────────────────────────────────────────────
            MakePart(root.transform, "LeftEye",
                PrimitiveType.Sphere,
                pos:   new Vector3(-0.08f, 0.225f, 0.305f),
                scale: Vector3.one * 0.042f,
                color: cEye);

            MakePart(root.transform, "RightEye",
                PrimitiveType.Sphere,
                pos:   new Vector3( 0.08f, 0.225f, 0.305f),
                scale: Vector3.one * 0.042f,
                color: cEye);

            // ── Legs — each is a pivot empty + a hanging capsule visual ──────
            // Hip pivot positions match the animation keyframe defaults exactly.
            //   FrontLeft / FrontRight default pos: (±0.09, 0.12, 0.14)
            //   BackLeft  / BackRight  default pos: (±0.09, 0.12,-0.12)
            MakeLeg(root.transform, "FrontLeft",
                pivotPos: new Vector3(-0.09f, 0.12f,  0.14f), color: cBody);
            MakeLeg(root.transform, "FrontRight",
                pivotPos: new Vector3( 0.09f, 0.12f,  0.14f), color: cBody);
            MakeLeg(root.transform, "BackLeft",
                pivotPos: new Vector3(-0.09f, 0.12f, -0.12f), color: cBody);
            MakeLeg(root.transform, "BackRight",
                pivotPos: new Vector3( 0.09f, 0.12f, -0.12f), color: cBody);

            // ── Tail — three-segment chain behind the body ──────────────────
            MakeTail(root.transform, cTail);

            return root;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject MakePart(Transform parent, string name,
            PrimitiveType pType, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(pType);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            ApplyColor(go, color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void MakeLeg(Transform parent, string name, Vector3 pivotPos, Color color)
        {
            // Pivot empty — this is what the walk animation drives (localPosition)
            var pivot = new GameObject(name + "Hip");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;

            // Capsule hangs below the pivot
            var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = name + "Leg";
            leg.transform.SetParent(pivot.transform, false);
            leg.transform.localPosition = new Vector3(0f, -0.055f, 0f);
            leg.transform.localScale    = new Vector3(0.048f, 0.055f, 0.048f);
            ApplyColor(leg, color);
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }

        private static void MakeTail(Transform parent, Color color)
        {
            // TailRoot — driven by walk/idle Y-rotation wag
            var tailRoot = new GameObject("TailRoot");
            tailRoot.transform.SetParent(parent, false);
            tailRoot.transform.localPosition = new Vector3(0f, 0.12f, -0.18f);

            // Segment 1 — widest, closest to body
            var seg1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            seg1.name = "TailSeg1";
            seg1.transform.SetParent(tailRoot.transform, false);
            seg1.transform.localPosition = new Vector3(0f, 0f, -0.07f);
            seg1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // capsule along Z
            seg1.transform.localScale    = new Vector3(0.034f, 0.065f, 0.034f);
            ApplyColor(seg1, color);
            Object.DestroyImmediate(seg1.GetComponent<Collider>());

            // Segment 2 — parented to seg1, curves slightly up
            var seg2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            seg2.name = "TailSeg2";
            seg2.transform.SetParent(seg1.transform, false);
            seg2.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            seg2.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            seg2.transform.localScale    = new Vector3(0.82f, 0.80f, 0.82f);
            ApplyColor(seg2, color);
            Object.DestroyImmediate(seg2.GetComponent<Collider>());

            // Segment 3 — tip, thinnest
            var seg3 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            seg3.name = "TailSeg3";
            seg3.transform.SetParent(seg2.transform, false);
            seg3.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            seg3.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            seg3.transform.localScale    = new Vector3(0.78f, 0.78f, 0.78f);
            ApplyColor(seg3, color);
            Object.DestroyImmediate(seg3.GetComponent<Collider>());
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            Shader sh = (r.sharedMaterial != null && r.sharedMaterial.shader != null)
                ? r.sharedMaterial.shader
                : Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                    ?? Shader.Find("Standard");
            var mat = new Material(sh) { color = color };
            r.sharedMaterial = mat;
        }

        /// <summary>
        /// Saves every unsaved material on <paramref name="go"/> as a .mat asset in
        /// <paramref name="folder"/> BEFORE the prefab is saved, so the prefab ends up
        /// referencing real on-disk assets rather than in-memory objects (which cause
        /// the pink error-shader after a domain reload).
        /// </summary>
        private static void SaveMaterialsToDisk(GameObject go, string folder)
        {
            EnsureFolder(folder);
            int idx = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats    = r.sharedMaterials;
                bool dirty  = false;
                for (int i  = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || AssetDatabase.Contains(mats[i])) continue;
                    string safeName = go.name + "_" + r.gameObject.name +
                                      (idx > 0 ? "_" + idx : "");
                    string matPath  = AssetDatabase.GenerateUniqueAssetPath(
                                          folder + "/" + safeName + ".mat");
                    AssetDatabase.CreateAsset(mats[i], matPath);
                    mats[i] = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    dirty   = true;
                    idx++;
                }
                if (dirty) r.sharedMaterials = mats;
            }
            AssetDatabase.SaveAssets();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Animation Clips
        //
        // The Animator lives on the visual prefab root (RatVisual).
        // All paths are therefore relative to RatVisual itself.
        // We use:
        //   • localPosition.x/y/z for position curves  (no angle-wrap issues)
        //   • localRotation.x/y/z/w for rotation curves (quaternion = correct SLERP)
        //   • localScale.x/y/z for scale curves
        // ═════════════════════════════════════════════════════════════════════

        // ── Idle (2 s loop) ───────────────────────────────────────────────────

        private static AnimationClip BuildIdleClip()
        {
            var clip = NewClip("Rat_Idle", WrapMode.Loop);

            // Gentle breathing — body scale Y
            ScaleYCurve(clip, "Body",
                K(0.0f, 0.200f), K(1.0f, 0.213f), K(2.0f, 0.200f));

            // Head lifts slightly on inhale
            PosYCurve(clip, "Head",
                K(0.0f, 0.210f), K(1.0f, 0.222f), K(2.0f, 0.210f));

            // Tail sways left-right (Y-axis quaternion)
            var qN = Quaternion.identity;
            var qR = Quaternion.Euler(0f,  14f, 0f);
            var qL = Quaternion.Euler(0f, -14f, 0f);
            RotCurve(clip, "TailRoot",
                (0.0f, qN), (0.5f, qR), (1.0f, qN), (1.5f, qL), (2.0f, qN));

            return clip;
        }

        // ── Walk (0.8 s loop — diagonal quadruped gait) ───────────────────────
        //
        // Leg positions in walk cycle (amplitude ± 0.04 m in Z, ± 0.03 m lift in Y):
        //
        //   Group A (FrontLeft + BackRight): starts forward, stance → swing
        //   Group B (FrontRight + BackLeft): starts back,    swing  → stance
        //
        // Default hip positions:
        //   FrontLeft  (-0.09, 0.12,  0.14)   BackRight ( 0.09, 0.12, -0.12)
        //   FrontRight ( 0.09, 0.12,  0.14)   BackLeft  (-0.09, 0.12, -0.12)

        private static AnimationClip BuildWalkClip()
        {
            var clip = NewClip("Rat_Walk", WrapMode.Loop);

            const float Y0  = 0.12f;   // default hip Y
            const float YUp = 0.15f;   // lifted hip Y during swing
            const float A   = 0.04f;   // Z-amplitude
            const float DUR = 0.8f;

            // ── Group A: FrontLeftHip ──────────────────────────────────────
            float flZ = 0.14f;
            PosZCurve(clip, "FrontLeftHip",
                K(0.00f, flZ + A), K(0.24f, flZ),     K(0.48f, flZ - A),
                K(0.64f, flZ),     K(DUR,   flZ + A));
            PosYCurve(clip, "FrontLeftHip",
                K(0.00f, Y0), K(0.48f, Y0), K(0.56f, YUp), K(0.64f, Y0), K(DUR, Y0));

            // ── Group A: BackRightHip ──────────────────────────────────────
            float brZ = -0.12f;
            PosZCurve(clip, "BackRightHip",
                K(0.00f, brZ + A), K(0.24f, brZ),     K(0.48f, brZ - A),
                K(0.64f, brZ),     K(DUR,   brZ + A));
            PosYCurve(clip, "BackRightHip",
                K(0.00f, Y0), K(0.48f, Y0), K(0.56f, YUp), K(0.64f, Y0), K(DUR, Y0));

            // ── Group B: FrontRightHip ─────────────────────────────────────
            float frZ = 0.14f;
            PosZCurve(clip, "FrontRightHip",
                K(0.00f, frZ - A), K(0.16f, frZ),     K(0.32f, frZ + A),
                K(0.56f, frZ),     K(DUR,   frZ - A));
            PosYCurve(clip, "FrontRightHip",
                K(0.00f, Y0), K(0.08f, YUp), K(0.16f, Y0), K(0.32f, Y0), K(DUR, Y0));

            // ── Group B: BackLeftHip ───────────────────────────────────────
            float blZ = -0.12f;
            PosZCurve(clip, "BackLeftHip",
                K(0.00f, blZ - A), K(0.16f, blZ),     K(0.32f, blZ + A),
                K(0.56f, blZ),     K(DUR,   blZ - A));
            PosYCurve(clip, "BackLeftHip",
                K(0.00f, Y0), K(0.08f, YUp), K(0.16f, Y0), K(0.32f, Y0), K(DUR, Y0));

            // ── Body bob (Y) ───────────────────────────────────────────────
            PosYCurve(clip, "Body",
                K(0.0f, 0.130f), K(0.2f, 0.137f), K(0.4f, 0.130f),
                K(0.6f, 0.137f), K(DUR, 0.130f));

            // ── Tail wag (quaternion Y-rotation) ──────────────────────────
            var qN = Quaternion.identity;
            var qR = Quaternion.Euler(0f,  18f, 0f);
            var qL = Quaternion.Euler(0f, -18f, 0f);
            RotCurve(clip, "TailRoot",
                (0.0f, qN), (0.2f, qR), (0.4f, qN), (0.6f, qL), (DUR, qN));

            return clip;
        }

        // ── Attack (1.5 s once) ───────────────────────────────────────────────

        private static AnimationClip BuildAttackClip()
        {
            var clip = NewClip("Rat_Attack", WrapMode.Once);

            const float DUR = 1.5f;

            // Head lunges forward-down (snap) then recoils
            PosZCurve(clip, "Head",
                K(0.0f,        0.22f), K(DUR * 0.35f, 0.33f),
                K(DUR * 0.55f, 0.30f), K(DUR,         0.22f));
            PosYCurve(clip, "Head",
                K(0.0f,        0.21f), K(DUR * 0.35f, 0.11f),
                K(DUR * 0.65f, 0.21f), K(DUR,         0.21f));

            // Body surges forward
            PosZCurve(clip, "Body",
                K(0.0f, 0f), K(DUR * 0.30f, 0.07f), K(DUR, 0f));

            // Front legs thrust forward together for the lunge
            foreach (string hip in new[] { "FrontLeftHip", "FrontRightHip" })
            {
                PosZCurve(clip, hip,
                    K(0.0f,        0.14f), K(DUR * 0.20f, 0.23f),
                    K(DUR * 0.50f, 0.09f), K(DUR,         0.14f));
            }

            return clip;
        }

        // ── Hit (0.3 s once) ─────────────────────────────────────────────────

        private static AnimationClip BuildHitClip()
        {
            var clip = NewClip("Rat_Hit", WrapMode.Once);

            // Body recoils backward
            PosZCurve(clip, "Body",
                K(0f, 0f), K(0.10f, -0.055f), K(0.30f, 0f));

            // Head follows the recoil
            PosZCurve(clip, "Head",
                K(0f, 0.22f), K(0.10f, 0.14f), K(0.30f, 0.22f));

            return clip;
        }

        // ── Die (1.2 s once) ─────────────────────────────────────────────────

        private static AnimationClip BuildDieClip()
        {
            var clip = NewClip("Rat_Die", WrapMode.Once);

            // Whole visual tilts onto its side (Z rotation) and sinks slightly
            // Using quaternion curves ensures no euler-wrap artefacts.
            RotCurve(clip, "",   // "" = the visual root itself
                (0.0f,  Quaternion.identity),
                (0.80f, Quaternion.Euler(0f, 0f,  75f)),
                (1.20f, Quaternion.Euler(0f, 0f,  90f)));

            PosYCurve(clip, "",
                K(0f, 0f), K(1.2f, -0.06f));

            return clip;
        }

        // ═════════════════════════════════════════════════════════════════════
        // AnimatorController
        // ═════════════════════════════════════════════════════════════════════

        private static AnimatorController BuildAnimatorController(
            AnimationClip idle, AnimationClip walk,
            AnimationClip attack, AnimationClip hit, AnimationClip die)
        {
            // CreateAnimatorControllerAtPath saves the asset immediately
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // Parameters (must match MobBrain hash names exactly)
            ctrl.AddParameter("Speed",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsHit",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;

            // ── States ─────────────────────────────────────────────────────
            var sIdle   = sm.AddState("Idle");   sIdle.motion   = idle;
            var sWalk   = sm.AddState("Walk");   sWalk.motion   = walk;
            var sAttack = sm.AddState("Attack"); sAttack.motion = attack;
            var sHit    = sm.AddState("Hit");    sHit.motion    = hit;
            var sDead   = sm.AddState("Dead");   sDead.motion   = die;

            sm.defaultState = sIdle;

            // ── Transitions ────────────────────────────────────────────────

            // Idle → Walk (Speed > 0.1)
            Transition(sIdle, sWalk,   hasExit: false, dur: 0.15f)
                .AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            // Walk → Idle (Speed < 0.1)
            Transition(sWalk, sIdle,   hasExit: false, dur: 0.20f)
                .AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Walk → Attack
            Transition(sWalk, sAttack, hasExit: false, dur: 0.08f)
                .AddCondition(AnimatorConditionMode.If, 0, "Attack");

            // Idle → Attack
            Transition(sIdle, sAttack, hasExit: false, dur: 0.08f)
                .AddCondition(AnimatorConditionMode.If, 0, "Attack");

            // Attack → Idle (on clip finish)
            Transition(sAttack, sIdle, hasExit: true, exitTime: 0.92f, dur: 0.12f);

            // Hit exits back to Idle
            Transition(sHit, sIdle,    hasExit: true, exitTime: 0.95f, dur: 0.10f);

            // AnyState → Hit
            var anyHit = sm.AddAnyStateTransition(sHit);
            anyHit.hasExitTime      = false;
            anyHit.duration         = 0.05f;
            anyHit.canTransitionToSelf = false;
            anyHit.AddCondition(AnimatorConditionMode.If, 0, "IsHit");

            // AnyState → Dead (highest priority, can interrupt everything)
            var anyDead = sm.AddAnyStateTransition(sDead);
            anyDead.hasExitTime         = false;
            anyDead.duration            = 0.10f;
            anyDead.canTransitionToSelf = false;
            anyDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static AnimatorStateTransition Transition(
            AnimatorState from, AnimatorState to,
            bool hasExit, float dur = 0.15f, float exitTime = 1f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExit;
            t.exitTime    = exitTime;
            t.duration    = dur;
            return t;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Curve helpers
        // ═════════════════════════════════════════════════════════════════════

        private static AnimationClip NewClip(string name, WrapMode wrap)
        {
            var clip = new AnimationClip { name = name, wrapMode = wrap };
            clip.frameRate = 30f;
            return clip;
        }

        private static Keyframe K(float t, float v) =>
            new Keyframe(t, v, 0f, 0f); // zero tangents; SmoothTangents called below

        // Position Y
        private static void PosYCurve(AnimationClip clip, string path, params Keyframe[] keys) =>
            SetPosCurve(clip, path, "localPosition.y", keys);

        // Position Z
        private static void PosZCurve(AnimationClip clip, string path, params Keyframe[] keys) =>
            SetPosCurve(clip, path, "localPosition.z", keys);

        private static void SetPosCurve(AnimationClip clip, string path, string prop, Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        // Scale Y
        private static void ScaleYCurve(AnimationClip clip, string path, params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
            clip.SetCurve(path, typeof(Transform), "localScale.y", curve);
        }

        // Quaternion rotation — avoids ALL euler-angle wrap artefacts
        private static void RotCurve(AnimationClip clip, string path,
            params (float time, Quaternion rot)[] keys)
        {
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            var cw = new AnimationCurve();

            foreach (var (t, q) in keys)
            {
                cx.AddKey(new Keyframe(t, q.x, 0f, 0f));
                cy.AddKey(new Keyframe(t, q.y, 0f, 0f));
                cz.AddKey(new Keyframe(t, q.z, 0f, 0f));
                cw.AddKey(new Keyframe(t, q.w, 0f, 0f));
            }

            for (int i = 0; i < cx.length; i++) { cx.SmoothTangents(i, 0f); cy.SmoothTangents(i, 0f); cz.SmoothTangents(i, 0f); cw.SmoothTangents(i, 0f); }

            clip.SetCurve(path, typeof(Transform), "localRotation.x", cx);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", cy);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", cz);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", cw);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Asset utilities
        // ═════════════════════════════════════════════════════════════════════

        private static void SaveClip(AnimationClip clip, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        private static void StampDefinition(GameObject prefab)
        {
            var def = AssetDatabase.LoadAssetAtPath<Kaligo.Mobs.MobDefinition>(RatAssetPath);
            if (def == null)
            {
                Debug.LogWarning("[RatModelCreator] Rat.asset not found at " + RatAssetPath +
                                 " — set prefabOverride manually in the Inspector.");
                return;
            }
            def.prefabOverride = prefab;
            EditorUtility.SetDirty(def);
        }

        private const string MatFolder = "Assets/Characters/Mobs/Models/Materials";

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Characters/Mobs/Models");
            EnsureFolder(MatFolder);
            EnsureFolder("Assets/Characters/Mobs/Animations");
            EnsureFolder("Assets/Characters/Mobs/Animations/Rat");
            EnsureFolder("Assets/Editor/Characters");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts   = path.Split('/');
            string cur  = parts[0];
            for (int i  = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
