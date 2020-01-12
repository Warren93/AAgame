using UnityEngine;
using System.Collections;

public class dummy_player_script : MonoBehaviour {

	Vector3 destination;

	// Use this for initialization
	void Start () {
		destination = transform.position + transform.forward * 100;
		InvokeRepeating ("changeDestination", 1.0f, 3.0f);
	}

	void changeDestination() {
		float horizontal = 300;
		float vertical = 100;
		destination = new Vector3 (Random.Range (-horizontal, horizontal),
		                           Random.Range (vertical, 2 * vertical),
		                           Random.Range (-horizontal, horizontal));
	}

	// Update is called once per frame
	void Update () {
		float speed = 30;
		transform.Translate ((destination - transform.position).normalized * speed * Time.deltaTime);
	}
}
