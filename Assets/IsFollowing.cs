using UnityEngine;
using UnityEngine.AI;

public class FollowerAI : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("If left empty, will automatically find the spawned player at runtime")]
    public Transform target;
    
    [Header("Follow Settings")]
    public float updateInterval = 0.2f;  // How often to recalculate path
    public float stoppingDistance = 1.5f; // How close before stopping
    public float findPlayerRetryInterval = 0.5f; // How often to retry finding player
    
    [Header("Detection")]
    [Tooltip("Enemy will only chase when player is within this radius. Set to 0 for infinite range.")]
    public float detectionRadius = 15f;
    
    [Header("Model Orientation")]
    [Tooltip("Y-axis rotation offset in degrees. Adjust if enemy faces sideways (e.g., 90 or -90)")]
    [Range(-180f, 180f)]
    public float rotationYOffset = 0f;
    
    [Header("Runtime Detection")]
    [Tooltip("Only follow players spawned after scene start (ignores pre-existing scene objects)")]
    public bool onlyFollowRuntimeSpawnedPlayer = true;
    
    private NavMeshAgent agent;
    private float nextUpdateTime;
    private float nextFindPlayerTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError("FollowerAI requires a NavMeshAgent component!", this);
            enabled = false;
            return;
        }
        
        agent.stoppingDistance = stoppingDistance;
        
        // Clear any pre-assigned target if we only want runtime-spawned players
        if (onlyFollowRuntimeSpawnedPlayer)
        {
            target = null;
        }
        
        // Don't try to find player immediately - wait for spawn to complete
        nextFindPlayerTime = Time.time + 0.1f;
    }

    void Update()
    {
        // If no target, keep trying to find the runtime-spawned player
        if (target == null)
        {
            if (Time.time >= nextFindPlayerTime)
            {
                nextFindPlayerTime = Time.time + findPlayerRetryInterval;
                FindRuntimePlayer();
            }
            return;
        }

        // Check if player is within detection radius
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        bool playerInRange = detectionRadius <= 0f || distanceToTarget <= detectionRadius;
        
        if (!playerInRange)
        {
            // Stop moving if player is out of range
            if (agent.isOnNavMesh && agent.hasPath)
            {
                agent.ResetPath();
            }
            return;
        }

        // Only update destination periodically for performance
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(target.position);
            }
        }
        
        // Apply rotation offset to correct model orientation
        ApplyRotationOffset();
    }
    
    void ApplyRotationOffset()
    {
        if (target == null || !agent.isOnNavMesh) return;
        
        // Get direction to target
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f; // Keep rotation on Y-axis only
        
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            // Calculate look rotation and apply offset
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            Quaternion offsetRotation = Quaternion.Euler(0f, rotationYOffset, 0f);
            transform.rotation = lookRotation * offsetRotation;
        }
    }

    void FindRuntimePlayer()
    {
        // ONLY look for the spawned player by name
        // This is the object spawned by FPSDungeonGenerator.GeneratePlayerSpawn()
        // This ensures we ignore any pre-existing player objects in the scene
        GameObject spawnedPlayer = GameObject.Find("Player_Spawned");
        if (spawnedPlayer != null)
        {
            // Make sure it's actually in the scene (not a prefab in assets) and active
            if (spawnedPlayer.scene.IsValid() && spawnedPlayer.activeInHierarchy)
            {
                target = spawnedPlayer.transform;
                Debug.Log($"FollowerAI found runtime player: {spawnedPlayer.name}");
                return;
            }
        }
        
        // If onlyFollowRuntimeSpawnedPlayer is false, fall back to other detection methods
        if (!onlyFollowRuntimeSpawnedPlayer)
        {
            // Fallback: Find by "Player" tag
            GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject player in taggedPlayers)
            {
                if (player.scene.IsValid() && player.activeInHierarchy)
                {
                    target = player.transform;
                    Debug.Log($"FollowerAI found tagged player: {player.name}");
                    return;
                }
            }
            
            // Fallback: Look for CharacterController
            CharacterController[] controllers = FindObjectsOfType<CharacterController>();
            foreach (CharacterController controller in controllers)
            {
                if (controller.gameObject.scene.IsValid() && controller.gameObject.activeInHierarchy)
                {
                    target = controller.transform;
                    Debug.Log($"FollowerAI found player via CharacterController: {controller.name}");
                    return;
                }
            }
        }
    }
    
    // Call this method to manually set the target (useful from other scripts)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    void OnDrawGizmosSelected()
    {
        // Show stopping distance in yellow
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        // Show detection radius in red
        if (detectionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
