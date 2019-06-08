using UnityEngine;
using System.Collections;

public class MechScript2 : MonoBehaviour {

	/*TODO:
	 * - Prevent mechs from going through ground when player goes above airModeCutoff
	 * - Change airModeCutoffAlt to something lower
	 * - Maybe don't enter air mode if target's y position in mech frame is less than airModeCutoffAlt
	 * - Maybe see if you can get off-mesh links working
	 * (have mech transform to air stance and prevent onGround from being true when agent.isOnOffMeshLink is true)
	 * - get mech weapons and shooting working
	 * - get 'wander' state working
	 * - test with multiple mechs (only way to test local agent avoidance)
	 * */

	GameObject player;
	Vector3 target;
	Vector3 wanderDestination;
	Vector3 wanderTarget;
	float wanderTargetSpeed = 120f; // was 80
	PlayerScript playerInfo;
	Animator anim;
	Rigidbody rb;
	Transform spine;
	Transform chest;
	float chestHeight;

	Transform L_footThrust1, L_footThrust2, L_shinThrust1, L_shinthrust2, R_footThrust1, R_footthrust2, R_shinThrust1, R_shinthrust2;

	const int ATTACK = 0;
	const int WANDER_AIR = 1;
	const int WANDER_GROUND = 2;
	public int state = ATTACK;
	float maxWanderSpeed = 80f; // was 100

	bool onGround = false;
    bool groundAirTransitionAllowed = true;
    GameObject terrainObj = null;
	GameObject terrain;

	float defaultRunSpeed = 12f; // was 1.2 for run anim speed of 0.1
	float runSpeedStep = 0f;
	float runSpeedStepIncrement = 3f;

	float airSpeedIncrement = 3f;
	float airTurnRate = 50f;
	float descentRate = 0.05f;
	float defaultAirSpeed = 20f; // was 20
	float airSpeedWeight = 1.2f;

	float planeModeCutoffSpeed1 = 1f; // was 25, then 23, then 18, then 5
	float planeModeCutoffSpeed2 = 25f; // was 27
	float defaultPlaneSpeed = 30f;

	float currentSpeed = 0f;
	float attackDistance = 180f;

	Vector3 groundAimVector;
	bool facingPlayer = false;
	bool planeMode = false;

	bool descentMode = false;

	float groundTurnRate = 50f; // was 50
	Vector3 groundNormal = Vector3.up;
	float prevGroundFacingAngle = 180f;
	Quaternion savedGroundRot;
	float facingAngle = 45f; // was 60
	float fov = 60f;
	float sightRange = 300f;

	float myAlt;
	float targetAlt;
	float airModeCutoffAlt = 65f; // was 200 (not multiplied by localScale.x), was then 33 (multiplied by localScale.x)

	NavMeshAgent agent;
	Vector3 obstacleAvoidanceVector = Vector3.zero;
	Vector3 allyAvoidanceVector = Vector3.zero;

	public string navAgentStatusString;

	// Use this for initialization
	void Start () {
		terrain = GameObject.Find ("Terrain");
		agent = GetComponent<NavMeshAgent> ();
		agent.stoppingDistance = attackDistance;
		agent.avoidancePriority = Random.Range (0, 99);
		agent.autoTraverseOffMeshLink = false;

		savedGroundRot = Quaternion.identity;

		defaultRunSpeed *= transform.localScale.x;
		descentRate *= transform.localScale.x;
		defaultAirSpeed *= transform.localScale.x;
		planeModeCutoffSpeed1 *= transform.localScale.x;
		planeModeCutoffSpeed2 *= transform.localScale.x;
		defaultPlaneSpeed *= transform.localScale.x;
		airModeCutoffAlt *= transform.localScale.x;

		//player = GameObject.Find ("dummy player");
		player = GameObject.FindGameObjectWithTag ("Player");
		target = player.transform.position;
		playerInfo = player.GetComponent<PlayerScript> ();

		anim = GetComponentInChildren<Animator> ();
		rb = GetComponent<Rigidbody> ();
		foreach (Transform child in transform.GetChild(0)) {
			if (child.name == "metarig") {
				spine = child.GetChild(0).GetChild(2);
				break;
			}
		}
		chest = spine.GetChild (0);
		/* the following foreach loop only works of "optimize game objects" is checked in mech rig tab (but this messes up LateUpdate overrides
		foreach (Transform child in transform.GetChild(0)) {
			if (child.name == "chest")
				chest = child;
			else if (child.name == "spine")
				spine = child;
		}
		*/
		chestHeight = Vector3.Distance (chest.transform.position, transform.position);

		rb = GetComponent<Rigidbody> ();

		foreach (Transform child in transform.GetChild(0)) {
			if (child.name == "metarig") {
				L_footThrust1 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1);
				L_footThrust2 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(2);
				L_shinThrust1 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(2);
				L_shinthrust2 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(3);
			}
		}
		foreach (Transform child in transform.GetChild(0)) {
			if (child.name == "metarig") {
				R_footThrust1 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(1);
				R_footthrust2 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(2);
				R_shinThrust1 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(2);
				R_shinthrust2 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
			}
		}

		//Debug.Log ("spine is " + spine + ", and chest is " + chest);
		//Debug.Log ("chest height is " + chestHeight);
		//Invoke ("changeSpeed", 2.0f);

		InvokeRepeating ("updateObstacleAvoidanceVector", Random.Range (0.1f, 1.3f), 0.2f); // repeat rate was 0.5f
		InvokeRepeating ("updateAllyAvoidanceVector", Random.Range (0.1f, 1.3f), 0.2f);
	}

	//void changeSpeed() {
	//	currentSpeed = 12;
	//}

	void changeStateTo(int newState) {
		if (newState == state)
			return;
		state = newState;
		if (state == WANDER_AIR || state == WANDER_GROUND) {
			float spread = 30f;
			wanderTarget = transform.position + transform.forward * 100 + new Vector3(Random.Range(-spread, spread),
			                                                                          Random.Range(-spread, spread),
			                                                                          Random.Range(-spread, spread));
			InvokeRepeating("changeWanderDestination", 0.1f, 3);
		}
		else if (state == ATTACK)
			CancelInvoke("changeWanderDestination");
	}

	void changeWanderDestination() {
		float raycastOriginAlt = 10000;
		float radius = 600; // was 300
		// get random point in sky within radius of player
		Vector3 randomPoint = player.transform.position
						+ new Vector3 (Random.Range (-radius, radius),
			              Random.Range (-radius, radius),
			              Random.Range (-radius, radius));
		if (state == WANDER_AIR)
			wanderDestination = randomPoint;
		else if (state == WANDER_GROUND) {
			// raycast from some point in the sky at a set altitude to get a random point on the ground within radius of player
			Vector3 raycastOrigin = randomPoint;
			raycastOrigin.y = raycastOriginAlt;
			RaycastHit hitInfo;
			bool didHit = Physics.Raycast (raycastOrigin, Vector3.down, out hitInfo, Mathf.Infinity,
			                               1 << LayerMask.NameToLayer ("Default")
			                               | 1 << LayerMask.NameToLayer ("Ground")
			                               | 1 << LayerMask.NameToLayer ("Obstacle"));
			if (didHit) // if raycast hit ground, set wander destination to random point 10 meters above ground within radius of player
				wanderDestination = hitInfo.point + hitInfo.normal.normalized * 10; 
			else // otherwise set it to random point in the air within radius of player
				wanderDestination = randomPoint;
		}

	}

	float getAltitude(Vector3 pos) {
		RaycastHit hitInfo;
		bool didHit = Physics.Raycast(pos, transform.up * -1, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer ("Ground"));
		if (didHit) {
			return Vector3.Distance(hitInfo.point, pos);
			terrainObj = hitInfo.collider.gameObject;
		}
		else {
			if(Physics.Raycast(pos, transform.up, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer ("Ground")))
				terrainObj = hitInfo.collider.gameObject;
				return -1 * Vector3.Distance(hitInfo.point, pos);
		}
		return Mathf.Infinity;
	}

	float getAltitudeWithOffset (Vector3 pos, Vector3 offsetDirection, float offset) {
		float alt = getAltitude (pos + offsetDirection * offset);
		if (alt == Mathf.Infinity) {
			alt = getAltitude (pos);
			return alt;
		}
		return alt - offset;
	}

	Vector3 getGroundNormal() {
		RaycastHit hitInfo;
		bool didHit = Physics.Raycast(transform.position + transform.up * 20, transform.up * -1, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer ("Ground"));
		if (didHit)
			return hitInfo.normal;
		//Debug.Log("returning up vec as ground normal " + Random.Range(0, 100));
		return transform.up;
	}

	void checkFacingPlayer() {
		if (Mathf.Abs(Vector3.Angle(player.transform.position - transform.position, transform.forward)) <= facingAngle)
			facingPlayer = true;
		else
			facingPlayer = false;
	}
	bool playerIsInSight() {
		if (Mathf.Abs(Vector3.Angle(player.transform.position - transform.position, transform.forward)) <= fov
		    && Vector3.Distance(player.transform.position, transform.position) <= sightRange)
			return true;
		else
			return false;
	}

	// Update is called once per frame
	void Update () {

		if (agent.enabled && !agent.isOnNavMesh)
			Debug.Log("agent active but not on navmesh");

		if (agent.enabled)
			Debug.DrawLine (transform.position, agent.destination, Color.red);

		if (state != ATTACK)
			Debug.DrawLine (transform.position, wanderTarget, Color.blue);

		dampenRigidbodyForces ();

		bool playerInSight = playerIsInSight ();

		wanderTarget += (wanderDestination - wanderTarget).normalized * wanderTargetSpeed * Time.deltaTime;

        state = ATTACK;
        /*
		if (playerInSight && state != ATTACK)
			changeStateTo(ATTACK);
		else if (!playerInSight && state == ATTACK) {
			float randFloat = Random.Range(0.0f, 1.0f);
			if (randFloat > 0.5f)
				changeStateTo(WANDER_AIR);
			else
				changeStateTo(WANDER_GROUND);
		}
        */

		if (state == ATTACK)
			target = player.transform.position;
		else
			target = wanderTarget;
		//Debug.Log ("playerAlt is " + targetAlt);

		Vector3 newGroundNormal = getGroundNormal ();
		if (!onGround && Mathf.Abs(Vector3.Angle(newGroundNormal, Vector3.up)) > 75)
			newGroundNormal = Vector3.up;
        groundNormal = Vector3.Slerp(groundNormal, newGroundNormal, 10 * Time.deltaTime);
        //groundNormal = Vector3.RotateTowards(groundNormal, newGroundNormal, 5 * Mathf.Deg2Rad * Time.deltaTime, 0); 

        groundNormal = Vector3.up; // SIMPLIFIED

		float distToTarget = Vector3.Distance(transform.position, target);
		if (onGround && targetAlt <= airModeCutoffAlt) {
			if (distToTarget > attackDistance) {
				currentSpeed = Mathf.Lerp(0, defaultRunSpeed, runSpeedStep);
				runSpeedStep += runSpeedStepIncrement * Time.deltaTime;
				//currentSpeed = defaultRunSpeed;
			}
			else {
				runSpeedStep = 0;
				currentSpeed = 0;
			}
		}
		else {
			runSpeedStep = 0;
			if (distToTarget > attackDistance) {
				if (facingPlayer)
					currentSpeed = (distToTarget - attackDistance) * airSpeedWeight;
				else if (currentSpeed >= 10)
					currentSpeed -= 10;

				// limit speed if in wander state
				if (state == WANDER_AIR)
					currentSpeed = Mathf.Clamp(currentSpeed, 0, maxWanderSpeed);

				float angleBetweenHeadingAndTarget = Mathf.Abs(Vector3.Angle(transform.forward, target - transform.position));
				if (angleBetweenHeadingAndTarget < 1)
					angleBetweenHeadingAndTarget = 1;
				currentSpeed *= (1.0f / angleBetweenHeadingAndTarget);
			}
		}

		if (!descentMode)
			checkDescentPossible ();
		else if (descentMode) {
			if( currentSpeed > myAlt * descentRate) {
				currentSpeed = myAlt * descentRate * 0.5f;
				//Debug.Log("got here, current speed is now: " + currentSpeed + ", descent speed is " + myAlt * descentRate);
			}
		}

		if (onGround) {
			CancelInvoke("disableDescentMode");
			descentMode = false;
		}

		if (currentSpeed >= planeModeCutoffSpeed2 /*&& anim.GetCurrentAnimatorStateInfo (0).IsName("Air Stance")*/ && !descentMode)
			planeMode = true;
		if (currentSpeed <= planeModeCutoffSpeed1 || myAlt < 12)
			planeMode = false;

		if (onGround)
			enableThrusters(false);
		else
			enableThrusters(true);

		groundMovement ();

		anim.SetBool ("On Ground", onGround);
		anim.SetFloat ("Current Speed", currentSpeed);
		checkFacingPlayer ();
		anim.SetBool ("Facing Player", facingPlayer);
		anim.SetBool ("Plane Mode Enabled", planeMode);

		//Debug.Log ("player alt is " + targetAlt + ", and airmode cutoff alt is " + airModeCutoffAlt);
		//printState ();
		/*
		if (onGround)
			Debug.Log ("GROUND");
		else
			Debug.Log("\t\tair");
			*/
	}

	void LateUpdate() {
		if (anim.GetCurrentAnimatorStateInfo (0).IsName("Aim While Running") || anim.GetCurrentAnimatorStateInfo (0).IsName("Ground Idle")) {
			//Debug.Log("Case 1");
			Vector3 targetInLocalFrame = transform.InverseTransformPoint (target);
			Vector3 spinePosInLocalFrame = transform.InverseTransformPoint(spine.position);
			Vector3 targetGroundProjection = targetInLocalFrame;
			targetGroundProjection.y = 0;
			float angle = Vector3.Angle (targetInLocalFrame - spinePosInLocalFrame, targetGroundProjection);
			if (angle > 45 || angle < -45) {
				float maxHeight = targetGroundProjection.magnitude * Mathf.Sin(Mathf.Rad2Deg * 45);
				if (targetInLocalFrame.y > 0)
					targetInLocalFrame = new Vector3(targetInLocalFrame.x, maxHeight, targetInLocalFrame.z);
				else if (targetInLocalFrame.y < 0)
					targetInLocalFrame = new Vector3(targetInLocalFrame.x, -maxHeight, targetInLocalFrame.z);
				target = transform.TransformPoint(targetInLocalFrame);
			}
			groundAimVector = Vector3.Slerp(groundAimVector, (target - spine.position).normalized, 10 * Time.deltaTime);
			spine.LookAt(spine.position + groundAimVector, (transform.up + Vector3.up) * 0.5f);
			spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
		}
		else if (!anim.GetCurrentAnimatorStateInfo (0).IsName("Transform To Plane Mode") && myAlt >=0 && myAlt < 8) {
			//Debug.Log("*********   Case 2");
			groundAimVector = Vector3.Slerp(groundAimVector, transform.forward, 10 * Time.deltaTime);
			Vector3 up = (transform.up + Vector3.up) * 0.5f;
			//if (!onGround)
			//	up = transform.up;
			spine.LookAt(spine.position + groundAimVector, up);
			spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
		}
		else {
			//Debug.Log("Case three");
			groundAimVector = transform.forward;
		}
	}

	void groundMovement() {

		float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
		if (onGround && !agent.enabled) {
			if (currentSpeed > 0)
				agent.enabled = true;
			else
				agent.enabled = false;
		}
		if (onGround && targetAlt <= airModeCutoffAlt && !planeMode /*&& (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathComplete)*/) {
			Vector3 playerPosInLocalFrame = transform.InverseTransformPoint (target);
			Vector3 adjustedPlayerPosInLocalFrame = playerPosInLocalFrame;
			adjustedPlayerPosInLocalFrame.y = 0;
			Vector3 destinationPoint = transform.TransformPoint (adjustedPlayerPosInLocalFrame);
			if (agent.enabled && !agent.pathPending)
				agent.SetDestination(destinationPoint + allyAvoidanceVector);
			if (currentSpeed > 0) {
				transform.LookAt(transform.position + transform.forward, groundNormal);
			}
			else {
				Vector3 vecToTarget = destinationPoint - rb.position;
				Quaternion desiredRot = Quaternion.LookRotation (vecToTarget, groundNormal);
				transform.rotation = Quaternion.RotateTowards (savedGroundRot,
			                                               		desiredRot,
			                                               		groundTurnRate * Time.deltaTime);
			}
			savedGroundRot = transform.rotation;

		}
		agent.speed = currentSpeed;
		//Debug.Log ("path status is " + agent.pathStatus + ", stale?: " + agent.isPathStale);

		if (!onGround)
			agent.enabled = false;

		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Air Stance")) {
			savedGroundRot = Quaternion.LookRotation(transform.forward, groundNormal);
		}
		/*
		if (onGround && targetAlt <= airModeCutoffAlt && !planeMode) {
			Vector3 oldForwardVec = transform.forward;
			transform.rotation = Quaternion.identity;
			groundNormal = getGroundNormal ();
			Vector3 crossProdVec = Vector3.Cross(groundNormal.normalized, Vector3.right);
			transform.LookAt (transform.position + crossProdVec, groundNormal);
			Vector3 playerPosInLocalFrame = transform.InverseTransformPoint (target);
			Vector3 adjustedPlayerPosInLocalFrame = playerPosInLocalFrame;
			adjustedPlayerPosInLocalFrame.y = 0;
			Vector3 destinationPoint = transform.TransformPoint (adjustedPlayerPosInLocalFrame);

			// check ground obstruction
			RaycastHit hitInfo;
			bool didHit = Physics.Raycast(rb.position, destinationPoint - rb.position, out hitInfo, 100, 1 << LayerMask.NameToLayer ("Ground"));
			if (didHit && Vector3.Distance(rb.position, hitInfo.point) < currentSpeed * Time.fixedDeltaTime) {
				Debug.Log("BLOCK DETECTED");
				destinationPoint = hitInfo.point;
				destinationPoint += hitInfo.normal * 0.2f;
			}

			Vector3 vecToTarget = destinationPoint - rb.position;

			Quaternion desiredRot = Quaternion.LookRotation (vecToTarget, groundNormal);
			transform.rotation = Quaternion.RotateTowards (savedGroundRot,
			                                               	desiredRot,
			                                             	groundTurnRate * Time.deltaTime);
			savedGroundRot = transform.rotation;

		}
		*/
	}


    /*
    void setOnGround(bool newOnGroundState, bool preventImmediateChange) {
        if (groundAirTransitionAllowed) {
            onGround = newOnGroundState;
            if (preventImmediateChange) {
                groundAirTransitionAllowed = false;
                if (IsInvoking("reAllowGroundAirTransition"))
                    CancelInvoke("reAllowGroundAirTransition");
                Invoke("reAllowGroundAirTransition", 0.75f);
            }
        }
    }
    */

    void reAllowGroundAirTransition() {
        groundAirTransitionAllowed = true;
    }


    void FixedUpdate() {

		Vector3 movementVec = Vector3.zero;

		myAlt = getAltitudeWithOffset (gameObject.transform.position, transform.up, 8);
		targetAlt = getAltitude (target);

        float lookAheadMult = 200; // was 150, then 300, then 30 (without currentSpeed * Time.deltaTime part)
        //if (agent.enabled)
        //    lookAheadMult = Vector3.Distance(transform.position, agent.destination) * 1.2f;
        Vector3 direc = (target - transform.position).normalized;
        direc.y = 0;
        //Vector3 lookAheadPoint = transform.position + direc * currentSpeed * lookAheadMult * Time.deltaTime;
        Vector3 lookAheadPoint = transform.position + direc * lookAheadMult;
        bool goingOffCliff = getAltitudeWithOffset(lookAheadPoint, Vector3.up, 8) > 150;

        if (goingOffCliff)
            Debug.DrawRay(transform.position, Vector3.up * 500, Color.magenta);

        Debug.DrawLine(transform.position, lookAheadPoint, Color.black);

        // conditions that must be met for this mech to be considered to be on the ground:
        // -- must be within 3 meters of ground (above of below)
        // -- target must be below the cutoff altitude above which the target must be pursued in the air (the 'airModeCutoffAlt') NOTE: ALTITUDE IS MEASURED BASED ON THE MECH'S UP VECTOR, *NOT* THE WORLD UP VECTOR
        // -- must not be heading off a cliff while travelling towards target
        // -- must be standing more or less upright (i.e. within 75 degrees of vertical)
        if (groundAirTransitionAllowed) {

            if (myAlt <= 3 && myAlt > -3 && targetAlt <= airModeCutoffAlt && !goingOffCliff && Mathf.Abs(Vector3.Angle(Vector3.up, groundNormal)) <= 75) {
                onGround = true;
                groundAirTransitionAllowed = false;
                Invoke("reAllowGroundAirTransition", 1.5f);
            }
            else if (myAlt <= 3 && myAlt > -3 && Mathf.Abs(transform.position.y - target.y) < airModeCutoffAlt && !goingOffCliff && Mathf.Abs(Vector3.Angle(Vector3.up, groundNormal)) <= 75) { // also check actual altitude difference (this time according to world up vector)
                onGround = true;
                groundAirTransitionAllowed = false;
                Invoke("reAllowGroundAirTransition", 1.5f);
            }
            else
                onGround = false;

            // Prevent getting stuck at edge of platform
            bool navAgentDestinationTooFarFromTarget = false;
            if (agent.enabled) {
                Vector3 a = agent.destination;
                a.y = 0;
                Vector3 b = transform.position;
                b.y = 0;
                float cutoffDistance = 300; // was 100, then 200
                if (Vector3.Distance(a, b) > cutoffDistance)
                    navAgentDestinationTooFarFromTarget = true;
            }

            if (goingOffCliff || navAgentDestinationTooFarFromTarget) {
                onGround = false;
                groundAirTransitionAllowed = false;
                Invoke("reAllowGroundAirTransition", 1.5f);
            }

            if (!onGround)
			    agent.enabled = false;

            if (onGround && agent.enabled == true && Vector3.Distance(transform.position, agent.destination) < 1)
                onGround = false;

        }

        if (!agent.enabled || myAlt < 0)
            rb.isKinematic = false;
        else
            rb.isKinematic = true;

        // Prevent getting stuck at edge of platform
        /*
        Vector3 a = agent.destination;
        a.y = 0;
        Vector3 b = transform.position;
        b.y = 0;
        if (Vector3.Distance(a, b) > currentSpeed * Time.deltaTime * 1.5f) {
			agent.enabled = false;
			onGround = false;
		}
        */

        //Debug.Log ("alt is " + myAlt);

        /*
		Quaternion savedRot = transform.rotation;
		transform.LookAt (transform.position + transform.forward);
		Vector3 adjustedTargetPos = transform.InverseTransformPoint(target);
		transform.rotation = savedRot;
		adjustedTargetPos.y = 0;
        */
        Vector3 adjustedTargetPos = new Vector3(target.x, transform.position.y, target.z);

		if (descentMode && myAlt != Mathf.Infinity) {
			//Debug.Log("descending, alt is now: " + myAlt + ", descent speed is " + myAlt * descentRate * Time.deltaTime
			//          + ", current forward speed is " + currentSpeed);
			Vector3 up = Vector3.up;
			if (myAlt < 12)
				up = Vector3.Slerp(groundNormal, up, myAlt / 12.0f);
            //transform.LookAt(transform.TransformPoint(adjustedTargetPos), up);
            transform.LookAt(adjustedTargetPos, up);
            float speed = myAlt * descentRate;
			speed += 20;

			movementVec += Vector3.down * speed;
			//rb.MovePosition(rb.position + Vector3.down * myAlt * descentRate * Time.deltaTime);
		}
		else if (!onGround && !planeMode) {
			//transform.LookAt(target + transform.up * -chestHeight);
			Vector3 up = Vector3.up;
			if (myAlt < 12)
				up = Vector3.Slerp(groundNormal, up, myAlt / 12.0f);
			Quaternion rot = Quaternion.LookRotation((target + transform.up * -chestHeight) - rb.position, up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, airTurnRate * Time.deltaTime);
		}
		else if (!onGround && planeMode) {
			//transform.LookAt(target);
			Quaternion rot = Quaternion.LookRotation(target - rb.position);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 0.5f * airTurnRate * Time.deltaTime);
		}


		if (onGround) {
			if (myAlt < 0)
				rb.position = rb.position + transform.up * Mathf.Abs(myAlt);
			else if (myAlt > 0)
				rb.position = rb.position - transform.up * Mathf.Abs(myAlt);
		}



		movementVec += transform.forward * currentSpeed;
		movementVec += obstacleAvoidanceVector;
		movementVec += allyAvoidanceVector;

		if (!onGround && planeMode) {
			//float newHeadingHorizontalProj = transform.InverseTransformPoint(chest.position + movementVec).x;
			//transform.RotateAround(chest.position, transform.forward, -newHeadingHorizontalProj * 10);

			Vector3 desiredMovementVec = (target - transform.position).normalized * currentSpeed;
			desiredMovementVec += obstacleAvoidanceVector;
			desiredMovementVec += allyAvoidanceVector;

			Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;
			float proj = Vector3.Dot(desiredMovementVec, right);


			proj *= 5f;

			int mult;

			if (Mathf.Abs(Vector3.Angle(transform.up, Vector3.up)) >= 90)
				mult = 1;
			else
				mult = -1;

			transform.RotateAround(chest.position, transform.forward, proj * mult * Time.deltaTime);
		}

		if (!agent.enabled)
			rb.MovePosition(rb.position + movementVec * Time.deltaTime);
		
	}

	void checkDescentPossible() {
		if (targetAlt <= airModeCutoffAlt * 0.5f
		    && myAlt > 3
		    && myAlt < 500
		    && !anim.GetCurrentAnimatorStateInfo (0).IsName("Transform To Plane Mode")
		    && Vector3.Distance(target, transform.position) < attackDistance * 2) {
				descentMode = true;
				Invoke ("disableDescentMode", 7);
		}
	}

	void disableDescentMode() {
		descentMode = false;
	}

	void updateObstacleAvoidanceVector() {
		obstacleAvoidanceVector = getAvoidanceVec (1 << LayerMask.NameToLayer ("Obstacle"), attackDistance, 1000);
		// if flying at low alt, lower the avoidance vector so that it doesn't come into effect instantly upon leaving the ground
		if (myAlt < 12) {
			float adjustedAlt = myAlt;
			if (adjustedAlt < 0)
				adjustedAlt = 0;
			obstacleAvoidanceVector *= adjustedAlt / 12.0f;
		}
	}

	void updateAllyAvoidanceVector() {
		allyAvoidanceVector = getAvoidanceVec (1 << LayerMask.NameToLayer ("Default"), attackDistance, 100); // weight (last param) was 100
		// if flying at low alt, lower the avoidance vector so that it doesn't come into effect instantly upon leaving the ground
		if (myAlt < 12 && myAlt > -12) {
			float adjustedAlt = myAlt;
			if (adjustedAlt < 0)
				adjustedAlt = 0;
			allyAvoidanceVector *= adjustedAlt / 12.0f;
		}
	}

	Vector3 getAvoidanceVec(int layerMask, float radius, float weight) {
		Vector3 vecFromObs = Vector3.zero;
		// get colliders of all nearby obstacles (type depends on layerMask)
		Collider[] hitColliders = Physics.OverlapSphere(chest.position, radius, layerMask);
		foreach (Collider col in hitColliders) {

			// don't count self or any own extremity as obstacle
			if (col.transform.root.gameObject == gameObject)
				continue;

			// get distance to closest point of this obstacle's collider
			Vector3 closestPointOnObs = col.ClosestPointOnBounds(chest.transform.position);
			float distToObs = Vector3.Distance(chest.position, closestPointOnObs);
			// prevent later division by zero (and very small distances)
			if (Mathf.Abs(distToObs) < 0.2f)
				distToObs = 0.2f;

			// check if there is ground between obstacle and this mech, and if so, check if it's below the mech
			// if yes, then disregard this obstacle since there's walkable ground on top of it
			RaycastHit hitInfo;
			bool hitGround = Physics.Raycast(chest.position, closestPointOnObs - chest.position, out hitInfo, distToObs, 1 << LayerMask.NameToLayer("Ground"));
			if (hitGround && Vector3.Distance(chest.position, hitInfo.point) < distToObs && hitInfo.point.y >= closestPointOnObs.y) {
				//Debug.Log ("ground detected over obstacle");
				continue;
			}

			// magnitude of vector inversely proportional to distance from closest point on detected obstacle
			vecFromObs += ( (chest.position - closestPointOnObs).normalized / distToObs );

		}
		return vecFromObs * weight;
	}

	void enableThrusters(bool state){
		L_footThrust1.gameObject.SetActive (state);
		L_footThrust2.gameObject.SetActive (state);
		L_shinThrust1.gameObject.SetActive (state);
		L_shinthrust2.gameObject.SetActive (state);

		R_footThrust1.gameObject.SetActive (state);
		R_footthrust2.gameObject.SetActive (state);
		R_shinThrust1.gameObject.SetActive (state);
		R_shinthrust2.gameObject.SetActive (state);
	}

	void dampenRigidbodyForces() {
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
	}

	void printState() {
		Debug.Log ("altitude: " + myAlt + ", on ground: " + onGround + ", plane mode: " + planeMode + ", speed: " + currentSpeed + ", descent mode: " + descentMode + ", rigidbody position: " + rb.position);
	}

}
