using UnityEngine;

/// Handles sound effect playback for pitch-mapped musical notes, 
/// wrong step penalties, level complete stingers, and game over audio.

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Note Audio Clips")]
    public AudioClip[] noteClips;

    [Header("Feedback Audio Clips")]
    public AudioClip wrongStepClip;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip gameOverSound;

    void Awake()
    {
        Instance = this;
    }

    // =========================================================================
    // AUDIO PLAYBACK METHODS
    // =========================================================================

    public void PlayNote(int noteIndex)
    {
        if (noteClips != null && noteIndex >= 0 && noteIndex < noteClips.Length)
        {
            audioSource.pitch = 1.0f;
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
    public void PlayLevelCompleteSound()
    {
        if (levelCompleteSound != null)
        {
            audioSource.PlayOneShot(levelCompleteSound, 2f);
        }
    }
    public void PlayGameOverSound()
    {
        if (gameOverSound != null)
        {
            audioSource.PlayOneShot(gameOverSound, 2f);
        }
    }
}