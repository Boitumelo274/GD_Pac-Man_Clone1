using UnityEngine;

public class PauseContoller : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject pauseButton;
    public void Pause()
    {
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        pauseButton.SetActive(false);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        pauseButton.SetActive(true);
    }
}
