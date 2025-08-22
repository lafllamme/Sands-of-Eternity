using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public int amount = 1;
    public float rotateSpeed = 120f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return; // Tag so, wie du ihn angelegt hast
        GameManager.I?.AddCoins(amount);
        Destroy(gameObject);
    }
}