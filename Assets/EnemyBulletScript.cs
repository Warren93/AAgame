﻿using UnityEngine;
using System.Collections;

public class EnemyBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	public float distanceTraveled;
	public int damage = 5;
	Rigidbody myRigidbody = null;
	Transform myTransform = null;
	TrailRenderer trail = null;
	bool curveTowardPointEnabled = false;
	Vector3 targetPoint;
	Vector3 originalFwdVec;
	float angleStep;
	float currentStep;

	void Awake() {
		myRigidbody = GetComponent<Rigidbody>();
		myTransform = transform;
		trail = GetComponent<TrailRenderer> ();
		myRigidbody.solverIterationCount = 4;
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

	// Update is called once per frame
	void Update () {

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
		//Debug.DrawRay(transform.position, transform.forward * 50, Color.cyan);
	}

	void FixedUpdate() {
		move ();
	}
	
	void move() {
		//Vector3 oldWorldPos = transform.position;
		Vector3 newPos = myTransform.forward * speed;
		myRigidbody.MovePosition(transform.position + (newPos * Time.deltaTime));
		distanceTraveled += speed * Time.deltaTime * 0.8f;
		//Vector3 newWorldPos = transform.position + (newPos * Time.deltaTime);
		//distanceTraveled += Mathf.Abs(newWorldPos.magnitude - oldWorldPos.magnitude);
	}

	/*
	void OnCollisionEnter(Collision collision) {
		collisionFunction(collision.collider);
	}
	*/

	void OnTriggerEnter(Collider other) {
		collisionFunction(other);
	}

	void collisionFunction(Collider col) {
		if (col.gameObject.tag != "Enemy") {
			if (col.gameObject.tag == "Player") {
					col.gameObject.GetComponent<PlayerScript>().hitpoints -= damage;
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
		trail.enabled = true;
	}

	void selfDestruct() {
		CancelInvoke("reactivateTrail");
		trail.enabled = false;
		GameManagerScript.numActiveBullets--;
		gameObject.SetActive (false);
		// set back to default value
		curveTowardPointEnabled = false;
		damage = 5; // default
	}
}