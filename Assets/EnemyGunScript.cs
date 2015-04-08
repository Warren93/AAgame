using UnityEngine;
using System.Collections;

public class EnemyGunScript : MonoBehaviour {
	
	float rateOfFire = 0.1f;
	bool canShootThisFrame = true;
	public GameObject bulletPrefab;
	float bulletSpeed = 40;
	EnemyScript shooterInfo;
	float bulletScaleFactor = 6;
	float shootChance = 20;
	Vector3 gun1Pos;
	Vector3 gun2Pos;

	// Use this for initialization
	void Start () {
		shooterInfo = GetComponent<EnemyScript> ();
		gun1Pos = transform.GetChild (0).localPosition;
		gun2Pos = transform.GetChild (1).localPosition;
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 target = transform.position + (transform.forward * shooterInfo.currentWeaponRange);
		// if player is within 80 degrees of forward vector, go straight at the player
		if (Vector3.Angle((shooterInfo.player.transform.position - transform.position), (target - transform.position)) <= 80)
			target = shooterInfo.player.transform.position;
		if (shooterInfo.state == EnemyScript.PURSUE && (int)Random.Range(0, shootChance) == (int)(shootChance * 0.5f) && canShootThisFrame) {
			float overallBulletSpeed = shooterInfo.newPos.magnitude + bulletSpeed;
			Vector3 shootPos;
			if ((int)Random.Range(0, 100) > 50)
				shootPos = transform.position + (transform.rotation * gun1Pos);
			else
				shootPos = transform.position + (transform.rotation * gun2Pos);
			GameObject bullet = (GameObject) Instantiate(bulletPrefab,
			                                             shootPos + transform.forward * (shooterInfo.newPos.magnitude / shooterInfo.defaultSpeed),
			                                             Quaternion.LookRotation(target - transform.position,
			                        											transform.up));
			EnemyBulletScript bulletInfo = bullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = overallBulletSpeed;
			bulletInfo.maxRange = shooterInfo.currentWeaponRange;

			// resize bullet
			bullet.transform.localScale *= bulletScaleFactor;
			TrailRenderer trail = bullet.GetComponent<TrailRenderer> ();
			trail.startWidth *= bulletScaleFactor;
			trail.endWidth *= bulletScaleFactor;

			canShootThisFrame = false;
			Invoke("reAllowShooting", rateOfFire);
		}
	}

	void reAllowShooting() {
		canShootThisFrame = true;
	}
}
