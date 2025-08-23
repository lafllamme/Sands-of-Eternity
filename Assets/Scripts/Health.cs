using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Min(1)] public int maxHP = 100;
    public int CurrentHP { get; private set; }

    // Debug
    [Header("Debug")]
    public bool debugLogs = true;           // zum An/Abschalten im Inspector

    // Events
    public event Action<int,int> OnHealthChanged; // (current,max)
    public event Action OnDied;

    void Awake()
    {
        CurrentHP = Mathf.Clamp(CurrentHP <= 0 ? maxHP : CurrentHP, 0, maxHP);
        if (debugLogs) Debug.Log($"[Health:{name}] Awake → {CurrentHP}/{maxHP}");
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    void OnEnable()
    {
        if (debugLogs) StartCoroutine(DebugTicker());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator DebugTicker()
    {
        while (true)
        {
            Debug.Log($"[Health:{name}] Tick {Time.time:F2} → {CurrentHP}/{maxHP} Alive:{IsAlive}");
            yield return new WaitForSeconds(1f);
        }
    }

    public float Normalized => maxHP > 0 ? (float)CurrentHP / maxHP : 0f;
    public bool IsAlive => CurrentHP > 0;

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            if (debugLogs) Debug.Log($"[Health:{name}] TakeDamage({amount}) IGNORED (alive:{IsAlive})");
            return;
        }

        int before = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        if (debugLogs) Debug.Log($"[Health:{name}] Took {amount} dmg: {before}→{CurrentHP} / {maxHP}");
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP == 0) Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        int before = CurrentHP;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        if (debugLogs) Debug.Log($"[Health:{name}] Healed {amount}: {before}→{CurrentHP} / {maxHP}");
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public void SetMaxHP(int newMax, bool refill = true)
    {
        maxHP = Mathf.Max(1, newMax);
        if (refill) CurrentHP = maxHP;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);
        if (debugLogs) Debug.Log($"[Health:{name}] SetMaxHP({newMax}, refill:{refill}) → {CurrentHP}/{maxHP}");
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        if (CurrentHP == 0) Die();
    }

    void Die()
    {
        if (debugLogs) Debug.Log($"[Health:{name}] DIED at {Time.time:F2}");
        OnDied?.Invoke();
    }
}
