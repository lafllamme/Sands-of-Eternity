using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("Coin Settings")]
    public GameObject coinPrefab;
    public int coinCount = 10;
    public Vector2 spawnArea = new Vector2(20, 20); // X = width, Y = depth (Z)

    [Tooltip("Base Y of the spawned coins (raise them a bit above ground).")]
    public float spawnY = 0.25f;

    [Header("Rotation Options")]
    [Tooltip("Use the prefab's rotation instead of identity.")]
    public bool usePrefabRotation = true;

    [Tooltip("Add a random yaw around world up (keeps them upright).")]
    public bool randomYaw = false;

    void Start() => SpawnCoins();

    void SpawnCoins()
    {
        if (!coinPrefab) return;

        for (int i = 0; i < coinCount; i++)
        {
            float x = Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f);
            float z = Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f);
            Vector3 pos = new Vector3(x, spawnY, z) + transform.position;

            Quaternion rot = usePrefabRotation ? coinPrefab.transform.rotation : Quaternion.identity;
            if (randomYaw)
                rot = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * rot;

            Instantiate(coinPrefab, pos, rot);
        }
    }

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