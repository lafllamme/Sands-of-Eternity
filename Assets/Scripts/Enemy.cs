using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float speed = 2.5f;
    [Min(0f)] public float stopDistance = 1.1f;   // how close to move before stopping (visual)

    [Header("Damage (distance-based)")]
    public int   touchDamage   = 1;
    [Min(0f)] public float damageInterval = 0.5f; // sec between hits
    [Min(0f)] public float hitRange       = 1.2f; // damage is applied when dist <= hitRange

    Transform player;
    Health playerHealth;
    float lastHitTime;

    void OnValidate()
    {
        // keep sensible defaults in the Inspector
        if (hitRange < stopDistance) hitRange = stopDistance;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player) playerHealth = player.GetComponent<Health>();

        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.useGravity = false; rb.isKinematic = true; }

        // You can keep your Collider non-trigger if you like; damage is distance-based now.
    }

    void Update()
    {
        if (!player) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        // 1) move until we are close enough visually
        if (dist > stopDistance)
        {
            Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);
            transform.position += dir * speed * Time.deltaTime;
        }

        // 2) tick damage while standing close (no trigger needed)
        if (playerHealth && dist <= hitRange && Time.time >= lastHitTime + damageInterval)
        {
            playerHealth.TakeDamage(touchDamage);
            lastHitTime = Time.time;
            Debug.Log($"[Enemy] Damage tick: -{touchDamage}  dist={dist:F2}");
        }
    }
}
