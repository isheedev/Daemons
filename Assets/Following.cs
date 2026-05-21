using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Following : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    [Tooltip("Higher values make the agent start/stop quicker, reducing 'drift'.")]
    public float acceleration = 60f; // Default high for snappy movement
    public float rotationSpeed = 5f;
    public float stoppingDistance = 1.5f;

    [Header("Facing Settings")]
    [Tooltip("If your model faces -Z instead of +Z (e.g. Mixamo/Blender)")]
    public bool invertForward = false;
    [Tooltip("Fine-tune facing angle")]
    [Range(-180f, 180f)] public float rotationOffset = 0f;
    [Tooltip("Should the enemy look at the player when stopped?")]
    public bool facePlayerWhenStopped = true;

    [Header("Optional Model Reference")]
    [Tooltip("Assign your visual model here if it's a child object.")]
    public Transform modelTransform;

    private Transform player;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.speed = moveSpeed;
        agent.acceleration = acceleration; // Apply the snappy acceleration
        agent.angularSpeed = 0f; // Disable agent's internal rotation so we can control it
        agent.updateRotation = false; // Double ensure agent doesn't rotate model
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            // Fallback: some generators name the player object "Player_Spawned"
            playerObject = GameObject.Find("Player_Spawned");
            if (playerObject != null)
                Debug.Log("Following: found player by name 'Player_Spawned' as fallback.");
        }

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogError("Player not found! Tag your player as 'Player' or name it 'Player_Spawned'.");
    }

    private void Update()
    {
        if (player == null) return;

        // --- 1. Movement Logic ---
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        bool usingNav = agent != null && agent.isOnNavMesh;

        if (distanceToPlayer > stoppingDistance)
        {
            if (usingNav)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // Fallback kinematic movement when NavMesh is not available (e.g., runtime build failed)
                Vector3 targetPos = new Vector3(player.position.x, transform.position.y, player.position.z);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (usingNav) agent.isStopped = true;
        }

        // --- 2. Facing Logic ---
        Vector3 targetDirection = Vector3.zero;

        // Condition A: If using NavMesh and moving fast enough, look where we are going (prevents moonwalking)
        if (agent != null && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.1f)
        {
            targetDirection = agent.velocity;
        }
        // Condition B: If not using NavMesh but moving kinematically, look towards the player
        else if ((!agent.isOnNavMesh) && (distanceToPlayer > 0.01f))
        {
            targetDirection = player.position - transform.position;
        }
        // Condition C: If stopped (or moving very slowly) and allowed, look at player
        else if (facePlayerWhenStopped)
        {
            targetDirection = player.position - transform.position;
        }

        // Apply Rotation if we have a valid direction
        if (targetDirection.sqrMagnitude > 0.001f)
        {
            RotateTowards(targetDirection);
        }
    }

    private void RotateTowards(Vector3 dir)
    {
        dir.y = 0; // Keep rotation flat on the ground
        if (dir == Vector3.zero) return;

        // Calculate rotation based on direction
        Vector3 facing = invertForward ? -dir.normalized : dir.normalized;
        Quaternion lookRotation = Quaternion.LookRotation(facing);
        
        // Add the optional offset
        Quaternion targetRot = lookRotation * Quaternion.Euler(0, rotationOffset, 0);

        // Smoothly rotate
        Transform target = modelTransform != null ? modelTransform : transform;
        target.rotation = Quaternion.Slerp(target.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        Gizmos.color = Color.cyan;
        Transform target = modelTransform != null ? modelTransform : transform;
        Vector3 forward = invertForward ? -target.forward : target.forward;
        Gizmos.DrawLine(target.position, target.position + forward * 2);
    }
}