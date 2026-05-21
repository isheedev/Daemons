using UnityEngine;

public class GunShooting : MonoBehaviour
{
    [Header("References")]
    public GameObject lowPolyBulletPrefab;
    public Transform bulletSpawnPoint;
    public Animator gunAnimator; // Add this reference
    
    [Header("Settings")]
    public float bulletSpeed = 50f;
    public float bulletLifetime = 3f;
    public float fireRate = 0.1f;
    
    private float nextFireTime = 0f;
    private bool isFiring = false; 

    void Update()
    {
        if (Input.GetButton("Fire2"))
        {
            isFiring = true;
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            isFiring = false;
        }
    }

    void Shoot()
    {
        if (lowPolyBulletPrefab == null || bulletSpawnPoint == null) return;

        // --- THE ANIMATOR FIX ---
        if (gunAnimator != null)
        {
            // This matches the "TrShoot" parameter in your screenshot
            gunAnimator.SetTrigger("TrShoot"); 
        }

        GameObject bullet = Instantiate(lowPolyBulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb == null) bulletRb = bullet.AddComponent<Rigidbody>();
        
        bulletRb.useGravity = false;
        bulletRb.velocity = bulletSpawnPoint.forward * bulletSpeed;

        Destroy(bullet, bulletLifetime);
    }
}