using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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

    private string[] noteNames = new string[]
    {
        "C3", "C#3", "D3", "D#3", "E3", "F3", "F#3", "G3", "G#3", "A3", "A#3", "B3", // Octave 3 (Indices 0-11)
        "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A4", "A#4", "B4", // Octave 4 (Indices 12-23)
        "C5", "C#5", "D5", "D#5", "E5", "F5", "F#5", "G5", "G#5", "A5", "A#5", "B5"  // Octave 5 (Indices 24-35)
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

    [Header("Replay & Shared UI References")]
    public TextMeshProUGUI replayStatusText;
    public GameObject playAgainButton;

    [Header("Phase Banner UI")]
    [SerializeField] private TextMeshProUGUI phaseText;
    private Coroutine activeBannerCoroutine;

    [Header("UI Frame References")]
    public GameObject gridBorderFrame;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateRiffQueue(int[] sequence, int currentStep)
    {
        if (sequence == null || queueSlots == null || queueSlots.Length == 0) return;

        for (int i = 0; i < queueSlots.Length; i++)
        {
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
                queueSlots[targetSlot].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

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
        gameOverPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = $"{finalScore}";

        if (accuracyText != null)
            accuracyText.text = $"{accuracy:F1}%";

        if (maxComboText != null)
            maxComboText.text = $"{maxCombo}x";

        AttachReplayUI(gameOverPanel.transform);
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

        AttachReplayUI(levelCompletePanel.transform);
    }

    // Reparents the single text and button to whichever end-screen panel is active
    private void AttachReplayUI(Transform targetPanel)
    {
        if (targetPanel == null) return;

        if (replayStatusText != null)
        {
            replayStatusText.transform.SetParent(targetPanel, false);
        }

        if (playAgainButton != null)
        {
            playAgainButton.transform.SetParent(targetPanel, false);
            playAgainButton.SetActive(false);
        }
    }

    public void SetReplayStatePlaying()
    {
        if (replayStatusText != null)
        {
            replayStatusText.text = "REPLAYING YOUR MELODY...";
        }

        if (playAgainButton != null)
        {
            playAgainButton.SetActive(false);
        }
    }

    public void SetReplayStateComplete()
    {
        if (replayStatusText != null)
        {
            replayStatusText.text = "MELODY PLAYBACK COMPLETE!";
        }

        if (playAgainButton != null)
        {
            playAgainButton.SetActive(true);
        }
    }

    public void ShowPhaseText(string message, float duration = 2.0f)
    {
        if (phaseText == null) return;

        if (activeBannerCoroutine != null)
        {
            StopCoroutine(activeBannerCoroutine);
        }

        activeBannerCoroutine = StartCoroutine(ShowPhaseTextRoutine(message, duration));
    }

    private IEnumerator ShowPhaseTextRoutine(string message, float duration)
    {
        phaseText.text = message;
        phaseText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        phaseText.gameObject.SetActive(false);
        activeBannerCoroutine = null;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("MainMenu");
    }
}