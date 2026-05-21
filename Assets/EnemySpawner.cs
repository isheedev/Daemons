using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab / Pooling")]
    public GameObject enemyPrefab;
    [Tooltip("Enable simple pooling to reuse enemies instead of Instantiate/Destroy")]
    public bool usePooling = true;
    [Tooltip("Only used if usePooling is true")]
    public int poolSize = 20;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints; // leave empty to spawn at random positions around center
    public float spawnRadius = 6f;  // used if spawnPoints is empty (random circle around spawner)
    [Tooltip("Seconds between spawns")]
    public float spawnInterval = 2f;
    public int maxAlive = 10;       // max alive at one time
    public bool spawnOnStart = true;
    public bool randomizeSpawnPoint = true; // pick random spawnPoint, otherwise cycle through

    [Header("Debug")]
    public bool showGizmos = true;

    // Internal
    private List<GameObject> pool;
    private int poolIndex = 0;
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int nextSpawnPointIndex = 0;

    void Awake()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab is not assigned.", this);
            enabled = false;
            return;
        }

        if (usePooling)
            CreatePool();
    }

    void Start()
    {
        if (spawnOnStart)
            StartSpawning();
    }

    void OnEnable()
    {
        // In case StartSpawning was called earlier while disabled
    }

    void OnDisable()
    {
        StopSpawning();
    }

    void CreatePool()
    {
        pool = new List<GameObject>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(enemyPrefab, transform);
            go.SetActive(false);
            AttachReturnOnDeath(go);
            pool.Add(go);
        }
    }

    // Optional: attach a component or subscribe to enemy death to return to pool.
    // This implementation expects enemies to call "NotifyDeath" on this spawner,
    // or you can wire a small script on enemy that calls it when destroyed/disabled.
    void AttachReturnOnDeath(GameObject enemy)
    {
        var r = enemy.GetComponent<SpawnerReturn>();
        if (r == null)
            enemy.AddComponent<SpawnerReturn>().owner = this;
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        // enforce max alive
        if (aliveEnemies.Count >= maxAlive) return;

        Vector3 spawnPos = CalculateSpawnPosition();
        GameObject enemy = GetEnemyInstance(spawnPos, Quaternion.identity);
        if (enemy == null) return;

        // Optionally initialize enemy (health, AI target, etc.) here:
        // var ai = enemy.GetComponent<EnemyAI>();
        // if (ai) ai.SetTarget(playerTransform);

        aliveEnemies.Add(enemy);
    }

    GameObject GetEnemyInstance(Vector3 pos, Quaternion rot)
    {
        if (usePooling)
        {
            // find a free object in the pool
            for (int i = 0; i < pool.Count; i++)
            {
                poolIndex = (poolIndex + 1) % pool.Count;
                var candidate = pool[poolIndex];
                if (!candidate.activeInHierarchy)
                {
                    candidate.transform.position = pos;
                    candidate.transform.rotation = rot;
                    candidate.SetActive(true);
                    return candidate;
                }
            }
            // pool exhausted
            return null;
        }
        else
        {
            var go = Instantiate(enemyPrefab, pos, rot);
            AttachReturnOnDeath(go);
            return go;
        }
    }

    // This method is intended to be called by the enemy when it dies / disables
    // Example: spawner.NotifyEnemyDeath(this.gameObject);
    public void NotifyEnemyDeath(GameObject enemy)
    {
        if (aliveEnemies.Contains(enemy))
            aliveEnemies.Remove(enemy);

        // If using pooling, we just deactivate the enemy here.
        if (usePooling && enemy != null)
        {
            enemy.SetActive(false);
        }
        // if not pooling, you might destroy the enemy elsewhere or let it handle itself
    }

    Vector3 CalculateSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform chosen;
            if (randomizeSpawnPoint)
                chosen = spawnPoints[Random.Range(0, spawnPoints.Length)];
            else
            {
                chosen = spawnPoints[nextSpawnPointIndex];
                nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
            }

            return chosen.position;
        }
        else
        {
            // random position inside circle on XZ plane
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(rand.x, 0f, rand.y);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        if (spawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var p in spawnPoints)
            {
                if (p != null)
                    Gizmos.DrawSphere(p.position, 0.25f);
            }
        }
    }
}

/// <summary>
/// Helper that notifies the spawner when the enemy is disabled/died.
/// The enemy prefab should call this component's Notify on death, or simply rely
/// on OnDisable to tell the owner spawner. Adjust to fit your enemy lifecycle.
/// </summary>
public class SpawnerReturn : MonoBehaviour
{
    [HideInInspector] public EnemySpawner owner;

    void OnDisable()
    {
        if (owner != null)
            owner.NotifyEnemyDeath(this.gameObject);
    }

    // If you prefer explicit call from enemy scripts:
    public void NotifyDeath()
    {
        if (owner != null)
            owner.NotifyEnemyDeath(this.gameObject);

        if (owner != null && owner.usePooling)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
