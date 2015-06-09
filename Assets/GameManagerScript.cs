﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManagerScript : MonoBehaviour {

	public bool enemiesEnabled;
	public bool flakTowersEnabled;

	public Color backgroundColor;
	Color[]  backgroundCols = {Color.red, Color.yellow, Color.blue, Color.green, Color.magenta};

	public GameObject ground;

	public GameObject Enemy;
	public GameObject enemyFlakTowerPrefab;
	GameObject Player;
	PlayerScript playerInfo;
	public static int score;

	List<GameObject> asteroids;
	List<GameObject> uniqueAsteroids;
	public static List<GameObject> enemies;
	public Material asteroidMat;
	bool enemiesInitialized = false;

	Rect infoBarRect;
	Rect warningRect;

	static bool firstLoad = true;
	bool showLevelLoadMsg = true;
	int aiType = 0;
	public static bool showWelcomeMsg = true;

	public static float creationRadius;
	public static float creationHeight;
	public static float creationAlt;
	public static float mapRadius;
	public static float warnRadius;

	int numUniqueObstacles = 10;
	int numObstacles = 800; // was 800, then 700
	int numLargeObstacles = 75; // was 50
	//int numObstacles = 0;
	int numEnemies = 50;
	//int numEnemies = 1;

	float globalLowerBound = 2;
	float globalUpperBound = 55; // was 55
	float globalBoundRatio = 0.75f; // was 0.75

	/*
	float boidCamDist = 0;
	float boidCamZoom = 1;
	Camera boidCam;
	*/

	Camera mainCam;
	Camera mouseLookCam;

	GUIStyle guiStyle;

	// Use this for initialization
	void Start () {

		mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		mouseLookCam = GameObject.FindGameObjectWithTag("MouseLookCam").GetComponent<Camera>();
		
		backgroundColor = backgroundCols[Random.Range(0, backgroundCols.Length - 1)];
		backgroundColor = new Color (184.0f / 255.0f, 145.0f / 255.0f, 61.0f / 255.0f);

		float groundRadius = ground.GetComponent<TerrainCollider>().bounds.extents.x;
		//float groundRadius = ground.transform.localScale.x;
		ground.transform.position += Vector3.left * groundRadius;
		ground.transform.position += Vector3.back * groundRadius;

		Application.targetFrameRate = 60;

		if(Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
			QualitySettings.antiAliasing = 2;
		else
			QualitySettings.antiAliasing = 4;

		//guiStyle = new GUIStyle();

		score = 0;

		infoBarRect = new Rect (10, 10, Screen.width * 0.5f, 35);
		warningRect = new Rect (0, 0, Screen.width * 0.6f, 50);
		warningRect.center = new Vector2 (Screen.width * 0.5f, Screen.height * 0.5f);

		creationRadius = 1000.0f; // was 800
		creationHeight = 500.0f;
		creationAlt = 100.0f; // was 900
		ground.transform.position += Vector3.down * creationRadius * 0.5f;
		mapRadius = creationRadius * 2.2f; // was 0.9, then 1.2
		warnRadius = mapRadius - 120;


		/*
		boidCam = (Camera) Instantiate(GameObject.FindGameObjectWithTag("MainCamera").camera,
		                               Vector3.zero,
		                               Quaternion.FromToRotation(new Vector3(0, 0, 0), new Vector3(0, 0, 1)));

		boidCam.enabled = false;
		*/

		asteroids = new List<GameObject> ();
		uniqueAsteroids = new List<GameObject> ();
		if (enemies != null)
			enemies.Clear();
		else
			enemies = new List<GameObject>();
		Player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = Player.GetComponent<PlayerScript> ();

		if (enemiesEnabled) {
			// create enemy
			for (int i = 0; i < numEnemies; i++) {
				//Vector3 spawnPos = Vector3.zero + Vector3.back * 70;
				Vector3 spawnPos = Vector3.back * 400;
				float f1, f2, f3;
				/*
				f1 = Random.Range(-10, 10);
				f2 = Random.Range(-10, 10);
				f3 = Random.Range(-10, 10);
				Vector3 createPt = new Vector3(f1, f2, f3);
				createPt.Normalize();
				createPt *= Random.Range(60, 120);
				*/
				f1 = Random.Range(-120, 120);
				f2 = Random.Range(-120, 120);
				f3 = Random.Range(-120, 120);
				Vector3 createPt = new Vector3(f1, f2, f3);
				spawnPos += createPt;
				spawnEnemyAt (spawnPos);
			}
		}

		//createUniqueAsteroids (); // the asteroids from which all others will be duplicated
		//createAsteroids (numObstacles, Random.Range(1.0f, 1.2f));
		//createAsteroids (numLargeObstacles, Random.Range(3.0f, 5.0f));

		if (flakTowersEnabled)
			createFlakTowers ();

		if (firstLoad)
			showLevelLoadMsg = false;
			

		Invoke ("removeLevelLoadMessage", 2);
	}
	
	// Update is called once per frame
	void Update () {

		/*
		if (Time.frameCount % 30 == 0) {
			System.GC.Collect();
		}
		*/

		//Debug.Log ("alt is " + creationAlt);
		Screen.lockCursor = true;
		Cursor.visible = false;

		foreach (Camera cam in Camera.allCameras) {
			cam.backgroundColor = backgroundColor;
		}

		// damage player if they get too far from the center of the game world
		if (Player && Vector3.Distance(Vector3.zero, Player.transform.position) > mapRadius) {
			PlayerScript playerInfo = Player.GetComponent<PlayerScript> ();
			playerInfo.hitpoints -= 60 * Time.deltaTime;
		}
		/*
		if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Alpha3)
		    || Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Alpha6)) {
			foreach (GameObject enemy in enemies) {
				if (!enemy)
					continue;
				EnemyScript currentEnemy = enemy.GetComponent<EnemyScript>();
				if (currentEnemy.enabled == false) {
					setAI_Type(currentEnemy);
					currentEnemy.enabled = true;
				}
			}
			if (enemiesInitialized == false) {
				enemiesInitialized = true;
			}
		}
		*/

		/*
		// toggle asteroids on/off with M key
		if (Input.GetKeyDown (KeyCode.M)) {
			foreach (GameObject obj in asteroids) {
				if (obj.activeSelf == true)
					obj.SetActive(false);
				else if (obj.activeSelf == false)
					obj.SetActive(true);
			}
		}
		*/

		if (Input.GetKeyDown(KeyCode.R))
			Application.LoadLevel(0);


		// exit game on ESC
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (showWelcomeMsg == true) {
				showWelcomeMsg = false;
				//Screen.lockCursor = true;
			}
			else
				Application.Quit();
		}

	}

	void setAI_Type(EnemyScript currentEnemy) {
		// disable senses
		currentEnemy.playerInvisible = true;
		currentEnemy.playerUnsmellable = true;
		// disable behaviors
		currentEnemy.encirclingBehaviorEnabled = false;
		currentEnemy.searchStateEnabled = false;
		// disable energy consumption
		currentEnemy.energyConsumptionEnabled = false;
		// enable basic behavior
		currentEnemy.proximityCheck = true;
		currentEnemy.flockingEnabled = true;

		// renable featuers based on selected AI type

		// enable search
		if (Input.GetKeyUp(KeyCode.Alpha1)) {
			aiType = 1;
			currentEnemy.searchStateEnabled = true;
		}
		// enable senses
		else if (Input.GetKeyUp(KeyCode.Alpha2)) {
			aiType = 2;
			currentEnemy.playerInvisible = false;
			currentEnemy.playerUnsmellable = false;
			// disable the simplified system that's used in the absence of senses
			currentEnemy.proximityCheck = false;
		}
		// enable energy consumption
		else if (Input.GetKeyUp(KeyCode.Alpha3)) {
			aiType = 3;
			currentEnemy.energyConsumptionEnabled = true;
		}
		// enable search and senses
		else if (Input.GetKeyUp(KeyCode.Alpha4)) {
			aiType = 4;
			currentEnemy.searchStateEnabled = true;
			currentEnemy.playerInvisible = false;
			currentEnemy.playerUnsmellable = false;
			currentEnemy.proximityCheck = false;
		}
		// enable search, senses, and energy
		else if (Input.GetKeyUp(KeyCode.Alpha5)) {
			aiType = 5;
			// search
			currentEnemy.searchStateEnabled = true;
			// senses
			currentEnemy.playerInvisible = false;
			currentEnemy.playerUnsmellable = false;
			currentEnemy.proximityCheck = false;
			// energy
			currentEnemy.energyConsumptionEnabled = true;
		}
		// enable earch, senses, energy, and encircling (i.e. enable everything)
		else if (Input.GetKeyUp(KeyCode.Alpha6)) {
			aiType = 6;
			// search
			currentEnemy.searchStateEnabled = true;
			// senses
			currentEnemy.playerInvisible = false;
			currentEnemy.playerUnsmellable = false;
			currentEnemy.proximityCheck = false;
			// energy
			currentEnemy.energyConsumptionEnabled = true;
			// encircling
			currentEnemy.encirclingBehaviorEnabled = true;
		}
	}

	void OnGUI() {
		if (guiStyle == null) {
			guiStyle = new GUIStyle(GUI.skin.box);
			guiStyle.fontSize = 16;
		}

		Camera currentCam;
		if (mainCam.enabled)
			currentCam = mainCam;
		else
			currentCam = mouseLookCam;


		if (Player) {
			// draw crosshairs
			Vector3 crosshairsWorldPt = Player.transform.position + (Player.transform.forward * playerInfo.currentWeaponRange);
			if (currentCam
			    && Vector3.Angle(currentCam.transform.forward, crosshairsWorldPt - currentCam.transform.position) <= currentCam.fieldOfView * 0.5f) {
				Vector3 crosshairsPt = currentCam.WorldToScreenPoint(crosshairsWorldPt);
				crosshairsPt.y = Screen.height - crosshairsPt.y;
				float s = 9;
				GUI.Label(new Rect(crosshairsPt.x - (0.5f * s), crosshairsPt.y - (0.5f * s), s, s), "+", guiStyle);
			}

			// draw target indicator on current target
			if (playerInfo.currentSelectedTarget != null
			    && Vector3.Angle(currentCam.transform.forward,
			                 playerInfo.currentSelectedTarget.transform.position - currentCam.transform.position)
			    				<= currentCam.fieldOfView * 0.5f) {
				Vector3 targetPt = currentCam.WorldToScreenPoint(playerInfo.currentSelectedTarget.transform.position);
				targetPt.y = Screen.height - targetPt.y;
				float s = 30;
				GUI.Label(new Rect(targetPt.x - (0.5f * s), targetPt.y - (0.5f * s), s, s), "+", guiStyle);
			}

			/*
			string aiTypeStr = aiType.ToString();
			if (aiType == 0)
				aiTypeStr = "not set";
			GUI.Box(infoBarRect,
			        "AI type: " + aiTypeStr + "    Boost: " + (int)playerInfo.boostCharge + "   Health: " + (int)playerInfo.hitpoints + "   Score: " + score,  guiStyle);
			*/

			HPScript targetHP = null;
			string targetHP_Str = "";
			if (playerInfo.currentSelectedTarget != null && playerInfo.currentSelectedTarget.GetComponent<HPScript>() != null) {
				targetHP = playerInfo.currentSelectedTarget.GetComponent<HPScript>();
				targetHP_Str = ((int)(targetHP.hitpoints)).ToString();
			}

			GUI.Box(infoBarRect,
			        "Boost: " + (int)playerInfo.boostCharge + "   Health: " + (int)playerInfo.hitpoints
			        + "   Score: " + score + "    Target HP: " + targetHP_Str,  guiStyle);

			float playerDistFromOrigin = Vector3.Distance(Vector3.zero, Player.transform.position);
			float distToEdge = mapRadius - playerDistFromOrigin;
			if (playerDistFromOrigin > warnRadius) {
				GUI.Box(warningRect, "Approaching edge of game area (distance: " + (int)distToEdge
				        + ")\nTurn back or you will take damage", guiStyle);
			}
			else if (!firstLoad && showLevelLoadMsg) {
				GUI.Box(warningRect, "\nGame reset", guiStyle);
			}
			/*
			else if (showWelcomeMsg) {
				GUI.Box(warningRect, "Read directions below, then press ESC to exit this message\nDon't forget the ranking thing explained at the bottom", guiStyle);
			}
			*/
		}
		if (firstLoad == true)
			firstLoad = false;
	}

	void spawnEnemyAt(Vector3 position) {
		GameObject enemy = (GameObject) Instantiate (Enemy);
		enemy.transform.position = position;
		enemies.Add (enemy);
		enemy.GetComponent<EnemyScript> ().enabled = true;
		enemy.GetComponent<Renderer>().material.color = Color.green;
		enemy.GetComponent<EnemyScript> ().gameManger = gameObject;
	}

	void createAsteroids(int numToCreate, float scaleFactor) {
		for (int i = 0; i < numToCreate; i++) {
			float xPos, yPos, zPos;
			/*
			f1 = Random.Range(-10, 10);
			f2 = Random.Range(-10, 10);
			f3 = Random.Range(-10, 10);
			Vector3 createPt = new Vector3(f1, f2, f3);
			createPt.Normalize();
			createPt *= Random.Range(20, creationRadius);
			*/
			xPos = Random.Range(-creationRadius, creationRadius);
			yPos = Random.Range(-creationHeight * 0.5f, creationHeight * 0.5f);
			zPos = Random.Range(-creationRadius, creationRadius);
			Vector3 createPt = new Vector3(xPos, yPos, zPos);
			createPt.y += creationAlt;
			int idx = Random.Range(0, numUniqueObstacles - 1);
			GameObject obstacle = (GameObject) Instantiate(uniqueAsteroids[idx]);
			obstacle.transform.localScale *= scaleFactor;
			obstacle.transform.position = createPt;
			obstacle.isStatic = true;
			obstacle.tag = "Obstacle";

			asteroids.Add(obstacle);

		}

		for (int i = 0; i < uniqueAsteroids.Count; i++) {
			Destroy(uniqueAsteroids[i]);
		}

	}

	void createUniqueAsteroids() {
		for (int i = 0; i < numUniqueObstacles; i++) {
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			
			float upperBound = Random.Range(globalLowerBound, globalUpperBound);
			float lowerBound = upperBound * globalBoundRatio;
			
			// randomize shape
			Mesh sphereMesh = sphere.GetComponent<MeshFilter>().mesh;
			Vector3[] verts = sphereMesh.vertices;
			Vector3 avgVertPos = Vector3.zero;
			for (int j = 0; j < verts.Length; j++)
				avgVertPos += verts[j];
			avgVertPos /= verts.Length;
			float originalRadius = Vector3.Distance(verts[0], avgVertPos);
			
			//for (int j = 0; j < verts.Length; j++) {
			//	Debug.Log("Vert " + j + ": " + verts[j]);
			//}
			
			//Debug.Log("Max triangle vert index is " + maxIdx);
			Hashtable uniqueVerts = new Hashtable();
			
			for (int j = 0; j < verts.Length; j++) {
				// if we've already encountered this vertex...
				if (uniqueVerts.Contains(verts[j].ToString())) {
					// ...retrieve the value we gave it last time
					//string oldVertString = verts[j].ToString();
					verts[j] = (Vector3) uniqueVerts[verts[j].ToString()];
					//Debug.Log("already encountered vertex " + oldVertString + ", using prev. value of " + verts[j]);
				}
				else { // otherwise...
					// ...generate new vertex value...
					Vector3 vecFromCenter = verts[j] - avgVertPos;
					float originalVertRadius = vecFromCenter.magnitude;
					vecFromCenter.Normalize();
					float rf = Random.Range(lowerBound * originalVertRadius, upperBound * originalVertRadius);
					Vector3 newVertexValue = verts[j] + vecFromCenter * rf;
					// ...and store it in this vertex's position in hashtable
					//Debug.Log("first time encountering vertex " + verts[j] + ", storing new value " + newVertexValue);
					uniqueVerts.Add(verts[j].ToString(), newVertexValue);
					verts[j] = newVertexValue;
				}
			}

			/*
			float newRadius = 0;
			for (int j = 0; j < verts.Length; j++) {
				float currentVertDist = Vector3.Distance(verts[j], avgVertPos);
				if (Vector3.Distance(verts[j], avgVertPos) > newRadius)
					newRadius = currentVertDist;
			}
			float scaleFactor = newRadius / originalRadius;
			*/

			float maxRadius = Vector3.Distance(verts[0], avgVertPos);
			float minRadius = maxRadius;
			for (int j = 0; j < verts.Length; j++) {
				float currentVertDist = Vector3.Distance(verts[j], avgVertPos);
				if (currentVertDist > maxRadius)
					maxRadius = currentVertDist;
				if (currentVertDist < minRadius)
					minRadius = currentVertDist;
			}
			float avgRadius = (maxRadius + minRadius) * 0.5f;
			float scaleFactor = avgRadius / originalRadius;

			SphereCollider col = sphere.transform.GetComponent<SphereCollider>();
			col.radius *= scaleFactor;
			
			sphereMesh.vertices = verts;
			sphereMesh.RecalculateNormals();
			sphereMesh.RecalculateBounds();
			uniqueVerts.Clear();
			sphere.GetComponent<Renderer>().material = asteroidMat;

			uniqueAsteroids.Add(sphere);
		}
	}

	void createFlakTowers() {
		TerrainCollider terrainCol = ground.GetComponent<TerrainCollider> ();
		float groundRadius = terrainCol.bounds.extents.x;
		groundRadius -= 5;
		for (int i = 0; i < 20; i++) {
			Vector3 raycastOrigin = new Vector3 (Random.Range (-groundRadius, groundRadius),
		                               		1000,
		                               		Random.Range (-groundRadius, groundRadius));
			Ray ray = new Ray(raycastOrigin, Vector3.down);
			RaycastHit hit;
			terrainCol.Raycast(ray, out hit, Mathf.Infinity);
			GameObject tower = (GameObject)Instantiate (enemyFlakTowerPrefab, hit.point, Quaternion.identity);
			tower.transform.GetChild(0).GetComponent<HPScript>().hitpoints = 15;
		}
	}

	void removeLevelLoadMessage() {
		showLevelLoadMsg = false;
	}
}
