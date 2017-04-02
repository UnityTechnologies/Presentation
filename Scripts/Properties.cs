using UnityEngine;
using System;

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

		private const string ASSET_NAME = "Properties.asset";

		private static Properties instance;

		public static Properties Instance
		{
			get 
			{
				if (instance == null)
				{
#if UNITY_EDITOR
					var path = Path.Combine(PresentationUtils.PackageRoot, ASSET_NAME);
					AssetDatabase.LoadAssetAtPath<Properties>(path);
#endif

					if (instance == null)
					{
						CreateInstance<Properties>();
#if UNITY_EDITOR
						AssetDatabase.CreateAsset(instance, path);
#else
						instance.hideFlags = HideFlags.HideAndDontSave;
#endif
					}
				}

				return instance;
			}
		}

		public KeyCode NextSlide = KeyCode.RightArrow;
		public KeyCode PreviousSlide = KeyCode.LeftArrow;

		protected Properties()
		{
			if (instance != null)
			{
				Debug.LogError("Properties already exists. Did you query it in a constructor?");
				return;
			}

			instance = this;
		}

	}
}