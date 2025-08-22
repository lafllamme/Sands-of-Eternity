using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud I { get; private set; }

    [Header("HP Bar")]
    [Tooltip("Image type MUST be Filled → Horizontal")]
    public Image hpFill;
    public Gradient hpGradient;   // set in Inspector (Red ← Yellow ← Green)

    [Header("Texts")]
    public TMP_Text coinText;

    // cached
    private PlayerStats playerStats;

    void Awake() => I = this;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats)
            {
                // initial push (nutzt Getter HP)
                UpdateHP(playerStats.HP, playerStats.maxHP);
                // subscribe
                playerStats.onHealthChanged += UpdateHP;
            }
        }

        // Coins initial + subscribe (falls GameManager Event hat)
        if (coinText) coinText.text = (GameManager.I ? GameManager.I.coins : 0).ToString();
        if (GameManager.I != null) GameManager.I.onCoinsChanged += UpdateCoins;
    }

    void OnDestroy()
    {
        if (playerStats != null) playerStats.onHealthChanged -= UpdateHP;
        if (GameManager.I != null) GameManager.I.onCoinsChanged -= UpdateCoins;
    }

    // ---------- Public API ----------
    public void SetHP(int hp, int max) => UpdateHP(hp, max);
    public void SetCoins(int coins) => UpdateCoins(coins);

    // ---------- Internals ----------
    private void UpdateHP(int hp, int max)
    {
        if (!hpFill || max <= 0) return;

        float t = Mathf.Clamp01((float)hp / max);
        hpFill.fillAmount = t;

        // Farbe aus Gradient (1 = full, 0 = empty)
        if (hpGradient != null) hpFill.color = hpGradient.Evaluate(t);
    }

    private void UpdateCoins(int coins)
    {
        if (coinText) coinText.text = coins.ToString();
    }
}