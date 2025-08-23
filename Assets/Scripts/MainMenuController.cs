using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;   // neues Input System
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";   // exakter Szenenname

    [Header("UI (optional)")]
    [SerializeField] private StartScreen startScreen;         // referenziere dein StartScreen-Objekt, wenn du Fade willst

    [Header("Input")]
    [SerializeField] private bool allowSpaceToStart = true;   // optional: Space startet auch
    [SerializeField] private bool allowAnyKeyToStart = false; // optional

    void Awake()
    {
        // Safety: falls wir aus einer pausierten Szene kommen
        Time.timeScale = 1f;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool pressed =
            (kb != null && (
                kb.enterKey.wasPressedThisFrame ||
                kb.numpadEnterKey.wasPressedThisFrame ||
                (allowSpaceToStart && kb.spaceKey.wasPressedThisFrame) ||
                (allowAnyKeyToStart && kb.anyKey.wasPressedThisFrame)))
            ||
            (gp != null && (
                gp.startButton.wasPressedThisFrame ||
                gp.buttonSouth.wasPressedThisFrame)); // A/Cross

        if (pressed)
            StartCoroutine(StartSequence());
#else
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            (allowSpaceToStart && Input.GetKeyDown(KeyCode.Space)) ||
            (allowAnyKeyToStart && Input.anyKeyDown))
        {
            StartCoroutine(StartSequence());
        }
#endif
    }

    private IEnumerator StartSequence()
    {
        // hübsches Fade, falls zugewiesen
        if (startScreen != null)
            yield return startScreen.FadeOut();

        // sicherstellen, dass Gameplay läuft
        Time.timeScale = 1f;

        // Szene laden
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }
}
