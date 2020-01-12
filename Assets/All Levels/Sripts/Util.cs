using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Util {
    public static Vector3 closestPointOnTransformedBounds(Transform trans, Vector3 point)
    {
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


    public static T getClosest<T>(Vector3 point, IEnumerable<T> objects) where T: Component
    {
        T closest = null;
        float distToClosest = Mathf.Infinity;
        foreach (var obj in objects)
        {
            var distToCurrent = Vector3.Distance(point, obj.transform.position);
            if (closest == null || distToCurrent < distToClosest)
            {
                closest = obj;
                distToClosest = distToCurrent;
            }
        }
        return closest;
    }

    public static float getFlatDist(Vector3 a, Vector3 b)
    {
        Vector2 a2d = new Vector2(a.x, a.z);
        Vector2 b2d = new Vector2(b.x, b.z);
        return Vector2.Distance(a2d, b2d);
    }
}
