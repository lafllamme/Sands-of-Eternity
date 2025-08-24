using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHud : MonoBehaviour
{
    public static UIHud I { get; private set; }

    [Header("HP Bar (child Image must be Filled → Horizontal)")]
    public Image hpFill;
    public Gradient hpGradient;

    [Header("HP Text (optional)")]
    public TMP_Text hpText;
    public bool showPercent = true;

    [Header("Other")]
    public TMP_Text coinText;

    [Header("Lives")]
    public TMP_Text livesText;   // <-- dein neuer Text (Inspector setzen!)

    // cached
    private Health playerHealth;

    void Awake()
    {
        I = this;

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

        if (GameManager.I)
        {
            SetLives(GameManager.I.Lives);
            GameManager.I.OnLivesChanged += SetLives;
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHP;
        if (GameManager.I) GameManager.I.OnLivesChanged -= SetLives;
    }

    // ---------- Public API ----------
    public void SetHP(int hp, int max) => UpdateHP(hp, max);
    public void SetCoins(int coins)     => UpdateCoins(coins);
    public void SetLives(int lives)     => UpdateLives(lives);

    // ---------- Internals ----------
    private void UpdateHP(int hp, int max)
    {
        if (!hpFill || max <= 0) return;

        float t = Mathf.Clamp01((float)hp / max);
        hpFill.fillAmount = t;

        if (hpGradient != null)
            hpFill.color = hpGradient.Evaluate(t);

        if (hpText)
        {
            hpText.text = showPercent ? $"{Mathf.RoundToInt(t * 100f)}%" : $"{hp}/{max}";
            Color c = hpFill.color;
            float luminance = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            hpText.color = (luminance < 0.55f) ? Color.white : Color.black;
        }
    }

    private void UpdateCoins(int coins)
    {
        if (coinText) coinText.text = coins.ToString();
    }

    private void UpdateLives(int lives)
    {
        if (livesText) livesText.text = $"x{lives}";
    }
}