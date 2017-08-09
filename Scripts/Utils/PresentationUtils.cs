#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Unity.Presentation.Utils
{
    /// <summary>
    /// General presentation utils.
    /// </summary>
    public class PresentationUtils
    {
        /// <summary>
        /// Default presentation folder root.
        /// </summary>
        public const string DEFAULT_PACKAGE_ROOT = "Assets/Presentation";

        /// <summary>
        /// Presentation folder root in the project.
        /// </summary>
        public static string PackageRoot
        {
            get
            {
#if UNITY_EDITOR
                // Looks for the folder relative to Editor/PresentationWindow.cs in case it was moved in the project.
                var guids = AssetDatabase.FindAssets("PresentationWindow t:Script");
                if (guids.Length == 0) return DEFAULT_PACKAGE_ROOT;

                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return path.Substring(0, path.IndexOf("Editor/PresentationWindow.cs"));
#else
				return DEFAULT_PACKAGE_ROOT;
#endif
            }
        }

    }
}