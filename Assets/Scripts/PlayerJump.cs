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
    public string airBool = "air";            // bool im Animator
    public string jumpTrigger = "jump";       // optional
    public string landTrigger = "";           // optional (leer lassen, wenn nicht vorhanden)

    [Header("Jump shape")]
    public float height = 1.1f;
    public float duration = 0.5f;

    [Header("Landing FX")]
    public float landSquash = 0.08f;
    public float squashTime = 0.08f;
    public float cooldown = 0.1f;

    [Header("Air control")]
    public bool canMoveInAir = true;          // vom Mover benutzt
    public bool lockAttackWhileAir = false;

    public bool IsAir { get; private set; }

    Vector3 baseLocalPos, baseLocalScale;
    float lastJumpEnd = -999f;

    int airHash, jumpHash, landHash;

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

        SetAir(false); // nicht in Air starten
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
        if (IsAir) return false;
        if (Time.time < lastJumpEnd + cooldown) return false;
        if (!isActiveAndEnabled) return false;

        StartCoroutine(JumpCo());
        return true;
    }

    IEnumerator JumpCo()
    {
        IsAir = true;
        SetAir(true);
        TrySetTrigger(jumpHash);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);   // 0..1
            float x = (u - 0.5f) * 2f;               // -1..1
            float h = (1f - x * x) * height;         // Parabel 0..1..0
            if (visualRoot) visualRoot.localPosition = baseLocalPos + Vector3.up * h;
            yield return null;
        }

        // Landen
        TrySetTrigger(landHash);
        if (visualRoot) visualRoot.localPosition = baseLocalPos;

        // >>> WICHTIG: ab hier sofort zurück in den Base-Layer
        IsAir = false;
        SetAir(false);

        // kleiner Squash (läuft jetzt über Idle/Locomotion weiter)
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