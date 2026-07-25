using UnityEngine;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour
{
    public GameObject[,] board;
    public Transform boardParent;

    // Add player reference
    public GameObject player;
    public string nodeTag = "MazeNode";

    void Start()
    {
        if (boardParent == null)
        {
            Debug.LogError("Board Parent not assigned! Please drag the parent GameObject containing all maze nodes.");
            return;
        }

        // Get all children of the board parent
        List<GameObject> mazeNodes = new List<GameObject>();

        foreach (Transform child in boardParent)
        {
            if (child.CompareTag(nodeTag))
            {
                mazeNodes.Add(child.gameObject);
            }
        }

        if (mazeNodes.Count == 0)
        {
            Debug.LogError($"No maze nodes found under {boardParent.name}! Make sure your nodes are children or tagged correctly.");
            return;
        }

        Debug.Log($"Found {mazeNodes.Count} maze nodes");

        // Find min and max positions
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (GameObject node in mazeNodes)
        {
            Vector2 pos = node.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);

           
            Debug.Log($"Node: {node.name} is located at : ({node.transform.localPosition.x}, {node.transform.localPosition.y})");
        }

        // Calculate board dimensions
        int boardWidth = maxX - minX + 1;
        int boardHeight = maxY - minY + 1;

        Debug.Log($"Board dimensions: {boardWidth} x {boardHeight}");
        Debug.Log($"X range: {minX} to {maxX}, Y range: {minY} to {maxY}");

        // Initialize the array
        board = new GameObject[boardWidth, boardHeight];

        // Place all nodes in the array
        foreach (GameObject node in mazeNodes)
        {
            Vector2 pos = node.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            // Convert world coordinates to array indices
            int arrayX = x - minX;
            int arrayY = y - minY;

            board[arrayX, arrayY] = node;
        }

        if (player != null)
        {
      
            Debug.Log($"Player is found at : ({player.transform.localPosition.x}, {player.transform.localPosition.y}, {player.transform.localPosition.z})");
          
        }
        else
        {
            Debug.LogWarning("Player not assigned!");
        }

       
    }

    
}