using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletLinkScript : MonoBehaviour {

	LineRenderer line;
	public List<GameObject> bullets;
	public float bulletSpeed;
	public List<Vector3> prevBulletPositions;
	GameObject player;
	public float detectionWidth;
	LayerMask playerLayer;
	PlayerScript playerInfo;
	public float damage;
	bool interpolateColorEnabled;
	Color col1, col2;
	float colorChangeRate = 0;
	float currentColorStep = 0;
	int mult = 1;

	public void Init () {
		damage = 5;
		interpolateColorEnabled = false;
		line = GetComponent<LineRenderer> ();
		bullets = new List<GameObject> (10);
		prevBulletPositions = new List<Vector3> (10);
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

	public void interpolateColor (Color a, Color b, float rate) {
		col1 = a;
		col2 = b;
		colorChangeRate = rate;
		interpolateColorEnabled = true;
	}

	/*
	void OnEnable() {
		line.SetVertexCount (bullets.Count + 1);
	}
	*/

	// Update is called once per frame
	public void Update () {

		// PREVENT GLITCHES DUE TO OBJECT POOLING
		if (prevBulletPositions.Count != bullets.Count) {
			selfDestruct();
			return;
		}

		for (int i = 0; i < prevBulletPositions.Count; i++) {
			if (Vector3.Distance(bullets[i].transform.position, prevBulletPositions[i]) > bulletSpeed * Time.deltaTime * 7.5f) {
				selfDestruct();
				return;
			}
			else
				prevBulletPositions[i] = bullets[i].transform.position;
		}



		if (interpolateColorEnabled == true) {
			if (currentColorStep > 1 && mult == 1)
				mult = -1;
			else if (currentColorStep < 0 && mult == -1)
				mult = 1;
			currentColorStep += mult * colorChangeRate * Time.deltaTime;
			line.material.color = Color.Lerp(col1, col2, currentColorStep);
		}

		line.SetVertexCount (bullets.Count + 1);
		for (int i = 0; i < bullets.Count; i++) {
			if (!bullets[i].activeInHierarchy) {
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

		//for (int i = 0; i < prevBulletPositions.Count; i++)
		//	prevBulletPositions[i] = bullets[i].transform.position;

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
				GameObject hitEffect = ObjectPoolerScript.objectPooler.getHitEffect();
				hitEffect.transform.position = hitInfo.point;
				hitEffect.SetActive(true);
				selfDestruct();
			}
		}
	}

	public void setVisible(bool visible) {
		if (visible)
			line.enabled = true;
		else
			line.enabled = false;
	}

	public void selfDestruct() {
		bullets.Clear ();
		prevBulletPositions.Clear ();
		setVisible (false);
		gameObject.SetActive (false);
		// set things back to default
		damage = 5;
		interpolateColorEnabled = false;
		currentColorStep = 0;
	}
}
