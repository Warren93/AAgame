using UnityEngine;
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
	
	public static List<GameObject> enemies;
	bool enemiesInitialized = false;

	Rect infoBarRect;
	Rect warningRect;

	static bool firstLoad = true;
	bool showLevelLoadMsg = true;

	public static float creationRadius;
	public static float creationHeight;
	public static float creationAlt;
	float ceiling = 0;

	public GameObject levelBoundaryWall;
	GameObject leftWall, rightWall, forwardWall, backWall, ceilingWall, floorWall;

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

	float groundRadius = 0;

	// Use this for initialization
	void Start () {

		mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		mouseLookCam = GameObject.FindGameObjectWithTag("MouseLookCam").GetComponent<Camera>();
		
		//backgroundColor = backgroundCols[Random.Range(0, backgroundCols.Length - 1)];
		//backgroundColor = new Color (184.0f / 255.0f, 145.0f / 255.0f, 61.0f / 255.0f);
		//backgroundColor = Color.Lerp(Color.yellow, Color.red, 0.2f);
		//backgroundColor = Color.Lerp (Color.black, Color.blue, 0.1f);
		backgroundColor = Color.gray;

		RenderSettings.fogColor = backgroundColor;

		groundRadius = ground.GetComponent<TerrainCollider>().bounds.extents.x;
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

		// create walls so player can't fly off map
		leftWall = createWall (Vector3.left, groundRadius);
		rightWall = createWall (Vector3.right, groundRadius);
		forwardWall = createWall (Vector3.forward, groundRadius);
		backWall = createWall (Vector3.back, groundRadius);
		ceilingWall = createWall (Vector3.up, groundRadius);
		floorWall = createWall (Vector3.down, groundRadius);
		floorWall.transform.position += transform.up * creationRadius;
		ceiling = ceilingWall.transform.position.y - ceilingWall.GetComponent<BoxCollider> ().bounds.extents.y;

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
				f1 = Random.Range(-120, 120);
				f2 = Random.Range(-120, 120);
				f3 = Random.Range(-120, 120);
				Vector3 createPt = new Vector3(f1, f2, f3);
				spawnPos += createPt;
				spawnEnemyAt (spawnPos);
			}
		}

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

		Screen.lockCursor = true;
		Cursor.visible = false;

		foreach (Camera cam in Camera.allCameras) {
			cam.backgroundColor = backgroundColor;
		}

		// damage player if they get too far from the center of the game world
		/*
		if (Player && Vector3.Distance(Vector3.zero, Player.transform.position) > mapRadius) {
			PlayerScript playerInfo = Player.GetComponent<PlayerScript> ();
			playerInfo.hitpoints -= 60 * Time.deltaTime;
		}
		*/

		if (Input.GetKeyDown(KeyCode.R))
			Application.LoadLevel(0);


		// exit game on ESC
		if (Input.GetKeyDown(KeyCode.Escape))
			Application.Quit();

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

			HPScript targetHP = null;
			string targetHP_Str = "";
			if (playerInfo.currentSelectedTarget != null) {
				if (playerInfo.currentSelectedTarget.GetComponent<HPScript>() != null) {
					targetHP = playerInfo.currentSelectedTarget.GetComponent<HPScript>();
					targetHP_Str = ((int)(targetHP.hitpoints)).ToString();
				}
				else if (playerInfo.currentSelectedTarget.tag == "AirBoss")
					targetHP_Str = ((int)(playerInfo.currentSelectedTarget.GetComponent<AirBossScript>().hitpoints)).ToString();
			}

			GUI.Box(infoBarRect,
			        "Boost: " + (int)playerInfo.boostCharge + "   Health: " + (int)playerInfo.hitpoints
			        + "   Score: " + score + "    Target HP: " + targetHP_Str,  guiStyle);

			//float playerDistFromOrigin = Vector3.Distance(Vector3.zero, Player.transform.position);
			//float distToEdge = mapRadius - playerDistFromOrigin;
			float distToEdge = getDistToWorldEdge();
			if (distToEdge < 200) {
				GUI.Box(warningRect, "Approaching edge of game area (distance: " + (int)distToEdge + ")", guiStyle);
			}
			else if (!firstLoad && showLevelLoadMsg) {
				GUI.Box(warningRect, "\nGame reset", guiStyle);
			}
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

	GameObject createWall (Vector3 direction, float offset) {
		Vector3 spawnPos = Vector3.zero;
		spawnPos.y = ground.transform.position.y;
		GameObject wall = (GameObject)Instantiate (levelBoundaryWall,
		                                    spawnPos + direction * offset * 2,
		                                    Quaternion.identity);
		BoxCollider wallCol = wall.GetComponent<BoxCollider> ();
		float scaleFac = offset / wallCol.bounds.extents.x;
		wall.transform.localScale *= scaleFac;

		float n1 = wallCol.bounds.extents.x;
		wall.transform.localScale *= 1.2f;
		float n2 = wallCol.bounds.extents.x;
		float n3 = n2 - n1;
		wall.transform.position += direction * n3;

		if (direction != Vector3.down)
			wall.transform.position += Vector3.up * offset;
		wall.isStatic = true;
		return wall;
	}

	float getDistToWorldEdge() {
		float xDist = groundRadius - Mathf.Abs(Player.transform.position.x);
		float yDist = ceiling - Player.transform.position.y;
		float zDist = groundRadius - Mathf.Abs(Player.transform.position.z);
		return Mathf.Min(xDist, yDist, zDist);
	}
}
