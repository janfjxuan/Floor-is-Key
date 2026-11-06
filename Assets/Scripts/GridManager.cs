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

    [Header("Game Stats")]
    public int maxCombo = 0;
    private int totalSteps = 0;
    private int successfulSteps = 0;

    [Header("Riff Presets")]
    public RiffData[] availableRiffs;

    private TileController[,] grid;
    private int[] targetSequence;
    private int currentSequenceStep = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerateGrid();

        // 1. Pick a random Riff asset
        if (availableRiffs != null && availableRiffs.Length > 0)
        {
            int randomIndex = Random.Range(0, availableRiffs.Length);
            targetSequence = availableRiffs[randomIndex].noteSequence;
        }
        else
        {
            targetSequence = new int[] { 0, 2, 3, 0, 3, 5, 3 };
        }

        currentSequenceStep = 0;

        // 2. Clear stats & combo state
        score = 0;
        combo = 0;
        maxCombo = 0;
        totalSteps = 0;
        successfulSteps = 0;

        // 3. Get initial player position & spawn first target note
        int startX = 2;
        int startY = 2;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            startX = player.gridX;
            startY = player.gridY;
        }

        SpawnNextTargetNote(startX, startY);

        // 4. Initialize HUD display AND push the active riff sequence to the queue!
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScore(score);
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateTimer(timeRemaining);
            HUDManager.Instance.UpdateCombo(combo);
            HUDManager.Instance.UpdateRiffQueue(targetSequence, currentSequenceStep); // 👈 ADD THIS LINE
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

        totalSteps++; // Track every step attempted

        if (steppedTile.currentState == TileState.Safe)
        {
            successfulSteps++;
            // 1. Increment Combo Streak
            combo++;

            if (combo > maxCombo)
            {
                maxCombo = combo; // Track highest combo reached
            }

            // 2. Exponential Score Formula
            int addedScore = 100 + (20 * combo);
            score += addedScore;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateScore(score);
                HUDManager.Instance.UpdateCombo(combo);
            }

            AudioManager.Instance.PlayNote(steppedTile.noteIndex);
            // Advance to next note in sequence
            currentSequenceStep++;

            // WIN CONDITION: Player reached the end of the riff!
            if (currentSequenceStep >= targetSequence.Length)
            {
                CompleteLevel();
                return;
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateRiffQueue(targetSequence, currentSequenceStep);
            }

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
                return;
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

    void CompleteLevel()
    {
        isGameOver = true; // Prevents further movement or timer updates

        // Freeze player input and physics
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            player.enabled = false;
        }

        float accuracy = totalSteps > 0 ? ((float)successfulSteps / totalSteps) * 100f : 0f;

        // Show Level Complete UI Panel
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowLevelComplete(score, accuracy, maxCombo);
        }

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartReplay();
        }
    }

    void EndGame()
    {
        isGameOver = true;

        // Freeze player position in place
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero; // Stop all momentum immediately
                rb.simulated = false;              // Disable physics simulation
            }
            player.enabled = false;
        }

        // Calculate percentage accuracy safely
        float accuracy = totalSteps > 0 ? ((float)successfulSteps / totalSteps) * 100f : 0f;

        // Pass final stats to HUD
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowGameOver(score, accuracy, maxCombo);
        }

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartReplay();
        }
    }
}
