using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player'in altindaki World-Space Canvas'ta iki slider gosterir:
///   - Yesil  : Aktif parry penceresi (uzama dahil canlanir)
///   - Turuncu: Parry sonrasi karsi saldiri penceresi
/// Parry aktifken fare yonunde yarim daire arc cizer.
/// Counter penceresi sirasinda karakter altinrengine boyar.
///
/// LateUpdate, canvas'in oyuncu donusuyle birlikte donmesini ve
/// sola bakarken aynalanmasini engeller.
/// </summary>
public class ParryIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Sahne icindeki ParrySystem (otomatik bulunur, bos birakilabilir)")]
    [SerializeField] private ParrySystem parrySystem;
    [Tooltip("Yesil parry penceresi slider'i")]
    [SerializeField] private Slider parrySlider;
    [Tooltip("Turuncu karsi saldiri slider'i")]
    [SerializeField] private Slider counterSlider;
    [Tooltip("Karsi saldiri sirasinda rengi degisecek player SpriteRenderer")]
    [SerializeField] private SpriteRenderer playerSprite;

    [Header("Colors")]
    public Color counterGlowColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Color parryNormalColor = new Color(0.20f, 0.95f, 0.45f, 1f);
    public Color perfectWindowColor = new Color(1f, 0.82f, 0.2f, 1f);
    public Color perfectSuccessColor = Color.white;
    public Color counterNormalColor = new Color(1f, 0.72f, 0.15f, 1f);
    public Color counterUrgentColor = new Color(1f, 0.25f, 0.15f, 1f);
    [Range(0f, 1f)] public float counterUrgentThreshold = 0.25f;
    public float perfectPulseScale = 1.15f;
    public float perfectSuccessFlashDuration = 0.12f;



    // ── Internal State ──────────────────────────────────────────────────
    private Color     originalSpriteColor = Color.white;
    private Vector3   baseSelfScale;
    private Vector3   parrySliderBaseScale = Vector3.one;
    private Vector3   counterSliderBaseScale = Vector3.one;
    private Vector2   currentParryDir;
    private Transform playerRoot;
    private Image     parryFillImage;
    private Image     counterFillImage;
    private float     perfectSuccessFlashTimer;



    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        baseSelfScale = transform.localScale;
        playerRoot    = transform.parent;

        // parrySystem'i erken bul — OnEnable'da abone olmak icin gerekli
        if (parrySystem == null)
            parrySystem = GetComponentInParent<ParrySystem>();

    }



    private void OnDestroy()
    {
    }

    private void Start()
    {
        if (playerSprite == null)
        {
            var pc = GetComponentInParent<PlayerCombat>();
            if (pc != null) playerSprite = pc.GetComponentInChildren<SpriteRenderer>();
        }

        if (playerSprite != null)
            originalSpriteColor = playerSprite.color;

        if (parrySlider != null)
        {
            parrySliderBaseScale = parrySlider.transform.localScale;
            if (parrySlider.fillRect != null)
                parryFillImage = parrySlider.fillRect.GetComponent<Image>();
        }

        if (counterSlider != null)
        {
            counterSliderBaseScale = counterSlider.transform.localScale;
            if (counterSlider.fillRect != null)
                counterFillImage = counterSlider.fillRect.GetComponent<Image>();
        }

        SetParrySliderVisible(false);
        SetCounterSliderVisible(false);
    }

    private void OnEnable()
    {
        if (parrySystem == null) return;
        parrySystem.OnParryStarted         += OnParryStartedHandler;
        parrySystem.OnWindowNormalized     += UpdateParrySlider;
        parrySystem.OnCounterNormalized    += UpdateCounterSlider;
        parrySystem.OnCounterWindowStarted += ShowCounterSlider;
        parrySystem.OnCounterWindowEnded   += HideCounterSlider;
        parrySystem.OnParryResolved        += HandleParryResolved;
        parrySystem.OnParryFail            += HideParrySlider;
    }

    private void OnDisable()
    {
        if (parrySystem == null) return;
        parrySystem.OnParryStarted         -= OnParryStartedHandler;
        parrySystem.OnWindowNormalized     -= UpdateParrySlider;
        parrySystem.OnCounterNormalized    -= UpdateCounterSlider;
        parrySystem.OnCounterWindowStarted -= ShowCounterSlider;
        parrySystem.OnCounterWindowEnded   -= HideCounterSlider;
        parrySystem.OnParryResolved        -= HandleParryResolved;
        parrySystem.OnParryFail            -= HideParrySlider;
    }

    private void LateUpdate()
    {
        // 1. Canvas'i dunyaya gore hizali tut — oyuncu donunce slider da donmemeli
        transform.rotation = Quaternion.identity;

        // 2. Oyuncu sola bakinca (lossyScale.x = -1) canvas aynalanmasin
        if (playerRoot != null)
        {
            float px = playerRoot.lossyScale.x;
            float sx  = px < 0f
                ? -Mathf.Abs(baseSelfScale.x)
                :  Mathf.Abs(baseSelfScale.x);
            transform.localScale = new Vector3(sx, baseSelfScale.y, baseSelfScale.z);
        }

        if (perfectSuccessFlashTimer > 0f)
        {
            perfectSuccessFlashTimer -= Time.unscaledDeltaTime;
            if (perfectSuccessFlashTimer <= 0f)
                ApplyPerfectWindowVisuals();
        }
    }

    // ── Event Handlers ───────────────────────────────────────────────────

    private void OnParryStartedHandler(Vector2 dir)
    {
        currentParryDir = dir;
    }

    private void UpdateParrySlider(float normalized)
    {
        if (normalized > 0f && !parrySlider.gameObject.activeSelf)
            SetParrySliderVisible(true);

        if (parrySlider.gameObject.activeSelf)
            parrySlider.value = normalized;

        ApplyPerfectWindowVisuals();

        if (normalized <= 0f)
            SetParrySliderVisible(false);
    }

    private void UpdateCounterSlider(float normalized)
    {
        if (counterSlider != null && counterSlider.gameObject.activeSelf)
            counterSlider.value = normalized;

        ApplyCounterWindowVisuals(normalized);
    }

    private void ShowCounterSlider()
    {
        SetParrySliderVisible(false);
        SetCounterSliderVisible(true);
        ApplyCounterWindowVisuals(1f);
        if (playerSprite != null)
            playerSprite.color = counterGlowColor;
    }

    private void HideCounterSlider()
    {
        SetCounterSliderVisible(false);
        if (playerSprite != null)
            playerSprite.color = originalSpriteColor;

        if (counterFillImage != null)
            counterFillImage.color = counterNormalColor;

        if (counterSlider != null)
            counterSlider.transform.localScale = counterSliderBaseScale;
    }

    private void HideParrySlider()
    {
        SetParrySliderVisible(false);
        ApplyPerfectWindowVisuals();
    }

    private void HandleParryResolved(ParryEventData data)
    {
        if (!data.isPerfect)
            return;

        perfectSuccessFlashTimer = perfectSuccessFlashDuration;

        if (parryFillImage != null)
            parryFillImage.color = perfectSuccessColor;

        if (parrySlider != null)
            parrySlider.transform.localScale = parrySliderBaseScale * (perfectPulseScale + 0.05f);
    }



    // ── Visibility Helpers ───────────────────────────────────────────────

    private void SetParrySliderVisible(bool visible)
    {
        if (parrySlider != null)
            parrySlider.gameObject.SetActive(visible);
    }

    private void SetCounterSliderVisible(bool visible)
    {
        if (counterSlider != null)
            counterSlider.gameObject.SetActive(visible);
    }

    private void ApplyPerfectWindowVisuals()
    {
        if (parrySlider == null || parryFillImage == null)
            return;

        if (!parrySlider.gameObject.activeSelf)
        {
            parryFillImage.color = parryNormalColor;
            parrySlider.transform.localScale = parrySliderBaseScale;
            return;
        }

        if (perfectSuccessFlashTimer > 0f)
            return;

        bool inPerfectWindow = parrySystem != null &&
                               parrySystem.IsParryActive &&
                               parrySystem.enablePerfectParry &&
                               parrySystem.IsPerfectWindowActive;

        parryFillImage.color = inPerfectWindow ? perfectWindowColor : parryNormalColor;
        parrySlider.transform.localScale = inPerfectWindow
            ? parrySliderBaseScale * perfectPulseScale
            : parrySliderBaseScale;
    }

    private void ApplyCounterWindowVisuals(float normalized)
    {
        if (counterSlider == null || counterFillImage == null)
            return;

        bool isUrgent = normalized <= counterUrgentThreshold;
        counterFillImage.color = isUrgent ? counterUrgentColor : counterNormalColor;
        counterSlider.transform.localScale = isUrgent
            ? counterSliderBaseScale * 1.08f
            : counterSliderBaseScale;
    }
}
