using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Unity.Presentation.Utils;

namespace Unity.Presentation.Inspectors
{
    /// <summary>
    /// Custom editor for SlideDeck asset, also used in PresentationWindow.
    /// </summary>
    [CustomEditor(typeof(SlideDeck))]
    public class SlideDeckEditor : Editor
    {

#region Styles

        private class Styles
        {
            public readonly GUIContent TEXT_OPTIONS = new GUIContent("Options");
            public readonly GUIContent TEXT_BG_COLOR = new GUIContent("Background", "Background color of all slides in the presentation.");

            public readonly int ELEMENT_HEIGHT = 20;
            public readonly int SCROLLBAR_WIDTH = 16;

            public readonly Color SELECTED_COLOR = new Color(1, 0, 0);
            public readonly Color INACTIVE_COLOR = new Color(1, 1, 1, .2f);
            public readonly Color INVISIBLE_COLOR = new Color(0, 0, 0, .3f);

            public readonly GUIContent VISIBLE_ICON = new GUIContent(EditorGUIUtility.IconContent("animationvisibilitytoggleon"));
            public readonly GUIContent PLAYMODE_ICON = new GUIContent(EditorGUIUtility.IconContent("PlayButton"));
            public readonly GUIContent PLAY_ICON = new GUIContent(EditorGUIUtility.IconContent("PlayButton"));

            public readonly GUIStyle ICON = new GUIStyle();
            public readonly GUIStyle BG = new GUIStyle("RL Element");
            public readonly GUIStyle BG_INVISIBLE = new GUIStyle("RL Element");
            public readonly GUIStyle BG_SELECTED = new GUIStyle("RL Element");
            public readonly GUIStyle PLAY_BUTTON = new GUIStyle("MiniButton");

            public Styles()
            {
                ICON.fixedWidth = 12;
                ICON.alignment = TextAnchor.MiddleLeft;

                BG_INVISIBLE.normal.background = EditorGUIUtility.LoadRequired("ro_unselected_l") as Texture2D;
                BG_SELECTED.normal.background = EditorGUIUtility.LoadRequired("ro_unselected_l") as Texture2D;

                PLAY_BUTTON.fixedWidth = 20;
                PLAY_BUTTON.fixedHeight = ELEMENT_HEIGHT - 4;

                VISIBLE_ICON.tooltip = "Sets if the slide is visible.";
                PLAYMODE_ICON.tooltip = "Sets if the slide should start in Play Mode.";
                PLAY_ICON.tooltip = "Start Presentation from this slide.";
            }
        }

#endregion

#region Private variables

        private static Styles styles;

        // Reorderable list cache.
        private static Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();
        private SlideDeck instance;
        private float scroll;

#endregion

#region Static methods

        /// <summary>
        /// Draws inspector for a Slide Deck.
        /// </summary>
        /// <param name="deck">Slide Deck to draw.</param>
        /// <param name="scroll">Current vertical scroll value.</param>
        /// <param name="shouldSelect">A function which returns if the current slide should be selected.</param>
        /// <param name="onPlayPress">A function called when "Play" button of a slide is pressed.</param>
        /// <returns>Current vertical scroll value.</returns>
        public static float DrawInspector(SlideDeck deck, float scroll, Func<SlideDeck, int, bool> shouldSelect = null, Action<SlideDeck, int> onPlayPress = null)
        {
            if (styles == null) styles = new Styles();

            ReorderableList list;
            var key = deck.GetInstanceID() + "#" + (shouldSelect != null) + "#" + (onPlayPress != null);
            if (!lists.TryGetValue(key, out list))
            {
                // Init a ReorderableList for the deck and cache it.
                list = new ReorderableList(deck.Slides, typeof(PresentationSlide), true, true, true, true);
                lists.Add(key, list);

                list.onChangedCallback += (l) =>
                {
//					deck.Save();
                };

                list.drawHeaderCallback += (Rect rect) => GUI.Label(rect, deck.IsSavedOnDisk ? deck.Name + ".asset" : "<not saved>");

                list.elementHeightCallback += (int index) => styles.ELEMENT_HEIGHT;

                list.drawElementBackgroundCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (Event.current.type == EventType.repaint)
                    {
                        if (index < 0) return;

                        var bgcolor = GUI.backgroundColor;
                        var slide = deck.Slides[index];

                        if (shouldSelect != null && shouldSelect(deck, index))
                        {
                            GUI.backgroundColor = styles.SELECTED_COLOR;
                            rect.height += 3;
                            styles.BG_SELECTED.Draw(rect, false, isActive, isActive, isFocused);
                        }
                        else
                        if (slide.Visible || isFocused)
                        {
                            styles.BG.Draw(rect, false, isActive, isActive, isFocused);
                        }
                        else
                        {
                            GUI.backgroundColor = styles.INVISIBLE_COLOR;
                            rect.height += 3;
                            styles.BG_INVISIBLE.Draw(rect, false, isActive, isActive, isFocused);
                        }
                        GUI.backgroundColor = bgcolor;
                    }
                };

                list.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var changed = false;
                    var color = GUI.color;
                    var slide = deck.Slides[index];

                    // visible
                    EditorGUI.BeginChangeCheck();
                    if (!slide.Visible) GUI.color = styles.INACTIVE_COLOR;
                    var newVisible = GUI.Toggle(
                                         new Rect(rect.x, rect.y, styles.ICON.fixedWidth, rect.height), slide.Visible, styles.VISIBLE_ICON, styles.ICON);
                    if (EditorGUI.EndChangeCheck())
                    {
                        slide.Visible = newVisible;
                        changed = true;
                    }
                    GUI.color = color;
                    rect.x += styles.ICON.fixedWidth;

                    // play mode
                    EditorGUI.BeginChangeCheck();
                    if (!slide.StartInPlayMode) GUI.color = styles.INACTIVE_COLOR;
                    var newPlaymode = GUI.Toggle(
                                          new Rect(rect.x, rect.y, styles.ICON.fixedWidth, rect.height), slide.StartInPlayMode, styles.PLAYMODE_ICON, styles.ICON);
                    if (EditorGUI.EndChangeCheck())
                    {
                        slide.StartInPlayMode = newPlaymode;
                        changed = true;
                    }
                    GUI.color = color;
                    rect.x += styles.ICON.fixedWidth;

                    // scene
                    var w = rect.width - styles.ICON.fixedWidth * 2;
                    if (onPlayPress != null) w -= styles.PLAY_BUTTON.fixedWidth + 6;
                    var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(slide.ScenePath);
                    EditorGUI.BeginChangeCheck();
                    var newScene = EditorGUI.ObjectField(new Rect(rect.x, rect.y + 2, w, rect.height - 4), scene, typeof(SceneAsset), false) as SceneAsset;
                    if (EditorGUI.EndChangeCheck())
                    {
                        slide.Scene = newScene;
                        changed = true;
                    }
                    rect.x += w + 6;

                    if (onPlayPress != null)
                    {
                        rect.y += 2;
                        if (GUI.Button(rect, styles.PLAY_ICON, styles.PLAY_BUTTON))
                        {
                            onPlayPress(deck, index);
                        }
                    }

                    if (changed) deck.Save();
                };
            }

            scroll = EditorGUILayout.BeginScrollView(new Vector2(0, scroll), false, false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)).y;
            var r = GUILayoutUtility.GetRect(0, list.GetHeight() + 20);
            list.DoList(r);

            var propsName = "presentation_" + deck.name + "_options";
            var showOptions = EditorPrefs.GetBool(propsName, false);
            showOptions = GUIElements.Header(styles.TEXT_OPTIONS, showOptions);
            if (showOptions)
            {
                EditorGUI.indentLevel++;
                deck.BackgroundColor = EditorGUILayout.ColorField(styles.TEXT_BG_COLOR, deck.BackgroundColor, true, false, false, new ColorPickerHDRConfig(0, 1, 0, 1), GUILayout.ExpandWidth(true));
                EditorGUI.indentLevel--;
            }
            EditorPrefs.SetBool(propsName, showOptions);
            EditorGUILayout.EndScrollView();

            return scroll;
        }

#endregion

#region Unity callbacks

        private void OnEnable()
        {
            instance = target as SlideDeck;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Load This Slide Deck"))
            {
                Engine.Instance.LoadDeck(instance);
            }

            scroll = DrawInspector(instance, scroll);
        }

#endregion
    }
}