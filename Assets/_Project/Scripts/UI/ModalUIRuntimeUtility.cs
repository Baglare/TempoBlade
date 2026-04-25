using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ModalUIRuntimeUtility
{
    public static readonly Color OverlayColor = new Color(0.03f, 0.04f, 0.07f, 0.82f);
    public static readonly Color SectionColor = new Color(0.12f, 0.14f, 0.19f, 0.96f);
    public static readonly Color SectionAltColor = new Color(0.09f, 0.11f, 0.16f, 0.96f);
    public static readonly Color HeaderTextColor = new Color(0.96f, 0.97f, 1f, 1f);
    public static readonly Color BodyTextColor = new Color(0.89f, 0.92f, 0.98f, 1f);
    public static readonly Color CloseButtonColor = new Color(0.76f, 0.20f, 0.20f, 0.97f);

    public static void EnsureFullscreenCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 120);

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Stretch(canvasRect);
    }

    public static RectTransform CreateOrGetOverlayRoot(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            Stretch(existingRect);
            return existingRect;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = OverlayColor;
        image.raycastTarget = true;

        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        return rect;
    }

    public static RectTransform CreateCard(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    public static RectTransform CreateSection(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;

        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 1f;
        return rect;
    }

    public static TextMeshProUGUI CreateTitle(RectTransform parent, string title)
    {
        GameObject go = new GameObject("ModalTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.color = HeaderTextColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 40f);
        rect.anchoredPosition = new Vector2(0f, -2f);
        return text;
    }

    public static Button CreateCloseButton(RectTransform parent, UnityAction onClick)
    {
        GameObject buttonGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        Image image = buttonGo.GetComponent<Image>();
        image.color = CloseButtonColor;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(42f, 42f);
        rect.anchoredPosition = new Vector2(-6f, 0f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI label = textGo.GetComponent<TextMeshProUGUI>();
        label.text = "X";
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);

        return button;
    }

    public static RectTransform CreateScrollableList(RectTransform parent, string name, RectTransform existingContent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.05f);
        root.GetComponent<Mask>().showMaskGraphic = false;

        LayoutElement layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;
        layoutElement.minHeight = 220f;

        RectTransform viewport = root.GetComponent<RectTransform>();
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.sizeDelta = new Vector2(0f, 280f);

        StretchHorizontally(viewport);

        if (existingContent != null)
        {
            existingContent.SetParent(viewport, false);
            StretchHorizontally(existingContent);
            existingContent.anchorMin = new Vector2(0f, 1f);
            existingContent.anchorMax = new Vector2(1f, 1f);
            existingContent.pivot = new Vector2(0.5f, 1f);
            existingContent.anchoredPosition = Vector2.zero;

            ContentSizeFitter fitter = existingContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = existingContent.gameObject.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup layout = existingContent.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = existingContent.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
        }

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = existingContent;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        return viewport;
    }

    public static RectTransform Wrap(RectTransform target, RectTransform parent, string wrapperName, float minHeight = 0f)
    {
        if (target == null)
            return null;

        GameObject wrapperGo = new GameObject(wrapperName, typeof(RectTransform), typeof(LayoutElement));
        wrapperGo.transform.SetParent(parent, false);

        RectTransform wrapperRect = wrapperGo.GetComponent<RectTransform>();
        StretchHorizontally(wrapperRect);

        LayoutElement layout = wrapperGo.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        if (minHeight > 0f)
            layout.minHeight = minHeight;

        target.SetParent(wrapperRect, false);
        Stretch(target);
        target.offsetMin = new Vector2(0f, 0f);
        target.offsetMax = new Vector2(0f, 0f);
        target.localScale = Vector3.one;
        return wrapperRect;
    }

    public static RectTransform CreateRow(RectTransform parent, string name, float minHeight = 0f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        if (minHeight > 0f)
            le.minHeight = minHeight;

        RectTransform rect = go.GetComponent<RectTransform>();
        StretchHorizontally(rect);
        return rect;
    }

    public static void NormalizeText(TextMeshProUGUI text, bool allowWrap = true, float minHeight = 0f)
    {
        if (text == null)
            return;

        text.textWrappingMode = allowWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = BodyTextColor;

        RectTransform rect = text.rectTransform;
        rect.localScale = Vector3.one;

        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null)
            layout = text.gameObject.AddComponent<LayoutElement>();

        layout.flexibleWidth = 1f;
        if (minHeight > 0f)
            layout.minHeight = minHeight;
    }

    public static void NormalizeButton(Button button, float minHeight = 48f)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
            layout = button.gameObject.AddComponent<LayoutElement>();

        layout.flexibleWidth = 1f;
        layout.minHeight = minHeight;
    }

    public static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    public static void StretchHorizontally(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
        rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
        rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
        rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        rect.localScale = Vector3.one;
    }
}
