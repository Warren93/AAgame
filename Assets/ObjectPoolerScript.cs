using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPoolerScript : MonoBehaviour {

	public static ObjectPoolerScript objectPooler;
	public List<GameObject> pooledEnemyBullets;
	public GameObject enemyBulletPrefab;
	public int initialEnemyBulletPoolSize = 200;

	public List<GameObject> pooledPlayerBullets;
	public GameObject playerBulletPrefab;
	public int initialPlayerBulletPoolSize = 200;

	public List<GameObject> pooledBulletLinks;
	public GameObject bulletLinkPrefab;
	public int initialBulletLinkPoolSize = 200;
		
	void Awake() {
		objectPooler = this;
	}

	// Use this for initialization
	void Start () {
		// create the initial enemy bullets
		pooledEnemyBullets = new List<GameObject> ();
		for (int i = 0; i < initialEnemyBulletPoolSize; i++) {
			GameObject newBullet = (GameObject)Instantiate(enemyBulletPrefab);
			newBullet.SetActive(false);
			pooledEnemyBullets.Add(newBullet);
		}
		// create the initial player bullets
		pooledPlayerBullets = new List<GameObject> ();
		for (int i = 0; i < initialPlayerBulletPoolSize; i++) {
			GameObject newBullet = (GameObject)Instantiate(playerBulletPrefab);
			newBullet.SetActive(false);
			pooledPlayerBullets.Add(newBullet);
		}
		pooledBulletLinks = new List<GameObject> ();
		for (int i = 0; i < initialBulletLinkPoolSize; i++) {
			GameObject newBulletLink = (GameObject)Instantiate(bulletLinkPrefab);
			newBulletLink.SetActive(false);
			newBulletLink.GetComponent<BulletLinkScript>().Init();
			pooledBulletLinks.Add(newBulletLink);
		}
	}

	public GameObject getEnemyBullet() {
		for (int i = 0; i < pooledEnemyBullets.Count; i++)
			if (!pooledEnemyBullets[i].activeInHierarchy)
				return pooledEnemyBullets[i];
		GameObject newBullet = (GameObject)Instantiate (enemyBulletPrefab);
		newBullet.SetActive (false);
		pooledEnemyBullets.Add (newBullet);
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
	
	// Update is called once per frame
	/*
	void Update () {
		Debug.Log ("enemy bullets in pool: " + pooledEnemyBullets.Count
		           + ", and player bullets: " + pooledPlayerBullets.Count);
	}
	*/
}
