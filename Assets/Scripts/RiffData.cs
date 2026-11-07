using UnityEngine;

/// ScriptableObject container for musical riff note sequences.

[CreateAssetMenu(fileName = "NewRiff", menuName = "Rhythm/Riff Data")]
public class RiffData : ScriptableObject
{
    public string riffName;
    public int[] noteSequence;
}