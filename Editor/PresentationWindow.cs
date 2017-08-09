using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using Unity.Presentation.Inspectors;
using Unity.Presentation.Utils;

namespace Unity.Presentation
{
    /// <summary>
    /// Presentation configuration window.
    /// To open this window go to Window > Presentation in the editor.
    /// </summary>
    public class PresentationWindow : EditorWindow
    {
#region Styles

        /// <summary>
        /// Window styles.
        /// </summary>
        private class Styles
        {
            public readonly Color SELECTED_COLOR = new Color(1, 0, 0);

            public readonly GUIStyle BUTTON = new GUIStyle("CommandMid");
            public readonly GUIStyle BUTTONS_LEFT = new GUIStyle("CommandLeft");
            public readonly GUIStyle BUTTONS_MID = new GUIStyle("CommandMid");
            public readonly GUIStyle BUTTONS_RIGHT = new GUIStyle("CommandRight");
            public readonly GUIStyle NAME = new GUIStyle();

            public readonly GUIContent TEXT_NEW = new GUIContent("New", "Create new Presentation");
            public readonly GUIContent TEXT_LOAD = new GUIContent("Load", "Load Presentation from disk");
            public readonly GUIContent TEXT_SAVE = new GUIContent("Save", "Save Presentation to disk");
            public readonly GUIContent TEXT_BUILD = new GUIContent("Build", "Build Standalone Presentation for selected platform");
            public readonly GUIContent TEXT_PREV = new GUIContent("<<", "Go to the previous slide");
            public readonly GUIContent TEXT_FROM_BEGINNING = new GUIContent("> B", "Start Presentation from the first slide");
            public readonly GUIContent TEXT_STOP = new GUIContent("Stop", "Stop Presentation");
            public readonly GUIContent TEXT_NEXT = new GUIContent(">>", "Go to the next slide");

            public Styles()
            {
                var c = new GUIStyle("Command");

                BUTTON.fontSize = 9;
                // GUIStyle("Command") doesn't display text.
                BUTTON.normal.background = c.normal.background;
                BUTTON.active.background = c.active.background;
                BUTTONS_LEFT.fontSize = 9;
                BUTTONS_MID.fontSize = 9;
                BUTTONS_RIGHT.fontSize = 9;

                NAME.padding = new RectOffset(5, 0, 0, 0);
                NAME.fontStyle = FontStyle.Bold;
                NAME.alignment = TextAnchor.MiddleLeft;
                NAME.fixedHeight = BUTTONS_LEFT.fixedHeight + 4;
            }
        }

#endregion

#region Static methods

        /// <summary>
        /// Shows the window.
        /// </summary>
        [MenuItem("Window/Presentation")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.CreateInstance<PresentationWindow>();
            wnd.titleContent = new GUIContent("Presentation");
            wnd.Show();
        }

#endregion

#region Private variables

        /// <summary>
        /// Window styles.
        /// </summary>
        private static Styles styles;

        /// <summary>
        /// Properties ScriptableObject.
        /// </summary>
        private Properties props;

        /// <summary>
        /// Main presentation engine ScriptableObject.
        /// </summary>
        private Engine engine;

        /// <summary>
        /// Scroll position for slide list.
        /// </summary>
        private float scroll;

        /// <summary>
        /// Whether the window is currently focused.
        /// </summary>
        private bool focused;

#endregion

#region Unity callbacks

        private void OnEnable()
        {
            engine = Engine.Instance;
            props = Properties.Instance;

            engine.SlideChanged += slideChangedHandler; 
            EditorApplication.playmodeStateChanged += playmodeChangeHandler;

            this.minSize = new Vector2(300, 300);
        }

        private void OnFocus()
        {
            // Going into play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) return;
            focused = true;
        }

        private void OnLostFocus()
        {
            // Going into play mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) return;
            focused = false;
        }

        private void OnGUI()
        {
            if (styles == null) styles = new Styles();

            // Get next/previous slide events if the window is focused.
            if (Event.current.type == EventType.KeyUp)
            {
                if (Event.current.keyCode == props.PreviousSlide)
                {
                    engine.PreviousSlide();
                    Event.current.Use();
                }
                else
                if (Event.current.keyCode == props.NextSlide)
                {
                    engine.NextSlide();
                    Event.current.Use();
                }
            }
            else
            {
                var shouldBuild = false;
                var bgcolor = GUI.backgroundColor;
                var deck = engine.SlideDeck;

                EditorGUILayout.BeginHorizontal();
                if (engine.IsPresenting) GUI.enabled = false;
                if (GUILayout.Button(styles.TEXT_NEW, styles.BUTTONS_LEFT))
                {
                    engine.NewDeck();
                }
                if (GUILayout.Button(styles.TEXT_LOAD, styles.BUTTONS_MID))
                {
                    engine.LoadDeck();
                }
                if (GUILayout.Button(styles.TEXT_SAVE, styles.BUTTONS_RIGHT))
                {
                    deck.Save(true);
                }
                if (GUILayout.Button(styles.TEXT_BUILD, styles.BUTTON))
                {
                    shouldBuild = true;
                }
                GUI.enabled = true;

                GUILayout.Label(GUIContent.none, GUILayout.ExpandWidth(true));

                if (engine.IsPresenting) GUI.backgroundColor = styles.SELECTED_COLOR;
                if (!engine.IsPresenting) GUI.enabled = false;
                if (GUILayout.Button(styles.TEXT_PREV, styles.BUTTONS_LEFT))
                {
                    if (!engine.IsBusy) engine.PreviousSlide();
                }
                GUI.enabled = true;
                if (GUILayout.Button(engine.IsPresenting ? styles.TEXT_STOP : styles.TEXT_FROM_BEGINNING, styles.BUTTONS_MID))
                {
                    if (engine.IsPresenting)
                    {
                        engine.StopPresentation();
                    }
                    else
                    {
                        engine.StartPresentation();
                    }
                }
                if (!engine.IsPresenting) GUI.enabled = false;
                if (GUILayout.Button(styles.TEXT_NEXT, styles.BUTTONS_RIGHT))
                {
                    if (!engine.IsBusy) engine.NextSlide();
                }
                GUI.enabled = true;
                GUI.backgroundColor = bgcolor;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Slides list.
                scroll = SlideDeckEditor.DrawInspector(deck, scroll, shouldSelect, itemPlayHandler);

                if (shouldBuild)
                {
                    EditorUtils.BuildPresentation(deck);
                }
            }
        }

#endregion

#region Private functions

        private bool shouldSelect(SlideDeck deck, int index)
        {
            if (engine.SlideDeck != deck) return false;
            if (!engine.IsPresenting) return false;
            return engine.CurrentSlideId == index;
        }

#endregion

#region Event handlers

        private void itemPlayHandler(SlideDeck deck, int index)
        {
            if (engine.SlideDeck != deck) return;
            if (engine.IsPresenting) engine.GotoSlide(index);
            else engine.StartPresentation(index);
        }

        private void slideChangedHandler(object sender, SlideEventArgs e)
        {
            Repaint();
        }

        private void playmodeChangeHandler()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Went out of Play Mode. If we had focus, need to refocus the window.
                if (focused) Focus();
            } 
        }

#endregion

    }
}