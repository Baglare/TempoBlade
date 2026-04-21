using UnityEngine;

[DisallowMultipleComponent]
public class EnemyOverheadMeter : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private float barLength = 0.95f;
    [SerializeField] private float barWidth = 0.08f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);

    private LineRenderer backgroundLine;
    private LineRenderer fillLine;
    private bool initialized;
    private float currentProgress = 1f;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        backgroundLine = CreateRenderer("Background", 141, backgroundColor);
        fillLine = CreateRenderer("Fill", 142, Color.white);
        SetVisible(false);
    }

    public void Configure(Color fillColor, float length = 0.95f, float width = 0.08f)
    {
        Initialize();
        barLength = Mathf.Max(0.2f, length);
        barWidth = Mathf.Max(0.02f, width);
        backgroundLine.startWidth = barWidth;
        backgroundLine.endWidth = barWidth;
        fillLine.startWidth = barWidth * 0.72f;
        fillLine.endWidth = barWidth * 0.72f;
        fillLine.startColor = fillColor;
        fillLine.endColor = fillColor;
        UpdateBar(1f);
    }

    public void SetVisible(bool visible)
    {
        Initialize();
        backgroundLine.enabled = visible;
        fillLine.enabled = visible;
    }

    public void SetProgress(float progress01)
    {
        Initialize();
        currentProgress = Mathf.Clamp01(progress01);
        UpdateBar(currentProgress);
    }

    private void LateUpdate()
    {
        if (!initialized || !backgroundLine.enabled)
            return;

        Vector3 center = transform.position + offset;
        Vector3 start = center + Vector3.left * (barLength * 0.5f);
        Vector3 end = center + Vector3.right * (barLength * 0.5f);

        backgroundLine.SetPosition(0, start);
        backgroundLine.SetPosition(1, end);
        fillLine.SetPosition(0, start);
        fillLine.SetPosition(1, Vector3.Lerp(start, end, currentProgress));
    }

    private void UpdateBar(float progress01)
    {
        Vector3 center = transform.position + offset;
        Vector3 start = center + Vector3.left * (barLength * 0.5f);
        Vector3 end = center + Vector3.right * (barLength * 0.5f);
        backgroundLine.SetPosition(0, start);
        backgroundLine.SetPosition(1, end);
        fillLine.SetPosition(0, start);
        fillLine.SetPosition(1, Vector3.Lerp(start, end, progress01));
    }

    private LineRenderer CreateRenderer(string objectName, int sortingOrder, Color color)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        LineRenderer renderer = child.AddComponent<LineRenderer>();
        renderer.positionCount = 2;
        renderer.useWorldSpace = true;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = sortingOrder;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            renderer.material = new Material(shader);
        renderer.startColor = color;
        renderer.endColor = color;
        return renderer;
    }
}
