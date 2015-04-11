using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour {
	GameObject player;
	//float camAltAbovePlayer;
	//float camDistBehindPlayer;
	Vector3 vecFromPlayer;

	// new approach
	float lookY_Rotation = 0;
	float lookX_Rotation = 0;
	float lookZ_Rotation = 0;
	float mouseLookSensitivity = 200;

	float leftRightSlide = 0;
	float upDownSlide = 0;

	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag("Player");
		//transform.parent = player.transform;
		//camAltAbovePlayer = transform.position.y - player.transform.position.y;
		//camDistBehindPlayer = transform.position.z - player.transform.position.z;
		vecFromPlayer = transform.position - player.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (!player)
			return;
		//target = player.transform.position + (player.transform.up * camAltAbovePlayer);

	}


	void LateUpdate() {

		if (!player)
			return;

		float deltaMouseX, deltaMouseY, deltaZ, deltaUpDown, deltaLeftRight;
		deltaLeftRight = 0;
		deltaUpDown = 0;
		deltaZ = 0;
		deltaMouseX = Input.GetAxis ("Mouse X");
		deltaMouseY = Input.GetAxis ("Mouse Y");
		if (Input.GetKey(KeyCode.Q))
			deltaZ -= 1f;
		if (Input.GetKey(KeyCode.E))
			deltaZ += 1f;
		else if (!Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E))
			deltaZ = 0;

		// slide left right
		if (Input.GetKey(KeyCode.A))
			deltaLeftRight += 1f;
		if (Input.GetKey(KeyCode.D))
			deltaLeftRight -= 1f;
		else if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
			deltaLeftRight = 0;

		leftRightSlide += Time.deltaTime * deltaLeftRight * mouseLookSensitivity * 0.07f;
		leftRightSlide = Mathf.Clamp (leftRightSlide, -5, 5);

		// slide up/down
		if (Input.GetKey(KeyCode.W))
			deltaUpDown -= 1f;
		if (Input.GetKey(KeyCode.S))
			deltaUpDown += 1f;
		else if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
			deltaUpDown = 0;
		
		upDownSlide += Time.deltaTime * deltaUpDown * mouseLookSensitivity * 0.07f;
		upDownSlide = Mathf.Clamp (upDownSlide, -5, 2);


		transform.position = player.transform.position + (player.transform.rotation * vecFromPlayer);

		// slide left/right
		transform.position += player.transform.right * leftRightSlide;

		// slide up/down
		transform.position += player.transform.up * upDownSlide;

		transform.rotation = player.transform.rotation;
		lookY_Rotation += Time.deltaTime * deltaMouseX * mouseLookSensitivity * 0.07f;
		lookX_Rotation += Time.deltaTime * deltaMouseY * mouseLookSensitivity * 0.07f;
		lookZ_Rotation += Time.deltaTime * deltaZ * mouseLookSensitivity * 0.07f;

		lookX_Rotation = Mathf.Clamp(lookX_Rotation, -5, 5);
		lookY_Rotation = Mathf.Clamp(lookY_Rotation, -5, 5);
		lookZ_Rotation = Mathf.Clamp(lookZ_Rotation, -5, 5);

		transform.RotateAround(player.transform.position, player.transform.right, lookX_Rotation);
		transform.RotateAround(player.transform.position, player.transform.up, -lookY_Rotation);
		transform.RotateAround(player.transform.position, player.transform.forward, lookZ_Rotation);


		float defaultDecrement = Time.deltaTime * 8;

		if (deltaMouseX == 0) {
			float decrement = defaultDecrement;
			if (Mathf.Abs(lookX_Rotation) < defaultDecrement)
				decrement = Mathf.Abs(lookX_Rotation);
			if (lookX_Rotation > 0)
				lookX_Rotation -= decrement;
			else if (lookX_Rotation < 0)
				lookX_Rotation += decrement;
		}
		if (deltaMouseY == 0) {
			float decrement = defaultDecrement;
			if (Mathf.Abs(lookY_Rotation) < defaultDecrement)
				decrement = Mathf.Abs(lookY_Rotation);
			//Debug.Log("y rot is " + lookY_Rotation + ", abs val is " +  Mathf.Abs(lookY_Rotation) + ", decrement is " + decrement);
			if (lookY_Rotation > 0)
				lookY_Rotation -= decrement;
			else if (lookY_Rotation < 0)
				lookY_Rotation += decrement;
		}
		if (deltaZ == 0) {
			float decrement = defaultDecrement;
			if (Mathf.Abs(lookZ_Rotation) < defaultDecrement)
				decrement = Mathf.Abs(lookZ_Rotation);
			lookZ_Rotation -= lookZ_Rotation * decrement;
			if (Mathf.Abs(lookZ_Rotation) < 0.1f)
				lookZ_Rotation = 0;
		}
		if (deltaLeftRight == 0) {
			float decrement = defaultDecrement;
			if (Mathf.Abs(leftRightSlide) < defaultDecrement)
				decrement = Mathf.Abs(leftRightSlide);
			leftRightSlide -= leftRightSlide * decrement;
			if (Mathf.Abs(leftRightSlide) < 0.1f)
				leftRightSlide = 0;
		}
		if (deltaUpDown == 0) {
			float decrement = defaultDecrement;
			if (Mathf.Abs(upDownSlide) < defaultDecrement)
				decrement = Mathf.Abs(upDownSlide);
			upDownSlide -= upDownSlide * decrement;
			if (Mathf.Abs(upDownSlide) < 0.1f)
				upDownSlide = 0;
		}

		//Debug.Log ("dx: " + deltaMouseX + ", dy: " + deltaMouseY + ", dz: " + deltaZ);

		/*
		Debug.Log ("lookX_Rotation is " + lookX_Rotation
		           + ", lookY_Rotation is " + lookY_Rotation
		           + ", lookZ_Rotation is " + lookZ_Rotation);
*/		           

	}

}
