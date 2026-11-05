using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("UI Text References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

    [Header("Timer Bar References")]
    public Image timerFillImage;
    public float maxTime = 60f;

    [Header("End Screen Panels")]
    public GameObject gameOverPanel;

    [Header("Heart UI References")]
    public GameObject[] heartImages;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateTimer(float timeRemaining)
    {
        // Update numeric display
        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        // Update progress bar fill percentage (clamped 0.0 to 1.0)
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = Mathf.Clamp01(timeRemaining / maxTime);
        }
    }

    public void UpdateScore(int currentScore)
    {
        if (scoreText != null)
            scoreText.text = $"{currentScore}";
    }

    public void UpdateLives(int currentLives)
    {
        if (heartImages != null)
        {
            for (int i = 0; i < heartImages.Length; i++)
            {
                // Turns off heart graphics as lives decrease
                heartImages[i].SetActive(i < currentLives);
            }
        }
    }

    public void UpdateCombo(int currentCombo)
    {
        if (comboText != null)
        {
            comboText.text = $"x{currentCombo}";
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}