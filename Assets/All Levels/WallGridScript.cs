using UnityEngine;
using System.Collections;

public class WallGridScript : MonoBehaviour {

	MeshRenderer myMeshRenderer;
	Material myMaterial;
	GameObject player;
	public float warnDist = 75.0f;
	public Color gridColor = Color.white;
	Vector3 forwardDirection;

	// Use this for initialization
	void Start () {
		forwardDirection = transform.forward;
		myMeshRenderer = GetComponent<MeshRenderer> ();
		myMaterial = myMeshRenderer.material;
		player = GameObject.FindGameObjectWithTag ("Player");
	}
	
	// Update is called once per frame
	void Update () {
		//Debug.Log ("material name is " + myMeshRenderer.material.ToString () + ", and shader is " + myMeshRenderer.material.shader.ToString());
		//Debug.Log ("tint color is currently: " + myMeshRenderer.material.GetColor ("_TintColor"));
		//Debug.Log ("tint color is currently: " + myMeshRenderer.material.GetColor ("_Color"));

		Vector3 playerPos = player.transform.position;
		Vector3 gridPos = transform.position;
		float distToPlayer = Mathf.Infinity;

		if (forwardDirection.x != 0)
			distToPlayer = playerPos.x - gridPos.x;
		else if (forwardDirection.y != 0)
			distToPlayer = playerPos.y - gridPos.y;
		else
			distToPlayer = playerPos.z - gridPos.z;

		distToPlayer = Mathf.Abs (distToPlayer);

		//Debug.Log ("dist to player is " + distToPlayer);
		if (distToPlayer > warnDist && myMeshRenderer.enabled)
			myMeshRenderer.enabled = false;
		else if (distToPlayer <= warnDist) {
			//Debug.Log("here");
			myMeshRenderer.enabled = true;
			float alpha = (warnDist - distToPlayer) / warnDist;
			myMaterial.SetColor ("_Color", new Color (gridColor.r, gridColor.g, gridColor.b, alpha));
		}
	}
}
