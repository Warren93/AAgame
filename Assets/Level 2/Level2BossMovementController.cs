using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level2BossMovementController : MonoBehaviour {

    public Transform FrontLeftFoot, FrontRightFoot, BackLeftFoot, BackRightFoot;
    Transform[] feet = new Transform[4];

    public Transform FrontHalf;
    public Transform BackHalf;
    public Animator FrontAnimator;
    public Animator BackAnimator;

    public Transform FrontLeftIKTarget, FrontRightIKTarget, BackLeftIKTarget, BackRightIKTarget;
    Transform[] ikTargets = new Transform[4];

    Transform ground;

    Vector3 currentDestination;
    float strideLength = 200;
    float strideSpeed = 67;

    int footIndex = -1;
    Vector3[] footDestinations = new Vector3[4];
    Quaternion[] footTargetRotations = new Quaternion[4];

    private void Start()
    {

        feet[0] = FrontLeftFoot;
        feet[2] = FrontRightFoot;
        feet[3] = BackLeftFoot;
        feet[1] = BackRightFoot;

        ikTargets[0] = FrontLeftIKTarget;
        ikTargets[2] = FrontRightIKTarget;
        ikTargets[3] = BackLeftIKTarget;
        ikTargets[1] = BackRightIKTarget;

        ground = GameObject.FindGameObjectWithTag("Ground").transform;
        currentDestination = getNewDestination();

        step();
        InvokeRepeating("Step", 3, 3);
    }

    void step() {

        footIndex++;
        if (footIndex > 3)
            footIndex = 0;

        for (int i = 0; i < ikTargets.Length; i++) {
            footDestinations[i] = ikTargets[i].position;
            footTargetRotations[i] = Quaternion.LookRotation(currentDestination - transform.position, Vector3.up);
        }

        Vector3 offsetToFoot = ikTargets[footIndex].position - transform.position;
        footDestinations[footIndex] = Vector3.MoveTowards(transform.position + offsetToFoot, currentDestination + offsetToFoot, strideLength);
        RaycastHit hitInfo;
        if (Physics.Raycast(footDestinations[footIndex] + Vector3.up * 10000, Vector3.down, out hitInfo, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle")))) {
            footDestinations[footIndex] = hitInfo.point;
            footTargetRotations[footIndex] = Quaternion.LookRotation(currentDestination - transform.position, hitInfo.normal);
        }
    }

    private void Update()
    {
        float y = transform.position.y;
        Vector3 avgFootPos = Vector3.zero;
        for (int i = 0; i < feet.Length; i++) avgFootPos += feet[i].position;
        avgFootPos /= feet.Length;
        avgFootPos.y = y;
        transform.position = avgFootPos;
        ikTargets[footIndex].position = Vector3.MoveTowards(ikTargets[footIndex].position, footDestinations[footIndex], strideSpeed * Time.deltaTime);
        ikTargets[footIndex].rotation = Quaternion.Slerp(ikTargets[footIndex].rotation, footTargetRotations[footIndex], Time.deltaTime);

        if ( Vector3.Distance(transform.position, currentDestination) < 20)
            currentDestination = getNewDestination();
    }

    Vector3 getNewDestination() {
        Vector3 newDest = new Vector3(
                Random.Range(-ground.localScale.x / 2, ground.localScale.x / 2),
                transform.position.y,
                Random.Range(-ground.localScale.z / 2, ground.localScale.z / 2)
            );

        return newDest;
    }

}
