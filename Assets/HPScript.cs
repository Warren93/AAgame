using UnityEngine;
using System.Collections;

public class HPScript : MonoBehaviour {

	public int hitpoints = 100; // should be set depending on thing that this script is attached to
	public int scoreValue = 1; // the number of points gained from killing whatever this script is attached to
	public bool immediateSelfDestruct = true;

	// Update is called once per frame
	void Update () {
		if (hitpoints <= 0 && immediateSelfDestruct) {
			Destroy(gameObject);
			GameManagerScript.score += scoreValue;
		}
	}
}
