using System.Collections.Generic;
using UnityEngine;

public class PelletRoundSpawner : MonoBehaviour
{
    public static PelletRoundSpawner Instance { get; private set; }

    [Header("Maze Nodes")]
    public Transform boardParent;

    public string nodeTag = "MazeNode";

    [Header("Prefabs")]
    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;

    [Header("Round Settings")]
    [Tooltip("Set how many regular pellets spawn on each round, in order. " +
             "Element 0 = round 1, element 1 = round 2, etc. If the game " +
             "reaches a round beyond this list's length, the last value in " +
             "the list keeps being used.")]
    public List<int> pelletsPerRound = new List<int> { 10, 15, 20 };

    private int _roundIndex = 0; // increments each SpawnNewRound call

    public List<Transform> powerPelletNodes = new List<Transform>();

    [Header("Options")]
    public bool skipPortalNodes = true;

    [Header("Exclusion Zones")]
    public List<ExclusionZone> exclusionZones = new List<ExclusionZone>();

    [Header("Container")]
    public Transform pelletContainer;

    [System.Serializable]
    public class ExclusionZone
    {
        public Transform center;
        public float radius = 0.6f;
    }

    private List<Transform> _candidateNodes; 
    private readonly List<GameObject> _activePellets = new List<GameObject>();
    private int _remainingPelletsThisRound;
    private int _totalPelletsThisRound;

    public void NotifyPelletCollected()
    {
        _remainingPelletsThisRound--;
        GameEvents.RaisePelletCountChanged(_remainingPelletsThisRound, _totalPelletsThisRound);

        if (_remainingPelletsThisRound <= 0)
        {
            GameEvents.RaiseAllPelletsCollected();
        }
    }

    private void Awake()
    {
        Instance = this;
        CacheCandidateNodes();
    }

    public void SpawnNewRound()
    {
        ClearActivePellets();

        if (_candidateNodes == null) CacheCandidateNodes();
        if (_candidateNodes.Count == 0)
        {
            Debug.LogWarning("PelletRoundSpawner: no eligible nodes found - check Board Parent/Node Tag.");
            return;
        }

        Transform container = GetOrCreateContainer();
        int spawnedCount = 0;


        if (powerPelletPrefab != null)
        {
            foreach (var node in powerPelletNodes)
            {
                if (node == null) continue;
                _activePellets.Add(Instantiate(powerPelletPrefab, node.position, Quaternion.identity, container));
                spawnedCount++;
            }
        }


        List<Transform> shuffled = new List<Transform>(_candidateNodes);
        Shuffle(shuffled);

        int pelletsThisRound = GetPelletCountForCurrentRound();
        int count = Mathf.Min(pelletsThisRound, shuffled.Count);
        for (int i = 0; i < count; i++)
        {
            _activePellets.Add(Instantiate(pelletPrefab, shuffled[i].position, Quaternion.identity, container));
            spawnedCount++;
        }

        _remainingPelletsThisRound = spawnedCount;
        _totalPelletsThisRound = spawnedCount;
        GameEvents.RaisePelletCountChanged(_remainingPelletsThisRound, _totalPelletsThisRound);

        _roundIndex++;
    }

    private int GetPelletCountForCurrentRound()
    {
        if (pelletsPerRound == null || pelletsPerRound.Count == 0)
        {
            Debug.LogWarning("PelletRoundSpawner: Pellets Per Round list is empty, defaulting to 15.");
            return 15;
        }

        int index = Mathf.Min(_roundIndex, pelletsPerRound.Count - 1);
        return pelletsPerRound[index];
    }

    private void ClearActivePellets()
    {
        foreach (var p in _activePellets)
        {
            if (p != null) Destroy(p);
        }
        _activePellets.Clear();
    }

    private void CacheCandidateNodes()
    {
        _candidateNodes = new List<Transform>();
        if (boardParent == null)
        {
            Debug.LogError("PelletRoundSpawner: assign Board Parent (same one GameBoard uses).");
            return;
        }

        HashSet<Transform> powerSet = new HashSet<Transform>(powerPelletNodes);

        foreach (Transform node in boardParent)
        {
            if (!node.CompareTag(nodeTag)) continue;
            if (powerSet.Contains(node)) continue; // handled separately, always fixed

            if (skipPortalNodes)
            {
                Tile tile = node.GetComponent<Tile>();
                if (tile != null && tile.isPortal) continue;
            }

            if (IsInExclusionZone(node.position)) continue;

            _candidateNodes.Add(node);
        }
    }

    private bool IsInExclusionZone(Vector3 pos)
    {
        foreach (var zone in exclusionZones)
        {
            if (zone.center == null) continue;
            if (Vector3.Distance(pos, zone.center.position) <= zone.radius)
                return true;
        }
        return false;
    }

    private static void Shuffle(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private Transform GetOrCreateContainer()
    {
        if (pelletContainer != null) return pelletContainer;

        Transform existing = transform.Find("Pellets");
        if (existing != null)
        {
            pelletContainer = existing;
            return existing;
        }

        GameObject go = new GameObject("Pellets");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        pelletContainer = go.transform;
        return go.transform;
    }
}