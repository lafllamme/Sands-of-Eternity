using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public Key attackKey = Key.X;
    public int damage = 5;
    public float cooldown = 0.35f;

    [Tooltip("Radius der Trefferkugel")]
    public float radius = 0.75f;

    [Tooltip("Wie weit vor dem Spieler zentrieren wir die Kugel?")]
    public float range = 1.25f;

    [Tooltip("Optional: eigener Punkt für die Trefferkugel")]
    public Transform attackPoint;

    [Tooltip("Nur Gegner-Layer anhaken!")]
    public LayerMask enemyMask;

    float lastAttack;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (Time.time < lastAttack + cooldown) return;

        if (kb[attackKey].wasPressedThisFrame)
            DoAttack();
    }

    void DoAttack()
    {
        lastAttack = Time.time;

        Vector3 origin = attackPoint
            ? attackPoint.position
            : transform.position + transform.forward * range * 0.5f;

        Collider[] hits = Physics.OverlapSphere(
            origin, radius, enemyMask, QueryTriggerInteraction.Collide);

        Debug.Log($"[Attack] swing @ {origin} r={radius} hits={hits.Length} t={Time.time:F2}");

        foreach (var hit in hits)
        {
            var h = hit.GetComponentInParent<Health>();
            if (h != null)
            {
                int before = h.CurrentHP;
                h.TakeDamage(damage);
                Debug.Log($"[Attack] hit {hit.name} {before}->{h.CurrentHP}/{h.maxHP}");
            }
            else
            {
                Debug.Log($"[Attack] {hit.name} has no Health");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint
            ? attackPoint.position
            : transform.position + transform.forward * range * 0.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, radius);
    }
}