using UnityEngine;

[CreateAssetMenu(fileName = "ResourceReward", menuName = "Rewards/Resource")]
public class ResourceRewardSO : RewardDefinitionSO
{
    [Header("Resource Reward Settings")]
    public ProgressionResourceType resourceType = ProgressionResourceType.WeaponMaterial;
    public int defaultAmount = 1;
    public bool useCurrentRoomGoldFallbackForGold = true;

    public override void GrantReward(PlayerCombat player)
    {
        int amount = Mathf.Max(0, defaultAmount);
        if (resourceType == ProgressionResourceType.Gold && useCurrentRoomGoldFallbackForGold && RunManager.Instance != null)
        {
            RoomSO currentRoom = RunManager.Instance.GetCurrentRoomData();
            if (currentRoom != null && currentRoom.goldReward > 0)
                amount = currentRoom.goldReward;
        }

        RunResourceBankService.AddResource(resourceType, amount);
    }

    public override string GetDescription()
    {
        return "+" + defaultAmount + " " + ProgressionResourceUtility.GetDisplayName(resourceType);
    }
}
