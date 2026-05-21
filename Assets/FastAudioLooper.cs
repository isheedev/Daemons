using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FastAudioLooper : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float m_LoopInterval = 0.05f; 
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip m_Clip;
    [Range(0f, 1f)] [SerializeField] private float m_Volume = 0.5f;

    [Header("Pitch Variation")]
    [SerializeField] private float m_BasePitch = 1.0f;
    [SerializeField] private float m_PitchRange = 0.2f; // Variations of +/- 0.2

    private AudioSource m_Source;
    private float m_Timer;

    private void Start()
    {
        m_Source = GetComponent<AudioSource>();
        // Ensure the source isn't trying to play its own clip automatically
        m_Source.playOnAwake = false;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0)) 
        {
            m_Timer += Time.deltaTime;

            if (m_Timer >= m_LoopInterval)
            {
                m_Timer = 0f;
                PlayGlitchSound();
            }
        }
        else
        {
            // Reset timer so it clicks immediately when you press again
            m_Timer = m_LoopInterval;
        }
    }

    private void PlayGlitchSound()
    {
        if (m_Clip == null) return;

        // Apply random pitch for the 'yes' part of your request
        float randomPitch = m_BasePitch + Random.Range(-m_PitchRange, m_PitchRange);
        m_Source.pitch = randomPitch;

        // PlayOneShot allows sounds to overlap, preventing the "silence" bug
        m_Source.PlayOneShot(m_Clip, m_Volume);
    }
}