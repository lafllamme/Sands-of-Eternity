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

    [Tooltip("Seconds to wait before respawn (shown in HUD).")]
    public float respawnDelay = 0.75f;

    [Tooltip("Keep this much distance to the MapBounds walls when picking a random spawn.")]
    public float respawnPadding = 0.8f;

    [Tooltip("Optional fixed spawn. If null, we spawn to a random safe point inside MapBounds.")]
    public Transform playerSpawn;

    [Header("Death Screen")]
    [Tooltip("Assign the Canvas root. Keep disabled in the scene.")]
    public GameObject deathScreen;

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

            // Small HUD overlay while waiting (if you implemented it)
            UIHud.I?.ShowRespawn(respawnDelay, "RESPAWNING...");

            // unscaled time → unaffected by Time.timeScale
            float t = 0f;
            while (t < respawnDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

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

        // --- Pick respawn position ---
        Vector3 spawnPos;
        if (playerSpawn)
        {
            spawnPos = playerSpawn.position;
        }
        else if (MapBounds.I != null)
        {
            // uniform random inside bounds with padding, keep current Y
            Vector3 p = MapBounds.I.RandomPoint(respawnPadding);
            spawnPos = new Vector3(p.x, playerTransform.position.y, p.z);
        }
        else
        {
            // fallback: current position
            spawnPos = playerTransform.position;
        }

        // Move & clear velocities
        playerTransform.SetPositionAndRotation(spawnPos, playerTransform.rotation);
        var rb = playerTransform.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;         // <- correct property
            rb.angularVelocity = Vector3.zero;
        }

        // Refill HP
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