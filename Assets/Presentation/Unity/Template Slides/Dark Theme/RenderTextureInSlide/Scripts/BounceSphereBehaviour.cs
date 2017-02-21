using UnityEngine;
using System.Collections;

public class BounceSphereBehaviour : MonoBehaviour {

	private Rigidbody sphereRigidbody;
	public float upForceAmount;

	void Start(){
		sphereRigidbody = GetComponent<Rigidbody>();
	}

	void Update(){
		if(Input.GetKeyDown("space")){
			sphereRigidbody.AddForce(Vector3.up * upForceAmount);
		}
	}
}
