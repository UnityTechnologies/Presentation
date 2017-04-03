#if UNITY_EDITOR
using UnityEditor; 
using System.Reflection;
#endif

namespace Unity.Presentation.Utils
{

	// Reflection helper.
	public class InternalHelper 
	{
#if UNITY_EDITOR

		private static MethodInfo EditorApplication_globalEventHandler;
		private static System.Type GameView;
		private static System.Type WindowLayout;
		private static MethodInfo WindowLayout_FindEditorWindowOfType;
		private static MethodInfo WindowLayout_IsMaximized;
		private static MethodInfo WindowLayout_Maximize;
		private static MethodInfo WindowLayout_Unmaximize;

		// Returns current main Game View.
		public static EditorWindow GetGameView()
		{
			initWindowTypes();
			return WindowLayout_FindEditorWindowOfType.Invoke(null, new object[]{GameView}) as EditorWindow;
		}

		// Toggles Maximized state of a Game View.
		public static void ToggleGameViewSize()
		{
			initWindowTypes();
			var gameView = GetGameView();
			if ((bool)WindowLayout_IsMaximized.Invoke(null, new object[]{gameView}))
				WindowLayout_Unmaximize.Invoke(null, new object[]{gameView});
			else
				WindowLayout_Maximize.Invoke(null, new object[]{gameView});
		}

		// Initializes reflection.
		private static void initWindowTypes()
		{
			if (GameView != null) return;

			GameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
			WindowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
			WindowLayout_FindEditorWindowOfType = WindowLayout.GetMethod("FindEditorWindowOfType", BindingFlags.Static | BindingFlags.NonPublic);
			WindowLayout_IsMaximized = WindowLayout.GetMethod("IsMaximized", BindingFlags.Static | BindingFlags.NonPublic);
			WindowLayout_Maximize = WindowLayout.GetMethod("Maximize", BindingFlags.Static | BindingFlags.Public);
			WindowLayout_Unmaximize = WindowLayout.GetMethod("Unmaximize", BindingFlags.Static | BindingFlags.Public);
		}
			
#endif

	}
}