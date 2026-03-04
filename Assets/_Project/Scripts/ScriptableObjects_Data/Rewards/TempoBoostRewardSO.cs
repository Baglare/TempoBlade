using UnityEngine;

[CreateAssetMenu(fileName = "TempoBoostReward", menuName = "Rewards/TempoBoost")]
public class TempoBoostRewardSO : RewardDefinitionSO
{
    [Header("TempoBoost Ayarları")]
    [Tooltip("Tempo kazanım çarpanına eklenecek miktar (0.25 = %25)")]
    public float tempoGainBonus = 0.25f;

    public override void GrantReward(PlayerCombat player)
    {
        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.tempoGainMultiplier += tempoGainBonus;
        }
    }

    public override string GetDescription() => $"+%{tempoGainBonus * 100f:0} Tempo Kazanımı";
}
