using Kaligo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kaligo.EditorTools
{
    /// <summary>
    /// One-click setup for Sitting C — wires PlayerController onto the X Bot,
    /// tunes the CharacterController capsule, positions the Main Camera so the
    /// X Bot is visible, and adds a ground plane if the scene doesn't have one.
    ///
    /// Idempotent: safe to run more than once. Won't duplicate components or
    /// the ground plane.
    ///
    /// What it does NOT do:
    ///   - Save the scene (left to you so you can review changes).
    ///   - Tune turn speed / acceleration / etc. — those are "feel" decisions
    ///     and live in Sitting E.
    /// </summary>
    public static class SittingCSetup
    {
        private const string XBotName = "X Bot";
        private const string GroundName = "Ground";

        [MenuItem("Kaligo/Setup/Sitting C - Wire PlayerController")]
        public static void Run()
        {
            // 1. Find X Bot in the active scene.
            var xBot = GameObject.Find(XBotName);
            if (xBot == null)
            {
                EditorUtility.DisplayDialog(
                    "Kaligo Setup",
                    $"No GameObject named '{XBotName}' found in the active scene.\n\n" +
                    "Drag Assets/Characters/XBot/X Bot.fbx into the Hierarchy first, " +
                    "then run this menu again.",
                    "OK"
                );
                return;
            }

            // 2. Add PlayerController. The [RequireComponent(typeof(CharacterController))]
            //    on PlayerController auto-adds CharacterController if it's not there.
            var pc = xBot.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = xBot.AddComponent<PlayerController>();
            }

            // 3. Tune the CharacterController capsule for a humanoid (~1.8m tall).
            var cc = xBot.GetComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.3f;
            cc.height = 1.8f;
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;

            // 4. Verify the Animator has a controller (warn if missing) and disable
            //    Apply Root Motion so the Animator doesn't override our movement.
            //    All our locomotion clips are "in place" — root motion would fight Move().
            var anim = xBot.GetComponent<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                if (anim.runtimeAnimatorController == null)
                {
                    Debug.LogWarning(
                        "[Kaligo] X Bot's Animator has no Controller assigned. WASD will move " +
                        "the character but no animations will play. Drag XBotAnimator into the " +
                        "Animator's Controller slot."
                    );
                }
            }

            // 5. Position the Main Camera so the X Bot is visible at world origin.
            //    (Cinemachine in Sitting D will replace this; we just want a usable
            //     view for testing right now.)
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.5f, -5f);
                cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            }
            else
            {
                Debug.LogWarning(
                    "[Kaligo] No camera tagged 'MainCamera' found. PlayerController will fall " +
                    "back to world-relative WASD axes — playable, but not what we want long term."
                );
            }

            // 6. Ensure a ground plane exists, otherwise the X Bot falls forever.
            var ground = GameObject.Find(GroundName);
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = GroundName;
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50m × 50m
            }

            // 7. Mark the scene dirty so Unity knows to offer a save.
            EditorSceneManager.MarkSceneDirty(xBot.scene);

            // 8. Select the X Bot so the user can immediately inspect what was wired.
            Selection.activeGameObject = xBot;

            Debug.Log(
                "[Kaligo] Sitting C wired:\n" +
                "  • PlayerController + CharacterController on X Bot\n" +
                "  • Capsule tuned (center 0,0.9,0 / radius 0.3 / height 1.8)\n" +
                "  • Main Camera at (0, 1.5, -5)\n" +
                "  • Ground plane at origin (50×50m)\n\n" +
                "Save the scene (Ctrl+S) and press Play. Use WASD; hold Shift to sprint."
            );
        }
    }
}
