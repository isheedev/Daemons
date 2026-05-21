using UnityEngine;

public class DoubleBarrelShotgun : MonoBehaviour
{
    public Camera playerCamera;
    public float damage = 25f;   // Damage per pellet
    public float range = 50f;

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // Left click
        {
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            Debug.Log("Hit: " + hit.transform.name);

            // Look for EnemyGoreSpawner (this is your enemy health + gore logic)
            EnemyGoreSpawner enemy = hit.transform.GetComponentInParent<EnemyGoreSpawner>();
            if (enemy != null)
            {
                enemy.TakeDamage((int)damage);
            }
        }
    }
}
