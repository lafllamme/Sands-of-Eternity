using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Gameplay")]
    public Key attackKey = Key.X;
    public int damage = 5;
    public float cooldown = 0.35f;

    [Tooltip("Radius der Trefferkugel")]
    public float radius = 0.75f;

    [Tooltip("Fallback-Distanz falls kein attackPoint/slashTip")]
    public float range = 1.25f;

    [Tooltip("Optionaler Ursprung für die OverlapSphere")]
    public Transform attackPoint;

    [Tooltip("Nur Enemy-Layer anhaken!")]
    public LayerMask enemyMask;

    AttackFX fx;
    float lastAttack;

    void Awake()
    {
        fx = GetComponent<AttackFX>();
        if (fx) fx.OnHitMoment += DoDamage;           // Damage exakt im Swing
    }

    void OnDestroy()
    {
        if (fx) fx.OnHitMoment -= DoDamage;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (Time.time < lastAttack + cooldown) return;

        if (kb[attackKey].wasPressedThisFrame)
        {
            lastAttack = Time.time;
            if (fx) fx.PlaySwing(+1);                 // rechtsherum; bei Bedarf Richtung übergeben
            else DoDamage();                          // Fallback ohne FX
        }
    }

    void DoDamage()
    {
        Vector3 origin =
            attackPoint ? attackPoint.position :
            (fx && fx.slashTip ? fx.slashTip.position : transform.position + transform.forward * (range * 0.5f));

        var hits = Physics.OverlapSphere(origin, radius, enemyMask, QueryTriggerInteraction.Collide);

        foreach (var h in hits)
        {
            var hp = h.GetComponentInParent<Health>();
            if (hp == null) continue;

            hp.TakeDamage(damage);

            // Treffer-VFX optional über FX
            if (fx)
            {
                Vector3 p = h.ClosestPoint(origin);
                fx.HitAt(p);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 origin =
            attackPoint ? attackPoint.position :
            (fx && fx.slashTip ? fx.slashTip.position : transform.position + transform.forward * (range * 0.5f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, radius);
    }
#endif
}