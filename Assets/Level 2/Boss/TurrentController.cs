using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class TurrentController : MonoBehaviour {

    PlayerScript player;

    public List<BasicGunInfo> LeftGuns = new List<BasicGunInfo>(2);
    public List<BasicGunInfo> RightGuns = new List<BasicGunInfo>(2);
    public BasicGunInfo MainGun;
    public int RearGunsRowLength;
    public Transform RearGunsArray;
    List<Transform> rearGuns = new List<Transform>();

    float turretTurnRate = 5;
    Transform turretBase;
    Vector3 offsetFromBase;

    Coroutine currentAttack;
    const int NUM_ATTACK_TYPES = 1;
    Func<IEnumerator>[] attacks;
    float changeAttackInterval = 10;

	// Use this for initialization
	void Start () {
        turretBase = transform.parent;
        offsetFromBase = turretBase.InverseTransformPoint(transform.position);
        transform.parent = null;
        player = FindObjectOfType<PlayerScript>();
        List<BasicGunInfo> g = new List<BasicGunInfo>();
        foreach(Transform t in RearGunsArray) {
            if (t != RearGunsArray)
                rearGuns.Add(t);
        }

        attacks = new Func<IEnumerator>[1] {
            attack1
        };
        pickNewAttack();
    }
	
	// Update is called once per frame
	void Update () {

        transform.position = turretBase.TransformPoint(offsetFromBase);

        Vector3 playerPosLocal = transform.InverseTransformPoint(player.transform.position);
        playerPosLocal.y = 0;
        var tgtRot = Quaternion.LookRotation(transform.TransformPoint(playerPosLocal) - transform.position, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, tgtRot, turretTurnRate * Time.deltaTime);

        Vector3 target = player.transform.position;
        foreach (var gun in LeftGuns)
            gun.AimAt(target);
        foreach (var gun in RightGuns)
            gun.AimAt(target);
        MainGun.AimAt(target);
	}

    void pickNewAttack() {
        int newAtkIdx = Random.Range(0, NUM_ATTACK_TYPES);
        if (currentAttack != null)
            StopCoroutine(currentAttack);
        currentAttack = StartCoroutine(attacks[newAtkIdx]());
    }

    IEnumerator attack1() {
        while(true) {
            float maxRange = 2000;
            if (Vector3.Distance(MainGun.MuzzleTipPosition.position, player.transform.position) <= maxRange) {
                var bullet = ObjectPoolerScript.objectPooler.getEnemyBubbleBullet();
                var bulletInfo = bullet.GetComponent<EnemyBulletScript>();
                bulletInfo.damage = 10;
                bulletInfo.distanceTraveled = 0;
                bulletInfo.maxRange = maxRange;
                bulletInfo.speed = 250;
                bulletInfo.transform.position = MainGun.MuzzleTipPosition.position;
                float spread = 2;
                bulletInfo.transform.rotation = Quaternion.LookRotation(MainGun.PivotPoint.forward + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread), Random.Range(-spread, spread)));
                bulletInfo.delayedReactivateTrail();
                bulletInfo.gameObject.SetActive(true);
                Debug.Log("SHOOTING BUBBLE BULLET");
            }
            yield return new WaitForSeconds(0.3f);
        }
    }
}
