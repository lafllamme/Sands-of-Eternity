using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float speed = 2.5f;
    [Min(0f)] public float stopDistance = 1.1f;   // visuell anhalten

    [Header("Bounds")]
    [Tooltip("Sicherheitsabstand zu den Kanten beim Clampen")]
    [Min(0f)] public float clampPadding = 0.5f;

    Transform player;
    Health health;                 // NEW: cache health for death handling
    Rigidbody rb;
    Collider[] cols;
    EnemyAttack enemyAttack;       // if you added it earlier

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        rb = GetComponent<Rigidbody>();
        if (rb) { rb.useGravity = false; rb.isKinematic = true; }

        cols = GetComponentsInChildren<Collider>(includeInactive: false);
        enemyAttack = GetComponent<EnemyAttack>();

        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDied += HandleDeath;   // subscribe once
        }
        else
        {
            Debug.LogWarning($"[Enemy] No Health found on {name}");
        }
    }

    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        if (!player) return;

        // --- Move towards player ---
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > stopDistance)
        {
            Vector3 dir = (dist > 1e-4f) ? toPlayer / dist : Vector3.zero;
            transform.position += dir * speed * Time.deltaTime;
        }

        // --- Clamp to map bounds (nach der Bewegung!) ---
        if (MapBounds.I != null)
            transform.position = MapBounds.I.ClampXZ(transform.position, clampPadding);
    }

    // ===== Death pipeline =====
    void HandleDeath()
    {
        // 1) stop behavior immediately
        enabled = false;                // stops Update() movement
        if (enemyAttack) enemyAttack.enabled = false;
        if (rb) rb.isKinematic = true;

        // disable all colliders so it can’t hit or be hit anymore
        if (cols != null)
        {
            foreach (var c in cols) if (c) c.enabled = false;
        }

        // (optional) tell the HP-bar to fade immediately if present
        var cg = GetComponentInChildren<CanvasGroup>();
        if (cg) cg.alpha = 0f;

        Debug.Log($"[Enemy] Destroy {name} at {Time.time:F2}");

        // 2) destroy the WHOLE enemy (root is this object)
        Destroy(gameObject);
        // If your Health/Enemy lived on a child in the future, use:
        // Destroy(transform.root.gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        DrawCircleXZ(transform.position, stopDistance);
    }

    static void DrawCircleXZ(Vector3 center, float r, int seg = 32)
    {
        if (r <= 0f) return;
        Vector3 prev = center + new Vector3(r, 0f, 0f);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
#endif
}