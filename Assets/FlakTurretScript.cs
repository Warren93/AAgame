using UnityEngine;
using System.Collections;

public class FlakTurretScript : MonoBehaviour {

	GameObject player;
	PlayerScript playerInfo;
	GameObject gunBarrels;
	public GameObject flakBulletPrefab;
	Transform leftGunOut;
	Transform rightGunOut;
	Transform currentBarrelOut;
	float rateOfFire = 1.0f;
	float bulletScaleFactor;
	TerrainCollider terrainCol;

	float range;
	float ceiling;
	public float bulletSpeed;

	float horizontalDistance = 0;
	float verticalDistance = 0;
	float unadjustedElevationAngle = 0;

	// Use this for initialization
	void Start () {

		terrainCol = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ();

		range = 400;
		ceiling = 150;
		bulletSpeed = 400;

		player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = player.GetComponent<PlayerScript> ();
		gunBarrels = transform.GetChild (0).gameObject;
		leftGunOut = gunBarrels.transform.GetChild (0).gameObject.transform;
		rightGunOut = gunBarrels.transform.GetChild (1).gameObject.transform;
		currentBarrelOut = leftGunOut;

		bulletScaleFactor = 7.0f; // was 4

		InvokeRepeating ("shoot", Random.Range (0.1f, rateOfFire), rateOfFire);
	}
	
	// Update is called once per frame
	void Update () {

		//Vector3 target = player.transform.position;
		//target += 0.1f * Vector3.Distance(transform.position, player.transform.position) * playerInfo.newPos.normalized;

		Vector3 target = LeadCalculator.FirstOrderIntercept (
				transform.position,
				Vector3.zero,
				bulletSpeed * Time.deltaTime,
				player.transform.position,
				playerInfo.newPos);

		//Vector3 targetPosInLocalFrame = transform.rotation * (target);
		Vector3 targetPosInLocalFrame = transform.InverseTransformPoint (target);
		Vector3 adjustedTargetInWorldFrame = transform.TransformPoint(new Vector3 (targetPosInLocalFrame.x, 0, targetPosInLocalFrame.z));
		//float horizontalAngleToTarget = Mathf.Atan2(targetPosInLocalFrame.x, targetPosInLocalFrame.z);
		// float traversalSpeed = 5;
		//transform.RotateAround (transform.position, transform.up, horizontalAngleToTarget * Time.deltaTime * traversalSpeed);
		transform.LookAt (adjustedTargetInWorldFrame, transform.up);

		horizontalDistance = (adjustedTargetInWorldFrame - transform.position).magnitude;
		verticalDistance = targetPosInLocalFrame.y;

		//Debug.Log ("Y: " + targetPosInLocalFrame.y + ", X: " + targetPosInLocalFrame.z);
		unadjustedElevationAngle = Mathf.Rad2Deg * Mathf.Atan2 (targetPosInLocalFrame.y, targetPosInLocalFrame.z);
		float elevationAngle = unadjustedElevationAngle;
		if (elevationAngle < 0)
			elevationAngle = 0;
		else if (elevationAngle > 75)
			elevationAngle = 75;
		//Debug.Log ("elevation angle is " + elevationAngle);
		gunBarrels.transform.localEulerAngles = new Vector3(-elevationAngle, 0, 0);
		//Vector3 adjustedTargetForBarrels = transform.TransformPoint(new Vector3 (targetPosInLocalFrame.x, yCoord, targetPosInLocalFrame.z));
		//gunBarrels.transform.LookAt (adjustedTargetForBarrels, transform.up);

	}

	void shoot() {
		if (!playerInRange())
			return;
		//Debug.Log ("SHOT FLAK");
		GameObject flakBullet = (GameObject) Instantiate(flakBulletPrefab, currentBarrelOut.position, gunBarrels.transform.rotation);
		// alternate barrels
		if (currentBarrelOut == leftGunOut)
			currentBarrelOut = rightGunOut;
		else
			currentBarrelOut = leftGunOut;

		EnemyFlakBulletScript bulletInfo = flakBullet.GetComponent<EnemyFlakBulletScript>();
		bulletInfo.terrainCol = terrainCol;
		bulletInfo.speed = bulletSpeed;
		bulletInfo.maxRange = range;
		bulletInfo.player = player;
		
		// resize bullet
		flakBullet.transform.localScale *= bulletScaleFactor;
		TrailRenderer trail = flakBullet.GetComponent<TrailRenderer> ();
		trail.startWidth *= bulletScaleFactor;
		trail.endWidth *= bulletScaleFactor;
		trail.material.color = Color.yellow;
		flakBullet.renderer.material.color = Color.yellow;

	}

	bool playerInRange() {
		if (horizontalDistance <= range && verticalDistance <= ceiling && unadjustedElevationAngle >= 0)
			return true;
		else
			return false;
	}
}
