using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyStateFeedback : MonoBehaviour
{
    [Header("Ring")]
    [SerializeField] private float markerHeight = 1.1f;
    [SerializeField] private float markerRadius = 0.12f;
    [SerializeField] private float markerWidth = 0.04f;
    [SerializeField] private int markerSegments = 20;
    [SerializeField] private float executeReadyPulseSpeed = 7f;
    [SerializeField] private float executeReadyPulseAlpha = 0.45f;

    [Header("Tint Pulse")]
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private float tintPulseInDuration = 0.05f;
    [SerializeField] private float tintPulseOutDuration = 0.18f;
    [SerializeField] [Range(0f, 1f)] private float tintPulseStrength = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color stunColor = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color staggerColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color guardBreakColor = new Color(1f, 0.28f, 0.18f, 1f);
    [SerializeField] private Color brokenColor = new Color(1f, 0.35f, 0.1f, 1f);
    [SerializeField] private Color armorColor = new Color(0.75f, 0.75f, 0.85f, 1f);
    [SerializeField] private Color guardColor = new Color(0.15f, 0.9f, 1f, 1f);
    [SerializeField] private Color executeReadyColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color executedColor = new Color(1f, 0.1f, 0.1f, 1f);

    private LineRenderer ringRenderer;
    private Material runtimeMaterial;
    private Coroutine tintPulseRoutine;
    private EnemyStateFeedbackType activeState = EnemyStateFeedbackType.None;
    private float activeStateEndTime;
    private bool executeReadyActive;

    public static EnemyStateFeedback EnsureFor(GameObject target)
    {
        if (target == null)
            return null;

        EnemyStateFeedback feedback = target.GetComponent<EnemyStateFeedback>();
        if (feedback == null)
            feedback = target.AddComponent<EnemyStateFeedback>();

        return feedback;
    }

    private void Awake()
    {
        if (targetSprite == null)
            targetSprite = GetComponentInChildren<SpriteRenderer>();

        EnsureRingRenderer();
        ApplyVisualState(EnemyStateFeedbackType.None);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    private void Update()
    {
        if (activeState != EnemyStateFeedbackType.None && Time.time >= activeStateEndTime)
            activeState = EnemyStateFeedbackType.None;

        EnemyStateFeedbackType displayState = GetDisplayState();
        ApplyVisualState(displayState);
    }

    public void ShowState(EnemyStateFeedbackType state, float duration)
    {
        if (state == EnemyStateFeedbackType.None)
            return;

        if (state == EnemyStateFeedbackType.Executed)
            executeReadyActive = false;

        activeState = state;
        activeStateEndTime = Time.time + Mathf.Max(0.05f, duration);
        TriggerTintPulse(GetColorForState(state));
        ApplyVisualState(GetDisplayState());
    }

    public void SetExecuteReady(bool active)
    {
        if (executeReadyActive == active)
            return;

        executeReadyActive = active;
        if (active)
            TriggerTintPulse(GetColorForState(EnemyStateFeedbackType.ExecuteReady));
        else if (GetDisplayState() == EnemyStateFeedbackType.None)
            ApplyVisualState(EnemyStateFeedbackType.None);
    }

    private EnemyStateFeedbackType GetDisplayState()
    {
        if (activeState != EnemyStateFeedbackType.None && Time.time < activeStateEndTime)
            return activeState;

        return executeReadyActive ? EnemyStateFeedbackType.ExecuteReady : EnemyStateFeedbackType.None;
    }

    private void EnsureRingRenderer()
    {
        if (ringRenderer != null)
            return;

        GameObject ringObject = new GameObject("EnemyStateRing");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, markerHeight, 0f);

        ringRenderer = ringObject.AddComponent<LineRenderer>();
        ringRenderer.useWorldSpace = false;
        ringRenderer.loop = true;
        ringRenderer.positionCount = Mathf.Max(12, markerSegments);
        ringRenderer.widthMultiplier = markerWidth;
        ringRenderer.numCapVertices = 4;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;
        ringRenderer.textureMode = LineTextureMode.Stretch;
        ringRenderer.alignment = LineAlignment.TransformZ;
        ringRenderer.sortingLayerName = WorldSortingUtility.ResolveLayerName(WorldSortingLayers.WorldUI);
        ringRenderer.sortingOrder = 100;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
            ringRenderer.material = runtimeMaterial;
        }

        RebuildRingPoints();
    }

    private void RebuildRingPoints()
    {
        if (ringRenderer == null)
            return;

        int count = ringRenderer.positionCount;
        for (int i = 0; i < count; i++)
        {
            float t = (i / (float)count) * Mathf.PI * 2f;
            ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(t) * markerRadius, Mathf.Sin(t) * markerRadius, 0f));
        }
    }

    private void ApplyVisualState(EnemyStateFeedbackType state)
    {
        if (ringRenderer == null)
            return;

        if (state == EnemyStateFeedbackType.None)
        {
            ringRenderer.enabled = false;
            return;
        }

        Color color = GetColorForState(state);
        ringRenderer.enabled = true;

        if (state == EnemyStateFeedbackType.ExecuteReady)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * executeReadyPulseSpeed) * 0.5f;
            color.a = Mathf.Lerp(executeReadyPulseAlpha * 0.6f, executeReadyPulseAlpha, pulse);
        }

        ringRenderer.startColor = color;
        ringRenderer.endColor = color;
    }

    private Color GetColorForState(EnemyStateFeedbackType state)
    {
        switch (state)
        {
            case EnemyStateFeedbackType.Stun:
                return stunColor;
            case EnemyStateFeedbackType.Stagger:
                return staggerColor;
            case EnemyStateFeedbackType.GuardBreak:
                return guardBreakColor;
            case EnemyStateFeedbackType.Broken:
                return brokenColor;
            case EnemyStateFeedbackType.Armor:
                return armorColor;
            case EnemyStateFeedbackType.Guard:
                return guardColor;
            case EnemyStateFeedbackType.ExecuteReady:
                return executeReadyColor;
            case EnemyStateFeedbackType.Executed:
                return executedColor;
            default:
                return Color.white;
        }
    }

    private void TriggerTintPulse(Color targetColor)
    {
        if (targetSprite == null)
            return;

        if (tintPulseRoutine != null)
            StopCoroutine(tintPulseRoutine);

        tintPulseRoutine = StartCoroutine(TintPulseRoutine(targetColor));
    }

    private IEnumerator TintPulseRoutine(Color targetColor)
    {
        Color baseColor = targetSprite.color;
        Color pulseColor = Color.Lerp(baseColor, targetColor, tintPulseStrength);

        float elapsed = 0f;
        while (elapsed < tintPulseInDuration)
        {
            elapsed += Time.deltaTime;
            float t = tintPulseInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / tintPulseInDuration);
            targetSprite.color = Color.Lerp(baseColor, pulseColor, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < tintPulseOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = tintPulseOutDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / tintPulseOutDuration);
            targetSprite.color = Color.Lerp(pulseColor, baseColor, t);
            yield return null;
        }

        targetSprite.color = baseColor;
        tintPulseRoutine = null;
    }
}
