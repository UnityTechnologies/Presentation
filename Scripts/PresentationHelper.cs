using UnityEngine;
using System.Collections;
using System;

namespace Unity.Presentation 
{
	/// <summary>
	/// A helper behavior for various presentation functions.
	/// </summary>
	[ExecuteInEditMode]
	public class PresentationHelper : MonoBehaviour 
	{

		public event EventHandler Previous;
		public event EventHandler Next;
		public event EventHandler Frame;

		[HideInInspector]
		public KeyCode PreviousSlide = KeyCode.LeftArrow;
		[HideInInspector]
		public KeyCode NextSlide = KeyCode.RightArrow;

		void Update() 
		{
			if (Frame != null) Frame(this, EventArgs.Empty);

			if (Input.GetKeyUp(PreviousSlide) && Previous != null) Previous(this, EventArgs.Empty);
			else if (Input.GetKeyUp(NextSlide) && Next != null) Next(this, EventArgs.Empty);
			else if (Input.GetKeyUp(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
			{
#if UNITY_EDITOR
				Utils.ToggleGameViewSize();
#endif
			}
		}
			
#if UNITY_EDITOR
		void OnGUI()
		{
			if (Application.isPlaying) return;
			if (Event.current.type == EventType.KeyUp)
			{
				if (Event.current.keyCode == PreviousSlide) Debug.Log("PREV");
				else if (Event.current.keyCode == NextSlide) Debug.Log("NEXT");
			}
		}
#endif

		private void OnDestroy()
		{
			Previous = null;
			Next = null;
			Frame = null;
		}
	}
}