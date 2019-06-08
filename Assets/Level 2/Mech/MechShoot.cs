using UnityEngine;
using System.Collections;

public class MechShoot : MonoBehaviour {

	MechScript3 mechScript;
	public LaserEmitter[] laserEmitters;
	float attackChangeInterval = 30;
	const int MISSILE_ATTACK = 0;
	const int LASER_ATTACK_1 = 1;
	const int LASER_ATTACK_2 = 2;
	const int BULLET_ATTACK = 3;
	int numAttackTypes = 4;
	int attackType = LASER_ATTACK_1;

    int missileAttackProbability = 30;
    int laserAttack1Probability = 15;
    int laserAttack2Probability = 15;
    int bulletAttackProbability = 30;
    int[] probabilityMap = new int[100];

	float missileFireInterval = 1;
	float laserAttack_1_Interval = 5;
	float laserAttack_2_Interval = 10;
	float bulletAttackInterval = 2;
	float bulletBurstInterval = 0.2f;
	float fireInterval = 1;
	Transform target;
	PlayerScript playerInfo;
	public Transform leftGunOut;
	public Transform rightGunOut;
	public Transform leftWingTipGunOut;
	public Transform rightWingTipGunOut;
	public Transform currentProjectileStartPos;
	int cnt = 0;

	// Use this for initialization
	void Start () {

        int idx = 0;
        for (int i = 0; i < missileAttackProbability; i++) {
            probabilityMap[idx] = MISSILE_ATTACK;
            idx++;
        }
        for (int i = 0; i < laserAttack1Probability; i++) {
            probabilityMap[idx] = LASER_ATTACK_1;
            idx++;
        }
        for (int i = 0; i < laserAttack2Probability; i++) {
            probabilityMap[idx] = LASER_ATTACK_2;
            idx++;
        }
        for (int i = 0; i < bulletAttackProbability; i++) {
            probabilityMap[idx] = BULLET_ATTACK;
            idx++;
        }

        attackType = Mathf.RoundToInt(Random.Range (0, numAttackTypes-1));
		//attackType = LASER_ATTACK_2;
		fireInterval = getFireIntervalForAttack (attackType);

		target = GameObject.FindGameObjectWithTag ("Player").transform;
		playerInfo = GameObject.FindGameObjectWithTag ("Player").GetComponent<PlayerScript> ();

		mechScript = GetComponent<MechScript3> ();
		changeAttackType ();
		Invoke ("changeAttackType", attackChangeInterval + Random.Range(-10, 10));
	}
	
	// Update is called once per frame
	/*
	void Update () {
		
	}*/

	void shoot () {
		setGunMuzzlePosition ();
		if (attackType == MISSILE_ATTACK && GameManagerScript.numActiveEnemyMissiles <= 30 && mechScript.checkFacingPlayer ()) {
			//Debug.Log ("AA");
			GameObject missile = ObjectPoolerScript.objectPooler.getMissile ();
			missile.transform.position = currentProjectileStartPos.position;
			missile.transform.LookAt (target);
			missile.SetActive (true);
			missile.GetComponent<EnemyMissile> ().delayedReactivateTrail ();
		}
		else if (attackType == LASER_ATTACK_1) {
			if (laserEmitters.Length > 0)
				laserEmitters [0].fire (target, 0, 3, 1, 0, 1, LaserEmitter.DEFAULT_PATTERN);
		}
		else if (attackType == LASER_ATTACK_2) {
			for (int i = 0; i < laserEmitters.Length; i++) {
				laserEmitters [i].fire (target, i, 3, 1, 0, 1, LaserEmitter.PATTERN_2);
			}
		}
		else if (attackType == BULLET_ATTACK && mechScript.checkFacingPlayer ()) {
			StartCoroutine (shootBulletBurst ());
		}
	}

	IEnumerator shootBulletBurst () {
		for (var i = 0; i < 3; i++) {
			shootBullet ();
			yield return new WaitForSeconds (bulletBurstInterval);
		}
	}

	void shootBullet() {
		//Debug.Log ("AA");
		GameObject bullet = ObjectPoolerScript.objectPooler.getEnemyBullet ();
		bullet.transform.position = currentProjectileStartPos.position;
		float bulletSpeed = 300;
		//float bulletSpeed = playerInfo.newPos.magnitude / Time.fixedDeltaTime * 1.2f; //100;//mechScript.currentSpeed + 40;
		Vector3 targetWithLead = LeadCalculator.FirstOrderIntercept (
			currentProjectileStartPos.position,
			mechScript.movementVec * Time.deltaTime,
			//Vector3.zero,
			bulletSpeed * Time.fixedDeltaTime,
			target.position,
			playerInfo.newPos);
		//targetWithLead += target.transform.forward * -10;
		bullet.transform.LookAt (targetWithLead);

		EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = bulletSpeed;
		CollisionDetectionMode colMode = CollisionDetectionMode.ContinuousDynamic;
		if (bulletInfo.bulletRigidbody.collisionDetectionMode != colMode)
			bulletInfo.setRigidbodyCollisionMode(colMode);
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = 4000;

		bullet.SetActive(true);
		bulletInfo.delayedReactivateTrail();

		// resize bullet
		float bulletScaleFactor = 15;
		bullet.transform.localScale = Vector3.one * bulletScaleFactor;
		TrailRenderer trail = bullet.GetComponent<TrailRenderer> ();
		if (trail) {
			trail.startWidth = 0.05f * bulletScaleFactor;
			trail.endWidth = 0.001f * bulletScaleFactor;
			trail.material.color = Color.red;
		}
		bullet.GetComponent<Renderer>().material.color = Color.red;
	}

	// get muzzle pos based on animation state
	void setGunMuzzlePosition() {
		if (mechScript.planeMode) {
			if (cnt % 2 == 0)
				currentProjectileStartPos = leftWingTipGunOut;
			else
				currentProjectileStartPos = rightWingTipGunOut;
		}
		else {
			if (cnt % 2 == 0)
				currentProjectileStartPos = leftGunOut;
			else
				currentProjectileStartPos = rightGunOut;
		}
		cnt++;
	}

	void changeAttackType () {
        //if (attackType + 1 == LASER_ATTACK_2) {
        //	float r = Random.Range (0.0f, 100.0f);
        //	if (r <= 33.0f) {
        //		attackType += 2;
        //	}
        //}
        //else if (attackType + 1 == LASER_ATTACK_1) {
        //	float r = Random.Range (0.0f, 100.0f);
        //	if (r <= 66.0f) {
        //		attackType += 3;
        //	}
        //}
        //else {
        //	attackType++;
        //}
        //attackType %= numAttackTypes;
        int idx = Mathf.RoundToInt(Random.Range(0, 99));
        attackType = probabilityMap[idx];
        fireInterval = getFireIntervalForAttack (attackType);
		CancelInvoke ();
		InvokeRepeating ("shoot", 0, fireInterval);
		Invoke ("changeAttackType", attackChangeInterval);
	}

	float getFireIntervalForAttack (int attack) {
		if (attack == MISSILE_ATTACK)
			return missileFireInterval;
		else if (attack == LASER_ATTACK_1)
			return laserAttack_1_Interval;
		else if (attack == LASER_ATTACK_2)
			return laserAttack_2_Interval;
		else
			return 1;
	}
}
