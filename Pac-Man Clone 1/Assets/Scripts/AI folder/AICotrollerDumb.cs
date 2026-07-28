using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A deliberately "dumber" ghost AI. Unlike AIController (which uses BFS to
/// find the actual shortest path to the player, and will detour to guard the
/// fruit when it's out), this ghost:
///
///   - never does full pathfinding - at every intersection it just greedily
///     picks whichever valid neighbor node LOOKS geometrically closest to
///     the player right now. No lookahead, no memory of a route. That means
///     it can easily pick the "wrong" way around a wall that a real
///     pathfinder would have routed around.
///   - has a chance ("wanderChance") to ignore the player entirely and pick
///     a random valid direction instead, so it isn't a flawless predator.
///   - completely ignores fruit-protection duty. It never subscribes to the
///     fruit events at all - it was never assigned that job and never will
///     be. It just hunts the player, permanently, no matter what else is
///     going on in the round.
/// </summary>
public class AIControllerDumb : MonoBehaviour
{
    [Header("AI Settings")]
    public float chaseSpeed = 3.5f;

    [Header("\"Dumbness\" Settings")]
    [Range(0f, 1f)]
    [Tooltip("Chance at each intersection to ignore the player and wander down a random valid path instead.")]
    public float wanderChance = 0.2f;

    [Tooltip("Classic ghost rule: if false, it won't turn back the way it came unless every other neighbor is blocked (a dead end).")]
    public bool allowReversals = false;

    [Header("References")]
    private GameBoard gameBoard;
    private Transform playerTransform;

    // Movement variables (same node-hopping approach as AIController)
    private Vector2 direction = Vector2.zero;
    private Node currentNode, previousNode, targetNode;
    private float currentSpeed;

    private List<Node> allNodes = new List<Node>();

    void Start()
    {
        GameObject gameObj = GameObject.Find("Game");
        if (gameObj != null)
        {
            gameBoard = gameObj.GetComponent<GameBoard>();
        }

        if (gameBoard == null)
        {
            Debug.LogError("GameBoard not found!");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        FindAllNodes();
        FindClosestNode();

        if (currentNode != null && currentNode.validDirections != null)
        {
            for (int i = 0; i < currentNode.validDirections.Length; i++)
            {
                if (currentNode.validDirections[i] != Vector2.zero)
                {
                    direction = currentNode.validDirections[i];
                    break;
                }
            }
        }

        currentSpeed = chaseSpeed;

        SubscribeToEvents();
    }

    void SubscribeToEvents()
    {
        // NOTE: deliberately NOT subscribing to OnFruitSpawned / OnFruitDespawned -
        // this ghost doesn't do fruit duty. It always hunts the player.
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnGameWon += HandleGameWon;
    }

    void OnDestroy()
    {
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnGameWon -= HandleGameWon;
    }

    void Update()
    {
        // Same fix as AIController: currentNode is legitimately null while
        // travelling between two nodes. Only treat it as "lost" when there's
        // no node reference at all.
        if (currentNode == null && previousNode == null && targetNode == null)
        {
            FindClosestNode();
            return;
        }

        Move();
        UpdateRotation();
    }

    void Move()
    {
        if (targetNode != currentNode && targetNode != null)
        {
            if (OvershotTarget())
            {
                currentNode = targetNode;
                transform.localPosition = currentNode.transform.position;

                GameObject otherPortal = GetPortal(currentNode.transform.position);
                if (otherPortal != null)
                {
                    transform.localPosition = otherPortal.transform.position;
                    currentNode = otherPortal.GetComponent<Node>();
                }

                // Instead of following a pre-computed path, decide fresh at
                // every single node - this is what makes it "dumb": no
                // memory of a route, only ever a local, greedy guess.
                Node moveToNode = ChooseNextNode();

                if (moveToNode != null)
                {
                    previousNode = currentNode;
                    targetNode = moveToNode;
                    direction = (targetNode.transform.position - currentNode.transform.position).normalized;
                    currentNode = null;
                }
                else
                {
                    direction = Vector2.zero;
                    targetNode = null;
                }
            }
            else
            {
                transform.localPosition += (Vector3)(direction * currentSpeed) * Time.deltaTime;
            }
        }
        else if (targetNode == null && currentNode != null)
        {
            Node moveToNode = ChooseNextNode();
            if (moveToNode != null)
            {
                previousNode = currentNode;
                targetNode = moveToNode;
                direction = (targetNode.transform.position - currentNode.transform.position).normalized;
                currentNode = null;
            }
        }
    }

    // ========== THE "DUMB" DECISION-MAKING ==========
    Node ChooseNextNode()
    {
        if (currentNode == null || currentNode.neighbors == null) return null;

        List<Node> candidates = new List<Node>();
        foreach (Node n in currentNode.neighbors)
        {
            if (n == null) continue;
            if (!allowReversals && n == previousNode && HasOtherOptions(currentNode, previousNode)) continue;
            candidates.Add(n);
        }

        if (candidates.Count == 0)
        {
            // Dead end - the only way out is back the way it came
            return previousNode;
        }

        // Occasionally just wander randomly instead of chasing - keeps it
        // from being a perfect, unbeatable predator despite always being "on."
        if (playerTransform == null || Random.value < wanderChance)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        // Greedy pick: whichever neighbor is straight-line closest to the
        // player right now. No lookahead, no awareness of walls beyond this
        // one hop - it can easily pick the "wrong" way around an obstacle
        // that real pathfinding would have avoided.
        Node best = candidates[0];
        float bestDist = Vector2.Distance(best.transform.position, playerTransform.position);

        foreach (Node candidate in candidates)
        {
            float dist = Vector2.Distance(candidate.transform.position, playerTransform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    bool HasOtherOptions(Node from, Node exclude)
    {
        if (from.neighbors == null) return false;
        foreach (Node n in from.neighbors)
        {
            if (n != null && n != exclude) return true;
        }
        return false;
    }

    void UpdateRotation()
    {
        if (direction == Vector2.left)
        {
            transform.localScale = new Vector3(-0.5f, 0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.right)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.up)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        else if (direction == Vector2.down)
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(0, 0, 270);
        }
    }

    GameObject GetPortal(Vector2 pos)
    {
        if (gameBoard == null || gameBoard.board == null) return null;

        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);
        int arrayX = x - gameBoard.minX;
        int arrayY = y - gameBoard.minY;
        int width = gameBoard.board.GetLength(0);
        int height = gameBoard.board.GetLength(1);

        if (arrayX < 0 || arrayX >= width || arrayY < 0 || arrayY >= height) return null;

        GameObject tile = gameBoard.board[arrayX, arrayY];
        if (tile != null)
        {
            Tile tileComponent = tile.GetComponent<Tile>();
            if (tileComponent != null && tileComponent.isPortal)
            {
                return tileComponent.portalReciever;
            }
        }
        return null;
    }

    bool OvershotTarget()
    {
        if (previousNode == null || targetNode == null) return false;

        float nodeToTarget = LengthFromNode(targetNode.transform.position);
        float nodeToSelf = LengthFromNode(transform.localPosition);
        return nodeToSelf > nodeToTarget;
    }

    float LengthFromNode(Vector2 targetPosition)
    {
        Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
        return vec.sqrMagnitude;
    }

    void FindAllNodes()
    {
        allNodes.Clear();
        if (gameBoard == null || gameBoard.board == null) return;

        for (int x = 0; x < gameBoard.board.GetLength(0); x++)
        {
            for (int y = 0; y < gameBoard.board.GetLength(1); y++)
            {
                GameObject tile = gameBoard.board[x, y];
                if (tile != null)
                {
                    Node node = tile.GetComponent<Node>();
                    if (node != null && !allNodes.Contains(node))
                    {
                        allNodes.Add(node);
                    }
                }
            }
        }
    }

    void FindClosestNode()
    {
        if (allNodes.Count == 0)
        {
            FindAllNodes();
            if (allNodes.Count == 0) return;
        }

        Node closest = null;
        float closestDist = float.MaxValue;

        foreach (Node node in allNodes)
        {
            float dist = Vector2.Distance(transform.position, node.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = node;
            }
        }

        if (closest != null)
        {
            currentNode = closest;
            transform.position = currentNode.transform.position;
        }
    }

    void HandleRoundStarted()
    {
        currentSpeed = chaseSpeed;
    }

    void HandlePlayerDied()
    {
        // Still doesn't care about the fruit - still just resets to hunting.
        currentSpeed = chaseSpeed;
    }

    void HandleGameWon()
    {
        currentSpeed = 0f;
        direction = Vector2.zero;
        targetNode = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameEvents.RaisePlayerDied();
            Time.timeScale = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (playerTransform != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
}