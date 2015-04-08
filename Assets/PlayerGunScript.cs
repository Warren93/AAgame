using UnityEngine;
using System.Collections;

public class PlayerGunScript : MonoBehaviour {

	float gunLength;
	float rateOfFire = 0.1f;
	bool canShootThisFrame = true;
	public GameObject bulletPrefab;
	float bulletSpeed = 800;
	GameObject player;
	PlayerScript playerInfo;
	float bulletScaleFactor = 1;

	// Use this for initialization
	void Start () {
		gunLength = GetComponent<MeshFilter> ().mesh.bounds.size.magnitude;
		player = transform.parent.gameObject;
		playerInfo = player.GetComponent<PlayerScript> ();

		bulletScaleFactor = 3;

	}
	
	// Update is called once per frame
	void Update () {
		Vector3 target = player.transform.position + (player.transform.forward * playerInfo.currentWeaponRange);
		if (Input.GetMouseButton (0) && canShootThisFrame) {
			float overallBulletSpeed = playerInfo.forwardSpeed + bulletSpeed;
			GameObject bullet = (GameObject) Instantiate(bulletPrefab,
			                                             transform.position + transform.forward * (gunLength + 3) * (playerInfo.forwardSpeed / playerInfo.defaultForwardSpeed),
			                                             Quaternion.LookRotation(target - transform.position,
			                        											player.transform.up));
			PlayerBulletScript bulletInfo = bullet.GetComponent<PlayerBulletScript>();
			bulletInfo.speed = overallBulletSpeed;
			bulletInfo.maxRange = playerInfo.currentWeaponRange;

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
