using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPoolerScript : MonoBehaviour {

	public bool poolEnabled;

	public static ObjectPoolerScript objectPooler;

	public List<GameObject> pooledEnemyBullets;
	public GameObject enemyBulletPrefab;
	public int initialEnemyBulletPoolSize = 200;

    public List<GameObject> pooledEnemyBubbleBullets;
    public GameObject enemyBubbleBulletPrefab;
    public int initialEnemyBubbleBulletPoolSize = 200;

    public List<GameObject> pooledEnemyFlakBullets;
	public GameObject enemyFlakBulletPrefab;
	public int initialEnemyFlakBulletPoolSize = 200;

	public List<GameObject> pooledPlayerBullets;
	public GameObject playerBulletPrefab;
	public int initialPlayerBulletPoolSize = 200;

	public List<GameObject> pooledBulletLinks;
	public GameObject bulletLinkPrefab;
	public int initialBulletLinkPoolSize = 200;

	public List<GameObject> pooledMissiles;
	public GameObject missilePrefab;
	public int initialMissilePoolSize = 200;

	public List<HitEffectScript> pooledHitEffects;
	public GameObject hitEffectPrefab;
	public int initialHitEffectPoolSize = 200;

	void Awake() {
		objectPooler = this;
	}

	// Use this for initialization
	void Start () {

		if (!poolEnabled)
			return;

		// DETERMINE WHAT PLAYER BULLETS SHOULD DO EXTRA COLLISION CHECKING FOR
		LayerMask maskForPlayerBullets = (1 << LayerMask.NameToLayer ("Default"))
				| (1 << LayerMask.NameToLayer ("AirBoss"))
				| (1 << LayerMask.NameToLayer ("Ground"));

		// create the initial enemy bullets
		pooledEnemyBullets = new List<GameObject> ();
		for (int i = 0; i < initialEnemyBulletPoolSize; i++) {
			GameObject newBullet = (GameObject)Instantiate(enemyBulletPrefab);
			newBullet.SetActive(false);
			pooledEnemyBullets.Add(newBullet);
		}
        // create the initial enemy flak bullets
        pooledEnemyFlakBullets = new List<GameObject> ();
		for (int i = 0; i < initialEnemyFlakBulletPoolSize; i++) {
			GameObject newBullet = (GameObject)Instantiate(enemyFlakBulletPrefab);
			newBullet.SetActive(false);
			pooledEnemyFlakBullets.Add(newBullet);
		}
		// create the initial player bullets
		pooledPlayerBullets = new List<GameObject> ();
		for (int i = 0; i < initialPlayerBulletPoolSize; i++) {
			GameObject newBullet = (GameObject)Instantiate(playerBulletPrefab);
			newBullet.SetActive(false);
			newBullet.GetComponent<PlayerBulletScript>().relevantLayers = maskForPlayerBullets;
			pooledPlayerBullets.Add(newBullet);
		}
		pooledBulletLinks = new List<GameObject> ();
		for (int i = 0; i < initialBulletLinkPoolSize; i++) {
			GameObject newBulletLink = (GameObject)Instantiate(bulletLinkPrefab);
			newBulletLink.SetActive(false);
			newBulletLink.GetComponent<BulletLinkScript>().Init();
			pooledBulletLinks.Add(newBulletLink);
		}

		pooledHitEffects = new List<HitEffectScript> ();
		for (int i = 0; i < initialHitEffectPoolSize; i++) {
			GameObject newHitEffect = (GameObject)Instantiate(hitEffectPrefab);
			newHitEffect.SetActive(false);
			pooledHitEffects.Add(newHitEffect.GetComponent<HitEffectScript>());
		}


		// Level 2 stuff
		if (Application.loadedLevel == 2) {
			pooledMissiles = new List<GameObject> ();
			for (int i = 0; i < initialMissilePoolSize; i++) {
				GameObject newMissile = (GameObject)Instantiate (missilePrefab);
				newMissile.SetActive (false);
				pooledMissiles.Add (newMissile);
			}
            pooledEnemyBubbleBullets = new List<GameObject>();
            for (int i = 0; i < initialEnemyBubbleBulletPoolSize; i++)
            {
                GameObject newBubbleBullet = (GameObject)Instantiate(enemyBubbleBulletPrefab);
                newBubbleBullet.SetActive(false);
                pooledEnemyBullets.Add(newBubbleBullet);
            }
        }
	}

	public GameObject getEnemyBullet() {
		GameManagerScript.numActiveBullets++;
		for (int i = 0; i < pooledEnemyBullets.Count; i++)
			if (!pooledEnemyBullets[i].activeInHierarchy)
				return pooledEnemyBullets[i];
		GameObject newBullet = (GameObject)Instantiate (enemyBulletPrefab);
		newBullet.SetActive (false);
		pooledEnemyBullets.Add (newBullet);
		return newBullet;
	}

	public GameObject getEnemyFlakBullet() {
		for (int i = 0; i < pooledEnemyFlakBullets.Count; i++)
			if (!pooledEnemyFlakBullets[i].activeInHierarchy)
				return pooledEnemyFlakBullets[i];
		GameObject newBullet = (GameObject)Instantiate (enemyFlakBulletPrefab);
		newBullet.SetActive (false);
		pooledEnemyFlakBullets.Add (newBullet);
		return newBullet;
	}

	public GameObject getBulletLink() {
		for (int i = 0; i < pooledBulletLinks.Count; i++)
			if (!pooledBulletLinks[i].activeInHierarchy)
				return pooledBulletLinks[i];
		GameObject newBulletLink = (GameObject)Instantiate (bulletLinkPrefab);
		newBulletLink.SetActive (false);
		pooledBulletLinks.Add (newBulletLink);
		return newBulletLink;
	}

	public GameObject getPlayerBullet() {
		for (int i = 0; i < pooledPlayerBullets.Count; i++)
			if (!pooledPlayerBullets[i].activeInHierarchy)
				return pooledPlayerBullets[i];
		GameObject newBullet = (GameObject)Instantiate (playerBulletPrefab);
		newBullet.SetActive (false);
		pooledPlayerBullets.Add (newBullet);
		return newBullet;
	}

	public GameObject getMissile() {
		GameManagerScript.numActiveEnemyMissiles++;
		for (int i = 0; i < pooledMissiles.Count; i++)
			if (!pooledMissiles[i].activeInHierarchy)
				return pooledMissiles[i];
		GameObject newMissile = (GameObject)Instantiate (missilePrefab);
		newMissile.SetActive (false);
		pooledMissiles.Add (newMissile);
		return newMissile;
	}

    public GameObject getEnemyBubbleBullet()
    {
        GameManagerScript.numActiveBullets++;
        for (int i = 0; i < pooledEnemyBubbleBullets.Count; i++)
            if (!pooledEnemyBubbleBullets[i].activeInHierarchy)
                return pooledEnemyBubbleBullets[i];
        GameObject newBubbleBullet = (GameObject)Instantiate(enemyBubbleBulletPrefab);
        newBubbleBullet.SetActive(false);
        pooledEnemyBullets.Add(newBubbleBullet);
        return newBubbleBullet;
    }

    public GameObject getHitEffect() {
		for (int i = 0; i < pooledHitEffects.Count; i++) {
			if (!pooledHitEffects[i].gameObject.activeInHierarchy) {
				pooledHitEffects[i].initiateSelfDestruct();
				return pooledHitEffects[i].gameObject;
			}
		}
		GameObject newHitEffect = (GameObject)Instantiate (hitEffectPrefab);
		newHitEffect.SetActive (false);
		HitEffectScript hitEffectInfo = newHitEffect.GetComponent<HitEffectScript> ();
		pooledHitEffects.Add (hitEffectInfo);
		hitEffectInfo.initiateSelfDestruct();
		return newHitEffect.gameObject;
	}
	
	// Update is called once per frame
	/*
	void Update () {
		//Debug.Log("Approx. memory: " + System.GC.GetTotalMemory (false));
		Debug.Log ("enemy bullets in pool: " + pooledEnemyBullets.Count);
		           //+ ", and player bullets: " + pooledPlayerBullets.Count);
	}
	*/
}
