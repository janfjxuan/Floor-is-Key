using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;

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
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(gridX, gridY);
        }

        previousGridX = gridX;
        previousGridY = gridY;
        chunkStartX = gridX;
        chunkStartY = gridY;
    }

    void Update()
    {
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
    }

    void FixedUpdate()
    {
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
        ClampPositionToGrid();
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

        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(startX, startY);
        }
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

        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(chunkStartX, chunkStartY);
        }

        // Re-enable collider next frame
        Invoke(nameof(EnableCollider), 0.05f);
    }

    private void EnableCollider()
    {
        if (col != null) col.enabled = true;
    }

    void ClampPositionToGrid()
    {
        if (GridManager.Instance == null) return;

        Vector3 minTilePos = GridManager.Instance.GetTileWorldPosition(0, 0);
        Vector3 maxTilePos = GridManager.Instance.GetTileWorldPosition(
            GridManager.Instance.cols - 1,
            GridManager.Instance.rows - 1
        );

        float minX = minTilePos.x;
        float maxX = maxTilePos.x;
        float minY = minTilePos.y;
        float maxY = maxTilePos.y;

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        transform.position = clampedPos;
    }
}