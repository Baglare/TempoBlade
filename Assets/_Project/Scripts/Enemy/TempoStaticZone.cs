using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TempoStaticZone : MonoBehaviour
{
    [Header("Gameplay")]
    public float duration = 4f;
    public float radius = 2.5f;
    public float tempoGainMultiplier = 0.7f;
    public float tempoDecayMultiplier = 1.35f;

    [Header("Visual")]
    [SerializeField] private Color zoneColor = new Color(0.2f, 0.95f, 1f, 0.8f);
    [SerializeField] private float lineWidth = 0.06f;
    [SerializeField] private int segmentCount = 32;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float alphaPulse = 0.22f;

    private CircleCollider2D triggerCollider;
    private LineRenderer lineRenderer;
    private PlayerTempoDisruptionController playerController;

    public void Configure(float zoneDuration, float zoneRadius, float gainMultiplier, float decayMultiplier)
    {
        duration = zoneDuration;
        radius = zoneRadius;
        tempoGainMultiplier = gainMultiplier;
        tempoDecayMultiplier = decayMultiplier;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = radius;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = Mathf.Max(12, segmentCount);
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.sortingOrder = 90;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);

        RebuildCircle();
        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        if (lineRenderer == null)
            return;

        Color displayColor = zoneColor;
        float pulse = 0.5f + Mathf.Sin(Time.time * pulseSpeed) * 0.5f;
        displayColor.a = Mathf.Clamp01(zoneColor.a - alphaPulse + pulse * alphaPulse);
        lineRenderer.startColor = displayColor;
        lineRenderer.endColor = displayColor;
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerController = PlayerTempoDisruptionController.EnsureFor(other.gameObject);
        playerController?.RegisterZone(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerTempoDisruptionController controller = other.GetComponent<PlayerTempoDisruptionController>();
        controller?.UnregisterZone(this);
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.UnregisterZone(this);
    }

    private void RebuildCircle()
    {
        if (lineRenderer == null)
            return;

        int count = lineRenderer.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = (i / (float)count) * Mathf.PI * 2f;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
        }

        if (triggerCollider != null)
            triggerCollider.radius = radius;
    }
}
