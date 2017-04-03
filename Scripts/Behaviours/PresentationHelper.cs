using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Presentation.Utils;
#endif

namespace Unity.Presentation.Behaviors
{
	// A helper behavior for various presentation functions.
	[ExecuteInEditMode]
	public class PresentationHelper : MonoBehaviour 
	{

		#region Events

		// Previous slide event.
		public event EventHandler Previous;

		// Next slide event.
		public event EventHandler Next;

		// Frame event.
		public event EventHandler Frame;

		#endregion

		#region Public properties/fields.

		[HideInInspector]
		// Previous slide key binding.
		public KeyCode PreviousSlide = KeyCode.LeftArrow;

		[HideInInspector]
		// Next slide key binding.
		public KeyCode NextSlide = KeyCode.RightArrow;

		#endregion

		#region Unity callbacks

		void Update() 
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
		void OnGUI()
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