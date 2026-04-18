using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAimTelegraph : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.18f, 0.18f, 0.92f);
    [SerializeField] private float startWidth = 0.04f;
    [SerializeField] private float endWidth = 0.11f;
    [SerializeField] private int sortingOrder = 120;

    private static Material sharedMaterial;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        EnsureLine();
        Hide();
    }

    public void Configure(Color color, float start, float end, int order = 120)
    {
        telegraphColor = color;
        startWidth = start;
        endWidth = end;
        sortingOrder = order;
        EnsureLine();
        ApplyStyle();
    }

    public void Show(Vector3 start, Vector3 end)
    {
        EnsureLine();
        ApplyStyle();
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void Hide()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void EnsureLine()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (lineRenderer == null)
            return;

        if (sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                sharedMaterial = new Material(shader);
        }

        if (sharedMaterial != null)
            lineRenderer.material = sharedMaterial;
        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.startColor = telegraphColor;
        lineRenderer.endColor = telegraphColor;
    }
}
