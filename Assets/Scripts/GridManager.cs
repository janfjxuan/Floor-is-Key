using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core game loop coordinator managing grid initialization, score/lives tracking,
/// mode routing, and delegation to specialized sub-managers.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Core Sub-Managers")]
    public ToxicItemManager toxicManager;
    public GridPathfinder pathfinder;
    public RecallModeManager recallManager;

    public bool isToxicActive => toxicManager != null && toxicManager.isToxicActive;
    public bool isRecallModeActive => recallManager != null && recallManager.isRecallModeActive;

    [Header("Grid Configuration")]
    public GameObject tilePrefab;
    public int rows = 5;
    public int cols = 5;
    public float tileSpacing = 1.1f;
    private TileController[,] grid;

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
    private static List<int> riffPool = new List<int>();
    public int[] targetSequence;
    public int currentSequenceStep = 0;

    [Header("Ghost Settings")]
    public GridGhost ghostPrefab;
    private GridGhost activeGhost;
    public float ghostMoveInterval = 1.0f;

    [Header("Floating Feedback Prefab")]
    public GameObject floatingTextPrefab;

    void Awake()
    {
        Instance = this;

        // Auto-fetch attached sub-managers if not manually linked in Inspector
        if (toxicManager == null) toxicManager = GetComponent<ToxicItemManager>();
        if (pathfinder == null) pathfinder = GetComponent<GridPathfinder>();
        if (recallManager == null) recallManager = GetComponent<RecallModeManager>();
    }

    void Start()
    {
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.ClearRecording();
        }

        GenerateGrid();

        score = 0;
        combo = 0;
        maxCombo = 0;
        totalSteps = 0;
        successfulSteps = 0;
        currentSequenceStep = 0;

        if (recallManager != null)
        {
            recallManager.currentChunkIndex = 0;
        }

        // Safe riff loading
        if (availableRiffs != null && availableRiffs.Length > 0)
        {
            int selectedIndex = GetNextRiffIndex();
            if (selectedIndex >= 0 && selectedIndex < availableRiffs.Length &&
                availableRiffs[selectedIndex] != null &&
                availableRiffs[selectedIndex].noteSequence != null)
            {
                targetSequence = (int[])availableRiffs[selectedIndex].noteSequence.Clone();
            }
            else
            {
                targetSequence = new int[] { 0, 2, 3, 0, 3, 5, 3, 2, 0, 4, 5, 2, 1, 3, 0, 2, 4, 5 };
            }
        }
        else
        {
            targetSequence = new int[] { 0, 2, 3, 0, 3, 5, 3, 2, 0, 4, 5, 2, 1, 3, 0, 2, 4, 5 };
        }

        int startX = 2;
        int startY = 2;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            startX = player.gridX;
            startY = player.gridY;
        }

        if (GameManager.SelectedMode == GameMode.RecallMode)
        {
            StartRecallModeRoutine();
        }
        else
        {
            if (recallManager != null) recallManager.isRecallModeActive = false;
            SpawnNextTargetNote(startX, startY);
            SpawnGhost();
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScore(score);
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateTimer(timeRemaining);
            HUDManager.Instance.UpdateCombo(combo);
            HUDManager.Instance.UpdateRiffQueue(targetSequence, currentSequenceStep);
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

    // =========================================================================
    // GRID GENERATION & ACCESSORS
    // =========================================================================
    void GenerateGrid()
    {
        grid = new TileController[cols, rows];

        float offsetX = (cols - 1) * tileSpacing / 2f;
        float offsetY = (rows - 1) * tileSpacing / 2f;
        float verticalShift = -1.85f;

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

    public TileController GetTileAt(int x, int y)
    {
        if (grid != null && x >= 0 && x < cols && y >= 0 && y < rows)
        {
            return grid[x, y];
        }
        return null;
    }

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

    public void SetGridAndPlayerVisible(bool visible)
    {
        if (grid != null)
        {
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[x, y] != null)
                    {
                        grid[x, y].gameObject.SetActive(visible);
                    }
                }
            }
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.gameObject.SetActive(visible);
        if (activeGhost != null) activeGhost.gameObject.SetActive(visible);
    }

    private int GetNextRiffIndex()
    {
        if (availableRiffs == null || availableRiffs.Length == 0) return 0;

        if (riffPool.Count == 0 || riffPool.Count > availableRiffs.Length)
        {
            riffPool.Clear();
            for (int i = 0; i < availableRiffs.Length; i++)
            {
                if (availableRiffs[i] != null && availableRiffs[i].noteSequence != null)
                {
                    riffPool.Add(i);
                }
            }
        }

        if (riffPool.Count == 0) return 0;

        int randomIndex = Random.Range(0, riffPool.Count);
        int chosenIndex = riffPool[randomIndex];
        riffPool.RemoveAt(randomIndex);

        return chosenIndex;
    }

    // =========================================================================
    // PLAYER STEP & TILE INTERACTION LOGIC
    // =========================================================================
    public void OnPlayerStepTile(TileController steppedTile)
    {
        if (isGameOver || steppedTile == null || isRecallModeActive) return;

        PlayerController player = FindFirstObjectByType<PlayerController>();

        // ---------------------------------------------------------------------
        // RECALL MODE STEP HANDLING
        // ---------------------------------------------------------------------
        if (GameManager.SelectedMode == GameMode.RecallMode)
        {
            if (recallManager != null && recallManager.recallModePath.Count > 0 &&
                steppedTile.gridX == recallManager.recallModePath[0].x &&
                steppedTile.gridY == recallManager.recallModePath[0].y &&
                currentSequenceStep == 0)
            {
                return;
            }

            if (steppedTile.currentState == TileState.Safe)
            {
                totalSteps++;
                successfulSteps++;
                combo++;
                if (combo > maxCombo) maxCombo = combo;

                if (player != null) player.UpdateGridPosition(steppedTile.gridX, steppedTile.gridY);

                score += 100 + (20 * combo);

                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.UpdateScore(score);
                    HUDManager.Instance.UpdateCombo(combo);
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayNote(steppedTile.noteIndex);
                if (ReplayManager.Instance != null) ReplayManager.Instance.RecordStep(steppedTile.noteIndex, false, 60f - timeRemaining);

                currentSequenceStep++;

                int activeChunkLength = (recallManager != null && recallManager.currentChunkNotes != null)
                    ? recallManager.currentChunkNotes.Length
                    : 4;

                if (currentSequenceStep >= activeChunkLength)
                {
                    if (recallManager != null) recallManager.currentChunkIndex++;

                    if (recallManager != null && recallManager.GetCurrentChunkStartIndex() >= targetSequence.Length)
                    {
                        CompleteLevel();
                        return;
                    }
                    else
                    {
                        currentSequenceStep = 0; // Reset step counter before phase transition routine!
                        StartCoroutine(TransitionToNextChunkRoutine());
                        return;
                    }
                }

                if (HUDManager.Instance != null && recallManager != null)
                {
                    HUDManager.Instance.UpdateRiffQueue(recallManager.currentChunkNotes, currentSequenceStep);
                }
                SpawnNextRecallModeTarget();
            }
            else
            {
                HandleStepError(player);
            }
        }
        // ---------------------------------------------------------------------
        // STANDARD MODE STEP HANDLING
        // ---------------------------------------------------------------------
        else
        {
            if (steppedTile.currentState == TileState.Safe)
            {
                totalSteps++;
                successfulSteps++;
                combo++;
                if (combo > maxCombo) maxCombo = combo;

                if (player != null) player.UpdateGridPosition(steppedTile.gridX, steppedTile.gridY);

                score += 100 + (20 * combo);

                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.UpdateScore(score);
                    HUDManager.Instance.UpdateCombo(combo);
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayNote(steppedTile.noteIndex);
                if (ReplayManager.Instance != null) ReplayManager.Instance.RecordStep(steppedTile.noteIndex, false, 60f - timeRemaining);

                if (activeGhost != null && activeGhost.CheckPlayerCollision(new Vector2Int(steppedTile.gridX, steppedTile.gridY)))
                {
                    HandleGhostCollision();
                    if (isGameOver) return;
                }

                currentSequenceStep++;

                if (currentSequenceStep >= targetSequence.Length)
                {
                    CompleteLevel();
                    return;
                }

                if (HUDManager.Instance != null) HUDManager.Instance.UpdateRiffQueue(targetSequence, currentSequenceStep);
                SpawnNextTargetNote(steppedTile.gridX, steppedTile.gridY);
            }
            else if (steppedTile.currentState == TileState.Neutral)
            {
                totalSteps++;
                if (player != null) player.UpdateGridPosition(steppedTile.gridX, steppedTile.gridY);

                if (activeGhost != null && activeGhost.CheckPlayerCollision(new Vector2Int(steppedTile.gridX, steppedTile.gridY)))
                {
                    HandleGhostCollision();
                }
            }
            else if (steppedTile.currentState == TileState.Lava)
            {
                HandleStepError(player);
            }
            else if (steppedTile.currentState == TileState.Toxic)
            {
                totalSteps++;
                steppedTile.ClearItem();

                score += 300;
                combo += 2;
                if (combo > maxCombo) maxCombo = combo;

                if (HUDManager.Instance != null)
                {
                    HUDManager.Instance.UpdateScore(score);
                    HUDManager.Instance.UpdateCombo(combo);
                }

                if (floatingTextPrefab != null && player != null)
                {
                    Vector3 spawnPos = player.transform.position + new Vector3(0f, 0.5f, -1f);
                    Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
                }

                steppedTile.SetState(TileState.Neutral);
                TriggerToxicOverdrive();

                if (player != null)
                {
                    player.PerformToxicLeap(player.lastDirX, player.lastDirY);
                }

                if (activeGhost != null && activeGhost.CheckPlayerCollision(new Vector2Int(player.gridX, player.gridY)))
                {
                    HandleGhostCollision();
                }
            }
        }
    }

    private void HandleStepError(PlayerController player)
    {
        totalSteps++;
        combo = 0;
        lives--;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateCombo(combo);
        }

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.25f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongStep();
        if (ReplayManager.Instance != null) ReplayManager.Instance.RecordStep(-1, true, 60f - timeRemaining);

        if (lives <= 0)
        {
            EndGame();
            return;
        }

        if (GameManager.SelectedMode == GameMode.RecallMode)
        {
            if (recallManager != null) recallManager.isRecallModeActive = true;
            currentSequenceStep = 0; // Reset step counter on mistake

            // =========================================================================
            // RECALL MODE RESPRAWN FIX
            // Update chunk starting position to position 0 of the active chunk path!
            // =========================================================================
            if (player != null && recallManager != null && recallManager.recallModePath.Count > 0)
            {
                Vector2Int startPos = recallManager.recallModePath[0];
                player.SetChunkStartPosition(startPos.x, startPos.y);
                player.SnapToChunkStartTile();
            }
            // =========================================================================

            SpawnNextRecallModeTarget();

            if (HUDManager.Instance != null && recallManager != null && recallManager.currentChunkNotes != null)
            {
                HUDManager.Instance.UpdateRiffQueue(recallManager.currentChunkNotes, 0);
            }

            StartCoroutine(UnlockPreviewAfterDelay(0.3f));
        }
    }

    // =========================================================================
    // STANDARD MODE TARGET & PATH GENERATION
    // =========================================================================
    public void SpawnNextTargetNote(int playerX, int playerY)
    {
        ClearAllToxicMelons();

        int targetX, targetY;
        int maxAttempts = 100;
        int attempts = 0;

        do
        {
            targetX = Random.Range(0, cols);
            targetY = Random.Range(0, rows);
            attempts++;

            bool isGhostPos = (activeGhost != null && activeGhost.gridPosition.x == targetX && activeGhost.gridPosition.y == targetY);
            bool isPlayerPos = (targetX == playerX && targetY == playerY);

            if (!isGhostPos && !isPlayerPos) break;

        } while (attempts < maxAttempts);

        int noteToPlay = targetSequence[currentSequenceStep];

        bool validLayoutFound = false;
        int layoutAttempts = 0;

        while (!validLayoutFound && layoutAttempts < 200)
        {
            layoutAttempts++;

            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    grid[x, y].SetState(TileState.Neutral);

            grid[targetX, targetY].SetState(TileState.Safe, noteToPlay);

            int lavaCount = 4;
            int placedLava = 0;
            int lavaAttempts = 0;

            while (placedLava < lavaCount && lavaAttempts < 50)
            {
                lavaAttempts++;
                int lx = Random.Range(0, cols);
                int ly = Random.Range(0, rows);

                bool isTarget = (lx == targetX && ly == targetY);
                bool isPlayer = (lx == playerX && ly == playerY);
                bool isAlreadyLava = (grid[lx, ly].currentState == TileState.Lava);

                if (!isTarget && !isPlayer && !isAlreadyLava)
                {
                    grid[lx, ly].SetState(TileState.Lava);
                    placedLava++;
                }
            }

            if (pathfinder != null && pathfinder.HasValidPath(grid, cols, rows, playerX, playerY, targetX, targetY))
            {
                validLayoutFound = true;

                if (toxicManager != null)
                {
                    toxicManager.TrySpawnMelon(grid, cols, rows, playerX, playerY, activeGhost);
                }
            }
        }
    }

    // =========================================================================
    // RECALL MODE ROUTINES & DELEGATION
    // =========================================================================
    public void StartRecallModeRoutine()
    {
        if (recallManager != null)
        {
            // Reset current sequence step counter whenever a new chunk starts
            currentSequenceStep = 0;

            StartCoroutine(recallManager.StartRecallRoutine(
                grid, cols, rows, targetSequence, activeGhost,
                (x, y) =>
                {
                    PlayerController p = FindFirstObjectByType<PlayerController>();
                    if (p != null) p.SetChunkStartPosition(x, y);
                },
                () => SpawnNextRecallModeTarget()
            ));
        }
    }

    public void SpawnNextRecallModeTarget()
    {
        if (recallManager == null || currentSequenceStep >= recallManager.recallModePath.Count - 1) return;

        Vector2Int targetPos = recallManager.recallModePath[currentSequenceStep + 1];
        int noteToPlay = recallManager.currentChunkNotes[currentSequenceStep];

        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2Int playerPos = player != null ? new Vector2Int(player.gridX, player.gridY) : new Vector2Int(-1, -1);
        Vector2Int startPos = recallManager.recallModePath.Count > 0 ? recallManager.recallModePath[0] : new Vector2Int(-1, -1);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if ((x == playerPos.x && y == playerPos.y) || (x == startPos.x && y == startPos.y))
                {
                    grid[x, y].SetState(TileState.Neutral);
                }
                else
                {
                    grid[x, y].SetState(TileState.Lava);
                }
                grid[x, y].SetVisualHidden();
            }
        }

        grid[targetPos.x, targetPos.y].SetState(TileState.Safe, noteToPlay);
        grid[targetPos.x, targetPos.y].SetVisualHidden();
    }

    private IEnumerator UnlockPreviewAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (recallManager != null) recallManager.isRecallModeActive = false;
    }

    private IEnumerator TransitionToNextChunkRoutine()
    {
        if (recallManager != null) recallManager.isRecallModeActive = true;
        yield return new WaitForSeconds(0.6f);
        StartRecallModeRoutine();
    }

    // =========================================================================
    // GHOST MECHANICS
    // =========================================================================
    void SpawnGhost()
    {
        if (ghostPrefab == null) return;

        List<Vector2Int> validTiles = new List<Vector2Int>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2Int playerPos = player != null ? new Vector2Int(player.gridX, player.gridY) : new Vector2Int(-1, -1);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null &&
                    grid[x, y].currentState != TileState.Safe &&
                    (x != playerPos.x || y != playerPos.y))
                {
                    validTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        Vector2Int chosenPos = new Vector2Int(cols - 1, rows - 1);
        if (validTiles.Count > 0)
        {
            chosenPos = validTiles[Random.Range(0, validTiles.Count)];
        }

        if (activeGhost == null)
        {
            activeGhost = Instantiate(ghostPrefab);
        }

        activeGhost.gridPosition = chosenPos;
        activeGhost.UpdateWorldPosition();
    }

    public void CheckPlayerGhostCollision(Vector2Int playerPos)
    {
        if (GameManager.SelectedMode == GameMode.RecallMode) return;

        if (activeGhost != null && activeGhost.gameObject.activeSelf && activeGhost.CheckPlayerCollision(playerPos))
        {
            HandleGhostCollision();
        }
    }

    void HandleGhostCollision()
    {
        combo = 0;
        lives--;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateLives(lives);
            HUDManager.Instance.UpdateCombo(combo);
        }

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.25f, 0.3f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongStep();

        if (lives <= 0)
        {
            EndGame();
        }
    }

    // =========================================================================
    // TOXIC OVERDRIVE DELEGATION
    // =========================================================================
    public void TriggerToxicOverdrive()
    {
        if (toxicManager != null) toxicManager.TriggerToxicOverdrive();
    }

    public void ClearAllToxicMelons()
    {
        if (toxicManager != null) toxicManager.ClearAllToxicMelons(grid, cols, rows);
    }

    // =========================================================================
    // LEVEL COMPLETION & GAME OVER
    // =========================================================================
    void CompleteLevel()
    {
        isGameOver = true;

        SetGridAndPlayerVisible(false);

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

        if (AudioManager.Instance != null) AudioManager.Instance.PlayLevelCompleteSound();
        if (HUDManager.Instance != null) HUDManager.Instance.ShowLevelComplete(score, accuracy, maxCombo);

        StartCoroutine(StartReplayWithDelayRoutine(1.8f));
    }

    void EndGame()
    {
        isGameOver = true;

        SetGridAndPlayerVisible(false);

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

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameOverSound();
        if (HUDManager.Instance != null) HUDManager.Instance.ShowGameOver(score, accuracy, maxCombo);

        StartCoroutine(StartReplayWithDelayRoutine(1.8f));
    }

    private IEnumerator StartReplayWithDelayRoutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartReplay();
        }
    }
}