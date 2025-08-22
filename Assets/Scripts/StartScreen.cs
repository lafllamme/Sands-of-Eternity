using UnityEngine;
using UnityEngine.InputSystem;   // new Input System
using TMPro;

public class StartScreen : MonoBehaviour
{
    [Header("UI")]
    public GameObject overlay;          // Fullscreen Image (enabled at start)
    public CanvasGroup overlayGroup;    // OPTIONAL: CanvasGroup on the overlay (for fade)
    public CanvasGroup pressEnterGroup; // CanvasGroup on the TMP text ("Press Enter")

    [Header("Pulse")]
    public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [Header("Fade")]
    public float fadeDuration = 0.4f;   // seconds (unscaled)

    bool gameStarted;

    void Start()
    {
        if (overlay) overlay.SetActive(true);
        if (overlayGroup) overlayGroup.alpha = 1f;
        if (pressEnterGroup) pressEnterGroup.alpha = 1f;

        // pause gameplay while the start screen is visible
        Time.timeScale = 0f;
    }

    void Update()
    {
        // Pulse the "Press Enter" text while paused (use unscaled time)
        if (!gameStarted && pressEnterGroup)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f) + 0.5f; // 0..1
            pressEnterGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        // Start on Enter
        if (!gameStarted &&
            Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            StartGame();
        }
    }

    void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        StartCoroutine(FadeOutAndResume());
    }

    System.Collections.IEnumerator FadeOutAndResume()
    {
        float t = 0f;

        // If we can fade, do it; otherwise hide instantly.
        if (overlayGroup || pressEnterGroup)
        {
            // fade using unscaled time (because timeScale = 0)
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / fadeDuration);

                if (overlayGroup) overlayGroup.alpha = a;
                if (pressEnterGroup) pressEnterGroup.alpha = a;

                yield return null;
            }
        }

        if (overlay) overlay.SetActive(false);
        if (pressEnterGroup) pressEnterGroup.gameObject.SetActive(false);

        // resume gameplay
        Time.timeScale = 1f;
    }
}
