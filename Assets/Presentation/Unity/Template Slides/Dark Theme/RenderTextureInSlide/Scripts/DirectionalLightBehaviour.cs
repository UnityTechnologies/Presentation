using UnityEngine;
using System.Collections;

public class DirectionalLightBehaviour : MonoBehaviour {

	public void UpdateLightRotation(float sliderValue){
		transform.eulerAngles = new Vector3(50, sliderValue, 0);
	}
}
