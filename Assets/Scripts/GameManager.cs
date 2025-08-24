using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    // ---------- Coins ----------
    [Header("Run Stats")]
    public int coins = 0;
    public Action<int> onCoinsChanged;

    // ---------- Lives / Respawn ----------
    [Header("Lives & Respawn")]
    public int startLives = 3;
    public float respawnDelay = 0.75f;
    public Transform playerSpawn; // optional; auto-find by tag "PlayerSpawn" if null

    [Header("Death Screen")]
    public GameObject deathScreen; // assign Canvas root (keep disabled in scene)

    public int Lives { get; private set; }

    // cache
    private Health playerHealth;
    private Transform playerTransform;

    public Action<int> OnLivesChanged;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Lives = startLives;
    }

    void Start()
    {
        TryFindRefs();
        if (deathScreen) deathScreen.SetActive(false);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (playerHealth) playerHealth.OnDied -= OnPlayerDied;
    }

    // ---------- Coins ----------
    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        coins = Mathf.Max(0, coins + amount);
        onCoinsChanged?.Invoke(coins);
        UIHud.I?.SetCoins(coins);
    }

    public void ResetRun()
    {
        coins = 0;
        onCoinsChanged?.Invoke(coins);
        UIHud.I?.SetCoins(coins);
        Lives = startLives;
        OnLivesChanged?.Invoke(Lives);
    }

    // ---------- Player wiring ----------
    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        TryFindRefs();

        if (!deathScreen)
        {
            var ds = GameObject.Find("DeathScreen");
            if (ds) deathScreen = ds;
        }
        if (deathScreen) deathScreen.SetActive(false); // ensure hidden after load
        Time.timeScale = 1f; // safety
    }

    void TryFindRefs()
    {
        if (!playerTransform)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) RegisterPlayer(p.GetComponent<Health>());
        }

        if (!playerSpawn)
        {
            var sp = GameObject.FindGameObjectWithTag("PlayerSpawn");
            if (sp) playerSpawn = sp.transform;
        }
    }

    public void RegisterPlayer(Health h)
    {
        if (!h) return;

        if (playerHealth) playerHealth.OnDied -= OnPlayerDied;
        playerHealth = h;
        playerTransform = h.transform;
        playerHealth.OnDied += OnPlayerDied;
    }

    // ---------- Death / Respawn flow ----------
    void OnPlayerDied() => StartCoroutine(HandlePlayerDeath());

    IEnumerator HandlePlayerDeath()
    {
        if (Lives > 0)
        {
            Lives--;
            OnLivesChanged?.Invoke(Lives);
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer();
        }
        else
        {
            ShowDeathScreen();
        }
    }

    public void RespawnPlayer()
    {
        if (!playerTransform || !playerHealth) return;

        Vector3 spawnPos = playerSpawn ? playerSpawn.position : playerTransform.position;
        playerTransform.SetPositionAndRotation(spawnPos, playerTransform.rotation);

        var rb = playerTransform.GetComponent<Rigidbody>();
        if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        playerHealth.SetMaxHP(playerHealth.maxHP, refill: true);
    }

    public void ShowDeathScreen()
    {
        Time.timeScale = 0f;
        if (deathScreen) deathScreen.SetActive(true);
        else Debug.LogWarning("[GameManager] DeathScreen not assigned/found.");
    }

    // ---------- UI button hooks ----------
    public void Retry()
    {
        if (deathScreen) deathScreen.SetActive(false);
        Time.timeScale = 1f;
        Lives = startLives;
        OnLivesChanged?.Invoke(Lives);
        SceneManager.LoadScene("Game");
    }

    public void BackToMainMenu()
    {
        if (deathScreen) deathScreen.SetActive(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}