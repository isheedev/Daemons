using UnityEngine;

public class Muzzel : MonoBehaviour
{
    [Header("Flash Settings")]
    public GameObject flashHolder; // Parent object for the flash sprite
    public SpriteRenderer flashSpriteRenderer;
    public Sprite[] flashSprites; // Array of possible flash sprites
    public float flashTime = 0.1f; // How long the flash stays visible

    [Header("Random Rotation")]
    public bool randomRotation = true; // Should the flash rotate randomly?

    [Header("Size Variation")]
    public float minSize = 0.8f;
    public float maxSize = 1.2f;

    private float flashTimer;

    void Start()
    {
        // Deactivate the flash holder at start
        flashHolder.SetActive(false);
    }

    void Update()
    {
        // Countdown the timer when active
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
        }
        else if (flashHolder.activeInHierarchy)
        {
            flashHolder.SetActive(false);
        }
    }

    public void ActivateFlash()
    {
        // Activate the flash holder
        flashHolder.SetActive(true);

        // Set a random flash sprite if array has entries
        if (flashSprites != null && flashSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, flashSprites.Length);
            flashSpriteRenderer.sprite = flashSprites[randomIndex];
        }

        // Random rotation if enabled
        if (randomRotation)
        {
            float zRotation = Random.Range(0f, 360f);
            flashHolder.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        // Random size if enabled
        float randomSize = Random.Range(minSize, maxSize);
        flashHolder.transform.localScale = new Vector3(randomSize, randomSize, randomSize);

        // Reset the timer
        flashTimer = flashTime;
    }
}