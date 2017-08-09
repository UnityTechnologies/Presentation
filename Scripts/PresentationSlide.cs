using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Presentation
{
    /// <summary>
    /// Presentation slide.
    /// </summary>
    [Serializable]
    public class PresentationSlide
    {
        /// <summary>
        /// Idicates if this slide is visible or hidden.
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Indicates if this slide starts in Play Mode.
        /// </summary>
        public bool StartInPlayMode = true;

        /// <summary>
        /// Returns slide scene path.
        /// </summary>
        public string ScenePath
        {
            get
            {
#if UNITY_EDITOR
                return AssetDatabase.GetAssetPath(Scene);
#else
                return scenePath;
#endif
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Prepares the slide for standalone build, caching properties which are not obrainable in build.
        /// </summary>
        public void PrepareForBuild()
        {
            scenePath = ScenePath;
        }

        /// <summary>
        /// Scene asset in the editor.
        /// </summary>
        public SceneAsset Scene;
#endif

#pragma warning disable 414
        /// <summary>
        /// Scene path in the player.
        /// </summary>
        [SerializeField]
        private string scenePath;
#pragma warning restore 414

    }
}