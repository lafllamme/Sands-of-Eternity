using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Run Stats")]
    public int coins = 0;
    public Action<int> onCoinsChanged;  // fires with new total

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCoins(int amount)
    {
        if (amount == 0) return;
        coins = Mathf.Max(0, coins + amount);
        onCoinsChanged?.Invoke(coins);
        UIHud.I?.SetCoins(coins);       // safe for now; can remove once HUD subscribes
    }

    public void ResetRun()
    {
        coins = 0;
        onCoinsChanged?.Invoke(coins);
        UIHud.I?.SetCoins(coins);
    }
}