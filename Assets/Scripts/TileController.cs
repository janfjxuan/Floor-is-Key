using UnityEngine;

public enum TileState { Neutral, Safe, Lava, Toxic }

public class TileController : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public TileState currentState = TileState.Neutral;
    public int noteIndex = 0;

    private SpriteRenderer spriteRenderer;

    [Header("Item Visuals")]
    public GameObject currentItemObject;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetState(TileState newState, int assignedNote = 0)
    {
        currentState = newState;
        noteIndex = assignedNote;

        switch (newState)
        {
            case TileState.Neutral:
                spriteRenderer.color = new Color(0.1f, 0.1f, 0.18f);
                break;
            case TileState.Safe:
                spriteRenderer.color = new Color(0f, 0.95f, 1f);
                break;
            case TileState.Lava:
                spriteRenderer.color = new Color(1f, 0f, 0.33f);
                break;
            case TileState.Toxic:
                spriteRenderer.color = new Color(0.1f, 0.1f, 0.18f);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GridManager.Instance.OnPlayerStepTile(this);
        }
    }

    public void SetVisualHidden()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.1f, 0.1f, 0.18f);
        }
    }

    public void SpawnToxicItem(GameObject melonPrefab)
    {
        if (melonPrefab == null) return;

        if (currentItemObject != null) Destroy(currentItemObject);

        currentItemObject = Instantiate(melonPrefab, transform.position, Quaternion.identity, transform);
    }

    public void ClearItem()
    {
        if (currentItemObject != null)
        {
            Destroy(currentItemObject);
            currentItemObject = null;
        }
    }
}