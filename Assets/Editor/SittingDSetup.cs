using Kaligo;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kaligo.EditorTools
{
    /// <summary>
    /// One-click setup for Sitting D — replaces the static Main Camera with a
    /// Cinemachine third-person rig, mouse-driven, with wall-collision pull-in.
    ///
    /// What it does:
    ///   1. Adds CinemachineBrain to the Main Camera (Cinemachine drives it).
    ///   2. Creates a CameraTarget child on the X Bot at chest height. This is
    ///      the camera's pivot — its rotation is independent of the X Bot's.
    ///   3. Attaches CameraOrbitInput to the CameraTarget so mouse delta yaws
    ///      and pitches the pivot each frame.
    ///   4. Creates a "Player Camera" GameObject in the scene with:
    ///        - CinemachineCamera (the vcam)
    ///        - CinemachineThirdPersonFollow (rigid behind-shoulder follow)
    ///      The vcam tracks the CameraTarget; ThirdPersonFollow handles distance,
    ///      shoulder offset, and built-in collision pull-in.
    ///   5. Tags the X Bot as "Player" so the camera ignores it when checking
    ///      for collision (otherwise the X Bot's CharacterController capsule
    ///      pushes the camera in).
    ///
    /// Idempotent: safe to run more than once. Won't duplicate components,
    /// CameraTargets, or Player Cameras.
    ///
    /// What it does NOT do:
    ///   - Save the scene (so you can review before committing).
    ///   - Tune sensitivity, distance, shoulder offset, etc. — those are "feel"
    ///     decisions and live in Sitting E.
    /// </summary>
    public static class SittingDSetup
    {
        private const string XBotName = "X Bot";
        private const string CameraTargetName = "CameraTarget";
        private const string PlayerCameraName = "Player Camera";
        private const string PlayerTag = "Player";

        // Camera-target sits roughly at chest / upper torso of a 1.8m humanoid.
        private static readonly Vector3 CameraTargetLocalPos = new Vector3(0f, 1.5f, 0f);

        // Starting rig values. These are intentionally conservative — feel pass
        // is Sitting E.
        private const float StartDistance = 4f;
        private static readonly Vector3 StartShoulderOffset = new Vector3(0.5f, 0f, 0f);
        private const float StartVerticalArmLength = 0.4f;
        private const float StartCameraSide = 1f;     // 0 = left, 1 = right
        private const float StartCameraRadius = 0.2f; // collision sphere

        [MenuItem("Kaligo/Setup/Sitting D - Wire Cinemachine Camera")]
        public static void Run()
        {
            // 1. Find X Bot.
            var xBot = GameObject.Find(XBotName);
            if (xBot == null)
            {
                EditorUtility.DisplayDialog(
                    "Kaligo Setup",
                    $"No GameObject named '{XBotName}' found in the active scene.\n\n" +
                    "Run 'Kaligo → Setup → Sitting C - Wire PlayerController' first.",
                    "OK"
                );
                return;
            }

            // 2. Tag the X Bot as Player so the camera collision logic ignores it.
            //    "Player" is a built-in Unity tag, so this can't fail on missing tag.
            if (!xBot.CompareTag(PlayerTag))
            {
                xBot.tag = PlayerTag;
            }

            // 3. Find or create the CameraTarget child of X Bot.
            var camTargetTf = xBot.transform.Find(CameraTargetName);
            if (camTargetTf == null)
            {
                var go = new GameObject(CameraTargetName);
                go.transform.SetParent(xBot.transform, worldPositionStays: false);
                go.transform.localPosition = CameraTargetLocalPos;
                go.transform.localRotation = Quaternion.identity;
                camTargetTf = go.transform;
            }

            // 4. Attach CameraOrbitInput to CameraTarget if not already there.
            if (camTargetTf.GetComponent<CameraOrbitInput>() == null)
            {
                camTargetTf.gameObject.AddComponent<CameraOrbitInput>();
            }

            // 5. Add CinemachineBrain to Main Camera.
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                EditorUtility.DisplayDialog(
                    "Kaligo Setup",
                    "No camera tagged 'MainCamera' was found. " +
                    "Add or restore a Main Camera, then run this menu again.",
                    "OK"
                );
                return;
            }
            if (mainCam.GetComponent<CinemachineBrain>() == null)
            {
                mainCam.gameObject.AddComponent<CinemachineBrain>();
            }

            // 6. Create or reuse the Player Camera GameObject.
            var vcamGO = GameObject.Find(PlayerCameraName);
            if (vcamGO == null)
            {
                vcamGO = new GameObject(PlayerCameraName);
                // Position is irrelevant once the rig is following — Cinemachine
                // moves the GameObject every frame. We set a safe default for
                // visual sanity if the user pauses with no Brain active.
                vcamGO.transform.position = new Vector3(0f, 2f, -4f);
            }

            // 7. CinemachineCamera (the vcam itself).
            var cmCam = vcamGO.GetComponent<CinemachineCamera>();
            if (cmCam == null) cmCam = vcamGO.AddComponent<CinemachineCamera>();
            cmCam.Follow = camTargetTf;
            // LookAt deliberately left null: ThirdPersonFollow drives orientation
            // from the rig itself; adding a separate aim target double-aims.
            cmCam.LookAt = null;

            // 8. CinemachineThirdPersonFollow — rigid behind-shoulder rig.
            var tpf = vcamGO.GetComponent<CinemachineThirdPersonFollow>();
            if (tpf == null) tpf = vcamGO.AddComponent<CinemachineThirdPersonFollow>();
            tpf.CameraDistance = StartDistance;
            tpf.ShoulderOffset = StartShoulderOffset;
            tpf.VerticalArmLength = StartVerticalArmLength;
            tpf.CameraSide = StartCameraSide;

            // Camera collision settings live in the AvoidObstacles nested struct
            // in Cinemachine 3.x (not as direct fields on the component). Because
            // it's a struct, we read-modify-write the whole thing — assigning a
            // field on the getter would mutate a copy.
            //
            // IgnoreTag = "Player" prevents the camera collision raycasts from
            // catching the X Bot's own CharacterController capsule, which would
            // otherwise constantly pull the camera into the body.
            var avoid = tpf.AvoidObstacles;
            avoid.CameraRadius = StartCameraRadius;
            avoid.IgnoreTag = PlayerTag;
            tpf.AvoidObstacles = avoid;

            // 9. Mark the scene dirty so Unity offers to save.
            EditorSceneManager.MarkSceneDirty(xBot.scene);

            // 10. Select the vcam so the user lands on the rig's inspector for
            //     immediate tweaking.
            Selection.activeGameObject = vcamGO;

            Debug.Log(
                "[Kaligo] Sitting D wired:\n" +
                $"  • X Bot tagged '{PlayerTag}' (camera ignores own body)\n" +
                $"  • CameraTarget child on X Bot at {CameraTargetLocalPos}\n" +
                "  • CameraOrbitInput on CameraTarget (mouse drives yaw/pitch)\n" +
                "  • CinemachineBrain on Main Camera\n" +
                "  • Player Camera vcam: CinemachineCamera + ThirdPersonFollow\n" +
                $"    distance {StartDistance}, shoulder {StartShoulderOffset}, " +
                $"arm length {StartVerticalArmLength}, side {StartCameraSide}, " +
                $"radius {StartCameraRadius}\n\n" +
                "Save the scene (Ctrl+S) and press Play. Mouse orbits the camera; " +
                "WASD moves the X Bot relative to camera; Shift sprints.\n" +
                "Press Esc in the Editor to release the cursor."
            );
        }
    }
}
