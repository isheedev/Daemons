// MomentumController.cs
// Author: You
// License: MIT – do whatever, just keep this header.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class UltraKillMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraT;
    [SerializeField] CapsuleCollider capsule;
    Rigidbody rb;

    [Header("Speed Graph (Realtime)")]
    [SerializeField] AnimationCurve speedGraph = AnimationCurve.EaseInOut(0, 5, 1, 15);
    [SerializeField] float maxSpeed = 40f;          // hard cap
    [SerializeField] float maxWallSpeed = 25f;

    [Header("Ground")]
    [SerializeField] float groundAccel = 90f;
    [SerializeField] float groundFriction = 20f;
    [SerializeField] float slideBoost = 35f;

    [Header("Air")]
    [SerializeField] float airAccel = 55f;
    [SerializeField] float airCap = 0.35f;          // air-control % vs ground
    [SerializeField] float jumpVel = 10f;
    [SerializeField] float crouchJumpMult = 1.25f;

    [Header("Dash")]
    [SerializeField] int   maxDashes = 3;
    [SerializeField] float dashSpeed = 28f;
    [SerializeField] float dashTime  = .18f;
    [SerializeField] float dashExitMultiplier = 0.85f;

    [Header("Slide")]
    [SerializeField] float slideFriction = 4f;
    [SerializeField] float slideCooldown = .3f;
    [SerializeField] float slideBoostThreshold = 12f;

    [Header("Wall-Run")]
    [SerializeField] float wallRunAccel = 30f;
    [SerializeField] float wallRunJumpUp = 7f;
    [SerializeField] float wallRunJumpSide = 12f;
    [SerializeField] float wallRunTimeMax = 2.5f;
    [SerializeField] float wallStickTime = .2f;

    [Header("Ledge")]
    [SerializeField] float ledgeGrabRange = 1f;
    [SerializeField] float ledgeOffsetUp = .5f;
    [SerializeField] float ledgeOffsetForward = .3f;

    [Header("Coyote & Buffer")]
    [SerializeField] float coyoteTime = .12f;
    [SerializeField] float jumpBuffer = .10f;

    /* ——— State ——— */
    bool grounded, wasGrounded;
    float coyoteCounter, jumpBufferCounter;
    Vector3 wishDir;
    float slideTimer, wallRunTimer, ledgeTimer;
    int dashesLeft;
    bool sliding, wallRunning, ledgeGrabbing, dashing;
    Vector3 lastWallNormal;
    Vector3 savedVelocity; // for momentum conservation across states

    /* ——— Input ——— */
    struct FrameInput
    {
        public Vector2 move;
        public bool jump, crouch, dash;
    }
    FrameInput inp;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        dashesLeft = maxDashes;
    }

    void Update() => GatherInput();
    void FixedUpdate()
    {
        CheckGround();
        HandleTimers();
        CalculateWishDir();
        HandleLedge();
        if (ledgeGrabbing) return;

        HandleWallRun();
        if (wallRunning) return;

        HandleDash();
        if (dashing) return;

        HandleSlide();
        HandleJump();
        HandleFriction();
        HandleAcceleration();
        ClampSpeed();
    }

    #region Input
    void GatherInput()
    {
        inp.move   = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        inp.jump   = Input.GetKeyDown(KeyCode.Space);
        inp.crouch = Input.GetKey(KeyCode.LeftControl);
        inp.dash   = Input.GetKeyDown(KeyCode.LeftShift);

        if (inp.jump) jumpBufferCounter = jumpBuffer;
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;
    }
    #endregion

    #region Timers
    void HandleTimers()
    {
        if (coyoteCounter > 0f)
            coyoteCounter -= Time.fixedDeltaTime;
    }
    #endregion

    #region Ground
    void CheckGround()
    {
        wasGrounded = grounded;
        Vector3 basePos = transform.position + Vector3.up * 0.05f;
        grounded = Physics.Raycast(basePos, Vector3.down, capsule.height * .5f + .05f + 0.05f, ~0, QueryTriggerInteraction.Ignore);

        if (grounded && !wasGrounded)
        {
            dashesLeft = maxDashes;
            slideTimer = 0;
            if (savedVelocity.magnitude > slideBoostThreshold && inp.crouch)
                EnterSlide();
        }

        if (!grounded && wasGrounded) coyoteCounter = coyoteTime;
        if (grounded) coyoteCounter = 0;
    }

    void HandleFriction()
    {
        float friction = sliding ? slideFriction : (grounded ? groundFriction : 0);
        if (friction > 0)
        {
            Vector3 pv = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
            float speed = pv.magnitude;
            if (speed != 0)
            {
                float drop = speed * friction * Time.fixedDeltaTime;
                float newspeed = Mathf.Max(speed - drop, 0);
                rb.velocity = (pv * newspeed / speed) + Vector3.up * rb.velocity.y;
            }
        }
    }
    #endregion

    #region Wish
    void CalculateWishDir()
    {
        Vector3 right = cameraT.right;
        Vector3 forward = cameraT.forward;
        right.y = 0; forward.y = 0;
        right.Normalize(); forward.Normalize();
        wishDir = (right * inp.move.x + forward * inp.move.y).normalized;
    }
    #endregion

    #region Acceleration
    void HandleAcceleration()
    {
        float accel = grounded ? groundAccel : airAccel;
        if (sliding) accel *= 1.4f;
        if (wallRunning) accel = wallRunAccel;

        Vector3 current = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
        float proj = Vector3.Dot(current, wishDir);
        float addspeed = accel * Time.fixedDeltaTime;

        float accelSpeed = Mathf.Min(addspeed, maxSpeed - proj);
        if (proj < 0) accelSpeed = addspeed;

        Vector3 acc = wishDir * accelSpeed;
        if (!grounded) acc *= airCap;
        rb.velocity += acc;
    }

    void ClampSpeed()
    {
        float max = maxSpeed;
        if (wallRunning) max = maxWallSpeed;

        Vector3 h = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
        float spd = h.magnitude;
        if (spd > max)
            rb.velocity = h * (max / spd) + Vector3.up * rb.velocity.y;
    }
    #endregion

    #region Jump
    void HandleJump()
    {
        bool canJump = (grounded || coyoteCounter > 0) && !inp.crouch;
        if (jumpBufferCounter > 0 && canJump)
        {
            jumpBufferCounter = 0;
            coyoteCounter = 0;
            float mult = inp.crouch ? crouchJumpMult : 1f;
            rb.velocity = new Vector3(rb.velocity.x, jumpVel * mult, rb.velocity.z);
        }

        if (wallRunning && inp.jump)
        {
            ExitWallRun();
            Vector3 jumpDir = (lastWallNormal + Vector3.up).normalized;
            rb.velocity = jumpDir * wallRunJumpSide + Vector3.up * wallRunJumpUp;
        }
    }
    #endregion

    #region Slide
    void EnterSlide()
    {
        sliding = true;
        slideTimer = slideCooldown;
        rb.velocity += wishDir * slideBoost;
    }

    void HandleSlide()
    {
        if (inp.crouch && !sliding && grounded && rb.velocity.magnitude > slideBoostThreshold)
            EnterSlide();

        if (sliding && slideTimer > 0)
            slideTimer -= Time.fixedDeltaTime;
        else if (sliding && (!inp.crouch || !grounded))
            sliding = false;
    }
    #endregion

    #region Dash
    void HandleDash()
    {
        if (inp.dash && dashesLeft > 0 && !dashing)
        {
            dashesLeft--;
            StartCoroutine(DashRoutine());
        }
    }

    IEnumerator DashRoutine()
    {
        dashing = true;
        Vector3 dir = wishDir != Vector3.zero ? wishDir : cameraT.forward;
        Vector3 start = rb.velocity;
        Vector3 end   = dir * dashSpeed;
        float t = 0;
        while (t < dashTime)
        {
            t += Time.fixedDeltaTime;
            float a = t / dashTime;
            rb.velocity = Vector3.Lerp(start, end, a);
            yield return new WaitForFixedUpdate();
        }
        rb.velocity = end * dashExitMultiplier;
        dashing = false;
    }
    #endregion

    #region Wall-Run
    void HandleWallRun()
    {
        if (grounded) { wallRunning = false; return; }
        if (!inp.crouch && !wallRunning)
        {
            if (Physics.Raycast(transform.position, cameraT.right, 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                lastWallNormal = -cameraT.right;
                EnterWallRun();
            }
            else if (Physics.Raycast(transform.position, -cameraT.right, 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                lastWallNormal = cameraT.right;
                EnterWallRun();
            }
        }

        if (wallRunning)
        {
            wallRunTimer += Time.fixedDeltaTime;
            if (wallRunTimer > wallRunTimeMax || inp.crouch)
                ExitWallRun();
            else
            {
                rb.useGravity = false;
                Vector3 along = Vector3.Cross(lastWallNormal, Vector3.up);
                rb.velocity = Vector3.Project(rb.velocity, along) + Vector3.up * Mathf.Max(rb.velocity.y, 0);
            }
        }
    }

    void EnterWallRun()
    {
        if (wallRunning) return;
        wallRunning = true;
        wallRunTimer = 0;
        dashesLeft = maxDashes;
    }

    void ExitWallRun()
    {
        wallRunning = false;
        rb.useGravity = true;
    }
    #endregion

    #region Ledge
    void HandleLedge()
    {
        if (grounded || ledgeGrabbing) return;
        Vector3 check = transform.position + Vector3.up * (capsule.height * .5f - ledgeGrabRange);
        if (Physics.Raycast(check, cameraT.forward, ledgeGrabRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 ledgePos = transform.position + cameraT.forward * ledgeOffsetForward + Vector3.up * ledgeOffsetUp;
            transform.position = ledgePos;
            rb.velocity = Vector3.zero;
            ledgeGrabbing = true;
            Invoke(nameof(ExitLedge), .2f);
        }
    }

    void ExitLedge() => ledgeGrabbing = false;
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * capsule.height * .5f, .1f);
    }
    #endregion
}