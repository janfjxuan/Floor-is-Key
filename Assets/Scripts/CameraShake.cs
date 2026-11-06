using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Cache true home position once on launch
        originalPos = transform.localPosition;
    }

    public void Shake(float duration = 0.15f, float magnitude = 0.2f)
    {
        // Cancel any active shake so originalPos stays accurate
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        // Find player position to pull camera inward towards grid center
        Vector3 targetCenter = Vector3.zero;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            targetCenter = player.transform.position;
        }

        while (elapsed < duration)
        {
            // Calculate inward vector towards center
            Vector3 inwardDir = (targetCenter - transform.localPosition).normalized;

            // Dampen shift over duration
            float currentMag = Mathf.Lerp(magnitude, 0f, elapsed / duration);

            // Add inward impulse with slight random jitter
            float jitterX = Random.Range(-0.5f, 0.5f) * currentMag;
            float jitterY = Random.Range(-0.5f, 0.5f) * currentMag;

            Vector3 offset = (inwardDir * currentMag * -0.5f) + new Vector3(jitterX, jitterY, 0f);

            transform.localPosition = originalPos + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap cleanly back to home position
        transform.localPosition = originalPos;
    }
}