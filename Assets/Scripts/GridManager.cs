using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Configuration")]
    public GameObject tilePrefab;
    public int rows = 5;
    public int cols = 5;
    public float tileSpacing = 1.1f;

    [Header("Game State")]
    public float timeRemaining = 60f;
    public int lives = 3;
    public int score = 0;
    public bool isGameOver = false;

    private TileController[,] grid;
    private int[] targetSequence = new int[] { 0, 2, 3, 0, 3, 5, 3 };
    private int currentSequenceStep = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerateGrid();
        SpawnNextTargetNote(-1, -1);

        // Initialize HUD display
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScore(score);
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateTimer(timeRemaining);
        }
    }

    void Update()
    {
        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateTimer(timeRemaining);
        }

        if (timeRemaining <= 0)
        {
            EndGame();
        }
    }

    public void OnPlayerStepTile(TileController steppedTile)
    {
        if (isGameOver) return;

        if (steppedTile.currentState == TileState.Safe)
        {
            score += 100;
            if (HUDManager.Instance != null) HUDManager.Instance.UpdateScore(score);

            AudioManager.Instance.PlayNote(steppedTile.noteIndex);
            currentSequenceStep = (currentSequenceStep + 1) % targetSequence.Length;
            SpawnNextTargetNote(steppedTile.gridX, steppedTile.gridY);
        }
        else if (steppedTile.currentState == TileState.Lava)
        {
            lives--;
            if (HUDManager.Instance != null) HUDManager.Instance.UpdateLives(lives);

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.25f);
            AudioManager.Instance.PlayWrongStep();

            if (lives <= 0)
            {
                EndGame();
            }
        }
    }

    void GenerateGrid()
    {
        grid = new TileController[cols, rows];

        // Calculate offset so the 5x5 grid is perfectly centered at (0,0)
        float offsetX = (cols - 1) * tileSpacing / 2f;
        float offsetY = (rows - 1) * tileSpacing / 2f;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 pos = new Vector2(x * tileSpacing - offsetX, y * tileSpacing - offsetY);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                TileController tile = tileObj.GetComponent<TileController>();
                tile.gridX = x;
                tile.gridY = y;
                tile.SetState(TileState.Neutral);

                grid[x, y] = tile;
            }
        }
    }

    public void SpawnNextTargetNote(int playerX, int playerY)
    {
        // Reset all tiles to Neutral
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                grid[x, y].SetState(TileState.Neutral);
            }
        }

        // Pick next target tile (ensuring it's NOT the player's current tile)
        int targetX, targetY;
        do
        {
            targetX = Random.Range(0, cols);
            targetY = Random.Range(0, rows);
        } while (targetX == playerX && targetY == playerY);

        int noteToPlay = targetSequence[currentSequenceStep];
        grid[targetX, targetY].SetState(TileState.Safe, noteToPlay);

        // Spawn red lava tiles
        int lavaCount = 4;
        for (int i = 0; i < lavaCount; i++)
        {
            int lx = Random.Range(0, cols);
            int ly = Random.Range(0, rows);

            if ((lx != targetX || ly != targetY) && (lx != playerX || ly != playerY))
            {
                grid[lx, ly].SetState(TileState.Lava);
            }
        }
    }

    void EndGame()
    {
        isGameOver = true;
        Debug.Log("Game Over! Final Score: " + score);
    }
}
