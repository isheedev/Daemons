using UnityEngine;

public class RetroBulletHandler : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float damage = 25f;
    public float range = 100f;
    public float impactForce = 15f;

    [Header("Prefabs (The 'Slots')")]
    // Drop your Spark or Bullet Hole prefab here
    public GameObject impactPrefab; 
    
    // Optional: Drop a 'Tracer' prefab here (a short-lived line)
    public GameObject tracerPrefab; 
    public Transform muzzleLocation;

    [Header("Detection")]
    public LayerMask hitLayers;
    private Camera playerCam;

    void Start()
    {
        playerCam = Camera.main;
    }

    public void FireBullet()
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));

        // 1. Handle the visual Tracer (Line from gun to infinity/target)
        if (tracerPrefab != null && muzzleLocation != null)
        {
            Instantiate(tracerPrefab, muzzleLocation.position, muzzleLocation.rotation);
        }

        if (Physics.Raycast(rayOrigin, playerCam.transform.forward, out hit, range, hitLayers))
        {
            // 2. Damage the Gore System
            EnemyGoreSpawner enemy = hit.transform.GetComponent<EnemyGoreSpawner>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
            }

            // 3. Physical Impact (Pushing chunks)
            Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(playerCam.transform.forward * impactForce, ForceMode.Impulse);
            }

            // 4. Spawn the Impact Prefab (The spot you were looking for)
            if (impactPrefab != null)
            {
                // Instantiate at the hit point, rotated to face the surface (hit.normal)
                GameObject impact = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                
                // Stick it to the object we hit (so it moves with them)
                impact.transform.SetParent(hit.transform);
                
                Destroy(impact, 2f); // Clean up after 2 seconds
            }
        }
    }
}