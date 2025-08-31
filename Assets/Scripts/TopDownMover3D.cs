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
    [Range(0f, 1.0f)] public float backpedalSpeedMul = 0.85f;
    public float backThreshold = -0.15f;

    [Header("During Attack")]
    public bool lockTurnWhileSwinging = true;
    [Range(0f, 1f)] public float swingMoveMul = 0.8f;

    [Header("Bounds (optional outer clamp)")]
    public bool useMapBounds = true;
    public float clampPadding = 0.5f;    // Puffer zum Rand
    public Transform ground;             // nur Fallback, wenn kein MapBounds vorhanden
    public float margin = 0.5f;

    [Header("Animation Smoothing")]
    [Range(0f, 2f)] public float mobility = 1.0f;
    [Range(0.1f, 3f)] public float walkToRunSeconds = 1.2f;
    [Range(0.05f, 1.5f)] public float stopSeconds = 0.25f;
    float animSpeedParam = 0f;

    [Header("Air Tuning")]
    [Range(0f,1f)] public float airControlMul = 0.6f; // 0..1 horizontale Steuerung in der Luft
    public bool  lockTurnWhileAir = false;

    // --- intern ---
    Animator anim;
    static readonly int IDSpeed = Animator.StringToHash("speed");

    AttackFX fx;
    PlayerJump jump;                 // dein Jump-Script
    CharacterController controller;  // kinematische Kollision

    const float StickToGround = 5f;  // leichter Down-Pull, damit CC grounded bleibt

    float yLockFallback;

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        fx         = GetComponent<AttackFX>();
        anim       = GetComponentInChildren<Animator>();
        jump       = GetComponent<PlayerJump>();
        controller = GetComponent<CharacterController>();

        if (!anim) Debug.LogWarning("No Animator found under Player -> PlayerVisual -> LowPoly");
    }

    void Start()
    {
        yLockFallback = transform.position.y;
    }

    void Update()
    {
        // --- Input & kamerarelative Richtung ---
        Vector2 m = GetMove();
        Vector3 camFwd = Vector3.forward, camRight = Vector3.right;

        if (cameraRelative && cameraTransform)
        {
            camFwd   = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            camRight = Vector3.ProjectOnPlane(cameraTransform.right,  Vector3.up).normalized;
        }

        Vector3 dir = camRight * m.x + camFwd * m.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float forwardDot = cameraRelative ? Vector3.Dot(dir, camFwd) : dir.z;
        bool movingBack  = blockTurnWhenBack && forwardDot < backThreshold;

        bool swinging = fx && fx.IsSwinging;
        bool isAir    = jump && jump.IsAir;

        // Bewegungsskalierung
        float moveMul = 1f;
        if (movingBack) moveMul *= backpedalSpeedMul;
        if (swinging)   moveMul *= swingMoveMul;
        if (isAir)      moveMul *= (jump != null && jump.canMoveInAir ? Mathf.Clamp01(airControlMul) : 0f);

        float moveSpeed = speed * moveMul;

        // --- Move (CharacterController bevorzugt) ---
        if (controller)
        {
            // CC bewegt sich in Weltkoords; kleiner Down-Pull hält ihn am Boden
            Vector3 velocity = dir * moveSpeed + Vector3.down * StickToGround;
            controller.Move(velocity * Time.deltaTime);

            // Rechteckiger Außenrahmen (nur X/Z clampen)
            if (useMapBounds)
            {
                Vector3 p = transform.position;
                if (MapBounds.I != null) p = MapBounds.I.ClampXZ(p, clampPadding);
                else if (ground)
                {
                    float halfX = 5f * ground.localScale.x, halfZ = 5f * ground.localScale.z;
                    p.x = Mathf.Clamp(p.x, ground.position.x - halfX + margin, ground.position.x + halfX - margin);
                    p.z = Mathf.Clamp(p.z, ground.position.z - halfZ + margin, ground.position.z + halfZ - margin);
                }
                // Y dem Controller überlassen
                p.y = transform.position.y;
                transform.position = p;
            }
        }
        else
        {
            // Fallback ohne Physik (wie vorher)
            Vector3 p = transform.position + dir * (moveSpeed) * Time.deltaTime;
            p.y = yLockFallback;
            if (useMapBounds && MapBounds.I != null) p = MapBounds.I.ClampXZ(p, clampPadding);
            else if (ground)
            {
                float halfX = 5f * ground.localScale.x, halfZ = 5f * ground.localScale.z;
                p.x = Mathf.Clamp(p.x, ground.position.x - halfX + margin, ground.position.x + halfX - margin);
                p.z = Mathf.Clamp(p.z, ground.position.z - halfZ + margin, ground.position.z + halfZ - margin);
            }
            transform.position = p;
        }

        // --- Drehen ---
        if (!movingBack && !(lockTurnWhileSwinging && swinging) && !(lockTurnWhileAir && isAir) && dir.sqrMagnitude > 0.0001f)
        {
            var target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothTurn * Time.deltaTime);
        }

        // --- Animator-Speed geglättet setzen ---
        if (anim)
        {
            float target = Mathf.Clamp01(m.magnitude);
            if (movingBack) target *= 0.6f;

            float upRate   = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, walkToRunSeconds);
            float downRate = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, stopSeconds);
            float rate = (target > animSpeedParam) ? upRate : downRate;

            animSpeedParam = Mathf.MoveTowards(animSpeedParam, target, rate * Time.deltaTime);
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