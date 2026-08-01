using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 4.0f;

    private Vector2 direction = Vector2.zero;
    private Vector2 nextDirection;
    private Node currentNode, previousNode, targetNode;
    private GameBoard gameBoard;

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

       
        Node node = GetNodeAtPosition(transform.position);

        if (node != null)
        {
            currentNode = node;
            Debug.Log($"Current node: {currentNode.name}");
        }
        else
        {
            Debug.LogWarning("No node found at player position!");
        }

        direction = Vector2.left;
        ChangePosition(direction);
    }

    void Update()
    {
        CheckInput();
        Move();
        UpdateRotation();
    }

    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            ChangePosition(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            ChangePosition(Vector2.right);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            ChangePosition(Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            ChangePosition(Vector2.down);
        }
    }

    void MoveToNode(Vector2 d)
    {
        if (currentNode == null)
        {
            Debug.LogWarning("Current node is null!");
            return;
        }

        Node moveToNode = CanMove(d);
        if (moveToNode != null)
        {
            // Move to the node's WORLD position
            transform.position = moveToNode.transform.position;
            currentNode = moveToNode;
            Debug.Log($"Moved to: {currentNode.name}");
        }
        else
        {
            Debug.Log($"Cannot move {d} from {currentNode.name}");
        }
    }

    void ChangePosition(Vector2 d)
    {
        if (d != direction)
        {
            nextDirection = d;
        }

        if (currentNode != null)
        {
            Node moveToNode = CanMove(d);

            if (moveToNode != null)
            {
                direction = d;
                targetNode = moveToNode;
                previousNode = currentNode;
                currentNode = null;
            }
        }
    }

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
                }
            }
            else
            {
                transform.localPosition += (Vector3)(direction * speed) * Time.deltaTime;
            }
        }
    }

    void UpdateRotation()
    {
        if (direction == Vector2.left)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        else if (direction == Vector2.right)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.up)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else if (direction == Vector2.down)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
    }

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

    Node GetNodeAtPosition(Vector2 pos)
    {
        if (gameBoard == null || gameBoard.board == null)
        {
            Debug.LogError("GameBoard or board is null!");
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
            Debug.LogWarning($"Position ({x}, {y}) is outside board! Array index [{arrayX}, {arrayY}]");
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
}
