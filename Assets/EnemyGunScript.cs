using UnityEngine;
using System.Collections;

public class EnemyGunScript : MonoBehaviour {
	
	float rateOfFire = 0.1f;
	bool canShootThisFrame = true;
	public GameObject bulletPrefab;
	float bulletSpeed = 90;
	GameObject shooter;
	EnemyScript shooterInfo;
	float bulletScaleFactor = 6;
	float shootChance = 80;

	// Use this for initialization
	void Start () {
		shooter = transform.parent.gameObject;
		shooterInfo = shooter.GetComponent<EnemyScript> ();

	}
	
	// Update is called once per frame
	void Update () {
		Vector3 target = shooter.transform.position + (shooter.transform.forward * shooterInfo.currentWeaponRange);
		if (shooterInfo.state == EnemyScript.PURSUE && (int)Random.Range(0, shootChance) == (int)(shootChance * 0.5f) && canShootThisFrame) {
			float overallBulletSpeed = shooterInfo.newPos.magnitude + bulletSpeed;
			GameObject bullet = (GameObject) Instantiate(bulletPrefab,
			                                             transform.position + transform.forward * (shooterInfo.newPos.magnitude / shooterInfo.defaultSpeed),
			                                             Quaternion.LookRotation(target - transform.position,
			                        											shooter.transform.up));
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
