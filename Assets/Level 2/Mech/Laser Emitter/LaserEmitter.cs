using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BeamMaterialList {
	public List<Material> materials;
}

public class LaserEmitter : MonoBehaviour {

	Transform target;
	Vector3 aimPosition;
	public Transform startPoint;
	Vector3 endPoint;
	public float beamWidth = 1.0f;
	public float maxRange = 1000.0f;
	float maxRangeSquared;
	int chargeUpMaterialIdx = 0;
	int firingMaterialIdx = 0;
	float damagePerSecond = 40f;

	public MechShoot mechShootScript; 	
	public bool useCurrentMechProjectileStartPosAsStartPoint = true;
	public BeamMaterialList beamMaterials;
	LineRenderer lineRenderer;

	bool chargingUp = false;
	bool firing = false;
	float currentFiringTime = 1;

	int idxInPattern = 0;

	public static int DEFAULT_PATTERN = 0;
	public static int PATTERN_2 = 1;
	int currentFiringPattern = DEFAULT_PATTERN;

	const float aimLerpStep_DEFAULT_PATTERN = 25;
	const float aimLerpStep_PATTERN_2 = 15;
	float aimUpdateLerpStep = aimLerpStep_DEFAULT_PATTERN;

	Vector3 aimOffset = Vector3.zero;

	// pattern specific state vars
	float pattern2Radius = 30;
	float pattern2AimOffsetStep = 7;

	GameObject player;
	LayerMask playerLayer;
	LayerMask beamObstructionLayer;
	PlayerScript playerInfo;

	// Use this for initialization
	void Start () {
		maxRangeSquared = Mathf.Pow (maxRange, 2);
		lineRenderer = gameObject.GetComponent<LineRenderer> ();
		player = GameObject.FindGameObjectWithTag ("Player");
		playerLayer = 1 << player.layer;
		beamObstructionLayer = (1 << LayerMask.NameToLayer ("Obstacle")) | (1 << LayerMask.NameToLayer ("Ground"));
		playerInfo = player.GetComponent<PlayerScript> ();
	}

	void FixedUpdate() {
		if (!canUpdate ())
			return;
		updatePatternStateVariables ();
		updateAimPosition ();

		RaycastHit hitInfo;
		if (firing && Physics.SphereCast (startPoint.position, beamWidth, endPoint - startPoint.position, out hitInfo, maxRange)) {
			if (hitInfo.collider.gameObject.layer == player.layer) {
				playerInfo.hitpoints -= damagePerSecond * Time.deltaTime;
				PlayerScript.registerHit ();
			}
		}
	}

	public void fire(Transform newTarget, int newIdxInPattern, float chargeUpTime, float firingTime, int newChargeUpMaterialIdx, int newFiringMaterialIdx, int newFiringPattern) {
		if (!gameObject.activeInHierarchy)
			gameObject.SetActive (true);
		transform.localRotation = Quaternion.identity;
		CancelInvoke ();
		target = newTarget;
		idxInPattern = newIdxInPattern;
		chargingUp = false;
		firing = false;
		currentFiringTime = firingTime;
		chargeUpMaterialIdx = newChargeUpMaterialIdx;
		firingMaterialIdx = newFiringMaterialIdx;
		currentFiringPattern = newFiringPattern;
		aimPosition = target.position;
		setInitialPatternStateVariables ();
		startChargingUp (chargeUpTime);
	}

	void startChargingUp(float timeUntilFire) {
		if (timeUntilFire <= 0.01f) {
			startFiring ();
			return;
		}
		//Debug.Log ("Started charging up");
		chargingUp = true;
		lineRenderer.material = beamMaterials.materials [chargeUpMaterialIdx];
		lineRenderer.enabled = true;
		if (IsInvoking ("startFiring"))
			CancelInvoke ("startFiring");
		Invoke ("startFiring", timeUntilFire);
	}

	void startFiring() {
		//Debug.Log ("Started firing");
		firing = true;
		lineRenderer.material = beamMaterials.materials [firingMaterialIdx];
		lineRenderer.enabled = true;
		if (IsInvoking ("stopFiring"))
			CancelInvoke ("stopFiring");
		Invoke ("stopFiring", currentFiringTime);
	}

	void stopFiring() {
		chargingUp = false;
		firing = false;
		lineRenderer.enabled = false;
		gameObject.SetActive (false);
		//Debug.Log ("Stopped firing");
	}

	Vector3 getBeamEndpoint() {
		RaycastHit hitInfo;
		Vector3 adjustedTarget = aimPosition + aimOffset;
		if (Physics.SphereCast (startPoint.position, beamWidth, adjustedTarget - startPoint.position, out hitInfo, maxRange, beamObstructionLayer)) {
			return hitInfo.point;
		}
		else {
			Vector3 beamVector = adjustedTarget - startPoint.position;
			float scaleFac = maxRangeSquared / beamVector.sqrMagnitude;
			beamVector *= scaleFac;
			return startPoint.position + beamVector;
		}
	}

	void setInitialPatternStateVariables() {
		if (useCurrentMechProjectileStartPosAsStartPoint)
			startPoint = mechShootScript.currentProjectileStartPos;
		if (currentFiringPattern == DEFAULT_PATTERN) {
			aimOffset = Vector3.zero;
			aimUpdateLerpStep = aimLerpStep_DEFAULT_PATTERN;
		}
		else if (currentFiringPattern == PATTERN_2) {
			float angleIncrement = 0;
			angleIncrement = (Mathf.PI * 2) / 5;
			float radius = 1;
			float angle = idxInPattern * angleIncrement;
			Vector3 normalizedVecToTarget = (target.position - startPoint.position).normalized;
			Vector3 vecToCrossWith = Vector3.up;
			if (normalizedVecToTarget == Vector3.up)
				vecToCrossWith = Vector3.right;
			Vector3 perpVec1 = Vector3.Cross (normalizedVecToTarget, vecToCrossWith);
			Vector3 perpVec2 = Vector3.Cross (normalizedVecToTarget, perpVec1);
			aimOffset = perpVec1 * pattern2Radius * Mathf.Cos (angle) + perpVec2 * pattern2Radius * Mathf.Sin (angle);
			aimUpdateLerpStep = aimLerpStep_PATTERN_2;
		}
	}

	void updatePatternStateVariables() {
		if (currentFiringPattern == PATTERN_2) {
			aimOffset = Vector3.MoveTowards (aimOffset, Vector3.zero, pattern2AimOffsetStep * Time.deltaTime);
		}
	}

	void updateAimPosition() {
		aimPosition = Vector3.Lerp (aimPosition, target.position, aimUpdateLerpStep * Time.deltaTime);
	}

	bool canUpdate() {
		if (!chargingUp && !firing)
			return false;
		if (useCurrentMechProjectileStartPosAsStartPoint && mechShootScript.currentProjectileStartPos.position == null)
			return false;
		return true;
	}

	// Update is called once per frame
	void Update () {
		if (!canUpdate ())
			return;
		if (useCurrentMechProjectileStartPosAsStartPoint)
			startPoint = mechShootScript.currentProjectileStartPos;
		lineRenderer.SetPosition (0, startPoint.position);
		endPoint = getBeamEndpoint ();
		lineRenderer.SetPosition (1, endPoint);
	}
}
