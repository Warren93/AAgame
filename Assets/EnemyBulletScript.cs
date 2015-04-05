using UnityEngine;
using System.Collections;

public class EnemyBulletScript : MonoBehaviour {

	public float speed;
	public float maxRange;
	float distanceTraveled = 0;
	public GameObject HitEffectPrefab;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (distanceTraveled >= maxRange)
			Destroy(gameObject);
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
