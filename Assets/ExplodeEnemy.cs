using UnityEngine;

public class ExplodeEnemy : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] gorePrefabs;
    public int piecesToSpawn = 5;
    public float explosionForce = 5f;

    // This is the function you posted
    public void Explode()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();

        // Safety check to prevent errors if you forget to assign prefabs
        if (gorePrefabs == null || gorePrefabs.Length == 0)
        {
            Debug.LogWarning("No gore prefabs assigned on " + gameObject.name);
            Destroy(gameObject); // Just destroy enemy if no gore
            return;
        }

        for (int i = 0; i < piecesToSpawn; i++)
        {
            GameObject gorePrefab = gorePrefabs[Random.Range(0, gorePrefabs.Length)];

            // Random spawn inside scaled collider bounds
            Vector3 spawnPos = transform.position; // Default to center
            if (col != null)
            {
                spawnPos = col.bounds.center + new Vector3(
                    Random.Range(-col.bounds.extents.x * 0.5f, col.bounds.extents.x * 0.5f),
                    Random.Range(-col.bounds.extents.y * 0.5f, col.bounds.extents.y * 0.5f),
                    Random.Range(-col.bounds.extents.z * 0.5f, col.bounds.extents.z * 0.5f)
                );
            }

            // Slight upward adjustment
            spawnPos += Vector3.up * 0.1f;

            GameObject chunk = Instantiate(gorePrefab, spawnPos, Random.rotation);

            Rigidbody rb = chunk.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDir = Random.insideUnitSphere.normalized;
                rb.AddForce(randomDir * explosionForce, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }
}