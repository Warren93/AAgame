using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AirBossScript : MonoBehaviour {

	const int GOING_STRAIGHT = 1;
	const int TURNING = 2;
	int state;

	public float speed;
	public float defaultPropRotationSpeed;
	public float degreesToTurn;
	public float turnFreq;
	public float turnDuration;
	public float bankRate;

	Vector3 desiredHeading;
	float turnRate = 0;
	float remainingTurnTime = 0;

	List<GameObject> props;

	// Use this for initialization
	void Start () {
		state = GOING_STRAIGHT;
		props = new List<GameObject> ();
		for (int i = 0; i < 4; i++)
			props.Add(transform.GetChild(i).gameObject);

		Invoke ("beginTurning", turnFreq);
	}
	
	// Update is called once per frame
	void Update () {
		for (int i = 0; i < 4; i++)
			props[i].transform.RotateAround(props[i].transform.position,
			                                props[i].transform.forward,
			                                defaultPropRotationSpeed * Time.deltaTime);
		if (state == TURNING) {
			turnRate = Vector3.Angle(transform.forward, desiredHeading) / remainingTurnTime; // degrees to turn divided by time
			transform.RotateAround (transform.position, Vector3.up, turnRate * Time.deltaTime);
			//Debug.Log("turning at rate of " + turnRate + " degrees per sec, for " + turnDuration + " seconds");

			// bank
			if (remainingTurnTime >= turnDuration * 0.5f)
				transform.RotateAround(transform.position, transform.forward, -bankRate * Time.deltaTime);
			else
				transform.RotateAround(transform.position, transform.forward, bankRate * Time.deltaTime);

			remainingTurnTime -= Time.deltaTime;
		}
		//Debug.Log ("air boss y is " + transform.position.y);
	}

	void FixedUpdate() {
		rigidbody.MovePosition (transform.position + (transform.forward * speed * Time.deltaTime));
	}

	void beginTurning() {
		desiredHeading = Quaternion.Euler (new Vector3 (0, degreesToTurn, 0)) * transform.forward;
		remainingTurnTime = turnDuration;
		state = TURNING;
		Invoke ("finishTurning", turnDuration);
	}

	void finishTurning() {
		//transform.LookAt (transform.position + transform.forward * 1000);
		state = GOING_STRAIGHT;
		Invoke ("beginTurning", turnFreq);
	}
}
