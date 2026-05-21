using UnityEngine;

public class Ak47 : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Stats")]
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 10f; // Rounds per second

    private float nextTimeToFire = 0f;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        // GetMouseButton(0) returns true every frame the Left Mouse Button is held down.
        // Change (0) to (1) if you specifically meant Right Mouse Button.
        if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (playerCamera == null) return;

        RaycastHit hit;
        // Casts a ray from the center of the camera viewport
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            EnemyGoreSpawner enemy = hit.transform.GetComponentInParent<EnemyGoreSpawner>();
            
            if (enemy != null)
            {
                enemy.TakeDamage(Mathf.RoundToInt(damage));
            }
        }
    }
}