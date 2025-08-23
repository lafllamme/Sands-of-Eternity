using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Kind-Image (Type = Filled/Horizontal)")]
    public Image fill;
    public Gradient colorByPct;          // optional (grün→gelb→rot)
    public Camera targetCamera;          // leer = Camera.main

    [Header("Visibility")]
    public bool startHidden = true;
    [Tooltip("Wie lange nach Schaden sichtbar bleiben. <0 = immer sichtbar")]
    public float visibleSeconds = 1.2f;
    public float fadeDuration = 0.15f;
    public bool billboard = true;        // zur Kamera drehen (nur in XZ)

    // --- intern ---
    private Health health;
    private CanvasGroup cg;
    private float hideAt = -1f;
    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (!targetCamera) targetCamera = Camera.main;
        if (canvas.renderMode == RenderMode.WorldSpace && !canvas.worldCamera)
            canvas.worldCamera = targetCamera;

        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        if (startHidden) cg.alpha = 0f;

        health = GetComponentInParent<Health>();
        if (health != null)
        {
            health.OnHealthChanged += OnHpChanged;
            OnHpChanged(health.CurrentHP, health.maxHP); // initial push
        }
        else
        {
            Debug.LogWarning($"[EnemyHealthBar] No Health found on parent of {name}", this);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        if (health != null) health.OnHealthChanged -= OnHpChanged;
    }

    void LateUpdate()
    {
        // Billboard (nur horizontal)
        if (billboard && targetCamera)
        {
            var fwd = targetCamera.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f) transform.forward = fwd.normalized;
        }

        // Auto-hide (nur wenn Timer aktiv und nicht „immer sichtbar“)
        if (visibleSeconds >= 0f && hideAt > 0f && Time.time >= hideAt && cg.alpha > 0f)
        {
            float t = Mathf.InverseLerp(hideAt, hideAt + fadeDuration, Time.time);
            cg.alpha = Mathf.Lerp(1f, 0f, t);
        }
    }

    void OnHpChanged(int cur, int max)
    {
        if (!fill || max <= 0) return;

        float pct = Mathf.Clamp01((float)cur / max);
        fill.fillAmount = pct;

        if (colorByPct != null && colorByPct.colorKeys.Length > 0)
            fill.color = colorByPct.Evaluate(pct);

        // einblenden
        StopAllCoroutines();
        StartCoroutine(FadeTo(1f, fadeDuration));

        // ausblenden planen (sofern gewünscht)
        if (visibleSeconds >= 0f)
            hideAt = Time.time + visibleSeconds;

        // schnell aus wenn tot
        if (cur <= 0) hideAt = Time.time + 0.05f;
    }

    System.Collections.IEnumerator FadeTo(float target, float dur)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        cg.alpha = target;
    }
}