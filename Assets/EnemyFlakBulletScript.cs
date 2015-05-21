using UnityEngine;
using System.Collections;

public class EnemyFlakBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	float distanceTraveled = 0;
	public GameObject HitEffectPrefab;
	public GameObject subBulletPrefab;
	public GameObject player;
	public TerrainCollider terrainCol;
	float subBulletSpeed = 30;
	float subBulletRange = 15;

	// Update is called once per frame
	void Update () {
		if (distanceTraveled >= maxRange || Vector3.Distance(player.transform.position, transform.position) < 30) {
			createBulletExplosion();
			Destroy(gameObject);
		}
		//Debug.DrawRay(transform.position, transform.forward * 50, Color.cyan);
	}

	void FixedUpdate() {
		//Debug.Log ("speed is " + speed);
		Vector3 oldWorldPos = transform.position;
		Vector3 newPos = transform.forward * speed;
		rigidbody.MovePosition(transform.position + (newPos * Time.deltaTime));
		Vector3 newWorldPos = transform.position + (newPos * Time.deltaTime);
		distanceTraveled += Mathf.Abs(newWorldPos.magnitude - oldWorldPos.magnitude);

		//look ahead to see if going to hit something, because collision detection is apparently spotty otherwise...
		RaycastHit hitInfo;
		Ray ray = new Ray (transform.position, transform.forward);
		bool didHit = terrainCol.Raycast (ray,
		                               out hitInfo,
		                               speed * Time.deltaTime);
		if (didHit)
			collisionFunction(hitInfo.collider);

	}

	void createBulletExplosion() {
		PlayerScript playerInfo = player.GetComponent<PlayerScript> ();
		//Vector3 target = 2f * Vector3.Distance(transform.position, player.transform.position) * playerInfo.newPos.normalized;
		Vector3 target = LeadCalculator.FirstOrderIntercept (transform.position,
		                                                     Vector3.zero,
		                                                     subBulletSpeed,
		                                                     player.transform.position,
		                                                     playerInfo.newPos);
		for (int i = 0; i < 3; i++) {
			float noise = 15;
			Vector3 target2 = target + new Vector3(Random.Range(-noise, noise), Random.Range(-noise, noise), Random.Range(-noise, noise));
			/*
			GameObject subBullet = (GameObject) Instantiate(subBulletPrefab, transform.position,
			                                                Quaternion.LookRotation(target2 - transform.position));
			*/


			GameObject subBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			subBullet.transform.position = transform.position;
			subBullet.transform.rotation = Quaternion.LookRotation(target2 - transform.position);

			prepareBullet(subBullet);
		}
		for (int i = 0; i < 7; i++) {
			/*
			GameObject subBullet = (GameObject) Instantiate(subBulletPrefab, transform.position,
			                                                Quaternion.Euler(Random.Range(-30, 30), Random.Range(0, 360), 0));
			*/

			GameObject subBullet = ObjectPoolerScript.objectPooler.getEnemyBullet();
			subBullet.transform.position = transform.position;
			subBullet.transform.rotation = Quaternion.Euler(Random.Range(-30, 30), Random.Range(0, 360), 0);

			prepareBullet(subBullet);
		}
	}

	void prepareBullet(GameObject subBullet) {
		EnemyBulletScript bulletInfo = subBullet.GetComponent<EnemyBulletScript>();
		bulletInfo.speed = subBulletSpeed;
		bulletInfo.distanceTraveled = 0;
		bulletInfo.maxRange = subBulletRange;
		
		TrailRenderer trail = subBullet.GetComponent<TrailRenderer> ();
		trail.material.color = Color.yellow;
		subBullet.renderer.material.color = Color.yellow;
		
		subBullet.SetActive(true);
		bulletInfo.delayedReactivateTrail();
	}

	void OnCollisionEnter(Collision collision) {
		collisionFunction(collision.collider);
	}

	void collisionFunction(Collider col) {
		//if (col.gameObject.tag != "Enemy") {
			if (col.gameObject.tag == "Player") {
					col.gameObject.GetComponent<PlayerScript>().hitpoints -= 5;
			}
			Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
			Destroy (gameObject);
		//}
	}
}
