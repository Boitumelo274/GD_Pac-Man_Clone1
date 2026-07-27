using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI fruitProgressText;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel;

    [Header("Feel (optional)")]
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.15f;

    private Vector3 _fruitTextBaseScale;
    private Coroutine _punchRoutine;

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
    }

    private void OnDisable()
    {
        GameEvents.OnFruitProgressChanged -= HandleFruitProgressChanged;
        GameEvents.OnGameWon -= HandleGameWon;
    }

    private void Start()
    {
        //Show "0/X" immediately on level load, before any fruit is eaten.
        if (fruitProgressText != null)
        {
            fruitProgressText.text = "Fruit: 0 / 5";
        }

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
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

    private void HandleGameWon()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
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
}