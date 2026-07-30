using UnityEngine;

public class PauseContoller : MonoBehaviour
{
    public GameObject pausePanel;
    public void Pause()
    {
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }
}
