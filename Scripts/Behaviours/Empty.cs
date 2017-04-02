using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Presentation 
{

	[ExecuteInEditMode]
	public class Empty : MonoBehaviour
	{

		private void Awake()
		{
			var deck = Engine.Instance.SlideDeck;
			if (deck == null) return;
			GetComponent<Camera>().backgroundColor = deck.BackgroundColor;
		}

	}
}