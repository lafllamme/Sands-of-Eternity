using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("Coin Settings")]
    public GameObject coinPrefab;                 // Prefab-Referenz
    public int coinCount = 10;                    // wie viele Coins insgesamt
    public Vector2 spawnArea = new Vector2(20, 20); // X = Breite, Y = Tiefe (Z)

    [Tooltip("Y-Höhe, auf der die Coins liegen sollen (matcht Boden/Player).")]
    public float spawnY = 0.25f;

    void Start()
    {
        SpawnCoins();
    }

    void SpawnCoins()
    {
        if (!coinPrefab) return;

        for (int i = 0; i < coinCount; i++)
        {
            // Zufällige Position innerhalb der Fläche (X/Z). Y bleibt konstant.
            float x = Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f);
            float z = Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f);

            Vector3 pos = new Vector3(x, spawnY, z) + transform.position;
            Instantiate(coinPrefab, pos, Quaternion.identity);
        }
    }

    // Optional: Visualisierung der Spawnfläche im Editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
        Vector3 size = new Vector3(spawnArea.x, 0.01f, spawnArea.y);
        Vector3 center = transform.position + new Vector3(0f, spawnY, 0f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}