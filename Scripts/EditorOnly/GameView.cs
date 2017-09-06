#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Presentation.Utils;
using System.Reflection;

namespace Unity.Presentation.EditorOnly
{
    /// <summary>
    /// The singleton abstracting internal methods of GameView.
    /// </summary>
    public class GameView : ScriptableObject
    {

#region Consts

        /// <summary>
        /// Mac top menu height.
        /// </summary>
        private const int MENU_HEIGHT = 22;

        /// <summary>
        /// GameView state.
        /// </summary>
        private enum State
        {
            Normal,
            Maximized,
            Fullscreen
        }

#endregion

#region Public properties

        /// <summary>
        /// GameView singleton instance.
        /// </summary>
        public static GameView Instance
        {
            get
            {
                if (instance == null)
                {
                    var objs = Resources.FindObjectsOfTypeAll<GameView>();
                    if (objs.Length > 0) instance = objs[0];
                    else
                    {
                        instance = CreateInstance<GameView>();
                        instance.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is maximized.
        /// </summary>
        public bool IsMaximized
        {
            get
            {
                return state == State.Maximized;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is fullscreen.
        /// </summary>
        public bool IsFullscreen
        {
            get
            {
                return state == State.Fullscreen;
            }
        }

#endregion

#region Private variables

        private static GameView instance;

        private MethodInfo EditorApplication_globalEventHandler;
        private System.Type GameViewType;
        private System.Type WindowLayout;
        private MethodInfo WindowLayout_FindEditorWindowOfType;
        private MethodInfo WindowLayout_IsMaximized;
        private MethodInfo WindowLayout_Maximize;
        private MethodInfo WindowLayout_Unmaximize;

        private State state = State.Normal;

        private EditorWindow fullscreenGameView;

#endregion

#region Public methods

        /// <summary>
        /// Sets GameView into normal state.
        /// </summary>
        public void SetNormal()
        {
            if (state == State.Maximized)
            {
                var gameView = getGameView();
                if (gameView != null && isMaximized(gameView)) WindowLayout_Unmaximize.Invoke(null, new object[]{ gameView });
            }
            else if (state == State.Fullscreen)
            {
                if (fullscreenGameView == null) return;
                fullscreenGameView.Close();
                fullscreenGameView = null;
            }
            state = State.Normal;
        }

        /// <summary>
        /// Sets GameView into maximized state.
        /// </summary>
        public void SetMaximized()
        {
            if (state != State.Normal) return;

            WindowLayout_Maximize.Invoke(null, new object[]{ getGameView() });
            state = State.Maximized;
        }

        /// <summary>
        /// Sets GameView int fullscreen state.
        /// </summary>
        public void SetFullscreen()
        {
            if (state != State.Normal) return;

            if (fullscreenGameView == null)
            {
                fullscreenGameView = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);
                fullscreenGameView.name = "fullscreen";
            }

            fullscreenGameView.Show();
            fullscreenGameView.Focus();

            var res = Screen.currentResolution;
            var size = new Vector2(res.width, res.height + MENU_HEIGHT);
            fullscreenGameView.minSize = fullscreenGameView.maxSize = size;
            fullscreenGameView.position = new Rect(new Vector2(0, -MENU_HEIGHT), size);

            state = State.Fullscreen;
        }

#endregion

#region Unity methods

        private void OnEnable()
        {
            initWindowTypes();

            var gameView = getGameView();
            if (gameView != null && isMaximized(gameView)) state = State.Maximized;
        }

#endregion

#region Private functions

        private EditorWindow createEditorWindow()
        {
            var editorWindow = CreateInstance<EditorWindow>();
            editorWindow.titleContent = new GUIContent("Game View (temp)");
            return editorWindow;
        }

        private EditorWindow getGameView()
        {
            return WindowLayout_FindEditorWindowOfType.Invoke(null, new object[]{ GameViewType }) as EditorWindow;
        }

        private bool isMaximized(EditorWindow window)
        {
            return (bool)WindowLayout_IsMaximized.Invoke(null, new object[]{ window });
        }

        private void initWindowTypes()
        {
            if (GameViewType != null) return;

            GameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            WindowLayout = typeof(Editor).Assembly.GetType("UnityEditor.WindowLayout");
            WindowLayout_FindEditorWindowOfType = WindowLayout.GetMethod("FindEditorWindowOfType", BindingFlags.Static | BindingFlags.NonPublic);
            WindowLayout_IsMaximized = WindowLayout.GetMethod("IsMaximized", BindingFlags.Static | BindingFlags.NonPublic);
            WindowLayout_Maximize = WindowLayout.GetMethod("Maximize", BindingFlags.Static | BindingFlags.Public);
            WindowLayout_Unmaximize = WindowLayout.GetMethod("Unmaximize", BindingFlags.Static | BindingFlags.Public);
        }

#endregion

    }
}

#endif