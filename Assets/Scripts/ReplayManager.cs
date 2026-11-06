using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance;

    // Static list preserves the recorded notes across DontDestroyOnLoad scene transitions
    private static List<int> recordedNotes = new List<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[ReplayManager] Singleton Initialized.");
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
            Debug.Log($"[ReplayManager] Saved Note #{recordedNotes.Count}: Index {noteIndex}");
        }
    }

    public void ClearRecording()
    {
        recordedNotes.Clear();
        Debug.Log("[ReplayManager] Cleared recorded steps.");
    }

    public void StartReplay()
    {
        StopAllCoroutines(); // Stop any overlapping replay routines
        StartCoroutine(ReplaySequenceRoutine());
    }

    private IEnumerator ReplaySequenceRoutine()
    {
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

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayNote(noteToPlay);
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