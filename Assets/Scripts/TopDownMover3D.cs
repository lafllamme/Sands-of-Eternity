using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TopDownMover3D : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float smoothTurn = 15f;

    [Header("Camera-relative")]
    public bool cameraRelative = true;
    public Transform cameraTransform; // assign Main Camera; auto-finds if null

    [Header("Backpedal (no turning when going backwards)")]
    public bool blockTurnWhenBack = true;     // <— wichtig
    [Range(0f,1.0f)] public float backpedalSpeedMul = 0.85f; // optional langsamer rückwärts
    [Tooltip("Wie stark 'rückwärts' sein muss, bis wir das Drehen blocken (-1..0).")]
    public float backThreshold = -0.15f;      // Dot < -0.15 => rückwärts

    [Header("Bounds")]
    public bool useMapBounds = true;
    public float clampPadding = 0.5f;
    [Tooltip("Legacy plane clamp (used if MapBounds not present or useMapBounds = false).")]
    public Transform ground;
    public float margin = 0.5f;

    float yLock;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    void Start() => yLock = transform.position.y;

    void Update()
    {
        // --- input ---
        Vector2 m = GetMove();
        Vector3 dir;
        Vector3 camFwd = Vector3.forward; // fallback
        Vector3 camRight = Vector3.right; // fallback

        if (cameraRelative && cameraTransform)
        {
            // project camera basis to XZ so Y doesn't tilt movement
            camFwd   = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camRight = Vector3.ProjectOnPlane(cameraTransform.right,  Vector3.up).normalized;
            dir = camRight * m.x + camFwd * m.y;
        }
        else
        {
            dir = new Vector3(m.x, 0f, m.y); // world axes
        }

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // Vorwärts-/Rückwärts-Anteil relativ zur Kamera (oder Welt)
        float forwardDot =
            cameraRelative ? Vector3.Dot(dir, camFwd)
                           : dir.z; // Welt-z ist "vorwärts"

        bool movingBack = blockTurnWhenBack && forwardDot < backThreshold;

        // --- move ---
        float moveSpeed = speed * (movingBack ? backpedalSpeedMul : 1f);
        Vector3 p = transform.position + dir * moveSpeed * Time.deltaTime;
        p.y = yLock;

        // --- clamp ---
        if (useMapBounds && MapBounds.I != null)
        {
            p = MapBounds.I.ClampXZ(p, clampPadding);
        }
        else if (ground)
        {
            float halfX = 5f * ground.localScale.x; // Unity plane = 10x10
            float halfZ = 5f * ground.localScale.z;
            p.x = Mathf.Clamp(p.x, ground.position.x - halfX + margin, ground.position.x + halfX - margin);
            p.z = Mathf.Clamp(p.z, ground.position.z - halfZ + margin, ground.position.z + halfZ - margin);
        }
        transform.position = p;

        // --- face move direction ---
        if (!movingBack && dir.sqrMagnitude > 0.0001f)
        {
            var target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothTurn * Time.deltaTime);
        }
        // else: beim Rückwärtslaufen NICHT drehen (Backpedal)
    }

#if ENABLE_INPUT_SYSTEM
    Vector2 GetMove()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.aKey.isPressed || k.leftArrowKey.isPressed ? -1f : 0f)
                + (k.dKey.isPressed || k.rightArrowKey.isPressed ?  1f : 0f);
        float y = (k.sKey.isPressed || k.downArrowKey.isPressed ? -1f : 0f)
                + (k.wKey.isPressed || k.upArrowKey.isPressed   ?  1f : 0f);
        return new Vector2(x, y);
    }
#else
    Vector2 GetMove() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
}