using UnityEngine;
using System.Collections;

public class test : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        //Debug.DrawLine(closestPointOnTransformedBounds(GetComponent<Collider>(), GameObject.FindWithTag("Player").transform.position), GameObject.FindWithTag("Player").transform.position, Color.red);

        /*
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Ground")) {
            if (obj.name == "Terrain")
                continue;
            Debug.DrawRay(obj.transform.position + obj.transform.up * obj.transform.localScale.y, obj.transform.right * obj.transform.localScale.x * 0.5f * 10, Color.red);
            Debug.DrawRay(obj.transform.position + obj.transform.up * obj.transform.localScale.y, obj.transform.right * -obj.transform.localScale.x * 0.5f, Color.red);
            Debug.DrawRay(obj.transform.position + obj.transform.up * obj.transform.localScale.y, obj.transform.forward * obj.transform.localScale.z * 0.5f, Color.red);
            Debug.DrawRay(obj.transform.position + obj.transform.up * obj.transform.localScale.y, obj.transform.forward * -obj.transform.localScale.z * 0.5f, Color.red);
            Debug.DrawLine(transform.position, obj.transform.position, Color.yellow);
        }
        */

    }

    void OnDrawGizmos() {
        //Gizmos.color = Color.red;
        //Gizmos.DrawSphere(GetComponent<Collider>().ClosestPointOnBounds(GameObject.FindWithTag("Player").transform.position), 20);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(closestPointOnTransformedBounds(transform, GameObject.FindWithTag("Player").transform.position) /*- Vector3.up * 10*/, 20);
    }

    Vector3 closestPointOnTransformedBounds(Transform trans, Vector3 point) {
        // get point in transform's coord frame
        Vector3 pointInLocal = trans.InverseTransformPoint(point);
        // clamp point to be within box defined by transform
        pointInLocal.x = Mathf.Clamp(pointInLocal.x, -0.5f, 0.5f);
        pointInLocal.y = Mathf.Clamp(pointInLocal.y, -0.5f, 0.5f);
        pointInLocal.z = Mathf.Clamp(pointInLocal.z, -0.5f, 0.5f);
        // return result
        Vector3 retval = trans.TransformPoint(pointInLocal);
        return retval;
    }
}
