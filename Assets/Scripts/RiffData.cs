using UnityEngine;

[CreateAssetMenu(fileName = "NewRiff", menuName = "Rhythm/Riff Data")]
public class RiffData : ScriptableObject
{
    public string riffName;
    public int[] noteSequence;
}