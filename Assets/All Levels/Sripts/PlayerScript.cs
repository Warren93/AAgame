using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerScript : MonoBehaviour {

	Rigidbody myRigidBody;

	bool nocollide = false;
	public bool invincible = false;
	public float hitpoints;
	int healthRegenAmount = 2;
	float healthRegenRate = 2.0f;

	public float currentWeaponRange = 200;

	Vector3 prevDirection;
	Vector3 prevRigidBodyPosition;
	LayerMask groundLayer;

	Camera mouseLookCam;
	Camera mainCam;

	float mouseLookY_Rotation;
	float mouseLookX_Rotation;
	float mouseLookSensitivity;
	float mouseWheelSensitivity;
	public int mlb = 1; // mouse look button index

	public float defaultForwardSpeed;
	public float forwardSpeed;
	public float sidewaysSpeed;

	public float boostCharge;
	float boostDecrement = 25; // was 17, then 21, then 18
	//float boostDecrement = 0.0f;
	float boostIncrement;

	int obstacleDamage = 2;
	int enemyDamage = 5;

	public float mouseX_AxisSensitivity;
	public float mouseY_AxisSensitivity;
	public float rollRate;

	public Vector3 vecToMainCam; // in player frame
	public float cameraDistMult = 1;

	GameObject gameManagerRef;

	public Vector3 newPos;

	bool initialSetup = true;

	public GameObject currentSelectedTarget = null;

	bool allowSwitchToTargetCam = false;

	bool debug_disablePlayer = false;

	void Awake() {
		vecToMainCam = Vector3.up * 2.398773f + Vector3.forward * -7.03497f;
	}

	// Use this for initialization
	void Start () {

		myRigidBody = GetComponent<Rigidbody> ();
		prevRigidBodyPosition = myRigidBody.position;
		groundLayer = 1 << LayerMask.NameToLayer ("Ground");

		gameManagerRef = GameObject.FindGameObjectWithTag ("GameManager");

		hitpoints = 100;
		boostCharge = 100.0f;

		boostIncrement = boostDecrement * 0.9f; // was * 0.7, then 0.85, then 1.2, 1

		mouseLookY_Rotation = 0;
		mouseLookX_Rotation = 0;
		mouseLookSensitivity = 200;
		mouseWheelSensitivity = 10;

		mouseLookCam = GameObject.Find("MouseLookCam").GetComponent<Camera>(); // this is also the target cam
		mainCam = GameObject.Find ("Main Camera").GetComponent<Camera>();
		mouseLookCam.backgroundColor = mainCam.backgroundColor;

		//vecToMainCam = transform.rotation * (mainCam.transform.position - transform.position);

		mainCam.transform.position = transform.position + transform.TransformVector(vecToMainCam);
		mainCam.transform.rotation = transform.rotation;
		//Debug.Log ((mainCam.transform.position - transform.position).x + ", " + (mainCam.transform.position - transform.position).y + ", " + (mainCam.transform.position - transform.position).z);

		defaultForwardSpeed = 80.0f; // was 40 in thesis, then 80

		sidewaysSpeed = defaultForwardSpeed * 2.0f;
		mouseY_AxisSensitivity = 200.0f; // was 100
		mouseX_AxisSensitivity = mouseY_AxisSensitivity * 0.35f;
		rollRate = 90.0f; // was 75

		//transform.rotation = Quaternion.identity;

		Invoke ("turnOffInitialFreeze", 0.5f);
		InvokeRepeating ("healthRegen", healthRegenRate, healthRegenRate);
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

		if (debug_disablePlayer) {
			mouseLookCam.enabled = false;
			mainCam.enabled = false;
			return;
		}

		if (GameManagerScript.gamePaused)
			return;

		//Debug.DrawRay (transform.position, Vector3.up * 50, Color.cyan);

		dampenRigidbodyForces ();

		cameraDistMult -= Input.GetAxis ("Mouse ScrollWheel") * mouseWheelSensitivity;
		if (cameraDistMult < 1)
			cameraDistMult = 1;

		// lock onto new target if player presses F
		if (Input.GetKeyDown (KeyCode.F)) {
			GameObject potentialNewTarget = getTarget ();
			if (potentialNewTarget != null)
					currentSelectedTarget = potentialNewTarget;
			// delay switching to the target cam to avoid jarring, instant switch
			CancelInvoke ("allowTargetCam");
			Invoke ("allowTargetCam", 0.1f);
		}

		// CORNER(ish) CASE
		// if player has pressed the target cam button (F) but a target has not been selected,
		// and if the target/mouselook camera is still enabled despite the player NOT also pressing the mouselook button (abbrev. 'mlb')
		// switch to the main (follow) cam
		if (Input.GetKey (KeyCode.F) && currentSelectedTarget == null && !Input.GetMouseButton (mlb) && mouseLookCam.enabled)
			switchToMainCam();

		// if the player is pressing the mouselook button,
		// or if they're pressing the target cam button but there's no target
		// ...and if the delay to switch to the target cam has worn off...
		// then switch to the target/mouselook cam
		if (Input.GetMouseButton (mlb) || (Input.GetKey(KeyCode.F) && currentSelectedTarget != null) && allowSwitchToTargetCam) {
			switchToMouseLookCam();
		}
		else if (Input.GetMouseButtonUp (mlb) || Input.GetKeyUp(KeyCode.F)) { // if the player isn't pressing either the target or mouselook cam
			switchToMainCam();												  // buttons, then switch back to the main cam
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
			mouseLookCam.transform.position = transform.position + transform.TransformVector(vecToMainCam) * cameraDistMult;
			mouseLookCam.transform.rotation = transform.rotation;
			mouseLookY_Rotation += Time.deltaTime * deltaMouseX * mouseLookSensitivity;
			mouseLookX_Rotation += Time.deltaTime * deltaMouseY * mouseLookSensitivity;

			mouseLookCam.transform.RotateAround(transform.position, transform.right, mouseLookX_Rotation);
			mouseLookCam.transform.RotateAround(transform.position, transform.up, mouseLookY_Rotation);
		}
		else if (Input.GetKey (KeyCode.F)) {
			Vector3 enemyToLookAt = getSelectedEnemyPos();
			Vector3 vecFromEnemy = transform.position - enemyToLookAt;
			mouseLookCam.transform.position = transform.position + vecFromEnemy.normalized * vecToMainCam.magnitude * cameraDistMult;
			mouseLookCam.transform.position += transform.up * 2;
			mouseLookCam.transform.LookAt(enemyToLookAt);
		}
		// movement stuff
		if((deltaMouseX != 0 || deltaMouseY != 0) && !Input.GetMouseButton(mlb)){
			transform.RotateAround(transform.position, transform.up, Time.deltaTime * deltaMouseX * mouseX_AxisSensitivity);
			transform.RotateAround(transform.position, transform.right, Time.deltaTime * -1 * deltaMouseY * mouseY_AxisSensitivity);
		}

		forwardSpeed = defaultForwardSpeed;

		// accelerate (use boost)
		if (Input.GetKey (KeyCode.LeftShift) && boostCharge > 0) {
			forwardSpeed = defaultForwardSpeed * 2.65f; // was 2.25, then 2.5
			boostCharge -= boostDecrement * Time.deltaTime;
		}
		// decelerate
		if (Input.GetKey (KeyCode.Space))
			forwardSpeed = defaultForwardSpeed * 0.5f;

		//moveShip ();

		// recharge boost
		if (!Input.GetKey (KeyCode.LeftShift) && boostCharge < 100.0f)
			boostCharge += boostIncrement * Time.deltaTime;
		if (boostCharge > 100.0f)
			boostCharge = 100.0f;

		checkDead ();
		/*
		if (initialSetup)
			transform.rotation = Quaternion.identity;
			*/
	}

	void FixedUpdate() {

		if (debug_disablePlayer)
			return;

		if (GameManagerScript.gamePaused)
			return;

		moveShip ();
		preventGlitchingThroughGround ();
		//Debug.Log ("newPos mag is " + newPos.magnitude);
	}

	void moveShip() {
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

		myRigidBody.MovePosition (transform.position + newPos);
		//myRigidBody.AddForce(newPos, ForceMode.VelocityChange);

		// rolling
		Quaternion leftRotation = Quaternion.AngleAxis(rollRate * Time.deltaTime, Vector3.forward);
		Quaternion rightRotation = Quaternion.AngleAxis(-1 * rollRate * Time.deltaTime, Vector3.forward);
		if (Input.GetKey (KeyCode.Q))
			myRigidBody.MoveRotation (myRigidBody.rotation * leftRotation);
		if (Input.GetKey (KeyCode.E))
			myRigidBody.MoveRotation (myRigidBody.rotation * rightRotation);
	}

	void resetMouseLookCamPosition() {
		mouseLookCam.transform.rotation = mainCam.transform.rotation;
		mouseLookCam.transform.position = mainCam.transform.position;
	}

	void dampenRigidbodyForces() {
		myRigidBody.velocity = Vector3.zero;
		myRigidBody.angularVelocity = Vector3.zero;
	}

	// TODO: these camera functions need to be reworked now that there are multiple passes/frustums for each
	public void switchToMouseLookCam() {
		if (mouseLookCam.enabled)
			return;
		mainCam.GetComponent<CameraScript>().enabled = false;
		mainCam.enabled = false;
		mainCam.GetComponent<AudioListener>().enabled = false;
		if (mainCam.transform.childCount == 1)
			mainCam.transform.GetChild (0).gameObject.SetActive (false);
		mouseLookCam.enabled = true;
		mouseLookCam.GetComponent<AudioListener>().enabled = true;
		if (mouseLookCam.transform.childCount == 1)
			mouseLookCam.transform.GetChild (0).gameObject.SetActive (true);
	}

	public void switchToMainCam() {
		mainCam.GetComponent<CameraScript>().enabled = true;
		mainCam.enabled = true;
		mainCam.GetComponent<AudioListener>().enabled = true;
		if (mainCam.transform.childCount == 1)
			mainCam.transform.GetChild (0).gameObject.SetActive (true);
		mouseLookCam.enabled = false;
		mouseLookCam.GetComponent<AudioListener>().enabled = false;
		if (mouseLookCam.transform.childCount == 1)
			mouseLookCam.transform.GetChild (0).gameObject.SetActive (false);
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
			if (hit.collider.transform.root.gameObject.tag == "Enemy")
				relevantObjs.Add(hit.collider.transform.root.gameObject);
			if (hit.collider.gameObject.GetComponent<HPScript>() != null && (hit.collider.gameObject.tag == "Enemy" || hit.collider.gameObject.tag == "Enemy Flak"))
				relevantObjs.Add(hit.collider.gameObject);
			if (hit.collider.transform.parent != null && (hit.collider.transform.parent.gameObject.tag == "Enemy" || hit.collider.transform.parent.gameObject.tag == "AirBoss"))
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
		if (collision.collider.tag == "Obstacle" || collision.collider.tag == "Ground") {
			hitpoints -= obstacleDamage;
			registerHit();
		}
		if (collision.collider.tag == "Enemy" || collision.collider.tag == "Enemy Flak") {
			hitpoints -= enemyDamage;
			registerHit();
		}
	}

	public static void registerHit() {
		GameManagerScript.damageIndicatorColor = Color.red;
		GameManagerScript.damageIndicatorColorStep = 0;
	}

	void healthRegen() {
		if (hitpoints > 0 && hitpoints < 100) {
			if (hitpoints + healthRegenAmount > 100)
				hitpoints += (100 - hitpoints);
			else
				hitpoints += healthRegenAmount;
		}
	}

	void checkDead() {
		if (hitpoints <= 0 && !invincible)
			resetGame ();
	}


	void preventGlitchingThroughGround() {
		float bounceBack = 10.0f;
		RaycastHit hitInfo;
		Vector3 vecFromLastPos = myRigidBody.position - prevRigidBodyPosition;
		if (/*Input.GetKey(KeyCode.LeftShift)
			&&*/ Physics.Raycast(myRigidBody.position, vecFromLastPos, out hitInfo, vecFromLastPos.magnitude, groundLayer)
		    //&& Vector3.Angle(-transform.forward, hitInfo.normal) <= 60.0f
		    ) {
			//Debug.Log("HIT");
			//transform.position = hitInfo.point - vecFromLastPos * 5;
			transform.position += hitInfo.normal * bounceBack;
			hitpoints -= obstacleDamage;
			registerHit();
			//return;
		}
		prevRigidBodyPosition = myRigidBody.position;
	}


	void resetGame() {
		Application.LoadLevel(Application.loadedLevel);
	}
}
