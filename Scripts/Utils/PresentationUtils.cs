using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Unity.Presentation.Utils
{
	public class PresentationUtils
	{

		public const string DEFAULT_PACKAGE_ROOT = "Assets/Presentation";

		public static string PackageRoot
		{
			get 
			{
#if UNITY_EDITOR
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