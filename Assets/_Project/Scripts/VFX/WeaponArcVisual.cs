using UnityEngine;

/// <summary>
/// Kılıç Transform'unu saldırı menzilinin ucuna konumlandırır ve
/// saldırı yayını bir LineRenderer ile görselleştirir.
///
/// Kurulum (Inspector'dan):
///   1. Player veya Enemy altında boş bir child GameObject oluştur ("WeaponArcVisual").
///   2. Bu component'i o child'a ekle.
///   3. weaponTransform: Kılıç sprite'ının Transform'unu ata (opsiyonel).
///   4. PlayerCombat / EnemyMelee gibi script'te UpdateVisuals() çağır.
///
/// Silah Konumlama:
///   Kılıç, karakter merkezinden <range> birim uzağa (saldırı yönünde) konumlandırılır.
///   Bu, "attack range 3 ise kılıç ucu 3 birim uzakta" demektir.
/// </summary>
public enum WeaponArcPresentationMode
{
    DebugOnly,
    PresentationOnly,
    DebugAndPresentation
}

public class WeaponArcVisual : MonoBehaviour
{
    [Header("Kılıç Transform")]
    [Tooltip("Kılıç sprite'ının Transform'u. Menzil mesafesine, saldırı yönünde konumlandırılır. Null ise sadece yay çizilir.")]
    public Transform weaponTransform;

    [Tooltip("Sprite varsayılan yönüne göre açı düzeltmesi. Sprite 'yukarı' bakıyorsa -90, 'sağa' bakıyorsa 0.")]
    public float weaponRotationOffset = -90f;

    [Tooltip("Sprite pivot'u ile kılıç ucu arasındaki mesafe (world unit). Kılıç ucunun tam arc kenarına oturması için sprite'ın yarı boy uzunluğuna eşit ayarlayın. Örn: sprite 1 unit uzunsa 0.5 gir.")]
    public float weaponTipOffset = 0f;

    [Header("Yay Görselleştirmesi")]
    [Tooltip("Saldırı menzili (world unit). Bağlı script'ten Start()'ta otomatik atanır; Inspector'dan da override edilebilir.")]
    public float range = 1f;

    [Tooltip("Yayın toplam açısı (derece). Kılıcın taradığı alanı temsil eder.")]
    public float arcAngle = 70f;

    [Tooltip("Yay üzerindeki segment sayısı (daha yüksek = daha yumuşak yay).")]
    public int arcSegments = 20;

    [Tooltip("Pasif (hedef almak) hâlinde yay rengi.")]
    public Color arcColorIdle = new Color(1f, 0.5f, 0f, 0.35f);
    public bool showPreviewWhenInactive = false;
    [Range(0f, 1f)] public float previewOpacity = 0.18f;

    [Header("Preview / Active Feel")]
    [Tooltip("Mouse aim preview arc'i. Karakter facing'ini degistirmez.")]
    public bool previewEnabled = false;
    [Range(0f, 1f)] public float previewAlpha = 0.18f;
    [Range(0f, 1f)] public float activeAlpha = 0.9f;
    [Tooltip("0'dan buyukse attack/parry sinyali bittikten sonra active arc'i bu sure kadar tutar.")]
    public float activeDurationOverride = -1f;
    public Color arcColor = new Color(1f, 0.2f, 0f, 1f);
    public Color parryArcColor = new Color(0f, 0.8f, 1f, 1f);
    [Tooltip("V1 hook. Su an alpha dogrudan uygulanir, fade ayari ileride presentation tarafinda kullanilabilir.")]
    public float fadeSpeed = 20f;

    [Tooltip("Aktif saldırı anında yay rengi.")]
    public Color arcColorActive = new Color(1f, 0.2f, 0f, 0.9f);

    [Tooltip("Parry anında yay rengi.")]
    public Color arcColorParry = new Color(0f, 0.8f, 1f, 0.6f);
    [Tooltip("Perfect parry penceresinde yay rengi.")]
    public Color arcColorPerfectParry = new Color(1f, 0.9f, 0.25f, 0.95f);
    [Tooltip("Deflect edge bandi icin ic sinir rengi.")]
    public Color deflectEdgeColor = new Color(0.2f, 0.95f, 1f, 0.85f);
    [Tooltip("Perfect pencerede deflect edge rengi.")]
    public Color perfectDeflectEdgeColor = Color.white;
    [Tooltip("Yay çizgisinin genişliği (world unit).")]
    public float lineWidth = 0.07f;
    [Tooltip("Ic edge pass cizgisi icin genislik carpani.")]
    public float deflectEdgeWidthMultiplier = 0.7f;

    [Tooltip("LineRenderer için materyal. Atanmazsa Sprites/Default kullanılır. URP'de çalışmıyorsa Inspector'dan URP uyumlu bir materyal ata.")]
    public Material arcMaterial;

    [Header("Presentation Hooks")]
    public WeaponArcPresentationMode presentationMode = WeaponArcPresentationMode.DebugAndPresentation;
    public AttackVFXPresenter attackVfxPresenter;

    // ------------------------------------------------------------------ //
    private LineRenderer lr;
    private Vector2 lastDirection = Vector2.right;
    private Material runtimeMaterial;
    private float activeUntil = -999f;
    private float baseArcAngle; // Inspector'dan girilen orijinal acı

    // ------------------------------------------------------------------ //

    private void Awake()
    {
        baseArcAngle = arcAngle;
        lr = GetComponent<LineRenderer>();
        if (lr == null)
            lr = gameObject.AddComponent<LineRenderer>();
        InitLineRenderer();
    }

    private void InitLineRenderer()
    {
        ConfigureLineRenderer(lr, arcSegments + 1, 5);
    }

    private void ConfigureLineRenderer(LineRenderer target, int positions, int sortingOrder)
    {
        target.useWorldSpace = true;
        target.startWidth = lineWidth;
        target.endWidth = lineWidth;
        target.positionCount = positions;
        target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        target.receiveShadows = false;

        if (arcMaterial != null)
        {
            target.material = arcMaterial;
        }
        else
        {
            if (runtimeMaterial == null)
                runtimeMaterial = new Material(Shader.Find("Sprites/Default"));

            target.material = runtimeMaterial;
        }

        target.sortingLayerName = WorldSortingUtility.ResolveLayerName(WorldSortingLayers.CharacterVFX);
        target.sortingOrder = sortingOrder;
        target.enabled = false;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    // ================================================================== //
    // PUBLIC API
    // ================================================================== //

    public void UpdateVisuals(
        Vector2 origin,
        Vector2 dir,
        bool isAttacking,
        bool isParrying,
        float overrideAngle = -1f,
        float parryEdgeThickness = -1f,
        bool showDeflectEdge = false,
        bool isPerfectWindow = false)
    {
        if (overrideAngle > 0f)
            arcAngle = overrideAngle;
        else
            arcAngle = baseArcAngle;

        if (dir.sqrMagnitude > 0.0001f)
            lastDirection = dir.normalized;

        bool isActive = isAttacking || isParrying;
        if (isActive && activeDurationOverride > 0f)
            activeUntil = Mathf.Max(activeUntil, Time.time + activeDurationOverride);

        bool showActiveArc = isActive || Time.time < activeUntil;
        bool showPreview = (previewEnabled || showPreviewWhenInactive) && !showActiveArc;
        bool showDebugArc = presentationMode != WeaponArcPresentationMode.PresentationOnly;
        lr.enabled = (showActiveArc || showPreview) && showDebugArc;
        if ((showActiveArc || showPreview) && showDebugArc)
            DrawArc(origin, range, isAttacking || (showActiveArc && !isParrying), isParrying, isPerfectWindow, showPreview);

        if (attackVfxPresenter == null)
            attackVfxPresenter = GetComponent<AttackVFXPresenter>();

        if (attackVfxPresenter != null && presentationMode != WeaponArcPresentationMode.DebugOnly)
        {
            attackVfxPresenter.UpdatePresentation(new AttackPresentationContext
            {
                origin = origin,
                direction = lastDirection,
                range = range,
                arcAngle = arcAngle,
                isAttacking = isAttacking,
                isParrying = isParrying,
                isPerfectWindow = isPerfectWindow
            });
        }
        
        // Eğer parry yapılıyorsa silahı ileri fırlatma (PositionWeapon'ı atla), sadece saldırıda silahı konumlandır
        if (isAttacking)
        {
            PositionWeapon(origin, range);
            if (weaponTransform != null) weaponTransform.gameObject.SetActive(true);
        }
        else
        {
            if (weaponTransform != null) weaponTransform.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Yayı ve kılıcı gizler / gösterir.
    /// </summary>
    public void SetVisible(bool visible)
    {
        lr.enabled = visible;
        if (weaponTransform != null)
            weaponTransform.gameObject.SetActive(visible);
    }

    // ================================================================== //
    // PRIVATE HELPERS
    // ================================================================== //

    private void PositionWeapon(Vector2 origin, float range)
    {
        if (weaponTransform == null) return;

        // weaponTipOffset: sprite'ın pivot'u ile ucu arasındaki mesafe.
        // Ucu tam arc kenarına hizalamak için sprite merkezini range'den geri çekiyoruz.
        Vector2 tip = origin + lastDirection * (range - weaponTipOffset);
        weaponTransform.position = new Vector3(tip.x, tip.y, weaponTransform.position.z);

        float angle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
        weaponTransform.rotation = Quaternion.Euler(0f, 0f, angle + weaponRotationOffset);
    }

    private void DrawArc(Vector2 origin, float range, bool isAttacking, bool isParrying, bool isPerfectWindow, bool isPreview = false)
    {
        Color c = arcColorIdle;
        if (isAttacking)
        {
            c = arcColor;
            c.a = activeAlpha;
        }
        else if (isParrying)
        {
            c = isPerfectWindow ? arcColorPerfectParry : parryArcColor;
            c.a = activeAlpha;
        }
        else if (isPreview)
        {
            c.a = previewEnabled ? previewAlpha : previewOpacity;
        }

        lr.startColor = c;
        lr.endColor = c;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        float halfAngle = arcAngle * 0.5f;
        float baseAngleDeg = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float rad = (baseAngleDeg - halfAngle + t * arcAngle) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(
                origin.x + Mathf.Cos(rad) * range,
                origin.y + Mathf.Sin(rad) * range,
                0f
            ));
        }
    }

}
