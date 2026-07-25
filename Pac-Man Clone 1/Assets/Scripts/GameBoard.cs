using UnityEngine;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour
{
    public GameObject[,] board;
    public Transform boardParent;

    public GameObject player;
    public string nodeTag = "MazeNode";

    public int minX;
    public int minY;
    public int maxX;
    public int maxY;
    public int boardWidth;
    public int boardHeight;

    void Start()
    {
        if (boardParent == null)
        {
            Debug.LogError("Board Parent not assigned! Please drag the parent GameObject containing all maze nodes.");
            return;
        }

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
            Debug.LogError($"No maze nodes found under {boardParent.name}!");
            return;
        }

        Debug.Log($"Found {mazeNodes.Count} maze nodes");

   
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        foreach (GameObject node in mazeNodes)
        {
            Vector2 pos = node.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);

            Debug.Log($"Node: {node.name} at world: ({x}, {y})");
        }

        boardWidth = maxX - minX + 1;
        boardHeight = maxY - minY + 1;

        Debug.Log($"Board dimensions: {boardWidth} x {boardHeight}");
        Debug.Log($"X range: {minX} to {maxX}, Y range: {minY} to {maxY}");

        board = new GameObject[boardWidth, boardHeight];

        foreach (GameObject node in mazeNodes)
        {
            Vector2 pos = node.transform.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            int arrayX = x - minX;
            int arrayY = y - minY;

            board[arrayX, arrayY] = node;
        }

        Debug.Log($"Board initialized! Size: {boardWidth}x{boardHeight}");
    }
}