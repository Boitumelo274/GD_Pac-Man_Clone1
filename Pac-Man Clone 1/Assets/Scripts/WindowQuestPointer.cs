using UnityEngine;
using UnityEngine.UI;

public class QuestPointer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform arrowImage; // The arrow UI element
    [SerializeField] private RectTransform pointerContainer; // The parent container
    [SerializeField] private Canvas canvas;

    [Header("Settings")]
    [SerializeField] private float borderOffset = 30f; // Distance from screen edge
    [SerializeField] private float minDistanceToShow = 2f; // Min distance to show arrow

    [Header("Target")]
    private Transform targetObject; // The fruit transform
    private bool isTargetActive = false;

    [Header("References")]
    private Camera mainCamera;
    private Transform playerTransform;

    void Start()
    {
        // Get references
        mainCamera = Camera.main;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (arrowImage != null)
        {
            arrowImage.gameObject.SetActive(false);
        }

        // Subscribe to events
        SubscribeToEvents();
    }

    void SubscribeToEvents()
    {
        GameEvents.OnFruitSpawned += HandleFruitSpawned;
        GameEvents.OnFruitDespawned += HandleFruitDespawned;
        GameEvents.OnGameWon += HandleGameWon;
    }

    void OnDestroy()
    {
        GameEvents.OnFruitSpawned -= HandleFruitSpawned;
        GameEvents.OnFruitDespawned -= HandleFruitDespawned;
        GameEvents.OnGameWon -= HandleGameWon;
    }

    void Update()
    {
        if (!isTargetActive || targetObject == null || playerTransform == null)
        {
            if (arrowImage != null)
            {
                arrowImage.gameObject.SetActive(false);
            }
            return;
        }

        UpdateArrowPosition();
    }

    void UpdateArrowPosition()
    {
        // Get direction from player to target
        Vector3 directionToTarget = targetObject.position - playerTransform.position;
        float distance = directionToTarget.magnitude;

        // If target is too close, hide arrow
        if (distance < minDistanceToShow)
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        // Show arrow
        arrowImage.gameObject.SetActive(true);

        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetObject.position);

        // If the target is behind the camera, WorldToScreenPoint mirrors x/y,
        // which would otherwise make the arrow snap to the wrong side.
        // Flip it back so the direction stays correct.
        if (screenPos.z < 0)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        // Single, consistent check for on-screen vs off-screen (uses the same
        // borderOffset the clamping below uses, so there's no mismatched edge).
        bool isOffScreen = screenPos.x < borderOffset || screenPos.x > Screen.width - borderOffset ||
                            screenPos.y < borderOffset || screenPos.y > Screen.height - borderOffset ||
                            screenPos.z < 0;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dirFromCenter = (Vector2)screenPos - screenCenter;

        // Guard against a zero-length vector (target exactly at screen center),
        // which is what caused the angle to jitter/snap before.
        if (dirFromCenter.sqrMagnitude < 0.0001f)
        {
            pointerContainer.position = screenCenter;
            return;
        }

        dirFromCenter.Normalize();

        // Arrow art points "up" by default. Atan2 measures angle from the
        // positive x-axis (right), so we subtract 90 degrees to align the
        // sprite's tip with the direction instead of its side.
        float angle = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg - 90f;

        if (!isOffScreen)
        {
            // Target is visible on screen - place arrow directly on it
            pointerContainer.position = screenPos;
        }
        else
        {
            // Target is off-screen - place arrow on the screen edge
            Vector2 edgePos = screenCenter + dirFromCenter * Mathf.Min(Screen.width, Screen.height) * 0.45f;
            edgePos.x = Mathf.Clamp(edgePos.x, borderOffset, Screen.width - borderOffset);
            edgePos.y = Mathf.Clamp(edgePos.y, borderOffset, Screen.height - borderOffset);
            pointerContainer.position = edgePos;
        }

        arrowImage.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Event Handlers
    void HandleFruitSpawned(Vector3 position)
    {
        // Find the fruit GameObject
        BonusFruit[] fruits = FindObjectsOfType<BonusFruit>();
        foreach (BonusFruit fruit in fruits)
        {
            if (Vector2.Distance(fruit.transform.position, position) < 0.1f)
            {
                targetObject = fruit.transform;
                isTargetActive = true;
                Debug.Log("Quest Pointer: Fruit spawned! Showing direction.");
                break;
            }
        }
    }

    void HandleFruitDespawned(Vector3 position)
    {
        isTargetActive = false;
        targetObject = null;

        if (arrowImage != null)
        {
            arrowImage.gameObject.SetActive(false);
        }

        Debug.Log("Quest Pointer: Fruit collected! Hiding arrow.");
    }

    void HandleGameWon()
    {
        isTargetActive = false;
        targetObject = null;

        if (arrowImage != null)
        {
            arrowImage.gameObject.SetActive(false);
        }
    }
}