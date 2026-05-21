using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public AudioClip shootClip;
    public AudioClip reloadClip;  // Added separate reload sound
    private AudioSource source;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Update()
    {
        if (animator != null)
        {
            // Shoot on left mouse click
            if (Input.GetMouseButtonDown(0))
            {
                if (shootClip != null)
                {
                    source.PlayOneShot(shootClip);
                }
                Debug.Log("Shooting");
                animator.SetTrigger("TrShoot");
            }

            // Reload on R key press
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (reloadClip != null)
                {
                    source.PlayOneShot(reloadClip);
                }
                Debug.Log("Reloading");
                animator.SetTrigger("TrReload");
            }
        }
    }
}