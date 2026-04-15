using UnityEngine;

/// <summary>
/// Tek bir yetenek düğümünün veri tanımı.
/// Tier, ön koşullar, etkiler, commitment bayrağı ve görünürlük koşullarını taşır.
/// </summary>
[CreateAssetMenu(menuName = "TempoBlade/Skill Tree/Skill Node", order = 100)]
public class SkillNodeSO : ScriptableObject
{
    [Header("Kimlik")]
    [Tooltip("Benzersiz node kimliği (ör: 'tech_atk_t2_crit'). Save/load ve referanslar bu ID üzerinden çalışır.")]
    public string nodeId;

    [Tooltip("Oyuncuya gösterilen ad.")]
    public string displayName;

    [TextArea(2, 4)]
    [Tooltip("Oyuncuya gösterilen açıklama.")]
    public string description;

    public Sprite icon;

    [Header("Kademe")]
    [Range(1, 3)]
    [Tooltip("Node'un kademe seviyesi.")]
    public int tier = 1;

    [Tooltip("Normal progression modunda bu node'un acilabilir olmasi icin gereken tree rank. 0 veya altinda otomatik varsayilan kullanilir.")]
    public int requiredTreeRank = 0;

    [Tooltip("Bu node açıldığında karşıt eksenin commitment kilidi tetiklenir mi?")]
    public bool isCommitmentNode = false;

    [Tooltip("T3 alt-bölge etiketi. Form overlay bu etikete göre bölge erişimini belirler. T1-T2 için boş bırakılabilir.")]
    public string regionTag;

    [Header("Ön Koşullar")]
    [Tooltip("Bu node'dan önce açılması gereken node'lar.")]
    public SkillNodeSO[] prerequisites;

    [Header("Etkiler")]
    [Tooltip("Bu node aktifken uygulanacak etkiler (stat/flag/feature).")]
    public NodeEffect[] effects;

    [Header("Görünürlük")]
    [Tooltip("Node'un Hidden → Visible geçiş koşulu.")]
    public NodeVisibility visibility;

    [Header("Başlangıç Durumu")]
    [Tooltip("Oyun başında otomatik olarak açık mı?")]
    public bool startsUnlocked = false;

#if UNITY_EDITOR
    /// <summary>
    /// Inspector'da her değişiklikte effect key'leri doğrular.
    /// </summary>
    private void OnValidate()
    {
        if (effects == null) return;
        for (int i = 0; i < effects.Length; i++)
        {
            EffectKeyRegistry.WarnIfUnknown(
                effects[i].key,
                $"(Node: '{displayName}', Effect index: {i})"
            );
        }
    }
#endif
}
