using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UIRoundedBox : MaskableGraphic
{
    [Header("Box Dimensions & Style")]
    public float cornerRadius = 16f;
    public Color borderColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
    public float borderWidth = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        Color32 fillColor = color;
        Color32 strokeColor = borderColor;

        // Draw outer border box first
        if (borderWidth > 0)
        {
            AddRoundedRect(vh, rect, cornerRadius, strokeColor);
        }

        // Draw inner fill box shrunk by borderWidth
        Rect innerRect = new Rect(
            rect.x + borderWidth,
            rect.y + borderWidth,
            rect.width - (borderWidth * 2),
            rect.height - (borderWidth * 2)
        );

        float innerRadius = Mathf.Max(0, cornerRadius - borderWidth);
        AddRoundedRect(vh, innerRect, innerRadius, fillColor);
    }

    private void AddRoundedRect(VertexHelper vh, Rect r, float radius, Color32 col)
    {
        float minSize = Mathf.Min(r.width, r.height) * 0.5f;
        radius = Mathf.Clamp(radius, 0, minSize);

        int startIndex = vh.currentVertCount;

        vh.AddVert(new Vector3(r.xMin, r.yMin + radius), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax - radius), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + radius, r.yMax), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax - radius, r.yMax), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax - radius), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin + radius), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax - radius, r.yMin), col, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin + radius, r.yMin), col, Vector2.zero);

        // Fan triangles from center
        int centerIndex = vh.currentVertCount;
        vh.AddVert(new Vector3(r.center.x, r.center.y), col, Vector2.zero);

        for (int i = 0; i < 8; i++)
        {
            int next = (i + 1) % 8;
            vh.AddTriangle(centerIndex, startIndex + i, startIndex + next);
        }
    }
}