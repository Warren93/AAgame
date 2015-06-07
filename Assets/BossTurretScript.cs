using UnityEngine;
using System.Collections;

public class BossTurretScript : MonoBehaviour {

	GameObject player;
	PlayerScript playerInfo;
	LayerMask playerAndBossLayer;
	public float distToPlayer;
	public int rank;
	public bool wingtipTurret;
	GameObject boss;
	AirBossScript bossInfo;
	GameObject gunBarrels;
	public GameObject bulletPrefab;
	Transform leftGunOut;
	Transform rightGunOut;
	Transform currentBarrelOut;
	float rateOfFire = 1f; // was 0.1f
	float burstFireRate = 0.1f;
	int burstCounter;
	bool canShoot = true;
	float bulletScaleFactor;
	TerrainCollider terrainCol;

	public float range;
	float defaultRange;
	float extendedRange;
	float shorterRange;
	float bulletSpeed;
	float adjustedBulletSpeed;

	//float horizontalDistance = 0;
	//float verticalDistance = 0;
	float unadjustedElevationAngle = 0;

	const int STANDARD = 0;
	const int CONE = 1;
	const int RANDOM = 2;
	const int LINK = 3;
	const int SWEEP = 4;
	const int FLOWER = 5;
	const int TUNNEL = 6;
	int pattern;
	float patternChangeRate = 10;
	float baseAngle = Mathf.PI * 0.5f;
	float baseAngleIncrement = Mathf.PI * 0.05f;
	float variation = 0;
	float variationIncrement = Mathf.PI / 24.0f;
	float sweepAngle = 0;
	float sweepAngleIncrement = Mathf.PI * 0.07f;
	float sweepRange = 100;

	Color orange;
	Color pink;

	Vector3 target = Vector3.zero; // THE PLAYER'S POSITION (WITH LEAD CALCULATED)

	float boostMult = 1;

	// Use this for initialization
	void Start () {

		/*
		this.enabled = false;
		return;
		*/

		distToPlayer = Mathf.Infinity;
		rank = 0;

		orange = new Color (1.0f, 196.0f / 255.0f, 0.0f, 1.0f);
		pink = Color.Lerp (Color.red, Color.magenta, 0.5f); //new Color (1.0f, 102.0f / 255.0f, 184.0f / 255.0f, 1.0f);

		pattern = RANDOM; //FLOWER; // initial
		burstCounter = 5;

		//terrainCol = GameObject.FindGameObjectWithTag ("Ground").GetComponent<TerrainCollider> ();

		boss = transform.parent.parent.gameObject;
		bossInfo = boss.GetComponent<AirBossScript> ();

		range = 500;
		defaultRange = range;
		extendedRange = range; //range * 2;
		shorterRange = range * 0.6f;
		bulletSpeed = 200; // was 100 

		player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = player.GetComponent<PlayerScript> ();
		gunBarrels = transform.GetChild (0).gameObject;
		leftGunOut = gunBarrels.transform.GetChild (0).gameObject.transform;
		rightGunOut = gunBarrels.transform.GetChild (1).gameObject.transform;
		currentBarrelOut = leftGunOut;

		playerAndBossLayer = (1 << player.layer) | (1 << boss.layer);

		bulletScaleFactor = 12.0f; //4.0f;

		//InvokeRepeating ("shoot", 2.5f, rateOfFire);
		//Invoke ("shoot", rateOfFire); // OLD
		Invoke ("changePattern", 4f);
	}

	// Update is called once per frame
	void Update () {

		distToPlayer = Vector3.Distance (transform.position, player.transform.position);

		boostMult = 1;
		float leadMult = 1;
		if (pattern == STANDARD)
			leadMult = 0.85f;

		if (Input.GetKey(KeyCode.LeftShift) && !playerComingTowardTurret()) {
			boostMult = 3f;
			leadMult = 1;
		}

		adjustedBulletSpeed = bulletSpeed * boostMult;

		if (pattern == CONE)
			leadMult = 1;

		if (pattern == FLOWER)
			leadMult = 0.85f;

		if (pattern == SWEEP)
			range = shorterRange;
		else
			range = defaultRange;

		Vector3 playerVelocity = playerInfo.newPos * leadMult;
		if (pattern == TUNNEL)
			playerVelocity = player.transform.forward * playerInfo.newPos.magnitude;

		if (pattern != SWEEP) {
			target = LeadCalculator.FirstOrderIntercept (
					currentBarrelOut.position,
					Vector3.zero, //boss.transform.forward * bossInfo.speed * Time.deltaTime,
					adjustedBulletSpeed * Time.deltaTime,
					player.transform.position,
					//playerInfo.newPos);
					playerVelocity);
					//player.transform.forward * playerInfo.defaultForwardSpeed * Time.deltaTime);
		}
		else {
			//target = transform.position + boss.transform.forward * 50;
			target = transform.position
					+ (boss.transform.right * sweepRange * Mathf.Cos(sweepAngle))
					+ (boss.transform.forward * sweepRange * Mathf.Sin(sweepAngle));
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

		if ((pattern == LINK || pattern == RANDOM) && rank != 0 && rank != 1 && rank != 2)
			canShoot = false;
		else if ((pattern == FLOWER || pattern == TUNNEL) && rank != 0)
			canShoot = false;
		else
			canShoot = true;
		
		if (!canShoot)
			return;

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
			case LINK:
				linkShoot ();
				break;
			case SWEEP:
				sweepShoot ();
				break;
			case FLOWER:
				flowerShoot ();
				break;
			case TUNNEL:
				tunnelShoot ();
				break;
		}

		// burst fire
		if ((pattern == CONE || pattern == LINK || pattern == FLOWER) && burstCounter > 0) {
			Invoke ("shoot", burstFireRate);
			burstCounter--;
		}
		else if (burstCounter <= 0) {
			if (pattern == CONE)
				burstCounter = 5;
			else if (pattern == LINK)
				burstCounter = 8;
			else if (pattern == FLOWER)
				burstCounter = 9;
		}
	}

	void standardShoot() {
		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		bullet.transform.position = currentBarrelOut.position;
		//bullet.transform.rotation = gunBarrels.transform.rotation;
		bullet.transform.LookAt (target);

		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = adjustedBulletSpeed;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = range * 0.6f;
		
		setScaleAndColorForBullet (bullet, bulletInfo, bulletScaleFactor, Color.red);
		bulletInfo.damage = 1;
	}

	void coneShoot() {
		int numBullets = 10;
		float radius = 30;
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			//bullet.transform.rotation = gunBarrels.transform.rotation;
			float currentAngle = i * (Mathf.PI * 2 / numBullets);
			bullet.transform.LookAt (target);
			bullet.transform.LookAt (target
			                         + (bullet.transform.right * radius * Mathf.Cos(currentAngle))
			                         + (bullet.transform.up * radius * Mathf.Sin(currentAngle))
			                         );
		
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = adjustedBulletSpeed;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;
		
			setScaleAndColorForBullet (bullet, bulletInfo, 8, Color.magenta);
		}

		// center bullet
		GameObject centerBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		prepareBullet (centerBullet, adjustedBulletSpeed, range * 0.6f, 30, Color.magenta);
	}

	void randomShoot() {
		float spread = 40;
		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		bullet.transform.position = currentBarrelOut.position;
		//bullet.transform.rotation = gunBarrels.transform.rotation;
		bullet.transform.LookAt (target + new Vector3 (Random.Range(-spread, spread),
		                                               Random.Range(-spread, spread),
		                                               Random.Range(-spread, spread)));
		
		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = adjustedBulletSpeed;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = range * 0.6f;
		
		setScaleAndColorForBullet (bullet, bulletInfo, 60, Color.green);
	}

	void linkShoot() {
		int numBullets = 3;
		float radius = 115; // was 80, then 105
		float spread = 15; // was 20
		float angleIncrement = (2 * Mathf.PI) * 3;
		GameObject newLink = ObjectPoolerScript.objectPooler.getBulletLink();
		BulletLinkScript linkInfo = newLink.GetComponent<BulletLinkScript> ();
		linkInfo.bullets.Clear ();
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			//bullet.transform.rotation = gunBarrels.transform.rotation;
			float currentAngle = /*baseAngle + */ (i * ( Mathf.PI * 2 / numBullets));
			bullet.transform.LookAt (target);
			Vector3 adjustedTarget = target + bullet.transform.right * Mathf.Sin(variation) * spread;
			bullet.transform.LookAt (adjustedTarget
			                         + (bullet.transform.right * radius * Mathf.Cos(currentAngle))
			                         + (bullet.transform.up * radius * Mathf.Sin(currentAngle))
			                         );
			
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = adjustedBulletSpeed;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;

			linkInfo.bullets.Add(bullet);
			linkInfo.prevBulletPositions.Add(bullet.transform.position);
			
			setScaleAndColorForBullet (bullet, bulletInfo, 20, Color.cyan);
		}
		newLink.GetComponent<LineRenderer> ().material.color = Color.cyan;
		linkInfo.bulletSpeed = adjustedBulletSpeed;
		linkInfo.setWidth (1);
		newLink.SetActive (true);
		baseAngle += baseAngleIncrement;
		variation += variationIncrement;
		
		// center bullet
		GameObject centerBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		prepareBullet (centerBullet, adjustedBulletSpeed, range * 0.6f, 30, Color.cyan);
	}

	void sweepShoot() {
		int numBullets = 2;
		float halfSweepHeight = 120; // 35
		GameObject newLink = ObjectPoolerScript.objectPooler.getBulletLink();
		BulletLinkScript linkInfo = newLink.GetComponent<BulletLinkScript> ();
		linkInfo.bullets.Clear ();
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			//bullet.transform.rotation = gunBarrels.transform.rotation;
			float n;
			if (i == 0)
				n = halfSweepHeight;
			else
				n = -halfSweepHeight;
			bullet.transform.LookAt (target);
			bullet.transform.LookAt (target + (bullet.transform.up * n));
			
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = adjustedBulletSpeed;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.75f;

			linkInfo.bullets.Add(bullet);
			linkInfo.prevBulletPositions.Add(bullet.transform.position);
			
			setScaleAndColorForBullet (bullet, bulletInfo, 8, orange);
		}
		newLink.GetComponent<LineRenderer> ().material.color = orange;
		linkInfo.bulletSpeed = adjustedBulletSpeed;
		linkInfo.setWidth (5);
		linkInfo.damage = 10;
		linkInfo.interpolateColor (orange, Color.red, 3f);
		newLink.SetActive (true);
		sweepAngle += sweepAngleIncrement;
	}

	void flowerShoot() {
		int numBullets = 8;
		float radius = 60;
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			//bullet.transform.rotation = gunBarrels.transform.rotation;
			float currentAngle = i * (Mathf.PI * 2 / numBullets) + Mathf.PI * 0.125f;
			bullet.transform.LookAt (target);
			bullet.transform.LookAt ( bullet.transform.position
			                        + (bullet.transform.right * radius * Mathf.Cos(currentAngle))
			                       	+ (bullet.transform.up * radius * Mathf.Sin(currentAngle)) );
			
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = adjustedBulletSpeed * 0.75f;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;
			Vector3 offset = Vector3.zero;
			if (playerComingTowardTurret())
				offset = player.transform.forward * -20;
			bulletInfo.curveTowardPoint(target
			                            + (player.transform.right * Mathf.Sin(variation) * 30)
			                            + offset,
			                            1.5f);
			
			setScaleAndColorForBullet (bullet, bulletInfo, 35, pink);
		}
		variation += variationIncrement;
	}

	void flowerShootInner() {
		if (rank != 0)
			return;
		// center bullet
		GameObject centerBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		prepareBullet (centerBullet, adjustedBulletSpeed, range * 0.6f, 55, Color.blue);
	}

	void tunnelShoot() {
		int numBullets = 8;
		float radius = 45; // was 35
		GameObject newLink = ObjectPoolerScript.objectPooler.getBulletLink();
		BulletLinkScript linkInfo = newLink.GetComponent<BulletLinkScript> ();
		linkInfo.bullets.Clear ();
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			float currentAngle = i * (Mathf.PI * 2 / numBullets) + Mathf.PI * 0.125f + baseAngle;
			bullet.transform.LookAt (target);
			Vector3 targetRight = bullet.transform.right;
			Vector3 lookAtPt = bullet.transform.position
					+ (bullet.transform.right * radius * Mathf.Cos(currentAngle))
					+ (bullet.transform.up * radius * Mathf.Sin(currentAngle));
			Vector3 targetPt = lookAtPt + (target - bullet.transform.position) * range;
			bullet.transform.LookAt ( lookAtPt );
			
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = adjustedBulletSpeed * 0.75f;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;
			bulletInfo.damage = 10;
			bulletInfo.curveTowardPoint(targetPt + (targetRight * Mathf.Sin(variation) * 40.0f), 1.5f);
			
			setScaleAndColorForBullet (bullet, bulletInfo, 35, Color.Lerp(Color.blue, Color.magenta, 0.5f));

			linkInfo.bullets.Add(bullet);
			linkInfo.prevBulletPositions.Add(bullet.transform.position);
			//Debug.Log("bullets count: " + linkInfo.bullets.Count + ", prev pos count: " + linkInfo.prevBulletPositions.Count);
		}
		linkInfo.bulletSpeed = adjustedBulletSpeed * 0.75f;
		linkInfo.setWidth (2);
		linkInfo.damage = 10;
		linkInfo.interpolateColor (Color.blue, Color.magenta, 4.5f);
		newLink.SetActive (true);
		baseAngle += baseAngleIncrement;
		variation += variationIncrement;

		randomShoot ();
	}

	void verticalCurtainShoot() {
		int numBullets = 8;
		float localSpeed = adjustedBulletSpeed * 0.75f;
		float heightIncrement = 10; // was 10 with 8 bullets
		Vector3 localTarget = LeadCalculator.FirstOrderIntercept (
			currentBarrelOut.position,
			Vector3.zero,
			localSpeed * Time.deltaTime,
			player.transform.position,
			playerInfo.newPos * 1.15f);
		for (int i = 0; i < numBullets; i++) {
			GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			bullet.transform.position = currentBarrelOut.position;
			bullet.transform.LookAt (bullet.transform.position + transform.up);
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = localSpeed;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = range * 0.6f;
			bulletInfo.damage = 10;
			Vector3 endpoint = transform.InverseTransformPoint(localTarget);
			endpoint.y = (i + 1) * heightIncrement;
			//bullet.transform.LookAt (transform.TransformPoint(endpoint));
			bulletInfo.curveTowardPoint(transform.TransformPoint(endpoint), 3.0f / (i + 1.0f) );
			
			setScaleAndColorForBullet (bullet, bulletInfo, 55, orange);
		}

		//GameObject centerBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
		//prepareBullet (centerBullet, localSpeed, range * 0.6f, 55, orange);
	}

	void prepareBullet(GameObject bullet, float inputSpeed, float inputRange, float scaleFac, Color color) {
		bullet.transform.position = currentBarrelOut.position;
		bullet.transform.rotation = gunBarrels.transform.rotation;
		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = inputSpeed;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = inputRange;
		setScaleAndColorForBullet (bullet, bulletInfo, scaleFac, color);
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
		if (distToPlayer <= range && unadjustedElevationAngle >= 0)
			return true;
		else
			return false;
	}

	bool clearLOS(GameObject obj1, GameObject obj2, float range) {
		if (pattern == RANDOM || pattern == SWEEP || pattern == FLOWER || pattern == TUNNEL)
			return true;
		RaycastHit[] hits;
		Vector3 rayDirection = obj2.transform.position - obj1.transform.position;
		hits = Physics.RaycastAll(obj1.transform.position, rayDirection, range, playerAndBossLayer);
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

	bool playerComingTowardTurret() {
		if (player == null)
			return false;
		if (Vector3.Angle((transform.position - player.transform.position), playerInfo.newPos) < 90.0f) {
			//Debug.Log ("player coming TOWARD turret");
			return true;
		}
		//Debug.Log ("player going away from turret");
		return false;
	}


	void changePattern() {
		bulletSpeed = 200;
		burstFireRate = 0.1f;
		float timeUntilNextChange = patternChangeRate;
		if (pattern == STANDARD) { // CHANGE TO CONE
			pattern = CONE;
			burstCounter = 5;
			bulletSpeed = 200;
			rateOfFire = 1f;
		}
		else if (pattern == CONE) { // CHANGE TO RANDOM
			pattern = RANDOM;
			bulletSpeed = 130;
			rateOfFire = 0.05f;
			timeUntilNextChange = patternChangeRate * 0.5f;
		}
		else if (pattern == RANDOM) {  // CHANGE TO LINK
			pattern = LINK;
			burstCounter = 8;
			bulletSpeed = 150;
			rateOfFire = 1.6f;
		}
		else if (pattern == LINK) {  // CHANGE TO SWEEP
			pattern = SWEEP;
			bulletSpeed = 150;
			rateOfFire = 0.09f;
		}
		else if (pattern == SWEEP) {  // CHANGE TO FLOWER
			pattern = FLOWER;
			burstCounter = 9;
			bulletSpeed = 200;
			rateOfFire = 1.7f;
			timeUntilNextChange = patternChangeRate * 1.5f;
		}
		else if (pattern == FLOWER) {  // CHANGE TO TUNNEL
			pattern = TUNNEL;
			bulletSpeed = 220;
			rateOfFire = 0.08f;
			timeUntilNextChange = patternChangeRate * 2;
		}
		else if (pattern == TUNNEL) {  // CHANGE TO STANDARD
			pattern = STANDARD;
			bulletSpeed = 170;
			rateOfFire = 0.08f;
			//timeUntilNextChange = patternChangeRate * 10;
		}
		else {
			pattern = STANDARD;
			changePattern();
			return;
		}
		CancelInvoke ();
		InvokeRepeating ("shoot", 0.5f, rateOfFire);
		Invoke ("changePattern", timeUntilNextChange);
		if (pattern == STANDARD && wingtipTurret == true)
			InvokeRepeating("verticalCurtainShoot", 1, 1);
		else if (pattern == FLOWER)
			InvokeRepeating("flowerShootInner", 0.75f, 0.15f);
	}

	void OnDestroy() {
		if (bossInfo && bossInfo.turretsEnabled)
			bossInfo.turretScripts.Remove (this);
	}

}
