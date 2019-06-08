using UnityEngine;
using System.Collections;

public class anim_test_script : MonoBehaviour {

	Animator anim;

	// Use this for initialization
	void Start () {
		anim = GetComponentInChildren<Animator> ();
		//AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
		//Debug.Log("state is " + state.shortNameHash);
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.W) || Input.GetKeyDown (KeyCode.S)) {
			if (Input.GetKeyDown (KeyCode.W))
				anim.CrossFade("Move Forward", 0.2f, 0);
			else
				anim.CrossFade("Move Backward", 0.2f, 0);
		}
		else if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.S)) {
			//anim.CrossFade("Idle Animation", 0.2f, 0);
			anim.CrossFade("Attack Stance", 0.2f, 0);
		}
	}
}
