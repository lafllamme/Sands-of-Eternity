using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int hp;   // im Inspector sichtbar

    public int MaxHP => maxHP;
    public int HP    => hp;            // Read-only Getter

    // Events (gelassen wie bei dir, damit UIHud-Zuweisungen passen)
    public Action<int, int> onHealthChanged; // (hp, maxHP)
    public Action onDied;

    void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        hp    = Mathf.Max(1, maxHP);
        RaiseHealthChanged();
    }

    public bool IsAlive => hp > 0;

    public void TakeDamage(int dmg)
    {
        if (dmg <= 0 || !IsAlive) return;

        hp = Mathf.Max(0, hp - dmg);
        RaiseHealthChanged();

        if (hp == 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive) return;

        hp = Mathf.Min(maxHP, hp + amount);
        RaiseHealthChanged();
    }

    public void SetMaxHP(int newMax, bool refill = true)
    {
        maxHP = Mathf.Max(1, newMax);

        if (refill) hp = maxHP;
        hp = Mathf.Clamp(hp, 0, maxHP);

        RaiseHealthChanged();

        if (hp == 0) Die();
    }

    // ---- Helpers ----
    private void RaiseHealthChanged() => onHealthChanged?.Invoke(hp, maxHP);

    private void Die()
    {
        Debug.Log("Player down");
        onDied?.Invoke();
        // TODO: Disable Input / Respawn / Reload Scene etc.
    }
}