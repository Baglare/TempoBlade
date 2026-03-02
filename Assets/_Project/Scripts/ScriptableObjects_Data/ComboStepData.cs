using UnityEngine;

public enum ComboStepType { Normal, MultiHit, DashStrike }

[System.Serializable]
public class ComboStepData
{
    [Header("Hit")]
    public ComboStepType type = ComboStepType.Normal;
    [Tooltip("Silah hasarına çarpan (1.0 = base hasar)")]
    public float damageMultiplier = 1f;
    [Tooltip("Bu adımda silah menzeline eklenen bonus")]
    public float rangeBonus = 0f;

    [Header("Timing")]
    [Tooltip("Vuruş öncesi bekleme süresi (saniye). 0 = anında.")]
    public float windupTime = 0f;
    [Tooltip("Sonraki adım inputu için minimum bekleme (saniye)")]
    public float cooldownAfter = 0.15f;
    [Tooltip("Sonraki adım için input penceresi (saniye). Son adımda 0 bırak.")]
    public float comboWindow = 0.6f;

    [Header("Flags")]
    [Tooltip("Windup sırasında tüm oyuncu inputları bloke edilir")]
    public bool isUninterruptible = false;

    [Header("MultiHit (yalnızca MultiHit tipinde)")]
    [Tooltip("Ardışık vuruş sayısı. Toplam hasar = damageMultiplier × base hasar")]
    public int hitCount = 5;
    [Tooltip("Ardışık vuruşlar arası gecikme (saniye)")]
    public float timeBetweenHits = 0.07f;

    [Header("DashStrike (yalnızca DashStrike tipinde)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.25f;
}
