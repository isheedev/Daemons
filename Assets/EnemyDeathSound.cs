using UnityEngine;

public class EnemyDeathSound : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    [Header("Pitch Randomization")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    /// <summary>
    /// Call this method when the enemy's health reaches zero.
    /// </summary>
    public void PlayDeathSound()
    {
        if (deathClip == null)
        {
            Debug.LogWarning("No death clip assigned to " + gameObject.name);
            return;
        }

        // 1. Create a temporary game object to play the sound at the enemy's position
        // This ensures the sound plays even if the enemy is Destroy()'d immediately.
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = transform.position;

        // 2. Add an AudioSource and configure it
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = deathClip;
        aSource.volume = volume;

        // 3. Apply the random pitch
        aSource.pitch = Random.Range(minPitch, maxPitch);

        // 4. Play and set to destroy after the clip finishes
        aSource.Play();
        Destroy(tempGO, deathClip.length);
    }
}