using UnityEngine;
using System.Collections;

/// <summary>
/// Oda prefab'ının duvarlarına yerleştirilen fiziksel kapı.
/// Oda temizlenene kadar kilitli, temizlenince açılır ve ödül ikonu belirir.
/// Oyuncu üstüne gelince sahne geçişi başlar.
/// </summary>
public class RewardDoor : MonoBehaviour
{
    public enum DoorState { Locked, Unlocked, Used }

    public enum DoorDirection { Left, Right, Top, Bottom }

    [Header("Yön")]
    [Tooltip("Kapının odadaki yönü. Soldan giren oyuncu sağdaki kapının entryPoint'inde doğar.")]
    public DoorDirection direction = DoorDirection.Right;

    [Tooltip("Oyuncu bu kapıdan girdiğinde doğacağı nokta (child Transform).")]
    public Transform entryPoint;

    [Header("Durum")]
    [SerializeField] private DoorState currentState = DoorState.Locked;

    [Header("Ödül")]
    [Tooltip("Bu kapıdan geçince verilecek ödül (ScriptableObject)")]
    public RewardDefinitionSO doorReward;
    private int rewardChoiceIndex = -1;

    [Header("Görseller")]
    [Tooltip("Kapının ana sprite'ı (kilitli/açık görünüm)")]
    public SpriteRenderer doorRenderer;
    [Tooltip("Ödül ikonunu gösteren child sprite")]
    public SpriteRenderer rewardIconRenderer;
    [Tooltip("Kilit ikonu (opsiyonel, kilitliyken görünür)")]
    public GameObject lockIcon;

    [Header("Kilitli Görünüm")]
    public Color lockedTint = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color unlockedTint = Color.white;

    [Header("Ödül Renkleri (SO'da tintColor yoksa fallback)")]
    public Color defaultRewardColor = Color.white;

    [Header("Açılma Efekti")]
    public float unlockDuration = 0.4f;
    public float scalePunchAmount = 0.2f;

    [Header("Etkileşim")]
    [Tooltip("true = üstüne basınca geç, false = interact tuşu gerekir (gelecek)")]
    public bool triggerOnTouch = true;

    private bool hasTriggered = false;
    private Collider2D doorCollider;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        ApplyLockedVisuals();
    }

    // ────────────────────────────────────────
    // PUBLIC API (RoomManager tarafından çağrılır)
    // ────────────────────────────────────────

    /// <summary>
    /// Kapıya ödül tipi atar ve görsellerini günceller. Oda spawn'ında çağrılır.
    /// </summary>
    public void Initialize(RewardDefinitionSO rewardSO, int choiceIndex = -1)
    {
        doorReward = rewardSO;
        rewardChoiceIndex = choiceIndex;
        ApplyLockedVisuals();
    }

    /// <summary>
    /// Kapıyı açar. RoomCleared() tarafından çağrılır.
    /// </summary>
    public void Unlock()
    {
        if (currentState != DoorState.Locked) return;
        currentState = DoorState.Unlocked;
        StartCoroutine(UnlockRoutine());
    }

    /// <summary>
    /// Kapıyı tekrar kilitler (oda yeniden başlatılırsa).
    /// </summary>
    public void Lock()
    {
        currentState = DoorState.Locked;
        hasTriggered = false;
        ApplyLockedVisuals();
    }

    // ────────────────────────────────────────
    // GÖRSELLER
    // ────────────────────────────────────────

    private void ApplyLockedVisuals()
    {
        // Kapı karartılmış
        if (doorRenderer != null)
            doorRenderer.color = lockedTint;

        // Ödül ikonu gizli
        if (rewardIconRenderer != null)
            rewardIconRenderer.gameObject.SetActive(false);

        // Kilit ikonu göster
        if (lockIcon != null)
            lockIcon.SetActive(true);
    }

    private void ApplyUnlockedVisuals()
    {
        // Kapı aydınlatılmış
        if (doorRenderer != null)
            doorRenderer.color = unlockedTint;

        // Kilit ikonu gizle
        if (lockIcon != null)
            lockIcon.SetActive(false);

        // Ödül ikonu göster
        if (rewardIconRenderer != null && doorReward != null)
        {
            rewardIconRenderer.gameObject.SetActive(true);

            // SO'da ikon varsa onu kullan
            if (doorReward.icon != null)
                rewardIconRenderer.sprite = doorReward.icon;

            // SO'daki tintColor'ı kullan
            rewardIconRenderer.color = doorReward.tintColor;
        }
    }

    // ────────────────────────────────────────
    // AÇILMA EFEKTİ
    // ────────────────────────────────────────

    private IEnumerator UnlockRoutine()
    {
        // Scale punch + renk geçişi
        Vector3 originalScale = transform.localScale;
        Vector3 punchedScale = originalScale * (1f + scalePunchAmount);

        // 1. Büyü
        float t = 0f;
        float halfDuration = unlockDuration * 0.4f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerp = t / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, punchedScale, lerp);

            // Renk geçişi eş zamanlı
            if (doorRenderer != null)
                doorRenderer.color = Color.Lerp(lockedTint, unlockedTint, lerp);

            yield return null;
        }

        // 2. Küçül (geri)
        t = 0f;
        float secondHalf = unlockDuration * 0.6f;
        while (t < secondHalf)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punchedScale, originalScale, t / secondHalf);
            yield return null;
        }

        transform.localScale = originalScale;
        ApplyUnlockedVisuals();
    }

    // ────────────────────────────────────────
    // OYUNCU ETKİLEŞİMİ
    // ────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnTouch) return;
        if (currentState != DoorState.Unlocked) return;
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        UseDoor(other.gameObject);
    }

    /// <summary>
    /// Interact tuşuyla kapıyı kullanmak için (gelecek).
    /// </summary>
    public void InteractDoor(GameObject player)
    {
        if (currentState != DoorState.Unlocked) return;
        if (hasTriggered) return;
        UseDoor(player);
    }

    private void UseDoor(GameObject player)
    {
        hasTriggered = true;
        currentState = DoorState.Used;

        // Diğer kapıları kapat
        RewardDoor[] allDoors = FindObjectsByType<RewardDoor>(FindObjectsSortMode.None);
        foreach (var door in allDoors)
        {
            if (door != this && door.currentState == DoorState.Unlocked)
            {
                door.Lock();
            }
        }

        // RunManager'a seçimi ve kapı yönünü bildir
        if (RunManager.Instance != null)
        {
            RunRewardContext rewardContext = RunRewardResolver.CreateContext(doorReward, rewardChoiceIndex);
            RunManager.Instance.SetNextRewardContext(doorReward, rewardContext);
            RunManager.Instance.lastDoorDirection = (int)direction;

            // Oyuncu verilerini kaydet
            PlayerCombat pCombat = player.GetComponent<PlayerCombat>();
            RunManager.Instance.SavePlayerState(pCombat, TempoManager.Instance);
        }

        // Sahne geçişi
        if (LevelManager.Instance != null)
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.FadeOut(() => {
                    LevelManager.Instance.LoadNextLevel();
                });
            }
            else
            {
                LevelManager.Instance.LoadNextLevel();
            }
        }
    }
}
