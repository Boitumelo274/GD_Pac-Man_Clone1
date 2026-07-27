using UnityEngine;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public float patrolSpeed = 3.0f;
    public float chaseSpeed = 4.5f;
    public float protectSpeed = 3.8f;

    [Header("AI States")]
    public AIState currentState = AIState.Chasing;
    public enum AIState
    {
        Patrolling,
        Chasing,
        ProtectingFruit
    }

    [Header("Patrol Settings")]
    public float patrolWaitTime = 2f;
    private List<Node> patrolPoints = new List<Node>();
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;

    [Header("Fruit Protection")]
    public float protectionRadius = 3f;
    private Vector2 fruitPosition;
    private bool isFruitSpawned = false;

    [Header("References")]
    private GameBoard gameBoard;
    private Transform playerTransform;

    // Movement variables (EXACT COPY of PlayerController)
    private Vector2 direction = Vector2.zero;
    private Vector2 nextDirection;
    private Node currentNode, previousNode, targetNode;
    private float currentSpeed;

    // Pathfinding
    private List<Node> currentPath = new List<Node>();
    private int pathIndex = 0;
    private List<Node> allNodes = new List<Node>();

    // Timing
    private float pathfindTimer = 0f;
    private float pathfindInterval = 0.5f;

    void Start()
    {
        // Get GameBoard reference
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

        // Get player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // Find all nodes
        FindAllNodes();

        // Find closest node to start position
        FindClosestNode();

        // Set initial direction
        if (currentNode != null && currentNode.neighbors != null && currentNode.neighbors.Length > 0)
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

        // Set up patrol points
        SetupPatrolPoints();

        // Start with chase speed
        currentSpeed = chaseSpeed;
        currentState = AIState.Chasing;

        // Subscribe to events
        SubscribeToEvents();
    }

    void FindAllNodes()
    {
        allNodes.Clear();

        // Get all nodes from GameBoard
        if (gameBoard == null || gameBoard.board == null) return;

        for (int x = 0; x < gameBoard.board.GetLength(0); x++)
        {
            for (int y = 0; y < gameBoard.board.GetLength(1); y++)
            {
                GameObject tile = gameBoard.board[x, y];
                if (tile != null)
                {
                    Node node = tile.GetComponent<Node>();
                    if (node != null)
                    {
                        // Only add if not already in list
                        bool exists = false;
                        foreach (Node n in allNodes)
                        {
                            if (n == node) { exists = true; break; }
                        }
                        if (!exists) allNodes.Add(node);
                    }
                }
            }
        }

        Debug.Log($"Found {allNodes.Count} nodes");
    }

    void SetupPatrolPoints()
    {
        patrolPoints.Clear();

        if (allNodes.Count == 0) return;

        int patrolCount = Mathf.Min(4, allNodes.Count);

        List<Node> shuffled = new List<Node>(allNodes);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Node temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        for (int i = 0; i < patrolCount; i++)
        {
            if (shuffled[i] != null && !patrolPoints.Contains(shuffled[i]))
            {
                patrolPoints.Add(shuffled[i]);
            }
        }
    }

    void SubscribeToEvents()
    {
        GameEvents.OnFruitSpawned += HandleFruitSpawned;
        GameEvents.OnFruitDespawned += HandleFruitDespawned;
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnGameWon += HandleGameWon;
    }

    void OnDestroy()
    {
        GameEvents.OnFruitSpawned -= HandleFruitSpawned;
        GameEvents.OnFruitDespawned -= HandleFruitDespawned;
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnGameWon -= HandleGameWon;
    }

    void Update()
    {
        // FIX: currentNode is legitimately null while the AI is travelling
        // between two nodes (SetNextTargetFromPath/PatrolAroundFruit/chase
        // logic all set it to null on purpose). The old check here treated
        // that as "lost" and called FindClosestNode() every such frame,
        // which snaps transform.position onto the nearest node and skips
        // Move() for that frame — that's what was causing the
        // stutter/teleport glitch. We only want the recovery snap when the
        // AI has no node references at all (e.g. truly uninitialized).
        if (currentNode == null && previousNode == null && targetNode == null)
        {
            FindClosestNode();
            return;
        }

        // Update pathfinding periodically
        pathfindTimer += Time.deltaTime;
        if (pathfindTimer >= pathfindInterval)
        {
            pathfindTimer = 0f;
            UpdatePath();
        }

        // Handle waiting at patrol points
        if (isWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                isWaiting = false;
                MoveToNextPatrolPoint();
            }
            return;
        }

        // MOVE (EXACT SAME AS PLAYER)
        Move();
        UpdateRotation();
    }

    void UpdatePath()
    {
        // If fruit is spawned, protect it
        if (isFruitSpawned)
        {
            float distanceToFruit = Vector2.Distance(transform.position, fruitPosition);

            if (distanceToFruit > protectionRadius)
            {
                SetState(AIState.ProtectingFruit);
                SetupProtectPath();
                return;
            }
            else
            {
                if (currentState != AIState.ProtectingFruit)
                {
                    SetState(AIState.ProtectingFruit);
                }
                PatrolAroundFruit();
                return;
            }
        }

        // ALWAYS CHASE PLAYER
        SetState(AIState.Chasing);
        SetupChasePath();
    }

    void SetState(AIState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case AIState.Chasing:
                currentSpeed = chaseSpeed;
                break;
            case AIState.ProtectingFruit:
                currentSpeed = protectSpeed;
                break;
            default:
                currentSpeed = patrolSpeed;
                break;
        }
    }

    void SetupChasePath()
    {
        if (currentNode == null || playerTransform == null) return;

        // Get the node the player is at
        Node playerNode = GetNodeAtPosition(playerTransform.position);
        if (playerNode == null) return;

        // If we're already at the player's node, try to move toward them
        if (currentNode == playerNode)
        {
            // Try to move in the direction of the player
            Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
            Node moveNode = GetNodeInDirection(dirToPlayer);
            if (moveNode != null && moveNode != previousNode)
            {
                targetNode = moveNode;
                previousNode = currentNode;
                currentNode = null;
                direction = (targetNode.transform.position - previousNode.transform.position).normalized;

                // FIX: clear any leftover path from a previous chase target.
                // Without this, Move()'s overshoot handler could pick back up
                // a stale currentPath/pathIndex from an earlier calculation
                // and steer the AI toward an outdated destination.
                currentPath.Clear();
                pathIndex = 0;
            }
            return;
        }

        // Find path to player using BFS
        List<Node> path = FindPath(currentNode, playerNode);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
            SetNextTargetFromPath();
        }
    }

    void SetupProtectPath()
    {
        if (currentNode == null) return;

        Node fruitNode = GetNodeAtPosition(fruitPosition);
        if (fruitNode == null) return;

        // If we're already at the fruit node, patrol around it
        if (currentNode == fruitNode)
        {
            PatrolAroundFruit();
            return;
        }

        // Find path to fruit
        List<Node> path = FindPath(currentNode, fruitNode);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
            SetNextTargetFromPath();
        }
    }

    void SetNextTargetFromPath()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        if (pathIndex >= currentPath.Count) return;

        Node nextNode = currentPath[pathIndex];
        if (nextNode == null) return;

        // Check if this node is a valid neighbor
        if (currentNode != null)
        {
            bool isValidNeighbor = false;
            if (currentNode.neighbors != null)
            {
                for (int i = 0; i < currentNode.neighbors.Length; i++)
                {
                    if (currentNode.neighbors[i] == nextNode)
                    {
                        isValidNeighbor = true;
                        break;
                    }
                }
            }

            if (!isValidNeighbor)
            {
                // Invalid path, recalculate
                return;
            }
        }

        targetNode = nextNode;
        previousNode = currentNode;
        currentNode = null;
        direction = (targetNode.transform.position - previousNode.transform.position).normalized;

        pathIndex++;
    }

    void PatrolAroundFruit()
    {
        if (currentNode == null) return;

        // Find a random valid neighbor
        List<Node> neighbors = new List<Node>();
        if (currentNode.neighbors != null)
        {
            for (int i = 0; i < currentNode.neighbors.Length; i++)
            {
                if (currentNode.neighbors[i] != null && currentNode.neighbors[i] != previousNode)
                {
                    neighbors.Add(currentNode.neighbors[i]);
                }
            }
        }

        if (neighbors.Count > 0)
        {
            Node randomNeighbor = neighbors[Random.Range(0, neighbors.Count)];
            targetNode = randomNeighbor;
            previousNode = currentNode;
            currentNode = null;
            direction = (targetNode.transform.position - previousNode.transform.position).normalized;
        }
    }

    void MoveToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;

        Node targetPatrol = patrolPoints[currentPatrolIndex];
        if (targetPatrol != null && currentNode != null)
        {
            List<Node> path = FindPath(currentNode, targetPatrol);
            if (path != null && path.Count > 0)
            {
                currentPath = path;
                pathIndex = 0;
                SetNextTargetFromPath();
            }

            if (currentNode == targetPatrol)
            {
                StartWaiting();
            }
        }

        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
    }

    void StartWaiting()
    {
        isWaiting = true;
        patrolWaitTimer = patrolWaitTime;
        direction = Vector2.zero;
        targetNode = null;
    }

    // ========== EXACT SAME MOVE METHOD AS PLAYER ==========
    void Move()
    {
        if (targetNode != currentNode && targetNode != null)
        {
            if (nextDirection == direction * -1)
            {
                direction *= -1;
                Node tempNode = targetNode;
                targetNode = previousNode;
                previousNode = tempNode;
            }

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

                // Get next node from path
                if (currentPath != null && pathIndex < currentPath.Count)
                {
                    SetNextTargetFromPath();
                    return;
                }

                // Try to turn
                Node moveToNode = CanMove(nextDirection);
                if (moveToNode != null)
                {
                    direction = nextDirection;
                }

                if (moveToNode == null)
                {
                    moveToNode = CanMove(direction);
                }

                if (moveToNode != null)
                {
                    targetNode = moveToNode;
                    previousNode = currentNode;
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
        else if (targetNode == null && direction != Vector2.zero)
        {
            Node moveToNode = CanMove(direction);
            if (moveToNode != null)
            {
                targetNode = moveToNode;
                previousNode = currentNode;
                currentNode = null;
            }
            else
            {
                direction = Vector2.zero;
            }
        }
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

    // ========== EXACT SAME HELPER METHODS AS PLAYER ==========

    Node CanMove(Vector2 d)
    {
        if (currentNode == null || currentNode.neighbors == null)
            return null;

        for (int i = 0; i < currentNode.neighbors.Length; i++)
        {
            if (currentNode.validDirections[i] == d)
            {
                return currentNode.neighbors[i];
            }
        }
        return null;
    }

    Node GetNodeInDirection(Vector2 dir)
    {
        if (currentNode == null || currentNode.neighbors == null) return null;

        Node bestNode = null;
        float bestDot = -1f;

        for (int i = 0; i < currentNode.neighbors.Length; i++)
        {
            if (currentNode.neighbors[i] == null) continue;

            Vector2 nodeDir = (currentNode.neighbors[i].transform.position - currentNode.transform.position).normalized;
            float dot = Vector2.Dot(dir, nodeDir);

            if (dot > bestDot && dot > 0.5f)
            {
                bestDot = dot;
                bestNode = currentNode.neighbors[i];
            }
        }

        return bestNode;
    }

    Node GetNodeAtPosition(Vector2 pos)
    {
        if (gameBoard == null || gameBoard.board == null)
        {
            return null;
        }

        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        int arrayX = x - gameBoard.minX;
        int arrayY = y - gameBoard.minY;

        int width = gameBoard.board.GetLength(0);
        int height = gameBoard.board.GetLength(1);

        if (arrayX < 0 || arrayX >= width || arrayY < 0 || arrayY >= height)
        {
            return null;
        }

        GameObject tile = gameBoard.board[arrayX, arrayY];
        if (tile != null)
        {
            return tile.GetComponent<Node>();
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

    GameObject GetPortal(Vector2 pos)
    {
        if (gameBoard == null || gameBoard.board == null)
        {
            return null;
        }

        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        int arrayX = x - gameBoard.minX;
        int arrayY = y - gameBoard.minY;

        int width = gameBoard.board.GetLength(0);
        int height = gameBoard.board.GetLength(1);

        if (arrayX < 0 || arrayX >= width || arrayY < 0 || arrayY >= height)
        {
            return null;
        }

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

    // ========== PROPER BFS PATHFINDING ==========

    List<Node> FindPath(Node start, Node target)
    {
        List<Node> path = new List<Node>();

        if (start == null || target == null || start == target)
        {
            if (start == target && start != null)
            {
                path.Add(target);
            }
            return path;
        }

        // BFS Pathfinding - ONLY uses valid node connections
        Queue<Node> queue = new Queue<Node>();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        HashSet<Node> visited = new HashSet<Node>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

            if (current == target)
            {
                // Reconstruct path
                Stack<Node> reversePath = new Stack<Node>();
                Node node = target;
                while (node != start)
                {
                    reversePath.Push(node);
                    if (!cameFrom.ContainsKey(node)) break;
                    node = cameFrom[node];
                }

                while (reversePath.Count > 0)
                {
                    path.Add(reversePath.Pop());
                }
                return path;
            }

            // Only check valid neighbors
            if (current.neighbors != null)
            {
                for (int i = 0; i < current.neighbors.Length; i++)
                {
                    Node neighbor = current.neighbors[i];
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // No path found
        return path;
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

    // ========== EVENT HANDLERS ==========

    void HandleFruitSpawned(Vector3 position)
    {
        isFruitSpawned = true;
        fruitPosition = position;
        SetState(AIState.ProtectingFruit);
    }

    void HandleFruitDespawned(Vector3 position)
    {
        isFruitSpawned = false;
        SetState(AIState.Chasing);
    }

    void HandleRoundStarted()
    {
        isFruitSpawned = false;
        SetState(AIState.Chasing);
    }

    void HandlePlayerDied()
    {
        isFruitSpawned = false;
        SetState(AIState.Chasing);
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
        }
    }

    // ========== DEBUG VISUALIZATION ==========

    void OnDrawGizmosSelected()
    {
        // State color
        switch (currentState)
        {
            case AIState.Patrolling:
                Gizmos.color = Color.blue;
                break;
            case AIState.Chasing:
                Gizmos.color = Color.red;
                break;
            case AIState.ProtectingFruit:
                Gizmos.color = Color.yellow;
                break;
        }
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Fruit position
        if (isFruitSpawned)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(fruitPosition, 0.5f);
            Gizmos.DrawLine(transform.position, fruitPosition);
        }

        // Current path
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Vector3 prevPos = transform.position;
            foreach (Node node in currentPath)
            {
                if (node != null)
                {
                    Gizmos.DrawLine(prevPos, node.transform.position);
                    prevPos = node.transform.position;
                }
            }
        }
    }
}