using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayedNote
{
    public float timestamp;
    public int noteIndex;
    public bool isLava;
}

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance;
    private List<PlayedNote> history = new List<PlayedNote>();

    void Awake()
    {
        Instance = this;
    }

    public void RecordStep(int noteIndex, bool isLava, float currentTime)
    {
        history.Add(new PlayedNote
        {
            timestamp = currentTime,
            noteIndex = noteIndex,
            isLava = isLava
        });
    }

    public void StartReplay()
    {
        StartCoroutine(PlayBackRoutine());
    }

    private IEnumerator PlayBackRoutine()
    {
        if (history.Count == 0) yield break;

        float startTime = history[0].timestamp;

        foreach (var note in history)
        {
            float delay = note.timestamp - startTime;
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            startTime = note.timestamp;

            if (note.isLava)
            {
                AudioManager.Instance.PlayWrongStep();
                if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.15f);
            }
            else
            {
                AudioManager.Instance.PlayNote(note.noteIndex);
            }
        }
    }
}