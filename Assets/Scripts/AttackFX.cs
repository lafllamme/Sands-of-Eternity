using System;
using System.Collections;
using UnityEngine;
using Cinemachine;

public class AttackFX : MonoBehaviour
{
    [Header("Refs")]
    public Transform attackPivot;           // Child, wird gedreht
    public Transform slashTip;              // hat TrailRenderer
    public ParticleSystem hitVfx;           // optional kleine Sparks
    public AudioClip whoosh;                // optional
    public CinemachineImpulseSource impulse;

    [Header("Swing")]
    public float swingAngle = 140f;
    public float swingTime  = 0.12f;
    public float windup     = 0.04f;
    public float recovery   = 0.08f;
    public float leanAngle  = 10f;
    public float squashAmt  = 0.07f;

    [Header("Timing")]
    [Range(0f,1f)] public float hitMoment = 0.4f; // wann (0..1) das OnHitMoment-Event feuern soll

    public event Action OnHitMoment;        // -> PlayerAttack subscribed

    TrailRenderer trail;
    Vector3 baseScale;
    bool swinging;

    void Awake()
    {
        if (!attackPivot) Debug.LogWarning("[AttackFX] attackPivot not set");
        if (!slashTip)    Debug.LogWarning("[AttackFX] slashTip not set");

        trail = slashTip ? slashTip.GetComponent<TrailRenderer>() : null;
        if (trail) trail.emitting = false;

        baseScale = transform.localScale;
    }

    public void PlaySwing(int dir = 1)
    {
        if (!isActiveAndEnabled || swinging) return;
        StartCoroutine(SwingCo(Mathf.Sign(dir) >= 0 ? 1 : -1));
    }

    IEnumerator SwingCo(int dir)
    {
        swinging = true;
        if (!attackPivot) attackPivot = transform;

        // Windup
        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            float k = windup > 0f ? Mathf.SmoothStep(0, 1, t / windup) : 1f;
            transform.localRotation = Quaternion.Euler(-k * leanAngle, 0, 0);
            transform.localScale    = baseScale + Vector3.one * (k * squashAmt);
            yield return null;
        }

        // Start FX
        if (whoosh)  AudioSource.PlayClipAtPoint(whoosh, transform.position, 0.9f);
        if (impulse) impulse.GenerateImpulse(0.15f);
        if (trail)   { trail.Clear(); trail.emitting = true; }

        // Haupt-Swing
        float half = swingAngle * 0.5f * dir;
        Quaternion rotStart = Quaternion.Euler(0, -half, 0);
        Quaternion rotEnd   = Quaternion.Euler(0,  half, 0);

        bool fired = false;
        float dur = Mathf.Max(0.01f, swingTime);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / dur);
            float eased = 1f - Mathf.Pow(1f - u, 3f); // ease-out
            attackPivot.localRotation = Quaternion.Slerp(rotStart, rotEnd, eased);

            if (!fired && u >= hitMoment)
            {
                OnHitMoment?.Invoke();
                fired = true;
                if (impulse) impulse.GenerateImpulse(0.3f);
            }
            yield return null;
        }

        // Ende + Recovery
        if (trail) trail.emitting = false;

        t = 0f;
        Quaternion rot0 = transform.localRotation;
        Vector3     s0  = transform.localScale;
        while (t < recovery)
        {
            t += Time.deltaTime;
            float k = recovery > 0f ? Mathf.SmoothStep(0, 1, t / recovery) : 1f;
            transform.localRotation = Quaternion.Slerp(rot0, Quaternion.identity, k);
            transform.localScale    = Vector3.Lerp(s0, baseScale, k);
            yield return null;
        }

        attackPivot.localRotation = Quaternion.identity;
        transform.localRotation   = Quaternion.identity;
        transform.localScale      = baseScale;
        swinging = false;
    }

    // Optionale Hilfe für Treffer-VFX
    public void HitAt(Vector3 pos)
    {
        if (hitVfx) Instantiate(hitVfx, pos, Quaternion.identity);
    }
}