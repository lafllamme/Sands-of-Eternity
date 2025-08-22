using UnityEngine;

public class OrthoFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 10, -10);

    void LateUpdate()
    {
        if (!target) return;
        transform.position = target.position + offset;
        // Rotation bleibt die vorgesetzte (X=45°, Y=0°)
    }
}