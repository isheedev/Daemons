using UnityEngine;

public class recoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] private Vector3 recoilRotation = new Vector3(-10, 5, 0); // How much it kicks
    [SerializeField] private float snappiness = 10f; // How fast it kicks back
    [SerializeField] private float returnSpeed = 5f; // How fast it returns to center

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    void Update()
    {
        // Slowly move the target rotation back to zero (rest position)
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        
        // Smoothly transition the current rotation toward the target rotation
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);
        
        // Apply the rotation to the transform
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void FireRecoil()
    {
        // Add the recoil force to our target
        targetRotation += new Vector3(recoilRotation.x, Random.Range(-recoilRotation.y, recoilRotation.y), Random.Range(-recoilRotation.z, recoilRotation.z));
    }
}