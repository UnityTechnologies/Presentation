using UnityEngine;

namespace Unity.Presentation.Utils
{
    /// <summary>
    /// GUI styles and elements.
    /// </summary>
    internal static class GUIElements
    {
        public static GUIStyle HeaderStyle;
        public static GUIStyle HeaderCheckbox;
        public static GUIStyle HeaderFoldout;

        static GUIElements()
        {
            HeaderStyle = new GUIStyle("ShurikenModuleTitle") {
                font = (new GUIStyle("Label")).font,
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = 22,
                contentOffset = new Vector2(20f, -2f),
            };

            HeaderCheckbox = new GUIStyle("ShurikenCheckMark");
            HeaderFoldout = new GUIStyle("Foldout");
        }

        /// <summary>
        /// Draws a minimizable foldout header.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="expanded">If the foldout is expanded.</param>
        /// <returns>If the foldout is expanded.</returns>
        public static bool Header(GUIContent title, bool expanded)
        {
            var rect = GUILayoutUtility.GetRect(16f, 22f, HeaderStyle);
            GUI.Box(rect, title, HeaderStyle);

            var foldoutRect = new Rect(rect.x + 4f, rect.y + 3f, 13f, 13f);
            var e = Event.current;

            if (e.type == EventType.Repaint)
            {
                HeaderFoldout.Draw(foldoutRect, false, false, expanded, false);
            }

            if (e.type == EventType.MouseDown)
            {
                if (rect.Contains(e.mousePosition))
                {
                    expanded = !expanded;
                    e.Use();
                }
            }

            return expanded;
        }
    }
}
