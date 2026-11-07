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

    [Header("Recall Mode Settings")]
    public float previewDuration = 3.0f;
    public float baseSecondsPerNote = 0.45f;
    public bool isRecallModeActive = false;

    private List<Vector2Int> recallModePath = new List<Vector2Int>();

    [Header("Dynamic Escalation Settings")]
    public int[] chunkLengths = new int[] { 4, 6, 8 };
    private int currentChunkIndex = 0;
    private int[] currentChunkNotes;

    [Header("Ghost Settings")]
    public GridGhost ghostPrefab;
    private GridGhost activeGhost;
    public float ghostMoveInterval = 1.0f;

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

        // Safe riff loading with thorough null checks
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
            StartCoroutine(StartRecallRoutine());
        }
        else
        {
            isRecallModeActive = false;
            SpawnNextTargetNote(startX, startY);
            SpawnGhost(); // Spawn ghost AFTER target note is initialized in Standard Mode
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
                // Must not be the target (Safe) tile AND must not be the player tile
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

    public TileController GetTileAt(int x, int y)
    {
        if (grid != null && x >= 0 && x < cols && y >= 0 && y < rows)
        {
            return grid[x, y];
        }
        return null;
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
        if (player != null)
        {
            player.gameObject.SetActive(visible);
        }

        if (activeGhost != null)
        {
            activeGhost.gameObject.SetActive(visible);
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
        if (isGameOver || steppedTile == null || isRecallModeActive) return;

        PlayerController player = FindFirstObjectByType<PlayerController>();

        // =========================================================================
        // RECALL MODE LOGIC (Strict sequence memory, hidden tiles)
        // =========================================================================
        if (GameManager.SelectedMode == GameMode.RecallMode)
        {
            // Ignore start tile triggers right after phase spawn
            if (recallModePath.Count > 0 && steppedTile.gridX == recallModePath[0].x && steppedTile.gridY == recallModePath[0].y && currentSequenceStep == 0)
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

                int activeChunkLength = currentChunkNotes != null ? currentChunkNotes.Length : 4;

                if (currentSequenceStep >= activeChunkLength)
                {
                    currentChunkIndex++;

                    if (GetCurrentChunkStartIndex() >= targetSequence.Length)
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

                if (HUDManager.Instance != null) HUDManager.Instance.UpdateRiffQueue(currentChunkNotes, currentSequenceStep);
                SpawnNextRecallModeTarget();
            }
            else
            {
                // Wrong step in Recall Mode (stepping on Neutral or Lava)
                HandleStepError(player);
            }
        }
        // =========================================================================
        // STANDARD MODE LOGIC (Visible Grid, Neutral Pathing, Lava Hazards & Ghosts)
        // =========================================================================
        else
        {
            if (steppedTile.currentState == TileState.Safe)
            {
                // Target hit!
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
                // Safe path traversal towards target note
                totalSteps++;
                if (player != null) player.UpdateGridPosition(steppedTile.gridX, steppedTile.gridY);

                if (activeGhost != null && activeGhost.CheckPlayerCollision(new Vector2Int(steppedTile.gridX, steppedTile.gridY)))
                {
                    HandleGhostCollision();
                }
            }
            else if (steppedTile.currentState == TileState.Lava)
            {
                // Standard mode lava collision
                HandleStepError(player);
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
            isRecallModeActive = true;
            currentSequenceStep = 0;

            if (player != null) player.SnapToChunkStartTile();
            SpawnNextRecallModeTarget();

            if (HUDManager.Instance != null && currentChunkNotes != null)
            {
                HUDManager.Instance.UpdateRiffQueue(currentChunkNotes, 0);
            }

            StartCoroutine(UnlockPreviewAfterDelay(0.3f));
        }
    }

    public void CheckPlayerGhostCollision(Vector2Int playerPos)
    {
        // Ignore ghost collision checks completely in Recall Mode
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWrongStep();
        }

        if (lives <= 0)
        {
            EndGame();
        }
    }

    private IEnumerator UnlockPreviewAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isRecallModeActive = false;
    }

    private IEnumerator TransitionToNextChunkRoutine()
    {
        isRecallModeActive = true;
        yield return new WaitForSeconds(0.6f);
        StartCoroutine(StartRecallRoutine());
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

    public IEnumerator StartRecallRoutine()
    {
        isRecallModeActive = true;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.gameObject.SetActive(false);
        }

        if (activeGhost != null)
        {
            activeGhost.gameObject.SetActive(false);
        }

        int startIndex = GetCurrentChunkStartIndex();

        // Dynamically get target length: use chunkLengths array, or stick to the max chunk size for remaining notes
        int targetChunkLength = (currentChunkIndex < chunkLengths.Length)
            ? chunkLengths[currentChunkIndex]
            : chunkLengths[chunkLengths.Length - 1];

        // Ensure we don't overshoot the total sequence length
        int chunkLength = Mathf.Min(targetChunkLength, targetSequence.Length - startIndex);

        currentChunkNotes = new int[chunkLength];
        System.Array.Copy(targetSequence, startIndex, currentChunkNotes, 0, chunkLength);

        int startX = player != null ? player.gridX : 2;
        int startY = player != null ? player.gridY : 2;

        GenerateRecallModeSequence(startX, startY, chunkLength);

        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y].SetState(TileState.Neutral);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowPhaseText("LISTEN AND MEMORISE", 1.2f);
        }

        yield return new WaitForSeconds(0.8f);

        float currentSecondsPerNote = Mathf.Max(0.15f, GameManager.CustomPreviewSpeed - (currentChunkIndex * 0.05f));

        for (int i = 0; i < currentChunkNotes.Length; i++)
        {
            Vector2Int pos = recallModePath[i + 1];
            int noteForTile = currentChunkNotes[i];

            grid[pos.x, pos.y].SetState(TileState.Safe, noteForTile);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNote(noteForTile);
            }

            yield return new WaitForSeconds(currentSecondsPerNote);
        }

        yield return new WaitForSeconds(0.5f);

        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                grid[x, y].SetState(TileState.Neutral);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowPhaseText("PLAY IT BACK!", 1.5f);
        }

        yield return new WaitForSeconds(0.3f);

        currentSequenceStep = 0;

        if (player != null && recallModePath.Count > 0)
        {
            Vector2Int startPos = recallModePath[0];
            player.SetChunkStartPosition(startPos.x, startPos.y);
            player.gameObject.SetActive(true);
        }

        if (activeGhost != null)
        {
            activeGhost.gameObject.SetActive(false);
        }

        SpawnNextRecallModeTarget();

        yield return new WaitForSeconds(0.1f);
        isRecallModeActive = false;
    }

    void GenerateRecallModeSequence(int startX, int startY, int sequenceLimit)
    {
        recallModePath.Clear();

        Vector2Int startPos = new Vector2Int(startX, startY);
        recallModePath.Add(startPos);

        if (!BuildSelfAvoidingPath(startPos, sequenceLimit))
        {
            // Fallback: If strict non-overlapping path fails on a tight grid, reset and try random valid steps
            Debug.LogWarning("Could not find completely non-overlapping path of length " + sequenceLimit);
        }
    }

    private bool BuildSelfAvoidingPath(Vector2Int currentPos, int stepsRemaining)
    {
        if (stepsRemaining <= 0) return true;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        // Shuffle directions for randomized paths each run
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int temp = directions[i];
            int randomIndex = Random.Range(i, directions.Length);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            int nextX = currentPos.x + dir.x;
            int nextY = currentPos.y + dir.y;
            Vector2Int nextPos = new Vector2Int(nextX, nextY);

            // Ensure step stays inside grid bounds AND has not been visited yet in this sequence
            if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows && !recallModePath.Contains(nextPos))
            {
                recallModePath.Add(nextPos);

                if (BuildSelfAvoidingPath(nextPos, stepsRemaining - 1))
                {
                    return true;
                }

                // Backtrack if this path gets trapped early
                recallModePath.RemoveAt(recallModePath.Count - 1);
            }
        }

        return false;
    }

    void SpawnNextRecallModeTarget()
    {
        if (currentSequenceStep >= recallModePath.Count - 1) return;

        Vector2Int targetPos = recallModePath[currentSequenceStep + 1];
        int noteToPlay = currentChunkNotes[currentSequenceStep];

        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector2Int playerPos = player != null ? new Vector2Int(player.gridX, player.gridY) : new Vector2Int(-1, -1);
        Vector2Int startPos = recallModePath.Count > 0 ? recallModePath[0] : new Vector2Int(-1, -1);

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Set target tile safe, set player start tile to Neutral, rest to Lava
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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelCompleteSound();
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowLevelComplete(score, accuracy, maxCombo);
        }

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverSound();
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.ShowGameOver(score, accuracy, maxCombo);
        }

        StartCoroutine(StartReplayWithDelayRoutine(1.8f));
    }
}