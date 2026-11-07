using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Manages power-up spawning, single-melon lifecycle enforcement,
/// and Toxic Overdrive timer routines.

public class ToxicItemManager : MonoBehaviour
{
    [Header("Toxic Overdrive Settings")]
    public GameObject toxicMelonPrefab;
    public float toxicDuration = 4.0f;
    public bool isToxicActive = false;
    private Coroutine toxicCoroutine;

    public void ClearAllToxicMelons(TileController[,] grid, int cols, int rows)
    {
        if (grid == null) return;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].ClearItem();
                    if (grid[x, y].currentState == TileState.Toxic)
                    {
                        grid[x, y].currentState = TileState.Neutral;
                    }
                }
            }
        }
    }

    public void TrySpawnMelon(TileController[,] grid, int cols, int rows, int playerX, int playerY, GridGhost activeGhost)
    {
        if (Random.value >= 0.30f) return;

        List<Vector2Int> availableTiles = new List<Vector2Int>();

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                bool isPlayer = (x == playerX && y == playerY);
                bool isGhost = (activeGhost != null && activeGhost.gridPosition.x == x && activeGhost.gridPosition.y == y);

                if (grid[x, y].currentState == TileState.Neutral && !isPlayer && !isGhost)
                {
                    availableTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (availableTiles.Count > 0)
        {
            Vector2Int toxicPos = availableTiles[Random.Range(0, availableTiles.Count)];
            grid[toxicPos.x, toxicPos.y].SetState(TileState.Toxic);
            grid[toxicPos.x, toxicPos.y].SpawnToxicItem(toxicMelonPrefab);
        }
    }

    public void TriggerToxicOverdrive()
    {
        if (toxicCoroutine != null) StopCoroutine(toxicCoroutine);
        toxicCoroutine = StartCoroutine(ToxicOverdriveRoutine());
    }

    private IEnumerator ToxicOverdriveRoutine()
    {
        isToxicActive = true;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowToxicOverdriveText(1.5f);
        }

        yield return new WaitForSeconds(toxicDuration);

        isToxicActive = false;
    }
}