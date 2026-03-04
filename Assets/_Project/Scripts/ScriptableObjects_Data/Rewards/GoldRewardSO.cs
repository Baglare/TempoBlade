using UnityEngine;

[CreateAssetMenu(fileName = "GoldReward", menuName = "Rewards/Gold")]
public class GoldRewardSO : RewardDefinitionSO
{
    [Header("Gold Ayarları")]
    [Tooltip("Varsayılan altın miktarı (RoomSO'da goldReward > 0 ise o kullanılır)")]
    public int defaultGoldAmount = 10;

    public override void GrantReward(PlayerCombat player)
    {
        if (EconomyManager.Instance == null) return;

        int goldAmount = defaultGoldAmount;

        // RoomSO'daki goldReward değerini kullan (varsa)
        if (RunManager.Instance != null)
        {
            RoomSO currentRoom = RunManager.Instance.GetCurrentRoomData();
            if (currentRoom != null && currentRoom.goldReward > 0)
                goldAmount = currentRoom.goldReward;
        }

        EconomyManager.Instance.AddRunGold(goldAmount);
    }

    public override string GetDescription() => $"+{defaultGoldAmount} Altın";
}
