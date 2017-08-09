using UnityEngine;

namespace Unity.Presentation.Behaviors
{
    /// <summary>
    /// This component starts the engine in standalone presentation.
    /// </summary>
    public class Loader : MonoBehaviour
    {
        /// <summary>
        /// Link to Properties asset.
        /// </summary>
        public Properties Properties;

        /// <summary>
        /// Link to Slide Deck asset.
        /// </summary>
        public SlideDeck Deck;

        private void Start()
        {
            Engine.Instance.LoadDeck(Deck);
            Engine.Instance.StartPresentation();
        }
    }
}