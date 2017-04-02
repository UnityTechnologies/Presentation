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
	// Presentation configuration window.
	// To open this window go to Window > Presentation in the editor.
	public class PresentationWindow : EditorWindow 
	{

		#region Styles

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

		[MenuItem("Window/Presentation")]
		public static void ShowWindow()
		{
			var wnd = EditorWindow.CreateInstance<PresentationWindow>();
			wnd.titleContent = new GUIContent("Presentation");
			wnd.Show();
		}

		#endregion

		#region Private variables

		// Window styles.
		private static Styles styles;

		// Properties ScriptableObject.
		private Properties props;

		// Main presentation engine ScriptableObject.
		private Engine state;

		// Scroll position for slide list.
		private float scroll;

		// Whether the window is currently focused.
		private bool focused;

		#endregion

		#region Unity callbacks

		private void OnEnable()
		{
			state = Engine.Instance;
			props = Properties.Instance;

			state.SlideChanged += slideChangedHandler; 
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
					state.PreviousSlide();
					Event.current.Use();
				} else if (Event.current.keyCode == props.NextSlide)
				{
					state.NextSlide();
					Event.current.Use();
				}
			} 
			else 
			{
				var shouldBuild = false;
				var bgcolor = GUI.backgroundColor;
				var deck = state.SlideDeck;

				EditorGUILayout.BeginHorizontal();
				if (state.IsPresenting) GUI.enabled = false;
				if (GUILayout.Button(styles.TEXT_NEW, styles.BUTTONS_LEFT))
				{
					state.NewDeck();
				}
				if (GUILayout.Button(styles.TEXT_LOAD, styles.BUTTONS_MID))
				{
					state.LoadDeck();
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

				if (state.IsPresenting) GUI.backgroundColor = styles.SELECTED_COLOR;
				if (!state.IsPresenting) GUI.enabled = false;
				if (GUILayout.Button(styles.TEXT_PREV, styles.BUTTONS_LEFT))
				{
					if (!state.IsBusy) state.PreviousSlide();
				}
				GUI.enabled = true;
				if (GUILayout.Button(state.IsPresenting ? styles.TEXT_STOP : styles.TEXT_FROM_BEGINNING, styles.BUTTONS_MID))
				{
					if (state.IsPresenting)
					{
						state.StopPresentation();
					} else {
						state.StartPresentation();
					}
				}
				if (!state.IsPresenting) GUI.enabled = false;
				if (GUILayout.Button(styles.TEXT_NEXT, styles.BUTTONS_RIGHT))
				{
					if (!state.IsBusy) state.NextSlide();
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
			if (state.SlideDeck != deck) return false;
			if (!state.IsPresenting) return false;
			return state.CurrentSlideId == index;
		}

		#endregion

		#region Event handlers

		private void itemPlayHandler(SlideDeck deck, int index)
		{
			if (state.SlideDeck != deck) return;
			if (state.IsPresenting) state.GotoSlide(index);
			else state.StartPresentation(index);
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