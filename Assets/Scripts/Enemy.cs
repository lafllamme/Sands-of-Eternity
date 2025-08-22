using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2.5f;
    public float stopDistance = 1.1f;    // how close it gets before it stops

    [Header("Damage")]
    public int touchDamage = 1;
    public float damageInterval = 0.5f;  // seconds between hits while touching

    Transform player;
    float lastHitTime;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        // ensure trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!player) return;

        // move in XZ plane towards player
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > stopDistance)
        {
            Vector3 dir = toPlayer / dist;
            transform.position += dir * speed * Time.deltaTime;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastHitTime + damageInterval) return;

        var stats = other.GetComponent<PlayerStats>();
        if (stats != null && stats.IsAlive)
        {
            stats.TakeDamage(touchDamage);
            lastHitTime = Time.time;
        }
    }

    // optional gizmo to see stop radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}