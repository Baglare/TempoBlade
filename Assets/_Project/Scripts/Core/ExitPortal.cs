using UnityEngine;

public class ExitPortal : MonoBehaviour
{
    [Header("Portal Settings")]
    public RewardType portalReward;
    public SpriteRenderer rewardIconRenderer; // Uzerinde odulu gosteren icon
    
    // Gecici renkler (Ikonlar hazir olana kadar)
    public Color healColor = Color.green;
    public Color maxHealthColor = Color.red;
    public Color damageColor = Color.yellow;
    public Color tempoColor = Color.cyan;

    private bool hasTriggered = false;

    public void Initialize(RewardType type)
    {
        portalReward = type;
        
        // Gorsel geribildirim (Renk veya Sprite)
        if (rewardIconRenderer != null)
        {
            switch (type)
            {
                case RewardType.Heal: rewardIconRenderer.color = healColor; break;
                case RewardType.MaxHealth: rewardIconRenderer.color = maxHealthColor; break;
                case RewardType.DamageUp: rewardIconRenderer.color = damageColor; break;
                case RewardType.TempoBoost: rewardIconRenderer.color = tempoColor; break;
                default: rewardIconRenderer.color = Color.white; break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            // Odanin aktif olup olmadigi kontrolu (Zaten sadece oda bitince acilacaklari icin guvenli ama yine de)
            if (RoomManager.Instance != null && RoomManager.Instance.isRoomActive)
            {

                return;
            }

            hasTriggered = true;

            // RunManager'a secimi bildir
            if (RunManager.Instance != null)
            {
                RunManager.Instance.SetNextRewardContext(portalReward);
                
                // Oyuncu verilerini (Can, Hasar, Tempo) diger sahneye aktarmak uzere kaydet
                PlayerCombat pCombat = other.GetComponent<PlayerCombat>();
                RunManager.Instance.SavePlayerState(pCombat, TempoManager.Instance);
            }

            // Diger portallari kapat/yok et (Gorsel cila)
            // ... (Istege bagli)

            // Sonraki levele gec (Fade Out sonrasi)
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
}
