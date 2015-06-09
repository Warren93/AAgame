using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerScript : MonoBehaviour {

	bool nocollide = false;
	bool invincible = true;
	public float hitpoints;

	public float currentWeaponRange = 200;

	Vector3 prevDirection;
	Camera mouseLookCam;
	Camera mainCam;

	float mouseLookY_Rotation;
	float mouseLookX_Rotation;
	float mouseLookSensitivity;
	int mlb = 1; // mouse look button index

	public float defaultForwardSpeed;
	public float forwardSpeed;
	public float sidewaysSpeed;

	public float boostCharge;
	float boostDecrement = 25; // was 17, then 21, then 18
	//float boostDecrement = 0.0f;
	float boostIncrement;

	float obstacleDamage = 10;
	float enemyDamage = 5;

	public float mouseX_AxisSensitivity;
	public float mouseY_AxisSensitivity;
	public float rollRate;

	Vector3 vecToMainCam;

	GameObject gameManagerRef;

	public Vector3 newPos;

	bool initialSetup = true;

	public GameObject currentSelectedTarget = null;

	bool allowSwitchToTargetCam = false;

	// Use this for initialization
	void Start () {

		gameManagerRef = GameObject.FindGameObjectWithTag ("GameManager");

		hitpoints = 100.0f;
		boostCharge = 100.0f;

		boostIncrement = boostDecrement * 0.9f; // was * 0.7, then 0.85, then 1.2, 1

		mouseLookY_Rotation = 0;
		mouseLookX_Rotation = 0;
		mouseLookSensitivity = 200;

		mouseLookCam = GameObject.Find("MouseLookCam").GetComponent<Camera>();
		mainCam = GameObject.Find ("Main Camera").GetComponent<Camera>();
		mouseLookCam.backgroundColor = mainCam.backgroundColor;

		vecToMainCam = mainCam.transform.position - transform.position;

		defaultForwardSpeed = 80.0f; // was 40 in thesis, then 80

		sidewaysSpeed = defaultForwardSpeed * 2.0f;
		mouseY_AxisSensitivity = 100.0f;
		mouseX_AxisSensitivity = mouseY_AxisSensitivity * 0.35f;
		rollRate = 90.0f; // was 75

		transform.rotation = Quaternion.identity;
		//Debug.Log ("ORIGINAL rotation is " + transform.rotation.eulerAngles);

		Invoke ("turnOffInitialFreeze", 0.5f);
	}

	void turnOffInitialFreeze() {
		initialSetup = false;
	}

	void allowTargetCam() {
		if (Input.GetKey(KeyCode.F))
			allowSwitchToTargetCam = true;
	}

	// Update is called once per frame
	void Update () {
		//Debug.Log ("rotation is " + transform.rotation.eulerAngles);

		Debug.DrawRay(transform.position, Vector3.up * 50, Color.cyan);

		dampenRigidbodyForces ();
		//Debug.Log ("mouse look cam is at " + mouseLookCam.transform.position);

		if (Input.GetKeyDown(KeyCode.F)) {
			GameObject potentialNewTarget = getTarget();
			if (potentialNewTarget != null)
				currentSelectedTarget = potentialNewTarget;
			CancelInvoke("allowTargetCam");
			Invoke("allowTargetCam", 0.1f);
		}

		if (Input.GetKey (KeyCode.F) && currentSelectedTarget == null && !Input.GetMouseButton (mlb) && mouseLookCam.enabled)
			switchToMainCam();

		if (Input.GetMouseButton (mlb) || (Input.GetKey(KeyCode.F) && currentSelectedTarget != null) && allowSwitchToTargetCam) {
			switchToMouseLookCam();
		}
		else if (Input.GetMouseButtonUp (mlb) || Input.GetKeyUp(KeyCode.F)) {
			switchToMainCam();
			// reset mouselook camera position
			resetMouseLookCamPosition();
			mouseLookY_Rotation = 0;
			mouseLookX_Rotation = 0;
			if (Input.GetKeyUp(KeyCode.F))
				allowSwitchToTargetCam = false;
		}
		// get direction based on mouse movement direction
		float deltaMouseX, deltaMouseY;
		deltaMouseX = Input.GetAxis ("Mouse X");
		deltaMouseY = Input.GetAxis ("Mouse Y");
		// mouse look stuff
		if (Input.GetMouseButton(mlb)) {
			//mouseLookCam.transform.position = mainCam.transform.position;
			//mouseLookCam.transform.rotation = mainCam.transform.rotation;
			mouseLookCam.transform.position = transform.position + (transform.rotation * vecToMainCam);
			mouseLookCam.transform.rotation = transform.rotation;
			mouseLookY_Rotation += Time.deltaTime * deltaMouseX * mouseLookSensitivity;
			mouseLookX_Rotation += Time.deltaTime * deltaMouseY * mouseLookSensitivity;

			//mouseLookX_Rotation = Mathf.Clamp(mouseLookX_Rotation, -5, 5);
			//mouseLookY_Rotation = Mathf.Clamp(mouseLookY_Rotation, -5, 5);

			mouseLookCam.transform.RotateAround(transform.position, transform.right, mouseLookX_Rotation);
			mouseLookCam.transform.RotateAround(transform.position, transform.up, mouseLookY_Rotation);
		}
		else if (Input.GetKey (KeyCode.F)) {
			Vector3 enemyToLookAt = getSelectedEnemyPos();
			//mouseLookCam.transform.position = transform.position + transform.forward * 10;
			//mouseLookCam.transform.LookAt(transform.position, transform.up);
			Vector3 vecFromEnemy = transform.position - enemyToLookAt;
			mouseLookCam.transform.position = transform.position + vecFromEnemy.normalized * vecToMainCam.magnitude; //Vector3.Distance(transform.position, mainCam.transform.position);
			mouseLookCam.transform.position += transform.up * 2;
			mouseLookCam.transform.LookAt(enemyToLookAt);
		}
		// movement stuff
		if((deltaMouseX != 0 || deltaMouseY != 0) && !Input.GetMouseButton(mlb)){
			//Debug.Log("mouse moved");
			transform.RotateAround(transform.position, transform.up, Time.deltaTime * deltaMouseX * mouseX_AxisSensitivity);
			transform.RotateAround(transform.position, transform.right, Time.deltaTime * -1 * deltaMouseY * mouseY_AxisSensitivity);
		}

		forwardSpeed = defaultForwardSpeed;

		/*
		if (GameManagerScript.showWelcomeMsg == true)
			forwardSpeed = 0;
			*/

		//Debug.Log ("forward speed is " + forwardSpeed);

		// accelerate (use boost)
		if (Input.GetKey (KeyCode.LeftShift) && boostCharge > 0) {
			forwardSpeed = defaultForwardSpeed * 2.65f; // was 2.25, then 2.5
			boostCharge -= boostDecrement * Time.deltaTime;
		}
		// decelerate
		if (Input.GetKey (KeyCode.Space))
			forwardSpeed = defaultForwardSpeed * 0.5f;

		// forward movement
		newPos = (transform.TransformDirection (Vector3.forward) * forwardSpeed * Time.deltaTime);

		// sideways strafing
		sidewaysSpeed = forwardSpeed * 2.0f;
		if (Input.GetKey(KeyCode.A))
			newPos += (transform.TransformDirection(Vector3.left) * sidewaysSpeed * Time.deltaTime);
		if (Input.GetKey(KeyCode.D))
			newPos += (transform.TransformDirection(Vector3.right) * sidewaysSpeed * Time.deltaTime);
		if (Input.GetKey(KeyCode.W))
			newPos += (transform.TransformDirection(Vector3.up) * sidewaysSpeed * Time.deltaTime);
		if (Input.GetKey(KeyCode.S))
			newPos += (transform.TransformDirection(Vector3.down) * sidewaysSpeed * Time.deltaTime);

		if (newPos.magnitude > forwardSpeed * Time.deltaTime)
			newPos = Vector3.ClampMagnitude(newPos, forwardSpeed * Time.deltaTime);
		//Debug.Log ("newPos mag is " + newPos.magnitude);
		GetComponent<Rigidbody>().MovePosition (transform.position + newPos);

		// rolling
		Quaternion leftRotation = Quaternion.AngleAxis(rollRate * Time.deltaTime, Vector3.forward);
		Quaternion rightRotation = Quaternion.AngleAxis(-1 * rollRate * Time.deltaTime, Vector3.forward);
		if (Input.GetKey (KeyCode.Q))
			GetComponent<Rigidbody>().MoveRotation (GetComponent<Rigidbody>().rotation * leftRotation);
		if (Input.GetKey (KeyCode.E))
			GetComponent<Rigidbody>().MoveRotation (GetComponent<Rigidbody>().rotation * rightRotation);

		// recharge boost
		if (!Input.GetKey (KeyCode.LeftShift) && boostCharge < 100.0f)
			boostCharge += boostIncrement * Time.deltaTime;
		if (boostCharge > 100.0f)
			boostCharge = 100.0f;

		//Debug.Log ("boost is at " + boostCharge + ", hitpoints at " + hitpoints);

		checkDead ();

		if (initialSetup)
			transform.rotation = Quaternion.identity;

		/*
		string tgName = "NULL";
		if (currentSelectedTarget != null)
			tgName = currentSelectedTarget.name;
		*/
		//Debug.Log ("current target is " + tgName + ", at " + getSelectedEnemyPos () + ", and player is at " + transform.position);
		//Debug.Log ("allowSwitchToTargetCam is set to " + allowSwitchToTargetCam + ", and current target is " + tgName);
	}

	void resetMouseLookCamPosition() {
		mouseLookCam.transform.rotation = mainCam.transform.rotation;
		mouseLookCam.transform.position = mainCam.transform.position;
	}

	void dampenRigidbodyForces() {
		GetComponent<Rigidbody>().velocity = Vector3.zero;
		GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		/*
		float cutoff = 0.000001f;
		if (rigidbody.velocity.magnitude > 0 || rigidbody.angularVelocity.magnitude > 0) {
			//Debug.Log ("collision velocity: " + rigidbody.velocity.magnitude + ", collision angular: " + rigidbody.angularVelocity.magnitude);
			rigidbody.velocity *= 0.99999995f * Time.deltaTime;
			rigidbody.angularVelocity *= 0.99999995f * Time.deltaTime;
			if (rigidbody.velocity.magnitude <= cutoff)
				rigidbody.velocity = Vector3.zero;
			if (rigidbody.angularVelocity.magnitude <= cutoff)
				rigidbody.angularVelocity = Vector3.zero;
		}
		*/
	}

	void switchToMouseLookCam() {
		if (mouseLookCam.enabled)
			return;
		mainCam.GetComponent<CameraScript>().enabled = false;
		mainCam.enabled = false;
		mainCam.GetComponent<AudioListener>().enabled = false;
		mouseLookCam.enabled = true;
		mouseLookCam.GetComponent<AudioListener>().enabled = true;
	}

	void switchToMainCam() {
		mainCam.GetComponent<CameraScript>().enabled = true;
		mainCam.enabled = true;
		mainCam.GetComponent<AudioListener>().enabled = true;
		mouseLookCam.enabled = false;
		mouseLookCam.GetComponent<AudioListener>().enabled = false;
	}

	Vector3 getNearestEnemyPos() {
		Collider[] cols = Physics.OverlapSphere(transform.position, 150);
		List<Collider> relevantCols = new List<Collider> ();
		Vector3 nearest = transform.position;
		foreach (Collider col in cols)
			if (col.gameObject.tag == "Enemy")
				relevantCols.Add(col);
		if (relevantCols.Count <= 0)
			return nearest;
		nearest = relevantCols [0].gameObject.transform.position;
		float distToNearest = Vector3.Distance (transform.position, nearest);
		foreach (Collider col in relevantCols) {
			float distToCurrent = Vector3.Distance(transform.position, col.gameObject.transform.position);
			if (distToCurrent < distToNearest) {
				nearest = col.gameObject.transform.position;
				distToNearest = distToCurrent;
			}
		}
		return nearest;
	}

	GameObject getTarget() {
		RaycastHit[] hits;
		hits = Physics.SphereCastAll(transform.position, 20, transform.forward, currentWeaponRange);
		List<GameObject> relevantObjs = new List<GameObject> ();
		foreach (RaycastHit hit in hits) {
			if (hit.collider.gameObject.GetComponent<HPScript>() != null && hit.collider.gameObject.tag == "Enemy" || hit.collider.gameObject.tag == "Enemy Flak")
				relevantObjs.Add(hit.collider.gameObject);
			if (hit.collider.transform.parent != null && hit.collider.transform.parent.gameObject.tag == "Enemy")
				relevantObjs.Add(hit.collider.transform.parent.gameObject);
		}
		if (relevantObjs.Count <= 0)
			return null;
		GameObject closest = relevantObjs[0];
		float distToClosest = Vector3.Distance(transform.position, closest.transform.position);
		foreach (GameObject obj in relevantObjs) {
			float distToCurrent = Vector3.Distance(transform.position, obj.transform.position);
			if (distToCurrent < distToClosest) {
				closest = obj;
				distToClosest = distToCurrent;
			}
		}
		return closest;
	}

	Vector3 getSelectedEnemyPos() {
		if (currentSelectedTarget)
			return currentSelectedTarget.transform.position;
		else
			return transform.position;
	}

	void OnCollisionEnter(Collision collision) {
		if (nocollide)
			return;
		//Debug.Log ("in collsion function");
		if (collision.collider.tag == "Obstacle" || collision.collider.tag == "Ground")
			hitpoints -= obstacleDamage;
		if (collision.collider.tag == "Enemy" || collision.collider.tag == "Enemy Flak")
			hitpoints -= enemyDamage;
	}


	void checkDead() {
		if (hitpoints <= 0 && !invincible)
			resetGame ();
	}

	void resetGame() {
		Application.LoadLevel(0);
	}
}
