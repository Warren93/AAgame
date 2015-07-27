using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManagerScript : MonoBehaviour {

	public static int numActiveBullets = 0;
	public static int numFightersPursuingPlayer = 0;
	bool showDebugInfo = false;

	bool antiLagEnabled = false;
	bool showAntiLagState = false;
	public static float antiLagThreshold = 10; // was 0.28, now is reset based on baseline (average) frame time calculated at beginning of game
	float maxAllowedFrameTime = 10;
	bool measureFrames = false;
	uint numFramesMeasured = 0;
	float measuredFramesSum = 0;
	float averageFrameTime = 0;

	public bool enemiesEnabled;
	public bool flakTowersEnabled;

	public Color backgroundColor;
	Color[]  backgroundCols = {Color.red, Color.yellow, Color.blue, Color.green, Color.magenta};

	public GameObject Enemy;
	public GameObject enemyFlakTowerPrefab;
	GameObject Player;
	PlayerScript playerInfo;
	public static int score;
	
	public static List<GameObject> enemies;
	bool enemiesInitialized = false;

	Rect infoBarRect;
	Rect warningRect;
	Rect debugBox;
	Rect antiLagBox;

	static bool firstLoad = true;
	bool showLevelLoadMsg = true;

	int numEnemies = 35; // was 50
	int numFlakTowers = 12; // was 20
	
	Camera mainCam;
	Camera mouseLookCam;

	GUIStyle guiStyle;

	int konamiIdx = 0;
	bool konamiCodeEnabled = false;
	string debugIndicatorStr = "";

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

		Application.targetFrameRate = 60;
		//QualitySettings.vSyncCount = 0;

		/*
		if(Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.WindowsWebPlayer)
			QualitySettings.antiAliasing = 2;
		else
			QualitySettings.antiAliasing = 4;
		*/

		//guiStyle = new GUIStyle();

		score = 0;

		infoBarRect = new Rect (10, 10, Screen.width * 0.5f, 35);
		warningRect = new Rect (0, 0, Screen.width * 0.6f, 50);
		warningRect.center = new Vector2 (Screen.width * 0.5f, Screen.height * 0.5f);
		debugBox = new Rect (Screen.width * 0.5f, Screen.height * 0.6f, 400, 80);
		antiLagBox = new Rect (10, Screen.height - 45, 300, 35);

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

		Invoke ("toggleFrameMeasuring", 5.0f);
	}
	
	// Update is called once per frame
	void Update () {

		/*
		if (Time.frameCount % 30 == 0) {
			System.GC.Collect();
		}
		*/

		//Screen.lockCursor = true;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		foreach (Camera cam in Camera.allCameras) {
			cam.backgroundColor = backgroundColor;
		}

		if (Input.GetKeyDown(KeyCode.R))
			Application.LoadLevel(0);


		// exit game on ESC
		if (Input.GetKeyDown(KeyCode.Escape))
			Application.Quit();

		konamiCodeEnabled = checkKonamiCodeActivated ();
		//Debug.Log ("konami idx is " + konamiIdx);

		if (konamiCodeEnabled) {
			playerInfo.invincible = true;
			debugIndicatorStr = "    NO DEATH";
		}

		if (Input.GetKeyDown (KeyCode.I)) {
			if (showDebugInfo == true)
				showDebugInfo = false;
			else if (showDebugInfo == false)
				showDebugInfo = true;
		}

		if (Input.GetKeyDown (KeyCode.M)) {
			if (antiLagEnabled == true)
				antiLagEnabled = false;
			else
				antiLagEnabled = true;
			showAntiLagState = true;
			CancelInvoke("stopShowingAntiLagState");
			Invoke("stopShowingAntiLagState", 3.0f);
		}

		if (antiLagEnabled)
			antiLagThreshold = maxAllowedFrameTime;
		else
			antiLagThreshold = 10;

		if (measureFrames) {
			measuredFramesSum += Time.deltaTime;
			numFramesMeasured++;
		}

		// just in case something weird happens
		if (numFightersPursuingPlayer < 0)
			numFightersPursuingPlayer = 0;
		if (numActiveBullets < 0)
			numActiveBullets = 0;
	}

	void OnGUI() {
		if (guiStyle == null) {
			guiStyle = new GUIStyle(GUI.skin.box);
			guiStyle.fontSize = 16;
		}

		if (showDebugInfo)
			GUI.Box (debugBox, "Active enemy bullets: " + numActiveBullets
			         + "\nFighters pursuing player: " + numFightersPursuingPlayer
			         + "\nFrame rate: " + (int)(1.0f / Time.deltaTime)
			         + "\nAnti-lag threshold: " + antiLagThreshold, guiStyle);

		if (showAntiLagState) {
			string state_str;
			if (antiLagEnabled)
				state_str = "ON";
			else
				state_str = "OFF";
			GUI.Box (antiLagBox, "Lag prevention is: " + state_str, guiStyle);
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
			        + "   Score: " + score + "    Target HP: " + targetHP_Str + debugIndicatorStr,  guiStyle);

			/*
			if (!firstLoad && showLevelLoadMsg) {
				GUI.Box(warningRect, "\nGame reset", guiStyle);
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

	void createFlakTowers() {
		TerrainCollider terrainCol = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainCollider> ();
		float groundRadius = terrainCol.bounds.extents.x;
		groundRadius -= 5;
		for (int i = 0; i < numFlakTowers; i++) {
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

	void stopShowingAntiLagState() {
		showAntiLagState = false;
	}

	void toggleFrameMeasuring() {
		if (measureFrames == false) {
			measureFrames = true;
			// measure frame rate for 60 seconds
			Invoke ("toggleFrameMeasuring", 60.0f);
		}
		else if (measureFrames == true && numFramesMeasured > 0) { // then, compute average framerate and base anti-lag threshold on that
			measureFrames = false;
			averageFrameTime = measuredFramesSum / (float)numFramesMeasured;
			maxAllowedFrameTime = averageFrameTime + 0.008f;
			//Debug.Log("THRESHOLD IS NOW: " + maxAllowedFrameTime);
		}
	}

	bool checkKonamiCodeActivated () {
		if (!Input.anyKeyDown)
			return false;
		if (Input.GetKeyDown(KeyCode.UpArrow) && (konamiIdx == 0 || konamiIdx == 1))
			konamiIdx++;
		else if (Input.GetKeyDown(KeyCode.DownArrow) && (konamiIdx == 2 || konamiIdx == 3))
			konamiIdx++;
		else if (Input.GetKeyDown(KeyCode.LeftArrow) && (konamiIdx == 4 || konamiIdx == 6))
			konamiIdx++;
		else if (Input.GetKeyDown(KeyCode.RightArrow) && (konamiIdx == 5 || konamiIdx == 7))
			konamiIdx++;
		else if (Input.GetKeyDown(KeyCode.B) && konamiIdx == 8)
			konamiIdx++;
		else if (Input.GetKeyDown(KeyCode.A) && konamiIdx == 9)
			konamiIdx++;
		else
			konamiIdx = 0;

		if (konamiIdx == 10)
			return true;
		return false;
	}
}
