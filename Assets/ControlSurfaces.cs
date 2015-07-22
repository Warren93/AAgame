using UnityEngine;
using System.Collections;

public class ControlSurfaces : MonoBehaviour {

	Transform r_rudder, r_canard, r_elevon, l_rudder, l_canard, l_elevon;
	Quaternion r_rudder_rot, r_canard_rot, r_elevon_rot, l_rudder_rot, l_canard_rot, l_elevon_rot; // starting local rotations

	Quaternion r_elevon_up_rot, r_elevon_down_rot, r_canard_up_rot, r_canard_down_rot,
				l_elevon_up_rot, l_elevon_down_rot, l_canard_up_rot, l_canard_down_rot; // fixed max rotations for rolling

	GameObject r_exhaust, m_exhaust, l_exhaust, l_thruster, r_thruster, top_thruster, bottom_thruster;

	int mlb;

	float roll_sensitivity = 5;
	float pitch_sensitivity = 5;
	float yaw_sensitivity = 1.75f;
	float fixed_roll_angle = 30; // the max angle that a control surface can rotate for roll control alone

	// Use this for initialization
	void Start () {
		mlb = GetComponent<PlayerScript> ().mlb;

		r_rudder = transform.GetChild (2);
		r_canard = transform.GetChild (3);
		r_elevon = transform.GetChild (4);
		l_rudder = transform.GetChild (5);
		l_canard = transform.GetChild (6);
		l_elevon = transform.GetChild (7);

		r_exhaust = transform.GetChild (9).gameObject;
		m_exhaust = transform.GetChild (10).gameObject;
		l_exhaust = transform.GetChild (11).gameObject;
		l_thruster = transform.GetChild (12).gameObject;
		r_thruster = transform.GetChild (13).gameObject;
		top_thruster = transform.GetChild (14).gameObject;
		bottom_thruster = transform.GetChild (15).gameObject;

		r_rudder_rot = r_rudder.localRotation;
		r_canard_rot = r_canard.localRotation;
		r_elevon_rot = r_elevon.localRotation;
		l_rudder_rot = l_rudder.localRotation;
		l_canard_rot = l_canard.localRotation;
		l_elevon_rot = l_elevon.localRotation;

		r_elevon_up_rot = getFixedRot (r_elevon, fixed_roll_angle);
		r_elevon_down_rot = getFixedRot (r_elevon, -2 * fixed_roll_angle);
		l_elevon_up_rot = getFixedRot (l_elevon, fixed_roll_angle);
		l_elevon_down_rot = getFixedRot (l_elevon, -2 * fixed_roll_angle);

		r_canard_up_rot = getFixedRot (r_canard, fixed_roll_angle);
		r_canard_down_rot = getFixedRot (r_canard, -2 * fixed_roll_angle);
		l_canard_up_rot = getFixedRot (l_canard, fixed_roll_angle);
		l_canard_down_rot = getFixedRot (l_canard, -2 * fixed_roll_angle);
	}
	
	// Update is called once per frame
	void Update () {
		float deltaMouseX, deltaMouseY;
		deltaMouseX = Input.GetAxis ("Mouse X");
		deltaMouseY = Input.GetAxis ("Mouse Y");

		// gradually rotate all control surfaces back to their original local rotations
		r_elevon.localRotation = Quaternion.Slerp (r_elevon.localRotation, r_elevon_rot, 10 * Time.deltaTime);
		l_elevon.localRotation = Quaternion.Slerp (l_elevon.localRotation, l_elevon_rot, 10 * Time.deltaTime);
		r_canard.localRotation = Quaternion.Slerp (r_canard.localRotation, r_canard_rot, 10 * Time.deltaTime);
		l_canard.localRotation = Quaternion.Slerp (l_canard.localRotation, l_canard_rot, 10 * Time.deltaTime);
		r_rudder.localRotation = Quaternion.Slerp (r_rudder.localRotation, r_rudder_rot, 10 * Time.deltaTime);
		l_rudder.localRotation = Quaternion.Slerp (l_rudder.localRotation, l_rudder_rot, 10 * Time.deltaTime);

		// THRUSTERS
		if (Input.GetKey(KeyCode.A))
			r_thruster.SetActive(true);
		if (Input.GetKey(KeyCode.D))
			l_thruster.SetActive(true);
		if (Input.GetKey(KeyCode.W))
			bottom_thruster.SetActive(true);
		if (Input.GetKey(KeyCode.S))
			top_thruster.SetActive(true);

		if (!Input.GetKey(KeyCode.A))
			r_thruster.SetActive(false);
		if (!Input.GetKey(KeyCode.D))
			l_thruster.SetActive(false);
		if (!Input.GetKey(KeyCode.W))
			bottom_thruster.SetActive(false);
		if (!Input.GetKey(KeyCode.S))
			top_thruster.SetActive(false);

		if (Input.GetKey(KeyCode.LeftShift)
		    && !(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)))
			m_exhaust.SetActive(true);
		else
			m_exhaust.SetActive(false);

		// ROLL CONTROL
		// roll left
		if (Input.GetKey (KeyCode.Q)) {
			// elevons
			r_elevon.localRotation = Quaternion.Slerp (r_elevon.localRotation, r_elevon_down_rot, 15 * Time.deltaTime);
			l_elevon.localRotation = Quaternion.Slerp (l_elevon.localRotation, l_elevon_up_rot, 15 * Time.deltaTime);
			// canards
			r_canard.localRotation = Quaternion.Slerp (r_canard.localRotation, r_canard_down_rot, 15 * Time.deltaTime);
			l_canard.localRotation = Quaternion.Slerp (l_canard.localRotation, l_canard_up_rot, 15 * Time.deltaTime);
		}
		// roll right
		if (Input.GetKey (KeyCode.E)) {
			// elevons
			r_elevon.localRotation = Quaternion.Slerp (r_elevon.localRotation, r_elevon_up_rot, 15 * Time.deltaTime);
			l_elevon.localRotation = Quaternion.Slerp (l_elevon.localRotation, l_elevon_down_rot, 15 * Time.deltaTime);
			// canards
			r_canard.localRotation = Quaternion.Slerp (r_canard.localRotation, r_canard_up_rot, 15 * Time.deltaTime);
			l_canard.localRotation = Quaternion.Slerp (l_canard.localRotation, l_canard_down_rot, 15 * Time.deltaTime);
		}

		// if player is using mouse look, then they're not using mouse axis controls, so return early
		if (Input.GetMouseButton (mlb))
			return;

		// PITCH CONTROL
		// elevons
		r_elevon.RotateAround(r_elevon.position, r_elevon.transform.right, deltaMouseY * pitch_sensitivity);
		l_elevon.RotateAround(l_elevon.position, l_elevon.transform.right, deltaMouseY * pitch_sensitivity);
		// canards
		r_canard.RotateAround(r_canard.position, r_canard.transform.right, -deltaMouseY * pitch_sensitivity);
		l_canard.RotateAround(l_canard.position, l_canard.transform.right, -deltaMouseY * pitch_sensitivity);

		// YAW CONTROL
		// rudders
		r_rudder.RotateAround(r_rudder.position, r_rudder.transform.up, -deltaMouseX * yaw_sensitivity);
		l_rudder.RotateAround(l_rudder.position, l_rudder.transform.up, -deltaMouseX * yaw_sensitivity);
	}

	Quaternion getFixedRot(Transform input, float angle) {
		Transform dummy;
		dummy = input;
		dummy.RotateAround (input.position, input.right, angle);
		return dummy.rotation;
	}
}
