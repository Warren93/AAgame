using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossWaypoint : MonoBehaviour {

	public List<BossWaypoint> ConnectedWaypoints = new List<BossWaypoint>();

	public BossWaypoint GetNextWaypoint(BossWaypoint prevWaypoint)
	{
		var candidates = ConnectedWaypoints;
		if (prevWaypoint != null)
			candidates = ConnectedWaypoints.FindAll((wp) => { return wp != prevWaypoint; });
		if (candidates.Count == 0)
			return prevWaypoint;
		var r = Random.Range(0, candidates.Count);
		return candidates[r];
	}
}
