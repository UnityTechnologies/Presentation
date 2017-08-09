using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Unity.Presentation.Utils;
#endif

namespace Unity.Presentation
{
    /// <summary>
    /// Global properties for all presentations.
    /// </summary>
    public class Properties : ScriptableObject
    {

#region Consts

        private const string ASSET_NAME = "Properties.asset";

#endregion

#region Static properties

        /// <summary>
        /// Properties singleton instance.
        /// </summary>
        public static Properties Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    // In the editor try to load the asset from disk.
                    var path = Path.Combine(PresentationUtils.PackageRoot, ASSET_NAME);
                    AssetDatabase.LoadAssetAtPath<Properties>(path);
#endif

                    if (instance == null)
                    {
                        CreateInstance<Properties>();
#if UNITY_EDITOR
                        // In the editor save the asset to disk.
                        AssetDatabase.CreateAsset(instance, path);
#else
						instance.hideFlags = HideFlags.HideAndDontSave;
#endif
                    }
                }
                return instance;
            }
        }

#endregion

#region Public fields/properties

        /// <summary>
        /// Next slide key binding.
        /// </summary>
        public KeyCode NextSlide = KeyCode.RightArrow;

        /// <summary>
        /// Previous slide key binding.
        /// </summary>
        public KeyCode PreviousSlide = KeyCode.LeftArrow;

#endregion

#region Private variables

        // Singleton instance.
        private static Properties instance;

#endregion

#region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Properties()
        {
            if (instance != null)
            {
                Debug.LogError("Properties already exists. Did you query it in a constructor?");
                return;
            }

            instance = this;
        }

#endregion

    }
}