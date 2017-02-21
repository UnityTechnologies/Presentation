using UnityEngine;
using System.Collections;
using System;

namespace Unity.Presentation 
{
	/// <summary>
	/// A helper behavior for various presentation functions.
	/// </summary>
	public class PresentationHelper : MonoBehaviour 
	{

		public event EventHandler Previous;
		public event EventHandler Next;

		[HideInInspector]
		public KeyCode PreviousSlide = KeyCode.LeftArrow;
		[HideInInspector]
		public KeyCode NextSlide = KeyCode.RightArrow;

		void Update() 
		{
			if (Input.GetKeyUp(PreviousSlide) && Previous != null) Previous(this, EventArgs.Empty);
			else if (Input.GetKeyUp(NextSlide) && Next != null) Next(this, EventArgs.Empty);
			else if (Input.GetKeyUp(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
			{
#if UNITY_EDITOR
				Utils.ToggleGameViewSize();
#endif
			}
		}
	}
}