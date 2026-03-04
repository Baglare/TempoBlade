using UnityEngine;

[CreateAssetMenu(fileName = "MaxHealthReward", menuName = "Rewards/MaxHealth")]
public class MaxHealthRewardSO : RewardDefinitionSO
{
    [Header("MaxHealth Ayarları")]
    public float maxHealthIncrease = 10f;

    public override void GrantReward(PlayerCombat player)
    {
        player.maxHealth += maxHealthIncrease;
        player.currentHealth += maxHealthIncrease; // Mevcut canı da arttır
    }

    public override string GetDescription() => $"+{maxHealthIncrease} Maks Can";
}
