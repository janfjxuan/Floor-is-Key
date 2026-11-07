using UnityEngine;

public enum GameMode
{
    StandardMode,
    RecallMode
}

public static class GameManager
{
    public static GameMode SelectedMode = GameMode.StandardMode;
    public static float CustomPreviewSpeed = 0.5f; // Base seconds per note for Recall mode preview
    public static void SetSpeedSlow() { CustomPreviewSpeed = 0.6f; }
    public static void SetSpeedMedium() { CustomPreviewSpeed = 0.45f; }
    public static void SetSpeedFast() { CustomPreviewSpeed = 0.25f; }
}