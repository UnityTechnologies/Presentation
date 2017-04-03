using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Presentation 
{
	// Presentation asset.
	[CreateAssetMenu(fileName = "Slide Deck", menuName = "Slide Deck")]
	public class SlideDeck : ScriptableObject 
	{

		#region Consts

		// Play Mode filter for GetSlides.
		public enum PlayModeType
		{
			// Slides which work in Play Mode.
			PlayMode				= 1 << 0,

			// Slides which work outside of Play Mode.
			NonPlayMode				= 1 << 1,

			// All slides.
			All						= PlayMode | NonPlayMode
		}

		// Visibility filter for GetSlides.
		public enum VisibilityType
		{
			// Visible slides.
			Visible 				= 1 << 0,

			// Hidden slides.
			Hidden 					= 1 << 1,

			// All slides.
			All						= Visible | Hidden
		}

		#endregion

		#region Public fields/properties

		// The list of slides.
		public List<PresentationSlide> Slides 
		{
			get { return slides; }
		}

		// Slides background color
		public Color BackgroundColor
		{
			get { return backgroundColor; }
			set { backgroundColor = value; }
		}

		// Indicates if the slide deck is saved to disk.
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

		// Returns this slide deck asset path.
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

		// Returns this slide asset name.
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
		public List<PresentationSlide> slides = new List<PresentationSlide>();

		[SerializeField]
		private Color backgroundColor = Color.black;

		#endregion

		#region Public methods

#if UNITY_EDITOR
		// Saves the slide deck to an asset on disk.
		// bool createAssetIfNeeded -- Should the asset file be created if this asset hasn't been saved yet.
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

		// Prepares slides for a standalone build.
		public void PrepareSlidesForBuild()
		{
			foreach (var slide in Slides)
			{
				slide.PrepareForBuild();
			}
		}
#endif

		// Returns a list of slides based on filters.
		// PlayModeType playmode -- Play Mode filter.
		// VisibilityType visibility -- Visibility filter.
		public List<PresentationSlide> GetSlides(PlayModeType playmode, VisibilityType visibility)
		{
			if (playmode == PlayModeType.All && visibility == VisibilityType.All) return new List<PresentationSlide>(Slides);

			var list = new List<PresentationSlide>();

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

	#region Slide

	// Presentation slide.
	[Serializable]
	public class PresentationSlide
	{

		// Slide scene path.
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
		// Prepares the slide for standalone build.
		public void PrepareForBuild()
		{
			scenePath = ScenePath;
		}

		// Scene asset in the editor.
		public SceneAsset Scene;
#endif

#pragma warning disable 414
		[SerializeField]
		// Scene path in the player.
		private string scenePath;
#pragma warning restore 414

		// Idicates if this slide is visible or hidden.
		public bool Visible = true;

		// Indicates if this slide starts in Play Mode.
		public bool StartInPlayMode = true;
	}

	#endregion

}