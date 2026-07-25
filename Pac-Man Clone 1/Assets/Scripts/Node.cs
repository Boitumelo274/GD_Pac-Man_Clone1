using UnityEngine;

public class Node : MonoBehaviour
{
    public Node[] neighbors;
    public Vector2[] validDirections;

    void Start()
    {
        validDirections = new Vector2[neighbors.Length];

        for (int i = 0; i < neighbors.Length; i++)
        {
            if (neighbors[i] != null)
            {
                Vector2 tempVector = neighbors[i].transform.position - transform.position;
                validDirections[i] = tempVector.normalized;
            }
        }
    }

    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (neighbors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Node neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
                }
            }
        }
    }
}