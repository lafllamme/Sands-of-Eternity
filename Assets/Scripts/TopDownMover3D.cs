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
    public Transform cameraTransform;

    [Header("Backpedal (no turning when going backwards)")]
    public bool blockTurnWhenBack = true;
    [Range(0f,1.0f)] public float backpedalSpeedMul = 0.85f;
    public float backThreshold = -0.15f;

    [Header("During Attack")]
    public bool lockTurnWhileSwinging = true;
    [Range(0f,1f)] public float swingMoveMul = 0.8f;

    [Header("Bounds")]
    public bool useMapBounds = true;
    public float clampPadding = 0.5f;
    public Transform ground;
    public float margin = 0.5f;

    // -------- Animation Smoothing --------
    [Header("Animation Smoothing")]
    [Range(0f, 2f)] public float mobility = 1.0f;     // 0 = träge, 1 = normal, 2 = sehr agil
    [Range(0.1f, 3f)] public float walkToRunSeconds = 1.2f; // 0->1 (bei mobility=1)
    [Range(0.05f, 1.5f)] public float stopSeconds     = 0.25f; // 1->0 (bei mobility=1)
    float animSpeedParam = 0f; // geglätteter 0..1-Wert

    Animator anim;
    static readonly int IDSpeed = Animator.StringToHash("speed");

    float yLock;
    AttackFX fx;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        fx = GetComponent<AttackFX>();
        anim = GetComponentInChildren<Animator>();
        if (!anim) Debug.LogWarning("No Animator found under Player -> PlayerVisual -> LowPoly");
    }

    void Start() => yLock = transform.position.y;

    void Update()
    {
        // --- input & camera-relative dir ---
        Vector2 m = GetMove();
        Vector3 camFwd = Vector3.forward, camRight = Vector3.right;
        Vector3 dir;

        if (cameraRelative && cameraTransform)
        {
            camFwd   = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camRight = Vector3.ProjectOnPlane(cameraTransform.right,  Vector3.up).normalized;
            dir = camRight * m.x + camFwd * m.y;
        }
        else dir = new Vector3(m.x, 0f, m.y);

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float forwardDot = cameraRelative ? Vector3.Dot(dir, camFwd) : dir.z;
        bool movingBack  = blockTurnWhenBack && forwardDot < backThreshold;

        bool swinging = fx && fx.IsSwinging;
        float moveSpeed = speed * (movingBack ? backpedalSpeedMul : 1f) * (swinging ? swingMoveMul : 1f);

        // --- move ---
        Vector3 p = transform.position + dir * moveSpeed * Time.deltaTime;
        p.y = yLock;
        if (useMapBounds && MapBounds.I != null) p = MapBounds.I.ClampXZ(p, clampPadding);
        else if (ground)
        {
            float halfX = 5f * ground.localScale.x, halfZ = 5f * ground.localScale.z;
            p.x = Mathf.Clamp(p.x, ground.position.x - halfX + margin, ground.position.x + halfX - margin);
            p.z = Mathf.Clamp(p.z, ground.position.z - halfZ + margin, ground.position.z + halfZ - margin);
        }
        transform.position = p;

        // --- rotate ---
        if (!movingBack && !(lockTurnWhileSwinging && swinging) && dir.sqrMagnitude > 0.0001f)
        {
            var target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothTurn * Time.deltaTime);
        }

        // --- ANIMATOR: geglätteten speed 0..1 setzen ---
        if (anim)
        {
            float target = Mathf.Clamp01(m.magnitude); // 0..1 aus WASD
            if (movingBack) target *= 0.6f;

            // Mobility skaliert die „Beschleunigung“ (Rate in Einheiten pro Sekunde)
            float upRate   = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, walkToRunSeconds);
            float downRate = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, stopSeconds);

            float rate = (target > animSpeedParam) ? upRate : downRate;
            animSpeedParam = Mathf.MoveTowards(animSpeedParam, target, rate * Time.deltaTime);

            // Kein extra Damp-Time im Animator verwenden – wir glätten selbst:
            anim.SetFloat(IDSpeed, animSpeedParam);
        }
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