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



    // ── Internal State ──────────────────────────────────────────────────
    private Color     originalSpriteColor = Color.white;
    private Vector3   baseSelfScale;
    private Vector2   currentParryDir;
    private Transform playerRoot;



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

        if (normalized <= 0f)
            SetParrySliderVisible(false);
    }

    private void UpdateCounterSlider(float normalized)
    {
        if (counterSlider.gameObject.activeSelf)
            counterSlider.value = normalized;
    }

    private void ShowCounterSlider()
    {
        SetParrySliderVisible(false);
        SetCounterSliderVisible(true);
        if (playerSprite != null)
            playerSprite.color = counterGlowColor;
    }

    private void HideCounterSlider()
    {
        SetCounterSliderVisible(false);
        if (playerSprite != null)
            playerSprite.color = originalSpriteColor;
    }

    private void HideParrySlider()
    {
        SetParrySliderVisible(false);
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
}
