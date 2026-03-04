using UnityEngine;

[CreateAssetMenu(fileName = "DamageUpReward", menuName = "Rewards/DamageUp")]
public class DamageUpRewardSO : RewardDefinitionSO
{
    [Header("DamageUp Ayarları")]
    [Tooltip("Hasar çarpanına eklenecek miktar (0.2 = %20)")]
    public float damageMultiplierBonus = 0.2f;

    public override void GrantReward(PlayerCombat player)
    {
        player.damageMultiplier += damageMultiplierBonus;
    }

    public override string GetDescription() => $"+%{damageMultiplierBonus * 100f:0} Hasar";
}
