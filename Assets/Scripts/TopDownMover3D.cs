using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TopDownMover3D : MonoBehaviour
{
    public float speed = 5f;
    public float smoothTurn = 15f;

    [Header("Map Bounds")]
    [SerializeField] Transform ground;   // <-- hier deine Plane reinziehen
    [SerializeField] float margin = 0.5f;

    float yLock;

    void Start() => yLock = transform.position.y;

    void Update()
    {
        // --- Input ---
        Vector2 m = Vector2.zero;
        #if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed)  m.x -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) m.x += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed)  m.y -= 1;
            if (k.wKey.isPressed || k.upArrowKey.isPressed)    m.y += 1;
        }
        #else
        m = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        #endif

        Vector3 dir = new Vector3(m.x, 0, m.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        transform.position += dir * speed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.0001f)
        {
            var target = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothTurn * Time.deltaTime);
        }

        // am Boden halten
        var p = transform.position; p.y = yLock;

        // --- Bounds-Clamp basierend auf Plane-Größe ---
        if (ground)
        {
            // Unity-Plane ist 10x10 bei Scale=1 → Hälfte = 5
            float halfX = 5f * ground.localScale.x;
            float halfZ = 5f * ground.localScale.z;

            p.x = Mathf.Clamp(p.x, ground.position.x - halfX + margin, ground.position.x + halfX - margin);
            p.z = Mathf.Clamp(p.z, ground.position.z - halfZ + margin, ground.position.z + halfZ - margin);
        }

        transform.position = p;
    }
}
