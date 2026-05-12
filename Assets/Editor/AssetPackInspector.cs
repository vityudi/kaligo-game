#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Kaligo.Editor
{
    /// <summary>
    /// Scans a folder for prefabs and prints their Animator parameter names + animation
    /// clip names to the Console. Run after importing a third-party asset pack so you can
    /// see what's available for mob integration.
    ///
    /// Run via: Kaligo → Tools → Inspect Asset Pack Prefabs
    /// </summary>
    public static class AssetPackInspector
    {
        [MenuItem("Kaligo/Tools/Inspect Asset Pack Prefabs")]
        public static void InspectPrefabs()
        {
            // ── Ask user to pick a folder inside the project ──────────────────
            string folder = EditorUtility.OpenFolderPanel(
                "Select asset pack folder to inspect",
                Application.dataPath, "");

            if (string.IsNullOrEmpty(folder)) return;

            // Convert absolute path → project-relative
            if (folder.StartsWith(Application.dataPath))
                folder = "Assets" + folder.Substring(Application.dataPath.Length);

            var guids  = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Found",
                    "No prefabs found in: " + folder, "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Asset Pack Prefab Report ===");
            sb.AppendLine("Folder: " + folder);
            sb.AppendLine("Prefabs found: " + guids.Length);
            sb.AppendLine();

            var results = new List<string>();

            foreach (var guid in guids)
            {
                string path   = AssetDatabase.GUIDToAssetPath(guid);
                var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var anim = prefab.GetComponentInChildren<Animator>(true);
                string section = "PREFAB: " + prefab.name + "\n  Path: " + path;

                if (anim == null)
                {
                    section += "\n  (no Animator)";
                }
                else
                {
                    var ctrl = anim.runtimeAnimatorController as AnimatorController;
                    if (ctrl != null)
                    {
                        // Parameters
                        section += "\n  Controller: " + ctrl.name;
                        section += "\n  Parameters:";
                        foreach (var p in ctrl.parameters)
                            section += "\n    • " + p.name + "  [" + p.type + "]";

                        // Clips from all layers/states
                        var clips = new HashSet<string>();
                        foreach (var layer in ctrl.layers)
                            foreach (var state in layer.stateMachine.states)
                                if (state.state.motion is AnimationClip c)
                                    clips.Add(c.name);

                        section += "\n  Clips:";
                        foreach (var c in clips)
                            section += "\n    • " + c;
                    }
                    else
                    {
                        // Override controller or animator override
                        section += "\n  Controller: " + anim.runtimeAnimatorController?.name + " (override or runtime ctrl — open manually)";
                    }
                }

                results.Add(section);
                sb.AppendLine(section);
                sb.AppendLine();
            }

            // Write report to project root
            string reportPath = "Assets/../AssetPackReport.txt";
            File.WriteAllText(reportPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log("[AssetPackInspector]\n" + sb.ToString());
            EditorUtility.DisplayDialog(
                "Inspection Complete",
                guids.Length + " prefabs scanned.\n\nFull report saved to:\nAssetPackReport.txt (project root)\n\nAlso printed to Console.",
                "OK");
        }
    }
}
#endif
