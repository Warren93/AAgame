using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Level2BossMovementController : MonoBehaviour {

    public Transform FrontLeftFoot, FrontRightFoot, BackLeftFoot, BackRightFoot;
    public Transform FrontHalf;
    public Transform BackHalf;
    public Animator FrontAnimator;
    public Animator BackAnimator;


    Vector3 currentDestination; // dest where body should go, NOT dest on ground
    float strideDistance;
    float speed = 50;
    [HideInInspector]
    public float strideRate = 0.3333f;
    List<Vector3>[] footStepPoints = new List<Vector3>[4];
    [HideInInspector]
    public float frontLeftIKWeight, frontRightIKWeight, backLeftIKWeight, backRightIKWeight;
    float destinationReachedThreshold;

    public Transform frontLeftIKTarget, frontRightIKTarget, backLeftIKTarget, backRightIKTarget;

    enum Foot {
        LeftFront, RightFront, LeftBack, RightBack
    }


    Vector3 frontLeftFootRelativePos;
    Vector3 frontRightFootRelativePos;
    Vector3 backLeftFootRelativePos;
    Vector3 backRightFootRelativePos;

    bool movingFrontLeftFoot, movingFrontRightFoot, movingBackLeftFoot, movingBackRightFoot;

    private void Awake()
    {
        strideDistance = Vector3.Distance(FrontRightFoot.parent.position, BackLeftFoot.position) * 0.8f;
        destinationReachedThreshold = strideDistance / 2;
        changeDestination();
    }

    // Use this for initialization
    void Start () {
        
    }

    void getRelativeFootPositions() {
        frontLeftFootRelativePos = transform.InverseTransformPoint(FrontLeftFoot.position);
        frontRightFootRelativePos = transform.InverseTransformPoint(FrontRightFoot.position);
        backLeftFootRelativePos = transform.InverseTransformPoint(BackLeftFoot.position);
        backRightFootRelativePos = transform.InverseTransformPoint(BackRightFoot.position);
    }

	// Update is called once per frame
	void Update () {
        Vector3 vecToDest = currentDestination - transform.position;
        transform.position += vecToDest.normalized * speed * Time.deltaTime;
        transform.LookAt(currentDestination);

        if (Vector3.Distance(transform.position, currentDestination) < destinationReachedThreshold)
            changeDestination();

        getRelativeFootPositions();

        if (frontLeftFootRelativePos.z < 0)
            setFootDestination(Foot.LeftFront);
        if (frontRightFootRelativePos.z < 0)
            setFootDestination(Foot.RightFront);
        if (backLeftFootRelativePos.z > 2 * strideDistance)
            setFootDestination(Foot.LeftBack);
        if (backRightFootRelativePos.z > 2 * strideDistance)
            setFootDestination(Foot.RightBack);

    }

    void setFootDestination(Foot foot) {
        //Debug.Log("moved foot");
        // this is where you would do stuff
        switch (foot) { // WE WERE MEANT TO LIVE FOR SO MUCH MORE / BUT WE LOST OURSELVES
            case Foot.LeftFront:
                if (!movingFrontLeftFoot) {
                    Debug.Log("front left");
                    movingFrontLeftFoot = true;
                    StartCoroutine(moveFoot(frontLeftIKTarget, FrontLeftFoot, clampToGround(FrontLeftFoot.position + transform.forward * strideDistance), transform.rotation, () => { movingFrontLeftFoot = false; }));
                }
                break;
            case Foot.RightFront:
                if (!movingFrontRightFoot)
                {
                    Debug.Log("front right");
                    movingFrontRightFoot = true;
                    StartCoroutine(moveFoot(frontRightIKTarget, FrontRightFoot, clampToGround(FrontRightFoot.position + transform.forward * strideDistance), transform.rotation, () => { movingFrontRightFoot = false; }));
                }
                break;
            case Foot.LeftBack:
                if (!movingBackLeftFoot)
                {
                    Debug.Log("back left");
                    movingBackLeftFoot = true;
                    StartCoroutine(moveFoot(backLeftIKTarget, BackLeftFoot, clampToGround(BackLeftFoot.position + transform.forward * strideDistance), transform.rotation, () => { movingBackLeftFoot = false; }));
                }
                break;
            case Foot.RightBack:
                if (!movingBackRightFoot)
                {
                    Debug.Log("back right");
                    movingBackRightFoot = true;
                    StartCoroutine(moveFoot(backRightIKTarget, BackRightFoot, clampToGround(BackRightFoot.position + transform.forward * strideDistance), transform.rotation, () => { movingBackRightFoot = false; }));
                }
                break;
        }
    }

    IEnumerator moveFoot(Transform IKTarget, Transform foot, Vector3 targetPosition, Quaternion targetRotation, System.Action onFinishedCallback) {
        float distanceThreshold = 0.1f;
        float angleThreshold = 1;
        float distanceStep = 5;
        float rotationStep = 5;
        while (Vector3.Distance(foot.position, targetPosition) > distanceThreshold || Quaternion.Angle(foot.rotation, targetRotation) > angleThreshold) {
            IKTarget.rotation = Quaternion.Slerp(IKTarget.rotation, targetRotation, rotationStep * Time.deltaTime);
            IKTarget.position = Vector3.Lerp(IKTarget.position, foot.position + transform.forward * strideDistance, distanceStep * Time.deltaTime);
            yield return null;
        }
        onFinishedCallback();
    }

    Vector3 clampToGround(Vector3 pos) {
        RaycastHit hitInfo;
        if (Physics.Raycast(new Ray(pos + Vector3.up * 10000, Vector3.down), out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Ground"))) {
            return hitInfo.point;
        }
        return pos;
    }

    void changeDestination() {
        Vector3 newDest = new Vector3(Random.Range(-3000, 3000), transform.position.y, Random.Range(-3000, 3000));
        currentDestination = newDest;
    }

}
