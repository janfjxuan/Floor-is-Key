using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject settingsPanel;

    [Header("Speed Buttons")]
    public Button slowButton;
    public Button mediumButton;
    public Button fastButton;
    public Sprite normalButtonSprite;
    public Sprite selectedButtonSprite;

    void Start()
    {
        // Ensure settings panel is hidden when the main menu first loads
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void PlayStandardMode()
    {
        GameManager.SelectedMode = GameMode.StandardMode;
        SceneManager.LoadScene("GameScene");
    }

    public void OnClickRecallModeButton()
    {
        GameManager.SelectedMode = GameMode.RecallMode;

        // Show the settings panel when Recall Mode is clicked
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // --- Speed Selection & Direct Game Launch ---
    public void SetSpeedSlowAndPlay()
    {
        GameManager.SetSpeedSlow();
        HighlightButton(slowButton, mediumButton, fastButton);
        SceneManager.LoadScene("GameScene");
    }

    public void SetSpeedMediumAndPlay()
    {
        GameManager.SetSpeedMedium();
        HighlightButton(mediumButton, slowButton, fastButton);
        SceneManager.LoadScene("GameScene");
    }

    public void SetSpeedFastAndPlay()
    {
        GameManager.SetSpeedFast();
        HighlightButton(fastButton, slowButton, mediumButton);
        SceneManager.LoadScene("GameScene");
    }

    private void HighlightButton(Button selected, params Button[] others)
    {
        if (selected != null && selectedButtonSprite != null)
        {
            selected.image.sprite = selectedButtonSprite;
        }

        foreach (var btn in others)
        {
            if (btn != null && normalButtonSprite != null)
            {
                btn.image.sprite = normalButtonSprite;
            }
        }
    }
}