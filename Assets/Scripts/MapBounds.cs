using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MapBounds : MonoBehaviour
{
    public static MapBounds I { get; private set; }

    private BoxCollider box;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        box = GetComponent<BoxCollider>();
        box.isTrigger = false; // we want solid walls for clamping
    }

    /// <summary>Clamp a world position to the XZ area of this BoxCollider (with optional padding).</summary>
    public Vector3 ClampXZ(Vector3 pos, float padding = 0f)
    {
        Bounds b = box.bounds;

        float minX = b.min.x + padding;
        float maxX = b.max.x - padding;
        float minZ = b.min.z + padding;
        float maxZ = b.max.z - padding;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
    }

    /// <summary>
    /// Uniform random point INSIDE the BoxCollider (with optional padding from the walls).
    /// Y is set to the collider's center; feel free to overwrite Y at the call site.
    /// </summary>
    public Vector3 RandomPoint(float padding = 0f)
    {
        Bounds b = box.bounds;

        float x = Random.Range(b.min.x + padding, b.max.x - padding);
        float z = Random.Range(b.min.z + padding, b.max.z - padding);

        // We return a sensible Y (collider center). Caller can replace Y if needed.
        return new Vector3(x, b.center.y, z);
    }

    void OnDrawGizmosSelected()
    {
        var c = Gizmos.color;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        if (TryGetComponent(out BoxCollider bc))
            Gizmos.DrawCube(bc.bounds.center, bc.bounds.size);
        Gizmos.color = c;
    }
}