using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletLinkScript : MonoBehaviour {

	LineRenderer line;
	public List<GameObject> bullets;
	GameObject player;
	public GameObject HitEffectPrefab;
	public float detectionWidth;
	LayerMask playerLayer;
	PlayerScript playerInfo;
	float damage = 5;

	public void Init () {
		line = GetComponent<LineRenderer> ();
		bullets = new List<GameObject> ();
		player = GameObject.FindGameObjectWithTag ("Player");
		playerLayer = 1 << player.layer;
		playerInfo = player.GetComponent<PlayerScript> ();
		//if (!line)
		//				Debug.Log ("problem");
	}

	public void setWidth (float width) {
		line.SetWidth (width, width);
		detectionWidth = width;
	}

	/*
	void OnEnable() {
		line.SetVertexCount (bullets.Count + 1);
	}
	*/

	// Update is called once per frame
	void Update () {
		line.SetVertexCount (bullets.Count + 1);
		for (int i = 0; i < bullets.Count; i++) {
			if (!bullets[i].activeSelf) {
				selfDestruct();
				return;
			}
		}
		for (int i = 0; i < bullets.Count + 1; i++) {
			Vector3 v1;
			if (i < bullets.Count)
				v1 = bullets[i].transform.position;
			else
				v1 = bullets[0].transform.position;
			line.SetPosition(i, v1);

			// collision detection with raycasts
			/*
			Vector3 v2;
			if (i + 1 < bullets.Count)
				v2 = bullets[i + 1].transform.position;
			else
				v2 = bullets[0].transform.position;
			float dist = Vector3.Distance(v1, v2);
			if (dist <= 0)
				continue;
			Ray ray = new Ray(v1, (v2 - v1).normalized);
			RaycastHit hit;
			bool didHit = player.collider.Raycast(ray, out hit, dist);
			if (didHit) {
				playerInfo.hitpoints -= damage;
				Instantiate(HitEffectPrefab, hit.point, Quaternion.identity);
				selfDestruct();
			}
			*/
		}
		detectCollisions ();

	}

	/*
	void FixedUpdate() {
		detectCollisions ();
	}
	*/

	void detectCollisions() {
		// collision detection with spherecast
		for (int i = 0; i < bullets.Count + 1; i++) {
			Vector3 v1;
			if (i < bullets.Count)
				v1 = bullets[i].transform.position;
			else
				v1 = bullets[0].transform.position;
			Vector3 v2;
			if (i + 1 < bullets.Count)
				v2 = bullets[i + 1].transform.position;
			else
				v2 = bullets[0].transform.position;
			float dist = Vector3.Distance(v1, v2);
			if (dist <= 0)
				continue;
			RaycastHit hitInfo;
			bool didHit = Physics.SphereCast(v1, detectionWidth * 0.5f, (v2 - v1).normalized, out hitInfo, dist, playerLayer);
			if (didHit) {
				//Debug.Log("LINK HIT OBJ: " + hitInfo.collider.gameObject.name);
				playerInfo.hitpoints -= damage;
				Instantiate(HitEffectPrefab, hitInfo.point, Quaternion.identity);
				selfDestruct();
			}
		}
	}

	void selfDestruct() {
		CancelInvoke("reactivateTrail");
		gameObject.SetActive (false);
		bullets.Clear ();
	}
}
