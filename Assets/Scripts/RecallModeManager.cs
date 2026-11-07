using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Controls Recall Mode mechanics: sequence chunking, self-avoiding path generation,
/// preview animations, and recall target placement.

public class RecallModeManager : MonoBehaviour
{
    [Header("Recall Mode Settings")]
    public float previewDuration = 3.0f;
    public float baseSecondsPerNote = 0.45f;
    public bool isRecallModeActive = false;
    public List<Vector2Int> recallModePath = new List<Vector2Int>();

    [Header("Dynamic Escalation Settings")]
    public int[] chunkLengths = new int[] { 4, 6, 8 };
    public int currentChunkIndex = 0;
    public int[] currentChunkNotes;

    public IEnumerator StartRecallRoutine(
        TileController[,] grid,
        int cols,
        int rows,
        int[] targetSequence,
        GridGhost activeGhost,
        System.Action<int, int> setPlayerChunkStart,
        System.Action spawnRecallTargetCallback)
    {
        isRecallModeActive = true;

        if (GridManager.Instance != null)
        {
            GridManager.Instance.currentSequenceStep = 0;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.gameObject.SetActive(false);
        if (activeGhost != null) activeGhost.gameObject.SetActive(false);

        int startIndex = GetCurrentChunkStartIndex();

        int targetChunkLength = (currentChunkIndex < chunkLengths.Length)
            ? chunkLengths[currentChunkIndex]
            : chunkLengths[chunkLengths.Length - 1];

        int chunkLength = Mathf.Min(targetChunkLength, targetSequence.Length - startIndex);

        currentChunkNotes = new int[chunkLength];
        System.Array.Copy(targetSequence, startIndex, currentChunkNotes, 0, chunkLength);

        // Keep start position grounded to current player pos or default (2,2)
        int startX = player != null ? player.gridX : 2;
        int startY = player != null ? player.gridY : 2;

        GenerateRecallModeSequence(startX, startY, chunkLength, cols, rows);

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

            if (AudioManager.Instance != null) AudioManager.Instance.PlayNote(noteForTile);

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

        if (HUDManager.Instance != null && currentChunkNotes != null)
        {
            HUDManager.Instance.UpdateRiffQueue(currentChunkNotes, 0);
        }

        yield return new WaitForSeconds(0.3f);

        // CRITICAL FIX 2: Explicitly snap player to recallModePath[0]
        if (player != null && recallModePath.Count > 0)
        {
            Vector2Int startPos = recallModePath[0];
            setPlayerChunkStart?.Invoke(startPos.x, startPos.y);
            player.gameObject.SetActive(true);
        }

        if (activeGhost != null) activeGhost.gameObject.SetActive(false);

        spawnRecallTargetCallback?.Invoke();

        yield return new WaitForSeconds(0.1f);
        isRecallModeActive = false;
    }

    public void GenerateRecallModeSequence(int startX, int startY, int sequenceLimit, int cols, int rows)
    {
        recallModePath.Clear();
        Vector2Int startPos = new Vector2Int(startX, startY);
        recallModePath.Add(startPos);

        if (!BuildSelfAvoidingPath(startPos, sequenceLimit, cols, rows))
        {
            Debug.LogWarning("Could not find completely non-overlapping path of length " + sequenceLimit);
        }
    }

    private bool BuildSelfAvoidingPath(Vector2Int currentPos, int stepsRemaining, int cols, int rows)
    {
        if (stepsRemaining <= 0) return true;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

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

            if (nextX >= 0 && nextX < cols && nextY >= 0 && nextY < rows && !recallModePath.Contains(nextPos))
            {
                recallModePath.Add(nextPos);

                if (BuildSelfAvoidingPath(nextPos, stepsRemaining - 1, cols, rows)) return true;

                recallModePath.RemoveAt(recallModePath.Count - 1);
            }
        }

        return false;
    }

    public int GetCurrentChunkStartIndex()
    {
        int index = 0;
        for (int i = 0; i < currentChunkIndex; i++)
        {
            int length = (i < chunkLengths.Length) ? chunkLengths[i] : chunkLengths[chunkLengths.Length - 1];
            index += length;
        }
        return index;
    }
}