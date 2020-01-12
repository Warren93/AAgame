using UnityEngine;
using System.Collections;

public class QuadropedHalfController : MonoBehaviour {


    public Level2BossMovementController bossMovementController;
    Animator animator;
    Vector3 originalLocalPos;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
        originalLocalPos = transform.localPosition;
	}
	
	//// Update is called once per frame
	//void Update () {
	
	//}

    private void OnAnimatorIK()
    {
        //if (GameManagerScript.gamePaused)
        //    return;

        Transform leftTarget = null;
        Transform rightTarget = null;
        if (animator == bossMovementController.FrontAnimator) {
            leftTarget = bossMovementController.FrontLeftIKTarget;
            rightTarget = bossMovementController.FrontRightIKTarget;
        }
        else if (animator == bossMovementController.BackAnimator) { // back half faces backwards
            rightTarget = bossMovementController.BackLeftIKTarget;
            leftTarget = bossMovementController.BackRightIKTarget;
        }
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftTarget.position);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftTarget.rotation);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightTarget.position);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, rightTarget.rotation);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        //transform.localPosition = originalLocalPos;
    }
}
