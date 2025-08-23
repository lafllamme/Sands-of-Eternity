using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    public int damage = 2;
    public float cooldown = 0.6f;

    [Tooltip("Radius der Trefferkugel")]
    public float radius = 0.9f;

    [Tooltip("Wie weit vor dem Enemy wird die Kugel zentriert?")]
    public float range = 1.1f;

    [Tooltip("Optional: eigener Punkt für die Trefferkugel (leer-Child)")]
    public Transform attackPoint;

    [Tooltip("Nur den Player-Layer anhaken!")]
    public LayerMask playerMask;

    [Header("Auto trigger")]
    [Tooltip("Nur angreifen, wenn der Spieler näher als dieser Wert ist")]
    public float engageDistance = 1.3f;

    [Header("Debug")]
    public bool debugLogs = false;

    float lastAttack;
    Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (debugLogs)
            Debug.Log($"[EnemyAttack] Start. player={(player ? player.name : "null")}");
    }

    void Update()
    {
        if (!player) return;

        // nur attackieren wenn nah genug + cooldown fertig
        if (Time.time < lastAttack + cooldown) return;

        float distXZ = DistanceXZ(transform.position, player.position);
        if (distXZ > engageDistance) return;

        DoAttack();
    }

    void DoAttack()
    {
        lastAttack = Time.time;

        Vector3 origin = attackPoint
            ? attackPoint.position
            : transform.position + transform.forward * range * 0.5f;

        // nur Player-Layer
        Collider[] hits = Physics.OverlapSphere(
            origin, radius, playerMask, QueryTriggerInteraction.Collide);

        if (debugLogs)
            Debug.Log($"[EnemyAttack] swing @ {origin} r={radius} hits={hits.Length} t={Time.time:F2}");

        foreach (var hit in hits)
        {
            var h = hit.GetComponentInParent<Health>();
            if (h != null)
            {
                int before = h.CurrentHP;
                h.TakeDamage(damage);
                if (debugLogs)
                    Debug.Log($"[EnemyAttack] hit {hit.name} {before}->{h.CurrentHP}/{h.maxHP}");
            }
            else if (debugLogs)
            {
                Debug.Log($"[EnemyAttack] {hit.name} has NO Health");
            }
        }
    }

    // Nur XZ-Abstand (Topdown)
    static float DistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return (a - b).magnitude;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint
            ? attackPoint.position
            : transform.position + transform.forward * range * 0.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, radius);

        // Engage-Radius (nur zur Orientierung)
        Gizmos.color = new Color(1f, .3f, 0f, .5f);
        Gizmos.DrawWireSphere(transform.position, engageDistance);
    }
#endif
}