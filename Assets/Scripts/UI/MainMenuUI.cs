using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// Handles main menu scene navigation, speed settings panel, and mode launch.

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

    [Header("How To Play Modal")]
    public GameObject howToPlayPanel;

    void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void OpenHowToPlayPanel()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void CloseHowToPlayPanel()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
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

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

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