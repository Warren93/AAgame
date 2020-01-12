using UnityEngine;
using System.Collections;

public class EnemyMissile : MonoBehaviour {

	float speed = 200;
	float minimumSpeed;
	Transform target;
	Rigidbody rb;
	float fov = 160;
	public int damage = 5;
	public TrailRenderer trail;
	Vector3 vecToTarget = Vector3.zero;
	Vector3 prevRbPosition;
	bool prevRbPositionInitialized = false;
	float distanceTraveled = 0;
	float range = 1200; // was 500

	void Awake () {
		target = GameObject.FindGameObjectWithTag ("Player").transform;
		rb = GetComponent<Rigidbody> ();
	}

	void OnEnable () {
		distanceTraveled = 0;
		prevRbPositionInitialized = false;
		//Debug.Log ("missile activated");
	}
	
	// Update is called once per frame
	/*
	void Update () {
	}*/

	void FixedUpdate () {
		minimumSpeed = speed * 0.5f;
		if (GameManagerScript.gamePaused)
			return;
		if (Vector3.Angle (target.position - rb.position, transform.forward) <= fov) {
			vecToTarget += target.position - rb.position;
		}
		if (vecToTarget.magnitude < minimumSpeed)
			vecToTarget = vecToTarget.normalized * minimumSpeed;
		if (vecToTarget.magnitude > speed)
			vecToTarget = vecToTarget.normalized * speed;
		rb.transform.LookAt (rb.position + vecToTarget, transform.up);
		Vector3 prevPosInLocal = transform.InverseTransformPoint (prevRbPosition);
		transform.RotateAround (transform.position, transform.forward, prevPosInLocal.x * 20);
		rb.MovePosition (rb.position + vecToTarget * Time.deltaTime);

		if (!prevRbPositionInitialized) {
			prevRbPosition = rb.position;
			prevRbPositionInitialized = true;
		}
		distanceTraveled += Vector3.Distance(rb.position, prevRbPosition);
		if (distanceTraveled > range) {
			//Debug.Log ("missile reached max range");
			selfDestruct ();
		}
		prevRbPosition = rb.position;

		//rb.transform.transform.LookAt (target.position);
		//rb.MovePosition ((target.position - rb.position).normalized * speed);
	}

	void OnTriggerEnter(Collider other) {
		if (GameManagerScript.gamePaused)
			return;
		collisionFunction(other);
	}

	void collisionFunction(Collider col) {
		if (GameManagerScript.gamePaused)
			return;

		if (col.gameObject.tag == "Player") {
			col.gameObject.GetComponent<PlayerScript>().hitpoints -= damage;
			PlayerScript.registerHit();
		}
		GameObject hitEffect = ObjectPoolerScript.objectPooler.getHitEffect();
		hitEffect.transform.position = transform.position;
		hitEffect.SetActive(true);
		selfDestruct();
	}


	public void delayedReactivateTrail() {
		Invoke ("reactivateTrail", 0.2f);
	}

	void reactivateTrail() {
		trail.Clear ();
		trail.enabled = true;
	}

	void selfDestruct() {
		GameManagerScript.numActiveEnemyMissiles--;
		CancelInvoke("reactivateTrail");
		trail.enabled = false;
		gameObject.SetActive (false);
		damage = 5; // default
	}
}
