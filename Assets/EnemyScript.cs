using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyScript : MonoBehaviour {

	public const int WANDER = 0;
	public const int PURSUE = 1;
	public const int SEARCH = 2;

	public GameObject gameManger; // reference to the game manager object
	TerrainCollider terrainCol;
	float groundDetectDist = 200;
	float groundDetectWeight = 1500; // was 2500

	float boidUpdateFreq = 0.5f;

	float searchTime = 8;

	public float defaultSpeed;
	//float defaultSpeed = 20.0f;
	float maxSpeed;
	public Vector3 destination;
	public Vector3 direction;
	Vector3 facingDirection;
	float speedScaleFac = 0.1f; // was 0.25
	//float speedScaleFac = 0.15f;
	float energyLevel = 100;
	float energyRegenRate = 2.5f; // was 5, then 20
	float exhaustionTurnPenaltyCutoff = 100;

	float sightRange = 150.0f; // was 100
	float fov = 45.0f; // half of FOV, actually
	float smellRange;

	public float currentWeaponRange;

	float obstacleAvoidanceRange; // range at which obstacles and other agents are detected and avoided
	float agentAvoidanceRange;
	float cohesionRange;
	float closeInDist;

	float defaultCohesionRangeMult = 10;
	float longCohesionRangeMult = 30;

	List<GameObject> neighbors;
	List<GameObject> closeNeighbors;
	float neighborRefreshRate = 0.2f; // frequency in seconds at which neighboring NPCs are detected
	float neighborCheckTargetRate = 3.5f; //2.5f; //1.5f; // frequency in seconds that this agent
												// checks if any neighbor has found the player

	float defaultObstacleAvoidanceWeight = 200f; // was 20, then 35, then 65
	float defaultAgentAvoidanceRange;
	float defaultAgentAvoidanceWeight = 10f; // was 1.5 for original boid thesis thing
	float defaultCohesionWeight = 300.0f; // was 6
	float defaultAlignmentWeight = 4.0f;
	float defaultDestinationWeight = 1f;

	float obstacleAvoidanceWeight;
	float agentAvoidanceWeight;
	float cohesionWeight;
	float alignmentWeight;
	float destinationWeight;

	float smellWeight = 50000;
	float smellMultiplier = 1;
	float standoffWeight;

	float prevSmellLevel = 0;
	float smellLevel = 0;

	public int state;

	bool playerInSight;
	Vector3 lastKnownPlayerPos; // the last place the player was seen
	Vector3 prevLastKnownPlayerPos; // the previous last place the player was seen
	public Vector3 lastKnownPlayerVelocity; // the last known player velocity
	public Vector3 playerPosEstimate; // estimate of where the player currently is (useful if player not in sight)

	bool omniscient;
	// default values
	public bool playerInvisible = false;
	public bool proximityCheck = false;
	public bool playerUnsmellable = false;
	public bool flockingEnabled = true;
	public bool encirclingBehaviorEnabled = true;
	public bool searchStateEnabled = true;
	public bool energyConsumptionEnabled = false;
	public bool rammingEnabled = false;
	bool allowEvasion = true;
	public GameObject player;

	public Vector3 obstacleVec;
	public Vector3 agentVec;
	public Vector3 cohesionVec;
	public Vector3 alignmentVec;

	public Vector3 standoffVec;

	float turnAngle;
	bool checkTurnAngleThisFrame = false;
	float turnAngleCheckRate = 0.1f;
	Vector3 directionLastTurnCheck;
	bool tempPreventEnergyDrain = false;

	LayerMask airBossMask;

	public Vector3 newPos = Vector3.zero;

	// Use this for initialization
	void Start () {

		airBossMask = 1 << LayerMask.NameToLayer ("AirBoss");

		defaultSpeed = 65.0f; // was 65 in thesis, then 130
		currentWeaponRange = 500;

		//GetComponent<SphereCollider> ().enabled = false;
		//GetComponent<CapsuleCollider> ().enabled = false;

		obstacleAvoidanceRange = transform.localScale.magnitude * 1.5f;
		defaultAgentAvoidanceRange = obstacleAvoidanceRange * 4.2f; // was 2.2 for thesis

		setDefaultWeights ();

		smellRange = sightRange * 0.8f; // was * 2.5

		obstacleVec = agentVec = cohesionVec = alignmentVec = Vector3.zero;

		neighbors = new List<GameObject> ();
		closeNeighbors = new List<GameObject> ();

		direction = Vector3.forward;
		directionLastTurnCheck = direction;
		destination = Vector3.forward * 1000;
		facingDirection = direction;

		player = GameObject.FindGameObjectWithTag ("Player");
		terrainCol = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ();
		omniscient = false;

		//avoidanceRange = gameObject.GetComponent<SphereCollider>().bounds.extents.magnitude * 3;
		//closeInDist = obstacleAvoidanceRange * 8;
		closeInDist = 25; // was 15
		setCohesionRangeShort ();

		lastKnownPlayerPos = destination;
		playerPosEstimate = destination;
		prevLastKnownPlayerPos = destination;
		lastKnownPlayerVelocity = Vector3.zero;

		state = WANDER;
		changeColorBasedOnState ();
		playerInSight = false;

		//InvokeRepeating ("longAssBoidFunction", Random.Range (0.1f, boidUpdateFreq), boidUpdateFreq);

		InvokeRepeating ("changeDestination", Random.Range (neighborRefreshRate + 0.1f, neighborRefreshRate + 0.3f), 5);
		if (flockingEnabled) {
			InvokeRepeating ("getNeighbors", Random.Range (0.01f, neighborRefreshRate), neighborRefreshRate);
			InvokeRepeating ("checkNeighborFoundPlayer", Random.Range (0.01f, neighborCheckTargetRate), neighborCheckTargetRate);
		}
		//InvokeRepeating ("allowCheckTurnAngle", turnAngleCheckRate, turnAngleCheckRate);
		InvokeRepeating ("toggleEvasion", Random.Range (0.2f, 1.5f), 1.5f);
	}
	
	// Update is called once per frame
	void Update () {

		//Debug.DrawRay (player.transform.position + player.transform.up * 5, player.transform.right * energyLevel, Color.magenta);
		//Debug.Log ("energy level: " + energyLevel);

		longAssBoidFunction ();

		// Added these 2 liens because the enemies take too long to get to the player otherwise
		float distToPlayer = Vector3.Distance (transform.position, player.transform.position);
		if (distToPlayer > 200 && Vector3.Distance (transform.position, destination) > 200)
			newPos *= 2;

		if (distToPlayer <= 175 && Vector3.Angle(player.transform.forward, transform.position - player.transform.position) <= 15 && allowEvasion) {
			Vector3 myPosInPlayerFrame = player.transform.InverseTransformPoint(transform.position);
			if (myPosInPlayerFrame.z > 0) {
				Vector3 playerAimPoint = player.transform.TransformPoint(Vector3.forward * myPosInPlayerFrame.z);
				//Debug.DrawLine(transform.position, playerAimPoint, Color.magenta);
				Vector3 vecFromAimPoint = transform.position - playerAimPoint;
				float magnitude = vecFromAimPoint.magnitude;
				if (magnitude <= 0)
					magnitude = 0.001f;
				float mult = 75;
				if (state == PURSUE)
					mult = 35;
				Vector3 evasionVec = vecFromAimPoint.normalized * (1.0f / magnitude) * mult;
				//Debug.Log("evasion vec mag: " + evasionVec.magnitude);
				newPos += evasionVec;
			}
		}

		//rigidbody.MovePosition (transform.position + (newPos * Time.deltaTime));

		//checkTurnAngleThisFrame = false;
	}

	void FixedUpdate() {
		GetComponent<Rigidbody>().MovePosition (transform.position + (newPos * Time.deltaTime));
	}

	void longAssBoidFunction() {
		maxSpeed = defaultSpeed * 1.7f; // was * 1.3
		
		// RECOVER FROM ANY COLLISION(S)
		dampenRigidbodyForces ();
		
		// CHECK IF PLAYER IN SIGHT
		visionCheck ();
		
		if (proximityCheck)
			basicProximityCheck();
		
		// IF PLAYER IN SIGHT, PURSUE PLAYER
		if (playerInSight && GameManagerScript.numActiveBullets <= 500 && GameManagerScript.numFightersPursuingPlayer <= 10)
			changeStateTo (PURSUE);

		if ((GameManagerScript.numActiveBullets > 500 || GameManagerScript.numFightersPursuingPlayer > 10) && state == PURSUE)
			changeStateTo(WANDER);
		
		// UPDATE ESTIMATE OF WHERE PLAYER IS
		updatePlayerPosEstimate ();
		
		// CHANGE FLOCKING VARIABLES BASED ON STATE
		updateFlightParams ();
		
		Vector3 newDirection = destination - transform.position;
		if (state != PURSUE && newDirection.magnitude > 0)
			newDirection.Normalize();
		
		newDirection *= destinationWeight;
		
		if (state == WANDER) {
			getSmellMultiplier();
			newDirection *= smellMultiplier;
		}
		
		Vector3 directVectorToDest = newDirection;
		
		obstacleVec = obstacleCheck ();
		agentVec = agentCheck ();
		cohesionVec = getCohesionVec ();
		alignmentVec = getAlignmentVec ();
		
		standoffVec = Vector3.zero;
		if (encirclingBehaviorEnabled && flockingEnabled && neighbors.Count > 0)
			standoffVec = getStandoffVec ();
		
		newDirection += agentVec;
		newDirection += cohesionVec;
		newDirection += alignmentVec;
		
		// needed for surrounding
		newDirection += standoffVec;
		
		if (state == WANDER)
			newDirection.Normalize ();
		else {
			if (newDirection.magnitude > 1)
				newDirection *= 0.05f;
			else if (newDirection.magnitude > 0) {
				float sf = 1 / newDirection.magnitude;
				newDirection *= sf;
			}
		}
		
		newDirection += obstacleVec;

		float speed = defaultSpeed;

		if (!flockingEnabled)
			newDirection = directVectorToDest;
		
		direction.Normalize ();
		
		// EXHAUSTION PENALTY FOR TURNING
		if (energyLevel < exhaustionTurnPenaltyCutoff) {
			float originalMag = newDirection.magnitude;
			newDirection = Vector3.Slerp(direction, newDirection, (energyLevel) / exhaustionTurnPenaltyCutoff).normalized * originalMag;
		}
		
		if (checkTurnAngleThisFrame)
			turnAngle = Vector3.Angle(directionLastTurnCheck, newDirection);

		getRollRot (newDirection);

		direction += newDirection * speedScaleFac;

		// DON'T CRASH INTO GROUND...
		direction += groundCheckSimple (transform.position, groundDetectDist);
		
		if (checkTurnAngleThisFrame)
			directionLastTurnCheck = newDirection.normalized;
		
		newPos = direction * speed * (energyLevel / 100.0f);
		
		if ((state == WANDER && newPos.magnitude > defaultSpeed) || closeToPlayer())
			newPos = Vector3.ClampMagnitude (newPos, defaultSpeed);
		
		if (newPos.magnitude > maxSpeed)
			newPos = Vector3.ClampMagnitude (newPos, maxSpeed);
		
		if (!rammingEnabled && closeToPlayer())
			newPos = Vector3.ClampMagnitude (newPos, lastKnownPlayerVelocity.magnitude);

		/*
		if (GameManagerScript.enemies.Count <= 3) {
			Debug.DrawLine (transform.position, player.transform.position, Color.magenta);
			Debug.DrawRay (transform.position, newDirection * 10, Color.cyan);
			if (true) {
				Debug.Log ("new direction magnitude: " + newDirection.magnitude
				           + ", energy level is " + energyLevel + " and speed is " + newPos.magnitude
				           + ", turn angle is " + turnAngle);
			}
		}
		*/
		
		
		//if (closeToPlayer() && flockingEnabled)
		//	newPos = Vector3.ClampMagnitude(newPos, lastKnownPlayerVelocity.magnitude * 1.2f);
		
		//rigidbody.MovePosition (transform.position + (newPos * Time.deltaTime));

		if (state == WANDER) {
			facingDirection = direction;
		}
		else {
			if (energyLevel > exhaustionTurnPenaltyCutoff) {
				facingDirection = destination - transform.position;
				facingDirection.Normalize ();
			}
			else {
				Vector3 wanted = (destination - transform.position).normalized;
				facingDirection = Vector3.Slerp(direction, wanted, energyLevel / exhaustionTurnPenaltyCutoff);
			}
		}

		/*
		if (state == PURSUE || (state == SEARCH && Vector3.Distance (transform.position, destination) <= sightRange * 0.5f)) {
			facingDirection = destination - transform.position;
			facingDirection.Normalize ();
		}
		else
			facingDirection = direction;
		*/
		transform.LookAt (transform.position + facingDirection);
		/*
		Debug.Log ("current speed is " + newPos.magnitude
		           + ", default is " + defaultSpeed + ", max is " + maxSpeed
		           + ", energy level is " + energyLevel
		           + ", regen rate is " + energyRegenRate * Time.deltaTime + " per frame");
		*/
		
		if (!tempPreventEnergyDrain && energyConsumptionEnabled)
			adjustEnergyLevel (newPos.magnitude, turnAngle);
		
		changeColorBasedOnState ();
		
		/*
		if (state == SEARCH) {
			Debug.DrawRay(transform.position, transform.right * agentAvoidanceRange, Color.red);
			Debug.DrawRay(transform.position + transform.up * 2, transform.right * cohesionRange, Color.green);
		}
		*/
		//Debug.Log ("turn angle is " + turnAngle);
	}

	void getRollRot(Vector3 newDirec) {
		float scale = 1;
		float angleFromRight = Vector3.Angle (transform.right, newDirec);
		float angleFromLeft = Vector3.Angle (transform.right * -1, newDirec);
		if (angleFromRight < 90)
			transform.RotateAround(transform.position, transform.forward, -scale * (90 - angleFromRight));
		else if (angleFromLeft < 90)
			transform.RotateAround(transform.position, transform.forward, scale * (90 - angleFromLeft));
		//Debug.DrawRay (transform.position, transform.up * 20, Color.magenta);
	}

	void updateFlightParams() {

		agentAvoidanceWeight = defaultAgentAvoidanceWeight;
		if (state == WANDER) {
			//checkNeighborFoundPlayer ();
			destinationWeight = 1;
			//cohesionWeight = defaultCohesionRangeMult;
			cohesionWeight = defaultCohesionWeight;
			if (neighbors.Count <= 0) {
				setCohesionRangeLong ();
				cohesionRange *= 1.5f;
				cohesionWeight = defaultCohesionWeight * 1.33f;
			}
			// if flock too small, expand cohesion range to try to find/form a pack
			else if (neighbors.Count > 0 && neighbors.Count <= 2) {
				setCohesionRangeLong ();
				cohesionWeight = defaultCohesionWeight * 1000; // * 1.33f;
			}
			// if flock is in ideal size range, set moderate cohesion distance
			else if (neighbors.Count < 7) {
				setCohesionRangeShort();
				cohesionRange = cohesionRange * 1.3f;
				cohesionWeight = defaultCohesionWeight * 5000;
			}
			// if flock too big, lower cohesion distance
			else {
				setCohesionRangeShort();
				cohesionRange = cohesionRange * 0.2f; // was 0.5
				cohesionWeight = 0;
				agentAvoidanceWeight = defaultAgentAvoidanceWeight * 3; // was default x2
			}
		}
		else if (state == PURSUE) {
			//Debug.DrawLine(transform.position, playerPosEstimate, Color.yellow);
			cohesionWeight = 0; //defaultCohesionWeight * 0.2f;
			destinationWeight = 10f;
			alignmentWeight = defaultAlignmentWeight * 0.2f;
			agentAvoidanceWeight = defaultAgentAvoidanceWeight * 20;
			destination = playerPosEstimate;
			// if close to player, change paramters for surrounding player
			if (flockingEnabled && encirclingBehaviorEnabled && closeToPlayer() && rammingEnabled) {
				closeIn();
			}
			// if can't see player anymore, begin searching
			if (!playerInSight)
				changeStateTo(SEARCH);

		}
		else if (state == SEARCH) {
			//checkNeighborFoundPlayer ();
			//Debug.DrawLine(transform.position, playerPosEstimate, Color.magenta);
			//Debug.DrawLine(playerPosEstimate, playerPosEstimate + lastKnownPlayerVelocity, Color.red);
			destination = playerPosEstimate;
			spreadOut();

		}
		else
			Debug.LogError("undefined state");

	}

	void closeIn() {
		cohesionWeight = 0;
		setCohesionRangeShort ();
		cohesionRange *= 2;
		destinationWeight = 1;
		standoffWeight = 1;
		alignmentWeight = 0;
		agentAvoidanceRange = defaultAgentAvoidanceRange * 1.75f;
		agentAvoidanceWeight = defaultAgentAvoidanceWeight * 8;
		// find how many neighbors have surrounded the player
		int numNeighborsInPosition = 0;
		foreach (GameObject neighbor in neighbors) {
			if (!neighbor)
				continue;
			if (Vector3.Distance(neighbor.transform.position, player.transform.position) <= closeInDist * 1.2f)
				numNeighborsInPosition++;
		}
		// if 75% of the neighbors have surrounded the player, go in for the kill
		if ((float)numNeighborsInPosition / neighbors.Count >= 0.75 || (neighbors.Count  <= 0)) {
			destinationWeight = 2; // was 10
			//Debug.DrawLine(transform.position, player.transform.position, Color.white);
		}
		if (!flockingEnabled)
			setDefaultWeights();
	}

	void changeStateTo(int newState) {

		if (state == newState)
			return;

		if (newState == PURSUE)
			GameManagerScript.numFightersPursuingPlayer++;
		else if (state == PURSUE)
			GameManagerScript.numFightersPursuingPlayer--;

		if (!searchStateEnabled && newState == SEARCH)
			newState = WANDER;

		if (state == SEARCH && newState != SEARCH) {
			CancelInvoke("finishSearching");
			setDefaultWeights();
		}
		else if (state == WANDER && newState != WANDER)
			CancelInvoke("changeDestination");

		if (newState == WANDER) {
			InvokeRepeating ("changeDestination", 0.1f, 5);
		}
		else if (newState == PURSUE) {
			tempPreventEnergyDrain = true;
			CancelInvoke("allowEnergyDrain");
			Invoke("allowEnergyDrain", 0.5f);
			destination = lastKnownPlayerPos;
		}
		else if (newState == SEARCH) {
			Invoke("finishSearching", searchTime);
		}

		state = newState;
	}

	void spreadOut() {
		setCohesionRangeShort ();
		cohesionRange *= 1.5f; // not long but not too short
		agentAvoidanceRange = cohesionRange * 0.8f; // was x 0.3f
		agentAvoidanceWeight = defaultAgentAvoidanceWeight * 8f; // was 1.8

		cohesionWeight = 0;
		alignmentWeight = defaultAlignmentWeight * 0.5f;
		destinationWeight = Vector3.Distance(transform.position, playerPosEstimate) * 0.1f;

		// add noise to player position estimate
		//float max = sightRange * 0.66f;
		//playerPosEstimate += new Vector3 (Random.Range (-max, max), Random.Range (-max, max), Random.Range (-max, max));
	}

	void checkNeighborFoundPlayer() {
		if (state != WANDER)
			return;
		foreach (GameObject neighbor in neighbors) {
			if (!neighbor)
				continue;
			EnemyScript neighborSc = neighbor.GetComponent<EnemyScript>();
			if (neighborSc.state == PURSUE) {
				// set estimate of player position to the estimate of neighbor who has found player
				playerPosEstimate = neighborSc.playerPosEstimate;
				// update other stuff accordingly
				lastKnownPlayerPos = neighborSc.lastKnownPlayerPos;
				prevLastKnownPlayerPos = neighborSc.prevLastKnownPlayerPos;
				lastKnownPlayerVelocity = neighborSc.lastKnownPlayerVelocity;
				// set weights back to normal so flock not spread out in search pattern
				setDefaultWeights();
				return;
			}
		}
	}

	void setDefaultWeights() {
		obstacleAvoidanceWeight = defaultObstacleAvoidanceWeight;
		agentAvoidanceWeight = defaultAgentAvoidanceWeight;
		agentAvoidanceRange = defaultAgentAvoidanceRange;
		cohesionWeight = defaultCohesionWeight;
		alignmentWeight = defaultAlignmentWeight;
		destinationWeight = defaultDestinationWeight;
		standoffWeight = 1;
	}

	void updatePlayerPosEstimate() {
		if (playerInSight) {
			playerPosEstimate = lastKnownPlayerPos;
			//playerPosEstimate = player.transform.position;
		}
		else {
			playerPosEstimate += lastKnownPlayerVelocity;
		}
	}

	void updateMemoryOfPlayerPosTo(Vector3 position) {
		lastKnownPlayerPos = position;
		lastKnownPlayerVelocity = lastKnownPlayerPos - prevLastKnownPlayerPos;
		if (lastKnownPlayerVelocity.magnitude > 100)
			lastKnownPlayerVelocity = Vector3.ClampMagnitude(lastKnownPlayerVelocity, 100);
		prevLastKnownPlayerPos = lastKnownPlayerPos;
	}


	void visionCheck() {
		if (playerInvisible) {
			playerInSight = false;
			return;
		}
		if (omniscient) {
			if (state == WANDER)
				CancelInvoke("changeDestination");
			lastKnownPlayerPos = player.transform.position;
			state = PURSUE;
			playerInSight = true;
			return;
		}
		// check to see if player in sight
		if (player && Vector3.Distance(transform.position, player.transform.position) <= sightRange) {
			Vector3 vecToPlayer = player.transform.position - transform.position;
			float angle = Mathf.Abs(Vector3.Angle(facingDirection, vecToPlayer));
			if (angle <= fov && clearLOS(gameObject, player, sightRange)) {
				updateMemoryOfPlayerPosTo(player.transform.position);
				playerInSight = true;
				return;
			}
		}
		playerInSight = false;
		
	}

	// basic detection of player, not intended to be "realistic" like the above function
	void basicProximityCheck() {
		if (player && Vector3.Distance (transform.position, player.transform.position) <= sightRange) {
			updateMemoryOfPlayerPosTo(player.transform.position);
			playerInSight = true;
		}
		else
			playerInSight = false;
	}

	bool clearLOS(GameObject obj1, GameObject obj2, float range) {
		RaycastHit[] hits;
		Vector3 rayDirection = obj2.transform.position - obj1.transform.position;
		hits = Physics.RaycastAll(obj1.transform.position, rayDirection, range,
		       	  (1 << LayerMask.NameToLayer ("Player"))
				| (1 << LayerMask.NameToLayer ("AirBoss"))
				| (1 << LayerMask.NameToLayer ("Ground")));
		if (hits.Length <= 0)
			return false;
		GameObject closest = hits [0].collider.gameObject;
		float distToClosest = Vector3.Distance(obj1.transform.position, closest.transform.position);
		foreach (RaycastHit hit in hits) {
			GameObject current = hit.collider.gameObject;
			if (current == obj1)
				continue;
			float distToCurrent = Vector3.Distance(obj1.transform.position, current.transform.position);
			if (distToCurrent < distToClosest) {
				closest = current;
				distToClosest = distToCurrent;
			}
		}
		if (closest == obj2)
			return true;
		else
			return false;
	}

	void finishSearching() {
		if (state != WANDER) {
			//InvokeRepeating ("changeDestination", 5, 5);
			//state = WANDER;
			changeStateTo(WANDER);
		}
	}

	Vector3 obstacleCheck() {
		// check for obstacles and return vector away from them
		//Collider[] cols = Physics.OverlapSphere(transform.position, obstacleAvoidanceRange);
		Collider[] cols = Physics.OverlapSphere(transform.position, obstacleAvoidanceRange, airBossMask);
		Vector3 avoidanceVec = Vector3.zero;
		/*
		foreach (Collider col in cols) {
			if (col.gameObject.tag == "Obstacle" || col.gameObject.tag == "Score Powerup") {
				Vector3 vecFromObj = transform.position - col.gameObject.transform.position;
				float range = vecFromObj.magnitude - col.gameObject.GetComponent<SphereCollider>().radius;
				vecFromObj.Normalize();
				//avoidanceVec += vecFromObj * obstacleAvoidanceWeight * (1 / Mathf.Pow(range, 2));
				avoidanceVec += vecFromObj * (1 / range);
			}
		}
		*/
		foreach (Collider col in cols) {
			Vector3 vecFromObj = transform.position - col.gameObject.transform.position;
			float range = vecFromObj.magnitude;
			vecFromObj.Normalize();
			avoidanceVec += vecFromObj * (1 / range);
		}
		return avoidanceVec * obstacleAvoidanceWeight;
	}

	Vector3 agentCheck() {
		Vector3 avoidanceVec = Vector3.zero;
		foreach (GameObject enemy in closeNeighbors) {
			if (!enemy)
				continue;
			if (enemy != gameObject) {
				Vector3 vecFromObj = transform.position - enemy.transform.position;
				float range = vecFromObj.magnitude;
				vecFromObj.Normalize();
				//avoidanceVec += vecFromObj * agentAvoidanceWeight * (1 / Mathf.Pow(range, 2));
				avoidanceVec += vecFromObj * (1 / range);
			}
		}
		return avoidanceVec * agentAvoidanceWeight;
	}

	/*
	void getNeighbors() {
		neighbors.Clear ();
		closeNeighbors.Clear ();
		Collider[] cols = Physics.OverlapSphere(transform.position, cohesionRange);
		foreach (Collider col in cols) {
			if (col.gameObject.tag == "Enemy" && col.gameObject != gameObject) {
				neighbors.Add(col.gameObject);
				if (Vector3.Distance(transform.position, col.gameObject.transform.position) < agentAvoidanceRange)
					closeNeighbors.Add(col.gameObject);
			}
		}
	}
	*/

	void getNeighbors() {
		neighbors.Clear ();
		closeNeighbors.Clear ();
		foreach(GameObject enemy in GameManagerScript.enemies) {
			if (!enemy)
				continue;
			float distToOther = Vector3.Distance(transform.position, enemy.transform.position);
			if (distToOther <= cohesionRange) {
				neighbors.Add(enemy);
				if (distToOther < agentAvoidanceRange)
					closeNeighbors.Add(enemy);
			}
		}
	}

	Vector3 getCohesionVec() {
		// if no neighbors, return vector towards closest enemy in Game Manager's list
		if (flockingEnabled && neighbors.Count <= 0 && GameManagerScript.enemies.Count > 0) {
			float distToClosest = Mathf.Infinity;
			Vector3 positionOfClosest = Vector3.zero;
			foreach(GameObject enemy in GameManagerScript.enemies) {
				if (!enemy)
					continue;
				float distToCurrent = Vector3.Distance(transform.position, enemy.transform.position);
				if (enemy != gameObject && distToCurrent < distToClosest) {
					distToClosest = distToCurrent;
					positionOfClosest = enemy.transform.position;
				}
			}
			if (positionOfClosest != Vector3.zero)
				return (positionOfClosest - transform.position) * cohesionWeight;
			else
				return Vector3.zero;
		}

		Vector3 localCentroid = transform.position;
		float flockSize = neighbors.Count + 1;
		foreach (GameObject obj in neighbors) {
			if (!obj)
				continue;
			localCentroid += obj.transform.position;
		}
		localCentroid /= flockSize;
		// if already close enough to flock centroid, don't bother with cohesion
		if (Vector3.Distance (transform.position, localCentroid) < agentAvoidanceRange * 4f)
			return Vector3.zero;
		Vector3 vecToLocalCent = localCentroid - transform.position;
		return vecToLocalCent * cohesionWeight;
	}

	Vector3 getAlignmentVec () {
		// get average destination of flock
		Vector3 avgDest = destination;
		float flockSize = neighbors.Count + 1;
		foreach (GameObject obj in neighbors) {
			if (!obj)
				continue;
			EnemyScript currentObj = obj.GetComponent<EnemyScript>();
			if (!currentObj)
				continue;
			float mult = 1;
			if (currentObj.state == PURSUE)
				mult = 50;
			avgDest += currentObj.destination * mult;
		}
		avgDest += destination;
		avgDest /= flockSize;
		// get vector to average destination
		Vector3 vecToAvgDest = avgDest - transform.position;
		return vecToAvgDest.normalized * alignmentWeight;
	}

	Vector3 getStandoffVec() {
		Vector3 returnVec = Vector3.zero;
		if (closeToPlayer()) {
			returnVec = transform.position - player.transform.position;
			returnVec *= standoffWeight;
		}
		return returnVec;
	}

	float smellCheck() {
		if (!player || playerUnsmellable)
			return 0;
		float rangeToPlayer = Vector3.Distance (transform.position, player.transform.position);
		if (rangeToPlayer <= smellRange)
			smellLevel = 1.0f / rangeToPlayer;
		else
			smellLevel = 0;

		float deltaSmell = smellLevel - prevSmellLevel;
		deltaSmell *= smellWeight;
		//Debug.Log ("delta smell is " + deltaSmell);
		prevSmellLevel = smellLevel;

		return deltaSmell;

	}

	void getSmellMultiplier() {
		smellMultiplier = smellCheck ();
		if (smellMultiplier >= 0)
			smellMultiplier += 1;
		else
			smellMultiplier -= 1;

		smellMultiplier = Mathf.Clamp (smellMultiplier, -20.0f, 20.0f);
		//Debug.Log ("smell multiplier is " + smellMultiplier);
	}

	void dampenRigidbodyForces() {
		GetComponent<Rigidbody>().velocity = Vector3.zero;
		GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
	}

	void setCohesionRangeShort() {
		cohesionRange = agentAvoidanceRange * defaultCohesionRangeMult;
	}

	void setCohesionRangeLong() {
		cohesionRange = agentAvoidanceRange * longCohesionRangeMult;
	}

	void changeDestination() {
		if (state != WANDER) {
			CancelInvoke("changeDestination");
			return;
		}

		/*
		float f1, f2, f3;
		f1 = Random.Range(-terrainCol.bounds.extents.x, terrainCol.bounds.extents.x);
		f2 = Random.Range(terrainCol.transform.position.x + 50, terrainCol.transform.position.x + 220);
		f3 = Random.Range(-terrainCol.bounds.extents.z, terrainCol.bounds.extents.z);
		Vector3 newDestination = new Vector3(f1, f2, f3);
		newDestination += 300 * groundCheck (newDestination, 1000);
		newDestination = Vector3.ClampMagnitude (newDestination, terrainCol.bounds.extents.magnitude * 0.8f);
		destination = newDestination;
		*/

		float f1, f2, f3;
		float spread = 800.0f;
		f1 = Random.Range(-spread, spread);
		f2 = Random.Range(-spread * 0.5f, spread * 0.5f);
		f3 = Random.Range(-spread, spread);
		Vector3 newDestination = new Vector3(f1, f2, f3);
		newDestination += player.transform.position;
		newDestination = Vector3.ClampMagnitude (newDestination, terrainCol.bounds.extents.x * 0.8f);
		destination = newDestination;
	}

	void adjustEnergyLevel (float currentSpeed, float turnAngle) {
		float speedPenalty = currentSpeed - defaultSpeed;
		//float speedPenalty2 = currentSpeed - 40;
		if (speedPenalty > 0 && energyLevel > 0) {
			// decrement energy based on how much faster than normal (above default speed) we're going
			energyLevel -= speedPenalty * 0.5f * Time.deltaTime; // was 0.05, then 0.5
			// decrement energy based on turn angle
			//energyLevel -= turnAngle * speedPenalty * 10.0f * Time.deltaTime; // was * 0.2, then 0.1, then 0.05
		}
		else if (energyLevel < 100)
			energyLevel += energyRegenRate * Time.deltaTime;

		if (state == PURSUE)
			energyLevel -= turnAngle * 0.4f * Time.deltaTime;

		if (energyLevel < 5f)
			energyLevel = 5f;

		if (energyLevel > 100)
			energyLevel = 100;

		//Debug.Log ("ENERGY LEVEL: " + energyLevel);
	}

	void allowEnergyDrain() {
		tempPreventEnergyDrain = false;
		//Debug.LogWarning ("BUFF WEAROFF");
	}

	void changeColorBasedOnState() {
		if (state == WANDER)
			GetComponent<Renderer>().material.color = Color.green;
		else if (state == PURSUE)
			GetComponent<Renderer>().material.color = Color.red;
		else if (state == SEARCH)
			GetComponent<Renderer>().material.color = Color.yellow;
		else
			GetComponent<Renderer>().material.color = Color.white; // should probably never get here

		// make color blue if energy low
		if (energyLevel <= 10) {
			//renderer.material.color *= 0.5f;
			GetComponent<Renderer>().material.color = Color.blue;
			//Debug.Log("EXHAUSTED");
		}

		if (energyLevel < 1) {
			GetComponent<Renderer>().material.color = Color.cyan;
			Debug.LogWarning("problem");
		}

	}

	/*
	void allowCheckTurnAngle() {
		checkTurnAngleThisFrame = true;
	}
	*/

	void toggleEvasion () {
		if (allowEvasion)
			allowEvasion = false;
		else
			allowEvasion = true;
	}

	bool closeToPlayer() {
		if (playerInSight
			&& Vector3.Distance (transform.position, destination) < closeInDist
			&& (state == PURSUE || state == SEARCH))
			return true;
		else
			return false;
	}

	Vector3 groundCheck(Vector3 pos, float radius) {
		Vector3 returnVec = Vector3.zero;
		const int len = 6;
		Ray[] rays = new Ray[len];
		rays[0] = new Ray (pos, transform.up); // up
		rays[1] = new Ray (pos, transform.up * -1); // down
		rays[2] = new Ray (pos, transform.right * -1); // left
		rays[3] = new Ray (pos, transform.right); // right
		rays[4] = new Ray (pos, transform.forward); // forward
		rays[5] = new Ray (pos, transform.forward * -1); // back
		RaycastHit hitInfo;
		for (int i = 0; i < len; i++) {
			bool didHit;
			didHit = terrainCol.Raycast (rays[i], out hitInfo, radius);
			//didHit = Physics.SphereCast (rays[i].origin, 60, rays[i].direction, out hitInfo, radius);
			if (!didHit/* || hitInfo.collider.tag != "Ground"*/)
				continue;
			//Debug.Log("ray hit ground");
			Vector3 vecFromContactPt = transform.position - hitInfo.point;
			float distFromContactPt = vecFromContactPt.magnitude;
			if (distFromContactPt < 0.1f)
				distFromContactPt = 0.1f;
			returnVec += (vecFromContactPt.normalized / distFromContactPt) * groundDetectWeight;
		}
		return returnVec;
	}

	Vector3 groundCheckSimple(Vector3 pos, float dist) {
		Vector3 returnVec = Vector3.zero;
		Ray rayDown = new Ray (pos, Vector3.up * -1); // down
		Ray rayFwd = new Ray (pos, transform.forward); // forward
		RaycastHit hitInfo;
		bool didHit;
		didHit = terrainCol.Raycast (rayDown, out hitInfo, dist);
		if (didHit) {
			Vector3 vecFromContactPt = transform.position - hitInfo.point;
			float distFromContactPt = vecFromContactPt.magnitude;
			if (distFromContactPt < 0.1f)
				distFromContactPt = 0.1f;
			returnVec += (vecFromContactPt.normalized / Mathf.Pow(distFromContactPt, 2)) * groundDetectWeight;
		}
		didHit = terrainCol.Raycast (rayFwd, out hitInfo, dist * 0.2f);
		if (didHit) {
			Vector3 vecFromContactPt = transform.position - hitInfo.point;
			float distFromContactPt = vecFromContactPt.magnitude;
			if (distFromContactPt < 0.1f)
				distFromContactPt = 0.1f;
			returnVec += (vecFromContactPt.normalized / Mathf.Pow(distFromContactPt, 2)) * groundDetectWeight;
		}
		return returnVec;
	}

	/*
	void OnCollisionEnter(Collision collision) {
		if (collision.collider.gameObject.tag == "Ground")
			Debug.Log("enemy fighter hit ground");
	}
	*/

	bool checkVectorIsNaN(Vector3 test) {
		if (System.Single.IsNaN (test.x) || System.Single.IsNaN (test.y) || System.Single.IsNaN (test.z)) {
			//Debug.Log ("position is: " + rigidbody.position + ", direction is: " + direction);
			return true;
		}
		return false;
	}

	void OnDestroy() {
		if (state == PURSUE)
			GameManagerScript.numFightersPursuingPlayer--;
		GameManagerScript.enemies.Remove (gameObject);
	}
	

}
