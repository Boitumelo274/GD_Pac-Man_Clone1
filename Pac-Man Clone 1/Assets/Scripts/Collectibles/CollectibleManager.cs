using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("Round Setup")]
    [SerializeField] private PelletRoundSpawner pelletRoundSpawner;

    [Header("Win Condition")]
    [SerializeField] private int fruitRequiredToWin = 5;

    [SerializeField] private BonusFruit fruitPrefab;


    [SerializeField] private Transform[] fruitSpawnPoints;

    private int _fruitEatenCount;
    private bool _gameWon;
    private BonusFruit _activeFruit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnAllPelletsCollected += HandleAllPelletsCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnAllPelletsCollected -= HandleAllPelletsCollected;
    }

    private void Start()
    {
        StartNewRound();
    }

    public void StartNewRound()
    {
        if (_gameWon) return;

        DespawnActiveFruit();
        pelletRoundSpawner?.SpawnNewRound();

        GameEvents.RaiseRoundStarted();
    }

    private void HandlePlayerDied()
    {
        //Death resets the round layout
        //win progress persists across deaths.
        StartNewRound();
    }

    private void HandleAllPelletsCollected()
    {
        if (_gameWon) return;
        SpawnFruit();
    }

    ///<summary>Called by BonusFruit when it's eaten.</summary>
    public void NotifyFruitCollected(BonusFruit fruit)
    {
        if (_gameWon) return;

        _activeFruit = null;
        _fruitEatenCount++;
        GameEvents.RaiseFruitProgressChanged(_fruitEatenCount, fruitRequiredToWin);

        if (_fruitEatenCount >= fruitRequiredToWin)
        {
            _gameWon = true;
            GameEvents.RaiseGameWon();
            return;
        }

        StartNewRound();
    }

    private void SpawnFruit()
    {
        if (fruitPrefab == null || fruitSpawnPoints == null || fruitSpawnPoints.Length == 0)
        {
            Debug.LogWarning("CollectibleManager: fruit prefab or spawn points not assigned.");
            return;
        }

        Transform point = fruitSpawnPoints[Random.Range(0, fruitSpawnPoints.Length)];
        _activeFruit = Instantiate(fruitPrefab, point.position, Quaternion.identity);
        _activeFruit.gameObject.SetActive(true);
    }

    private void DespawnActiveFruit()
    {
        if (_activeFruit == null) return;
        Destroy(_activeFruit.gameObject);
        _activeFruit = null;
    }

    ///<summary>Fruit eaten so far toward the win condition.</summary>
    public int FruitEatenCount => _fruitEatenCount;
}