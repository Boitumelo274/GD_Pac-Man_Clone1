using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI fruitProgressText;
    [SerializeField] private TextMeshProUGUI pelletCountText;
    [SerializeField] private GameObject pauseButton;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel;

    [Header("Feel (optional)")]
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;


    [Header("UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI titleText;

    [Header("Start Sequence")]
    public GameObject transitionPanel;
    public GameObject countdownPanel;
    public CountDownController countDownController;

    [Header("Settings")]
    public string mainMenuSceneName = "Menu";

    private Vector3 _fruitTextBaseScale;
    private Coroutine _punchRoutine;

    private static bool _skipIntro = false;

    private void Awake()
    {
        if (fruitProgressText != null)
        {
            _fruitTextBaseScale = fruitProgressText.transform.localScale;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnFruitProgressChanged += HandleFruitProgressChanged;
        GameEvents.OnGameWon += HandleGameWon;
        GameEvents.OnPelletCountChanged += HandlePelletCountChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnFruitProgressChanged -= HandleFruitProgressChanged;
        GameEvents.OnGameWon -= HandleGameWon;
        GameEvents.OnPelletCountChanged -= HandlePelletCountChanged;
    }

    private void Start()
    {
        //Show "0/X" immediately on level load, before any fruit is eaten.
        if (fruitProgressText != null)
        {
            fruitProgressText.text = "Fruit: 0 / 5";
        }

        // Listen for player death
        GameEvents.OnPlayerDied += ShowGameOver;

        if (_skipIntro)
        {
            _skipIntro = false; // Reset the flag for the next time

            // Hide all intro/end panels and ensure time is running
            if (transitionPanel != null) transitionPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            Time.timeScale = 1f;
        }
        else
        {
            // First time loading the scene, play the intro sequence
            StartCoroutine(StartGameSequence());
        }
    }

    private IEnumerator StartGameSequence()
    {
        Time.timeScale = 0f;

        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (transitionPanel != null) transitionPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(3.5f);

        if (transitionPanel != null) transitionPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(true);

        if (countDownController != null)
        {
            countDownController.StartCount();
        }

        yield return new WaitForSecondsRealtime(3f);

        if (countdownPanel != null) countdownPanel.SetActive(false);

        Time.timeScale = 1f; 
    }

    private void HandleFruitProgressChanged(int eaten, int required)
    {
        if (fruitProgressText == null) return;

        fruitProgressText.text = $"Fruit: {eaten} / {required}";

        if (punchScale > 0f)
        {
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);
            _punchRoutine = StartCoroutine(PunchScale());
        }
    }

    private void HandlePelletCountChanged(int remaining, int total)
    {
        if (pelletCountText == null) return;
        pelletCountText.text = $"Pellets Remaining: {remaining}";
    }

    private void HandleGameWon()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            pauseButton.SetActive(false);
            Time.timeScale = 0f;
        }
    }

    private System.Collections.IEnumerator PunchScale()
    {
        Transform t = fruitProgressText.transform;
        float elapsed = 0f;
        Vector3 target = _fruitTextBaseScale * punchScale;

        //Scale up
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(_fruitTextBaseScale, target, elapsed / punchDuration);
            yield return null;
        }

        //Scale back down
        elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(target, _fruitTextBaseScale, elapsed / punchDuration);
            yield return null;
        }

        t.localScale = _fruitTextBaseScale;
    }
    private void OnDestroy()
    {
        GameEvents.OnPlayerDied -= ShowGameOver;
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            pauseButton.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        _skipIntro = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
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