using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManagerScript : MonoBehaviour {

	public static bool gamePaused = false;
	float originalTimeScale = 1;

	public static int numActiveBullets = 0;
	public static int numActiveEnemyMissiles = 0;
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
	public Material threeShadeMat;
	public Material fiveShadeMat;

	public GameObject Enemy;
	public GameObject mechPrefab;
	public GameObject enemyFlakTowerPrefab;
	GameObject Player;
	PlayerScript playerInfo;
	public static int score;
	
	public static List<GameObject> enemies;
	bool enemiesInitialized = false;

	Rect infoBarRect;
	Rect warningRect;
	Rect damageIndicatorOverlay;
	Rect debugBox;
	Rect antiLagBox;

	public static Color damageIndicatorColor;
	public static float damageIndicatorColorStep;

	static bool firstLoad = true;
	bool showLevelLoadMsg = true;

	int numEnemies = 35; // was 50
	int numFlakTowers = 12; // was 20
	
	Camera mainCam;
	Camera mouseLookCam;

    public Texture guiBackgroundTexture;

	GUIStyle guiStyle;
    GUIContent guiContent;
    public Texture crosshairsTexture;
    public Font guiFont;

	int konamiIdx = 0;
	bool konamiCodeEnabled = false;
	string debugIndicatorStr = "";

   public static float totalNumEnemies;

	// Use this for initialization
	void Start () {

        guiContent = new GUIContent();
        guiContent.image = guiBackgroundTexture;

        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		mouseLookCam = GameObject.FindGameObjectWithTag("MouseLookCam").GetComponent<Camera>();
		
		//backgroundColor = backgroundCols[Random.Range(0, backgroundCols.Length - 1)];
		//backgroundColor = new Color (184.0f / 255.0f, 145.0f / 255.0f, 61.0f / 255.0f);
		//backgroundColor = Color.Lerp(Color.yellow, Color.red, 0.2f);
		//backgroundColor = Color.Lerp (Color.black, Color.blue, 0.1f);

		// SET SHADER AND SKYBOX COLORS BASED ON CURRENT LEVEL
		if (Application.loadedLevel == 1) {
			setShadersAndSkyboxColor(Color.yellow);
            totalNumEnemies = numEnemies + numFlakTowers + 1;
		}

		//backgroundColor = Color.gray;
		//RenderSettings.fogColor = backgroundColor;

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
		damageIndicatorOverlay = new Rect (Screen.width * -0.2f, Screen.height * -0.2f, Screen.width * 1.2f, Screen.height * 1.2f);
		debugBox = new Rect (Screen.width * 0.5f, Screen.height * 0.6f, 400, 80);
		antiLagBox = new Rect (10, Screen.height - 45, 300, 35);

		damageIndicatorColor = Color.clear;

		if (enemies != null)
			enemies.Clear();
		else
			enemies = new List<GameObject>();
		Player = GameObject.FindGameObjectWithTag ("Player");
		playerInfo = Player.GetComponent<PlayerScript> ();

		if (Application.loadedLevel == 1) {
			if (enemiesEnabled) {
				// create enemy
				for (int i = 0; i < numEnemies; i++) {
					//Vector3 spawnPos = Vector3.zero + Vector3.back * 70;
					Vector3 spawnPos = new Vector3(-400, -150, -900);//Vector3.back * 400;
					float f1, f2, f3;
					f1 = Random.Range(-120, 120);
					f2 = Random.Range(-120, 120);
					f3 = Random.Range(-50, 50);
					Vector3 createPt = new Vector3(f1, f2, f3);
					spawnPos += createPt;
					spawnEnemyAt (spawnPos);
				}
			}

			if (flakTowersEnabled)
				createFlakTowers ();
		}
		else if (Application.loadedLevel == 2) {
            totalNumEnemies = 10;
			foreach (GameObject playerBullet in ObjectPoolerScript.objectPooler.pooledPlayerBullets) {
				playerBullet.GetComponent<PlayerBulletScript> ().lookAheadMultiplier = 1.0f; // this should probably be 1 by default, but I'm  keeping it as is (0.5) for now out of superstition
			}
			Debug.Log("LEVEL 2");
			spawnMechs(10);
		}

		if (firstLoad)
			showLevelLoadMsg = false;
			
		if (Application.loadedLevel == 1) {
			GameObject cylinder = GameObject.Find ("Cylinder");
			//cylinder.transform.position = GameObject.Find("Terrain").transform.position + Vector3.down * 5f;
			cylinder.transform.position = new Vector3(cylinder.transform.position.x, GameObject.Find ("Terrain").transform.position.y - 5f, cylinder.transform.position.x);
		}

		Invoke ("removeLevelLoadMessage", 2);

		Invoke ("toggleFrameMeasuring", 5.0f);
	}
	
	// Update is called once per frame
	void Update () {

        //Debug.Log(totalNumEnemies);

        if (totalNumEnemies <= 0 && Application.loadedLevel + 1 <= Application.levelCount) {
            Application.LoadLevel(Application.loadedLevel + 1);
        }
		//Debug.Log ("num active missiles is " + numActiveEnemyMissiles);

		if (Input.GetKeyDown(KeyCode.P)) {
			if (gamePaused) {
				gamePaused = false;
				playerInfo.switchToMainCam();
				Time.timeScale = originalTimeScale;
			}
			else {
				gamePaused = true;
				Time.timeScale = 0;
			}
		}

        //Screen.lockCursor = true;
        if (!gamePaused) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (gamePaused)
			return;

		if (damageIndicatorColor != Color.clear) {
			damageIndicatorColor = Color.Lerp (damageIndicatorColor, Color.clear, damageIndicatorColorStep);
			damageIndicatorColorStep += Time.deltaTime;
		}

        /*
		if (Time.frameCount % 30 == 0) {
			System.GC.Collect();
		}
		*/

		foreach (Camera cam in Camera.allCameras) {
			cam.backgroundColor = backgroundColor;
		}

		if (Input.GetKeyDown(KeyCode.R))
			Application.LoadLevel(Application.loadedLevel);


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
			guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.font = guiFont;
            guiStyle.alignment = TextAnchor.MiddleCenter;
			guiStyle.fontSize = 28;
		}

		if (gamePaused) {
			GUI.Box (warningRect, "GAME PAUSED - Press P to resume", guiStyle);
		}

		Color oldColor = GUI.backgroundColor;
		GUI.backgroundColor = damageIndicatorColor;
		//GUI.Box (damageIndicatorOverlay, "");
		GUI.Button (damageIndicatorOverlay, "");
		//GUI.Button (damageIndicatorOverlay, "");
		GUI.backgroundColor = oldColor;

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
				float s = 16;
				GUI.Label(new Rect(crosshairsPt.x - (0.5f * s), crosshairsPt.y - (0.5f * s), s, s), crosshairsTexture);
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
            if (targetHP_Str != "")
                targetHP_Str = "    Target HP: " + targetHP_Str;

            GUI.DrawTexture(infoBarRect, guiBackgroundTexture, ScaleMode.StretchToFill);

            GUI.Label(infoBarRect, "Boost: " + (int)playerInfo.boostCharge + "   Health: " + Mathf.CeilToInt(playerInfo.hitpoints)
                    + "   Score: " + score + targetHP_Str + debugIndicatorStr, guiStyle);

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
		//enemy.GetComponent<Renderer>().material.color = Color.green;

		MeshRenderer enemyRenderer = enemy.GetComponent<MeshRenderer> ();
		//enemyRenderer.sharedMaterial.SetColor ("_Color2", Color.red);
		//Debug.Log ("material is " + enemyRenderer.sharedMaterial);

		enemy.GetComponent<EnemyScript> ().gameManger = gameObject;
	}

	void spawnMechs(int num) {
		GameObject groundObj = GameObject.Find ("Terrain");
		float radius = 100;
		for (int i = 0; i < num; i++) {
			Vector2 raycastOrigin = new Vector2 (Random.Range (-radius, radius),
			                                     Random.Range (-radius, radius));
			Ray ray = new Ray(new Vector3(raycastOrigin.x, 1000, raycastOrigin.y), Vector3.down);
			RaycastHit hit;
			Physics.Raycast(ray, out hit, Mathf.Infinity, (1 << LayerMask.NameToLayer("Ground")) | 1 << LayerMask.NameToLayer("Obstacle"));
			//groundObj.GetComponent<TerrainCollider>().Raycast(ray, out hit, Mathf.Infinity);
			GameObject mech = (GameObject)Instantiate (mechPrefab, hit.point, Quaternion.identity);
		}
	}

	/*
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
	*/

	void createFlakTowers() {
		TerrainCollider terrainCol = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainCollider> ();
		float areaInnerRadius = 800;
		float areaOuterRadius = 850;
		for (int i = 0; i < numFlakTowers; i++) {
			Vector2 raycastOrigin = new Vector2 (Random.Range (-10000, 10000),
			                                     Random.Range (-10000, 10000));
			float randFloat = Random.Range(areaInnerRadius, areaOuterRadius);
			float scaleFac = randFloat / raycastOrigin.magnitude;
			raycastOrigin *= scaleFac;
			//Debug.Log("raycast origin is now: " + raycastOrigin);
			Ray ray = new Ray(new Vector3(raycastOrigin.x, 1000, raycastOrigin.y), Vector3.down);
			RaycastHit hit;
			terrainCol.Raycast(ray, out hit, Mathf.Infinity);
			GameObject tower = (GameObject)Instantiate (enemyFlakTowerPrefab, hit.point, Quaternion.identity);
			tower.transform.GetChild(0).GetComponent<HPScript>().hitpoints = 15;
		}
	}

	void setShadersAndSkyboxColor(Color highlight) {
		// skybox
		RenderSettings.skybox.SetColor("_Tint", highlight);
		RenderSettings.skybox.SetFloat("_Exposure", 0.2f);
		// object shader
		threeShadeMat.SetColor("_Color1", highlight);
		threeShadeMat.SetColor("_Color2", highlight * 5f);
		threeShadeMat.SetColor("_Color3", Color.black);
		// ground shader
		fiveShadeMat.SetColor ("_Color1", highlight);
		fiveShadeMat.SetColor ("_Color2", highlight * 0.8f);
		fiveShadeMat.SetColor ("_Color3", highlight * 0.6f);
		fiveShadeMat.SetColor ("_Color4", highlight * 0.4f);
		fiveShadeMat.SetColor ("_Color5", highlight * 0.2f);
		// fog
		RenderSettings.fogColor = highlight * 0.5f;
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
