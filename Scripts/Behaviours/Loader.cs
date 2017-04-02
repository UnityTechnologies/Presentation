using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Presentation.Behaviors
{
	public class Loader : MonoBehaviour 
	{
		public Properties Properties;
		public SlideDeck Deck;

		private void Start()
		{
			Engine.Instance.LoadDeck(Deck);
			Engine.Instance.StartPresentation();
		}
	}
}