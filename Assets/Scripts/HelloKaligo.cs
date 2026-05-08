using UnityEngine;
using UnityEngine.InputSystem;

namespace Kaligo
{
    /// <summary>
    /// First script in the project. A sanity check that the Unity → C# → Input System
    /// pipeline works end-to-end. Once this prints to the Console, Phase 0's
    /// exit criterion is satisfied.
    ///
    /// To use:
    ///   1. In the Hierarchy, create an empty GameObject (right-click → Create Empty),
    ///      or use the default Cube in the SampleScene.
    ///   2. Drag this script onto the GameObject in the Inspector.
    ///   3. Press Play. You should see "The fog parts. Kaligo wakes." in the Console.
    ///   4. Press Space. You should see "Step." every time you press it.
    ///
    /// If the Console window isn't open: Window → General → Console (or Ctrl+Shift+C).
    /// </summary>
    public class HelloKaligo : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("The fog parts. Kaligo wakes. (Phase 0: hello, world.)");
        }

        private void Update()
        {
            // Using Unity's new Input System (the URP template's default).
            // We check Keyboard.current is non-null in case the editor briefly
            // loses focus or the platform has no keyboard attached.
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("Step. (Space pressed.)");
            }
        }
    }
}
