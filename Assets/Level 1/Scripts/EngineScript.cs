using UnityEngine;
using System.Collections;

public class EngineScript : MonoBehaviour {
	
 	HPScript myHP_Script;
	GameObject smoke;

	// Use this for initialization
	void Start () {
		myHP_Script = GetComponent<HPScript> ();
		smoke = transform.GetChild (0).gameObject;
	}

	void Update () {
		if (myHP_Script.hitpoints <= 0) {
			smoke.SetActive(true);
		}
	}

}
