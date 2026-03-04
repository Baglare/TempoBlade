using UnityEngine;

[CreateAssetMenu(fileName = "HealReward", menuName = "Rewards/Heal")]
public class HealRewardSO : RewardDefinitionSO
{
    [Header("Heal Ayarları")]
    public float healAmount = 20f;

    public override void GrantReward(PlayerCombat player)
    {
        player.currentHealth = Mathf.Min(player.currentHealth + healAmount, player.maxHealth);
    }

    public override string GetDescription() => $"+{healAmount} Can";
}
