using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// Controls autonomous Ghost enemy movement on the grid, interval timing,
/// speed modifiers during Toxic Overdrive, and player collision detection.

public class GridGhost : MonoBehaviour
{
    public Vector2Int gridPosition;
    public float moveInterval = 1.5f; // Fallback interval if GridManager isn't assigned

    void Start()
    {
        StartCoroutine(PatrolRoutine());
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // Dynamically get move speed from GridManager
            float baseInterval = (GridManager.Instance != null) ? GridManager.Instance.ghostMoveInterval : moveInterval;

            // CONSEQUENCE: Ghost moves 2x faster (half interval duration) during Toxic Overdrive!
            if (GridManager.Instance != null && GridManager.Instance.isToxicActive)
            {
                baseInterval *= 0.5f;
            }

            yield return new WaitForSeconds(baseInterval);

            // Skip movement while in Recall Mode or during Game Over
            if (GameManager.SelectedMode == GameMode.RecallMode) continue;

            if (GridManager.Instance != null && !GridManager.Instance.isGameOver)
            {
                MoveRandomly(GridManager.Instance.cols, GridManager.Instance.rows);

                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null && CheckPlayerCollision(new Vector2Int(player.gridX, player.gridY)))
                {
                    GridManager.Instance.CheckPlayerGhostCollision(new Vector2Int(player.gridX, player.gridY));
                }
            }
        }
    }

    // =========================================================================
    // AI PATHING & POSITIONING
    // =========================================================================

    public void MoveRandomly(int gridWidth, int gridHeight, List<Vector2Int> obstacles = null)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        List<Vector2Int> validMoves = new List<Vector2Int>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int candidatePos = gridPosition + dir;

            // Check grid boundaries
            if (candidatePos.x >= 0 && candidatePos.x < gridWidth && candidatePos.y >= 0 && candidatePos.y < gridHeight)
            {
                // Must be a DIFFERENT tile from current position
                if (candidatePos == gridPosition) continue;

                // Check custom obstacles list if provided
                if (obstacles != null && obstacles.Contains(candidatePos)) continue;

                // Prevent ghost from stepping onto safe (cyan) target tiles
                if (GridManager.Instance != null && GridManager.Instance.GetTileWorldPosition(candidatePos.x, candidatePos.y) != Vector3.zero)
                {
                    TileController tile = GridManager.Instance.GetTileAt(candidatePos.x, candidatePos.y);
                    if (tile != null && tile.currentState == TileState.Safe)
                    {
                        continue;
                    }
                }

                validMoves.Add(candidatePos);
            }
        }

        // Move to a new adjacent tile
        if (validMoves.Count > 0)
        {
            gridPosition = validMoves[Random.Range(0, validMoves.Count)];
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

    // =========================================================================
    // COLLISION CHECKS
    // =========================================================================

    public bool CheckPlayerCollision(Vector2Int playerPos)
    {
        return gridPosition == playerPos;
    }
}