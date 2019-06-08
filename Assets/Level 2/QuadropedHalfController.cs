using UnityEngine;
using System.Collections;

public class QuadropedHalfController : MonoBehaviour {


    public Level2BossMovementController bossMovementController;
    Animator animator;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();	
	}
	
	//// Update is called once per frame
	//void Update () {
	
	//}

    private void OnAnimatorIK()
    {
        Transform leftTarget = null;
        Transform rightTarget = null;
        if (animator == bossMovementController.FrontAnimator) {
            leftTarget = bossMovementController.frontLeftIKTarget;
            rightTarget = bossMovementController.frontRightIKTarget;
        }
        else if (animator == bossMovementController.BackAnimator) {
            leftTarget = bossMovementController.backLeftIKTarget;
            rightTarget = bossMovementController.backRightIKTarget;
        }
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftTarget.position);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, rightTarget.rotation);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPosition(AvatarIKGoal.RightFoot, leftTarget.position);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, rightTarget.rotation);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
    }
}
