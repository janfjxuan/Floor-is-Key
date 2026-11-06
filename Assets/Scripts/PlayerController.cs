using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveCooldown = 0.15f; // Time between grid steps
    private float moveTimer = 0f;

    [Header("Grid Position")]
    public int gridX = 2;
    public int gridY = 2;

    public int previousGridX = 2;
    public int previousGridY = 2;

    [Header("Phase Starting Position")]
    public int chunkStartX = 2;
    public int chunkStartY = 2;

    private Rigidbody2D rb;
    private Collider2D col;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Snap instantly to starting position center
        SnapToGridPosition(gridX, gridY);

        previousGridX = gridX;
        previousGridY = gridY;
        chunkStartX = gridX;
        chunkStartY = gridY;
    }

    void Update()
    {
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            return;
        }

        // Handle movement cooldown timer
        if (moveTimer > 0f)
        {
            moveTimer -= Time.deltaTime;
        }
        else
        {
            HandleGridInput();
        }
    }

    void HandleGridInput()
    {
        int targetX = gridX;
        int targetY = gridY;
        bool inputReceived = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetY++;
            inputReceived = true;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetY--;
            inputReceived = true;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            targetX--;
            inputReceived = true;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            targetX++;
            inputReceived = true;
        }

        if (inputReceived)
        {
            // Check boundaries before moving
            if (GridManager.Instance != null)
            {
                if (targetX >= 0 && targetX < GridManager.Instance.cols &&
                    targetY >= 0 && targetY < GridManager.Instance.rows)
                {
                    UpdateGridPosition(targetX, targetY);
                    SnapToGridPosition(gridX, gridY);
                    moveTimer = moveCooldown; // Apply delay between steps
                }
            }
        }
    }

    public void UpdateGridPosition(int newX, int newY)
    {
        if (newX != gridX || newY != gridY)
        {
            previousGridX = gridX;
            previousGridY = gridY;

            gridX = newX;
            gridY = newY;
        }
    }

    public void SetChunkStartPosition(int startX, int startY)
    {
        chunkStartX = startX;
        chunkStartY = startY;
        gridX = startX;
        gridY = startY;
        previousGridX = startX;
        previousGridY = startY;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        SnapToGridPosition(startX, startY);
    }

    // Teleports player back safely without double-triggering collisions
    public void SnapToChunkStartTile()
    {
        gridX = chunkStartX;
        gridY = chunkStartY;
        previousGridX = chunkStartX;
        previousGridY = chunkStartY;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Disable collider briefly to avoid triggering collision on target teleport tile
        if (col != null) col.enabled = false;

        SnapToGridPosition(chunkStartX, chunkStartY);

        // Re-enable collider next frame
        Invoke(nameof(EnableCollider), 0.05f);
    }

    private void SnapToGridPosition(int x, int y)
    {
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(x, y);
        }
    }

    private void EnableCollider()
    {
        if (col != null) col.enabled = true;
    }
}