using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;

    [Header("Grid Position")]
    public int gridX = 2; // Default to grid center (2, 2)
    public int gridY = 2;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Snap physical starting transform position to grid center (2, 2)
        if (GridManager.Instance != null)
        {
            transform.position = GridManager.Instance.GetTileWorldPosition(gridX, gridY);
        }
    }

    void Update()
    {
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        // Read WASD and Arrow Key inputs
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized; // Prevents diagonal speed boost
    }

    void FixedUpdate()
    {
        // Stop physics movement completely when game is over
        if (GridManager.Instance != null && GridManager.Instance.isGameOver)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Apply normal velocity movement
        rb.linearVelocity = moveInput * moveSpeed;

        // Clamp world position inside grid bounds after moving
        ClampPositionToGrid();
    }

    void ClampPositionToGrid()
    {
        if (GridManager.Instance == null) return;

        // Get world positions of bottom-left (0,0) and top-right (cols-1, rows-1) tiles
        Vector3 minTilePos = GridManager.Instance.GetTileWorldPosition(0, 0);
        Vector3 maxTilePos = GridManager.Instance.GetTileWorldPosition(
            GridManager.Instance.cols - 1,
            GridManager.Instance.rows - 1
        );

        // Calculate world bounds (minTilePos to maxTilePos)
        float minX = minTilePos.x;
        float maxX = maxTilePos.x;
        float minY = minTilePos.y;
        float maxY = maxTilePos.y;

        // Clamp player position so circle center never exits outer tile boundaries
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        transform.position = clampedPos;
    }
}