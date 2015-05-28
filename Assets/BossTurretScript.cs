using UnityEngine;
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
	float rateOfFire = 2f; // was 0.1f
	float burstFireRate = 0.1f;
	int burstCounter;
	float bulletScaleFactor;
	TerrainCollider terrainCol;

	public float range;
	float bulletSpeed;

	//float horizontalDistance = 0;
	//float verticalDistance = 0;
	float unadjustedElevationAngle = 0;

	const int STANDARD = 0;
	const int CONE = 1;
	const int RANDOM = 2;
	int pattern;
	float patternChangeRate = 5;

	Vector3 target = Vector3.zero; // THE PLAYER'S POSITION (WITH LEAD CALCULATED)

	float boostMult = 1;

	// Use this for initialization
	void Start () {

		pattern = CONE;
		burstCounter = 5;

		//terrainCol = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ();

		boss = transform.parent.parent.gameObject;
		bossInfo = boss.GetComponent<AirBossScript> ();

		range = 500;
		bulletSpeed = 200; // was 100 

		player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = player.GetComponent<PlayerScript> ();
		gunBarrels = transform.GetChild (0).gameObject;
		leftGunOut = gunBarrels.transform.GetChild (0).gameObject.transform;
		rightGunOut = gunBarrels.transform.GetChild (1).gameObject.transform;
		currentBarrelOut = leftGunOut;

		bulletScaleFactor = 12.0f; //4.0f;

		InvokeRepeating ("shoot", Random.Range (0.1f, rateOfFire), rateOfFire);
		//Invoke ("shoot", rateOfFire);
		Invoke ("changePattern", patternChangeRate);
	}
	
	// Update is called once per frame
	void Update () {
		boostMult = 1;
		float leadMult = 0.85f;
		if (Input.GetKey(KeyCode.LeftShift)) {
			boostMult = 3f;
			leadMult = 1;
		}

		if (pattern == CONE)
			leadMult = 1;

		if (pattern != RANDOM) {
			target = LeadCalculator.FirstOrderIntercept (
					currentBarrelOut.position,
					Vector3.zero, //boss.transform.forward * bossInfo.speed * Time.deltaTime,
					bulletSpeed * boostMult *Time.deltaTime,
					player.transform.position,
					//playerInfo.newPos);
					playerInfo.newPos * leadMult);
					//player.transform.forward * playerInfo.defaultForwardSpeed * Time.deltaTime);
		}
		else {
			target = transform.position + new Vector3 (Random.Range(-50, 50),
			                                       Random.Range(-50, 50),
			                                       Random.Range(-50, 50));
		}
		Vector3 targetPosInLocalFrame = transform.InverseTransformPoint (target);
		Vector3 adjustedTargetInWorldFrame = transform.TransformPoint(new Vector3 (targetPosInLocalFrame.x, 0, targetPosInLocalFrame.z));
		transform.LookAt (adjustedTargetInWorldFrame, transform.up);
		
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
		if (!playerInRange() || !clearLOS(gameObject, player, range))
			return;
		// alternate barrels
		if (currentBarrelOut == leftGunOut)
			currentBarrelOut = rightGunOut;
		else
			currentBarrelOut = leftGunOut;

		// choose shoot function based on current shooting pattern
		switch (pattern) {
			case STANDARD:
				standardShoot ();
				break;
			case CONE:
				coneShoot ();
				break;
			case RANDOM:
				randomShoot ();
				break;
		}

		// burst fire
		if (pattern == CONE && burstCounter > 0) {
			Invoke ("shoot", burstFireRate);
			burstCounter--;
		}
		else if (burstCounter <= 0)
			burstCounter = 5;
	}

	void standardShoot() {
		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		bullet.transform.position = currentBarrelOut.position;
		//bullet.transform.rotation = gunBarrels.transform.rotation;
		bullet.transform.LookAt (target);

		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = bulletSpeed * boostMult;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = range * 0.6f;
		
		setScaleAndColorForBullet (bullet, bulletInfo, bulletScaleFactor, Color.white);
	}

	void coneShoot() {
		int numBullets = 10;
		float radius = 30;
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			//bullet.transform.rotation = gunBarrels.transform.rotation;
			bullet.transform.LookAt (target);
			bullet.transform.LookAt (target
			                         + (bullet.transform.right * radius * Mathf.Cos(i * ( Mathf.PI * 2 / numBullets)))
			                         + (bullet.transform.up * radius * Mathf.Sin(i * (Mathf.PI * 2 / numBullets)))
			                         );
		
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = bulletSpeed * 0.75f;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;
		
			setScaleAndColorForBullet (bullet, bulletInfo, 8, Color.magenta);
		}

		// center bullet
		GameObject centerBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		centerBullet.transform.position = currentBarrelOut.position;
		centerBullet.transform.rotation = gunBarrels.transform.rotation;
		EnemyBulletScript centerBulletInfo = centerBullet.GetComponent<EnemyBulletScript>();
		centerBulletInfo.speed = bulletSpeed * 0.75f;
		centerBulletInfo.distanceTraveled = 0;
		centerBulletInfo.maxRange = range * 0.6f;
		
		setScaleAndColorForBullet (centerBullet, centerBulletInfo, 30, Color.magenta);
	}

	void randomShoot() {
		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		bullet.transform.position = currentBarrelOut.position;
		//bullet.transform.rotation = gunBarrels.transform.rotation;
		bullet.transform.LookAt (target);
		
		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = bulletSpeed * boostMult;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = range * 0.6f;
		
		setScaleAndColorForBullet (bullet, bulletInfo, 60, Color.green);
	}

	void setScaleAndColorForBullet(GameObject bullet, EnemyBulletScript bulletInfo, float scaleFac, Color color) {
		bullet.SetActive(true);
		bulletInfo.delayedReactivateTrail();
		
		// resize bullet
		bullet.transform.localScale = Vector3.one * scaleFac;
		TrailRenderer trail = bullet.GetComponent<TrailRenderer> ();
		trail.startWidth = 0.05f * scaleFac;
		trail.endWidth = 0.001f * scaleFac;
		// change bullet color
		trail.material.color = color;
		bullet.renderer.material.color = color;
	}

	float correctedSin(float input) {
		float retval = Mathf.Sin (input);
		if (input > Mathf.PI * 0.5f || input < 0)
			retval *= -1;
		return retval;
	}

	bool playerInRange() {
		if (Vector3.Distance(transform.position, player.transform.position) <= range && unadjustedElevationAngle >= 0)
			return true;
		else
			return false;
	}

	bool clearLOS(GameObject obj1, GameObject obj2, float range) {
		RaycastHit[] hits;
		Vector3 rayDirection = obj2.transform.position - obj1.transform.position;
		hits = Physics.RaycastAll(obj1.transform.position, rayDirection, range);
		if (hits.Length <= 0)
			return false;
		GameObject closest = hits [0].collider.gameObject;
		float distToClosest = Vector3.Distance(obj1.transform.position, closest.transform.position);
		foreach (RaycastHit hit in hits) {
			GameObject current = hit.collider.gameObject;
			if (current == obj1)
				continue;
			float distToCurrent = Vector3.Distance(obj1.transform.position, current.transform.position);
			if (distToCurrent < distToClosest) {
				closest = current;
				distToClosest = distToCurrent;
			}
		}
		if (closest == obj2)
			return true;
		else
			return false;
	}

	void changePattern() {
		burstCounter = 5;
		if (pattern == STANDARD) {
			pattern = CONE;
			rateOfFire = 2f;
		}
		else if (pattern == CONE) {
			pattern = RANDOM;
			rateOfFire = 0.05f;
		}
		else if (pattern == RANDOM) {
			pattern = STANDARD;
			rateOfFire = 0.1f;
		}
		CancelInvoke ();
		InvokeRepeating ("shoot", Random.Range (0.1f, rateOfFire), rateOfFire);
		Invoke ("changePattern", patternChangeRate);
	}
}
