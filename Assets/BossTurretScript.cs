﻿using UnityEngine;
using System.Collections;

public class BossTurretScript : MonoBehaviour {

	GameObject player;
	PlayerScript playerInfo;
	GameObject boss;
	AirBossScript bossInfo;
	GameObject gunBarrels;
	public GameObject bulletPrefab;
	Transform leftGunOut;
	Transform rightGunOut;
	Transform currentBarrelOut;
	float rateOfFire = 0.1f;
	float bulletScaleFactor;
	TerrainCollider terrainCol;

	public float range;
	float bulletSpeed;

	//float horizontalDistance = 0;
	//float verticalDistance = 0;
	float unadjustedElevationAngle = 0;

	// Use this for initialization
	void Start () {

		//terrainCol = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ();

		boss = transform.parent.parent.gameObject;
		bossInfo = boss.GetComponent<AirBossScript> ();

		range = 500;
		bulletSpeed = 100;

		player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = player.GetComponent<PlayerScript> ();
		gunBarrels = transform.GetChild (0).gameObject;
		leftGunOut = gunBarrels.transform.GetChild (0).gameObject.transform;
		rightGunOut = gunBarrels.transform.GetChild (1).gameObject.transform;
		currentBarrelOut = leftGunOut;

		bulletScaleFactor = 4.0f;

		InvokeRepeating ("shoot", Random.Range (0.1f, rateOfFire), rateOfFire);
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 target = LeadCalculator.FirstOrderIntercept (
				currentBarrelOut.position,
				Vector3.zero, //boss.transform.forward * bossInfo.speed * Time.deltaTime,
				bulletSpeed * Time.deltaTime,
				player.transform.position,
				playerInfo.newPos);
		Vector3 targetPosInLocalFrame = transform.InverseTransformPoint (target);
		Vector3 adjustedTargetInWorldFrame = transform.TransformPoint(new Vector3 (targetPosInLocalFrame.x, 0, targetPosInLocalFrame.z));
		//float horizontalAngleToTarget = Mathf.Atan2(targetPosInLocalFrame.x, targetPosInLocalFrame.z);
		// float traversalSpeed = 5;
		//transform.RotateAround (transform.position, transform.up, horizontalAngleToTarget * Time.deltaTime * traversalSpeed);
		transform.LookAt (adjustedTargetInWorldFrame, transform.up);

		//horizontalDistance = (adjustedTargetInWorldFrame - transform.position).magnitude;
		//verticalDistance = targetPosInLocalFrame.y;
		unadjustedElevationAngle = Mathf.Rad2Deg * Mathf.Atan2 (targetPosInLocalFrame.y, targetPosInLocalFrame.z);
		float elevationAngle = unadjustedElevationAngle;
		if (elevationAngle < 0)
			elevationAngle = 0;
		else if (elevationAngle > 75)
			elevationAngle = 75;
		//Debug.Log ("elevation angle is " + elevationAngle);
		gunBarrels.transform.localEulerAngles = new Vector3(-elevationAngle, 0, 0);

	}

	void shoot() {
		if (!playerInRange())
			return;
		// alternate barrels
		if (currentBarrelOut == leftGunOut)
			currentBarrelOut = rightGunOut;
		else
			currentBarrelOut = leftGunOut;

		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		bullet.transform.position = currentBarrelOut.position;
		bullet.transform.rotation = gunBarrels.transform.rotation;
		
		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = bulletSpeed;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = range;
		
		bullet.SetActive(true);
		bulletInfo.delayedReactivateTrail();
		
		// resize bullet
		bullet.transform.localScale = Vector3.one * bulletScaleFactor;
		TrailRenderer trail = bullet.GetComponent<TrailRenderer> ();
		trail.startWidth = 0.05f * bulletScaleFactor;
		trail.endWidth = 0.001f * bulletScaleFactor;

	}

	bool playerInRange() {
		if (Vector3.Distance(transform.position, player.transform.position) <= range && unadjustedElevationAngle >= 0)
			return true;
		else
			return false;
	}
}
