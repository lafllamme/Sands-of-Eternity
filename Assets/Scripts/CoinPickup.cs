using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    [Header("Pickup")]
    public int amount = 1;
    public bool debugLogs = false;

    // ---------- Spin ----------
    public enum AxisPreset { Up, Right, Forward, Custom }

    [Header("Spin")]
    public AxisPreset spinAxis = AxisPreset.Up;     // Up = coin-like spin
    public Vector3 customAxis = Vector3.up;         // used when spinAxis = Custom
    public float rotateSpeed = 180f;
    [Tooltip("ON = spin around a global axis (recommended). OFF = spin around the coin's local axis.")]
    public bool spinInWorldSpace = true;

    // ---------- Bobbing ----------
    [Header("Bobbing")]
    [Min(0f)] public float bobAmplitude = 0.1f;     // 0 = off
    [Min(0f)] public float bobSpeed = 2.5f;
    public bool randomizeBobPhase = true;

    // ---------- FX ----------
    [Header("FX (optional)")]
    public AudioClip pickupSfx;
    public ParticleSystem pickupVfx;
    public float destroyDelay = 0.05f;

    float baseY;
    float bobPhase;
    bool collected;
    Collider col;
    Renderer[] rends;

    void OnValidate()
    {
        // Make sure we behave as a trigger pickup
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void Awake()
    {
        col   = GetComponent<Collider>();
        rends = GetComponentsInChildren<Renderer>(includeInactive: false);

        baseY    = transform.position.y;
        bobPhase = randomizeBobPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void Update()
    {
        // ---- Spin ----
        if (rotateSpeed != 0f)
        {
            Vector3 localAxis = spinAxis switch
            {
                AxisPreset.Up      => Vector3.up,
                AxisPreset.Right   => Vector3.right,
                AxisPreset.Forward => Vector3.forward,
                _                  => (customAxis.sqrMagnitude > 0f ? customAxis.normalized : Vector3.up)
            };

            if (spinInWorldSpace)
            {
                // Map preset to WORLD axis (consistent for all spawned coins)
                Vector3 worldAxis = spinAxis switch
                {
                    AxisPreset.Up      => Vector3.up,
                    AxisPreset.Right   => Vector3.right,
                    AxisPreset.Forward => Vector3.forward,
                    _                  => (customAxis.sqrMagnitude > 0f ? customAxis.normalized : Vector3.up)
                };
                transform.Rotate(worldAxis * rotateSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                transform.Rotate(localAxis * rotateSpeed * Time.deltaTime, Space.Self);
            }
        }

        // ---- Bobbing ----
        if (bobAmplitude > 0f && bobSpeed > 0f)
        {
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
            transform.position = p;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;

        // immediately unpickable & invisible
        if (col) col.enabled = false;
        if (rends != null) foreach (var r in rends) if (r) r.enabled = false;

        GameManager.I?.AddCoins(amount);
        if (debugLogs) Debug.Log($"[Coin] +{amount} @ {transform.position}");

        if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position, 0.9f);
        if (pickupVfx)
        {
            var v = Instantiate(pickupVfx, transform.position, Quaternion.identity);
            var m = v.main;
            float killAfter = m.duration + (m.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? m.startLifetime.constantMax
                : m.startLifetime.constant);
            Destroy(v.gameObject, killAfter + 0.1f);
        }

        Destroy(gameObject, destroyDelay);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Small gizmo to visualize the spin axis at edit-time (uses world axis when enabled)
        Vector3 pos = transform.position + Vector3.up * 0.1f;
        Vector3 axisLocal = spinAxis switch
        {
            AxisPreset.Up      => Vector3.up,
            AxisPreset.Right   => Vector3.right,
            AxisPreset.Forward => Vector3.forward,
            _                  => (customAxis.sqrMagnitude > 0f ? customAxis.normalized : Vector3.up)
        };
        Vector3 axis = spinInWorldSpace ? 
            (spinAxis == AxisPreset.Custom ? axisLocal : (spinAxis == AxisPreset.Up ? Vector3.up : spinAxis == AxisPreset.Right ? Vector3.right : Vector3.forward))
            : transform.TransformDirection(axisLocal);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos - axis * 0.5f, pos + axis * 0.5f);
    }
#endif
}