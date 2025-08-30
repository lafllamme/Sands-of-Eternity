using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerJump : MonoBehaviour
{
    [Header("Visual root (wird angehoben)")]
    public Transform visualRoot;              // z.B. PlayerVisual
    public Animator animator;                 // LowPoly-Animator (optional auto-find)

    [Header("Animator parameters")]
    public string airBool = "air";            // Bool im Animator
    public string jumpTrigger = "jump";       // Trigger für Jump/DoubleJump
    public string landTrigger = "";           // optional

    [Header("Jump shape (1. Sprung)")]
    public float height = 1.1f;
    public float duration = 0.5f;

    [Header("Extra Jumps")]
    public int   maxJumps = 2;                // 1 = Single, 2 = Double Jump
    public float secondJumpHeightMul   = 1.25f;
    public float secondJumpDurationMul = 0.90f;

    [Header("Landing FX")]
    public float landSquash = 0.08f;
    public float squashTime = 0.08f;
    public float cooldown = 0.1f;

    [Header("Air control")]
    public bool canMoveInAir = true;
    public bool lockAttackWhileAir = false;

    public bool IsAir { get; private set; }

    Vector3 baseLocalPos, baseLocalScale;
    float lastJumpEnd = -999f;

    int airHash, jumpHash, landHash;

    // Runtime
    int jumpsLeft = 0;
    bool requestExtraJump = false;

    void Awake()
    {
        if (!visualRoot)
        {
            var t = transform.Find("PlayerVisual");
            if (t) visualRoot = t;
        }
        if (!animator) animator = GetComponentInChildren<Animator>();

        baseLocalPos   = visualRoot ? visualRoot.localPosition : Vector3.zero;
        baseLocalScale = visualRoot ? visualRoot.localScale    : Vector3.one;

        airHash  = !string.IsNullOrEmpty(airBool)     ? Animator.StringToHash(airBool)     : 0;
        jumpHash = !string.IsNullOrEmpty(jumpTrigger) ? Animator.StringToHash(jumpTrigger) : 0;
        landHash = !string.IsNullOrEmpty(landTrigger) ? Animator.StringToHash(landTrigger) : 0;

        SetAir(false);
    }

    void OnDisable() => SetAir(false);

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        bool pressed = kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        bool pressed = Input.GetKeyDown(KeyCode.Space);
#endif
        if (pressed) TryJump();
    }

    public bool TryJump()
    {
        // Start aus Boden
        if (!IsAir)
        {
            if (Time.time < lastJumpEnd + cooldown) return false;
            jumpsLeft = Mathf.Max(0, maxJumps - 1); // den ersten Sprung verbrauchen wir jetzt
            requestExtraJump = false;
            StartCoroutine(JumpCo());
            return true;
        }

        // Extra-Sprung in der Luft
        if (IsAir && jumpsLeft > 0)
        {
            jumpsLeft--;
            requestExtraJump = true; // Coroutine picked das auf und startet neuen Bogen
            return true;
        }

        return false;
    }

    IEnumerator JumpCo()
    {
        IsAir = true;
        SetAir(true);

        float startH = CurrentOffsetY();
        int arcIndex = 0; // 0=erster, 1=zweiter, ...

        // Eine oder mehrere Bögen, je nach Extra-Jumps
        for (;;)
        {
            float H = (arcIndex == 0) ? height   : height   * secondJumpHeightMul;
            float D = (arcIndex == 0) ? duration : duration * secondJumpDurationMul;

            // Trigger (gleich für Double Jump – falls du einen extra Double-Anim hast, kannst du hier umschalten)
            TrySetTrigger(jumpHash);

            float t = 0f;
            bool restartArc = false;

            while (t < D)
            {
                t += Time.deltaTime;

                // Double/Triple Jump angefordert? -> neuen Bogen nahtlos ab aktueller Höhe starten
                if (requestExtraJump)
                {
                    requestExtraJump = false;
                    startH  = CurrentOffsetY(); // rebase
                    arcIndex++;
                    restartArc = true;
                    break; // inner loop verlassen, nächster Bogen
                }

                float u = Mathf.Clamp01(t / D);
                // Start bei aktueller Höhe, hump on top, Ende wieder bei 0
                float h = startH * (1f - u) + Parabola(u) * H;
                SetOffsetY(h);
                yield return null;
            }

            if (restartArc) continue; // nächster Bogen (Double/Triple)
            break;                    // fertig: wir sind gelandet (h≈0)
        }

        // Landen
        TrySetTrigger(landHash);
        SetOffsetY(0f);

        // sofort zurück in den Base-Layer
        IsAir = false;
        SetAir(false);

        // Squash läuft während Idle/Locomotion weiter
        if (landSquash > 0f && visualRoot)
        {
            Vector3 s0 = baseLocalScale;
            Vector3 sA = new Vector3(s0.x + landSquash, s0.y - landSquash * 1.8f, s0.z + landSquash);
            float st = 0f;
            while (st < squashTime)
            {
                st += Time.deltaTime;
                float k = Mathf.Clamp01(st / squashTime);
                visualRoot.localScale = Vector3.Lerp(sA, s0, k);
                yield return null;
            }
            visualRoot.localScale = s0;
        }

        lastJumpEnd = Time.time;
        jumpsLeft = 0;
    }

    // ------- helpers -------
    float CurrentOffsetY() => visualRoot ? (visualRoot.localPosition.y - baseLocalPos.y) : 0f;

    void SetOffsetY(float y)
    {
        if (!visualRoot) return;
        var p = baseLocalPos; p.y += y;
        visualRoot.localPosition = p;
    }

    static float Parabola(float u)
    {
        float x = (u - 0.5f) * 2f; // -1..1
        return 1f - x * x;         // 0..1..0
    }

    void SetAir(bool v)
    {
        if (animator && airHash != 0 && HasParam(airHash))
            animator.SetBool(airHash, v);
    }

    void TrySetTrigger(int hash)
    {
        if (animator && hash != 0 && HasParam(hash))
            animator.SetTrigger(hash);
    }

    bool HasParam(int hash)
    {
        if (!animator) return false;
        foreach (var p in animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }
}