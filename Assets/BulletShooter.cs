using UnityEngine;

public class BulletShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("The bullet prefab to spawn")]
    public GameObject bulletPrefab;
    
    [Tooltip("Speed at which bullets travel")]
    public float bulletSpeed = 50f;
    
    [Tooltip("How long bullets live before being destroyed")]
    public float bulletLifetime = 5f;
    
    [Header("Raycast Settings")]
    [Tooltip("Maximum distance for the raycast")]
    public float maxRaycastDistance = 1000f;
    
    [Tooltip("Layers the raycast can hit")]
    public LayerMask raycastLayers = ~0; // Everything by default
    
    [Header("Spawn Settings")]
    [Tooltip("Transform where bullets spawn from (e.g., gun muzzle). If null, uses this transform.")]
    public Transform spawnPoint;
    
    [Tooltip("Camera to use for aiming. If null, uses Camera.main.")]
    public Camera aimCamera;
    
    [Header("Input Settings")]
    [Tooltip("Key to fire bullets")]
    public KeyCode fireKey = KeyCode.Mouse0;
    
    [Tooltip("Allow holding down fire key for automatic fire")]
    public bool automaticFire = false;
    
    [Tooltip("Fire rate in shots per second (only used if automaticFire is true)")]
    public float fireRate = 10f;
    
    [Header("Optional References")]
    [Tooltip("WeaponSway script to trigger recoil on fire")]
    public WeaponSway weaponSway;
    
    private float nextFireTime = 0f;

    void Start()
    {
        // Default to main camera if none assigned
        if (aimCamera == null)
            aimCamera = Camera.main;
        
        // Default to this transform if no spawn point assigned
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    void Update()
    {
        bool shouldFire = false;
        
        if (automaticFire)
        {
            // Automatic fire - check if key is held and enough time has passed
            shouldFire = Input.GetKey(fireKey) && Time.time >= nextFireTime;
        }
        else
        {
            // Semi-automatic - only fire on key down
            shouldFire = Input.GetKeyDown(fireKey);
        }
        
        if (shouldFire)
        {
            FireBullet();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }
    
    public void FireBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("BulletShooter: No bullet prefab assigned!");
            return;
        }
        
        // Cast a ray from the center of the screen
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Determine the target point - either where the ray hits or a far point
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, raycastLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            // If nothing hit, aim at a far point along the ray
            targetPoint = ray.origin + ray.direction * maxRaycastDistance;
        }
        
        // Calculate direction from spawn point to target
        Vector3 bulletDirection = (targetPoint - spawnPoint.position).normalized;
        
        // Spawn the bullet at the spawn point, facing the target direction
        GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.LookRotation(bulletDirection));
        
        // Try to get a Rigidbody and set velocity
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bulletDirection * bulletSpeed;
        }
        else
        {
            // If no Rigidbody, add a Bullet component to handle movement
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent == null)
            {
                bulletComponent = bullet.AddComponent<Bullet>();
            }
            bulletComponent.Initialize(bulletDirection, bulletSpeed);
        }
        
        // Destroy the bullet after its lifetime
        Destroy(bullet, bulletLifetime);
        
        // Trigger recoil if WeaponSway is assigned
        if (weaponSway != null)
        {
            weaponSway.TriggerRecoil();
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;
            
        if (aimCamera != null)
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Gizmos.color = Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * maxRaycastDistance);
            
            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
            }
        }
    }
}

/// <summary>
/// Simple bullet movement component for bullets without Rigidbody
/// </summary>
public class Bullet : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private bool initialized = false;
    
    public void Initialize(Vector3 dir, float spd)
    {
        direction = dir;
        speed = spd;
        initialized = true;
    }
    
    void Update()
    {
        if (initialized)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}
