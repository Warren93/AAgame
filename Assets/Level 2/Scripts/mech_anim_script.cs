using UnityEngine;
using System.Collections;

public class mech_anim_script : MonoBehaviour {

	Animator anim;
	Rigidbody rb;
	float defaultRunSpeed = 12f; // was 1.2 for run anim speed of 0.1
	float currentRunSpeed = 0;
	Transform spine;
	Vector3 vecToTarget;
	GameObject player;

	const int INITIAL = 0;
	const int PREPARE_FOR_AIR_STANCE = 1;
	const int AIR_STANCE = 2;
	const int PLANE_MODE = 3;
	const int AIM_AND_RUN = 4;
	bool transformationInProgress = false; // is the mech in the process of transforming?
	bool movingOnGround = false;
	bool movingOnGroundPrev = false;
	bool isRunning = false; // is the mech currently running (only relevant when mech is on the ground)
	int state = INITIAL; // the current transformation "mode," which is also related to the current animation

	// Use this for initialization
	void Start () {
		Application.targetFrameRate = 60;
		anim = GetComponentInChildren<Animator> ();
		player = GameObject.Find ("dummy player"); //GameObject.FindGameObjectWithTag ("Player");
		rb = GetComponent<Rigidbody> ();
		spine = transform.GetChild(0).GetChild(15).GetChild(0).GetChild(0);
		//Debug.Log ("spine is " + spine.name);
		anim.Play ("Air Stance");
		anim.Play ("Stand");
		anim.Play ("Run Mode");
		//Invoke ("startRunning", 0.5f);
		state = AIM_AND_RUN; //INITIAL;
		InvokeRepeating ("simulateGroundMove", 0.5f, 5.0f);
		//Invoke ("simulateGroundMove", 0.5f);
	}

	void simulateGroundMove() {

		if (movingOnGround == true)
			movingOnGround = false;
		else
			movingOnGround = true;
		//Invoke ("simulateGroundMove", Random.Range(0.1f, 3.0f));
	}

	void startRunning() {
		state = INITIAL;
		anim.CrossFade("Run", 2.0f);
		Invoke ("transformToAirStance", 3.0f);
	}

	/*
	void transformToAirStance() {
		state = AIR_STANCE;
		float t = 0.5f;
		foreach (AnimatorClipInfo clipInfo in anim.GetCurrentAnimatorClipInfo(4)) {
			//Debug.Log("CLIP IS " + clipInfo.clip.name);
			if (clipInfo.clip.name == "Default_Stance") {
				//Debug.Log ("stand detected");
				t = 2.0f;
			}
		}
		anim.CrossFade("No Upright Anim", t);
		anim.CrossFade("Air Stance", t);
		transformationInProgress = false;
		//Invoke ("transformToPlane", 3.0f);
	}
	*/

	void transformToAirStance() {
		state = AIR_STANCE;
		float t = 0.5f;
		if (isClipPlaying ("Default_Stance", 4))
			t = 2.0f;
		anim.CrossFade("No Upright Anim", t);
		anim.CrossFade("Air Stance", t);
		transformationInProgress = false;
	}

	void transformToPlane () {
		state = PLANE_MODE;
		//anim.CrossFade ("Transform To Plane", 0.5f);
		anim.Play("Transform To Plane");
		Invoke ("transformBackToAirStance", 3.0f);
	}

	void transformBackToAirStance() {
		state = AIR_STANCE;
		anim.Play ("Transform To Air Stance");
		Invoke ("transformToUprightRun", 1.0f);
	}

	void transformToUprightRun() {
		state = AIM_AND_RUN;
		if (Random.Range (0, 100) > 100)
			anim.CrossFade ("Run Upright", 0.5f);
		else
			anim.CrossFade ("Stand", 0.5f);
		vecToTarget = transform.forward;
		Invoke ("prepareToTransformToAirStance", 3.0f);
	}

	void prepareToTransformToAirStance() {
		transformationInProgress = true;
		state = PREPARE_FOR_AIR_STANCE;
		Invoke ("transformToAirStance", 1.0f);
	}

	bool OnGround() {
		float planeModeModifier = 1;
		if (state == PLANE_MODE)
			planeModeModifier = 10;
		RaycastHit hitInfo;
		bool didHit = Physics.Raycast(transform.position, transform.up * -1, out hitInfo, 10, 1 << LayerMask.NameToLayer ("Ground"));
		//if (didHit)
		//				Debug.Log ("dist: " + Vector3.Distance (hitInfo.point, transform.position));
		if (didHit && Vector3.Distance(hitInfo.point, transform.position) < 0.5f * planeModeModifier)
			return true;
		return false;
	}

	bool isClipPlaying(string name, int idx) {
		foreach (AnimatorClipInfo clipInfo in anim.GetCurrentAnimatorClipInfo(idx)) {
			if (clipInfo.clip.name == name) {
				return true;
			}
		}
		return false;
	}

	void FixedUpdate() {

		// there should be code here figuring out if the player or ground destination is far enough away
		// that the mech should move towards it (and this code should replace the "simulateGroundMove" thing above)

		if (OnGround() && !transformationInProgress) {
			if (movingOnGround)
				rb.MovePosition(rb.position + transform.forward * defaultRunSpeed * Time.fixedDeltaTime);
		}
		//else if (!OnGround())
		//	Debug.Log("not on ground " + Random.Range(0f, 100f));

	}

	void Update() {
		if (!transformationInProgress && OnGround() && movingOnGround != movingOnGroundPrev) {
			if (movingOnGround) {
				//Debug.Log("started moving");
				if (state == INITIAL)
					anim.CrossFade("Run", 3.0f);
				else if (state == AIM_AND_RUN)
					anim.CrossFade ("Run Upright", 3.0f);
			}
			else {
				//Debug.Log("stopped moving");
				if (state == INITIAL) {
					anim.CrossFade ("Default", 0.5f);
				}
				else if (state == AIM_AND_RUN) {
					anim.CrossFade ("No Upright Anim", 0.5f);
					anim.CrossFade ("Stand", 0.5f);
				}

			}
		}
		else if (!transformationInProgress && !OnGround() && state != AIR_STANCE && state != PLANE_MODE) { // note to self: this should also be based on player's altitude, not just mech's
			Debug.Log("initiating transformation");
			float t = 0.5f;
			if (isClipPlaying ("Default_Stance", 4))
				t = 2.0f;
			anim.CrossFade("No Upright Anim", t);
			anim.CrossFade("Air Stance", t);
			prepareToTransformToAirStance();
		}
		movingOnGroundPrev = movingOnGround;
	}

	// TODO: create function that chooses whether player should be pursued on ground or in air (probably just based on player's height above ground,
	// though possibly also their speed).
	// Also: if player is to be pursued in air, check player's speed to determine whether mech should transform to plane mode.

	void LateUpdate() {
		if (state == AIM_AND_RUN) {
			Vector3 target = player.transform.position;//Camera.main.transform.position;
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
			vecToTarget = Vector3.Slerp(vecToTarget, (target - spine.position).normalized, 10 * Time.deltaTime);
			spine.LookAt(spine.position + vecToTarget);
			spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
		}
		else if (state == PREPARE_FOR_AIR_STANCE /*&& Vector3.Angle(spine.forward, transform.forward) > 2.0f*/) {
			//Debug.Log("GOT HERE!!");
			vecToTarget = Vector3.Slerp(vecToTarget, transform.forward, 10 * Time.deltaTime);
			spine.LookAt(spine.position + vecToTarget);
			spine.RotateAround(spine.position, spine.forward, -90); // correct for Blender axis weirdness
		}
	}
	

}
