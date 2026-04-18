using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TemporaryEnemySummon : MonoBehaviour
{
    [SerializeField] private float duration = 7.5f;
    [SerializeField] private float temporaryAlpha = 0.72f;
    [SerializeField] private Vector3 sliderOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private Vector2 sliderSize = new Vector2(0.9f, 0.14f);

    private float expireTime;
    private EnemyBase enemyBase;
    private SpriteRenderer[] spriteRenderers;
    private Slider lifetimeSlider;
    private Canvas sliderCanvas;
    private bool initialized;

    public void Configure(float summonDuration, float alpha)
    {
        duration = summonDuration;
        temporaryAlpha = alpha;

        if (initialized)
        {
            expireTime = Time.time + duration;
            ApplyTemporaryVisual();
        }
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        initialized = true;
        enemyBase = GetComponent<EnemyBase>();
        if (enemyBase != null)
            enemyBase.SetSuppressDeathRewards(true);

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        ApplyTemporaryVisual();
        CreateLifetimeSlider();

        expireTime = Time.time + duration;
        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        if (lifetimeSlider != null)
            lifetimeSlider.value = Mathf.Clamp01((expireTime - Time.time) / Mathf.Max(0.01f, duration));

        if (sliderCanvas != null && Camera.main != null)
            sliderCanvas.transform.forward = Camera.main.transform.forward;
    }

    private IEnumerator LifetimeRoutine()
    {
        while (Time.time < expireTime)
            yield return null;
        Destroy(gameObject);
    }

    private void ApplyTemporaryVisual()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null)
                continue;

            Color color = sr.color;
            color.a = Mathf.Min(color.a, temporaryAlpha);
            sr.color = color;
        }
    }

    private void CreateLifetimeSlider()
    {
        GameObject canvasObject = new GameObject("SummonLifetimeCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = sliderOffset;

        sliderCanvas = canvasObject.AddComponent<Canvas>();
        sliderCanvas.renderMode = RenderMode.WorldSpace;
        sliderCanvas.sortingOrder = 120;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = sliderSize;

        GameObject sliderObject = new GameObject("LifetimeSlider");
        sliderObject.transform.SetParent(canvasObject.transform, false);
        lifetimeSlider = sliderObject.AddComponent<Slider>();
        lifetimeSlider.minValue = 0f;
        lifetimeSlider.maxValue = 1f;
        lifetimeSlider.value = 1f;
        lifetimeSlider.direction = Slider.Direction.LeftToRight;
        lifetimeSlider.transition = Selectable.Transition.None;

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = sliderSize;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(0.02f, 0.02f);
        fillAreaRect.offsetMax = new Vector2(-0.02f, -0.02f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.4f, 1f, 1f, 0.85f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        lifetimeSlider.fillRect = fillRect;
        lifetimeSlider.targetGraphic = fillImage;
    }
}
