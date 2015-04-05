using UnityEngine;
using System.Collections;

public class HitEffectScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Invoke ("SelfDestruct", 0.2f);
	}
	
	// Update is called once per frame
	void Update () {

	}

	void SelfDestruct() {
		Destroy (gameObject);
	}
}
