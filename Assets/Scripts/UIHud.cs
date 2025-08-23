using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud I { get; private set; }

    [Header("HP Bar (child Image must be Filled → Horizontal)")]
    public Image hpFill;                 // drag HPBarFill here
    public Gradient hpGradient;          // green→yellow→red gradient

    [Header("HP Text (optional)")]
    public TMP_Text hpText;              // drag HPText here (TextMeshPro)
    public bool showPercent = true;      // false = show "HP / Max" instead

    [Header("Other")]
    public TMP_Text coinText;

    // cached
    private Health playerHealth;

    void Awake()
    {
        I = this;

        // Safety: make sure the fill image is configured correctly
        if (hpFill && hpFill.type != Image.Type.Filled)
        {
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

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
        // If you later expose a coin event:
        // GameManager.I.onCoinsChanged += UpdateCoins;
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHP;
        // if (GameManager.I) GameManager.I.onCoinsChanged -= UpdateCoins;
    }

    // ---------- Public API ----------
    public void SetHP(int hp, int max) => UpdateHP(hp, max);
    public void SetCoins(int coins)     => UpdateCoins(coins);

    // ---------- Internals ----------
    private void UpdateHP(int hp, int max)
    {
        if (!hpFill || max <= 0) return;

        float t = Mathf.Clamp01((float)hp / max);

        // Fill amount
        hpFill.fillAmount = t;

        // Bar color from gradient (1 = full, 0 = empty)
        if (hpGradient != null)
            hpFill.color = hpGradient.Evaluate(t);

        // Text (percent or absolute)
        if (hpText)
        {
            hpText.text = showPercent ? $"{Mathf.RoundToInt(t * 100f)}%" : $"{hp}/{max}";

            // Auto-contrast: pick white/black based on bar brightness
            Color c = hpFill.color;
            float luminance = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b; // perceptual
            hpText.color = (luminance < 0.55f) ? Color.white : Color.black;
        }

        // Debug
        // Debug.Log($"[UIHud] UpdateHP {hp}/{max} (t={t:F2}) at {Time.time:F2}");
    }

    private void UpdateCoins(int coins)
    {
        if (coinText) coinText.text = coins.ToString();
    }
}
