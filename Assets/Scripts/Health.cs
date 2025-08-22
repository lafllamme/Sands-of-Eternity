using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Min(1)] public int maxHP = 100;
    public int CurrentHP { get; private set; }

    // event: (current, max)
    public event Action<int,int> OnHealthChanged;

    void Awake() {
        CurrentHP = maxHP;
    }

    public void TakeDamage(int amount) {
        if (amount <= 0) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        // später: Tod/Respawn
    }

    public void Heal(int amount) {
        if (amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public float Normalized => maxHP > 0 ? (float)CurrentHP / maxHP : 0f;
}