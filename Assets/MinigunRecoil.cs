using UnityEngine;

[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource is always attached
public class MinigunRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float recoilX = 0.5f;
    public float recoilY = 0.3f;
    public float recoilZ = 0.05f;
    public float fireRate = 600f;
    [Range(0f, 2f)]
    public float chaosMultiplier = 1.5f;
    public float restoreSpeed = 5f;

    [Header("Sway Settings")]
    public float swayAmount = 0.05f;
    public float swaySpeed = 10f;

    [Header("Input & Animation")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public string firingBoolName = "TrShoot";
    public Animator weaponAnimator;

    [Header("Audio")]
    public AudioClip fireSound;
    [Range(0, 1)] public float volume = 0.5f;
    [Tooltip("If true, the sound loops while holding the trigger. If false, it plays every shot.")]
    public bool useLoopingSound = true;

    [Header("Debug")]
    public bool showDebug = false;

    [Header("Muzzle Flash")]
    public GameObject muzzleFlash;
    public float flashInterval = 0.05f;
    public float muzzleFlashRotationSpeed = 720f;

    // Internal Variables
    Vector3 initialPosition;
    Quaternion initialRotation;
    Vector3 recoilPosition;
    Quaternion recoilRotation = Quaternion.identity;
    Vector3 swayOffset;
    float swayTimer;
    float nextFireTime;
    float nextFlashTime;
    bool flashState;
    bool wasFiring = false;

    AudioSource audioSource;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = useLoopingSound;
        audioSource.clip = fireSound;
        audioSource.volume = volume;

        if (!weaponAnimator)
            weaponAnimator = GetComponent<Animator>();

        if (muzzleFlash)
            muzzleFlash.SetActive(false);
    }

    void Update()
    {
        bool isFiring = Input.GetKey(fireKey);

        HandleAudio(isFiring);
        HandleAnimation(isFiring);
        HandleFiring(isFiring);
        ApplyWalkingSway();
        RecoverRecoil();
        ApplyFinalTransform();

        wasFiring = isFiring;
    }

    void HandleAudio(bool isFiring)
    {
        if (!fireSound) return;

        if (useLoopingSound)
        {
            // Start looping when trigger is pressed
            if (isFiring && !wasFiring)
            {
                audioSource.Play();
            }
            // Stop looping when trigger is released
            else if (!isFiring && wasFiring)
            {
                audioSource.Stop();
            }
        }
        // If not looping, the sound is triggered inside ApplyChaoticRecoil() per shot
    }

    void HandleFiring(bool isFiring)
    {
        if (!isFiring)
        {
            if (muzzleFlash) muzzleFlash.SetActive(false);
            return;
        }

        if (Time.time >= nextFireTime)
        {
            ApplyChaoticRecoil();
            
            // Play one-shot sound if not using the looping method
            if (!useLoopingSound && fireSound)
            {
                audioSource.PlayOneShot(fireSound, volume);
            }

            nextFireTime = Time.time + (60f / fireRate);
        }

        HandleMuzzleFlash();
    }

    // ... [Rest of your existing ApplyChaoticRecoil, RecoverRecoil, etc. remains the same] ...
    
    void ApplyChaoticRecoil()
    {
        float randX = Random.Range(-chaosMultiplier, chaosMultiplier);
        float randY = Random.Range(-chaosMultiplier, chaosMultiplier);
        float randZ = Random.Range(0.5f, chaosMultiplier);

        recoilPosition += new Vector3(randX * 0.01f, randY * 0.01f, -recoilZ * randZ);
        Vector3 recoilEuler = new Vector3(-recoilX * (1f + randX), recoilY * randY, 0f);
        recoilRotation *= Quaternion.Euler(recoilEuler);
    }

    void RecoverRecoil()
    {
        recoilPosition = Vector3.Lerp(recoilPosition, Vector3.zero, Time.deltaTime * restoreSpeed);
        recoilRotation = Quaternion.Slerp(recoilRotation, Quaternion.identity, Time.deltaTime * restoreSpeed);
    }

    void HandleMuzzleFlash()
    {
        if (!muzzleFlash) return;

        if (Time.time >= nextFlashTime)
        {
            flashState = !flashState;
            muzzleFlash.SetActive(flashState);
            nextFlashTime = Time.time + flashInterval;
        }

        muzzleFlash.transform.Rotate(Vector3.forward, muzzleFlashRotationSpeed * Time.deltaTime, Space.Self);
    }

    void ApplyWalkingSway()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            swayTimer += Time.deltaTime * swaySpeed;
            float x = Mathf.Cos(swayTimer * 0.5f) * swayAmount;
            float y = Mathf.Sin(swayTimer) * swayAmount;
            swayOffset = new Vector3(x, y, 0f);
        }
        else
        {
            swayTimer = 0f;
            swayOffset = Vector3.Lerp(swayOffset, Vector3.zero, Time.deltaTime * restoreSpeed);
        }
    }

    void ApplyFinalTransform()
    {
        transform.localPosition = initialPosition + swayOffset + recoilPosition;
        transform.localRotation = initialRotation * recoilRotation;
    }

    void HandleAnimation(bool isFiring)
    {
        if (!weaponAnimator) return;
        if (HasParameter(firingBoolName))
            weaponAnimator.SetBool(firingBoolName, isFiring);
    }

    bool HasParameter(string paramName)
    {
        if (!weaponAnimator || !weaponAnimator.isInitialized) return false;
        foreach (AnimatorControllerParameter param in weaponAnimator.parameters)
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }
}