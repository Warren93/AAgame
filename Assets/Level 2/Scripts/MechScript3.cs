using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MechScript3 : MonoBehaviour
{

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
	[HideInInspector]
    public Vector3 movementVec = Vector3.zero;
    PlayerScript playerInfo;
    Animator anim;
    Rigidbody rb;
    Vector3 prevRigidbodyPos;
    Transform spine;
    public Transform chest = null;
    float chestHeight;

    Transform L_footThrust1, L_footThrust2, L_shinThrust1, L_shinthrust2, R_footThrust1, R_footthrust2, R_shinThrust1, R_shinthrust2;

    const int ATTACK = 0;
    const int WANDER_AIR = 1;
    const int WANDER_GROUND = 2;
    const int LANDING = 3;
    const int WANDER_PLATFORM = 4;
    public int state = ATTACK;
    float maxWanderSpeed = 150f; // was 100, then 80
    MechWanderTarget wanderTarget;

    bool onGround = false;
    bool groundAirTransitionAllowed = true;
    GameObject terrain;

    float defaultRunSpeed = 12f; // was 1.2 for run anim speed of 0.1
    float runSpeedStep = 0f;
    float runSpeedStepIncrement = 3f;

    float airSpeedIncrement = 3f;
    float airTurnRate = 50f;
    float descentRate = 0.05f;
    float defaultAirSpeed = 20f; // was 20
    GameObject dummyParentForPlaneMode;

    float planeModeCutoffSpeed1 = 1f; // was 25, then 23, then 18, then 5
    float planeModeCutoffSpeed2 = 25f; // was 27
    float defaultPlaneSpeed = 30f;
    float planeAcceleration = 10f;

	[HideInInspector]
    public float currentSpeed = 0f;
    float stopDistance;
    float attackDistance = 180f;

    Vector3 groundAimVector;
    bool facingPlayer = false;
    public bool planeMode = false;

    bool descentMode = false;

    float groundTurnRate = 50f; // was 50
    float prevGroundFacingAngle = 180f;
    Quaternion savedGroundRot;
	float facingAngle = 45f; // was 60
    float fov = 60f;
    float sightRange = 600f; // was 300

    float myAlt;
    float targetAlt;
    float airModeCutoffAlt = 65f; // was 200 (not multiplied by localScale.x), was then 33 (multiplied by localScale.x)

    public NavMeshAgent agent;
    bool repathAllowed = true;
    Vector3 obstacleAvoidanceVector = Vector3.zero;
	float allyAvoidanceWeight = 300; // was 100 (pre 6/4/17);
    Vector3 allyAvoidanceVector = Vector3.zero;

    float groundDetectRadius = 300; // was 200
    GameObject nearestGroundRef = null;
    public GameObject lastObjLandedOn = null;
    Vector3 landingPoint;

    public string navAgentStatusString;

	const int OBSTACLE = 1;
	const int ALLY = 2;

    void Awake()
    {
        // MechWanderTarget script needs to know chest position when it is initialized, so make sure to find it here before that script gets initialized
        foreach (Transform child in transform.GetChild(0))
        {
            if (child.name == "metarig")
            {
                spine = child.GetChild(0).GetChild(2);
                break;
            }
        }
        chest = spine.GetChild(0);

        agent = GetComponent<NavMeshAgent>();
    }

    // Use this for initialization
    void Start()
    {
        dummyParentForPlaneMode = new GameObject();

        stopDistance = attackDistance;

        terrain = GameObject.Find("Terrain");
        agent.avoidancePriority = Random.Range(0, 99);
        agent.autoTraverseOffMeshLink = false;
        agent.acceleration = 1000;

        savedGroundRot = Quaternion.identity;

        defaultRunSpeed *= transform.localScale.x;
        descentRate *= transform.localScale.x;
        defaultAirSpeed *= transform.localScale.x;
        planeModeCutoffSpeed1 *= transform.localScale.x;
        planeModeCutoffSpeed2 *= transform.localScale.x;
        defaultPlaneSpeed *= transform.localScale.x;
        airModeCutoffAlt *= transform.localScale.x;

        //player = GameObject.Find ("dummy player");
        player = GameObject.FindGameObjectWithTag("Player");
        target = player.transform.position;
        playerInfo = player.GetComponent<PlayerScript>();

        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        /* the following foreach loop only works of "optimize game objects" is checked in mech rig tab (but this messes up LateUpdate overrides
		foreach (Transform child in transform.GetChild(0)) {
			if (child.name == "chest")
				chest = child;
			else if (child.name == "spine")
				spine = child;
		}
		*/

        foreach (Transform child in transform.GetChild(0))
        {
            if (child.name == "metarig")
            {
                spine = child.GetChild(0).GetChild(2);
                break;
            }
        }
        chest = spine.GetChild(0);

        chestHeight = Vector3.Distance(chest.transform.position, transform.position);

        rb = GetComponent<Rigidbody>();
        prevRigidbodyPos = rb.position;

        foreach (Transform child in transform.GetChild(0))
        {
            if (child.name == "metarig")
            {
                L_footThrust1 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1);
                L_footThrust2 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(2);
                L_shinThrust1 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(2);
                L_shinthrust2 = child.GetChild(0).GetChild(0).GetChild(0).GetChild(3);
            }
        }
        foreach (Transform child in transform.GetChild(0))
        {
            if (child.name == "metarig")
            {
                R_footThrust1 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(1);
                R_footthrust2 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetChild(2);
                R_shinThrust1 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(2);
                R_shinthrust2 = child.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
            }
        }

        //Debug.Log ("spine is " + spine + ", and chest is " + chest);
        //Debug.Log ("chest height is " + chestHeight);
        //Invoke ("changeSpeed", 2.0f);

        wanderTarget = GetComponent<MechWanderTarget>();

//        InvokeRepeating("updateObstacleAvoidanceVector", Random.Range(0.1f, 1.3f), 0.05f); // repeat rate was 0.5f, then 0.2
//        InvokeRepeating("updateAllyAvoidanceVector", Random.Range(0.1f, 1.3f), 0.2f);

		Collider[] colliderComps = GetComponentsInChildren<Collider> ();
		for (int i = 0; i < colliderComps.Length; i++) {
			for (int j = 0; j < colliderComps.Length; j++) {
				Physics.IgnoreCollision (colliderComps [i], colliderComps [j]);
			}
		}
		//Debug.Log ("found " + colliderComps.Length + " colliders in mech");

		StartCoroutine(getAvoidanceVec(1 << LayerMask.NameToLayer("Obstacle"), attackDistance, 10000, OBSTACLE));
		StartCoroutine (getAvoidanceVec (1 << LayerMask.NameToLayer ("Enemy"), attackDistance, allyAvoidanceWeight, ALLY));
    }

    void changeStateTo(int newState)
    {
        if (newState == state)
            return;
        if (IsInvoking("checkCanLand"))
            CancelInvoke("checkCanLand");
        if (IsInvoking("goBackToAirWander"))
            CancelInvoke("goBackToAirWander");
        state = newState;
        if (state == WANDER_AIR) {
            wanderTarget.wanderType = MechWanderTarget.AIR;
            wanderTarget.startWanderTarget();
            InvokeRepeating("checkCanLand", 5, 5);
        }
        else if (state == WANDER_GROUND)
        {
            wanderTarget.wanderType = MechWanderTarget.GROUND;
            wanderTarget.startWanderTarget();
        }
        else if (state == LANDING)
        {
            //landingPoint = nearestGroundRef.GetComponent<Collider>().ClosestPointOnBounds(chest.position);
            landingPoint = Util.closestPointOnTransformedBounds(nearestGroundRef.transform, chest.position);
            Vector3 toObjCenter = nearestGroundRef.transform.position - landingPoint;
            toObjCenter.y = 0;
            toObjCenter *= 0.2f;
            landingPoint += toObjCenter;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(landingPoint, out hit, 20, NavMesh.AllAreas))
                landingPoint = hit.position;
            wanderTarget.stopWanderTarget();
        }
        else if (state == WANDER_PLATFORM) {
            Invoke("goBackToAirWander", 15);
            wanderTarget.wanderType = MechWanderTarget.WANDER_PLATFORM;
            wanderTarget.startWanderTarget();  
        }
        else if (state == ATTACK)
            wanderTarget.stopWanderTarget();
    }

    void reAllowNavAgentRePath ()
    {
        repathAllowed = true;
    }

    float getAltitude(Vector3 pos)
    {
        RaycastHit hitInfo;
        bool didHit = Physics.Raycast(pos, Vector3.up * -1, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("Ground"));
        if (didHit)
        {
            return Vector3.Distance(hitInfo.point, pos);
        }
        else if (Physics.Raycast(pos, Vector3.up, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("Ground")))
        {
            return -1 * Vector3.Distance(hitInfo.point, pos);
        }
        return Mathf.Infinity;
    }

    float getAltitudeWithOffset(Vector3 pos, float offset)
    {
        float alt = getAltitude(pos + Vector3.up * offset);
        if (alt == Mathf.Infinity)
        {
            alt = getAltitude(pos);
            return alt;
        }
        return alt - offset;
    }

    public bool checkFacingPlayer()
    {
		if (Mathf.Abs (Vector3.Angle (player.transform.position - transform.position, transform.forward)) <= facingAngle) {
			facingPlayer = true;
			return true;
		} else {
			facingPlayer = false;
			return false;
		}
    }

    bool playerIsInSight()
    {
        if (Mathf.Abs(Vector3.Angle(player.transform.position - transform.position, transform.forward)) <= fov
            && Vector3.Distance(player.transform.position, transform.position) <= sightRange)
            return true;
        else
            return false;
    }

    // Update is called once per frame
    void Update()
    {


		//Debug.Log ("obstacle avoidance vector is " + obstacleAvoidanceVector + " and ally avoidance vector is " + allyAvoidanceVector);

        /*
        if (state == LANDING)
            Debug.Log("LANDING, dist to target is " + Vector3.Distance(transform.position, target) + ", current speed is " + currentSpeed);
        else
            Debug.Log("NOT LANDING, dist to target is " + Vector3.Distance(transform.position, target));
        */

        if (agent.enabled && !agent.isOnNavMesh)
            Debug.Log("agent active but not on navmesh");

        if (agent.enabled)
            Debug.DrawLine(transform.position, agent.destination, Color.red);

        dampenRigidbodyForces();

        if (state == WANDER_PLATFORM)
            stopDistance = 3;
        else
            stopDistance = attackDistance;

        agent.stoppingDistance = stopDistance;

        /***********************************************
        DETERMINE STATE
        ***********************************************/

        bool playerInSight = playerIsInSight();

		if (playerInSight && state != ATTACK)
			changeStateTo(ATTACK);
		else if (!playerInSight && state == ATTACK) {
            /*
			float randFloat = Random.Range(0.0f, 1.0f);
			if (randFloat > 0.5f)
				changeStateTo(WANDER_AIR);
			else
				changeStateTo(WANDER_GROUND);
                */
            //changeStateTo(WANDER_GROUND);
            changeStateTo(WANDER_AIR);
		}

        if (state == LANDING)
            Debug.DrawLine(transform.position, landingPoint, Color.blue);

        if (state == ATTACK)
            target = player.transform.position;
        else if (state == LANDING)
        {
            if (onGround) {
                target = wanderTarget.currentPosition + Vector3.up * chestHeight;
                // check what object the mech landed on, and store it to avoid landing on it twice in a row
                RaycastHit hit;
                Ray ray = new Ray(transform.position, Vector3.down);
                if (Physics.Raycast(ray, out hit, 10, 1 << LayerMask.NameToLayer("Ground"))) {
                    lastObjLandedOn = hit.transform.gameObject;
                    changeStateTo(WANDER_PLATFORM);
                }
            }
            else
                target = landingPoint;
        }
        else if (state == WANDER_PLATFORM)
            target = wanderTarget.currentPosition + Vector3.up * (chestHeight - 5);
        else if (state != WANDER_PLATFORM)
            target = wanderTarget.currentPosition;


        /***********************************************
        DETERMINE SPEED
        ***********************************************/

        float distToTarget = Vector3.Distance(transform.position, target);
        if (onGround && targetAlt <= airModeCutoffAlt) // GROUND SPEED BEHAVIOR
        {
            if (distToTarget > stopDistance)
            {
                currentSpeed = Mathf.Lerp(0, defaultRunSpeed, runSpeedStep);
                runSpeedStep += runSpeedStepIncrement * Time.deltaTime;
            }
            else {
                runSpeedStep = 0;
                currentSpeed = 0;
            }
        }
        else // AIR SPEED BEHAVIOR
        {
            runSpeedStep = 0;
            if (distToTarget > attackDistance && state != LANDING)
            {
				float newSpeed = currentSpeed;//0;
                // set air speed proportional to distance from target
                if (Mathf.Abs(Vector3.Angle(target - transform.position, transform.forward)) <= facingAngle)
                    newSpeed = (distToTarget - attackDistance);

                // slow down if angle between forward direction and target increases
                float angleBetweenHeadingAndTarget = Mathf.Abs(Vector3.Angle(transform.forward, target - transform.position));
                angleBetweenHeadingAndTarget = Mathf.Clamp(angleBetweenHeadingAndTarget, 1, 10);
                newSpeed *= (1.0f / angleBetweenHeadingAndTarget);

                // limit acceleration/deceleration
                currentSpeed = Mathf.MoveTowards(currentSpeed, newSpeed, planeAcceleration);
                // limit max speed when not pursuing player
                if (state == WANDER_AIR)
                    currentSpeed = Mathf.Clamp(currentSpeed, 0, maxWanderSpeed);
            }
            else if (state == LANDING)
            {
                currentSpeed = distToTarget;
                currentSpeed = Mathf.Clamp(currentSpeed, 10, defaultAirSpeed);
            }
        }

        if (!descentMode && state != LANDING && state != WANDER_PLATFORM)
            checkDescentPossible();
        else if (descentMode)
        {
            if (currentSpeed > myAlt * descentRate)
            {
                currentSpeed = myAlt * descentRate * 0.5f;
                //Debug.Log("got here, current speed is now: " + currentSpeed + ", descent speed is " + myAlt * descentRate);
            }
        }

        if (onGround || state == LANDING)
        {
            CancelInvoke("disableDescentMode");
            descentMode = false;
        }

        if (currentSpeed >= planeModeCutoffSpeed2 && !descentMode)
            planeMode = true;
        if (currentSpeed <= planeModeCutoffSpeed1 || myAlt < 12 || state == LANDING)
            planeMode = false;

        if (onGround)
            enableThrusters(false);
        else
            enableThrusters(true);

        airMovement();
        groundMovement();

        anim.SetBool("On Ground", onGround);
        anim.SetFloat("Current Speed", currentSpeed);
        checkFacingPlayer();
        anim.SetBool("Facing Player", facingPlayer);
        anim.SetBool("Plane Mode Enabled", planeMode);

        //Debug.Log ("player alt is " + targetAlt + ", and airmode cutoff alt is " + airModeCutoffAlt);
        //printState ();
        /*
		if (onGround)
			Debug.Log ("GROUND");
		else
			Debug.Log("\t\tair");
			*/
    }

    void LateUpdate()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Aim While Running") || anim.GetCurrentAnimatorStateInfo(0).IsName("Ground Idle")) // if in appropriate animation state for aiming, aim torso at player
        {
            //NOTE: A lot of this local frame stuff (if not all of it) is really only necessary if the mech's up axis is not the same as the world up axis. Since this script has been simplified and the mech
            // no longer aligns itself with the normal vector of the patch of ground it's on, getting stuff in local frame is probably unnecessary now.

            Vector3 targetInLocalFrame = transform.InverseTransformPoint(target); // get target in mech's frame of reference (coordinate system)
            Vector3 spinePosInLocalFrame = transform.InverseTransformPoint(spine.position); // get position of mech's spine in mech's frame
            Vector3 targetGroundProjection = targetInLocalFrame;
            targetGroundProjection.y = 0; // pretent target is on ground plane (level with mech)
            float angle = Vector3.Angle(targetInLocalFrame - spinePosInLocalFrame, targetGroundProjection); // Get angle between vector to target (in mech frame) and vector to target's projection on ground (also in mech frame)
                                                                                                            // So, in other words, get the elevation angle that the mech's torso will have to aim up or down by

            if (angle > 45 || angle < -45) // if the angle would require the mech to look higher than 45 degrees upward or lower than 45 degrees downward, clamp the angle to 45 degrees
            {
                float maxHeight = targetGroundProjection.magnitude * Mathf.Sin(Mathf.Rad2Deg * 45);
                if (targetInLocalFrame.y > 0)
                    targetInLocalFrame = new Vector3(targetInLocalFrame.x, maxHeight, targetInLocalFrame.z);
                else if (targetInLocalFrame.y < 0)
                    targetInLocalFrame = new Vector3(targetInLocalFrame.x, -maxHeight, targetInLocalFrame.z);
                target = transform.TransformPoint(targetInLocalFrame);
            }

            // slerp towards target (don't really want torso to instantly aim at target)
            groundAimVector = Vector3.Slerp(groundAimVector, (target - spine.position).normalized, 10 * Time.deltaTime);
            spine.LookAt(spine.position + groundAimVector, (transform.up + Vector3.up) * 0.5f);
            spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
        }
        else if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Transform To Plane Mode") && myAlt >= 0 && myAlt < 8) // if mech is not in appropriate animation state for aiming (or is flying at low altitude of less than 8 meters), slerp back to mech's regular forward vector
        {
            groundAimVector = Vector3.Slerp(groundAimVector, transform.forward, 10 * Time.deltaTime);
            Vector3 up = (transform.up + Vector3.up) * 0.5f;
            spine.LookAt(spine.position + groundAimVector, up);
            spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
        }
        else {
            groundAimVector = transform.forward;
        }
    }

    void airMovement()
    {

        movementVec = Vector3.zero;

        myAlt = getAltitudeWithOffset(gameObject.transform.position, 8);
        targetAlt = getAltitude(target);

        float lookAheadMult = 200; // was 150, then 300, then 30 (without currentSpeed * Time.deltaTime part)
        Vector3 direc = (target - transform.position).normalized;
        direc.y = 0;
        Vector3 lookAheadPoint = transform.position + direc * lookAheadMult;
        bool goingOffCliff = getAltitudeWithOffset(lookAheadPoint, 8) > 150;

        /*
        if (goingOffCliff)
            Debug.DrawRay(transform.position, Vector3.up * 500, Color.magenta);
            */

        //Debug.DrawLine(transform.position, lookAheadPoint, Color.black);

        // conditions that must be met for this mech to be considered to be on the ground:
        // -- must be within 3 meters of ground (above of below)
        // -- altitude difference between this mech and its target must be less than the cutoff altitude for changing to air mode
        // -- must not be heading off a cliff while travelling towards target
        if (groundAirTransitionAllowed)
        {
            if (myAlt <= 3
                && Mathf.Abs(transform.position.y - target.y) < airModeCutoffAlt
                && !goingOffCliff)
            {
                onGround = true;
                groundAirTransitionAllowed = false;
                Invoke("reAllowGroundAirTransition", 1.5f);
            }
            else
                onGround = false;

            if (goingOffCliff && state != WANDER_PLATFORM)
            {
                onGround = false;
                groundAirTransitionAllowed = false;
                Invoke("reAllowGroundAirTransition", 1.5f);
            }
            

            if (!onGround)
                agent.enabled = false;

        }

        // if the navmesh agent is disabled or if the mech has clipped below the ground, then we want the mech's
        // rigidbody component to not be kinematic (meaning, we DO want it to be stopped by collisions so it will hopefully
        // be pushed back above the terrain or will not go through obstacles that are not being avoided since the navemesh
        // agent is disabled)
        if (!agent.enabled || myAlt < 0)
            rb.isKinematic = false;
        else
            rb.isKinematic = true;

        Vector3 adjustedTargetPos = new Vector3(target.x, transform.position.y, target.z);

        if (descentMode && myAlt != Mathf.Infinity)
        {
            //transform.LookAt(adjustedTargetPos, Vector3.up);

            Quaternion rot = Quaternion.LookRotation((adjustedTargetPos) - rb.position, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, airTurnRate * Time.deltaTime);

            float speed = myAlt * descentRate;
            speed += 20;

            movementVec += Vector3.down * speed;
        }
        else if (state == LANDING)
        {
            Quaternion rot = Quaternion.LookRotation((adjustedTargetPos) - rb.position, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, airTurnRate * Time.deltaTime);
        }
        else if (!onGround && !planeMode)
        {
            Quaternion rot = Quaternion.LookRotation((target + transform.up * -chestHeight) - rb.position, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, airTurnRate * Time.deltaTime);
        }
        else if (!onGround && planeMode)
        {
            
            Vector3 newForward = (target - chest.position).normalized;

            // move dummy transform to chest position, make it face the current forward direction, and then parent this transform to the dummy
            // which effectively changes the center of rotation to the chest (which is better than the normal pivot point, which is near the mech's feet)
            dummyParentForPlaneMode.transform.position = chest.position;
            dummyParentForPlaneMode.transform.rotation = transform.rotation;
            transform.parent = dummyParentForPlaneMode.transform;

            // this will be calculated again later, but whatever
            Vector3 desiredMovementVec = newForward * currentSpeed;
            //desiredMovementVec += obstacleAvoidanceVector * 0.5f;
            //desiredMovementVec += allyAvoidanceVector;

            // get local right (or x) axis (except we don't want just transform.right because that won't necessarily be horizontal)
            Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;

            // project movement vector onto local right axis to find how far left or right the desired movement vector points
            float proj = Vector3.Dot(desiredMovementVec, right);

            // scale projection
            float scaleFac = 50;
            float newZRot = proj * Time.deltaTime * -1 * scaleFac;

            Vector3 up = Vector3.up + right * proj;

            // rotate the parent (which is the dummy transform) to face new heading

            dummyParentForPlaneMode.transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(newForward, transform.up), 0.3f * airTurnRate * Time.deltaTime);
            dummyParentForPlaneMode.transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.forward, up), 0.7f * airTurnRate * Time.deltaTime);

            // then unparent from dummy transform
            transform.parent = null;
        }
    }

    void groundMovement()
    {

        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (onGround && !agent.enabled)
        {
            if (currentSpeed > 0)
                agent.enabled = true;
            else
                agent.enabled = false;
        }
        if (onGround /*&& targetAlt <= airModeCutoffAlt && !planeMode && (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathComplete)*/)
        {
            Vector3 playerPosInLocalFrame = transform.InverseTransformPoint(target);
            Vector3 adjustedPlayerPosInLocalFrame = playerPosInLocalFrame;
            adjustedPlayerPosInLocalFrame.y = 0;
            Vector3 destinationPoint = transform.TransformPoint(adjustedPlayerPosInLocalFrame);
            
            if (repathAllowed && agent.enabled && !agent.pathPending) {
                agent.SetDestination(destinationPoint + allyAvoidanceVector);
                
                if (IsInvoking("reAllowNavAgentRePath"))
                    CancelInvoke("reAllowNavAgentRePath");
                repathAllowed = false;
                Invoke("reAllowNavAgentRePath", 0.8f);
                
            }

            if (currentSpeed <= 0)
                transform.LookAt(destinationPoint);


        }
        agent.speed = currentSpeed;

        //Debug.Log("navagent path pending?: " + agent.pathPending);

        if (!onGround)
            agent.enabled = false;

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Air Stance"))
        {
            savedGroundRot = Quaternion.LookRotation(transform.forward, Vector3.up);
        }
    }

    void reAllowGroundAirTransition()
    {
        groundAirTransitionAllowed = true;
    }


    void FixedUpdate()
    {
        
        if (onGround)
        {
            if (myAlt < 0)
                rb.position = rb.position + transform.up * Mathf.Abs(myAlt);
            else if (myAlt > 0)
                rb.position = rb.position - transform.up * Mathf.Abs(myAlt);
        }
        
        if (state == LANDING)
            movementVec += (target - rb.position).normalized * currentSpeed;
        else {
            movementVec += transform.forward * currentSpeed;
            movementVec += obstacleAvoidanceVector;

            movementVec += allyAvoidanceVector;
        }

        if (!agent.enabled)
            rb.MovePosition(rb.position + movementVec * Time.deltaTime);

        prevRigidbodyPos = rb.position;


    }

    void checkDescentPossible()
    {
        if (targetAlt <= airModeCutoffAlt * 0.5f
            && myAlt > 3
            && myAlt < 500
            && !anim.GetCurrentAnimatorStateInfo(0).IsName("Transform To Plane Mode")
            && Vector3.Distance(target, transform.position) < attackDistance * 2)
        {
            descentMode = true;
            Invoke("disableDescentMode", 7);
        }
    }

    void disableDescentMode()
    {
        descentMode = false;
    }

//    void updateObstacleAvoidanceVector()
//    {
//        obstacleAvoidanceVector = getAvoidanceVec(1 << LayerMask.NameToLayer("Obstacle"), attackDistance, 10000); // weight was 1000, then 6000
//        // if flying at low alt, lower the avoidance vector so that it doesn't come into effect instantly upon leaving the ground
//        if (myAlt < 12)
//        {
//            float adjustedAlt = myAlt;
//            if (adjustedAlt < 0)
//                adjustedAlt = 0;
//            obstacleAvoidanceVector *= adjustedAlt / 12.0f;
//        }
//    }
//
//    void updateAllyAvoidanceVector()
//    {
//        allyAvoidanceVector = getAvoidanceVec(1 << LayerMask.NameToLayer("Enemy"), attackDistance, allyAvoidanceWeight); // weight (last param) was 100
//        // if flying at low alt, lower the avoidance vector so that it doesn't come into effect instantly upon leaving the ground
//        if (myAlt < 12 && myAlt > -12)
//        {
//            float adjustedAlt = myAlt;
//            if (adjustedAlt < 0)
//                adjustedAlt = 0;
//            allyAvoidanceVector *= adjustedAlt / 12.0f;
//        }
//    }

	IEnumerator getAvoidanceVec(int layerMask, float radius, float weight, int avoidanceVecType, bool reduceVectorAtLowAltitude = true)
    {
		while (true) {
			// get colliders of all nearby obstacles (type depends on layerMask)
			Collider[] hitColliders = Physics.OverlapSphere (chest.position, radius, layerMask);
			for (int i = 0; i < hitColliders.Length; i++) {

				Vector3 vecFromObs = Vector3.zero;

				Collider col = hitColliders [i];

				// don't count self or any own extremity as obstacle
				if (col.transform.root.gameObject == gameObject)
					continue;

				// get distance to closest point of this obstacle's collider
				//Vector3 closestPointOnObs = col.ClosestPointOnBounds(chest.transform.position);
				Vector3 closestPointOnObs = Util.closestPointOnTransformedBounds (col.transform, chest.position);
				float distToObs = Vector3.Distance (chest.position, closestPointOnObs);
				// prevent later division by zero (and very small distances)
				if (Mathf.Abs (distToObs) < 0.2f)
					distToObs = 0.2f;

				// check if there is ground between obstacle and this mech, and if so, check if it's below the mech
				// if yes, then disregard this obstacle since there's walkable ground on top of it
				if (state != WANDER_AIR) { // ...but don't do this if mech is in air wandering mode, or else it will smack into platforms from time to time
					RaycastHit hitInfo;
					bool hitGround = Physics.Raycast (chest.position, closestPointOnObs - chest.position, out hitInfo, distToObs, 1 << LayerMask.NameToLayer ("Ground"));
					if (hitGround && Vector3.Distance (chest.position, hitInfo.point) < distToObs && hitInfo.point.y >= closestPointOnObs.y) {
						//Debug.Log ("ground detected over obstacle");
						//continue;
						yield return null;
					}
				}

				// magnitude of vector inversely proportional to distance from closest point on detected obstacle
				vecFromObs += ((chest.position - closestPointOnObs).normalized / distToObs) * weight;

				if (reduceVectorAtLowAltitude) {
					if (myAlt < 12 && myAlt > -12) {
						float adjustedAlt = myAlt;
						if (adjustedAlt < 0)
							adjustedAlt = 0;
						vecFromObs *= adjustedAlt / 12.0f;
					}
				}

				if (avoidanceVecType == OBSTACLE)
					obstacleAvoidanceVector = vecFromObs;
				else if (avoidanceVecType == ALLY)
					allyAvoidanceVector = vecFromObs;

				yield return null;

			}

			yield return null;
		}
        //return vecFromObs * weight;
    }

    void checkCanLand()
    {
    // There's a better way to do this; sort the list of found ground objects using array sort function:
    // https://msdn.microsoft.com/en-us/library/cxt053xf(v=vs.110).aspx
        //... sorting should be based on distance from chest.position. Then just go through and pick the closest one that isn't lastObjLandedOn
        Collider[] hitColliders = Physics.OverlapSphere(chest.position, groundDetectRadius, 1 << LayerMask.NameToLayer("Ground"));
        if (hitColliders.Length <= 0 || hitColliders[0].gameObject.name == "Terrain") {
            return;
        }
        /*
        GameObject currentClosestGroundObj = hitColliders[0].gameObject;
        foreach (Collider col in hitColliders)
        {
            if (Vector3.Distance(col.gameObject.transform.position, chest.position) < Vector3.Distance(currentClosestGroundObj.transform.position, chest.position) && col.gameObject.name != "Terrain")
                currentClosestGroundObj = col.gameObject;
        }
        if (currentClosestGroundObj != lastObjLandedOn)
            nearestGroundRef = currentClosestGroundObj;
        else
            return;
            */

        List<Collider> colList = new List<Collider>();
        foreach (Collider col in hitColliders)
            if (col.gameObject.name != "Terrain") 
                colList.Add(col);

        colList.Sort(compareColsByDistFromSelf);
        //hitColliders.Sort(compareColsByDistFromSelf);
        GameObject currentClosestGroundObj = hitColliders[0].gameObject;

        /*
        Debug.Log("sorted array: ");
        for (int i = 0; i < colList.Count; i++)
            Debug.Log("collider for obj " + colList[i].gameObject.name + " at position " + i + " in array is " + Vector3.Distance(colList[i].gameObject.transform.position, chest.position) + " from mech");
        */

        // go through the sorted list of ground object colliders and find the first (i.e. closest) one that is not the one the mech just landed (or attempted to land) on
        GameObject prevNearestGroundRef = nearestGroundRef;
        foreach (Collider col in colList) {
            if (col.gameObject.transform.position.y < transform.position.y - 10 && col.gameObject != prevNearestGroundRef) {
                nearestGroundRef = col.gameObject;
                break;
            }
        }

        // if a new nearest ground object was not found, don't immediately go back to the old one

        if (!onGround
            && state == WANDER_AIR
            && nearestGroundRef != null
            && nearestGroundRef != prevNearestGroundRef)
        {
            changeStateTo(LANDING);
        }
    }

    int compareColsByDistFromSelf(Collider col1, Collider col2)
    {
        if (col1 == null && col2 != null)
            return -1;
        else if (col1 != null && col2 == null)
            return 1;
        else if (col1 == null && col2 == null)
            return 0;

        float distFromCol1 = Vector3.Distance(col1.gameObject.transform.position, chest.position);
        float distFromCol2 = Vector3.Distance(col2.gameObject.transform.position, chest.position);
        if (distFromCol1 > distFromCol2)
            return 1;
        else if (distFromCol1 < distFromCol2)
            return -1;
        else
            return 0;
    }

    void goBackToAirWander() {
        changeStateTo(WANDER_AIR);
    }

    void enableThrusters(bool state)
    {
        L_footThrust1.gameObject.SetActive(state);
        L_footThrust2.gameObject.SetActive(state);
        L_shinThrust1.gameObject.SetActive(state);
        L_shinthrust2.gameObject.SetActive(state);

        R_footThrust1.gameObject.SetActive(state);
        R_footthrust2.gameObject.SetActive(state);
        R_shinThrust1.gameObject.SetActive(state);
        R_shinthrust2.gameObject.SetActive(state);
    }

    void dampenRigidbodyForces()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void printState()
    {
        Debug.Log("altitude: " + myAlt + ", on ground: " + onGround + ", plane mode: " + planeMode + ", speed: " + currentSpeed + ", descent mode: " + descentMode + ", rigidbody position: " + rb.position);
    }

    /*
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(chest.position - Vector3.up * groundDetectRadius, groundDetectRadius);
    }
	*/

}