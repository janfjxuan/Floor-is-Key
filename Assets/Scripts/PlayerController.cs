using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 7f;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Read WASD and Arrow Key inputs
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized; // Prevents diagonal speed boost
    }

    void FixedUpdate()
    {
        // Direct velocity control for instant start/stop movement
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
