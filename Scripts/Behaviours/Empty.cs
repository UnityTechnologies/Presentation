using UnityEngine;

namespace Unity.Presentation.Behaviours
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