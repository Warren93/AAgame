using UnityEngine;
using System.Collections;

public class PlayerGunScript : MonoBehaviour {

	//float gunLength;
	Transform gunOutTransform;
	float rateOfFire = 0.05f;
	bool canShootThisFrame = true;
	public GameObject bulletPrefab;
	float bulletSpeed = 800;
	GameObject player;
	PlayerScript playerInfo;
	float bulletScaleFactor = 1;

	// Use this for initialization
	void Start () {
		//gunLength = GetComponent<MeshFilter> ().mesh.bounds.size.magnitude;
		gunOutTransform = transform.GetChild (0);
		player = transform.parent.gameObject;
		playerInfo = player.GetComponent<PlayerScript> ();

		bulletScaleFactor = 3;

	}
	
	// Update is called once per frame
	void Update () {
		if (GameManagerScript.gamePaused)
			return;

		Vector3 target = player.transform.position + (player.transform.forward * playerInfo.currentWeaponRange);
		if (Input.GetMouseButton (0) && canShootThisFrame) {
			float overallBulletSpeed = playerInfo.forwardSpeed + bulletSpeed;
			/*
			GameObject bullet = (GameObject) Instantiate(bulletPrefab,
			                                             transform.position + transform.forward * (gunLength + 3.5f), //* (playerInfo.forwardSpeed / playerInfo.defaultForwardSpeed),
			                                             Quaternion.LookRotation(target - transform.position,
			                        											player.transform.up));
			*/

			GameObject bullet = ObjectPoolerScript.objectPooler.getPlayerBullet();
			//bullet.transform.position = transform.position + transform.forward * (gunLength + 3.5f);
			bullet.transform.position = gunOutTransform.position;
			bullet.transform.rotation = Quaternion.LookRotation(target - transform.position,
			                                                    player.transform.up);

			PlayerBulletScript bulletInfo = bullet.GetComponent<PlayerBulletScript>();
			bulletInfo.speed = overallBulletSpeed;
			bulletInfo.distanceTraveled = 0;
			bulletInfo.maxRange = playerInfo.currentWeaponRange;

			bullet.SetActive(true);
			//bulletInfo.delayedReactivateTrail();

			// resize bullet
			bullet.transform.localScale = Vector3.one * bulletScaleFactor;
			//LineRenderer trail = bullet.GetComponent<LineRenderer> ();

			canShootThisFrame = false;
			Invoke("reAllowShooting", rateOfFire);
		}
	}

	void reAllowShooting() {
		canShootThisFrame = true;
	}
}
