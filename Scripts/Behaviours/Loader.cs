using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Presentation.Behaviors
{
	public class Loader : MonoBehaviour 
	{
		public Properties Properties;

		private void Start()
		{
			Engine.Instance.StartPresentation();
		}
	}
}