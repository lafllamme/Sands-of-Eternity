using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud I { get; private set; }

    [Header("HP Bar")]
    [Tooltip("Image type MUST be Filled → Horizontal")]
    public Image hpFill;
    public Gradient hpGradient;

    [Header("Texts")]
    public TMP_Text coinText;

    // cached
    private Health playerHealth;

    void Awake() => I = this;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                UpdateHP(playerHealth.CurrentHP, playerHealth.maxHP);
                playerHealth.OnHealthChanged += UpdateHP;
            }
        }

        if (coinText) coinText.text = (GameManager.I ? GameManager.I.coins : 0).ToString();
        // Falls du ein Coin-Event hast: GameManager.I.onCoinsChanged += UpdateCoins;
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHP;
        // if (GameManager.I) GameManager.I.onCoinsChanged -= UpdateCoins;
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

        if (hpGradient != null) hpFill.color = hpGradient.Evaluate(t);
    	Debug.Log($"[UIHud] UpdateHP {hp}/{max} (t={t:F2}) at {Time.time:F2}");

    }

    private void UpdateCoins(int coins)
    {
        if (coinText) coinText.text = coins.ToString();
    }
}
