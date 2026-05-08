using System;
using UnityEditor;
using UnityEngine;

namespace Kaligo.EditorTools
{
    /// <summary>
    /// Auto-configures Mixamo FBX imports inside our project conventions:
    ///   - Assets/Characters/   → humanoid rig, self-generated avatar.
    ///   - Assets/Animations/   → humanoid rig, avatar COPIED from the X Bot.
    ///   - Assets/Animations/SwordAndShield/Locomotion/ and /Stance/ → Loop Time on.
    ///
    /// This means: drop a new Mixamo .fbx into one of those folders and Unity
    /// will configure it correctly the moment it finishes importing — no manual
    /// Rig tab, no manual Loop Time, no manual Apply.
    ///
    /// To re-apply settings to assets that were imported BEFORE this script
    /// existed, use the menu: Kaligo → Reimport Animation Assets.
    ///
    /// Convention note: the path-based logic below assumes character FBXs live in
    /// Assets/Characters/ and animation FBXs live in Assets/Animations/. If you
    /// drop a non-humanoid FBX (e.g. a weapon or prop) into either folder by
    /// mistake, this script will incorrectly mark it humanoid. Keep props in
    /// Assets/Models/ or similar.
    /// </summary>
    public class FbxImportSettings : AssetPostprocessor
    {
        private const string SourceAvatarPath = "Assets/Characters/XBot/X Bot.fbx";
        private const string AnimationsRoot = "Assets/Animations/";
        private const string CharactersRoot = "Assets/Characters/";

        // Subfolders inside Assets/Animations/ whose clips should loop by default.
        // (Idles, walks, runs, strafes, etc. loop; attacks, deaths, draws don't.)
        private static readonly string[] LoopingSubfolders =
        {
            "/Locomotion/",
            "/Stance/",
        };

        private void OnPreprocessModel()
        {
            string path = assetPath;
            bool isAnimation = path.StartsWith(AnimationsRoot, StringComparison.OrdinalIgnoreCase);
            bool isCharacter = path.StartsWith(CharactersRoot, StringComparison.OrdinalIgnoreCase);

            if (!isAnimation && !isCharacter) return;

            var importer = (ModelImporter)assetImporter;

            // Everything under Characters/ and Animations/ is humanoid.
            importer.animationType = ModelImporterAnimationType.Human;

            if (isAnimation)
            {
                ConfigureAnimationFbx(importer, path);
            }
            // Character FBXs keep avatarSetup at default (Create From This Model).
        }

        private static void ConfigureAnimationFbx(ModelImporter importer, string path)
        {
            // Tell Unity to reuse the X Bot's avatar instead of generating a new one.
            // We resolve the avatar lazily here because, on the very first import of
            // the X Bot itself, this asset doesn't exist yet.
            var sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(SourceAvatarPath);
            if (sourceAvatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }
            else
            {
                Debug.LogWarning(
                    $"[FbxImportSettings] Source avatar not found at {SourceAvatarPath}. " +
                    $"Imported '{path}' with a self-generated avatar. Run " +
                    $"'Kaligo → Reimport Animation Assets' once the X Bot is in place."
                );
            }

            // Apply loop time per the looping-subfolder convention.
            if (ShouldLoop(path))
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = true;
                    clips[i].loopPose = true;
                }

                importer.clipAnimations = clips;
            }
        }

        private static bool ShouldLoop(string assetPath)
        {
            foreach (string folder in LoopingSubfolders)
            {
                if (assetPath.IndexOf(folder, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Menu utility: forces a reimport of every FBX under Assets/Animations and
    /// Assets/Characters. Useful after changing FbxImportSettings rules, or to
    /// retroactively apply them to assets imported before this script existed.
    /// </summary>
    public static class ReimportAssetsMenu
    {
        private static readonly string[] SearchFolders =
        {
            "Assets/Animations",
            "Assets/Characters",
        };

        [MenuItem("Kaligo/Reimport Animation Assets")]
        public static void ReimportAnimationAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", SearchFolders);
            int count = 0;

            try
            {
                AssetDatabase.StartAssetEditing(); // batch the reimports for speed
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[Kaligo] Reimported {count} FBX models from {string.Join(", ", SearchFolders)}.");
        }
    }
}
