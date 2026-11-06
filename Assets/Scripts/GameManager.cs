public enum GameMode
{
    Standard, // Regular target note & lava gameplay
    StopHammerTime, // Memory recall mode
}

public static class GameManager
{
    public static GameMode SelectedMode = GameMode.Standard;
}