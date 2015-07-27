using UnityEngine;
using System.Collections;

public class StartScreenScript : MonoBehaviour {

	Rect infoBox;
	GUIStyle guiStyle;
	string instructions;

	// Use this for initialization
	void Start () {
		infoBox = new Rect (Screen.width * 0.25f, Screen.height * 0.25f, Screen.width * 0.9f, Screen.height * 0.5f);
		instructions =      "\tW, S, A, D - move up, down, left, and right."
						+ "\n\tMouse - control pitch and yaw."
						+ "\n\tQ, E - roll left and right."
						+ "\n\tLeft Shift - hold to move faster."
						+ "\n\tSpace - hold to move slower."
						+ "\n\tLeft Mouse Button - fire."
						+ "\n\tRight Mouse Button - hold to look around with mouse."
						+ "\n\tF - tap to lock onto enemy in crosshairs (dot), hold to point camera at locked enemy."
						+ "\n\tR - reset game."
						+ "\n\tPress any key to start.";
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.anyKey)
			Application.LoadLevel(1);
	}

	void OnGUI() {
		if (guiStyle == null) {
			guiStyle = new GUIStyle(GUI.skin.box);
			guiStyle.fontSize = 16;
			guiStyle.alignment = TextAnchor.UpperLeft;
		}

		GUI.contentColor = Color.white;
		GUI.Label (infoBox, instructions, guiStyle);
	}
}
