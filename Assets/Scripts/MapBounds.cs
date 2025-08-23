using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MapBounds : MonoBehaviour
{
    public static MapBounds I { get; private set; }

    BoxCollider box;

    void Awake()
    {
        I = this;
        box = GetComponent<BoxCollider>();
        // sicherstellen, dass es eine „feste“ Wand ist (kein Trigger)
        box.isTrigger = false;
    }

    public Vector3 ClampXZ(Vector3 pos, float padding = 0f)
    {
        // Welt-Bounds der Box
        Bounds b = box.bounds;

        float minX = b.min.x + padding;
        float maxX = b.max.x - padding;
        float minZ = b.min.z + padding;
        float maxZ = b.max.z - padding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
    }

    void OnDrawGizmosSelected()
    {
        var c = Gizmos.color;
        Gizmos.color = new Color(0,1,1,0.25f);
        if (TryGetComponent(out BoxCollider bc))
            Gizmos.DrawCube(bc.bounds.center, bc.bounds.size);
        Gizmos.color = c;
    }
}