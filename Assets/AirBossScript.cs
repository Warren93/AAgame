using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AirBossScript : MonoBehaviour {

	public bool turretsEnabled;

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
	public List<BossTurretScript> turretScripts;

	// Use this for initialization
	void Start () {

		/*
		foreach (Transform child in transform)
			child.gameObject.SetActive(false);
		this.enabled = false;
		return;
		*/

		/*
		foreach (Transform child in transform) {
			if (child.gameObject.GetComponent<MeshRenderer>() != null)
				child.gameObject.renderer.enabled = false;
			if (child.tag == "Enemy Flak") {
				child.transform.GetChild(0).renderer.enabled = false;
				child.transform.GetChild(0).transform.GetChild(0).renderer.enabled = false;
			}
		}
		renderer.enabled = false;
		*/

		Vector3 groundExtents = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ().bounds.extents;
		transform.position += transform.right * -0.4f * groundExtents.x;
		transform.position += transform.forward * -0.6f * groundExtents.z;

		state = GOING_STRAIGHT;
		props = new List<GameObject> ();
		turretScripts = new List<BossTurretScript> ();
		for (int i = 0; i < 4; i++)
			props.Add(transform.GetChild(i).gameObject);
		List<Transform> newChildren = new List<Transform> ();
		List<Vector3> newChildPositions = new List<Vector3> ();
		List<Vector3> newChildScales = new List<Vector3> ();
		foreach (Transform child in transform) {
			if (child.tag == "AirBossWingCollider") {
				Transform newChild = (Transform) Instantiate(child, child.position, child.rotation);
				Vector3 currentChildScale = child.localScale;
				Vector3 currentChildLocalPos = child.localPosition;
				Vector3 currentChildRot = child.rotation.eulerAngles;
				currentChildLocalPos.x *= -1;
				currentChildRot.y *= -1;
				currentChildRot.z *= -1;
				newChildPositions.Add(currentChildLocalPos);
				newChildScales.Add(currentChildScale);
				newChild.rotation = Quaternion.Euler(currentChildRot);
				newChildren.Add(newChild);
			}
			else if (child.tag == "Enemy Flak") {
				BossTurretScript script = child.transform.GetChild(0).GetComponent<BossTurretScript>();
				turretScripts.Add(script);
				if (!turretsEnabled) {
					script.gameObject.SetActive(false);
					script.transform.parent.gameObject.SetActive(false);
				}
			}
		}

		for (int i = 0; i < newChildren.Count; i++) {
			newChildren[i].parent = transform;
			newChildren[i].localScale = newChildScales[i];
			newChildren[i].localPosition = newChildPositions[i];
		}

		if (turretsEnabled) {
			// mark which turrets are located on wingtips
			// (ONLY WORKS IF TURRETS ARE ORDERED PROPERLY IN HIERARCHY)
			for (int i = 0; i < turretScripts.Count; i++) {
				if (i == 0 || i == 1 || i ==6 || i ==7)
					turretScripts[i].wingtipTurret = true;
				else
					turretScripts[i].wingtipTurret = false;
			}
		}

		Invoke ("beginTurning", turnFreq * 0.5f);
	}
	
	// Update is called once per frame
	void Update () {
		// this C# sorting technique taken from post by user "GenericTypeTea" on Stack Overflow:
		// http://stackoverflow.com/questions/3309188/how-to-sort-a-listt-by-a-property-in-the-object
		if (turretsEnabled) {
			turretScripts.Sort(
				delegate(BossTurretScript p1, BossTurretScript p2) {
					return p1.distToPlayer.CompareTo(p2.distToPlayer);
				}
			);
			//Debug.DrawRay (turretScripts [0].transform.position, Vector3.down * 1000, Color.magenta);
			for (int i = 0; i < turretScripts.Count; i++)
				turretScripts[i].rank = i;
		}
		

		for (int i = 0; i < 4; i++)
			props[i].transform.RotateAround(props[i].transform.position,
			                                props[i].transform.forward,
			                                defaultPropRotationSpeed * Time.deltaTime);
		executeTurn ();

		//transform.Translate(Vector3.forward * speed * Time.deltaTime);
		//rigidbody.MovePosition (transform.position + (transform.forward * speed * Time.deltaTime));
	}


	void FixedUpdate() {
		GetComponent<Rigidbody>().MovePosition (transform.position + (transform.forward * speed * Time.deltaTime));
	}


	void executeTurn() {
		if (state == TURNING) {
			turnRate = Vector3.Angle(transform.forward, desiredHeading) / remainingTurnTime; // degrees to turn divided by time
			transform.RotateAround (transform.position, Vector3.up, turnRate * Time.deltaTime);
			//rigidbody.MoveRotation(Quaternion.RotateTowards(transform.rotation, Quaternion.FromToRotation(transform.up, transform.right), turnRate * Time.deltaTime));
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

	void beginTurning() {
		desiredHeading = Quaternion.Euler (new Vector3 (0, degreesToTurn, 0)) * transform.forward;
		remainingTurnTime = turnDuration;
		state = TURNING;
		Invoke ("finishTurning", turnDuration);
	}

	void finishTurning() {
		//transform.LookAt (transform.position + transform.forward * 1000);
		transform.LookAt (transform.position + desiredHeading);
		state = GOING_STRAIGHT;
		Invoke ("beginTurning", turnFreq);
	}
}
	