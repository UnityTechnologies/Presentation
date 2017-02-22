#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Unity.Presentation 
{
	/// <summary>
	/// Presentation asset.
	/// </summary>
	[CreateAssetMenu(fileName = "Slide Deck", menuName = "Slide Deck")]
	public class SlideDeck : ScriptableObject 
	{
		public List<PresentationSlide> Slides = new List<PresentationSlide>();

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

		public string Name
		{
			get
			{
				return name;
			}
		}

		public void Save(bool createAssetIfNeeded = false)
		{
#if UNITY_EDITOR
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
#endif
		}
	}

	/// <summary>
	/// Presentation slide.
	/// </summary>
	[Serializable]
	public class PresentationSlide
	{
		public string ScenePath
		{
			get 
			{
				return AssetDatabase.GetAssetPath(Scene);
			}
		}
		public SceneAsset Scene;
		public bool Visible = true;
		public bool StartInPlayMode = true;
	}
}