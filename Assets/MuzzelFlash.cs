using UnityEngine;

public class GunController : MonoBehaviour
{
    public GameObject muzzleFlash; // Assign in inspector
    
    void Start()
    {
        // Start with muzzle flash hidden
        muzzleFlash.SetActive(false);
    }
    
    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // Or your firing input
        {
            Shoot();
        }
    }
    
    void Shoot()
    {
        // Show muzzle flash
        muzzleFlash.SetActive(true);
        
        // Hide after short delay
        Invoke("HideMuzzleFlash", 0.2f); // Adjust time as needed
        
        // Your shooting logic here
    }
    
    void HideMuzzleFlash()
    {
        muzzleFlash.SetActive(false);
    }
}