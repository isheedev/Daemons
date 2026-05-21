using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("How much the weapon sways when moving")]
    public float swayAmount = 0.02f;
    [Tooltip("How smooth the sway motion is")]
    public float swaySmoothing = 6f;
    [Tooltip("Maximum sway angle in degrees")]
    public float maxSwayAmount = 5f;
    
    [Header("Bob Settings")]
    [Tooltip("Enable head bob when walking")]
    public bool enableBob = true;
    [Tooltip("How much the weapon bobs up and down")]
    public float bobAmount = 0.05f;
    [Tooltip("How fast the bobbing occurs")]
    public float bobSpeed = 10f;
    
    [Header("Recoil Settings")]
    [Tooltip("How much the weapon kicks back on X-axis (vertical)")]
    public float recoilX = 2f;
    [Tooltip("How much the weapon kicks back on Y-axis (horizontal)")]
    public float recoilY = 0.5f;
    [Tooltip("How much the weapon kicks back on Z-axis (depth)")]
    public float recoilZ = 0.1f;
    [Tooltip("How fast the weapon returns to original position")]
    public float recoilReturnSpeed = 5f;
    [Tooltip("Random recoil variation (0-1)")]
    [Range(0f, 1f)]
    public float recoilRandomness = 0.3f;
    
    [Header("Input Settings")]
    [Tooltip("Key code for shooting (default: Mouse0)")]
    public KeyCode shootKey = KeyCode.Mouse0;
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    private float bobTimer = 0f;
    private Vector3 recoilOffset;
    private Quaternion recoilRotation;

    void Start()
    {
        // Store initial transform
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        targetPosition = initialPosition;
        targetRotation = initialRotation;
    }

    void Update()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Calculate sway from mouse movement
        CalculateSway(mouseX, mouseY);
        
        // Calculate bob from movement
        if (enableBob)
        {
            CalculateBob(moveX, moveY);
        }
        
        // Handle shooting recoil
        if (Input.GetKeyDown(shootKey))
        {
            ApplyRecoil();
        }
        
        // Smoothly return from recoil
        ReturnFromRecoil();
        
        // Apply all transformations
        ApplyTransformations();
    }
    
    void CalculateSway(float mouseX, float mouseY)
    {
        // Calculate sway based on mouse movement
        float swayX = -mouseY * swayAmount;
        float swayY = mouseX * swayAmount;
        
        // Clamp sway
        swayX = Mathf.Clamp(swayX, -maxSwayAmount, maxSwayAmount);
        swayY = Mathf.Clamp(swayY, -maxSwayAmount, maxSwayAmount);
        
        // Apply to target rotation
        Quaternion swayRotation = Quaternion.Euler(swayX, swayY, 0f);
        targetRotation = initialRotation * swayRotation;
    }
    
    void CalculateBob(float moveX, float moveY)
    {
        // Only bob if moving
        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            
            // Calculate bob offset using sine wave
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmount;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
            
            targetPosition = initialPosition + new Vector3(bobOffsetX, bobOffsetY, 0f);
        }
        else
        {
            // Reset bob timer when not moving
            bobTimer = 0f;
            targetPosition = initialPosition;
        }
    }
    
    void ApplyRecoil()
    {
        // Add random variation to recoil
        float randomX = Random.Range(-recoilRandomness, recoilRandomness);
        float randomY = Random.Range(-recoilRandomness, recoilRandomness);
        
        // Calculate recoil rotation
        Vector3 recoilEuler = new Vector3(
            -recoilX + randomX,
            recoilY + randomY,
            0f
        );
        
        recoilRotation = Quaternion.Euler(recoilEuler);
        
        // Calculate recoil position offset (weapon pulls back)
        recoilOffset = new Vector3(0f, 0f, -recoilZ);
    }
    
    void ReturnFromRecoil()
    {
        // Smoothly return rotation to zero
        recoilRotation = Quaternion.Slerp(recoilRotation, Quaternion.identity, Time.deltaTime * recoilReturnSpeed);
        
        // Smoothly return position to zero
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
    }
    
    void ApplyTransformations()
    {
        // Combine all position offsets
        Vector3 finalPosition = targetPosition + recoilOffset;
        
        // Combine all rotations
        Quaternion finalRotation = targetRotation * recoilRotation;
        
        // Smoothly interpolate to final transform
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * swaySmoothing);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation, Time.deltaTime * swaySmoothing);
    }
    
    // Public method to trigger recoil from other scripts (e.g., shooting script)
    public void TriggerRecoil()
    {
        ApplyRecoil();
    }
    
    // Public method to customize recoil amount
    public void TriggerRecoil(float multiplier)
    {
        float originalX = recoilX;
        float originalY = recoilY;
        float originalZ = recoilZ;
        
        recoilX *= multiplier;
        recoilY *= multiplier;
        recoilZ *= multiplier;
        
        ApplyRecoil();
        
        recoilX = originalX;
        recoilY = originalY;
        recoilZ = originalZ;
    }
}
