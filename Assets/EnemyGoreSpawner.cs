using UnityEngine;
using System.Collections;

// This script handles the enemy's health, gore effects, blood stains, and death sounds.
public class EnemyGoreSpawner : MonoBehaviour
{
    [Header("Gore Settings")]
    public GameObject[] gorePrefabs;
    public int piecesToSpawn = 8;
    public float explosionForce = 6f;
    public float goreLifetime = 8f;
    public float physicsDisableDelay = 1.5f;

    [Header("Performance Limits")]
    public int maxActiveGore = 30;
    private static int activeGoreCount;

    [Header("Blood Effects")]
    public GameObject bloodParticleSystemPrefab;
    public GameObject bloodStainPrefab;
    public float bloodParticleDuration = 1f;
    public float bloodStainDuration = 30f;
    public Vector2 bloodStainSizeRange = new Vector2(0.8f, 1.2f);

    [Header("Raycast Settings")]
    public LayerMask floorLayerMask = ~0;
    public float raycastDistance = 2f;
    public Vector3 raycastOffset = new Vector3(0, 0.5f, 0);

    [Header("Spawn Offset")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Optional Health")]
    public int health = 50;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    [Header("Pitch Randomization")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            ExplodeEnemy();
        }
    }

    private void ExplodeEnemy()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) col = GetComponentInChildren<Collider>();

        // Play death sound
        PlayDeathSound();

        // Blood particles
        if (bloodParticleSystemPrefab != null)
        {
            GameObject bloodParticles =
                Instantiate(bloodParticleSystemPrefab, transform.position + spawnOffset, Quaternion.identity);
            Destroy(bloodParticles, bloodParticleDuration);
        }

        // Blood stain
        CreateBloodStain();

        // Spawn gore pieces (with hard cap)
        for (int i = 0; i < piecesToSpawn; i++)
        {
            if (activeGoreCount >= maxActiveGore)
                break;

            SpawnGorePiece(col);
        }

        Destroy(gameObject);
    }

    private void PlayDeathSound()
    {
        if (deathClip == null)
        {
            return;
        }

        // Create a temporary game object to play the sound at the enemy's position
        // This ensures the sound plays even if the enemy is Destroy()'d immediately.
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = transform.position;

        // Add an AudioSource and configure it
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = deathClip;
        aSource.volume = volume;

        // Apply the random pitch
        aSource.pitch = Random.Range(minPitch, maxPitch);

        // Play and set to destroy after the clip finishes
        aSource.Play();
        Destroy(tempGO, deathClip.length);
    }

    private void SpawnGorePiece(Collider col)
    {
        if (gorePrefabs == null || gorePrefabs.Length == 0 || col == null)
            return;

        GameObject gorePrefab = gorePrefabs[Random.Range(0, gorePrefabs.Length)];

        Vector3 randomLocalOffset = new Vector3(
            Random.Range(-col.bounds.extents.x * 0.5f, col.bounds.extents.x * 0.5f),
            Random.Range(-col.bounds.extents.y * 0.5f, col.bounds.extents.y * 0.5f),
            Random.Range(-col.bounds.extents.z * 0.5f, col.bounds.extents.z * 0.5f)
        );

        Vector3 spawnPos = col.bounds.center + randomLocalOffset + spawnOffset;
        GameObject chunk = Instantiate(gorePrefab, spawnPos, Random.rotation);

        activeGoreCount++;

        // Add tracking component to ensure counter decrements
        GoreTracker tracker = chunk.AddComponent<GoreTracker>();

        // Force cheap physics settings
        Rigidbody rb = chunk.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.interpolation = RigidbodyInterpolation.None;

            Vector3 randomDir = Random.insideUnitSphere.normalized;
            rb.AddForce(randomDir * explosionForce, ForceMode.Impulse);

            StartCoroutine(DisablePhysicsAfterDelay(rb, physicsDisableDelay));
        }

        // Auto-despawn
        StartCoroutine(DestroyGoreAfterTime(chunk, goreLifetime));
    }

    private IEnumerator DisablePhysicsAfterDelay(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }

    private IEnumerator DestroyGoreAfterTime(GameObject gore, float time)
    {
        yield return new WaitForSeconds(time);

        if (gore != null)
        {
            activeGoreCount--;
            Destroy(gore);
        }
    }

    private void CreateBloodStain()
    {
        if (bloodStainPrefab == null)
            return;

        Vector3 rayStart = transform.position + raycastOffset;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, floorLayerMask))
        {
            GameObject bloodStain = Instantiate(bloodStainPrefab);

            bloodStain.transform.position = hit.point + hit.normal * 0.01f;
            bloodStain.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            bloodStain.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.Self);

            float randomScale = Random.Range(bloodStainSizeRange.x, bloodStainSizeRange.y);
            bloodStain.transform.localScale = Vector3.one * randomScale;

            bloodStain.transform.SetParent(hit.transform);
            Destroy(bloodStain, bloodStainDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + raycastOffset;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * raycastDistance);
        Gizmos.DrawWireSphere(rayStart, 0.1f);
    }
}

// Helper component to ensure gore count is always properly decremented
public class GoreTracker : MonoBehaviour
{
    private void OnDestroy()
    {
        // Access the static counter through reflection or make it accessible
        var goreSpawnerType = typeof(EnemyGoreSpawner);
        var field = goreSpawnerType.GetField("activeGoreCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (field != null)
        {
            int currentCount = (int)field.GetValue(null);
            field.SetValue(null, Mathf.Max(0, currentCount - 1));
        }
    }
}