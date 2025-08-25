using System;
using System.Collections;
using UnityEngine;
using Cinemachine;

public class AttackFX : MonoBehaviour
{
    [Header("Refs")]
    public Transform visual;                 // NEU: optisches Child (nicht der Player-Root)
    public Transform attackPivot;            // dreht den Slash (liegt unter Visual)
    public Transform slashTip;               // hat TrailRenderer
    public ParticleSystem hitVfx;            // optionale Sparks
    public AudioClip whoosh;                 // optional
    public CinemachineImpulseSource impulse; // optional (liegt z.B. auf Player)

    [Header("Swing")]
    public float swingAngle = 140f;
    public float swingTime  = 0.12f;
    public float windup     = 0.04f;
    public float recovery   = 0.08f;
    public float leanAngle  = 10f;           // Visual kippt kurz nach vorn
    public float squashAmt  = 0.07f;         // leichtes Squash/Stretch

    [Header("Timing")]
    [Range(0f,1f)] public float hitMoment = 0.4f;  // 0..1 im Swing

    [Header("Camera Shake")]
    [Range(0f,1f)] public float hitShake = 0.12f;  // Stärke des EINEN Impulses

    public event Action OnHitMoment;         // -> PlayerAttack subscribed
    public bool IsSwinging => swinging;      // NEU: von außen abfragbar

    TrailRenderer trail;
    bool swinging;

    // Visual-Defaults
    Vector3    baseVisualScale;
    Quaternion baseVisualRot;

    void Awake()
    {
        if (!visual)     visual     = transform; // Fallback, besser echtes Child zuweisen
        if (!attackPivot) Debug.LogWarning("[AttackFX] attackPivot not set");
        if (!slashTip)    Debug.LogWarning("[AttackFX] slashTip not set");

        trail = slashTip ? slashTip.GetComponent<TrailRenderer>() : null;
        if (trail) trail.emitting = false;

        baseVisualScale = visual.localScale;
        baseVisualRot   = visual.localRotation;
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

        // --- Windup (NUR Visual verändern) ---
        float t = 0f;
        while (t < windup)
        {
            t += Time.deltaTime;
            float k = windup > 0f ? Mathf.SmoothStep(0, 1, t / Mathf.Max(0.0001f, windup)) : 1f;
            visual.localRotation = Quaternion.Euler(-k * leanAngle, 0f, 0f);
            visual.localScale    = baseVisualScale + Vector3.one * (k * squashAmt);
            yield return null;
        }

        // --- Start FX (kein Impulse hier!) ---
        if (whoosh)  AudioSource.PlayClipAtPoint(whoosh, transform.position, 0.9f);
        if (trail)   { trail.Clear(); trail.emitting = true; }

        // --- Haupt-Swing (dreht NUR attackPivot um Y) ---
        float half = swingAngle * 0.5f * dir;
        Quaternion rotStart = Quaternion.Euler(0f, -half, 0f);
        Quaternion rotEnd   = Quaternion.Euler(0f,  half, 0f);

        bool fired = false;
        float dur = Mathf.Max(0.01f, swingTime);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / dur);
            float eased = 1f - Mathf.Pow(1f - u, 3f); // ease-out
            attackPivot.localRotation = Quaternion.Slerp(rotStart, rotEnd, eased);

            // einziges Hit-Event + Impulse genau hier
            if (!fired && u >= hitMoment)
            {
                fired = true;
                OnHitMoment?.Invoke();
                if (impulse && hitShake > 0f) impulse.GenerateImpulse(hitShake);
            }
            yield return null;
        }

        // --- Ende + Recovery (Visual zurück) ---
        if (trail) trail.emitting = false;

        t = 0f;
        Quaternion rot0 = visual.localRotation;
        Vector3     s0  = visual.localScale;
        while (t < recovery)
        {
            t += Time.deltaTime;
            float k = recovery > 0f ? Mathf.SmoothStep(0, 1, t / Mathf.Max(0.0001f, recovery)) : 1f;
            visual.localRotation = Quaternion.Slerp(rot0, baseVisualRot, k);
            visual.localScale    = Vector3.Lerp(s0,  baseVisualScale, k);
            yield return null;
        }

        attackPivot.localRotation = Quaternion.identity;
        visual.localRotation      = baseVisualRot;
        visual.localScale         = baseVisualScale;

        swinging = false;
    }

    // Treffer-VFX am Impact-Punkt
    public void HitAt(Vector3 pos)
    {
        if (hitVfx) Instantiate(hitVfx, pos, Quaternion.identity);
    }
}