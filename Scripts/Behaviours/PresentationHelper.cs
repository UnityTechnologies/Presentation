using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Presentation.Utils;
#endif

namespace Unity.Presentation.Behaviors
{
    /// <summary>
    /// Helper behavior instantiated to slide scenes which forwards Game View events to <see cref="Unity.Presentation.Engine"/>.
    /// </summary>
    [ExecuteInEditMode]
    public class PresentationHelper : MonoBehaviour
    {
#region Events

        /// <summary>
        /// Previous slide event.
        /// </summary>
        public event EventHandler Previous;

        /// <summary>
        /// Next slide event.
        /// </summary>
        public event EventHandler Next;

        /// <summary>
        /// Frame event.
        /// </summary>
        public event EventHandler Frame;

#endregion

#region Public properties/fields.

        /// <summary>
        /// Previous slide key binding.
        /// </summary>
        [HideInInspector]
        public KeyCode PreviousSlide = KeyCode.LeftArrow;

        /// <summary>
        /// Next slide key binding.
        /// </summary>
        [HideInInspector]
        public KeyCode NextSlide = KeyCode.RightArrow;

#endregion

#region Unity callbacks

        private void Update()
        {
            if (Frame != null) Frame(this, EventArgs.Empty);

            if (Input.GetKeyUp(PreviousSlide) && Previous != null) Previous(this, EventArgs.Empty);
            else if (Input.GetKeyUp(NextSlide) && Next != null) Next(this, EventArgs.Empty);
            else if (Input.GetKeyUp(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
            {
#if UNITY_EDITOR
                InternalHelper.ToggleGameViewSize();
#endif
            } 
#if !UNITY_EDITOR
			else if (Input.GetKeyUp(KeyCode.Escape))
			{
				Application.Quit();
			}
#endif
        }
			
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (Application.isPlaying) return;
            if (Event.current.type == EventType.KeyUp)
            {
                if (Event.current.keyCode == PreviousSlide && Previous != null) Previous(this, EventArgs.Empty);
                else if (Event.current.keyCode == NextSlide && Next != null) Next(this, EventArgs.Empty);
            }
        }
#endif

        private void OnDestroy()
        {
            Previous = null;
            Next = null;
            Frame = null;
        }

#endregion
    }
}