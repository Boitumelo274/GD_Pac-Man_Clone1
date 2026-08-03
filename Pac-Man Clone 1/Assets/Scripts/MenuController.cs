using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Collections;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject transitionPanel;
    public string gameSceneName = "SampleScene";
    private const float DELAY_TIME = 2f;

    public void StartGame()
    {
        StartCoroutine(LoadSceneWithDelay());
    }

    private IEnumerator LoadSceneWithDelay()
    {
        transitionPanel.SetActive(true);
        yield return new WaitForSeconds(DELAY_TIME);
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Debug.Log("Quit button pressed");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        Debug.LogWarning("Quit not supported on WebGL");
#else
        Application.Quit();
#endif
    }
}
