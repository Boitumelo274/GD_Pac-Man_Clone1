using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel;
    public string gameSceneName = "SampleScene";

    public void StartGame()
    {
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
