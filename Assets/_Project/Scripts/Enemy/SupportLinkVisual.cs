using UnityEngine;

[DisallowMultipleComponent]
public class SupportLinkVisual : MonoBehaviour
{
    [SerializeField] private Color linkColor = new Color(0.2f, 1f, 0.55f, 0.9f);
    [SerializeField] private float width = 0.06f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float alphaPulse = 0.25f;

    private LineRenderer lineRenderer;
    private Transform target;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = width;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingOrder = 95;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);

        lineRenderer.enabled = false;
    }

    public void SetTarget(Transform nextTarget)
    {
        target = nextTarget;
        if (lineRenderer != null)
            lineRenderer.enabled = target != null;
    }

    private void Update()
    {
        if (lineRenderer == null || target == null)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, target.position);

        Color displayColor = linkColor;
        float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        displayColor.a = Mathf.Clamp01(linkColor.a - alphaPulse + pulse * alphaPulse);
        lineRenderer.startColor = displayColor;
        lineRenderer.endColor = displayColor;
    }
}
