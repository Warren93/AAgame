using UnityEngine;
using System.Collections;

public class EnemyBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	public float distanceTraveled;
	public int damage = 5;
	public Rigidbody bulletRigidbody = null;
	Transform myTransform = null;
	TrailRenderer trail = null;
	bool curveTowardPointEnabled = false;
	Vector3 targetPoint;
	Vector3 originalFwdVec;
	float angleStep;
	float currentStep;

	void Awake() {
		bulletRigidbody = GetComponent<Rigidbody>();
		myTransform = transform;
		trail = GetComponent<TrailRenderer> ();
		bulletRigidbody.solverIterations = 4;
	}

	/*
	void OnEnable() {
		damage = 5; // default
	}
	*/

	public void curveTowardPoint(Vector3 point, float step) {
		targetPoint = point;
		angleStep = step;
		currentStep = 0;
		originalFwdVec = transform.forward;
		curveTowardPointEnabled = true;
	}

	public void setRigidbodyCollisionMode(CollisionDetectionMode mode) {
		bulletRigidbody.collisionDetectionMode = mode;
	}

	// Update is called once per frame
	void Update () {

		if (GameManagerScript.gamePaused)
			return;

		if (Time.deltaTime > GameManagerScript.antiLagThreshold) {
			selfDestruct();
			return;
		}

		if (curveTowardPointEnabled) {
			if (currentStep < 1)
				currentStep += angleStep * Time.deltaTime;
			myTransform.LookAt(myTransform.position + Vector3.Slerp(originalFwdVec, targetPoint - myTransform.position, currentStep));
			if (Vector3.Distance(myTransform.position, targetPoint) <= speed * 2.5f * Time.deltaTime)
				curveTowardPointEnabled = false;
		}


		if (distanceTraveled >= maxRange)
			selfDestruct();
	}

	void FixedUpdate() {

		if (GameManagerScript.gamePaused)
			return;

	    move ();
        
	}
	
	void move() {
		Vector3 newPos = myTransform.forward * speed;
		bulletRigidbody.MovePosition(transform.position + (newPos * Time.deltaTime));
		distanceTraveled += speed * Time.deltaTime * 0.8f;
	}

    void OnTriggerEnter(Collider other) {
		if (GameManagerScript.gamePaused)
			return;
		collisionFunction(other);
	}

	void collisionFunction(Collider col) {
		if (GameManagerScript.gamePaused)
			return;
		if (col.gameObject.tag != "Enemy") {
			if (col.gameObject.tag == "Player") {
				col.gameObject.GetComponent<PlayerScript>().hitpoints -= damage;
				PlayerScript.registerHit();
			}
			GameObject hitEffect = ObjectPoolerScript.objectPooler.getHitEffect();
			hitEffect.transform.position = transform.position;
			hitEffect.SetActive(true);
			selfDestruct();
		}
	}
	
	public void delayedReactivateTrail() {
		Invoke ("reactivateTrail", 0.2f);
	}

	void reactivateTrail() {
		if (trail != null)
			trail.enabled = true;
	}

	void selfDestruct() {
		CancelInvoke("reactivateTrail");
		if (trail != null)
			trail.enabled = false;
		GameManagerScript.numActiveBullets--;
		gameObject.SetActive (false);
		// set back to default value
		curveTowardPointEnabled = false;
		damage = 5; // default
	}
}