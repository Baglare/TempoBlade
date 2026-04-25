using UnityEngine;

/// <summary>
/// Lightweight 2-panel responsive splitter.
/// Avoids LayoutGroup conflicts by placing the first two child panels manually.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ResponsiveSplitLayout : MonoBehaviour
{
    public float stackBelowWidth = 1320f;
    public float desktopSpacing = 24f;
    public float stackedSpacing = 18f;
    public bool keepFirstSectionWiderOnDesktop = false;
    public float firstSectionFlexibleWidth = 1.15f;
    public float secondSectionFlexibleWidth = 1f;

    private RectTransform rectTransform;
    private float lastWidth = -1f;
    private bool lastStackedState;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh(true);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        if (rectTransform == null)
            return;

        float width = rectTransform.rect.width;
        if (!force && Mathf.Abs(width - lastWidth) < 2f)
            return;

        lastWidth = width;
        bool shouldStack = width > 0f && width <= stackBelowWidth;
        lastStackedState = shouldStack;

        RectTransform first = rectTransform.childCount > 0 ? rectTransform.GetChild(0) as RectTransform : null;
        RectTransform second = rectTransform.childCount > 1 ? rectTransform.GetChild(1) as RectTransform : null;

        if (first == null)
            return;

        if (second == null)
        {
            Stretch(first);
            return;
        }

        if (shouldStack)
            ApplyStackedLayout(first, second);
        else
            ApplyDesktopLayout(first, second);
    }

    private void ApplyDesktopLayout(RectTransform first, RectTransform second)
    {
        float totalWeight = Mathf.Max(0.01f, firstSectionFlexibleWidth + secondSectionFlexibleWidth);
        float firstWidth = keepFirstSectionWiderOnDesktop
            ? firstSectionFlexibleWidth / totalWeight
            : 0.5f;

        float secondWidth = 1f - firstWidth;
        float halfGap = desktopSpacing * 0.5f;

        first.anchorMin = new Vector2(0f, 0f);
        first.anchorMax = new Vector2(firstWidth, 1f);
        first.offsetMin = new Vector2(0f, 0f);
        first.offsetMax = new Vector2(-halfGap, 0f);
        first.pivot = new Vector2(0.5f, 0.5f);
        first.localScale = Vector3.one;

        second.anchorMin = new Vector2(firstWidth, 0f);
        second.anchorMax = new Vector2(1f, 1f);
        second.offsetMin = new Vector2(halfGap, 0f);
        second.offsetMax = new Vector2(0f, 0f);
        second.pivot = new Vector2(0.5f, 0.5f);
        second.localScale = Vector3.one;
    }

    private void ApplyStackedLayout(RectTransform first, RectTransform second)
    {
        float halfGap = stackedSpacing * 0.5f;

        first.anchorMin = new Vector2(0f, 0.5f);
        first.anchorMax = new Vector2(1f, 1f);
        first.offsetMin = new Vector2(0f, halfGap);
        first.offsetMax = new Vector2(0f, 0f);
        first.pivot = new Vector2(0.5f, 0.5f);
        first.localScale = Vector3.one;

        second.anchorMin = new Vector2(0f, 0f);
        second.anchorMax = new Vector2(1f, 0.5f);
        second.offsetMin = new Vector2(0f, 0f);
        second.offsetMax = new Vector2(0f, -halfGap);
        second.pivot = new Vector2(0.5f, 0.5f);
        second.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }
}
