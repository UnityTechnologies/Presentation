using UnityEngine;

namespace Unity.Presentation.Behaviors
{

	// A script for standalone presentation which starts the engine.
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