using UnityEngine;
using System.Collections;

public class PlayerBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	public float distanceTraveled;
	public GameObject HitEffectPrefab;
	public LayerMask relevantLayers;

	// Update is called once per frame
	void Update () {
		if (distanceTraveled >= maxRange)
			selfDestruct();
	}

	void FixedUpdate() {
		//Debug.Log ("speed is " + speed);
		Vector3 oldWorldPos = transform.position;
		Vector3 newPos = transform.forward * speed;
		rigidbody.MovePosition(transform.position + (newPos * Time.deltaTime));
		Vector3 newWorldPos = transform.position + (newPos * Time.deltaTime);
		distanceTraveled += Mathf.Abs(newWorldPos.magnitude - oldWorldPos.magnitude);

		//look ahead to see if going to hit something, because collision detection is apparently spotty otherwise...
		RaycastHit hitInfo;
		bool didHit = Physics.Raycast (transform.position, transform.forward,
		                               out hitInfo,
		                               speed * Time.deltaTime * 0.5f, relevantLayers);
		if (didHit)
			collisionFunction(hitInfo.collider);
	}

	void OnCollisionEnter(Collision collision) {
		collisionFunction(collision.collider);
	}

	void collisionFunction(Collider col) {
		HPScript otherHP = col.gameObject.GetComponent<HPScript> ();
		HPScript otherParentHP = null;
		if (col.transform.parent != null)
			otherParentHP = col.gameObject.transform.parent.GetComponent<HPScript> ();
		if (col.gameObject.tag != "Player") {
			if (otherHP != null) {
				otherHP.hitpoints -= 1;
			}
			else if (col.transform.parent != null) {
				otherParentHP.hitpoints -= 1;
			}
			Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
			selfDestruct();
		}
	}

	/*
	public void delayedReactivateTrail() {
		Invoke ("reactivateTrail", 0.2f);
	}
	
	void reactivateTrail() {
		gameObject.GetComponent<TrailRenderer> ().enabled = true;
	}
	*/

	void selfDestruct() {
		//CancelInvoke("reactivateTrail");
		//gameObject.GetComponent<TrailRenderer> ().enabled = false;
		gameObject.SetActive (false);
	}
}
