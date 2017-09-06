using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Presentation
{
    /// <summary>
    /// Presentation asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Slide Deck", menuName = "Slide Deck")]
    public class SlideDeck : ScriptableObject
    {
#region Consts

        /// <summary>
        /// Play Mode filter for GetSlides.
        /// </summary>
        [Flags]
        public enum PlayModeType
        {
            /// <summary>
            /// Slides which work in Play Mode.
            /// </summary>
            PlayMode = 1 << 0,

            /// <summary>
            /// Slides which work outside of Play Mode.
            /// </summary>
            NonPlayMode = 1 << 1,

            /// <summary>
            /// All slides.
            /// </summary>
            All = PlayMode | NonPlayMode
        }

        /// <summary>
        /// Visibility filter for GetSlides.
        /// </summary>
        [Flags]
        public enum VisibilityType
        {
            /// <summary>
            /// Visible slides.
            /// </summary>
            Visible = 1 << 0,

            /// <summary>
            /// Hidden slides.
            /// </summary>
            Hidden = 1 << 1,

            /// <summary>
            /// All slides.
            /// </summary>
            All = Visible | Hidden
        }

#endregion

#region Public fields

        /// <summary>
        /// The list of slides.
        /// </summary>
        public List<PresentationSlide> Slides
        {
            get { return slides; }
        }

        /// <summary>
        /// Slides background color
        /// </summary>
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        /// <summary>
        /// Indicates if the slide deck is saved to disk.
        /// </summary>
        public bool IsSavedOnDisk
        {
            get
            {
#if UNITY_EDITOR
                return !string.IsNullOrEmpty(Path);
#else
				return true;
#endif
            }
        }

        /// <summary>
        /// Slide deck asset path.
        /// </summary>
        public string Path
        {
            get
            {
#if UNITY_EDITOR
                return AssetDatabase.GetAssetPath(this);
#else
				return null;
#endif
            }
        }

        /// <summary>
        /// Slide asset name.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

#endregion

#region Private variables

        [SerializeField]
        [FormerlySerializedAs("Slides")]
        private List<PresentationSlide> slides = new List<PresentationSlide>();

        [SerializeField]
        private Color backgroundColor = Color.black;

#endregion

#region Public methods

#if UNITY_EDITOR
        /// <summary>
        /// Saves the slide deck to an asset on disk.
        /// </summary>
        /// <param name="createAssetIfNeeded">Should the asset file be created if this asset hasn't been saved yet.</param>
        public void Save(bool createAssetIfNeeded = false)
        {
            if (IsSavedOnDisk)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (createAssetIfNeeded)
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save Slide Deck", "Presentation.asset", "asset", "");
                    if (string.IsNullOrEmpty(path)) return;

                    this.hideFlags = HideFlags.None;
                    AssetDatabase.CreateAsset(this, path);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>
        /// Prepares slides for a standalone build.
        /// </summary>
        public void PrepareSlidesForBuild()
        {
            foreach (var slide in Slides) slide.PrepareForBuild();
        }
#endif

        /// <summary>
        /// Returns a list of slides based on filters.
        /// </summary>
        /// <param name="playmode">Play Mode filter.</param>
        /// <param name="visibility">Visibility filter.</param>
        /// <returns>A list of filtered slides.</returns>
        public List<PresentationSlide> GetSlides(PlayModeType playmode, VisibilityType visibility)
        {
            if (playmode == PlayModeType.All && visibility == VisibilityType.All) return Slides;

            var list = new List<PresentationSlide>(slides.Count);

            foreach (var slide in Slides)
            {
                if (slide.StartInPlayMode)
                {
                    if ((playmode & PlayModeType.PlayMode) == 0) continue;
                }
                else
                {
                    if ((playmode & PlayModeType.NonPlayMode) == 0) continue;
                }

                if (slide.Visible)
                {
                    if ((visibility & VisibilityType.Visible) == 0) continue;
                }
                else
                {
                    if ((visibility & VisibilityType.Hidden) == 0) continue;
                }

                list.Add(slide);
            }

            return list;
        }

#endregion

    }
}