using UnityEngine;
using System.Collections;

/// Controls Pac-Man movement, grid step cooldowns, direction animations,
/// position snapping, and Toxic Leap mechanics.

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveCooldown = 0.15f;
    private float moveTimer = 0f;

    [Header("Grid Position")]
    public int gridX = 2;
    public int gridY = 2;
    public int previousGridX = 2;
    public int previousGridY = 2;

    [Header("Phase Starting Position")]
    public int chunkStartX = 2;
    public int chunkStartY = 2;
    // Active movement direction tracking for Toxic Leap
    public int lastDirX = 0;
    public int lastDirY = 0;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

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

        if (moveTimer > 0f)
        {
            moveTimer -= Time.deltaTime;
        }
        else
        {
            HandleGridInput();
        }
    }

    void FixedUpdate()
    {
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // =========================================================================
    // INPUT HANDLING & MOVEMENT
    // =========================================================================

    void HandleGridInput()
    {
        // Block player movement if game is over, if Recall Mode is active (preview/transition),
        // or if the recall sequence is complete
        if (GridManager.Instance != null)
        {
            if (GridManager.Instance.isGameOver ||
                GridManager.Instance.isRecallModeActive ||
                GridManager.Instance.IsRecallSequenceComplete())
            {
                return;
            }
        }

        int targetX = gridX;
        int targetY = gridY;
        bool inputReceived = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            targetY++;
            lastDirX = 0; lastDirY = 1;
            inputReceived = true;
            if (anim != null) anim.SetInteger("MoveDirection", 1);
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            targetY--;
            lastDirX = 0; lastDirY = -1;
            inputReceived = true;
            if (anim != null) anim.SetInteger("MoveDirection", 3);
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            targetX--;
            lastDirX = -1; lastDirY = 0;
            inputReceived = true;
            if (anim != null) anim.SetInteger("MoveDirection", 2);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            targetX++;
            lastDirX = 1; lastDirY = 0;
            inputReceived = true;
            if (anim != null) anim.SetInteger("MoveDirection", 0);
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

                    // 2x Movement Speed
                    float effectiveCooldown = moveCooldown;
                    if (GridManager.Instance.isToxicActive)
                    {
                        effectiveCooldown *= 0.5f;
                    }

                    moveTimer = effectiveCooldown;

                    GridManager.Instance.CheckPlayerGhostCollision(new Vector2Int(gridX, gridY));
                }
            }
        }
    }

    // =========================================================================
    // POSITIONING & TELEPORTATION
    // =========================================================================

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

    // =========================================================================
    // SPECIAL MECHANICS
    // =========================================================================

    public void PerformToxicLeap(int dirX, int dirY)
    {
        // Don't execute leap if sequence is complete or game is over
        if (GridManager.Instance != null &&
           (GridManager.Instance.isGameOver || GridManager.Instance.IsRecallSequenceComplete()))
        {
            return;
        }

        // Calculate the bonus extra step forward in the player's active input direction
        int leapX = gridX + dirX;
        int leapY = gridY + dirY;

        // Boundary check to ensure the leap target stays within grid bounds
        if (GridManager.Instance != null &&
            leapX >= 0 && leapX < GridManager.Instance.cols &&
            leapY >= 0 && leapY < GridManager.Instance.rows)
        {
            UpdateGridPosition(leapX, leapY);
            SnapToGridPosition(gridX, gridY);
        }
    }
}