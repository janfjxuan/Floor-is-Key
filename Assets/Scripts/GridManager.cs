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
    public int combo = 0;
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
        int startX = 2;
        int startY = 2;
        SpawnNextTargetNote(-1, -1);

        // Initialize HUD display
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScore(score);
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateTimer(timeRemaining);
            HUDManager.Instance.UpdateCombo(combo);
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

    // Returns the world space position of a tile at (gridX, gridY)
    public Vector3 GetTileWorldPosition(int x, int y)
    {
        if (grid != null && x >= 0 && x < cols && y >= 0 && y < rows)
        {
            if (grid[x, y] != null)
            {
                return grid[x, y].transform.position;
            }
        }
        return Vector3.zero;
    }

    public void OnPlayerStepTile(TileController steppedTile)
    {
        if (isGameOver) return;

        if (steppedTile.currentState == TileState.Safe)
        {
            // 1. Increment Combo Streak
            combo++;

            // 2. Exponential Score Formula
            int addedScore = 100 + (20 * combo);
            score += addedScore;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateScore(score);
                HUDManager.Instance.UpdateCombo(combo);
            }

            AudioManager.Instance.PlayNote(steppedTile.noteIndex);
            currentSequenceStep = (currentSequenceStep + 1) % targetSequence.Length;

            SpawnNextTargetNote(steppedTile.gridX, steppedTile.gridY);
            ReplayManager.Instance.RecordStep(steppedTile.noteIndex, false, 60f - timeRemaining);
        }
        else if (steppedTile.currentState == TileState.Lava)
        {
            // Reset Combo to 0 on Lava hit
            combo = 0;
            lives--;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateLives(lives);
                HUDManager.Instance.UpdateCombo(combo); // Reset HUD text to x0
            }

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.25f);
            AudioManager.Instance.PlayWrongStep();

            if (lives <= 0)
            {
                EndGame();
            }

            ReplayManager.Instance.RecordStep(-1, true, 60f - timeRemaining);
        }
    }

    void GenerateGrid()
    {
        grid = new TileController[cols, rows];

        // Calculate offsets to center grid horizontally and shift vertically below the HUD
        float offsetX = (cols - 1) * tileSpacing / 2f;
        float offsetY = (rows - 1) * tileSpacing / 2f;

        // Shift the Y center down (adjust -1.2f to taste)
        float verticalShift = -1.5f;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 pos = new Vector2(
                    x * tileSpacing - offsetX,
                    (y * tileSpacing - offsetY) + verticalShift
                );
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
        // 1. Pick next target tile (ensuring it's NOT the player's current tile)
        int targetX, targetY;
        do
        {
            targetX = Random.Range(0, cols);
            targetY = Random.Range(0, rows);
        } while (targetX == playerX && targetY == playerY);

        int noteToPlay = targetSequence[currentSequenceStep];

        // Try generating a valid layout until BFS confirms a clear path exists
        bool validLayoutFound = false;
        int layoutAttempts = 0;
        int maxLayoutAttempts = 200;

        while (!validLayoutFound && layoutAttempts < maxLayoutAttempts)
        {
            layoutAttempts++;

            // Clear all tiles to Neutral
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    grid[x, y].SetState(TileState.Neutral);
                }
            }

            // Set Safe target
            grid[targetX, targetY].SetState(TileState.Safe, noteToPlay);

            // Place 4 lava tiles
            int lavaCount = 4;
            int placedLava = 0;
            int lavaAttempts = 0;

            while (placedLava < lavaCount && lavaAttempts < 50)
            {
                lavaAttempts++;
                int lx = Random.Range(0, cols);
                int ly = Random.Range(0, rows);

                // STRICT CHECK: Never place lava on Target, Player position, or existing Lava
                bool isTarget = (lx == targetX && ly == targetY);
                bool isPlayer = (lx == playerX && ly == playerY);
                bool isAlreadyLava = (grid[lx, ly].currentState == TileState.Lava);

                if (!isTarget && !isPlayer && !isAlreadyLava)
                {
                    grid[lx, ly].SetState(TileState.Lava);
                    placedLava++;
                }
            }

            // Verify if a clear path exists from Player to Target using BFS
            if (HasValidPath(playerX, playerY, targetX, targetY))
            {
                validLayoutFound = true;
            }
        }
    }

    // Breadth-First Search (BFS) to guarantee a walkable path exists
    private bool HasValidPath(int startX, int startY, int targetX, int targetY)
    {
        // If starting point is invalid (-1 on game start), assume starting center (2,2)
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
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current.x == targetX && current.y == targetY)
            {
                return true; // Path found!
            }

            foreach (Vector2Int dir in directions)
            {
                int nextX = current.x + dir.x;
                int nextY = current.y + dir.y;
                Vector2Int nextPos = new Vector2Int(nextX, nextY);

                if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows)
                {
                    // Can walk on Neutral or Safe tiles (never Lava)
                    if (!visited.Contains(nextPos) && grid[nextX, nextY].currentState != TileState.Lava)
                    {
                        visited.Add(nextPos);
                        queue.Enqueue(nextPos);
                    }
                }
            }
        }

        return false; // Path blocked by lava wall
    }

    void EndGame()
    {
        isGameOver = true;
        // Disable player controls so they can't move during replay
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        // Trigger end screen overlay
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowGameOver();
        }

        // Trigger performance replay playback
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartReplay();
        }
    }
}
