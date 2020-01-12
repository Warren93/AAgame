using UnityEngine;
using System.Collections;

public class BasicGunInfo : MonoBehaviour {

    public Transform PivotPoint;
    public Transform MuzzleTipPosition;
    public bool CanPivotLocalX, CanPivotLocalY;

    public void AimAt(Vector3 position) {
        Vector3 ptInLocal = PivotPoint.InverseTransformPoint(position);
        if (!CanPivotLocalX)
            ptInLocal.y = 0;
        if (!CanPivotLocalY)
            ptInLocal.x = 0;
        PivotPoint.rotation = Quaternion.LookRotation(PivotPoint.TransformPoint(ptInLocal) - PivotPoint.position, PivotPoint.up);
    }
}
