using UnityEngine;
using System.Collections;

public class StartScreen : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup overlayGroup;    // CanvasGroup auf deinem Fullscreen-Image (optional)
    public CanvasGroup pressEnterGroup; // CanvasGroup auf dem "Press Enter" Text

    [Header("Pulse")]
    public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Fade")]
    public float fadeDuration = 0.4f;   // Sekunden (unscaled)

    void Start()
    {
        if (overlayGroup) overlayGroup.alpha = 1f;
        if (pressEnterGroup) pressEnterGroup.alpha = 1f;
        // KEIN Input, KEIN Time.timeScale hier
    }

    void Update()
    {
        if (!pressEnterGroup) return;

        // Pulsierender Text mit unscaled time (unabhängig von Time.timeScale)
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f) + 0.5f;
        pressEnterGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
    }

    public IEnumerator FadeOut()
    {
        // optionales Fade-Out für Overlay und Text
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeDuration);
            if (overlayGroup)    overlayGroup.alpha    = a;
            if (pressEnterGroup) pressEnterGroup.alpha = a;
            yield return null;
        }
    }
}
