using UnityEngine;
using System.Collections;

public class HPScript : MonoBehaviour {

	public int hitpoints = 100; // should be set depending on thing that this script is attached to
	public int scoreValue = 1; // the number of points gained from killing whatever this script is attached to
	public GameObject smokePrefab;
	public bool immediateSelfDestruct = true;

	// Update is called once per frame
	void Update () {
		if (hitpoints <= 0 && immediateSelfDestruct) {
			if (gameObject.tag == "Enemy Flak")
				Instantiate (smokePrefab, transform.position + Vector3.down * 3, Quaternion.identity);
			Destroy(gameObject);
			GameManagerScript.score += scoreValue;
		}
	}
}
