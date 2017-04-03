using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Unity.Presentation.Utils;
#endif

namespace Unity.Presentation 
{

	// Global properties for all presentations.
	public class Properties : ScriptableObject 
	{

		#region Consts

		// Properties asset name in the project.
		private const string ASSET_NAME = "Properties.asset";

		#endregion

		#region Static properties

		// Returns and creates the singleton instance.
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

		// Next slide key binding.
		public KeyCode NextSlide = KeyCode.RightArrow;

		// Previous slide key binding.
		public KeyCode PreviousSlide = KeyCode.LeftArrow;

		#endregion

		#region Private variables

		// Singleton instance.
		private static Properties instance;

		#endregion

		#region Constructor

		// Constructor.
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