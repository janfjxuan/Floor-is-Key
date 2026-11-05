using UnityEngine;
using TMPro;
using System.Linq;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("UI Text References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
            timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
    }

    public void UpdateScore(int currentScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
    }

    public void UpdateLives(int currentLives)
    {
        if (livesText != null)
        {
            // Displays heart icons matching remaining lives
            string hearts = currentLives > 0
            ? string.Concat(Enumerable.Repeat("❤️", currentLives))
            : "DEAD";
            livesText.text = $"Lives: {hearts}";
        }
    }
}