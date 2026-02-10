using UnityEngine;
public interface IRootMotionReceiver
{
    public void OnProcessRootMotion(Vector3 deltaPosition, Quaternion deltaRotation);
}
