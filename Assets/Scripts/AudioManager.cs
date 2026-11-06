using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource audioSource;
    public AudioClip[] noteClips; // Array of 13 note clips (C1 to C2)
    public AudioClip wrongStepClip; // buzzer/error sound

    void Awake()
    {
        Instance = this;
    }

    // Plays the specific note by index (0 to 12)
    public void PlayNote(int noteIndex)
    {
        if (noteClips != null && noteIndex >= 0 && noteIndex < noteClips.Length)
        {
            audioSource.pitch = 1.0f; // Reset pitch to normal
            audioSource.PlayOneShot(noteClips[noteIndex]);
        }
    }

    public void PlayWrongStep()
    {
        if (wrongStepClip != null)
        {
            audioSource.PlayOneShot(wrongStepClip, 2f);
        }
    }
}