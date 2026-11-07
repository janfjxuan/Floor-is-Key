using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridGhost : MonoBehaviour
{
    public Vector2Int gridPosition;
    public float moveInterval = 1.5f; // Time in seconds between ghost movements

    void Start()
    {
        StartCoroutine(PatrolRoutine());
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveInterval);

            if (GridManager.Instance != null && !GridManager.Instance.isGameOver)
            {
                MoveRandomly(GridManager.Instance.cols, GridManager.Instance.rows);

                // Check if ghost patrolled directly onto the player's current tile!
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null && CheckPlayerCollision(new Vector2Int(player.gridX, player.gridY)))
                {
                    GridManager.Instance.CheckPlayerGhostCollision(new Vector2Int(player.gridX, player.gridY));
                }
            }
        }
    }

    public void MoveRandomly(int gridWidth, int gridHeight, List<Vector2Int> obstacles = null)
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>
        {
            gridPosition + Vector2Int.up,
            gridPosition + Vector2Int.down,
            gridPosition + Vector2Int.left,
            gridPosition + Vector2Int.right
        };

        // Filter out-of-bounds moves
        possibleMoves.RemoveAll(pos => pos.x < 0 || pos.x >= gridWidth || pos.y < 0 || pos.y >= gridHeight);

        if (possibleMoves.Count > 0)
        {
            gridPosition = possibleMoves[Random.Range(0, possibleMoves.Count)];
            UpdateWorldPosition();
        }
    }

    public void UpdateWorldPosition()
    {
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(gridPosition.x, gridPosition.y);
        }
        else
        {
            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);
        }
    }

    public bool CheckPlayerCollision(Vector2Int playerPos)
    {
        return gridPosition == playerPos;
    }
}