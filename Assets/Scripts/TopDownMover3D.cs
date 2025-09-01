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
    public float clampPadding = 0.5f;
    public Transform ground;
    public float margin = 0.5f;

    [Header("Animation Smoothing")]
    [Range(0f, 2f)] public float mobility = 1.0f;
    [Range(0.1f, 3f)] public float walkToRunSeconds = 1.2f;
    [Range(0.05f, 1.5f)] public float stopSeconds = 0.25f;
    float animSpeedParam = 0f;

    [Header("Air Tuning")]
    [Range(0f,1f)] public float airControlMul = 0.6f;
    public bool  lockTurnWhileAir = false;

    [Header("Gravity (natürliches Fallen)")]
    public float gravity = 25f;       // m/s²
    public float maxFallSpeed = 40f;  // Kappung
    public float groundStick = 5f;    // nur wenn grounded

    [Header("Fall-Erkennung")]
    [Tooltip("So lange ungrounded, bevor wir in Falling wechseln.")]
    public float fallEnterDelay = 0.08f;
    [Tooltip("Einmaliger Snap nach unten beim Start, um den CC sicher zu erden.")]
    public float spawnSnapDown = 0.2f;
    public bool  snapOnStart = true;

    // intern
    Animator anim;
    static readonly int IDSpeed = Animator.StringToHash("speed");

    AttackFX fx;
    PlayerJump jump;
    CharacterController controller;

    float lastJumpH = 0f;   // Delta-Quelle aus PlayerJump.CurrentHeight
    float fallSpeed = 0f;
    float notGroundedTime = 0f;
    float yLockFallback;
    int   airHash = 0;      // optionaler Animator-Bool („air“)

    void Awake()
    {
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        fx         = GetComponent<AttackFX>();
        anim       = GetComponentInChildren<Animator>();
        jump       = GetComponent<PlayerJump>();
        controller = GetComponent<CharacterController>();

        if (jump != null && anim != null && !string.IsNullOrEmpty(jump.airBool))
            airHash = Animator.StringToHash(jump.airBool);

        if (!anim) Debug.LogWarning("No Animator found under Player -> PlayerVisual -> LowPoly");
    }

    void Start()
    {
        yLockFallback = transform.position.y;
        lastJumpH     = jump ? jump.CurrentHeight : 0f;

        if (controller)
        {
            controller.minMoveDistance = 0f; // kleine Deltas nicht verlieren
            if (snapOnStart && spawnSnapDown > 0f)
                controller.Move(Vector3.down * spawnSnapDown);
        }
    }

    void Update()
    {
        // --- Input & Kamera ---
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
        bool grounded = controller ? controller.isGrounded : true;

        // Timer für „wirklich in der Luft“
        notGroundedTime = grounded ? 0f : notGroundedTime + Time.deltaTime;

        float moveMul = 1f;
        if (movingBack) moveMul *= backpedalSpeedMul;
        if (swinging)   moveMul *= swingMoveMul;
        if (isAir)      moveMul *= (jump != null && jump.canMoveInAir ? Mathf.Clamp01(airControlMul) : 0f);

        float moveSpeed = speed * moveMul;

        // --- Bewegung (Controller bevorzugt) ---
        if (controller)
        {
            // 1) Horizontal
            controller.Move(dir * (moveSpeed * Time.deltaTime));

            // 2) Vertikal aus Jump (Delta der Sprunghöhe)
            float curH = jump ? jump.CurrentHeight : 0f;
            float dH   = curH - lastJumpH;
            if (dH != 0f) controller.Move(Vector3.up * dH);
            lastJumpH = curH;

            // 3) Natürliches Fallen (nur wenn NICHT im Jump und nach Gnadenzeit)
            bool freefall = !isAir && !grounded && notGroundedTime > fallEnterDelay;
            if (freefall)
            {
                fallSpeed = Mathf.Min(fallSpeed + gravity * Time.deltaTime, maxFallSpeed);
                controller.Move(Vector3.down * fallSpeed * Time.deltaTime);

                if (airHash != 0 && anim) anim.SetBool(airHash, true); // Animator in „Air“
            }
            else
            {
                // am Boden oder im Jump: Fallspeed resetten
                fallSpeed = 0f;

                // leichter Boden-Pull nur wenn grounded & nicht im Jump
                if (grounded && !isAir)
                    controller.Move(Vector3.down * (groundStick * Time.deltaTime));

                // Animator-Flag runternehmen, wenn nicht im Jump
                if (airHash != 0 && anim && grounded && !isAir)
                    anim.SetBool(airHash, false);
            }

            // 4) Außen-Clamp X/Z
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
                p.y = transform.position.y; // Y kommt vom CC
                transform.position = p;
            }
        }
        else
        {
            // Fallback ohne CC
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

        // --- Animator-Speed (geglättet) ---
        if (anim)
        {
            float target = Mathf.Clamp01(m.magnitude);
            if (movingBack) target *= 0.6f;

            float upRate   = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, walkToRunSeconds);
            float downRate = (mobility <= 0f ? 0.0001f : mobility) / Mathf.Max(0.0001f, stopSeconds);
            float rate     = (target > animSpeedParam) ? upRate : downRate;

            animSpeedParam = Mathf.MoveTowards(animSpeedParam, target, rate * Time.deltaTime);
            anim.SetFloat(IDSpeed, animSpeedParam);
        }
    }

    private Vector2 GetMove()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            float x = (k.aKey.isPressed || k.leftArrowKey.isPressed ? -1f : 0f)
                    + (k.dKey.isPressed || k.rightArrowKey.isPressed ?  1f : 0f);
            float y = (k.sKey.isPressed || k.downArrowKey.isPressed ? -1f : 0f)
                    + (k.wKey.isPressed || k.upArrowKey.isPressed   ?  1f : 0f);
            return new Vector2(x, y);
        }
#endif
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}