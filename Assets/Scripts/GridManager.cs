using System.Collections;
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

    private static List<int> riffPool = new List<int>();

    private TileController[,] grid;
    private int[] targetSequence;
    private int currentSequenceStep = 0;

    [Header("Hammer Time Settings")]
    public float previewDuration = 3.0f;
    public float baseSecondsPerNote = 0.45f;
    public bool isHammerTimePreviewing = false;

    private List<Vector2Int> hammerTimePath = new List<Vector2Int>();

    [Header("Dynamic Escalation Settings")]
    public int[] chunkLengths = new int[] { 4, 6, 8 };
    private int currentChunkIndex = 0;
    private int totalChunks = 0;
    private int[] currentChunkNotes;

    void Awake()
    {
        Instance = this;
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
        currentChunkIndex = 0;

        if (availableRiffs != null && availableRiffs.Length > 0)
        {
            int selectedIndex = GetNextRiffIndex();
            targetSequence = (int[])availableRiffs[selectedIndex].noteSequence.Clone();
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

        if (GameManager.SelectedMode == GameMode.StopHammerTime)
        {
            StartCoroutine(StartHammerTimeRoutine());
        }
        else
        {
            isHammerTimePreviewing = false;
            SpawnNextTargetNote(startX, startY);
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

    private int GetNextRiffIndex()
    {
        if (riffPool.Count == 0 || riffPool.Count > availableRiffs.Length)
        {
            riffPool.Clear();
            for (int i = 0; i < availableRiffs.Length; i++)
            {
                riffPool.Add(i);
            }
        }

        int randomIndex = Random.Range(0, riffPool.Count);
        int chosenIndex = riffPool[randomIndex];
        riffPool.RemoveAt(randomIndex);

        return chosenIndex;
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

    public void SetGridAndPlayerVisible(bool visible)
    {
        // Toggle all grid tiles
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

        // Toggle player sprite
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.gameObject.SetActive(visible);
        }
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

    public void OnPlayerStepTile(TileController steppedTile)
    {
        if (isGameOver || steppedTile == null || isHammerTimePreviewing) return;

        totalSteps++;
        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (steppedTile.currentState == TileState.Safe)
        {
            if (player != null)
            {
                player.UpdateGridPosition(steppedTile.gridX, steppedTile.gridY);
            }

            successfulSteps++;
            combo++;

            if (combo > maxCombo) maxCombo = combo;

            int addedScore = 100 + (20 * combo);
            score += addedScore;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateScore(score);
                HUDManager.Instance.UpdateCombo(combo);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNote(steppedTile.noteIndex);
            }

            if (ReplayManager.Instance != null)
            {
                ReplayManager.Instance.RecordStep(steppedTile.noteIndex, false, 60f - timeRemaining);
            }

            currentSequenceStep++;

            if (GameManager.SelectedMode == GameMode.StopHammerTime)
            {
                int activeChunkLength = currentChunkNotes != null ? currentChunkNotes.Length : 4;

                if (currentSequenceStep >= activeChunkLength)
                {
                    currentChunkIndex++;

                    if (currentChunkIndex >= totalChunks || GetCurrentChunkStartIndex() >= targetSequence.Length)
                    {
                        CompleteLevel();
                        return;
                    }
                    else
                    {
                        StartCoroutine(TransitionToNextChunkRoutine());
                        return;
                    }
                }
            }
            else
            {
                if (currentSequenceStep >= targetSequence.Length)
                {
                    CompleteLevel();
                    return;
                }
            }

            if (HUDManager.Instance != null)
            {
                if (GameManager.SelectedMode == GameMode.StopHammerTime)
                {
                    HUDManager.Instance.UpdateRiffQueue(currentChunkNotes, currentSequenceStep);
                }
                else
                {
                    HUDManager.Instance.UpdateRiffQueue(targetSequence, currentSequenceStep);
                }
            }

            if (GameManager.SelectedMode == GameMode.StopHammerTime)
            {
                SpawnNextHammerTimeTarget();
            }
            else
            {
                SpawnNextTargetNote(steppedTile.gridX, steppedTile.gridY);
            }
        }
        else if (steppedTile.currentState == TileState.Lava)
        {
            combo = 0;
            lives--;

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateLives(lives);
                HUDManager.Instance.UpdateCombo(combo);
            }

            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.25f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWrongStep();
            }

            if (ReplayManager.Instance != null)
            {
                ReplayManager.Instance.RecordStep(-1, true, 60f - timeRemaining);
            }

            if (lives <= 0)
            {
                EndGame();
                return;
            }

            // Clean snap back to chunk start on error
            if (GameManager.SelectedMode == GameMode.StopHammerTime)
            {
                isHammerTimePreviewing = true;
                currentSequenceStep = 0;

                if (player != null)
                {
                    player.SnapToChunkStartTile();
                }

                SpawnNextHammerTimeTarget();

                if (HUDManager.Instance != null && currentChunkNotes != null)
                {
                    HUDManager.Instance.UpdateRiffQueue(currentChunkNotes, 0);
                }

                StartCoroutine(UnlockPreviewAfterDelay(0.15f));
            }
        }
    }

    private IEnumerator UnlockPreviewAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isHammerTimePreviewing = false;
    }

    private IEnumerator TransitionToNextChunkRoutine()
    {
        isHammerTimePreviewing = true;
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(StartHammerTimeRoutine());
    }

    private int GetCurrentChunkStartIndex()
    {
        int index = 0;
        for (int i = 0; i < currentChunkIndex; i++)
        {
            int length = (i < chunkLengths.Length) ? chunkLengths[i] : chunkLengths[chunkLengths.Length - 1];
            index += length;
        }
        return index;
    }

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

    public void SpawnNextTargetNote(int playerX, int playerY)
    {
        int targetX, targetY;
        do
        {
            targetX = Random.Range(0, cols);
            targetY = Random.Range(0, rows);
        } while (targetX == playerX && targetY == playerY);

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

            if (HasValidPath(playerX, playerY, targetX, targetY))
            {
                validLayoutFound = true;
            }
        }
    }

    private bool HasValidPath(int startX, int startY, int targetX, int targetY)
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
                    if (!visited.Contains(nextPos) && grid[nextX, nextY].currentState != TileState.Lava)
                    {
                        visited.Add(nextPos);
                        queue.Enqueue(nextPos);
                    }
                }
            }
        }

        return false;
    }

    // ==========================================
    // HAMMER TIME MODE LOGIC
    // ==========================================

    public IEnumerator StartHammerTimeRoutine()
    {
        isHammerTimePreviewing = true;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.gameObject.SetActive(false); // Hide player sprite during preview
        }

        int startIndex = GetCurrentChunkStartIndex();

        int targetChunkLength = (currentChunkIndex < chunkLengths.Length) ? chunkLengths[currentChunkIndex] : chunkLengths[chunkLengths.Length - 1];
        int chunkLength = Mathf.Min(targetChunkLength, targetSequence.Length - startIndex);

        totalChunks = chunkLengths.Length;

        currentChunkNotes = new int[chunkLength];
        System.Array.Copy(targetSequence, startIndex, currentChunkNotes, 0, chunkLength);

        // Start path from player position (or center)
        int startX = player != null ? player.gridX : 2;
        int startY = player != null ? player.gridY : 2;

        GenerateHammerTimeSequence(startX, startY, chunkLength);

        // Clear grid visuals to neutral (including start tile!)
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y].SetState(TileState.Neutral);

        // 1. Display banner first so player can read it clearly
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowPhaseText("LISTEN AND MEMORISE", 1.2f);
        }

        // 2. Short pause so player reads message BEFORE preview tiles start lighting up
        yield return new WaitForSeconds(0.8f);

        float currentSecondsPerNote = Mathf.Max(0.22f, baseSecondsPerNote - (currentChunkIndex * 0.08f));

        for (int i = 0; i < currentChunkNotes.Length; i++)
        {
            Vector2Int pos = hammerTimePath[i + 1]; // Path target 1..N
            int noteForTile = currentChunkNotes[i];

            grid[pos.x, pos.y].SetState(TileState.Safe, noteForTile);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNote(noteForTile);
            }

            yield return new WaitForSeconds(currentSecondsPerNote);
        }

        yield return new WaitForSeconds(0.5f);

        // Blackout Phase: Reset all tiles to neutral
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y].SetState(TileState.Neutral);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowPhaseText("STOP! HAMMER TIME!", 1.5f);
        }

        yield return new WaitForSeconds(0.3f);

        currentSequenceStep = 0;

        // Teleport and unhide player on hammerTimePath[0]
        if (player != null && hammerTimePath.Count > 0)
        {
            Vector2Int startPos = hammerTimePath[0];
            player.SetChunkStartPosition(startPos.x, startPos.y);
            player.gameObject.SetActive(true);
        }

        SpawnNextHammerTimeTarget();

        isHammerTimePreviewing = false; // Enable tile collisions
    }

    void GenerateHammerTimeSequence(int startX, int startY, int sequenceLimit)
    {
        hammerTimePath.Clear();

        // Step 0 is the starting tile where the player stands
        Vector2Int currentPos = new Vector2Int(startX, startY);
        hammerTimePath.Add(currentPos);

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        for (int i = 0; i < sequenceLimit; i++)
        {
            List<Vector2Int> validNeighbors = new List<Vector2Int>();

            foreach (Vector2Int dir in directions)
            {
                int nextX = currentPos.x + dir.x;
                int nextY = currentPos.y + dir.y;

                if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows)
                {
                    Vector2Int nextPos = new Vector2Int(nextX, nextY);
                    if (!hammerTimePath.Contains(nextPos))
                    {
                        validNeighbors.Add(nextPos);
                    }
                }
            }

            if (validNeighbors.Count == 0)
            {
                foreach (Vector2Int dir in directions)
                {
                    int nextX = currentPos.x + dir.x;
                    int nextY = currentPos.y + dir.y;
                    if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows)
                    {
                        validNeighbors.Add(new Vector2Int(nextX, nextY));
                    }
                }
            }

            Vector2Int chosenStep = validNeighbors[Random.Range(0, validNeighbors.Count)];
            hammerTimePath.Add(chosenStep);

            currentPos = chosenStep;
        }
    }

    void SpawnNextHammerTimeTarget()
    {
        if (currentSequenceStep >= hammerTimePath.Count - 1) return;

        Vector2Int targetPos = hammerTimePath[currentSequenceStep + 1];
        int noteToPlay = currentChunkNotes[currentSequenceStep];

        // 1. Set all tiles to hidden Lava
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                grid[x, y].SetState(TileState.Lava);
                grid[x, y].SetVisualHidden();
            }
        }

        // 2. Tile 0 (player starting location) is set to Neutral
        if (hammerTimePath.Count > 0)
        {
            Vector2Int startPos = hammerTimePath[0];
            grid[startPos.x, startPos.y].SetState(TileState.Neutral);
            grid[startPos.x, startPos.y].SetVisualHidden();
        }

        // 3. Set ONLY the next target path tile to Safe
        grid[targetPos.x, targetPos.y].SetState(TileState.Safe, noteToPlay);
        grid[targetPos.x, targetPos.y].SetVisualHidden();
    }

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

        // 1. Play celebratory victory audio chord/chime
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelCompleteSound();
        }

        // 2. Display the Level Complete HUD Panel with final stats
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowLevelComplete(score, accuracy, maxCombo);
        }

        // 3. Start replay after a brief pause so player can celebrate and read stats
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

        // 1. Play lose game sound effect
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverSound();
        }

        // 2. Show Game Over panel immediately
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowGameOver(score, accuracy, maxCombo);
        }

        // 3. Pause briefly so player hears the lose sound and reads stats, then start replay
        StartCoroutine(StartReplayWithDelayRoutine(1.8f));
    }
}