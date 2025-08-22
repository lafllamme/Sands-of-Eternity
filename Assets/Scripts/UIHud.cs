using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud I { get; private set; }

    [Header("HP")]
    public Image hpFill;      // referenziert das "HPBarFill" Image
    [Header("Coins")]
    public TMP_Text coinText; // referenziert den Text "CoinText"

    void Awake() => I = this;

    public void SetHP(int hp, int max)
    {
        if (!hpFill) return;
        hpFill.fillAmount = max > 0 ? (float)hp / max : 0f;
    }

    public void SetCoins(int coins)
    {
        if (coinText) coinText.text = coins.ToString();
    }

    void Start()
    {
        // Player holen 
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        var ps = playerGO ? playerGO.GetComponent<PlayerStats>() : null;
        if (ps) SetHP(ps.hp, ps.maxHP);
        SetCoins(GameManager.I ? GameManager.I.coins : 0);
    }
}