using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Run Stats")]
    public int coins = 0;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        UIHud.I?.SetCoins(coins);   // UI aktualisieren
    }
}