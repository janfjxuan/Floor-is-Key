using System.Collections.Generic;
using UnityEngine;

/// Handles BFS pathfinding validation and tile grid traversal logic.

public class GridPathfinder : MonoBehaviour
{
    public bool HasValidPath(TileController[,] grid, int cols, int rows, int startX, int startY, int targetX, int targetY)
    {
        if (startX < 0 || startY < 0)
        {
            startX = 2;
            startY = 2;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int start = new Vector2Int(startX, startY);
        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current.x == targetX && current.y == targetY) return true;

            foreach (Vector2Int dir in directions)
            {
                int nextX = current.x + dir.x;
                int nextY = current.y + dir.y;
                Vector2Int nextPos = new Vector2Int(nextX, nextY);

                if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows)
                {
                    if (!visited.Contains(nextPos) && grid[nextX, nextY] != null && grid[nextX, nextY].currentState != TileState.Lava)
                    {
                        visited.Add(nextPos);
                        queue.Enqueue(nextPos);
                    }
                }
            }
        }

        return false;
    }
}