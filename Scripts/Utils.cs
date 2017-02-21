#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Presentation
{
	public class Utils
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

#if UNITY_EDITOR

		private static System.Type GameView;
		private static System.Type WindowLayout;
		private static MethodInfo FindEditorWindowOfType;
		private static MethodInfo IsMaximized;
		private static MethodInfo Maximize;
		private static MethodInfo Unmaximize;

		public static void ToggleGameViewSize()
		{
			if (GameView == null)
			{
				GameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
				WindowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
				FindEditorWindowOfType = WindowLayout.GetMethod("FindEditorWindowOfType", BindingFlags.Static | BindingFlags.NonPublic);
				IsMaximized = WindowLayout.GetMethod("IsMaximized", BindingFlags.Static | BindingFlags.NonPublic);
				Maximize = WindowLayout.GetMethod("Maximize", BindingFlags.Static | BindingFlags.Public);
				Unmaximize = WindowLayout.GetMethod("Unmaximize", BindingFlags.Static | BindingFlags.Public);
			}

			var gameView = FindEditorWindowOfType.Invoke(null, new object[]{GameView});
			if ((bool)IsMaximized.Invoke(null, new object[]{gameView}))
				Unmaximize.Invoke(null, new object[]{gameView});
			else
				Maximize.Invoke(null, new object[]{gameView});
		}
#endif

	}
}