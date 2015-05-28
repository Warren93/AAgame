using UnityEngine;
using System.Collections;

public class EngineScript : MonoBehaviour {

	HPScript parentHP;

	// Use this for initialization
	void Start () {
		parentHP = transform.parent.gameObject.GetComponent<HPScript> ();
	}
	
	void OnCollisionEnter(Collision collision) {
		if (collision.collider.gameObject.tag == "PlayerBullet")
			parentHP.hitpoints -= 4;
	}
}
