using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class Level2BossMovementController : MonoBehaviour {

    public Transform FrontLeftFoot, FrontRightFoot, BackLeftFoot, BackRightFoot;
    Transform[] feet = new Transform[4];

    public Transform FrontHalf;
    public Transform BackHalf;
    public Animator FrontAnimator;
    public Animator BackAnimator;

    public Transform FrontLeftIKTarget, FrontRightIKTarget, BackLeftIKTarget, BackRightIKTarget;
    Transform[] ikTargets = new Transform[4];
    Vector3 currentFootStartPosition;

    Transform ground;

    BossWaypoint currentWpt;
    BossWaypoint prevWpt = null;
    float strideLength = 200;
    float strideSpeed = 75; // was 67
    float maxStepPositionHeight = 400;
    float footPlacementOffset = 135;
    float maxFootRaiseAmount = 200;
    float maxFootPlacementError = 20;
    float waypointRadius = 50;

    int footIndex = -1;
    Vector3[] footDestinations = new Vector3[4];
    Quaternion[] footTargetRotations = new Quaternion[4];

    float originalHeightAboveGround;

    private void Start()
    {

        originalHeightAboveGround = transform.position.y;

        feet[0] = FrontLeftFoot;
        feet[2] = FrontRightFoot;
        feet[3] = BackLeftFoot;
        feet[1] = BackRightFoot;

        ikTargets[0] = FrontLeftIKTarget;
        ikTargets[2] = FrontRightIKTarget;
        ikTargets[3] = BackLeftIKTarget;
        ikTargets[1] = BackRightIKTarget;

        foreach (var tgt in ikTargets) tgt.parent = null;

        ground = GameObject.FindGameObjectWithTag("Ground").transform;
        //currentDestination = new Vector3(-2000, 0, -2000);//getNewDestination();

        var wpts = GameObject.FindObjectsOfType<BossWaypoint>();
        var closestWpt = Util.getClosest(transform.position, wpts);
        currentWpt = closestWpt;

        step();
        //InvokeRepeating("step", 3, 3);
    }

    void step() {

        Quaternion originalRot = transform.rotation;
        transform.rotation = Quaternion.LookRotation(currentWpt.transform.position - transform.position, Vector3.up);

        footIndex++;
        if (footIndex > 3)
            footIndex = 0;

        Vector3 footFwd = feet[footIndex].position - transform.position;
        footFwd.y = 0;

        for (int i = 0; i < ikTargets.Length; i++) {
            footDestinations[i] = ikTargets[i].position;
            footTargetRotations[i] = Quaternion.LookRotation(footFwd, Vector3.up);
        }

        // set new foot step position for the current foot
        Vector3 offsetToFoot = ikTargets[footIndex].position - transform.position;
        var relativeFootPosCurrent = transform.position + offsetToFoot;
        var relativeFootPosDestination = currentWpt.transform.position + offsetToFoot;
        currentFootStartPosition = relativeFootPosCurrent;
        footDestinations[footIndex] = Vector3.MoveTowards(relativeFootPosCurrent, relativeFootPosDestination, strideLength);


        RaycastHit hitInfo;
        if (Physics.Raycast(footDestinations[footIndex] + Vector3.up * 100000, Vector3.down, out hitInfo, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle")))) {
            var pt = hitInfo.point;
            var normal = hitInfo.normal;
            if (pt.y > maxStepPositionHeight && Physics.Raycast(new Vector3(transform.position.x, maxStepPositionHeight, transform.position.z), transform.position - pt, out hitInfo, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle"))))
            {
                pt = hitInfo.point;
                pt.y = 0;
                normal = hitInfo.normal;
                //Debug.LogWarning("STEP POSITION TOO HIGH, GOING TO " + pt);
            }
            footDestinations[footIndex] = pt;
            footTargetRotations[footIndex] = Quaternion.LookRotation(footFwd, normal);
        }
        else {
            Debug.Log("Raycast failed");
        }

        transform.rotation = originalRot;
    }

    private void Update()
    {
        if (GameManagerScript.gamePaused)
            return;

        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentDestination - transform.position, Vector3.up), Time.deltaTime * 0.2f);

        // the calculation of newRot is probably going to have to be reworked...or at least tested some more...
        Vector3 front = Vector3.Lerp(FrontLeftFoot.position, FrontRightFoot.position, 0.5f);
        Vector3 back = Vector3.Lerp(BackLeftFoot.position, BackRightFoot.position, 0.5f);
        Vector3 left = Vector3.Lerp(FrontLeftFoot.position, BackRightFoot.position, 0.5f);
        Vector3 right = Vector3.Lerp(FrontRightFoot.position, BackLeftFoot.position, 0.5f);
        Vector3 currentRotFwd = Vector3.zero;
        Vector3 destinationLocalPos = transform.InverseTransformPoint(currentWpt.transform.position);
        float absX = Mathf.Abs(destinationLocalPos.x);
        float absZ = Mathf.Abs(destinationLocalPos.z);
        float maxCoord = Mathf.Max(absX, absZ);
        if (maxCoord == absX) {
            if (destinationLocalPos.x >= 0)
                currentRotFwd = Quaternion.AngleAxis(-90, Vector3.up) * (right - transform.position);
            else
                currentRotFwd = Quaternion.AngleAxis(90, Vector3.up) * (left - transform.position);
        }
        else if (maxCoord == absZ) {
            if (destinationLocalPos.z >= 0)
                currentRotFwd = front - transform.position;
            else
                currentRotFwd = transform.position - back;
        }
        currentRotFwd.y = 0;
        Debug.DrawRay(transform.position, currentRotFwd, Color.red, 1.0f);
        var newRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(currentRotFwd, Vector3.up), Time.deltaTime);



        Vector3[] originalFootPositions = new Vector3[4];
        for (int i = 0; i < 4; i++) originalFootPositions[i] = transform.InverseTransformPoint(feet[i].position);
        transform.rotation = newRot;
        for (int i = 0; i < 4; i++) feet[i].position = transform.TransformPoint(originalFootPositions[i]);

        float y = transform.position.y;
        Vector3 avgFootPos = Vector3.zero;
        for (int i = 0; i < feet.Length; i++) avgFootPos += feet[i].position;
        avgFootPos /= feet.Length;
        avgFootPos.y += originalHeightAboveGround;
        transform.position = Vector3.MoveTowards(transform.position, avgFootPos, 80 * Time.deltaTime);

        // figure out how much the foot should be raised
        float normalizedCurrentStrideDistance = Util.getFlatDist(ikTargets[footIndex].position, footDestinations[footIndex]) / Util.getFlatDist(currentFootStartPosition, footDestinations[footIndex]);
        var footRaiseAmount = Mathf.Sin(normalizedCurrentStrideDistance * Mathf.PI) * maxFootRaiseAmount;


        // move foot
        ikTargets[footIndex].position = Vector3.MoveTowards(ikTargets[footIndex].position, footDestinations[footIndex] + Vector3.up * footRaiseAmount, strideSpeed * Time.deltaTime);
        
        RaycastHit hitInfo;
        if (Physics.Raycast(ikTargets[footIndex].position, ikTargets[footIndex].up * -1, out hitInfo, 10, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle"))))
        {
            var normal = hitInfo.normal;
            ikTargets[footIndex].position += normal * footPlacementOffset;
            ikTargets[footIndex].rotation = Quaternion.Slerp(ikTargets[footIndex].rotation, Quaternion.LookRotation(ikTargets[footIndex].forward, normal), Time.deltaTime);
        }
        ikTargets[footIndex].rotation = Quaternion.Slerp(ikTargets[footIndex].rotation, footTargetRotations[footIndex], Time.deltaTime);

        // start next step
        if (Util.getFlatDist(ikTargets[footIndex].position, footDestinations[footIndex]) <= maxFootPlacementError) 
        {
            Debug.Log("Boss completed step");
            step();
        }

        if (Util.getFlatDist(transform.position, currentWpt.transform.position) < waypointRadius)
        {
            var tmpWpt = currentWpt;
            currentWpt = currentWpt.GetNextWaypoint(prevWpt);
            prevWpt = tmpWpt;
            Debug.Log("Boss reached waypoint!");
        }
    }

    

    void OnDrawGizmos()
    {
        if (EditorApplication.isPlaying && currentWpt != null)
            Gizmos.DrawSphere(currentWpt.transform.position, 100);
    }

}
