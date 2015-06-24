using UnityEngine;
using System.Collections;

public class PlayerBulletScript : MonoBehaviour {

	MeshRenderer myMeshRenderer;
	LineRenderer myLineRenderer;
	Rigidbody myRigidBody;
	Transform playerTransform;
	public float speed;
	public float maxRange;
	public float distanceTraveled;
	public LayerMask relevantLayers;

	void Awake() {
		myMeshRenderer = GetComponent<MeshRenderer> ();
		myLineRenderer = GetComponent<LineRenderer> ();
		myRigidBody = GetComponent<Rigidbody> ();
	}

	void Start() {
		playerTransform = GameObject.FindGameObjectWithTag ("Player").transform;
	}

	// Update is called once per frame
	void Update () {
		
		if (!myMeshRenderer.enabled && Vector3.Distance(playerTransform.position, transform.position) > 3)
			myMeshRenderer.enabled = true;
		if (!myLineRenderer.enabled && Vector3.Distance(playerTransform.position, transform.position) > 3.1f)
			myLineRenderer.enabled = true;

		if (distanceTraveled >= maxRange)
			selfDestruct();
	}

	void FixedUpdate() {
		//Debug.Log ("speed is " + speed);
		Vector3 oldWorldPos = transform.position;
		Vector3 newPos = transform.forward * speed;
		myRigidBody.MovePosition(transform.position + (newPos * Time.deltaTime));
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
			GameObject hitEffect = ObjectPoolerScript.objectPooler.getHitEffect();
			hitEffect.transform.position = transform.position;
			hitEffect.SetActive(true);
			selfDestruct();
			if (otherHP != null && otherHP.hitpoints > 0) {
				otherHP.hitpoints -= 1;
			}
			else if (col.transform.parent != null && otherParentHP != null && otherParentHP.hitpoints > 0) {
				otherParentHP.hitpoints -= 1;
			}
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
		myMeshRenderer.enabled = false;
		myLineRenderer.enabled = false;
		gameObject.SetActive (false);
	}
}
