using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void PlayStandardMode()
    {
        GameManager.SelectedMode = GameMode.Standard;
        SceneManager.LoadScene("GameScene");
    }

    public void PlayHammerTimeMode()
    {
        GameManager.SelectedMode = GameMode.StopHammerTime;
        SceneManager.LoadScene("GameScene");
    }
}