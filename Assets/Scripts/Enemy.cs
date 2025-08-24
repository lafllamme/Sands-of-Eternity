using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] public float speed = 2.5f;
    [Min(0f)] public float turnSpeed = 12f;
    [Min(0f)] public float stopDistance = 1.1f;   // stop this close to target

    [Header("Aggro (chase logic)")]
    [Min(0f)] public float aggroRadius = 6f;      // start chasing
    [Min(0f)] public float loseAggroRadius = 9f;  // give up if farther than this
    [Min(0f)] public float loseDelay = 1.25f;     // memory time after losing sight

    [Header("Wander (when not chasing)")]
    [Min(0f)] public float wanderRadius = 4f;     // roam around the spawn point
    [Min(0f)] public float waypointTolerance = 0.2f;
    public Vector2 wanderWait = new Vector2(0.6f, 1.6f); // min/max seconds between picks

    [Header("Bounds")]
    [Tooltip("Sicherheitsabstand zu den Kanten beim Clampen")]
    [Min(0f)] public float clampPadding = 0.5f;

    // ---- internals ----
    enum State { Wander, Chase }
    State state = State.Wander;

    Transform player;
    Health health;
    Rigidbody rb;
    Collider[] cols;
    EnemyAttack enemyAttack;

    Vector3 homePos;
    Vector3 wanderTarget;
    float   nextWanderTime;
    float   lostPlayerAt = -1f;
    float   yLock;

    void OnValidate()
    {
        if (loseAggroRadius < aggroRadius + 0.01f)
            loseAggroRadius = aggroRadius + 0.01f;
        if (wanderWait.x > wanderWait.y)
            (wanderWait.x, wanderWait.y) = (wanderWait.y, wanderWait.x);
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        rb = GetComponent<Rigidbody>();
        if (rb) { rb.useGravity = false; rb.isKinematic = true; }

        cols = GetComponentsInChildren<Collider>(includeInactive: false);
        enemyAttack = GetComponent<EnemyAttack>();

        health = GetComponent<Health>();
        if (health) health.OnDied += HandleDeath; else Debug.LogWarning($"[Enemy] No Health on {name}");

        homePos = transform.position;
        yLock   = transform.position.y;
        PickNewWanderTarget(immediate: true);
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDeath;
    }

    void Update()
    {
        float distToPlayer = player ? DistXZ(transform.position, player.position) : float.PositiveInfinity;

        // ---- state transitions ----
        if (player)
        {
            if (state != State.Chase && distToPlayer <= aggroRadius)
            {
                state = State.Chase;
                lostPlayerAt = -1f;
            }
            else if (state == State.Chase)
            {
                if (distToPlayer > loseAggroRadius)
                {
                    if (lostPlayerAt < 0f) lostPlayerAt = Time.time;               // start memory timer
                    if (Time.time - lostPlayerAt >= loseDelay)
                    {
                        state = State.Wander;
                        PickNewWanderTarget(immediate: false);
                    }
                }
                else
                {
                    lostPlayerAt = -1f; // still close → keep chasing
                }
            }
        }

        // ---- act ----
        if (state == State.Chase && player)
        {
            MoveTowards(player.position, stopDistance);
        }
        else // Wander
        {
            if (Time.time >= nextWanderTime ||
                DistXZ(transform.position, wanderTarget) <= waypointTolerance)
            {
                PickNewWanderTarget(immediate: false);
            }
            MoveTowards(wanderTarget, 0f);
        }

        // keep Y locked & clamp to map
        var p = transform.position; p.y = yLock;
        if (MapBounds.I != null) p = MapBounds.I.ClampXZ(p, clampPadding);
        transform.position = p;
    }

    // ---- movement helpers ----
    void MoveTowards(Vector3 targetWorld, float stop)
    {
        Vector3 to = targetWorld - transform.position; to.y = 0f;
        float dist = to.magnitude;
        if (dist > Mathf.Max(stop, 0f))
        {
            Vector3 dir = to / Mathf.Max(dist, 0.0001f);
            transform.position += dir * speed * Time.deltaTime;

            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }
    }

    void PickNewWanderTarget(bool immediate)
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 guess = homePos + new Vector3(r.x, 0f, r.y);
        wanderTarget = (MapBounds.I != null) ? MapBounds.I.ClampXZ(guess, clampPadding) : guess;
        nextWanderTime = Time.time + (immediate ? 0f : Random.Range(wanderWait.x, wanderWait.y));
    }

    static float DistXZ(Vector3 a, Vector3 b) { a.y = b.y = 0f; return (a - b).magnitude; }

    // ===== Death pipeline =====
    void HandleDeath()
    {
        // stop behaviour
        enabled = false;
        if (enemyAttack) enemyAttack.enabled = false;
        if (rb) rb.isKinematic = true;

        // disable all colliders
        if (cols != null) foreach (var c in cols) if (c) c.enabled = false;

        // hide any local HP bar instantly
        var cg = GetComponentInChildren<CanvasGroup>(); if (cg) cg.alpha = 0f;

        Debug.Log($"[Enemy] Destroy {name} at {Time.time:F2}");
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Color c = Gizmos.color;
        // aggro ring
        Gizmos.color = new Color(0f, 1f, .4f, .35f);
        Gizmos.DrawWireSphere(transform.position, aggroRadius);
        // lose ring
        Gizmos.color = new Color(1f, .4f, .2f, .35f);
        Gizmos.DrawWireSphere(transform.position, loseAggroRadius);
        // stop distance (small ring)
        Gizmos.color = new Color(1f, .7f, 0f, .6f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = c;
    }
#endif
}