using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource audioSource;
    public AudioClip[] noteClips;
    public AudioClip wrongStepClip;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private AudioClip gameOverSound;

    void Awake()
    {
        Instance = this;
    }

    // Plays the specific note by index (0 to 12)
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