using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerJump : MonoBehaviour
{
    [Header("Visual root (wird angehoben)")]
    public Transform visualRoot;              
    public Animator animator;                 

    [Header("Animator parameters")]
    public string airBool = "air";            
    public string jumpTrigger = "jump";       
    public string landTrigger = "land";       // <— Default hilft beim Neu-Anlegen

    [Header("Jump shape (1. Sprung)")]
    public float height = 1.1f;
    public float duration = 0.5f;

    [Header("Extra Jumps")]
    public int   maxJumps = 2;                
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
    public float CurrentHeight { get; private set; }

    Vector3 baseLocalPos, baseLocalScale;
    float lastJumpEnd = -999f;

    int airHash, jumpHash, landHash;

    // Runtime
    int  jumpsLeft = 0;
    bool requestExtraJump = false;

    CharacterController cc; // <— für echtes Grounded

    void Awake()
    {
        if (!visualRoot)
        {
            var t = transform.Find("PlayerVisual");
            if (t) visualRoot = t;
        }
        if (!animator) animator = GetComponentInChildren<Animator>();
        cc = GetComponent<CharacterController>();

        baseLocalPos   = visualRoot ? visualRoot.localPosition : Vector3.zero;
        baseLocalScale = visualRoot ? visualRoot.localScale    : Vector3.one;

        airHash  = !string.IsNullOrEmpty(airBool)     ? Animator.StringToHash(airBool)     : 0;
        jumpHash = !string.IsNullOrEmpty(jumpTrigger) ? Animator.StringToHash(jumpTrigger) : 0;
        landHash = !string.IsNullOrEmpty(landTrigger) ? Animator.StringToHash(landTrigger) : 0;

        SetAir(false);
        CurrentHeight = 0f;
    }

    void OnDisable()
    {
        SetAir(false);
        CurrentHeight = 0f;
    }

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
        if (!IsAir)
        {
            if (Time.time < lastJumpEnd + cooldown) return false;
            jumpsLeft = Mathf.Max(0, maxJumps - 1);
            requestExtraJump = false;
            StartCoroutine(JumpCo());
            return true;
        }

        if (IsAir && jumpsLeft > 0)
        {
            jumpsLeft--;
            requestExtraJump = true; // nahtloser Double/Triple
            return true;
        }

        return false;
    }

    IEnumerator JumpCo()
    {
        IsAir = true;
        SetAir(true);

        float startH = CurrentHeight;
        int arcIndex = 0;

        for (;;)
        {
            float H = (arcIndex == 0) ? height   : height   * secondJumpHeightMul;
            float D = (arcIndex == 0) ? duration : duration * secondJumpDurationMul;

            TrySetTrigger(jumpHash);

            float t = 0f;
            bool restartArc = false;

            while (t < D)
            {
                t += Time.deltaTime;

                if (requestExtraJump)
                {
                    requestExtraJump = false;
                    startH  = CurrentHeight; // rebase
                    arcIndex++;
                    restartArc = true;
                    break;
                }

                float u = Mathf.Clamp01(t / D);
                float h = startH * (1f - u) + Parabola(u) * H; // 0..H..0
                SetOffsetY(h);
                yield return null;
            }

            if (restartArc) continue;
            break;
        }

        // --- Freefall-Phase: NICHT sofort landen ---
        IsAir = false; // Mover übernimmt die Schwerkraft & "air" Verwaltung
        // Fürs Animieren können wir 'air' schon oben lassen:
        if (animator && airHash != 0 && HasParam(airHash))
            animator.SetBool(airHash, true);

        // Auf echten Bodenkontakt warten (mit CC)
        if (cc)
        {
            yield return null; // eine Frame-Gnade
            while (!cc.isGrounded) yield return null;
        }

        // Jetzt WIRKLICH gelandet -> Land-Trigger feuern
        TrySetTrigger(landHash);
        SetOffsetY(0f);

        if (animator && airHash != 0 && HasParam(airHash))
            animator.SetBool(airHash, false);

        // Squash während Idle/Locomotion
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
        jumpsLeft   = 0;
    }

    // ------- helpers -------
    void SetOffsetY(float y)
    {
        CurrentHeight = y;
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
        if (hash == 0) return;
        if (animator && HasParam(hash))
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