using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 10;
    public int hp;

    void Awake() => hp = maxHP;

    public void TakeDamage(int dmg)
    {
        hp = Mathf.Max(0, hp - dmg);
        UIHud.I?.SetHP(hp, maxHP);
        if (hp == 0) OnDeath();
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(maxHP, hp + amount);
        UIHud.I?.SetHP(hp, maxHP);
    }

    void OnDeath()
    {
        // später: Respawn/Reload Scene
        Debug.Log("Player down");
    }
}