#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Kaligo.Editor.Characters
{
    /// <summary>
    /// Procedurally builds visual prefabs, animation clips and AnimatorControllers for
    /// Deer, Chicken, Sheep, Wolf, Bear and Goblin.
    ///
    /// Run via: Kaligo → Build Models → Create [Name] Model
    ///      or: Kaligo → Build Models → Create All Remaining Mob Models
    /// </summary>
    public static class MobModelCreator
    {
        private const string ModelDir = "Assets/Characters/Mobs/Models";
        private const string AnimBase = "Assets/Characters/Mobs/Animations";

        // ═════════════════════════════════════════════════════════════════════
        // Menu entry points
        // ═════════════════════════════════════════════════════════════════════

        [MenuItem("Kaligo/Build Models/Create All Remaining Mob Models")]
        public static void CreateAll()
        {
            CreateDeerModel();
            CreateChickenModel();
            CreateSheepModel();
            CreateWolfModel();
            CreateBearModel();
            CreateGoblinModel();
            EditorUtility.DisplayDialog("Done", "All 6 mob models created!", "Nice!");
        }

        [MenuItem("Kaligo/Build Models/Create Deer Model")]
        public static void CreateDeerModel()   => BuildMob("Deer",   BuildDeerVisual,   BuildDeerClips,   false,
            "Assets/Characters/Mobs/Data/Passive/Deer.asset");

        [MenuItem("Kaligo/Build Models/Create Chicken Model")]
        public static void CreateChickenModel() => BuildMob("Chicken", BuildChickenVisual, BuildChickenClips, false,
            "Assets/Characters/Mobs/Data/Passive/Chicken.asset");

        [MenuItem("Kaligo/Build Models/Create Sheep Model")]
        public static void CreateSheepModel()  => BuildMob("Sheep",  BuildSheepVisual,  BuildSheepClips,  false,
            "Assets/Characters/Mobs/Data/Passive/Sheep.asset");

        [MenuItem("Kaligo/Build Models/Create Wolf Model")]
        public static void CreateWolfModel()   => BuildMob("Wolf",   BuildWolfVisual,   BuildWolfClips,   true,
            "Assets/Characters/Mobs/Data/Aggressive/Wolf.asset");

        [MenuItem("Kaligo/Build Models/Create Bear Model")]
        public static void CreateBearModel()   => BuildMob("Bear",   BuildBearVisual,   BuildBearClips,   true,
            "Assets/Characters/Mobs/Data/Aggressive/Bear.asset");

        [MenuItem("Kaligo/Build Models/Create Goblin Model")]
        public static void CreateGoblinModel() => BuildMob("Goblin", BuildGoblinVisual, BuildGoblinClips, true,
            "Assets/Characters/Mobs/Data/Aggressive/Goblin.asset");

        // ── Shared pipeline ───────────────────────────────────────────────────

        private delegate GameObject VisualBuilder();
        private delegate (AnimationClip idle, AnimationClip walk, AnimationClip attack,
                          AnimationClip hit, AnimationClip die) ClipBuilder(string animDir);

        private static void BuildMob(string name, VisualBuilder vb, ClipBuilder cb,
                                     bool aggressive, string defPath)
        {
            string animDir = AnimBase + "/" + name;
            EnsureFolders(ModelDir, AnimBase, animDir);

            var visual = vb();
            var (idle, walk, attack, hit, die) = cb(animDir);

            SaveClip(idle,   animDir + "/" + name + "_Idle.anim");
            SaveClip(walk,   animDir + "/" + name + "_Walk.anim");
            if (attack != null) SaveClip(attack, animDir + "/" + name + "_Attack.anim");
            SaveClip(hit,    animDir + "/" + name + "_Hit.anim");
            SaveClip(die,    animDir + "/" + name + "_Die.anim");

            string ctrlPath = animDir + "/" + name + "_Controller.controller";
            var    ctrl     = aggressive
                ? BuildAggressiveController(ctrlPath, idle, walk, attack, hit, die)
                : BuildPassiveController   (ctrlPath, idle, walk,         hit, die);

            var anim = visual.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            string prefabPath = ModelDir + "/" + name + "Visual.prefab";
            string matFolder  = ModelDir + "/Materials";
            // Save materials to disk FIRST — prefab then references real .mat assets,
            // not in-memory objects that go pink after a domain reload.
            SaveMaterialsToDisk(visual, matFolder);
            var prefab = PrefabUtility.SaveAsPrefabAsset(visual, prefabPath, out bool ok);
            Object.DestroyImmediate(visual);

            if (ok)
            {
                StampDef(defPath, prefab);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[MobModelCreator] " + name + " model created at " + prefabPath);
            }
            else Debug.LogError("[MobModelCreator] Failed to save prefab for " + name);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DEER  (passive, h=1.5, r=0.4)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildDeerVisual()
        {
            var root = new GameObject("DeerVisual");

            var cFur    = new Color(0.72f, 0.54f, 0.32f); // warm tan
            var cDark   = new Color(0.52f, 0.36f, 0.18f); // darker brown for face/legs
            var cAntler = new Color(0.50f, 0.32f, 0.14f); // antler brown
            var cEye    = new Color(0.06f, 0.04f, 0.03f);
            var cNose   = new Color(0.30f, 0.18f, 0.14f);
            var cTail   = Color.white;

            // Body
            MP(root.transform, "Body", PrimitiveType.Sphere,
               new Vector3(0f, 0.82f, 0f), new Vector3(0.48f, 0.28f, 0.62f), cFur);

            // Neck
            MP(root.transform, "Neck", PrimitiveType.Capsule,
               new Vector3(0f, 1.06f, 0.28f), new Vector3(0.14f, 0.22f, 0.14f), cFur,
               Quaternion.Euler(28f, 0f, 0f));

            // Head
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 1.30f, 0.48f), new Vector3(0.24f, 0.22f, 0.26f), cFur);

            // Snout / nose
            MP(root.transform, "Snout", PrimitiveType.Sphere,
               new Vector3(0f, 1.22f, 0.64f), new Vector3(0.12f, 0.09f, 0.13f), cDark);
            MP(root.transform, "Nose", PrimitiveType.Sphere,
               new Vector3(0f, 1.20f, 0.73f), new Vector3(0.06f, 0.05f, 0.06f), cNose);

            // Ears
            MP(root.transform, "LeftEar",  PrimitiveType.Sphere,
               new Vector3(-0.14f, 1.42f, 0.42f), new Vector3(0.07f, 0.14f, 0.05f), cFur);
            MP(root.transform, "RightEar", PrimitiveType.Sphere,
               new Vector3( 0.14f, 1.42f, 0.42f), new Vector3(0.07f, 0.14f, 0.05f), cFur);

            // Eyes
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.10f, 1.32f, 0.59f), Vector3.one * 0.048f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.10f, 1.32f, 0.59f), Vector3.one * 0.048f, cEye);

            // Antlers
            BuildAntler(root.transform, -1f, cAntler); // left
            BuildAntler(root.transform,  1f, cAntler); // right

            // Legs (hip pivot + capsule)
            ML(root.transform, "FrontLeft",  new Vector3(-0.20f, 0.70f,  0.28f), cDark, -0.32f, new Vector3(0.09f, 0.32f, 0.09f));
            ML(root.transform, "FrontRight", new Vector3( 0.20f, 0.70f,  0.28f), cDark, -0.32f, new Vector3(0.09f, 0.32f, 0.09f));
            ML(root.transform, "BackLeft",   new Vector3(-0.17f, 0.70f, -0.26f), cDark, -0.32f, new Vector3(0.09f, 0.32f, 0.09f));
            ML(root.transform, "BackRight",  new Vector3( 0.17f, 0.70f, -0.26f), cDark, -0.32f, new Vector3(0.09f, 0.32f, 0.09f));

            // Tail (white bob)
            MP(root.transform, "Tail", PrimitiveType.Sphere,
               new Vector3(0f, 0.80f, -0.38f), Vector3.one * 0.10f, cTail);

            return root;
        }

        private static void BuildAntler(Transform root, float side, Color c)
        {
            string prefix = side < 0 ? "Left" : "Right";
            var go = MP(root, prefix + "AntlerBase", PrimitiveType.Capsule,
                new Vector3(side * 0.09f, 1.52f, 0.38f),
                new Vector3(0.034f, 0.12f, 0.034f), c,
                Quaternion.Euler(-8f, 0f, side * -16f));
            MP(go.transform, prefix + "AntlerA", PrimitiveType.Capsule,
                new Vector3(side * 0.05f, 0.11f,  0.02f),
                new Vector3(0.80f, 0.70f, 0.80f), c,
                Quaternion.Euler(10f, side * -28f, side * -15f));
            MP(go.transform, prefix + "AntlerB", PrimitiveType.Capsule,
                new Vector3(side * -0.02f, 0.07f, -0.01f),
                new Vector3(0.65f, 0.65f, 0.65f), c,
                Quaternion.Euler(-12f, side * 22f, side * 10f));
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildDeerClips(string dir)
        {
            // Idle — gentle breathing + ear twitch
            var idle = NewClip("Deer_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Body", K(0f, 0.28f), K(1.2f, 0.295f), K(2.4f, 0.28f));
            PosYCurve  (idle, "Head", K(0f, 1.30f), K(1.2f, 1.315f), K(2.4f, 1.30f));
            PosZCurve  (idle, "LeftEar",  K(0f, 0.42f), K(0.6f, 0.44f), K(1.2f, 0.42f), K(2.4f, 0.42f));
            PosZCurve  (idle, "RightEar", K(0f, 0.42f), K(1.8f, 0.44f), K(2.4f, 0.42f));

            // Walk — elegant quadruped gait, 0.8 s cycle
            var walk = QuadWalk("Deer_Walk", dur: 0.8f, hipY: 0.70f, hipYUp: 0.74f, amp: 0.07f,
                flZ: 0.28f, brZ: -0.26f, frZ: 0.28f, blZ: -0.26f,
                bodyPath: "Body", bodyY: 0.82f, bodyYPeak: 0.833f, tailYaw: 0f);

            // Hit — flinch backward
            var hit = NewClip("Deer_Hit", WrapMode.Once);
            PosZCurve(hit, "Body", K(0f, 0f), K(0.10f, -0.06f), K(0.32f, 0f));
            PosZCurve(hit, "Head", K(0f, 0.48f), K(0.10f, 0.38f), K(0.32f, 0.48f));

            // Die — tips onto side
            var die = NewClip("Deer_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity), (0.9f, Quaternion.Euler(0f, 0f, 80f)),
                              (1.3f, Quaternion.Euler(0f, 0f, 90f)));
            PosYCurve(die, "", K(0f, 0f), K(1.3f, -0.06f));

            return (idle, walk, null, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // CHICKEN  (passive, h=0.55, r=0.2)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildChickenVisual()
        {
            var root = new GameObject("ChickenVisual");

            var cFeather = new Color(0.96f, 0.92f, 0.82f); // cream white
            var cBeak    = new Color(0.90f, 0.72f, 0.10f); // yellow
            var cRed     = new Color(0.85f, 0.14f, 0.10f); // comb / wattle
            var cLeg     = new Color(0.88f, 0.68f, 0.15f); // yellow-orange legs
            var cEye     = new Color(0.06f, 0.05f, 0.04f);

            // Body
            MP(root.transform, "Body", PrimitiveType.Sphere,
               new Vector3(0f, 0.28f, 0f), new Vector3(0.30f, 0.27f, 0.32f), cFeather);

            // Head
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 0.43f, 0.15f), new Vector3(0.14f, 0.14f, 0.14f), cFeather);

            // Beak
            MP(root.transform, "Beak", PrimitiveType.Sphere,
               new Vector3(0f, 0.41f, 0.265f), new Vector3(0.06f, 0.04f, 0.08f), cBeak);

            // Wattle (dangly red bit)
            MP(root.transform, "Wattle", PrimitiveType.Sphere,
               new Vector3(0f, 0.37f, 0.255f), new Vector3(0.04f, 0.05f, 0.04f), cRed);

            // Comb (3 tiny red bumps on head)
            MP(root.transform, "Comb1", PrimitiveType.Sphere,
               new Vector3(0f,   0.515f, 0.12f), Vector3.one * 0.038f, cRed);
            MP(root.transform, "Comb2", PrimitiveType.Sphere,
               new Vector3(0.03f, 0.508f, 0.10f), Vector3.one * 0.030f, cRed);
            MP(root.transform, "Comb3", PrimitiveType.Sphere,
               new Vector3(-0.03f,0.508f, 0.10f), Vector3.one * 0.030f, cRed);

            // Eyes
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.07f, 0.445f, 0.22f), Vector3.one * 0.032f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.07f, 0.445f, 0.22f), Vector3.one * 0.032f, cEye);

            // Wings (flat ovals on sides)
            MP(root.transform, "LeftWing",  PrimitiveType.Sphere,
               new Vector3(-0.185f, 0.28f, 0.02f), new Vector3(0.05f, 0.18f, 0.22f), cFeather,
               Quaternion.Euler(0f, 0f, 12f));
            MP(root.transform, "RightWing", PrimitiveType.Sphere,
               new Vector3( 0.185f, 0.28f, 0.02f), new Vector3(0.05f, 0.18f, 0.22f), cFeather,
               Quaternion.Euler(0f, 0f, -12f));

            // Tail feathers (upward tuft at rear)
            MP(root.transform, "TailFeathers", PrimitiveType.Sphere,
               new Vector3(0f, 0.34f, -0.18f), new Vector3(0.12f, 0.12f, 0.10f), cFeather,
               Quaternion.Euler(-30f, 0f, 0f));

            // Legs — biped: LeftLegHip / RightLegHip
            ML(root.transform, "LeftLeg",   new Vector3(-0.07f, 0.13f, 0.04f), cLeg, -0.065f, new Vector3(0.052f, 0.065f, 0.052f));
            ML(root.transform, "RightLeg",  new Vector3( 0.07f, 0.13f, 0.04f), cLeg, -0.065f, new Vector3(0.052f, 0.065f, 0.052f));

            return root;
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildChickenClips(string dir)
        {
            // Idle — subtle body bob + wing micro-flap
            var idle = NewClip("Chicken_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Body",
                K(0f, 0.27f), K(0.8f, 0.282f), K(1.6f, 0.27f));
            RotCurve(idle, "LeftWing",
                (0f, Quaternion.Euler(0f,0f,12f)), (0.8f, Quaternion.Euler(0f,0f,22f)),
                (1.6f, Quaternion.Euler(0f,0f,12f)));
            RotCurve(idle, "RightWing",
                (0f, Quaternion.Euler(0f,0f,-12f)), (0.8f, Quaternion.Euler(0f,0f,-22f)),
                (1.6f, Quaternion.Euler(0f,0f,-12f)));

            // Walk — bipedal chicken strut with dramatic head bob, 0.6 s cycle
            var walk = NewClip("Chicken_Walk", WrapMode.Loop);
            const float cDur = 0.6f; const float cAmp = 0.04f; const float cLift = 0.04f;
            const float cHY = 0.13f; const float cHYUp = 0.17f;
            // Left leg: stance 0-0.3, swing 0.3-0.6
            PosZCurve(walk, "LeftLegHip",
                K(0f, 0.04f+cAmp), K(0.30f, 0.04f-cAmp), K(0.45f, 0.04f), K(cDur, 0.04f+cAmp));
            PosYCurve(walk, "LeftLegHip",
                K(0f, cHY), K(0.30f, cHY), K(0.375f, cHYUp), K(0.45f, cHY), K(cDur, cHY));
            // Right leg: opposite phase
            PosZCurve(walk, "RightLegHip",
                K(0f, 0.04f-cAmp), K(0.15f, 0.04f), K(0.30f, 0.04f+cAmp), K(cDur, 0.04f-cAmp));
            PosYCurve(walk, "RightLegHip",
                K(0f, cHY), K(0.075f, cHYUp), K(0.15f, cHY), K(0.30f, cHY), K(cDur, cHY));
            // Iconic chicken head bob (Z forward on each step)
            PosZCurve(walk, "Head",
                K(0f, 0.15f), K(0.15f, 0.21f), K(0.30f, 0.15f), K(0.45f, 0.21f), K(cDur, 0.15f));
            PosYCurve(walk, "Body",
                K(0f, 0.28f), K(0.15f, 0.286f), K(0.30f, 0.28f), K(0.45f, 0.286f), K(cDur, 0.28f));

            // Hit — flinch
            var hit = NewClip("Chicken_Hit", WrapMode.Once);
            PosZCurve(hit, "Body", K(0f, 0f), K(0.08f, -0.05f), K(0.28f, 0f));
            PosZCurve(hit, "Head", K(0f, 0.15f), K(0.08f, 0.08f), K(0.28f, 0.15f));

            // Die — flop over
            var die = NewClip("Chicken_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity),
                              (0.7f, Quaternion.Euler(0f, 0f, 75f)),
                              (1.0f, Quaternion.Euler(0f, 0f, 90f)));
            PosYCurve(die, "", K(0f, 0f), K(1.0f, -0.05f));

            return (idle, walk, null, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // SHEEP  (passive, h=1.1, r=0.35)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildSheepVisual()
        {
            var root = new GameObject("SheepVisual");

            var cWool  = new Color(0.94f, 0.93f, 0.88f); // off-white wool
            var cFace  = new Color(0.62f, 0.52f, 0.40f); // grey-tan face/legs
            var cNose  = new Color(0.60f, 0.34f, 0.34f); // pinkish nose
            var cEye   = new Color(0.08f, 0.06f, 0.05f);

            // Body — big fluffy sphere
            MP(root.transform, "Body", PrimitiveType.Sphere,
               new Vector3(0f, 0.55f, 0f), new Vector3(0.56f, 0.48f, 0.62f), cWool);
            // Extra wool bumps for texture
            MP(root.transform, "WoolTop",  PrimitiveType.Sphere,
               new Vector3(0f, 0.94f, -0.05f), new Vector3(0.28f, 0.14f, 0.24f), cWool);
            MP(root.transform, "WoolMid",  PrimitiveType.Sphere,
               new Vector3(-0.25f, 0.68f, 0.12f), new Vector3(0.16f, 0.14f, 0.20f), cWool);
            MP(root.transform, "WoolMid2", PrimitiveType.Sphere,
               new Vector3( 0.25f, 0.68f, 0.12f), new Vector3(0.16f, 0.14f, 0.20f), cWool);

            // Head
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 0.93f, 0.36f), new Vector3(0.26f, 0.24f, 0.28f), cFace);

            // Ears (drooping)
            MP(root.transform, "LeftEar",  PrimitiveType.Sphere,
               new Vector3(-0.16f, 0.91f, 0.30f), new Vector3(0.06f, 0.14f, 0.06f), cFace,
               Quaternion.Euler(0f, 0f, 28f));
            MP(root.transform, "RightEar", PrimitiveType.Sphere,
               new Vector3( 0.16f, 0.91f, 0.30f), new Vector3(0.06f, 0.14f, 0.06f), cFace,
               Quaternion.Euler(0f, 0f, -28f));

            // Eyes
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.10f, 0.95f, 0.52f), Vector3.one * 0.044f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.10f, 0.95f, 0.52f), Vector3.one * 0.044f, cEye);
            MP(root.transform, "Nose", PrimitiveType.Sphere,
               new Vector3(0f, 0.86f, 0.54f), new Vector3(0.10f, 0.07f, 0.08f), cNose);

            // Short stubby legs
            ML(root.transform, "FrontLeft",  new Vector3(-0.16f, 0.43f,  0.24f), cFace, -0.20f, new Vector3(0.10f, 0.20f, 0.10f));
            ML(root.transform, "FrontRight", new Vector3( 0.16f, 0.43f,  0.24f), cFace, -0.20f, new Vector3(0.10f, 0.20f, 0.10f));
            ML(root.transform, "BackLeft",   new Vector3(-0.14f, 0.43f, -0.22f), cFace, -0.20f, new Vector3(0.10f, 0.20f, 0.10f));
            ML(root.transform, "BackRight",  new Vector3( 0.14f, 0.43f, -0.22f), cFace, -0.20f, new Vector3(0.10f, 0.20f, 0.10f));

            // Tiny tail bob
            MP(root.transform, "Tail", PrimitiveType.Sphere,
               new Vector3(0f, 0.58f, -0.36f), Vector3.one * 0.08f, cWool);

            return root;
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildSheepClips(string dir)
        {
            var idle = NewClip("Sheep_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Body", K(0f, 0.48f), K(1.4f, 0.498f), K(2.8f, 0.48f));
            PosYCurve  (idle, "Head", K(0f, 0.93f), K(1.4f, 0.944f), K(2.8f, 0.93f));

            var walk = QuadWalk("Sheep_Walk", dur: 1.0f, hipY: 0.43f, hipYUp: 0.46f, amp: 0.04f,
                flZ: 0.24f, brZ: -0.22f, frZ: 0.24f, blZ: -0.22f,
                bodyPath: "Body", bodyY: 0.55f, bodyYPeak: 0.558f, tailYaw: 0f);

            var hit = NewClip("Sheep_Hit", WrapMode.Once);
            PosZCurve(hit, "Body", K(0f, 0f), K(0.10f, -0.06f), K(0.32f, 0f));
            PosZCurve(hit, "Head", K(0f, 0.36f), K(0.10f, 0.26f), K(0.32f, 0.36f));

            var die = NewClip("Sheep_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity), (0.9f, Quaternion.Euler(0f, 0f, 80f)),
                              (1.3f, Quaternion.Euler(0f, 0f, 90f)));
            PosYCurve(die, "", K(0f, 0f), K(1.3f, -0.05f));

            return (idle, walk, null, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // WOLF  (aggressive, h=1.2, r=0.35)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildWolfVisual()
        {
            var root = new GameObject("WolfVisual");

            var cGrey   = new Color(0.48f, 0.48f, 0.52f);
            var cDark   = new Color(0.30f, 0.30f, 0.34f);
            var cBelly  = new Color(0.70f, 0.68f, 0.65f);
            var cEye    = new Color(0.82f, 0.68f, 0.12f); // amber
            var cNose   = new Color(0.14f, 0.12f, 0.12f);

            // Body — lean and elongated
            MP(root.transform, "Body", PrimitiveType.Sphere,
               new Vector3(0f, 0.65f, 0.04f), new Vector3(0.42f, 0.30f, 0.70f), cGrey);
            // Belly lighter patch
            MP(root.transform, "Belly", PrimitiveType.Sphere,
               new Vector3(0f, 0.60f, 0.10f), new Vector3(0.26f, 0.20f, 0.48f), cBelly);

            // Neck
            MP(root.transform, "Neck", PrimitiveType.Capsule,
               new Vector3(0f, 0.88f, 0.36f), new Vector3(0.16f, 0.18f, 0.16f), cGrey,
               Quaternion.Euler(26f, 0f, 0f));

            // Head — wider at back, tapers to snout
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 0.98f, 0.46f), new Vector3(0.28f, 0.24f, 0.34f), cGrey);

            // Snout — elongated
            MP(root.transform, "Snout", PrimitiveType.Sphere,
               new Vector3(0f, 0.91f, 0.66f), new Vector3(0.16f, 0.13f, 0.26f), cDark);
            MP(root.transform, "Nose",  PrimitiveType.Sphere,
               new Vector3(0f, 0.90f, 0.78f), new Vector3(0.07f, 0.06f, 0.07f), cNose);

            // Pointed ears
            MP(root.transform, "LeftEar",  PrimitiveType.Sphere,
               new Vector3(-0.12f, 1.14f, 0.42f), new Vector3(0.07f, 0.13f, 0.05f), cGrey);
            MP(root.transform, "RightEar", PrimitiveType.Sphere,
               new Vector3( 0.12f, 1.14f, 0.42f), new Vector3(0.07f, 0.13f, 0.05f), cGrey);

            // Eyes (amber)
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.11f, 1.01f, 0.66f), Vector3.one * 0.048f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.11f, 1.01f, 0.66f), Vector3.one * 0.048f, cEye);

            // Legs
            ML(root.transform, "FrontLeft",  new Vector3(-0.18f, 0.62f,  0.32f), cDark, -0.28f, new Vector3(0.08f, 0.28f, 0.08f));
            ML(root.transform, "FrontRight", new Vector3( 0.18f, 0.62f,  0.32f), cDark, -0.28f, new Vector3(0.08f, 0.28f, 0.08f));
            ML(root.transform, "BackLeft",   new Vector3(-0.16f, 0.62f, -0.28f), cDark, -0.28f, new Vector3(0.08f, 0.28f, 0.08f));
            ML(root.transform, "BackRight",  new Vector3( 0.16f, 0.62f, -0.28f), cDark, -0.28f, new Vector3(0.08f, 0.28f, 0.08f));

            // Bushy tail — 2 segments, curves up
            var tailRoot = new GameObject("TailRoot");
            tailRoot.transform.SetParent(root.transform, false);
            tailRoot.transform.localPosition = new Vector3(0f, 0.70f, -0.42f);
            MP(tailRoot.transform, "TailSeg1", PrimitiveType.Capsule,
               new Vector3(0f, 0f, -0.09f), new Vector3(0.08f, 0.11f, 0.08f), cGrey,
               Quaternion.Euler(90f, 0f, 0f));
            var seg1 = tailRoot.transform.Find("TailSeg1");
            MP(seg1, "TailSeg2", PrimitiveType.Capsule,
               new Vector3(0f, -0.22f, 0f), new Vector3(0.85f, 0.80f, 0.85f), cGrey,
               Quaternion.Euler(-40f, 0f, 0f));

            return root;
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildWolfClips(string dir)
        {
            var idle = NewClip("Wolf_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Body", K(0f, 0.30f), K(1.0f, 0.312f), K(2.0f, 0.30f));
            PosYCurve  (idle, "Head", K(0f, 0.98f), K(1.0f, 0.992f), K(2.0f, 0.98f));
            RotCurve(idle, "TailRoot",
                (0f, Quaternion.identity), (0.5f, Quaternion.Euler(0f, 20f, 0f)),
                (1.0f, Quaternion.identity), (1.5f, Quaternion.Euler(0f, -20f, 0f)),
                (2.0f, Quaternion.identity));

            var walk = QuadWalk("Wolf_Walk", dur: 0.70f, hipY: 0.62f, hipYUp: 0.66f, amp: 0.06f,
                flZ: 0.32f, brZ: -0.28f, frZ: 0.32f, blZ: -0.28f,
                bodyPath: "Body", bodyY: 0.65f, bodyYPeak: 0.661f, tailYaw: 22f);

            // Attack — snap forward
            var attack = NewClip("Wolf_Attack", WrapMode.Once);
            const float wDur = 2.0f;
            PosZCurve(attack, "Snout", K(0f, 0.66f), K(wDur*0.3f, 0.84f), K(wDur*0.6f, 0.75f), K(wDur, 0.66f));
            PosYCurve(attack, "Snout", K(0f, 0.91f), K(wDur*0.3f, 0.78f), K(wDur*0.6f, 0.88f), K(wDur, 0.91f));
            PosZCurve(attack, "Body",  K(0f, 0.04f), K(wDur*0.28f, 0.14f), K(wDur, 0.04f));
            foreach (var h in new[]{ "FrontLeftHip", "FrontRightHip" })
                PosZCurve(attack, h, K(0f, 0.32f), K(wDur*0.22f, 0.46f), K(wDur*0.5f, 0.22f), K(wDur, 0.32f));

            var hit = NewClip("Wolf_Hit", WrapMode.Once);
            PosZCurve(hit, "Body",  K(0f, 0.04f), K(0.10f, -0.06f), K(0.32f, 0.04f));
            PosZCurve(hit, "Snout", K(0f, 0.66f), K(0.10f, 0.54f),  K(0.32f, 0.66f));

            var die = NewClip("Wolf_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity), (0.9f, Quaternion.Euler(0f, 0f, 80f)),
                              (1.3f, Quaternion.Euler(0f, 0f, 90f)));
            PosYCurve(die, "", K(0f, 0f), K(1.3f, -0.06f));

            return (idle, walk, attack, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // BEAR  (aggressive, h=2.2, r=0.6)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildBearVisual()
        {
            var root = new GameObject("BearVisual");

            var cBrown  = new Color(0.30f, 0.18f, 0.08f);
            var cDark   = new Color(0.20f, 0.12f, 0.06f);
            var cMuzzle = new Color(0.62f, 0.46f, 0.28f);
            var cEye    = new Color(0.10f, 0.07f, 0.05f);
            var cNose   = new Color(0.12f, 0.08f, 0.06f);

            // Massive body
            MP(root.transform, "Body", PrimitiveType.Sphere,
               new Vector3(0f, 1.10f, 0.04f), new Vector3(0.90f, 0.78f, 1.00f), cBrown);

            // Large head
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 1.76f, 0.60f), new Vector3(0.60f, 0.55f, 0.62f), cBrown);

            // Muzzle / light patch
            MP(root.transform, "Muzzle", PrimitiveType.Sphere,
               new Vector3(0f, 1.62f, 0.93f), new Vector3(0.30f, 0.22f, 0.28f), cMuzzle);
            MP(root.transform, "Nose",   PrimitiveType.Sphere,
               new Vector3(0f, 1.62f, 1.05f), new Vector3(0.11f, 0.09f, 0.10f), cNose);

            // Round ears
            MP(root.transform, "LeftEar",  PrimitiveType.Sphere,
               new Vector3(-0.26f, 2.07f, 0.54f), new Vector3(0.16f, 0.15f, 0.14f), cBrown);
            MP(root.transform, "RightEar", PrimitiveType.Sphere,
               new Vector3( 0.26f, 2.07f, 0.54f), new Vector3(0.16f, 0.15f, 0.14f), cBrown);

            // Tiny eye highlights
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.20f, 1.82f, 0.87f), Vector3.one * 0.065f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.20f, 1.82f, 0.87f), Vector3.one * 0.065f, cEye);

            // Thick stocky legs
            ML(root.transform, "FrontLeft",  new Vector3(-0.34f, 0.80f,  0.42f), cDark, -0.38f, new Vector3(0.18f, 0.38f, 0.18f));
            ML(root.transform, "FrontRight", new Vector3( 0.34f, 0.80f,  0.42f), cDark, -0.38f, new Vector3(0.18f, 0.38f, 0.18f));
            ML(root.transform, "BackLeft",   new Vector3(-0.32f, 0.80f, -0.38f), cDark, -0.38f, new Vector3(0.18f, 0.38f, 0.18f));
            ML(root.transform, "BackRight",  new Vector3( 0.32f, 0.80f, -0.38f), cDark, -0.38f, new Vector3(0.18f, 0.38f, 0.18f));

            // Tiny tail
            MP(root.transform, "Tail", PrimitiveType.Sphere,
               new Vector3(0f, 1.10f, -0.56f), new Vector3(0.10f, 0.10f, 0.08f), cDark);

            return root;
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildBearClips(string dir)
        {
            var idle = NewClip("Bear_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Body", K(0f, 0.78f), K(1.5f, 0.802f), K(3.0f, 0.78f));
            PosYCurve  (idle, "Head", K(0f, 1.76f), K(1.5f, 1.778f), K(3.0f, 1.76f));
            // Slow imposing side-sway
            PosXCurve  (idle, "Body", K(0f, 0f), K(1.5f, 0.06f), K(3.0f, 0f));

            // Bear walk — slow lumber, 1.2 s, with body sway
            var walk = QuadWalk("Bear_Walk", dur: 1.20f, hipY: 0.80f, hipYUp: 0.85f, amp: 0.06f,
                flZ: 0.42f, brZ: -0.38f, frZ: 0.42f, blZ: -0.38f,
                bodyPath: "Body", bodyY: 1.10f, bodyYPeak: 1.114f, tailYaw: 0f);
            // Add sway to the walk
            PosXCurve(walk, "Body", K(0f, -0.05f), K(0.6f, 0.05f), K(1.2f, -0.05f));

            // Massive lunge attack
            var attack = NewClip("Bear_Attack", WrapMode.Once);
            const float bDur = 3.0f;
            PosZCurve(attack, "Body",  K(0f, 0.04f), K(bDur*0.28f, 0.20f), K(bDur, 0.04f));
            PosYCurve(attack, "Body",  K(0f, 1.10f), K(bDur*0.20f, 1.22f), K(bDur*0.35f, 1.00f), K(bDur, 1.10f));
            PosZCurve(attack, "Muzzle",K(0f, 0.93f), K(bDur*0.28f, 1.15f), K(bDur, 0.93f));
            foreach (var h in new[]{ "FrontLeftHip", "FrontRightHip" })
            {
                PosZCurve(attack, h, K(0f, 0.42f), K(bDur*0.20f, 0.64f), K(bDur*0.50f, 0.30f), K(bDur, 0.42f));
                PosYCurve(attack, h, K(0f, 0.80f), K(bDur*0.15f, 1.10f), K(bDur*0.35f, 0.60f), K(bDur, 0.80f));
            }

            var hit = NewClip("Bear_Hit", WrapMode.Once);
            PosZCurve(hit, "Body",   K(0f, 0.04f),  K(0.12f, -0.10f), K(0.40f, 0.04f));
            PosZCurve(hit, "Muzzle", K(0f, 0.93f),  K(0.12f,  0.76f), K(0.40f, 0.93f));

            var die = NewClip("Bear_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity),
                              (1.4f, Quaternion.Euler(0f, 0f, 82f)),
                              (2.0f, Quaternion.Euler(0f, 0f, 90f)));
            PosYCurve(die, "", K(0f, 0f), K(2.0f, -0.10f));

            return (idle, walk, attack, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // GOBLIN  (aggressive, h=1.2, r=0.28)
        // ═════════════════════════════════════════════════════════════════════

        private static GameObject BuildGoblinVisual()
        {
            var root = new GameObject("GoblinVisual");

            var cSkin  = new Color(0.28f, 0.54f, 0.20f); // goblin green
            var cDark  = new Color(0.18f, 0.36f, 0.12f); // darker green
            var cEye   = new Color(0.88f, 0.82f, 0.08f); // yellow eyes
            var cTooth = new Color(0.88f, 0.86f, 0.74f); // off-white teeth
            var cCloth = new Color(0.28f, 0.24f, 0.20f); // dark grey clothing/loincloth

            // Torso (hunched — shifted slightly back)
            MP(root.transform, "Torso", PrimitiveType.Sphere,
               new Vector3(0f, 0.55f, -0.04f), new Vector3(0.30f, 0.35f, 0.26f), cSkin);

            // Loin-cloth accent
            MP(root.transform, "Cloth", PrimitiveType.Sphere,
               new Vector3(0f, 0.40f, 0.02f), new Vector3(0.24f, 0.10f, 0.18f), cCloth);

            // Head — oversized
            MP(root.transform, "Head", PrimitiveType.Sphere,
               new Vector3(0f, 0.92f, 0.08f), new Vector3(0.32f, 0.30f, 0.32f), cSkin);

            // Big nose
            MP(root.transform, "Nose", PrimitiveType.Sphere,
               new Vector3(0f, 0.88f, 0.32f), new Vector3(0.09f, 0.07f, 0.18f), cDark);

            // Big ears
            MP(root.transform, "LeftEar",  PrimitiveType.Sphere,
               new Vector3(-0.25f, 0.92f, 0.04f), new Vector3(0.10f, 0.15f, 0.08f), cSkin,
               Quaternion.Euler(0f, 0f, 22f));
            MP(root.transform, "RightEar", PrimitiveType.Sphere,
               new Vector3( 0.25f, 0.92f, 0.04f), new Vector3(0.10f, 0.15f, 0.08f), cSkin,
               Quaternion.Euler(0f, 0f, -22f));

            // Eyes (yellow)
            MP(root.transform, "LeftEye",  PrimitiveType.Sphere,
               new Vector3(-0.11f, 0.96f, 0.27f), Vector3.one * 0.058f, cEye);
            MP(root.transform, "RightEye", PrimitiveType.Sphere,
               new Vector3( 0.11f, 0.96f, 0.27f), Vector3.one * 0.058f, cEye);

            // Tusk / tooth
            MP(root.transform, "LeftTusk",  PrimitiveType.Sphere,
               new Vector3(-0.06f, 0.82f, 0.28f), new Vector3(0.04f, 0.07f, 0.04f), cTooth);
            MP(root.transform, "RightTusk", PrimitiveType.Sphere,
               new Vector3( 0.06f, 0.82f, 0.28f), new Vector3(0.04f, 0.07f, 0.04f), cTooth);

            // Arms — LeftArmShoulder / RightArmShoulder (pivot) + capsule child
            ML(root.transform, "LeftArm",  new Vector3(-0.22f, 0.65f, 0f), cDark, -0.14f, new Vector3(0.07f, 0.14f, 0.07f));
            ML(root.transform, "RightArm", new Vector3( 0.22f, 0.65f, 0f), cDark, -0.14f, new Vector3(0.07f, 0.14f, 0.07f));

            // Legs
            ML(root.transform, "LeftLeg",  new Vector3(-0.10f, 0.30f, 0f), cDark, -0.15f, new Vector3(0.09f, 0.15f, 0.09f));
            ML(root.transform, "RightLeg", new Vector3( 0.10f, 0.30f, 0f), cDark, -0.15f, new Vector3(0.09f, 0.15f, 0.09f));

            return root;
        }

        private static (AnimationClip, AnimationClip, AnimationClip, AnimationClip, AnimationClip)
            BuildGoblinClips(string dir)
        {
            var idle = NewClip("Goblin_Idle", WrapMode.Loop);
            ScaleYCurve(idle, "Torso", K(0f, 0.35f), K(1.0f, 0.362f), K(2.0f, 0.35f));
            // Intimidating side-to-side sway
            RotCurve(idle, "Torso",
                (0f, Quaternion.identity), (0.8f, Quaternion.Euler(0f, 0f,  8f)),
                (1.6f, Quaternion.Euler(0f, 0f, -8f)), (2.4f, Quaternion.identity));
            PosYCurve(idle, "Head", K(0f, 0.92f), K(1.0f, 0.932f), K(2.0f, 0.92f));

            // Bipedal walk — 0.7 s, hunched run
            var walk = NewClip("Goblin_Walk", WrapMode.Loop);
            const float gDur = 0.7f; const float gAmp = 0.06f; const float gLift = 0.04f;
            const float gHY  = 0.30f; const float gHYUp = 0.34f;
            const float gAY  = 0.65f; const float gAAmp = 0.05f;
            // Left leg: stance 0–0.35, swing 0.35–0.7
            PosZCurve(walk, "LeftLegHip",
                K(0f, gAmp), K(0.35f, -gAmp), K(0.525f, 0f), K(gDur, gAmp));
            PosYCurve(walk, "LeftLegHip",
                K(0f, gHY), K(0.35f, gHY), K(0.44f, gHYUp), K(0.525f, gHY), K(gDur, gHY));
            // Right leg: opposite
            PosZCurve(walk, "RightLegHip",
                K(0f, -gAmp), K(0.175f, 0f), K(0.35f, gAmp), K(gDur, -gAmp));
            PosYCurve(walk, "RightLegHip",
                K(0f, gHY), K(0.088f, gHYUp), K(0.175f, gHY), K(0.35f, gHY), K(gDur, gHY));
            // Arms swing opposite to opposite leg
            PosZCurve(walk, "LeftArmHip",
                K(0f, -gAAmp), K(0.35f, gAAmp), K(gDur, -gAAmp));
            PosZCurve(walk, "RightArmHip",
                K(0f, gAAmp), K(0.35f, -gAAmp), K(gDur, gAAmp));
            // Body bob
            PosYCurve(walk, "Torso",
                K(0f, 0.55f), K(0.175f, 0.558f), K(0.35f, 0.55f), K(0.525f, 0.558f), K(gDur, 0.55f));
            PosYCurve(walk, "Head",
                K(0f, 0.92f), K(0.175f, 0.930f), K(0.35f, 0.92f), K(0.525f, 0.930f), K(gDur, 0.92f));

            // Attack — goblin leaping slash
            var attack = NewClip("Goblin_Attack", WrapMode.Once);
            const float gbDur = 1.8f;
            PosZCurve(attack, "Torso",  K(0f, -0.04f), K(gbDur*0.30f, 0.12f), K(gbDur, -0.04f));
            PosYCurve(attack, "Torso",  K(0f, 0.55f),  K(gbDur*0.20f, 0.65f), K(gbDur*0.40f, 0.45f), K(gbDur, 0.55f));
            // Both arms swing up then down hard
            foreach (var a in new[]{ "LeftArmHip", "RightArmHip" })
                PosYCurve(attack, a, K(0f, gAY), K(gbDur*0.22f, gAY+0.20f), K(gbDur*0.45f, gAY-0.06f), K(gbDur, gAY));
            foreach (var a in new[]{ "LeftArmHip", "RightArmHip" })
                PosZCurve(attack, a, K(0f, 0f), K(gbDur*0.22f, -0.10f), K(gbDur*0.45f, 0.12f), K(gbDur, 0f));

            var hit = NewClip("Goblin_Hit", WrapMode.Once);
            PosZCurve(hit, "Torso", K(0f, -0.04f), K(0.10f, -0.14f), K(0.35f, -0.04f));
            PosZCurve(hit, "Head",  K(0f,  0.08f), K(0.10f, -0.04f), K(0.35f,  0.08f));

            var die = NewClip("Goblin_Die", WrapMode.Once);
            RotCurve(die, "", (0f, Quaternion.identity),
                              (0.9f, Quaternion.Euler(80f, 0f, 0f)),
                              (1.3f, Quaternion.Euler(90f, 0f, 0f)));
            PosYCurve(die, "", K(0f, 0f), K(1.3f, -0.04f));

            return (idle, walk, attack, hit, die);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Shared walk helpers
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a diagonal-gait quadruped walk clip.
        /// Group A = FrontLeft + BackRight  (in-phase)
        /// Group B = FrontRight + BackLeft  (opposite)
        /// </summary>
        private static AnimationClip QuadWalk(
            string name, float dur, float hipY, float hipYUp, float amp,
            float flZ, float brZ, float frZ, float blZ,
            string bodyPath, float bodyY, float bodyYPeak, float tailYaw)
        {
            var clip = NewClip(name, WrapMode.Loop);

            float t25 = dur * 0.30f; float t50 = dur * 0.50f;
            float t60 = dur * 0.60f; float t70 = dur * 0.70f; float t80 = dur * 0.80f;

            // Group A: starts forward
            PosZCurve(clip, "FrontLeftHip",
                K(0f, flZ+amp), K(t25, flZ), K(t50, flZ-amp), K(t70, flZ), K(dur, flZ+amp));
            PosYCurve(clip, "FrontLeftHip",
                K(0f, hipY), K(t50, hipY), K(t60, hipYUp), K(t70, hipY), K(dur, hipY));

            PosZCurve(clip, "BackRightHip",
                K(0f, brZ+amp), K(t25, brZ), K(t50, brZ-amp), K(t70, brZ), K(dur, brZ+amp));
            PosYCurve(clip, "BackRightHip",
                K(0f, hipY), K(t50, hipY), K(t60, hipYUp), K(t70, hipY), K(dur, hipY));

            // Group B: starts back
            float t20 = dur * 0.20f; float t35 = dur * 0.35f;
            PosZCurve(clip, "FrontRightHip",
                K(0f, frZ-amp), K(t20, frZ), K(t35, frZ+amp), K(t70, frZ), K(dur, frZ-amp));
            PosYCurve(clip, "FrontRightHip",
                K(0f, hipY), K(dur*0.10f, hipYUp), K(t20, hipY), K(t35, hipY), K(dur, hipY));

            PosZCurve(clip, "BackLeftHip",
                K(0f, blZ-amp), K(t20, blZ), K(t35, blZ+amp), K(t70, blZ), K(dur, blZ-amp));
            PosYCurve(clip, "BackLeftHip",
                K(0f, hipY), K(dur*0.10f, hipYUp), K(t20, hipY), K(t35, hipY), K(dur, hipY));

            // Body bob
            PosYCurve(clip, bodyPath,
                K(0f, bodyY), K(dur*0.25f, bodyYPeak), K(dur*0.5f, bodyY),
                K(dur*0.75f, bodyYPeak), K(dur, bodyY));

            // Tail wag (if requested)
            if (tailYaw > 0f)
            {
                var qN = Quaternion.identity;
                var qR = Quaternion.Euler(0f, tailYaw, 0f);
                var qL = Quaternion.Euler(0f, -tailYaw, 0f);
                RotCurve(clip, "TailRoot",
                    (0f, qN), (dur*0.25f, qR), (dur*0.5f, qN), (dur*0.75f, qL), (dur, qN));
            }

            return clip;
        }

        // ═════════════════════════════════════════════════════════════════════
        // AnimatorController builders
        // ═════════════════════════════════════════════════════════════════════

        private static AnimatorController BuildPassiveController(
            string path, AnimationClip idle, AnimationClip walk,
            AnimationClip hit, AnimationClip die)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsHit",  AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var sI = sm.AddState("Idle");   sI.motion = idle;
            var sW = sm.AddState("Walk");   sW.motion = walk;
            var sH = sm.AddState("Hit");    sH.motion = hit;
            var sD = sm.AddState("Dead");   sD.motion = die;
            sm.defaultState = sI;

            Tr(sI, sW, false, 0.15f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Tr(sW, sI, false, 0.20f).AddCondition(AnimatorConditionMode.Less,    0.1f, "Speed");
            Tr(sH, sI, true,  0.10f, 0.92f);

            var anyHit  = sm.AddAnyStateTransition(sH);
            anyHit.hasExitTime = false; anyHit.duration = 0.05f;
            anyHit.canTransitionToSelf = false;
            anyHit.AddCondition(AnimatorConditionMode.If, 0, "IsHit");

            var anyDead = sm.AddAnyStateTransition(sD);
            anyDead.hasExitTime = false; anyDead.duration = 0.10f;
            anyDead.canTransitionToSelf = false;
            anyDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static AnimatorController BuildAggressiveController(
            string path, AnimationClip idle, AnimationClip walk,
            AnimationClip attack, AnimationClip hit, AnimationClip die)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Speed",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsHit",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var sI = sm.AddState("Idle");   sI.motion = idle;
            var sW = sm.AddState("Walk");   sW.motion = walk;
            var sA = sm.AddState("Attack"); sA.motion = attack ?? idle;
            var sH = sm.AddState("Hit");    sH.motion = hit;
            var sD = sm.AddState("Dead");   sD.motion = die;
            sm.defaultState = sI;

            Tr(sI, sW, false, 0.15f).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            Tr(sW, sI, false, 0.20f).AddCondition(AnimatorConditionMode.Less,    0.1f, "Speed");
            Tr(sW, sA, false, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            Tr(sI, sA, false, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            Tr(sA, sI, true,  0.12f, 0.92f);
            Tr(sH, sI, true,  0.10f, 0.92f);

            var anyHit  = sm.AddAnyStateTransition(sH);
            anyHit.hasExitTime = false; anyHit.duration = 0.05f;
            anyHit.canTransitionToSelf = false;
            anyHit.AddCondition(AnimatorConditionMode.If, 0, "IsHit");

            var anyDead = sm.AddAnyStateTransition(sD);
            anyDead.hasExitTime = false; anyDead.duration = 0.10f;
            anyDead.canTransitionToSelf = false;
            anyDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        private static AnimatorStateTransition Tr(
            AnimatorState from, AnimatorState to,
            bool hasExit, float dur, float exitTime = 1f)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = hasExit; t.exitTime = exitTime; t.duration = dur;
            return t;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Mesh / material helpers
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Make a primitive, apply color, remove collider, return it.</summary>
        private static GameObject MP(Transform parent, string name, PrimitiveType pType,
            Vector3 pos, Vector3 scale, Color color,
            Quaternion rotation = default(Quaternion))
        {
            var go = GameObject.CreatePrimitive(pType);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale    = scale;
            go.transform.localRotation = (rotation == default(Quaternion)) ? Quaternion.identity : rotation;
            AC(go, color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Make a hip-pivot empty + hanging capsule child.</summary>
        private static void ML(Transform parent, string name,
            Vector3 pivotPos, Color color, float legLocalY, Vector3 legScale)
        {
            var pivot = new GameObject(name + "Hip");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;

            var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leg.name = name + "Leg";
            leg.transform.SetParent(pivot.transform, false);
            leg.transform.localPosition = new Vector3(0f, legLocalY, 0f);
            leg.transform.localScale    = legScale;
            AC(leg, color);
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }

        private static void AC(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            // Borrow the primitive's default shader — guarantees it matches whatever
            // render pipeline is active (URP, HDRP, Built-in).
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
        // Curve helpers
        // ═════════════════════════════════════════════════════════════════════

        private static AnimationClip NewClip(string name, WrapMode wrap)
        {
            var c = new AnimationClip { name = name, wrapMode = wrap };
            c.frameRate = 30f;
            return c;
        }

        private static Keyframe K(float t, float v) => new Keyframe(t, v, 0f, 0f);

        private static void PosXCurve(AnimationClip clip, string path, params Keyframe[] keys)
            => PC(clip, path, "localPosition.x", keys);
        private static void PosYCurve(AnimationClip clip, string path, params Keyframe[] keys)
            => PC(clip, path, "localPosition.y", keys);
        private static void PosZCurve(AnimationClip clip, string path, params Keyframe[] keys)
            => PC(clip, path, "localPosition.z", keys);
        private static void ScaleYCurve(AnimationClip clip, string path, params Keyframe[] keys)
            => PC(clip, path, "localScale.y", keys);

        private static void PC(AnimationClip clip, string path, string prop, Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        private static void RotCurve(AnimationClip clip, string path,
            params (float time, Quaternion rot)[] keys)
        {
            var cx = new AnimationCurve(); var cy = new AnimationCurve();
            var cz = new AnimationCurve(); var cw = new AnimationCurve();
            foreach (var (t, q) in keys)
            {
                cx.AddKey(new Keyframe(t, q.x, 0f, 0f)); cy.AddKey(new Keyframe(t, q.y, 0f, 0f));
                cz.AddKey(new Keyframe(t, q.z, 0f, 0f)); cw.AddKey(new Keyframe(t, q.w, 0f, 0f));
            }
            for (int i = 0; i < cx.length; i++)
            {
                cx.SmoothTangents(i, 0f); cy.SmoothTangents(i, 0f);
                cz.SmoothTangents(i, 0f); cw.SmoothTangents(i, 0f);
            }
            clip.SetCurve(path, typeof(Transform), "localRotation.x", cx);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", cy);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", cz);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", cw);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Asset helpers
        // ═════════════════════════════════════════════════════════════════════

        private static void SaveClip(AnimationClip clip, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        private static void StampDef(string assetPath, GameObject prefab)
        {
            var def = AssetDatabase.LoadAssetAtPath<Kaligo.Mobs.MobDefinition>(assetPath);
            if (def == null) { Debug.LogWarning("[MobModelCreator] Missing: " + assetPath); return; }
            def.prefabOverride = prefab;
            EditorUtility.SetDirty(def);
        }

        private static void EnsureFolders(params string[] paths)
        {
            foreach (var path in paths) EnsureFolder(path);
            EnsureFolder(ModelDir + "/Materials");   // always create mat folder
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/'); string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
