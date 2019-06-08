using UnityEngine;
using System.Collections;

public class MechWanderTarget : MonoBehaviour {

    MechScript3 mechScript;

    public static int AIR = 0;
    public static int GROUND = 1;
    public static int LAND_ON_NEAREST_GROUND = 2;
    public static int WANDER_PLATFORM = 3;

    public int wanderType = 0;
    Vector3 airSpaceOriginPoint;
    public Vector3 currentPosition;
    Vector3 currentDestination;
    float verticalSpread = 100;
    float horizontalSpread = 500; // was 100
    float destinationChangeRate = 10;
    float speedChangeRate = 2;
    float turnRate = 10 * Mathf.Deg2Rad;

    bool canDrawGizmos = false;

    // min and max were 60 and 100
    float minSpeed = 100;
    float maxSpeed = 200;

    float speedStep = 5;
    float currentSpeed;
    float desiredSpeed;

    //TerrainCollider terrainCol;
	Collider terrainCol;

    GameObject lastObjLandedOn = null;
    float proximityDist;

    // Use this for initialization
    void Start () {

        mechScript = gameObject.GetComponent<MechScript3>();

        airSpaceOriginPoint = Vector3.up * 700; // was x500, then x350
		terrainCol = GameObject.Find("Terrain").GetComponent<Collider>();//.GetComponent<TerrainCollider>();
		horizontalSpread = terrainCol.bounds.extents.x - 50;
        startWanderTarget();
        Invoke("enableGizmos", 0.5f);
    }
	
    void enableGizmos() {
        canDrawGizmos = true;
    }

    public void startWanderTarget () {

        lastObjLandedOn = mechScript.lastObjLandedOn;

        // cancel these functions if the target was never stopped
        stopWanderTarget();

        if (wanderType == WANDER_PLATFORM && lastObjLandedOn != null) {

            proximityDist = lastObjLandedOn.GetComponent<Collider>().bounds.size.magnitude * 0.1f;

            Vector3 randomPosition = lastObjLandedOn.transform.position
                + lastObjLandedOn.transform.right * Random.Range(-lastObjLandedOn.transform.localScale.x * 0.5f, lastObjLandedOn.transform.localScale.x * 0.5f)
                + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y * 0.5f
                + lastObjLandedOn.transform.forward * Random.Range(-lastObjLandedOn.transform.localScale.z * 0.5f, lastObjLandedOn.transform.localScale.z * 0.5f);
            //InvokeRepeating("changeDestination", Random.Range(0.5f, 2.0f), 2);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPosition, out hit, 5, NavMesh.AllAreas))
                currentPosition = hit.position;
            else
                currentPosition = transform.position;

            changeDestination();
            return;
        }

        // start near mech
        currentPosition = airSpaceOriginPoint + new Vector3(Random.Range(-horizontalSpread, horizontalSpread), Random.Range(-verticalSpread, verticalSpread), Random.Range(-horizontalSpread, horizontalSpread));

        // set random starting destination
        changeDestination();

        // begin randomly changing direction at constant intervals
        InvokeRepeating("changeDestination", Random.Range(0.5f, 2.0f), destinationChangeRate);
    }

    public void stopWanderTarget() {
        if (IsInvoking("changeDestination"))
            CancelInvoke("changeDestination");
    }

    void changeDestination() {

        if (wanderType == WANDER_PLATFORM) {
            if (lastObjLandedOn == null)
                return;
            currentDestination = lastObjLandedOn.transform.position
                + lastObjLandedOn.transform.right * Random.Range(-lastObjLandedOn.transform.localScale.x * 0.5f, lastObjLandedOn.transform.localScale.x * 0.5f)
                + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y * 0.5f
                + lastObjLandedOn.transform.forward * Random.Range(-lastObjLandedOn.transform.localScale.z * 0.5f, lastObjLandedOn.transform.localScale.z * 0.5f);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(currentDestination, out hit, 5, NavMesh.AllAreas))
                currentDestination = hit.position;
            else {
                currentDestination = transform.position;
                Debug.Log("failed to find new destination on navmesh");
            }

            currentSpeed = Vector3.Distance(currentPosition, currentDestination) / destinationChangeRate;
            currentSpeed = Mathf.Clamp(currentSpeed, 80, 1000);
            return;
        }


        currentDestination = airSpaceOriginPoint + new Vector3(Random.Range(-horizontalSpread, horizontalSpread), Random.Range(-verticalSpread, verticalSpread), Random.Range(-horizontalSpread, horizontalSpread)) + airSpaceOriginPoint;
        currentSpeed = Vector3.Distance(currentPosition, currentDestination) / destinationChangeRate;
    }

    // Update is called once per frame
    void Update () {

        lastObjLandedOn = mechScript.lastObjLandedOn;

        /*
        if (lastObjLandedOn != null)
        {
            Debug.DrawRay(lastObjLandedOn.transform.position + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y, lastObjLandedOn.transform.right * lastObjLandedOn.transform.localScale.x * 0.5f, Color.red);
            Debug.DrawRay(lastObjLandedOn.transform.position + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y, lastObjLandedOn.transform.right * -lastObjLandedOn.transform.localScale.x * 0.5f, Color.red);
            Debug.DrawRay(lastObjLandedOn.transform.position + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y, lastObjLandedOn.transform.forward * lastObjLandedOn.transform.localScale.z * 0.5f, Color.red);
            Debug.DrawRay(lastObjLandedOn.transform.position + lastObjLandedOn.transform.up * lastObjLandedOn.transform.localScale.y, lastObjLandedOn.transform.forward * -lastObjLandedOn.transform.localScale.z * 0.5f, Color.red);
            Debug.DrawLine(transform.position, lastObjLandedOn.transform.position, Color.yellow);
        }
        else
            Debug.Log("Last object landed on is null");
            */

        // move target position based on speed and current direction
        currentPosition += (currentDestination - currentPosition).normalized * currentSpeed * Time.deltaTime;

        // don't let the target go off the map (i.e. off the edge of the terrain)
        float border = 5;
        float xMax = terrainCol.bounds.center.x + terrainCol.bounds.extents.x - border;
        float xMin = terrainCol.bounds.center.x - terrainCol.bounds.extents.x + border;
        float zMax = terrainCol.bounds.center.z + terrainCol.bounds.extents.z - border;
        float zMin = terrainCol.bounds.center.z - terrainCol.bounds.extents.z + border;
        // x axis
        if (currentPosition.x > xMax)
            currentPosition.x = xMax;
        if (currentPosition.x < xMin)
            currentPosition.x = xMin;
        // z axis
        if (currentPosition.z > zMax)
            currentPosition.z = zMax;
        if (currentPosition.z < zMin)
            currentPosition.z = zMin;


        // prevent target from going underground
        float alt = getAltitudeOverTerrainOnly(currentPosition + Vector3.up * 1000) - 1000;

        // if wander type is "ground," snap wander target to terrain
        if (wanderType == GROUND && alt != Mathf.Infinity && alt != Mathf.NegativeInfinity) {
            if (alt < 0)
                currentPosition.y += Mathf.Abs(alt);
            else
                currentPosition.y -= alt;
        }
        else if (alt < 0 && alt > Mathf.NegativeInfinity) // also prevent target from going below ground even if wander type is not "ground"
            currentPosition.y += Mathf.Abs(alt);

        //Debug.Log("alt was " + alt + " and is now " + (getAltitudeOverTerrainOnly(currentPosition + Vector3.up * 1000) - 1000));

        //Debug.Log("position is now " + currentPosition);

        Debug.DrawLine(transform.position, currentPosition, Color.Lerp(Color.red, Color.yellow, 0.5f));
        Debug.DrawLine(currentPosition, currentDestination, Color.magenta);

        if (wanderType == WANDER_PLATFORM) {

            if (Vector3.Distance(transform.position, currentDestination) < proximityDist)
                changeDestination();

            Debug.DrawRay(currentDestination, Vector3.up * 1000, Color.cyan);
            Debug.DrawRay(currentPosition, Vector3.up * 1000, Color.green);
        }

    }

    float getAltitude(Vector3 pos)
    {
        RaycastHit hitInfo;
        bool didHit = Physics.Raycast(pos, Vector3.up * -1, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("Ground"));
        if (didHit)
        {
            return Vector3.Distance(hitInfo.point, pos);
        }
        else if (Physics.Raycast(pos, Vector3.up, out hitInfo, Mathf.Infinity, 1 << LayerMask.NameToLayer("Ground")))
        {
            return -1 * Vector3.Distance(hitInfo.point, pos);
        }
        return Mathf.Infinity;
    }

    float getAltitudeOverTerrainOnly(Vector3 pos)
    {
        RaycastHit hitInfo;
        Ray downRay = new Ray(pos, Vector3.up * -1);
        Ray upRay = new Ray(pos, Vector3.up);

        if (terrainCol.Raycast(downRay, out hitInfo, Mathf.Infinity))
        {
            return Vector3.Distance(hitInfo.point, pos);
        }
        else if (terrainCol.Raycast(upRay, out hitInfo, Mathf.Infinity))
        {
            return -1 * Vector3.Distance(hitInfo.point, pos);
        }
        return Mathf.Infinity;
    }


    void OnDrawGizmos()
    {
        if (!canDrawGizmos)
            return;

        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawSphere(currentPosition, 5);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(currentDestination, proximityDist);

        /*
        float border = 5;
        float xMax = GameObject.Find("Terrain").GetComponent<TerrainCollider>().bounds.center.x + terrainCol.bounds.extents.x - border;
        float xMin = GameObject.Find("Terrain").GetComponent<TerrainCollider>().bounds.center.x - terrainCol.bounds.extents.x + border;
        float zMax = GameObject.Find("Terrain").GetComponent<TerrainCollider>().bounds.center.z + terrainCol.bounds.extents.z - border;
        float zMin = GameObject.Find("Terrain").GetComponent<TerrainCollider>().bounds.center.z - terrainCol.bounds.extents.z + border;

        Gizmos.DrawSphere(new Vector3(xMax, 0, zMax), 50);
        Gizmos.DrawSphere(new Vector3(xMax, 0, zMin), 50);
        Gizmos.DrawSphere(new Vector3(xMin, 0, zMax), 50);
        Gizmos.DrawSphere(new Vector3(xMin, 0, zMin), 50);
        */

    }
}
