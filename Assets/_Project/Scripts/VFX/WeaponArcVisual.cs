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

    [Tooltip("Aktif saldırı anında yay rengi.")]
    public Color arcColorActive = new Color(1f, 0.2f, 0f, 0.9f);

    [Tooltip("Parry anında yay rengi.")]
    public Color arcColorParry = new Color(0f, 0.8f, 1f, 0.6f);
    [Tooltip("Yay çizgisinin genişliği (world unit).")]
    public float lineWidth = 0.07f;

    [Tooltip("LineRenderer için materyal. Atanmazsa Sprites/Default kullanılır. URP'de çalışmıyorsa Inspector'dan URP uyumlu bir materyal ata.")]
    public Material arcMaterial;

    // ------------------------------------------------------------------ //
    private LineRenderer lr;
    private Vector2 lastDirection = Vector2.right;
    private bool runtimeMaterialCreated;
    private float baseArcAngle; // Inspector'dan girilen orijinal acı

    // ------------------------------------------------------------------ //

    private void Awake()
    {
        baseArcAngle = arcAngle;
        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();
        InitLineRenderer();
    }

    private void InitLineRenderer()
    {
        ConfigureLineRenderer(lr, arcSegments + 1);
    }

    private void ConfigureLineRenderer(LineRenderer target, int positions)
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
            target.material = new Material(Shader.Find("Sprites/Default"));
            runtimeMaterialCreated = true;
        }

        target.sortingLayerName = "Default";
        target.sortingOrder = 5;
        target.enabled = false;
    }

    private void OnDestroy()
    {
        if (runtimeMaterialCreated && lr != null && lr.material != null)
            Destroy(lr.material);
    }

    // ================================================================== //
    // PUBLIC API
    // ================================================================== //

    public void UpdateVisuals(Vector2 origin, Vector2 dir, bool isAttacking, bool isParrying, float overrideAngle = -1f, float parryEdgeThickness = -1f)
    {
        if (overrideAngle > 0f)
            arcAngle = overrideAngle;
        else
            arcAngle = baseArcAngle;

        if (dir.sqrMagnitude > 0.0001f)
            lastDirection = dir.normalized;

        bool isVisible = isAttacking || isParrying;
        lr.enabled = isVisible;
        if (isVisible)
        {
            DrawArc(origin, range, isAttacking, isParrying, parryEdgeThickness);
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

    private void DrawArc(Vector2 origin, float range, bool isAttacking, bool isParrying, float parryEdgeThickness)
    {
        Color c = arcColorIdle;
        if (isAttacking) c = arcColorActive;
        else if (isParrying) c = arcColorParry;

        lr.startColor = c;
        lr.endColor = c;
        float drawWidth = lineWidth;
        if (isParrying && parryEdgeThickness > 0f)
            drawWidth = Mathf.Max(lineWidth, parryEdgeThickness);

        lr.startWidth = drawWidth;
        lr.endWidth = drawWidth;

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
