using UnityEngine;
using System.Collections;

public class HitEffectScript : MonoBehaviour {

	public void initiateSelfDestruct () {
		Invoke ("selfDestruct", 0.2f);
	}

	void selfDestruct() {
		gameObject.SetActive (false);
	}
}
