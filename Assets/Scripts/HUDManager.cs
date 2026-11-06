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

    [Header("Riff Queue UI")]
    public TextMeshProUGUI[] queueSlots;
    public Image[] queueSlotBackgrounds;

    public Sprite defaultKeySprite;
    public Sprite selectedKeySprite;

    // Note name lookup matching your 13 chromatic audio clips (0 = C4, 12 = C5)
    private string[] noteNames = new string[]
    {
        "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A4", "A#4", "B4", "C5"
    };

    [Header("Timer Bar References")]
    public Image timerFillImage;
    public float maxTime = 60f;

    [Header("Game Over References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI maxComboText;

    [Header("Level Complete References")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteScoreText;
    public TextMeshProUGUI levelCompleteAccuracyText;
    public TextMeshProUGUI levelCompleteMaxComboText;

    [Header("Heart UI References")]
    public GameObject[] heartImages;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateRiffQueue(int[] sequence, int currentStep)
    {
        if (sequence == null || queueSlots == null || queueSlots.Length == 0) return;

        for (int i = 0; i < queueSlots.Length; i++)
        {
            // Left-to-right offset: i = 0 is current step, i = 1 is next step, etc.
            int seqIndex = currentStep + i;

            int targetSlot = i;

            if (seqIndex < sequence.Length)
            {
                int noteIdx = sequence[seqIndex];
                if (noteIdx >= 0 && noteIdx < noteNames.Length)
                {
                    queueSlots[targetSlot].text = noteNames[noteIdx];
                    queueSlots[targetSlot].gameObject.SetActive(true);

                    if (queueSlotBackgrounds != null && targetSlot < queueSlotBackgrounds.Length)
                    {
                        queueSlotBackgrounds[targetSlot].sprite = (i == 0) ? selectedKeySprite : defaultKeySprite;
                    }
                }
            }
            else
            {
                // Clear or hide slots once the remaining sequence is shorter than queue length
                queueSlots[targetSlot].gameObject.SetActive(false);
            }
        }
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

    public void ShowGameOver(int finalScore, float accuracy, int maxCombo)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        if (finalScoreText != null)
            finalScoreText.text = $"{finalScore}";

        if (accuracyText != null)
            accuracyText.text = $"{accuracy:F1}%";

        if (maxComboText != null)
            maxComboText.text = $"{maxCombo}x";
    }

    public void ShowLevelComplete(int finalScore, float accuracy, int maxCombo)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);

            if (levelCompleteScoreText != null)
                levelCompleteScoreText.text = $"{finalScore}";

            if (levelCompleteAccuracyText != null)
                levelCompleteAccuracyText.text = $"{accuracy:F1}%";

            if (levelCompleteMaxComboText != null)
                levelCompleteMaxComboText.text = $"{maxCombo}x";
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}