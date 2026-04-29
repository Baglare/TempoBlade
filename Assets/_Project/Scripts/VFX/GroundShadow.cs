using UnityEngine;

[DisallowMultipleComponent]
public class GroundShadow : MonoBehaviour
{
    private const int TextureSize = 64;
    private static Sprite sharedOvalSprite;

    [Header("Follow")]
    public Transform followRoot;
    public Vector2 localOffset = new Vector2(0f, -0.18f);

    [Header("Visual")]
    public Vector2 scale = new Vector2(0.9f, 0.32f);
    [Range(0f, 1f)] public float opacity = 0.35f;
    public int sortingOffset = -20;

    private SpriteRenderer shadowRenderer;

    private void Awake()
    {
        EnsureRenderer();
        ApplyVisuals();
    }

    private void OnEnable()
    {
        EnsureRenderer();
        ApplyVisuals();
        UpdatePosition();
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void OnValidate()
    {
        scale.x = Mathf.Max(0.01f, scale.x);
        scale.y = Mathf.Max(0.01f, scale.y);

        if (shadowRenderer == null)
        {
            Transform existing = transform.Find("IsoGroundShadow");
            if (existing != null)
                shadowRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (shadowRenderer != null)
        {
            ApplyVisuals();
            UpdatePosition();
        }
    }

    private void EnsureRenderer()
    {
        if (shadowRenderer != null)
            return;

        Transform existing = transform.Find("IsoGroundShadow");
        GameObject shadowObject = existing != null ? existing.gameObject : new GameObject("IsoGroundShadow");
        if (existing == null)
            shadowObject.transform.SetParent(transform, false);

        shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();

        shadowRenderer.sprite = GetOvalSprite();
    }

    private void ApplyVisuals()
    {
        if (shadowRenderer == null)
            return;

        shadowRenderer.color = new Color(0f, 0f, 0f, opacity);
        shadowRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        WorldSortingUtility.ApplySorting(shadowRenderer, WorldSortingLayers.GroundVFX, sortingOffset);
    }

    private void UpdatePosition()
    {
        if (shadowRenderer == null)
            return;

        Transform root = followRoot != null ? followRoot : transform;
        shadowRenderer.transform.position = root.position + (Vector3)localOffset;
        shadowRenderer.transform.rotation = Quaternion.identity;
    }

    private static Sprite GetOvalSprite()
    {
        if (sharedOvalSprite != null)
            return sharedOvalSprite;

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "RuntimeOvalShadow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2((TextureSize - 1) * 0.5f, (TextureSize - 1) * 0.5f);
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d = Vector2.Distance(p, center) / (TextureSize * 0.5f);
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.35f, 1f, d));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        sharedOvalSprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        sharedOvalSprite.name = "RuntimeOvalShadowSprite";
        return sharedOvalSprite;
    }
}
