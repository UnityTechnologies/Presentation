using UnityEngine;
using System.Collections;

public class CubeBehaviour : MonoBehaviour {

	private bool isSpinning = true;
	private float spinSpeed = 5;

	public void spinToggle(bool toggleValue){
		isSpinning = toggleValue;
	}

	void Update(){
		if(isSpinning){
			transform.Rotate(transform.up * spinSpeed * Time.deltaTime);
		}
	}

	public void UpdateSpinSpeed(float sliderValue){
		spinSpeed = sliderValue;
	}

}
