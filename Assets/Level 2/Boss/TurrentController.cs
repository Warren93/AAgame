using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurrentController : MonoBehaviour {

    PlayerScript player;

    public List<BasicGunInfo> LeftGuns = new List<BasicGunInfo>(2);
    public List<BasicGunInfo> RightGuns = new List<BasicGunInfo>(2);
    public BasicGunInfo MainGun;
    public int RearGunsRowLength;
    public Transform RearGunsArray;
    List<Transform> rearGuns = new List<Transform>();

    float turretTurnRate = 10;
    Transform turretBase;
    Vector3 offsetFromBase;

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
}
