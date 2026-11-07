using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float fadeDuration = 0.8f;

    private TextMeshPro tmp;
    private Color originalColor;
    private float timer = 0f;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            originalColor = tmp.color;
        }
    }

    void Update()
    {
        // Float upward in world space
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        timer += Time.deltaTime;

        if (tmp != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}