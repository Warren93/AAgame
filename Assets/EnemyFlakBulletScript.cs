using UnityEngine;
using System.Collections;

public class EnemyFlakBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	float distanceTraveled = 0;
	public GameObject HitEffectPrefab;
	public GameObject subBulletPrefab;
	public GameObject player;
	float subBulletSpeed;
	float subBulletRange;

	// Use this for initialization
	void Start () {
		subBulletSpeed = 15;
		subBulletRange = 15;
	}
	
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
		/*
		RaycastHit hitInfo;
		bool didHit = Physics.Raycast (transform.position, transform.forward,
		                               out hitInfo,
		                               speed * Time.deltaTime * 0.5f);
		if (didHit)
			collisionFunction(hitInfo.collider);
		*/
	}

	void createBulletExplosion() {
		PlayerScript playerInfo = player.GetComponent<PlayerScript> ();
		Vector3 target = 2f * Vector3.Distance(transform.position, player.transform.position) * playerInfo.newPos.normalized;
		for (int i = 0; i < 3; i++) {
			float noise = 25;
			Vector3 target2 = target + new Vector3(Random.Range(-noise, noise), Random.Range(-noise, noise), Random.Range(-noise, noise));
			GameObject subBullet = (GameObject) Instantiate(subBulletPrefab, transform.position,
			                                                Quaternion.LookRotation(target2 - transform.position));
			EnemyBulletScript bulletInfo = subBullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = subBulletSpeed;
			bulletInfo.maxRange = subBulletRange;
		}
		for (int i = 0; i < 7; i++) {
			GameObject subBullet = (GameObject) Instantiate(subBulletPrefab, transform.position,
			                                                Quaternion.Euler(Random.Range(-30, 30), Random.Range(0, 360), 0));
			EnemyBulletScript bulletInfo = subBullet.GetComponent<EnemyBulletScript>();
			bulletInfo.speed = subBulletSpeed;
			bulletInfo.maxRange = subBulletRange;

		}
	}

	void OnCollisionEnter(Collision collision) {
		collisionFunction(collision.collider);
	}

	void collisionFunction(Collider col) {
		if (col.gameObject.tag != "Enemy") {
			if (col.gameObject.tag == "Player") {
					col.gameObject.GetComponent<PlayerScript>().hitpoints -= 5;
			}
			Instantiate(HitEffectPrefab, transform.position, Quaternion.identity);
			Destroy (gameObject);
		}
	}
}
