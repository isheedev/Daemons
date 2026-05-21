using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 12f;
    public float crouchSpeed = 3f;
    public float slideSpeed = 18f;
    
    [Header("Slide Settings")]
    public float slideForce = 10f;
    public float slideYScale = 0.5f;
    private float startYScale;
    
    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    private Rigidbody rb;
    private float moveSpeed;
    private Vector3 moveDirection;

    // States
    public bool isSprinting;
    public bool isCrouching;
    public bool isSliding;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
    }

    void Update()
    {
        HandleInput();
        SpeedControl();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void HandleInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        moveDirection = transform.forward * z + transform.right * x;

        // Start Crouch or Slide
        if (Input.GetKeyDown(crouchKey))
        {
            if (isSprinting && moveDirection.magnitude > 0)
                StartSlide();
            else
                StartCrouch();
        }

        // Stop Crouch or Slide
        if (Input.GetKeyUp(crouchKey))
        {
            StopCrouch();
        }

        // Check Sprinting
        isSprinting = Input.GetKey(sprintKey) && !isCrouching && !isSliding;
    }

    private void StartCrouch()
    {
        isCrouching = true;
        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); // Push to ground
    }

    private void StopCrouch()
    {
        isCrouching = false;
        isSliding = false;
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
    }

    private void StartSlide()
    {
        isSliding = true;
        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        
        // Add an initial burst of speed for the slide
        rb.AddForce(moveDirection.normalized * slideForce, ForceMode.Impulse);
    }

    private void MovePlayer()
    {
        // Set speed based on state
        if (isSliding) moveSpeed = slideSpeed;
        else if (isCrouching) moveSpeed = crouchSpeed;
        else if (isSprinting) moveSpeed = sprintSpeed;
        else moveSpeed = walkSpeed;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit velocity if it goes too high
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
        
        // End slide if player stops moving
        if (isSliding && flatVel.magnitude < walkSpeed)
        {
            isSliding = false;
        }
    }
}