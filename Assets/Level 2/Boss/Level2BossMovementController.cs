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
        currentDestination = new Vector3(-2000, 0, -2000);//getNewDestination();

        step();
        InvokeRepeating("step", 3, 3);
    }

    void step() {

        Quaternion originalRot = transform.rotation;
        transform.rotation = Quaternion.LookRotation(currentDestination - transform.position, Vector3.up);

        footIndex++;
        if (footIndex > 3)
            footIndex = 0;

        Vector3 footFwd = feet[footIndex].position - transform.position;
        footFwd.y = 0;

        for (int i = 0; i < ikTargets.Length; i++) {
            footDestinations[i] = ikTargets[i].position;
            footTargetRotations[i] = Quaternion.LookRotation(footFwd, Vector3.up);
        }

        Vector3 offsetToFoot = ikTargets[footIndex].position - transform.position;
        footDestinations[footIndex] = Vector3.MoveTowards(transform.position + offsetToFoot, currentDestination + offsetToFoot, strideLength);
        RaycastHit hitInfo;
        if (Physics.Raycast(footDestinations[footIndex] + Vector3.up * 100000, Vector3.down, out hitInfo, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle")))) {
            footDestinations[footIndex] = hitInfo.point;
            footTargetRotations[footIndex] = Quaternion.LookRotation(footFwd, hitInfo.normal);
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
        Vector3 destinationLocalPos = transform.InverseTransformPoint(currentDestination);
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
        var newRot = Quaternion.LookRotation(currentRotFwd, Vector3.up);



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
        ikTargets[footIndex].position = Vector3.MoveTowards(ikTargets[footIndex].position, footDestinations[footIndex], strideSpeed * Time.deltaTime);
        RaycastHit hitInfo;
        if (Physics.Raycast(ikTargets[footIndex].position + Vector3.up * 100000, Vector3.down, out hitInfo, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Obstacle"))))
        {
            ikTargets[footIndex].position = hitInfo.point;
            ikTargets[footIndex].rotation = Quaternion.LookRotation(ikTargets[footIndex].forward, hitInfo.normal);
            //if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle")) {
            //    Vector3 hitOffsetFlat = transform.position + (transform.position - hitInfo.point);
            //    hitOffsetFlat.y = 0;
            //    transform.position += Vector3.MoveTowards(transform.position, hitOffsetFlat, 20 * Time.deltaTime);
            //}
        }
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

    float getFlatDist(Vector3 a, Vector3 b) {
        Vector2 a2d = new Vector2(a.x, a.z);
        Vector2 b2d = new Vector2(b.x, b.z);
        return Vector2.Distance(a2d, b2d);
    }

}
