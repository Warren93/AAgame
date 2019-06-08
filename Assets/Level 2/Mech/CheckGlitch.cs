using UnityEngine;
using System.Collections;

public class CheckGlitch : MonoBehaviour {

	float threshold = 100;

	Vector3 prevRigidbodyPos;
	Rigidbody rb;

	Vector3 p1 = Vector3.zero;
	Vector3 p2 = Vector3.zero;

	Color orange;
	Color lineCol;

	bool activated = false;

	// Use this for initialization
	void Start () {
		lineCol = orange;
		rb = GetComponent<Rigidbody> ();
		orange = Color.Lerp (Color.red, Color.yellow, 0.5f);
		Invoke ("activateScript", 0.7f);
	}

	void activateScript() {
		activated = true;
	}

	// Update is called once per frame
	void Update () {
		if (p1 != p2)
			Debug.DrawLine(p1, p2, lineCol);
	}

	void FixedUpdate() {

		if (!activated) {
			prevRigidbodyPos = rb.position;
			return;
		}

		if (Vector3.Distance (rb.position, prevRigidbodyPos) > threshold) {
			p1 = rb.position;
			p2 = prevRigidbodyPos;
			if (GetComponent<NavMeshAgent>().enabled)
				lineCol = Color.magenta;
		}
		prevRigidbodyPos = rb.position;

	}


}
