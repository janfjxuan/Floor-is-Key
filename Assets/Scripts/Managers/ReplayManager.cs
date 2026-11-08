using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Persists and plays back player step history upon game over or level complete.

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance;

    private static List<int> recordedNotes = new List<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void RecordStep(int noteIndex, bool isLava, float timeStamp)
    {
        if (!isLava && noteIndex >= 0)
        {
            recordedNotes.Add(noteIndex);
        }
    }

    public void ClearRecording()
    {
        recordedNotes.Clear();
    }

    public void StartReplay()
    {
        StopAllCoroutines();
        StartCoroutine(ReplaySequenceRoutine());
    }

    private IEnumerator ReplaySequenceRoutine()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SetReplayStatePlaying();
        }

        yield return new WaitForSeconds(0.5f);

        if (recordedNotes != null && recordedNotes.Count > 0)
        {
            float stepDelay = 0.35f;

            for (int i = 0; i < recordedNotes.Count; i++)
            {
                int noteToPlay = recordedNotes[i];

                if (noteToPlay >= 0 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayNote(noteToPlay);
                }
                else if (noteToPlay == -1 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWrongStep();
                }

                yield return new WaitForSeconds(stepDelay);
            }
        }

        yield return new WaitForSeconds(0.4f);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SetReplayStateComplete();
        }
    }
}